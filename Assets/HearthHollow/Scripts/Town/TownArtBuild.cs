// Hearth & Hollow — Hearth's Hollow dressed in the Cainos "Pixel Art Top Down - Basic" pack (Settings.townArt).
// Ground: grass with flowers, a stone plaza round the Hearth, cobbled lanes; the raised ground round the town has stone faces
// and is planted with trees and bushes. Spots get Cainos props instead of placeholder blocks where the pack has something
// that fits (the Hearth sits on the altar, the gate is a stone arch, crates and barrels at the larder, the smithy, the workshop…).
// The cooking stations, the flame, people, creatures and stalls stay as they were (the pack has none of those).
using HearthHollow.Dungeon;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace HearthHollow.Town
{
    public partial class TownController
    {
        TownArt Art { get { return Settings != null ? Settings.townArt : null; } }
        Tilemap _wallsMap;

        static void SetLayer(Renderer r, string layer)
        {
            int id = SortingLayer.NameToID(layer);
            if (r != null && SortingLayer.IsValid(id)) r.sortingLayerID = id;
        }

        /// A wider, softer twin of a light that only lights the Walls layer (walls glow further than the ground).
        static Light2D WallGlow(Light2D src, float reach = 1.6f, float k = 0.6f)
        {
            if (src == null) return null;
            var g = new GameObject("Wall Glow");
            g.transform.SetParent(src.transform, false);
            var l = g.AddComponent<Light2D>();
            l.shadowsEnabled = false;   // walls glow, they aren't shadowed
            l.lightType = Light2D.LightType.Point;
            l.color = src.color; l.intensity = src.intensity * k;
            l.pointLightOuterRadius = src.pointLightOuterRadius * reach;
            l.pointLightInnerRadius = src.pointLightInnerRadius;
            l.falloffIntensity = 0.3f;
            DungeonController.ApplyToLayers(l, "Walls");
            return l;
        }

        /// Townsfolk and critters read as silhouettes in the dark (night / storm), like monsters in the dungeon.
        void Silhouette(CharacterAnimator anim, bool critter, string monsterId, float size)
        {
            if (anim == null || Settings == null || anim.transform.parent == null) return;
            var g = anim.transform.parent.gameObject.AddComponent<EnemyGlow>();
            if (critter)
            {
                var p = Settings.ProfileFor(monsterId);
                g.SetupCharacter(anim, p.rimColor, p.eyeColor, true, p.eyeHeight, p.eyeSpread, size, Settings);
            }
            else g.SetupCharacter(anim, new Color(1f, 0.82f, 0.55f), Color.white, false, 0f, 0f, 1f, Settings);   // friendly: warm rim, no eyes
        }

        /// Your own warm light, like in the dungeon, when it's dark out.
        void CarriedLight()
        {
            if (Player == null || Settings == null) return;
            var lgo = new GameObject("Player Light");
            lgo.transform.SetParent(Player.transform, false);
            lgo.transform.localPosition = new Vector3(0, 0.2f, 0);   // the centre of your collision circle (see the dungeon's player light)
            var l = lgo.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = new Color(1f, 0.76f, 0.48f);
            l.intensity = Settings.carriedLight * 0.8f;
            l.pointLightOuterRadius = Settings.carriedLightRadius * 0.8f;
            l.pointLightInnerRadius = 0.3f;
            l.falloffIntensity = Settings.carriedLightFalloff;
            DungeonController.ApplyToAllLayers(l);
            DungeonController.CastShadows(l);
            lgo.AddComponent<ShadowSafeLight>();   // never goes dark if its centre ends up inside a wall
            var w = WallGlow(l, Settings.wallGlowReach, Settings.wallGlow / Mathf.Max(0.01f, l.intensity));
            if (w != null) w.falloffIntensity = Settings.wallGlowFalloff;
        }

        /// Drops a Cainos prefab at pos (its feet), sorted with everything else by y. Not solid = no colliders.
        GameObject Prop(string name, Vector2 pos, Transform parent = null, float scale = 1f, bool solid = true, Color? tint = null, int? order = null)
        {
            var art = Art; if (art == null) return null;
            var pf = art.Prop(name);
            if (pf == null) { Debug.LogWarning("[Town] No Cainos prop called " + name); return null; }
            var go = Instantiate(pf, parent != null ? parent : _root.transform);
            go.name = name;
            go.transform.position = new Vector3(pos.x, pos.y, 0);
            go.transform.localScale = new Vector3(scale, scale, 1);
            foreach (var rb in go.GetComponentsInChildren<Rigidbody2D>()) rb.bodyType = RigidbodyType2D.Static;
            if (!solid) foreach (var col in go.GetComponentsInChildren<Collider2D>()) Destroy(col);
            int baseOrder = order.HasValue ? order.Value : DungeonController.SortOrder(pos.y);
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                bool shadow = sr.sharedMaterial != null && sr.sharedMaterial.name.Contains("Shadow");
                int rel = Mathf.Clamp(sr.sortingOrder, -4, 4);
                sr.sortingLayerName = "Default";
                sr.sortingOrder = shadow ? 3 : baseOrder + rel;
                if (tint.HasValue && !shadow) sr.color = tint.Value * sr.color;
            }
            return go;
        }

        void BuildTilesArt(TownArt art)
        {
            var grid = new GameObject("Grid").AddComponent<Grid>();
            grid.transform.SetParent(_root.transform, false);
            var ground = NewMap(grid, "Ground", 0);
            var walls = NewMap(grid, "Walls", 2);
            var decor = NewMap(grid, "Decor", 1);
            walls.gameObject.AddComponent<TilemapCollider2D>();
            _wallsMap = walls;
            // same layers as the dungeon: walls catch light from further away than the ground (see WallGlow)
            SetLayer(ground.GetComponent<TilemapRenderer>(), "Ground"); SetLayer(decor.GetComponent<TilemapRenderer>(), "Ground");
            SetLayer(walls.GetComponent<TilemapRenderer>(), "Walls");
            DungeonController.AddWallShadows(walls);
            Spots = TownLayout.Spots();   // (so decor keeps clear of them; Build() fills the full list next)
            var rng = new System.Random(1234);
            for (int x = -4; x < TownLayout.Width + 4; x++)
                for (int y = -4; y < TownLayout.Height + 4; y++)
                {
                    var c = new Vector3Int(x, y, 0);
                    if (TownLayout.IsOpen(x, y))
                    {
                        bool paved = TownLayout.IsPlaza(x, y) || TownLayout.IsPath(x, y);
                        TileBase t = TownLayout.IsPlaza(x, y) ? art.square : TownLayout.IsPath(x, y) ? art.path
                                   : (art.grassFlowers != null && rng.NextDouble() < art.flowerChance ? art.grassFlowers : art.grass);
                        ground.SetTile(c, t);
                        if (!paved && art.decor.Count > 0 && rng.NextDouble() < art.decorDensity && !NearSpot(new Vector2(x + 0.5f, y + 0.5f), 0.8f))
                            decor.SetTile(c, art.decor[rng.Next(art.decor.Count)]);
                    }
                    else
                    {
                        ground.SetTile(c, art.grass);   // under the trees
                        walls.SetTile(c, TownLayout.IsOpen(x, y - 1) ? art.wallFace : art.wallTop);
                    }
                }
        }

        void BuildFencesArt()
        {
            // a low stone wall along each plot's edge, with a wooden gate where the plot's sign is
            foreach (var f in TownLayout.Fences)
            {
                TownSpot plot = null;
                foreach (var s in Spots) if (s.action == TownAction.Plot && Mathf.Abs(s.pos.y - f.z) < 1.5f && s.pos.x >= f.x && s.pos.x <= f.y + 1) plot = s;
                int gx = plot != null ? Mathf.FloorToInt(plot.pos.x) : -999;
                for (int x = f.x; x <= f.y; x++)
                    if (x != gx && _wallsMap != null) _wallsMap.SetTile(new Vector3Int(x, f.z, 0), Art.wallFace);
            }
        }

        /// Cainos props for a spot. False = no art for it (keep the placeholder block).
        bool DressSpot(GameObject go, TownSpot s)
        {
            var t = go.transform; Vector2 p = s.pos;
            switch (s.action)
            {
                case TownAction.Hearth:
                    Prop("Altar 01", p + new Vector2(0, 0.3f), t, 0.85f, false, null, DungeonController.SortOrder(p.y) - 2);
                    return true;
                case TownAction.Gate:
                    Prop("Gate 02", p + new Vector2(0, -0.2f), t, 1f, false);
                    return true;
                case TownAction.Larder:
                    Prop("Barrel 01", p + new Vector2(-0.8f, 0f), t, 1f, false);
                    Prop("Crate 01", p + new Vector2(0.15f, 0.05f), t, 1f, false);
                    Prop("Pot 01", p + new Vector2(0.95f, -0.1f), t, 1f, false);
                    return true;
                case TownAction.Upgrades:   // the smithy: a stone forge arch with a glow inside
                    Prop("Gate 01", p, t, 1f, false);
                    Prop("Barrel 01", p + new Vector2(-1.6f, -0.1f), t, 1f, false);
                    Prop("Stone Cube 01", p + new Vector2(1.5f, -0.1f), t, 0.8f, false);
                    Glow(t, new Vector2(0, 0.6f), new Color(1f, 0.55f, 0.2f), 2.2f, 0.9f);
                    return true;
                case TownAction.Workshop:
                    Prop("Stone Bench 01 S", p, t, 1f, false);
                    Prop("Crate 01", p + new Vector2(-1.4f, 0.05f), t, 1f, false);
                    Prop("Crate 02", p + new Vector2(1.35f, 0.05f), t, 1f, false);
                    return true;
                case TownAction.Training:
                    Prop("Rune Pillar X2", p, t, 1f, false);
                    return true;
                case TownAction.BackToCamp:
                    Prop("Road Sign 01 W", p, t, 1f, false);
                    return true;
                case TownAction.Plot:
                    Prop("Wooden Gate 01", new Vector2(Mathf.Floor(p.x) + 0.5f, p.y + 0.6f), t, 1f, false);
                    return true;
                // market stalls: a stone counter from the pack under a striped awning, with their wares on top
                case TownAction.Merchant:
                    Stall(t, p, s.color, new Color(0.62f, 0.5f, 0.75f), new Color(0.9f, 0.8f, 0.4f));
                    Prop("Crate 02", p + new Vector2(-1.55f, 0f), t, 0.8f, false);
                    return true;
                case TownAction.Armourer:
                    Stall(t, p, s.color, new Color(0.62f, 0.64f, 0.7f), new Color(0.75f, 0.76f, 0.8f));
                    Prop("Barrel 01", p + new Vector2(1.55f, 0f), t, 0.9f, false);
                    return true;
                case TownAction.Baker:
                    Stall(t, p, s.color, new Color(0.85f, 0.62f, 0.3f), new Color(0.95f, 0.78f, 0.45f));
                    Prop("Crate 01", p + new Vector2(-1.3f, 0f), t, 0.75f, false);
                    return true;
                case TownAction.Florist:
                    Stall(t, p, s.color, new Color(1f, 0.55f, 0.7f), new Color(1f, 0.85f, 0.3f));
                    Prop("Pot 01", p + new Vector2(1.2f, -0.1f), t, 0.8f, false);
                    Prop("Pot 03", p + new Vector2(1.65f, 0f), t, 0.8f, false);
                    return true;
                case TownAction.Notice:
                {
                    int o = DungeonController.SortOrder(p.y);
                    Box(t, new Vector2(-0.5f, 0.55f), new Vector2(0.12f, 1.1f), Wood, o);
                    Box(t, new Vector2(0.5f, 0.55f), new Vector2(0.12f, 1.1f), Wood, o);
                    Box(t, new Vector2(0, 0.8f), new Vector2(1.3f, 0.7f), WoodLight, o + 1);
                    Box(t, new Vector2(0, 1.17f), new Vector2(1.4f, 0.08f), Wood, o + 2);
                    Box(t, new Vector2(-0.3f, 0.82f), new Vector2(0.3f, 0.38f), new Color(0.93f, 0.9f, 0.8f), o + 2, -4f);
                    Box(t, new Vector2(0.2f, 0.78f), new Vector2(0.34f, 0.3f), new Color(0.98f, 0.95f, 0.85f), o + 2, 3f);
                    Box(t, new Vector2(0.38f, 0.95f), new Vector2(0.2f, 0.2f), new Color(0.9f, 0.75f, 0.6f), o + 3, 8f);
                    Prop("Pot 02", p + new Vector2(-0.95f, -0.1f), t, 0.8f, false);
                    return true;
                }
                case TownAction.Bed:   // a hammock slung between two trees
                {
                    Prop("Tree 02", p + new Vector2(-1.55f, 0.1f), t, 0.75f, false);
                    Prop("Tree 01", p + new Vector2(1.55f, 0.1f), t, 0.75f, false);
                    int o = DungeonController.SortOrder(p.y) + 2;
                    Color cloth = new Color(0.78f, 0.62f, 0.42f), rope = new Color(0.9f, 0.85f, 0.7f);
                    Box(t, new Vector2(-1.25f, 0.85f), new Vector2(0.5f, 0.04f), rope, o, -20f);
                    Box(t, new Vector2(1.25f, 0.85f), new Vector2(0.5f, 0.04f), rope, o, 20f);
                    for (int i = 0; i < 9; i++)
                    {
                        float x = -1f + i * 0.25f, sag = 0.18f * (1f - (x * x));
                        Box(t, new Vector2(x, 0.75f - sag), new Vector2(0.27f, 0.2f), i % 2 == 0 ? cloth : cloth * 0.9f, o + 1);
                    }
                    Prop("Crate 02", p + new Vector2(0.4f, -0.5f), t, 0.6f, false);
                    return true;
                }
            }
            return false;
        }

        /// A market stall: the pack's stone bench as the counter, two posts and a striped awning, wares on top.
        void Stall(Transform t, Vector2 p, Color awning, Color wareA, Color wareB)
        {
            int o = DungeonController.SortOrder(p.y);
            Prop("Stone Bench 01 S", p + new Vector2(0, -0.05f), t, 1f, false, null, o);
            Box(t, new Vector2(-1.05f, 0.95f), new Vector2(0.1f, 1.9f), Wood, o - 1);
            Box(t, new Vector2(1.05f, 0.95f), new Vector2(0.1f, 1.9f), Wood, o - 1);
            for (int i = 0; i < 6; i++)
            {
                Box(t, new Vector2(-1.0f + i * 0.4f, 1.85f), new Vector2(0.4f, 0.3f), i % 2 == 0 ? awning : new Color(0.95f, 0.93f, 0.88f), o + 4);
                Box(t, new Vector2(-1.0f + i * 0.4f, 1.66f), new Vector2(0.32f, 0.1f), i % 2 == 0 ? awning * 0.85f : new Color(0.85f, 0.83f, 0.78f), o + 4);
            }
            for (int i = 0; i < 4; i++) Dot(t, new Vector2(-0.5f + i * 0.33f, 0.88f), 0.22f, i % 2 == 0 ? wareA : wareB, o + 5);
        }

        /// Trees and bushes on the raised ground, the well and benches in the square, stones round the pond.
        void BuildScenery()
        {
            var root = new GameObject("Scenery").transform;
            root.SetParent(_root.transform, false);
            var rng = new System.Random(4242);
            string[] trees = { "Tree 01", "Tree 02", "Tree 03" };
            string[] bushes = { "Bush 01", "Bush 02", "Bush 03", "Bush 04", "Bush 05", "Bush 06" };
            for (int x = -3; x < TownLayout.Width + 3; x++)
                for (int y = -3; y < TownLayout.Height + 3; y++)
                {
                    if (TownLayout.IsOpen(x, y) || TownLayout.IsOpen(x, y - 1)) continue;   // open ground or the stone face
                    int near = DistanceToOpen(x, y, 4);
                    if (near > 4) continue;
                    double roll = rng.NextDouble();
                    var at = new Vector2(x + 0.5f + (float)(rng.NextDouble() - 0.5) * 0.5f, y + 0.2f);
                    bool openAbove = false;   // a tree's canopy would cover the ground just north of it
                    for (int dx = -1; dx <= 1 && !openAbove; dx++) for (int dy = 1; dy <= 3; dy++) if (TownLayout.IsOpen(x + dx, y + dy)) { openAbove = true; break; }
                    if (near >= 2 && roll < 0.2 && !openAbove) Prop(trees[rng.Next(trees.Length)], at, root, 0.9f + (float)rng.NextDouble() * 0.2f, false);
                    else if (roll < 0.3) Prop(bushes[rng.Next(bushes.Length)], at, root, 1f, false);
                }

            // the square
            Prop("Well 01", new Vector2(32f, 14.2f), root);
            Prop("Stone Bench 01 E", new Vector2(21.3f, 18.6f), root);
            Prop("Stone Bench 01 W", new Vector2(28.7f, 18.6f), root);
            Prop("Statue 01", new Vector2(17.6f, 14f), root);
            Prop("Stone Lantern 01", new Vector2(22.2f, 13.2f), root, 1f, true);
            Prop("Stone Lantern 01", new Vector2(27.8f, 13.2f), root, 1f, true);
            Prop("Road Sign 01 E", new Vector2(21.5f, 10.4f), root, 1f, false);   // the market street, off the south road
            // the training yard
            Prop("Stone Bench 01 S", new Vector2(4f, 15f), root);
            Prop("Rune Pillar Broken", new Vector2(10.5f, 16f), root);

            // stones and bushes round the pond (skipping the jetty side)
            var r = TownLayout.Pond;
            string[] stones = { "Stone 01", "Stone 02", "Stone 03", "Stone 04", "Stone 05" };
            for (float x = r.xMin; x <= r.xMax; x += 0.9f)
            {
                Prop(stones[rng.Next(stones.Length)], new Vector2(x, r.yMax + 0.2f), root, 1f, false);
                Prop(stones[rng.Next(stones.Length)], new Vector2(x, r.yMin - 0.15f), root, 1f, false);
            }
            for (float y = r.yMin + 0.4f; y <= r.yMax; y += 1.1f) Prop(stones[rng.Next(stones.Length)], new Vector2(r.xMin - 0.25f, y), root, 1f, false);
            Prop("Bush 03", new Vector2(r.xMin - 0.1f, r.yMax + 0.9f), root, 1f, false);
        }

        static int DistanceToOpen(int x, int y, int max)
        {
            for (int d = 1; d <= max; d++)
                for (int dx = -d; dx <= d; dx++)
                    for (int dy = -d; dy <= d; dy++)
                        if ((Mathf.Abs(dx) == d || Mathf.Abs(dy) == d) && TownLayout.IsOpen(x + dx, y + dy)) return d;
            return max + 1;
        }
    }
}
