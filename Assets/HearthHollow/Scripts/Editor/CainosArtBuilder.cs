// Hearth & Hollow — "Hearth & Hollow > Apply Cainos Art".
// Swaps the placeholder art for the Cainos "Pixel Art Top Down - Basic" pack (Assets/Cainos, free, by Cainos — credit appreciated):
//   • makes Art Tiles from the pack's grass / stone / wall tilesets and plants (Dungeon/Tiles/Cainos),
//   • points every dungeon theme at them (grass routes vs stone routes, each with its own tint; hazards stay),
//   • builds the TownArt asset (tiles + every Cainos prefab) and hooks it into Dungeon Settings,
//   • switches the pack's materials to URP 2D's Sprite-Lit-Default so they're lit like the rest of the game.
// NB: the pack's optional URP patch also replaces Project Settings > Graphics' render pipeline — this project needs
//     Assets/Settings/UniversalRP.asset there (the game draws nothing lit without it).
// Safe to run again: it overwrites only what it made.
using System.Collections.Generic;
using System.IO;
using HearthHollow.Dungeon;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace HearthHollow.EditorTools
{
    public static class CainosArtBuilder
    {
        const string Pack = "Assets/Cainos/Pixel Art Top Down - Basic";
        const string Out = "Assets/HearthHollow/Dungeon/Tiles/Cainos";
        const string SettingsPath = "Assets/HearthHollow/Dungeon/DungeonSettings.asset";
        const string TownArtPath = "Assets/HearthHollow/Dungeon/TownArt.asset";

        static readonly string[] GrassRoutes = { "meadow", "berry", "salt", "chili", "firefly", "spore", "canopy", "harerun", "thunder", "hive" };

        // floor/wall tint per route (multiplies the pack's colours, keeps each route recognisable)
        static readonly Dictionary<string, Color> Tints = new Dictionary<string, Color>
        {
            { "meadow", new Color(1f, 1f, 1f) },          { "berry", new Color(1f, 0.86f, 0.95f) },
            { "salt", new Color(0.86f, 0.96f, 1f) },       { "chili", new Color(1f, 0.86f, 0.7f) },
            { "firefly", new Color(0.78f, 0.94f, 0.84f) }, { "spore", new Color(0.9f, 1f, 0.74f) },
            { "canopy", new Color(0.84f, 0.95f, 0.78f) },  { "harerun", new Color(1f, 1f, 0.9f) },
            { "thunder", new Color(0.84f, 0.87f, 1f) },    { "hive", new Color(1f, 0.92f, 0.68f) },
            { "cavern", new Color(0.78f, 0.84f, 0.95f) },  { "ember", new Color(1f, 0.74f, 0.64f) },
            { "frost", new Color(0.88f, 0.95f, 1f) },      { "heart", new Color(0.9f, 0.78f, 1f) },
            { "watchtower", new Color(1f, 0.97f, 0.92f) }, { "larder", new Color(1f, 0.9f, 0.76f) },
            { "frosthollow", new Color(0.8f, 0.9f, 1f) },  { "glowcap", new Color(0.74f, 0.95f, 1f) },
        };

        [MenuItem("Hearth & Hollow/Apply Cainos Art", priority = 4)]
        public static void Apply()
        {
            if (!AssetDatabase.IsValidFolder(Pack)) { EditorUtility.DisplayDialog("Cainos art", "The Cainos pack isn't at " + Pack + ". Import it first.", "OK"); return; }
            FixMaterials();
            if (!AssetDatabase.IsValidFolder(Out)) Directory.CreateDirectory(Out);

            var grassSpr = Range("TX Tileset Grass", "TX Tileset Grass ", 0, 16);
            var flowerSpr = Range("TX Tileset Grass", "TX Tileset Grass Flower ", 0, 16);
            var paveSpr = Range("TX Tileset Grass", "TX Tileset Grass Pavement ", 0, 8);
            var slabSpr = Names("TX Tileset Stone Ground", "TX Tileset Stone Ground_", 0, 1, 2, 8, 9, 10, 16, 17, 18);
            var smallSlab = Names("TX Tileset Stone Ground", "TX Tileset Stone Ground_", 27);
            var faceSpr = Names("TX Tileset Wall", "TX Tileset Wall_", 28, 29, 30, 31);
            var tuftSpr = Range("TX Plant", "TX Plant - Grass ", 1, 16, "00");
            var flowerDecor = Names2("TX Plant", "TX Plant - Flower 01");
            var pebbleSpr = Names2("TX Props", "TX Props - Stone 01", "TX Props - Stone 02", "TX Props - Stone 03", "TX Props - Stone 04");
            var brickSpr = Names2("TX Props", "TX Props - Brick 01", "TX Props - Brick 02", "TX Props - Brick 03");

            var grass = MakeTile("Grass", grassSpr, 0, 0, Color.white, Tile.ColliderType.None, 1);
            var flowers = MakeTile("Grass Flowers", flowerSpr, 0, 0, Color.white, Tile.ColliderType.None, 2);
            var path = MakeTile("Path Cobbles", paveSpr, 0, 0, Color.white, Tile.ColliderType.None, 3);
            if (path.sprites.Length == 8) { path.weights = new[] { 6f, 6f, 2f, 2f, 1f, 1f, 0.5f, 0.5f }; EditorUtility.SetDirty(path); }   // mostly whole cobbles
            var slabs = MakeTile("Stone Slabs", slabSpr, 3, 3, Color.white, Tile.ColliderType.None, 4);
            var tiles = MakeTile("Stone Tiles", smallSlab, 0, 0, Color.white, Tile.ColliderType.None, 5);
            var topGrass = MakeTile("Wall Top Grass", grassSpr, 0, 0, new Color(0.52f, 0.58f, 0.46f), Tile.ColliderType.Grid, 6);
            var topStone = MakeTile("Wall Top Stone", slabSpr, 3, 3, new Color(0.3f, 0.3f, 0.33f), Tile.ColliderType.Grid, 7);
            var face = MakeTile("Wall Face", faceSpr, 0, 0, Color.white, Tile.ColliderType.Grid, 8);
            var hedge = MakeTile("Wall Hedge", Names2("TX Plant", "TX Bush T3", "TX Bush T4", "TX Bush T5", "TX Bush T6"), 0, 0, new Color(0.82f, 0.86f, 0.8f), Tile.ColliderType.Grid, 13);
            hedge.offset = new Vector2(0f, -0.5f); EditorUtility.SetDirty(hedge);
            var rubble = MakeTile("Wall Rubble", Names2("TX Props", "TX Props Stone T7", "TX Props - Stone 06", "TX Props - Stone 05", "TX Props Stone T7"), 0, 0, new Color(0.62f, 0.62f, 0.66f), Tile.ColliderType.Grid, 14);
            rubble.offset = new Vector2(0f, -0.5f); EditorUtility.SetDirty(rubble);
            var tufts = MakeTile("Decor Tufts", tuftSpr, 0, 0, Color.white, Tile.ColliderType.None, 9);
            var flower = MakeTile("Decor Flower", flowerDecor, 0, 0, Color.white, Tile.ColliderType.None, 10);
            var pebbles = MakeTile("Decor Pebbles", pebbleSpr, 0, 0, Color.white, Tile.ColliderType.None, 11);
            var bricks = MakeTile("Decor Bricks", brickSpr, 0, 0, Color.white, Tile.ColliderType.None, 12);

            // ── dungeon themes ──
            var settings = AssetDatabase.LoadAssetAtPath<DungeonSettings>(SettingsPath);
            int n = 0;
            if (settings != null)
                foreach (var t in settings.themes)
                {
                    if (t == null) continue;
                    bool outdoors = System.Array.IndexOf(GrassRoutes, t.routeId) >= 0;
                    Color tint; if (!Tints.TryGetValue(t.routeId, out tint)) tint = Color.white;
                    if (outdoors)
                    {
                        t.floor = grass; t.floorAlt = flowers; t.floorAltChance = 0.15f;
                        t.wall = hedge;
                        t.decor = new List<TileBase> { tufts, tufts, flower, pebbles }; t.decorDensity = 0.07f;
                    }
                    else
                    {
                        bool built = t.routeId == "watchtower" || t.routeId == "larder";
                        t.floor = built ? tiles : slabs; t.floorAlt = null; t.floorAltChance = 0f;
                        t.wall = built ? (TileBase)topStone : rubble;   // caves: piled rocks; the watchtower and the larder: built walls
                        t.decor = new List<TileBase> { pebbles, pebbles, bricks }; t.decorDensity = 0.04f;
                    }
                    t.wallFace = face;
                    t.floorTint = tint; t.wallTint = tint;
                    EditorUtility.SetDirty(t); n++;
                }

            // ── town ──
            var art = AssetDatabase.LoadAssetAtPath<TownArt>(TownArtPath);
            if (art == null) { art = ScriptableObject.CreateInstance<TownArt>(); AssetDatabase.CreateAsset(art, TownArtPath); }
            art.grass = grass; art.grassFlowers = flowers; art.path = path; art.square = slabs;
            art.wallTop = topGrass; art.wallFace = face;
            art.decor = new List<TileBase> { tufts, tufts, tufts, flower, pebbles };
            art.props = new List<GameObject>();
            foreach (var g in AssetDatabase.FindAssets("t:Prefab", new[] { Pack + "/Prefab" }))
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g));
                if (p != null && !p.name.Contains("Player")) art.props.Add(p);
            }
            EditorUtility.SetDirty(art);
            if (settings != null) { settings.townArt = art; EditorUtility.SetDirty(settings); }
            AssetDatabase.SaveAssets();
            Debug.Log("[Hearth & Hollow] Cainos art applied: " + n + " dungeon themes, town art with " + art.props.Count + " props.");
        }

        static void FixMaterials()
        {
            // URP 2D's own sprite shaders, so the pack is lit by the Hearth, lanterns and weather like everything else
            var lit = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            var unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { Pack }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
                if (m == null || lit == null || unlit == null) continue;
                m.shader = m.name.Contains("Glow") ? unlit : lit;
                m.color = m.name.Contains("Shadow") ? new Color(1f, 1f, 1f, 0.6f) : Color.white;
                EditorUtility.SetDirty(m);
            }
        }

        static ArtTile MakeTile(string name, Sprite[] sprites, int pw, int ph, Color c, Tile.ColliderType col, int seed)
        {
            string path = Out + "/" + name + ".asset";
            var t = AssetDatabase.LoadAssetAtPath<ArtTile>(path);
            if (t == null) { t = ScriptableObject.CreateInstance<ArtTile>(); AssetDatabase.CreateAsset(t, path); }
            t.sprites = sprites; t.weights = new float[0]; t.offset = Vector2.zero; t.patternWidth = pw; t.patternHeight = ph; t.color = c; t.collider = col; t.seed = seed;
            EditorUtility.SetDirty(t);
            return t;
        }

        static Dictionary<string, Sprite> SpritesOf(string texture)
        {
            var d = new Dictionary<string, Sprite>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Pack + "/Texture/" + texture + ".png")) { var s = o as Sprite; if (s != null) d[s.name] = s; }
            return d;
        }
        static Sprite[] Range(string tex, string prefix, int from, int to, string fmt = null)
        {
            var d = SpritesOf(tex); var l = new List<Sprite>(); Sprite s;
            for (int i = from; i < to; i++) if (d.TryGetValue(prefix + (fmt != null ? i.ToString(fmt) : i.ToString()), out s)) l.Add(s);
            if (l.Count == 0) Debug.LogWarning("[Hearth & Hollow] No sprites found for " + prefix);
            return l.ToArray();
        }
        static Sprite[] Names(string tex, string prefix, params int[] ids)
        {
            var d = SpritesOf(tex); var l = new List<Sprite>(); Sprite s;
            foreach (var i in ids) if (d.TryGetValue(prefix + i, out s)) l.Add(s); else Debug.LogWarning("[Hearth & Hollow] Missing sprite " + prefix + i);
            return l.ToArray();
        }
        static Sprite[] Names2(string tex, params string[] names)
        {
            var d = SpritesOf(tex); var l = new List<Sprite>(); Sprite s;
            foreach (var n in names) if (d.TryGetValue(n, out s)) l.Add(s); else Debug.LogWarning("[Hearth & Hollow] Missing sprite " + n);
            return l.ToArray();
        }
    }
}
