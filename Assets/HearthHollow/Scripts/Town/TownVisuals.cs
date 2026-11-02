// Hearth & Hollow — upgrades you can SEE in Hearth's Hollow (placeholder pixel shapes until real art):
//   • the cooking area: a Pot, a BBQ and a sandwich bench side by side, each growing with its station level
//     (1 campfire → 2 stone ring → 3 fieldstone hearth → 4 clay hood + chimney → 5 iron-trimmed ember forge that glows);
//   • the larder: more shelf racks and crates as the meal pantry grows;
//   • the town itself: flowers (chapter 1+), lanterns (2+) and bunting (3+) as chapters of the Ember Path are cleared.
// The town rebuilds whenever any of these change (LookKey).
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Dungeon;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Town
{
    public partial class TownController
    {
        /// What the visible upgrades depend on: station levels, pantry tier, chapters cleared.
        string LookKey()
        {
            var p = Session.Profile;
            return Kitchen.Level(Session.Content, p, Station.Pot) + "/" + Kitchen.Level(Session.Content, p, Station.BBQ) + "/" +
                   Kitchen.Level(Session.Content, p, Station.Sandwich) + "/" + p.pantryTier + "/" + ChaptersCleared;
        }

        int ChaptersCleared
        {
            get { int n = 0; foreach (var r in Session.Content.routes) if (r.chapter > 0 && Session.Profile.clearedRouteIds.Contains(r.id)) n++; return n; }
        }

        // ───────── little drawing helpers (world units; pos = centre) ─────────
        static SpriteRenderer Shape(Transform parent, Sprite sprite, Vector2 pos, Vector2 size, Color col, int order, float rot = 0f)
        {
            var go = new GameObject("Shape");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, 0, rot);
            var b = sprite.bounds.size;
            go.transform.localScale = new Vector3(size.x / Mathf.Max(0.0001f, b.x), size.y / Mathf.Max(0.0001f, b.y), 1);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.color = col; sr.sortingOrder = order;
            return sr;
        }
        static SpriteRenderer Box(Transform p, Vector2 pos, Vector2 size, Color c, int order, float rot = 0f) { return Shape(p, DungeonFx.Pixel, pos, size, c, order, rot); }
        static SpriteRenderer Dot(Transform p, Vector2 pos, float d, Color c, int order) { return Shape(p, DungeonFx.Circle, pos, new Vector2(d, d), c, order); }

        static readonly Color Stone = new Color(0.55f, 0.55f, 0.53f), StoneDark = new Color(0.4f, 0.4f, 0.4f), Clay = new Color(0.72f, 0.42f, 0.28f),
                              Iron = new Color(0.22f, 0.22f, 0.25f), Wood = new Color(0.5f, 0.33f, 0.18f), WoodLight = new Color(0.66f, 0.47f, 0.28f);

        Light2D Glow(Transform parent, Vector2 pos, Color c, float radius, float intensity)
        {
            var g = new GameObject("Glow");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            var l = g.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = c; l.intensity = intensity; l.pointLightOuterRadius = radius; l.pointLightInnerRadius = radius * 0.2f;
            DungeonController.ApplyToAllLayers(l);
            DungeonController.CastShadows(l, 0.75f, 0.45f);   // as before, plus ShadowSafeLight: lanterns set into a wall get nudged out
            if (Art != null) WallGlow(l);   // lanterns and stations light the walls further
            return l;
        }

        // ───────── the cooking area ─────────
        void BuildKitchenArea(GameObject go, TownSpot s, DungeonTheme theme)
        {
            int baseOrder = DungeonController.SortOrder(s.pos.y) + 2;
            Station[] order = { Station.Pot, Station.BBQ, Station.Sandwich };
            for (int i = 0; i < 3; i++)
            {
                int lv = Kitchen.Level(Session.Content, Session.Profile, order[i]);
                var st = new GameObject(order[i] + " · level " + lv).transform;
                st.SetParent(go.transform, false);
                st.localPosition = new Vector3((i - 1) * 1.0f, 0.15f, 0);
                StationLook(st, order[i], lv, baseOrder, theme);
            }
        }

        void StationLook(Transform t, Station st, int lv, int o, DungeonTheme theme)
        {
            bool fire = st != Station.Sandwich;
            // shared build-up, level by level
            if (lv >= 4) { Box(t, new Vector2(0, 0.95f), new Vector2(0.9f, 0.9f), Clay, o - 1); Box(t, new Vector2(0.28f, 1.55f), new Vector2(0.2f, 0.45f), Clay * 0.85f, o - 1); }   // clay hood + chimney
            if (lv >= 3) Box(t, new Vector2(0, 0.2f), new Vector2(0.95f, 0.32f), StoneDark, o);                                  // fieldstone hearth
            if (lv >= 2 && fire) Shape(t, DungeonFx.Ring, new Vector2(0, lv >= 3 ? 0.42f : 0.25f), new Vector2(0.85f, 0.4f), Stone, o + 1);   // stone ring
            if (lv >= 2 && !fire) Box(t, new Vector2(0, lv >= 3 ? 0.45f : 0.25f), new Vector2(0.95f, 0.18f), WoodLight, o + 1);       // plank bench
            float top = lv >= 3 ? 0.5f : 0.3f;
            if (fire && theme != null && theme.campfire != null)
                Shape(t, theme.campfire, new Vector2(0, top + 0.12f), new Vector2(0.55f, 0.55f), Color.white, o + 2);
            if (st == Station.Pot) Dot(t, new Vector2(0, top + 0.4f), lv >= 3 ? 0.55f : 0.42f, Iron, o + 3);                      // the pot (bigger on a hearth)
            if (st == Station.BBQ && lv >= 2) { Box(t, new Vector2(0, top + 0.32f), new Vector2(0.7f, 0.05f), Iron, o + 3); Box(t, new Vector2(0, top + 0.42f), new Vector2(0.7f, 0.05f), Iron, o + 3); }   // grill bars
            if (st == Station.BBQ && lv < 2) Box(t, new Vector2(0, top + 0.4f), new Vector2(0.6f, 0.05f), Wood, o + 3, 15f);       // a stick of skewers
            if (st == Station.Sandwich)
            {
                if (lv < 2) Box(t, new Vector2(0, 0.25f), new Vector2(0.55f, 0.4f), Wood, o + 1);                                   // a crate to work on
                if (lv >= 3) { Box(t, new Vector2(-0.15f, top + 0.08f), new Vector2(0.45f, 0.08f), new Color(0.85f, 0.75f, 0.55f), o + 2); Dot(t, new Vector2(0.25f, top + 0.12f), 0.22f, new Color(0.85f, 0.62f, 0.3f), o + 3); }   // board + loaf
                if (lv >= 4) { Box(t, new Vector2(0, 1.0f), new Vector2(0.85f, 0.5f), Wood, o - 1); for (int k = 0; k < 3; k++) Dot(t, new Vector2(-0.25f + k * 0.25f, 1.05f), 0.16f, new Color(0.85f, 0.62f, 0.3f), o); }   // bread shelf
                if (lv >= 5) for (int k = 0; k < 4; k++) Box(t, new Vector2(-0.33f + k * 0.22f, 1.5f), new Vector2(0.22f, 0.22f), k % 2 == 0 ? new Color(0.85f, 0.25f, 0.2f) : Color.white, o);   // striped awning
            }
            if (lv >= 5 && fire)
            {
                Box(t, new Vector2(0, 0.37f), new Vector2(1.0f, 0.08f), Iron, o + 1);                                                   // iron trim
                for (int k = 0; k < 3; k++) Dot(t, new Vector2(-0.25f + k * 0.25f, 0.33f), 0.1f, new Color(1f, 0.55f, 0.15f), o + 2);   // embers
                Glow(t, new Vector2(0, 0.6f), new Color(1f, 0.55f, 0.2f), 2.2f, 0.9f);
            }
        }

        // ───────── pantry shelves by the larder ─────────
        static readonly float[] RackX = { 31.9f, 32.9f, 33.9f, 28.1f };

        void BuildShelves(TownSpot larder)
        {
            var root = new GameObject("Pantry shelves · tier " + Session.Profile.pantryTier).transform;
            root.SetParent(_root.transform, false);
            int racks = Mathf.Clamp(Session.Profile.pantryTier + 1, 1, RackX.Length);
            var rng = new System.Random(3);
            Color[] jars = { new Color(0.85f, 0.3f, 0.3f), new Color(0.95f, 0.8f, 0.3f), new Color(0.4f, 0.7f, 0.35f), new Color(0.6f, 0.45f, 0.8f), new Color(0.9f, 0.6f, 0.3f) };
            for (int i = 0; i < racks; i++)
            {
                var pos = new Vector2(RackX[i], larder.pos.y + 0.1f);
                int o = DungeonController.SortOrder(pos.y);
                Box(root, pos + new Vector2(0, 0.65f), new Vector2(0.8f, 1.3f), Wood, o);
                for (int s = 0; s < 3; s++)
                {
                    float y = 0.3f + s * 0.4f;
                    Box(root, pos + new Vector2(0, y), new Vector2(0.8f, 0.06f), WoodLight, o + 1);
                    for (int j = 0; j < 3; j++) Dot(root, pos + new Vector2(-0.25f + j * 0.25f, y + 0.12f), 0.16f, jars[rng.Next(jars.Length)], o + 2);
                }
                var col = root.gameObject.AddComponent<BoxCollider2D>();
                col.offset = pos + new Vector2(0, 0.2f); col.size = new Vector2(0.8f, 0.4f);
            }
            // crates stack up in front as the pantry grows
            int crates = Session.Profile.pantryTier * 2;
            for (int i = 0; i < crates; i++)
            {
                var pos = new Vector2(31.6f + (i % 4) * 0.55f, larder.pos.y - 0.7f + (i / 4) * 0.3f);
                if (Art != null) { string[] k = { "Crate 01", "Barrel 01", "Crate 02", "Pot 02" }; Prop(k[i % k.Length], pos + new Vector2(0, -0.15f), root, 0.55f, false); continue; }
                Box(root, pos, new Vector2(0.45f, 0.4f), i % 2 == 0 ? Wood : WoodLight, DungeonController.SortOrder(pos.y));
            }
        }

        // ───────── decorations by chapter ─────────
        static readonly Vector2[] LanternSpots =
        {
            new Vector2(16.5f, 26.5f), new Vector2(33.5f, 26.5f), new Vector2(16.5f, 12.8f), new Vector2(33.5f, 12.8f),
            new Vector2(23f, 27.3f), new Vector2(27f, 27.3f), new Vector2(22.4f, 10.5f), new Vector2(27.6f, 10.5f),
            new Vector2(14.5f, 23.4f), new Vector2(36.5f, 23.4f), new Vector2(10f, 9.2f), new Vector2(20.5f, 9.2f),
            new Vector2(18f, 36.5f), new Vector2(32f, 36.5f), new Vector2(39f, 28.5f), new Vector2(39f, 12f),
        };
        static readonly Vector4[] BuntingRuns =   // x0, y0, x1, y1
        {
            new Vector4(17f, 27.6f, 33f, 27.6f), new Vector4(9.5f, 9.7f, 21.5f, 9.7f), new Vector4(17f, 12.6f, 33f, 12.6f), new Vector4(35f, 23.7f, 47f, 23.7f),
        };

        void BuildDecor()
        {
            int ch = ChaptersCleared;
            if (ch <= 0) return;
            var root = new GameObject("Decorations · " + ch + " chapters").transform;
            root.SetParent(_root.transform, false);

            // flowers: 8 a chapter, on open ground away from everything you use
            var rng = new System.Random(99);
            Color[] petals = { new Color(1f, 0.85f, 0.3f), new Color(1f, 0.55f, 0.7f), new Color(0.75f, 0.6f, 1f), Color.white, new Color(1f, 0.5f, 0.3f) };
            int want = 8 * ch, made = 0;
            for (int tries = 0; tries < want * 20 && made < want; tries++)
            {
                var p = new Vector2((float)rng.NextDouble() * TownLayout.Width, (float)rng.NextDouble() * TownLayout.Height);
                if (!TownLayout.IsWalkable(p.x, p.y) || NearSpot(p, 1.8f)) continue;
                int o = DungeonController.SortOrder(p.y);
                if (Art != null)
                {
                    // a little bush or a flower from the pack, the flowers tinted in the chapter colours
                    if (made % 3 == 0) Prop(made % 2 == 0 ? "Bush 01" : "Bush 02", p, root, 0.9f, false);
                    else Prop("Flower 01", p, root, 1.6f, false, petals[rng.Next(petals.Length)]);
                    made++;
                    continue;
                }
                Box(root, p + new Vector2(0, 0.08f), new Vector2(0.04f, 0.16f), new Color(0.3f, 0.6f, 0.25f), o);
                Dot(root, p + new Vector2(0, 0.2f), 0.18f, petals[rng.Next(petals.Length)], o + 1);
                made++;
            }

            // lanterns: 2 a chapter from chapter 2
            int lanterns = Mathf.Min(LanternSpots.Length, Mathf.Max(0, 2 * (ch - 1)));
            for (int i = 0; i < lanterns; i++)
            {
                var p = LanternSpots[i];
                int o = DungeonController.SortOrder(p.y);
                if (Art != null)
                {
                    Prop("Stone Lantern 01", p, root, 1f, true);
                    Glow(root, p + new Vector2(0, 0.85f), new Color(1f, 0.8f, 0.45f), 2f, 0.7f);
                    continue;
                }
                Box(root, p + new Vector2(0, 0.55f), new Vector2(0.08f, 1.1f), Iron, o);
                Box(root, p + new Vector2(0, 1.15f), new Vector2(0.26f, 0.3f), new Color(1f, 0.85f, 0.45f), o + 1);
                Glow(root, p + new Vector2(0, 1.15f), new Color(1f, 0.8f, 0.45f), 2f, 0.7f);
            }

            // bunting: one more run a chapter from chapter 3
            int runs = Mathf.Min(BuntingRuns.Length, Mathf.Max(0, ch - 2));
            Color[] flags = { new Color(0.9f, 0.3f, 0.3f), new Color(0.95f, 0.8f, 0.3f), new Color(0.35f, 0.6f, 0.9f), new Color(0.45f, 0.8f, 0.45f) };
            for (int r = 0; r < runs; r++)
            {
                var b = BuntingRuns[r];
                Vector2 a = new Vector2(b.x, b.y), z = new Vector2(b.z, b.w);
                float len = Vector2.Distance(a, z);
                Box(root, (a + z) / 2f, new Vector2(len, 0.03f), new Color(0.9f, 0.9f, 0.85f), 30000);
                int n = Mathf.FloorToInt(len / 0.6f);
                for (int i = 0; i <= n; i++)
                {
                    var p = Vector2.Lerp(a, z, i / (float)Mathf.Max(1, n)) + new Vector2(0, -0.12f);
                    Box(root, p, new Vector2(0.2f, 0.2f), flags[i % flags.Length], 30001, 45f);
                }
            }
        }

        bool NearSpot(Vector2 p, float d)
        {
            foreach (var s in Spots) if (Vector2.Distance(p, s.pos + new Vector2(0, s.size.y * 0.5f)) < d + Mathf.Max(s.size.x, s.size.y) * 0.5f) return true;
            if (p.x > 27f && p.x < 35f && p.y > 22.5f && p.y < 27f) return true;   // the larder's shelves
            return false;
        }
    }
}
