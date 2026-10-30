// Hearth & Hollow — "Hearth & Hollow > Build Dungeon Kit".
// Creates (only what's missing — your edits are never overwritten):
//   Dungeon/Tiles/Markers   the four marker tiles room prefabs are painted with + a "Dungeon Markers" Tile Palette
//   Dungeon/Themes/<route>  a theme per route with generated placeholder tiles, props and colours
//   Dungeon/Rooms           a starter set of room prefabs (open arenas, pillars, pools, grove, campfire, ladder, exit, boss…)
//   Dungeon/DungeonSettings the asset tying it together (+ default monster behaviour), assigned to the scene's GameBootstrap.
// To regenerate a piece, delete it and run the menu again.
using System;
using System.Collections.Generic;
using System.IO;
using HearthHollow.Dungeon;
using HearthHollow.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using P = HearthHollow.EditorTools.DungeonPixelArt;

namespace HearthHollow.EditorTools
{
    public static class DungeonKitBuilder
    {
        const string Root = "Assets/HearthHollow/Dungeon";
        const string TilesDir = Root + "/Tiles";
        const string MarkersDir = TilesDir + "/Markers";
        const string ThemesDir = Root + "/Themes";
        const string RoomsDir = Root + "/Rooms";
        const string SettingsPath = Root + "/DungeonSettings.asset";

        class Markers { public Tile floor, wall, face, hazard; }

        [MenuItem("Hearth & Hollow/Build Dungeon Kit", priority = 3)]
        public static void BuildMenu()
        {
            var s = Build();
            if (s != null) { Selection.activeObject = s; EditorGUIUtility.PingObject(s); }
        }

        public static DungeonSettings Build()
        {
            EnsureFolder(Root); EnsureFolder(TilesDir); EnsureFolder(MarkersDir); EnsureFolder(ThemesDir); EnsureFolder(RoomsDir);
            var m = BuildMarkers();

            var settings = AssetDatabase.LoadAssetAtPath<DungeonSettings>(SettingsPath);
            if (settings == null) { settings = ScriptableObject.CreateInstance<DungeonSettings>(); AssetDatabase.CreateAsset(settings, SettingsPath); }
            settings.markerFloor = m.floor; settings.markerWall = m.wall; settings.markerWallFace = m.face; settings.markerHazard = m.hazard;

            foreach (var def in ThemeDefs())
            {
                var t = BuildTheme(def);
                if (t != null && !settings.themes.Contains(t)) settings.themes.Add(t);
            }
            settings.themes.RemoveAll(x => x == null);
            if (settings.fallbackTheme == null && settings.themes.Count > 0) settings.fallbackTheme = settings.themes[0];

            foreach (var r in BuildRooms(m)) if (r != null && !settings.rooms.Contains(r)) settings.rooms.Add(r);
            settings.rooms.RemoveAll(x => x == null);

            foreach (var e in DefaultEnemies())
                if (settings.enemies.Find(x => x != null && x.monsterId == e.monsterId) == null) settings.enemies.Add(e);

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            BuildPalette(m);
            ArtLibraryBuilder.Build();     // adds the 4-direction Move/Idle/Attack sheets to the Art Library
            WireScene(settings);
            Debug.Log("[Hearth & Hollow] Dungeon kit ready: " + settings.themes.Count + " themes, " + settings.rooms.Count + " rooms, " + settings.enemies.Count + " monster profiles.");
            return settings;
        }

        // ───────────────────────── files ─────────────────────────
        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static Sprite SaveSprite(string path, Color32[] px, int w, int h, Vector2 pivot)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px); tex.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), path), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 32;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            var ts = new TextureImporterSettings();
            ti.ReadTextureSettings(ts);
            ts.spriteAlignment = (int)SpriteAlignment.Custom;
            ts.spritePivot = pivot;
            ts.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(ts);
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Tile SaveTile(string path, Sprite sprite, Tile.ColliderType collider)
        {
            var t = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (t != null) return t;
            t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = sprite; t.colliderType = collider; t.color = Color.white;
            AssetDatabase.CreateAsset(t, path);
            return t;
        }

        static Tile TileFrom(string dir, string name, Color32[] px, Tile.ColliderType collider)
        {
            var sp = SaveSprite(dir + "/" + name + ".png", px, P.S, P.S, new Vector2(0.5f, 0.5f));
            return SaveTile(dir + "/" + name + ".asset", sp, collider);
        }

        // ───────────────────────── markers ─────────────────────────
        static Markers BuildMarkers()
        {
            return new Markers
            {
                floor = TileFrom(MarkersDir, "Marker Floor", P.Marker(P.C(150, 150, 150), P.C(140, 140, 140), 0), Tile.ColliderType.None),
                wall = TileFrom(MarkersDir, "Marker Wall", P.Marker(P.C(70, 70, 78), P.C(50, 50, 56), 1), Tile.ColliderType.Grid),
                face = TileFrom(MarkersDir, "Marker Wall Face", P.Marker(P.C(110, 104, 96), P.C(80, 76, 70), 3), Tile.ColliderType.Grid),
                hazard = TileFrom(MarkersDir, "Marker Hazard", P.Marker(P.C(60, 110, 170), P.C(110, 170, 230), 2), Tile.ColliderType.Grid),
            };
        }

        /// A Tile Palette with the four markers, for painting rooms (Window > 2D > Tile Palette).
        static void BuildPalette(Markers m)
        {
            const string palettePath = TilesDir + "/Dungeon Markers.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(palettePath) != null) return;
            try
            {
                var type = Type.GetType("UnityEditor.Tilemaps.GridPaletteUtility, Unity.2D.Tilemap.Editor");
                if (type == null) return;
                System.Reflection.MethodInfo create = null;
                foreach (var mi in type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                    if (mi.Name == "CreateNewPalette" && mi.GetParameters().Length == 6) { create = mi; break; }
                if (create == null) return;
                var pars = create.GetParameters();
                var go = create.Invoke(null, new object[] { TilesDir, "Dungeon Markers", GridLayout.CellLayout.Rectangle,
                    Enum.ToObject(pars[3].ParameterType, 1) /* CellSizing.Manual */, Vector3.one, GridLayout.CellSwizzle.XYZ }) as GameObject;
                string path = go != null ? AssetDatabase.GetAssetPath(go) : palettePath;
                var contents = PrefabUtility.LoadPrefabContents(path);
                var tm = contents.GetComponentInChildren<Tilemap>();
                if (tm != null)
                {
                    tm.SetTile(new Vector3Int(0, 0, 0), m.floor);
                    tm.SetTile(new Vector3Int(1, 0, 0), m.wall);
                    tm.SetTile(new Vector3Int(2, 0, 0), m.face);
                    tm.SetTile(new Vector3Int(3, 0, 0), m.hazard);
                }
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                PrefabUtility.UnloadPrefabContents(contents);
            }
            catch (Exception e) { Debug.LogWarning("[Hearth & Hollow] Couldn't create the marker Tile Palette (" + e.Message + "). Drag the tiles from Dungeon/Tiles/Markers into a new palette instead."); }
        }

        // ───────────────────────── themes ─────────────────────────
        class ThemeDef
        {
            public string id, name;
            public Color32 floor, floorAccent, wall, face, hazard, bg;
            public P.FloorStyle floorStyle, altStyle;
            public float accent, altAccent;
            public Color32 altFloor, altAccentC;
            public P.HazardStyle hazardStyle;
            public P.DecorStyle[] decor; public Color32 decorA, decorB;
            public Color light; public float lightIntensity, playerLight, speed = 1f;
            public Color32 metal, portal, leaf, fruit;
        }

        static IEnumerable<ThemeDef> ThemeDefs()
        {
            yield return new ThemeDef
            {
                id = "meadow", name = "Meadow Trail",
                floor = P.C(96, 146, 64), floorStyle = P.FloorStyle.Speckle, floorAccent = P.C(120, 170, 80), accent = 0.03f,
                altFloor = P.C(92, 140, 60), altStyle = P.FloorStyle.Speckle, altAccentC = P.C(250, 235, 140), altAccent = 0.04f,
                wall = P.C(78, 92, 60), face = P.C(124, 128, 98), hazard = P.C(70, 130, 175), hazardStyle = P.HazardStyle.Water,
                decor = new[] { P.DecorStyle.Tuft, P.DecorStyle.Flower, P.DecorStyle.Pebbles }, decorA = P.C(70, 120, 50), decorB = P.C(250, 250, 255),
                bg = P.C(20, 30, 16), light = new Color(1f, 0.98f, 0.9f), lightIntensity = 1f,
                metal = P.C(110, 104, 92), portal = P.C(120, 220, 255), leaf = P.C(70, 140, 60), fruit = P.C(255, 250, 230),
            };
            yield return new ThemeDef
            {
                id = "berry", name = "Berry Hunt",
                floor = P.C(86, 58, 78), floorStyle = P.FloorStyle.Speckle, floorAccent = P.C(110, 70, 100), accent = 0.03f,
                altFloor = P.C(72, 88, 52), altStyle = P.FloorStyle.Speckle, altAccentC = P.C(120, 60, 120), altAccent = 0.05f,
                wall = P.C(58, 36, 58), face = P.C(96, 60, 90), hazard = P.C(48, 72, 40), hazardStyle = P.HazardStyle.Thorns,
                decor = new[] { P.DecorStyle.Berries, P.DecorStyle.Tuft, P.DecorStyle.Mushroom }, decorA = P.C(60, 100, 50), decorB = P.C(150, 60, 170),
                bg = P.C(26, 14, 26), light = new Color(1f, 0.86f, 0.95f), lightIntensity = 0.95f,
                metal = P.C(100, 80, 100), portal = P.C(220, 140, 255), leaf = P.C(60, 110, 55), fruit = P.C(140, 60, 200), speed = 1.05f,
            };
            yield return new ThemeDef
            {
                id = "salt", name = "Salt Marsh Trail",
                floor = P.C(172, 152, 116), floorStyle = P.FloorStyle.Planks, floorAccent = P.C(200, 190, 160), accent = 0.0f,
                altFloor = P.C(150, 136, 110), altStyle = P.FloorStyle.Planks, altAccentC = P.C(235, 240, 245), altAccent = 0.02f,
                wall = P.C(90, 108, 118), face = P.C(126, 144, 152), hazard = P.C(58, 98, 112), hazardStyle = P.HazardStyle.Water,
                decor = new[] { P.DecorStyle.Shell, P.DecorStyle.Reed, P.DecorStyle.Pebbles }, decorA = P.C(110, 130, 90), decorB = P.C(240, 220, 200),
                bg = P.C(14, 26, 32), light = new Color(0.86f, 0.95f, 1f), lightIntensity = 1f,
                metal = P.C(120, 130, 136), portal = P.C(150, 240, 255), leaf = P.C(90, 140, 120), fruit = P.C(240, 245, 250),
            };
            yield return new ThemeDef
            {
                id = "cavern", name = "Cavern Hunt",
                floor = P.C(62, 68, 86), floorStyle = P.FloorStyle.Cobbles, floorAccent = P.C(80, 220, 210), accent = 0.008f,
                altFloor = P.C(58, 64, 82), altStyle = P.FloorStyle.Cobbles, altAccentC = P.C(110, 250, 230), altAccent = 0.03f,
                wall = P.C(38, 42, 54), face = P.C(66, 74, 92), hazard = P.C(14, 14, 22), hazardStyle = P.HazardStyle.Chasm,
                decor = new[] { P.DecorStyle.Crystal, P.DecorStyle.Mushroom, P.DecorStyle.Pebbles }, decorA = P.C(90, 230, 220), decorB = P.C(200, 255, 250),
                bg = P.C(6, 8, 12), light = new Color(0.55f, 0.62f, 0.85f), lightIntensity = 0.6f, playerLight = 1.4f,
                metal = P.C(90, 100, 120), portal = P.C(90, 255, 220), leaf = P.C(60, 120, 120), fruit = P.C(120, 255, 230), speed = 1.05f,
            };
            yield return new ThemeDef
            {
                id = "chili", name = "Chili Ridge Trail",
                floor = P.C(160, 82, 50), floorStyle = P.FloorStyle.Cracks, floorAccent = P.C(190, 120, 70), accent = 0.02f,
                altFloor = P.C(176, 132, 70), altStyle = P.FloorStyle.Speckle, altAccentC = P.C(220, 180, 90), altAccent = 0.04f,
                wall = P.C(96, 44, 28), face = P.C(140, 70, 44), hazard = P.C(120, 52, 30), hazardStyle = P.HazardStyle.Mud,
                decor = new[] { P.DecorStyle.Tuft, P.DecorStyle.Pebbles, P.DecorStyle.Berries }, decorA = P.C(150, 130, 60), decorB = P.C(220, 40, 30),
                bg = P.C(32, 12, 6), light = new Color(1f, 0.86f, 0.7f), lightIntensity = 1f,
                metal = P.C(120, 90, 70), portal = P.C(255, 200, 120), leaf = P.C(90, 120, 50), fruit = P.C(230, 40, 30), speed = 1.08f,
            };
            yield return new ThemeDef
            {
                id = "ember", name = "Ember Hunt",
                floor = P.C(62, 44, 40), floorStyle = P.FloorStyle.Cracks, floorAccent = P.C(255, 120, 40), accent = 0.012f,
                altFloor = P.C(56, 38, 34), altStyle = P.FloorStyle.Cracks, altAccentC = P.C(255, 170, 60), altAccent = 0.04f,
                wall = P.C(36, 22, 20), face = P.C(74, 42, 34), hazard = P.C(230, 100, 30), hazardStyle = P.HazardStyle.Lava,
                decor = new[] { P.DecorStyle.Ember, P.DecorStyle.Bones, P.DecorStyle.Pebbles }, decorA = P.C(230, 220, 200), decorB = P.C(255, 150, 40),
                bg = P.C(18, 6, 4), light = new Color(1f, 0.62f, 0.46f), lightIntensity = 0.75f, playerLight = 1.0f,
                metal = P.C(110, 70, 60), portal = P.C(255, 150, 60), leaf = P.C(110, 60, 40), fruit = P.C(255, 180, 60), speed = 1.12f,
            };
            // ───── story chapters 7–8 and the side dungeons (contentVersion 4) ─────
            yield return new ThemeDef
            {
                id = "frost", name = "Frostfang Pass",
                floor = P.C(206, 222, 236), floorStyle = P.FloorStyle.Speckle, floorAccent = P.C(240, 248, 255), accent = 0.05f,
                altFloor = P.C(170, 200, 224), altStyle = P.FloorStyle.Cobbles, altAccentC = P.C(220, 240, 255), altAccent = 0.02f,
                wall = P.C(80, 96, 120), face = P.C(120, 140, 166), hazard = P.C(150, 196, 230), hazardStyle = P.HazardStyle.Water,
                decor = new[] { P.DecorStyle.Crystal, P.DecorStyle.Pebbles, P.DecorStyle.Tuft }, decorA = P.C(180, 220, 250), decorB = P.C(250, 252, 255),
                bg = P.C(10, 16, 30), light = new Color(0.86f, 0.92f, 1f), lightIntensity = 0.95f,
                metal = P.C(130, 140, 160), portal = P.C(170, 230, 255), leaf = P.C(90, 120, 110), fruit = P.C(120, 80, 200), speed = 1.08f,
            };
            yield return new ThemeDef
            {
                id = "heart", name = "The Hollow Heart",
                floor = P.C(58, 40, 70), floorStyle = P.FloorStyle.Cracks, floorAccent = P.C(255, 210, 90), accent = 0.015f,
                altFloor = P.C(66, 46, 80), altStyle = P.FloorStyle.Cobbles, altAccentC = P.C(255, 190, 80), altAccent = 0.03f,
                wall = P.C(32, 20, 44), face = P.C(70, 48, 90), hazard = P.C(250, 190, 70), hazardStyle = P.HazardStyle.Lava,
                decor = new[] { P.DecorStyle.Ember, P.DecorStyle.Crystal, P.DecorStyle.Bones }, decorA = P.C(210, 170, 255), decorB = P.C(255, 210, 90),
                bg = P.C(12, 6, 18), light = new Color(0.9f, 0.72f, 1f), lightIntensity = 0.65f, playerLight = 1.2f,
                metal = P.C(110, 90, 120), portal = P.C(255, 220, 110), leaf = P.C(90, 60, 110), fruit = P.C(255, 200, 80), speed = 1.15f,
            };
            yield return new ThemeDef
            {
                id = "firefly", name = "Firefly Glade",
                floor = P.C(44, 82, 52), floorStyle = P.FloorStyle.Speckle, floorAccent = P.C(250, 240, 120), accent = 0.02f,
                altFloor = P.C(52, 92, 58), altStyle = P.FloorStyle.Speckle, altAccentC = P.C(255, 250, 160), altAccent = 0.05f,
                wall = P.C(28, 52, 34), face = P.C(54, 86, 58), hazard = P.C(40, 80, 110), hazardStyle = P.HazardStyle.Water,
                decor = new[] { P.DecorStyle.Flower, P.DecorStyle.Tuft, P.DecorStyle.Mushroom }, decorA = P.C(80, 150, 80), decorB = P.C(255, 240, 120),
                bg = P.C(6, 16, 10), light = new Color(0.75f, 0.95f, 0.8f), lightIntensity = 0.6f, playerLight = 0.9f,
                metal = P.C(90, 110, 90), portal = P.C(255, 240, 140), leaf = P.C(60, 120, 60), fruit = P.C(255, 240, 120), speed = 1.05f,
            };
            yield return new ThemeDef
            {
                id = "spore", name = "Spore Mire",
                floor = P.C(84, 92, 58), floorStyle = P.FloorStyle.Speckle, floorAccent = P.C(180, 120, 200), accent = 0.03f,
                altFloor = P.C(74, 70, 64), altStyle = P.FloorStyle.Cracks, altAccentC = P.C(150, 200, 90), altAccent = 0.03f,
                wall = P.C(52, 46, 40), face = P.C(88, 80, 66), hazard = P.C(70, 96, 50), hazardStyle = P.HazardStyle.Mud,
                decor = new[] { P.DecorStyle.Mushroom, P.DecorStyle.Reed, P.DecorStyle.Bones }, decorA = P.C(170, 110, 190), decorB = P.C(210, 240, 140),
                bg = P.C(14, 18, 8), light = new Color(0.82f, 1f, 0.7f), lightIntensity = 0.85f,
                metal = P.C(100, 96, 80), portal = P.C(190, 255, 140), leaf = P.C(90, 110, 60), fruit = P.C(200, 140, 220), speed = 1.05f,
            };
            yield return new ThemeDef
            {
                id = "watchtower", name = "Old Watchtower",
                floor = P.C(118, 114, 108), floorStyle = P.FloorStyle.Cobbles, floorAccent = P.C(150, 146, 136), accent = 0.01f,
                altFloor = P.C(104, 100, 94), altStyle = P.FloorStyle.Cracks, altAccentC = P.C(90, 120, 70), altAccent = 0.03f,
                wall = P.C(64, 60, 58), face = P.C(110, 104, 98), hazard = P.C(20, 18, 20), hazardStyle = P.HazardStyle.Chasm,
                decor = new[] { P.DecorStyle.Pebbles, P.DecorStyle.Bones, P.DecorStyle.Tuft }, decorA = P.C(90, 120, 70), decorB = P.C(220, 214, 200),
                bg = P.C(16, 16, 18), light = new Color(1f, 0.96f, 0.9f), lightIntensity = 0.9f,
                metal = P.C(120, 116, 110), portal = P.C(255, 220, 150), leaf = P.C(80, 110, 60), fruit = P.C(200, 60, 50), speed = 1.08f,
            };
            yield return new ThemeDef
            {
                id = "canopy", name = "Thornwood Canopy",
                floor = P.C(120, 86, 52), floorStyle = P.FloorStyle.Planks, floorAccent = P.C(150, 110, 70), accent = 0.0f,
                altFloor = P.C(70, 110, 54), altStyle = P.FloorStyle.Speckle, altAccentC = P.C(120, 170, 80), altAccent = 0.05f,
                wall = P.C(48, 76, 40), face = P.C(80, 112, 60), hazard = P.C(150, 200, 230), hazardStyle = P.HazardStyle.Chasm,
                decor = new[] { P.DecorStyle.Berries, P.DecorStyle.Flower, P.DecorStyle.Tuft }, decorA = P.C(70, 130, 60), decorB = P.C(230, 80, 90),
                bg = P.C(18, 28, 14), light = new Color(1f, 0.97f, 0.85f), lightIntensity = 1f,
                metal = P.C(110, 90, 70), portal = P.C(180, 255, 170), leaf = P.C(60, 120, 50), fruit = P.C(230, 80, 90), speed = 1.1f,
            };
            yield return new ThemeDef
            {
                id = "larder", name = "Forgotten Larder",
                floor = P.C(132, 96, 64), floorStyle = P.FloorStyle.Planks, floorAccent = P.C(160, 120, 80), accent = 0.0f,
                altFloor = P.C(150, 140, 120), altStyle = P.FloorStyle.Cobbles, altAccentC = P.C(200, 190, 160), altAccent = 0.02f,
                wall = P.C(70, 48, 34), face = P.C(116, 82, 56), hazard = P.C(110, 80, 50), hazardStyle = P.HazardStyle.Mud,
                decor = new[] { P.DecorStyle.Mushroom, P.DecorStyle.Pebbles, P.DecorStyle.Berries }, decorA = P.C(200, 170, 120), decorB = P.C(220, 90, 60),
                bg = P.C(20, 12, 6), light = new Color(1f, 0.85f, 0.62f), lightIntensity = 0.75f, playerLight = 0.9f,
                metal = P.C(120, 96, 70), portal = P.C(255, 210, 140), leaf = P.C(100, 110, 60), fruit = P.C(240, 200, 120), speed = 1.08f,
            };
            yield return new ThemeDef
            {
                id = "frosthollow", name = "Frost Hollow",
                floor = P.C(120, 156, 190), floorStyle = P.FloorStyle.Cobbles, floorAccent = P.C(200, 240, 255), accent = 0.015f,
                altFloor = P.C(150, 180, 210), altStyle = P.FloorStyle.Speckle, altAccentC = P.C(230, 250, 255), altAccent = 0.04f,
                wall = P.C(44, 60, 86), face = P.C(84, 108, 140), hazard = P.C(10, 14, 24), hazardStyle = P.HazardStyle.Chasm,
                decor = new[] { P.DecorStyle.Crystal, P.DecorStyle.Pebbles, P.DecorStyle.Bones }, decorA = P.C(170, 230, 255), decorB = P.C(240, 250, 255),
                bg = P.C(4, 8, 16), light = new Color(0.62f, 0.78f, 1f), lightIntensity = 0.6f, playerLight = 1.3f,
                metal = P.C(110, 130, 160), portal = P.C(160, 230, 255), leaf = P.C(80, 120, 130), fruit = P.C(120, 80, 200), speed = 1.12f,
            };
            // ───── four more side dungeons (contentVersion 5) ─────
            yield return new ThemeDef
            {
                id = "harerun", name = "Hare's Run",
                floor = P.C(138, 178, 84), floorStyle = P.FloorStyle.Speckle, floorAccent = P.C(176, 210, 110), accent = 0.03f,
                altFloor = P.C(196, 176, 112), altStyle = P.FloorStyle.Speckle, altAccentC = P.C(220, 200, 140), altAccent = 0.03f,
                wall = P.C(84, 120, 60), face = P.C(120, 156, 84), hazard = P.C(70, 120, 170), hazardStyle = P.HazardStyle.Water,
                decor = new[] { P.DecorStyle.Flower, P.DecorStyle.Tuft, P.DecorStyle.Pebbles }, decorA = P.C(110, 160, 70), decorB = P.C(250, 220, 120),
                bg = P.C(20, 34, 16), light = new Color(1f, 0.98f, 0.9f), lightIntensity = 1.05f,
                metal = P.C(130, 120, 100), portal = P.C(200, 255, 170), leaf = P.C(80, 140, 60), fruit = P.C(240, 120, 90), speed = 1f,
            };
            yield return new ThemeDef
            {
                id = "glowcap", name = "Glowcap Grotto",
                floor = P.C(46, 50, 70), floorStyle = P.FloorStyle.Cobbles, floorAccent = P.C(90, 220, 200), accent = 0.02f,
                altFloor = P.C(40, 56, 60), altStyle = P.FloorStyle.Cracks, altAccentC = P.C(110, 255, 210), altAccent = 0.03f,
                wall = P.C(20, 22, 34), face = P.C(44, 48, 70), hazard = P.C(8, 10, 16), hazardStyle = P.HazardStyle.Chasm,
                decor = new[] { P.DecorStyle.Mushroom, P.DecorStyle.Crystal, P.DecorStyle.Pebbles }, decorA = P.C(90, 230, 200), decorB = P.C(170, 255, 230),
                bg = P.C(2, 4, 8), light = new Color(0.6f, 0.9f, 0.95f), lightIntensity = 0.7f, playerLight = 1.1f,
                metal = P.C(90, 100, 120), portal = P.C(120, 255, 220), leaf = P.C(60, 110, 100), fruit = P.C(110, 255, 210), speed = 1.05f,
            };
            yield return new ThemeDef
            {
                id = "thunder", name = "Thunderpuff Hills",
                floor = P.C(96, 118, 96), floorStyle = P.FloorStyle.Speckle, floorAccent = P.C(200, 210, 255), accent = 0.015f,
                altFloor = P.C(110, 106, 120), altStyle = P.FloorStyle.Cracks, altAccentC = P.C(230, 230, 120), altAccent = 0.03f,
                wall = P.C(58, 62, 80), face = P.C(92, 98, 120), hazard = P.C(60, 80, 120), hazardStyle = P.HazardStyle.Water,
                decor = new[] { P.DecorStyle.Tuft, P.DecorStyle.Pebbles, P.DecorStyle.Crystal }, decorA = P.C(130, 150, 120), decorB = P.C(255, 240, 120),
                bg = P.C(10, 12, 22), light = new Color(0.8f, 0.85f, 1f), lightIntensity = 0.85f,
                metal = P.C(110, 116, 140), portal = P.C(255, 245, 140), leaf = P.C(80, 110, 80), fruit = P.C(255, 230, 90), speed = 1.08f,
            };
            yield return new ThemeDef
            {
                id = "hive", name = "Wild Hive",
                floor = P.C(196, 146, 52), floorStyle = P.FloorStyle.Cobbles, floorAccent = P.C(240, 196, 80), accent = 0.02f,
                altFloor = P.C(170, 120, 40), altStyle = P.FloorStyle.Speckle, altAccentC = P.C(255, 214, 110), altAccent = 0.05f,
                wall = P.C(110, 70, 20), face = P.C(160, 110, 40), hazard = P.C(240, 170, 40), hazardStyle = P.HazardStyle.Mud,
                decor = new[] { P.DecorStyle.Flower, P.DecorStyle.Ember, P.DecorStyle.Pebbles }, decorA = P.C(255, 200, 80), decorB = P.C(120, 70, 20),
                bg = P.C(30, 18, 4), light = new Color(1f, 0.9f, 0.62f), lightIntensity = 0.95f,
                metal = P.C(140, 100, 50), portal = P.C(255, 220, 90), leaf = P.C(120, 120, 40), fruit = P.C(255, 190, 40), speed = 1.05f,
            };
        }

        static DungeonTheme BuildTheme(ThemeDef d)
        {
            string dir = ThemesDir + "/" + d.id;
            string path = dir + "/Theme " + d.name + ".asset";
            var theme = AssetDatabase.LoadAssetAtPath<DungeonTheme>(path);
            if (theme != null) return theme;
            EnsureFolder(dir);
            int seed = d.id.GetHashCode() & 0xFFFF;

            theme = ScriptableObject.CreateInstance<DungeonTheme>();
            theme.routeId = d.id; theme.displayName = d.name;
            theme.floor = TileFrom(dir, "Floor", P.Floor(d.floor, d.floorStyle, seed, d.floorAccent, d.accent), Tile.ColliderType.None);
            theme.floorAlt = TileFrom(dir, "Floor Alt", P.Floor(d.altFloor, d.altStyle, seed + 1, d.altAccentC, d.altAccent), Tile.ColliderType.None);
            theme.wall = TileFrom(dir, "Wall", P.WallTop(d.wall, seed + 2), Tile.ColliderType.Grid);
            theme.wallFace = TileFrom(dir, "Wall Face", P.WallFace(d.face, seed + 3), Tile.ColliderType.Grid);
            theme.hazard = TileFrom(dir, "Hazard", P.Hazard(d.hazard, d.hazardStyle, seed + 4), Tile.ColliderType.Grid);
            for (int i = 0; i < d.decor.Length; i++)
                theme.decor.Add(TileFrom(dir, "Decor " + (i + 1) + " " + d.decor[i], P.Decor(d.decor[i], d.decorA, d.decorB, seed + 10 + i), Tile.ColliderType.None));
            theme.decorDensity = 0.05f;

            int w, h;
            var bars = P.DoorBars(d.metal, out w, out h);
            theme.doorClosed = SaveSprite(dir + "/Door Bars.png", bars, w, h, new Vector2(0.5f, 0.5f));
            theme.ladder = SaveSprite(dir + "/Ladder.png", P.Ladder(d.bg, P.C(150, 104, 60)), P.S, P.S, new Vector2(0.5f, 0.3f));
            theme.exitPortal = SaveSprite(dir + "/Exit.png", P.Portal(d.portal), P.S, P.S, new Vector2(0.5f, 0.5f));
            theme.campfire = SaveSprite(dir + "/Campfire.png", P.Campfire(), P.S, P.S, new Vector2(0.5f, 0.25f));
            theme.forage = SaveSprite(dir + "/Forage.png", P.Bush(d.leaf, d.fruit, seed + 20), P.S, P.S, new Vector2(0.5f, 0.25f));

            theme.background = d.bg;
            theme.lightColor = d.light; theme.lightIntensity = d.lightIntensity; theme.playerLight = d.playerLight;
            theme.enemySpeedMult = d.speed;
            AssetDatabase.CreateAsset(theme, path);
            return theme;
        }

        // ───────────────────────── rooms ─────────────────────────
        // Interior layouts: 11 rows × 21 columns, top row first.  Legend:
        //   .  floor   #  wall   o  pillar (wall)   ~  hazard
        //   E  enemy   X  elite / boss   f  forage   c  campfire   L  ladder   P  exit   S  player start
        static readonly string[] Blank =
        {
            ".....................", ".....................", ".....................", ".....................",
            ".....................", ".....................", ".....................", ".....................",
            ".....................", ".....................", ".....................",
        };

        static IEnumerable<RoomTemplate> BuildRooms(Markers m)
        {
            yield return Room(m, "Room_Blank (copy me)", 0, new[] { RoomKind.Combat }, Blank);
            yield return Room(m, "Start_Camp", 1, new[] { RoomKind.Start }, new[]
            {
                ".....................", ".....................", ".....................", ".....................",
                ".....................", "..........S..........", ".....................", ".....................",
                ".....................", ".....................", ".....................",
            });
            yield return Room(m, "Arena_Open", 2, new[] { RoomKind.Combat }, new[]
            {
                ".....................", "...E.............E...", ".....................", ".....................",
                ".....................", ".......E.....E.......", ".....................", ".....................",
                ".....................", "...E.............E...", ".....................",
            });
            yield return Room(m, "Arena_Pillars", 2, new[] { RoomKind.Combat }, new[]
            {
                ".....................", "..E...............E..", "....oo.........oo....", "....oo.........oo....",
                ".....................", ".........E.E.........", ".....................", "....oo.........oo....",
                "....oo.........oo....", "..E...............E..", ".....................",
            });
            yield return Room(m, "Arena_Cross", 2, new[] { RoomKind.Combat }, new[]
            {
                ".....................", "..E...............E..", ".....E.........E.....", "..........#..........",
                "..........#..........", "......#########......", "..........#..........", "..........#..........",
                ".....................", "..E......X........E..", ".....................",
            });
            yield return Room(m, "Arena_Pools", 2, new[] { RoomKind.Combat }, new[]
            {
                ".....................", ".~~~.............~~~.", ".~~~~.....E.....~~~~.", "...~.............~...",
                ".....................", "....E...........E....", ".....................", "...~.............~...",
                "..~~.............~~..", ".~~~~.....E.....~~~~.", ".~~~.............~~~.",
            });
            yield return Room(m, "Arena_Ring", 2, new[] { RoomKind.Combat }, new[]
            {
                ".....................", "...E.............E...", ".....................", ".......~~~~~~~.......",
                "......~~~~~~~~~......", "......~~~~~~~~~......", "......~~~~~~~~~......", ".......~~~~~~~.......",
                ".....................", "...E.......X.....E...", ".....................",
            });
            yield return Room(m, "Arena_Columns", 2, new[] { RoomKind.Combat }, new[]
            {
                ".....................", ".....E.........E.....", "...o...o.....o...o...", ".....................",
                ".....................", "........E...E........", ".....................", ".....................",
                "...o...o.....o...o...", ".....E.........E.....", ".....................",
            });
            yield return Room(m, "Arena_Halls", 2, new[] { RoomKind.Combat }, new[]
            {
                ".....................", "..E...............E..", ".....................", "...#####.....#####...",
                ".....................", ".........E.E.........", ".....................", "...#####.....#####...",
                ".....................", "..E...............E..", ".....................",
            });
            yield return Room(m, "Grove_Forage", 1, new[] { RoomKind.Gather }, new[]
            {
                ".....................", "..o...............o..", "....f..........f.....", ".......o.....o.......",
                ".....................", ".....................", ".....................", ".......o.....o.......",
                "....f..........f.....", "..o...............o..", ".....................",
            });
            yield return Room(m, "Rest_Campfire", 1, new[] { RoomKind.Rest }, new[]
            {
                ".....................", ".....................", "....o...........o....", ".....................",
                ".....................", "..........c..........", ".....................", ".....................",
                "....o...........o....", ".....................", ".....................",
            });
            yield return Room(m, "Ladder_Hall", 1, new[] { RoomKind.Ladder }, new[]
            {
                ".....................", "..E...............E..", ".....................", "..........L..........",
                "......o.......o......", ".....................", "....E...........E....", "......o.......o......",
                ".....................", "..E.......X.......E..", ".....................",
            });
            yield return Room(m, "Exit_Gate", 1, new[] { RoomKind.Exit }, new[]
            {
                ".....................", "...E.............E...", ".....o.........o.....", ".....................",
                ".....................", "..........P..........", ".....................", ".....................",
                ".....o.........o.....", "...E......X......E...", ".....................",
            });
            yield return Room(m, "Boss_Arena", 1, new[] { RoomKind.Boss }, new[]
            {
                ".....................", ".....................", "..oo.............oo..", "..oo......X......oo..",
                ".....................", "..........P..........", ".....................", "..oo.............oo..",
                "..oo...E.....E...oo..", ".....................", ".....................",
            });
        }

        static RoomTemplate Room(Markers m, string name, int weight, RoomKind[] kinds, string[] layout)
        {
            string path = RoomsDir + "/" + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<RoomTemplate>();

            int W = RoomTemplate.Width, H = RoomTemplate.Height;
            var root = new GameObject(name);
            var tpl = root.AddComponent<RoomTemplate>();
            tpl.usableAs = new List<RoomKind>(kinds);
            tpl.weight = weight;
            var gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(root.transform, false);
            gridGo.AddComponent<Grid>();
            tpl.floor = MakeTilemap(gridGo, "Floor", 0, false);
            tpl.decor = MakeTilemap(gridGo, "Decor", 1, false);
            tpl.walls = MakeTilemap(gridGo, "Walls", 2, true);

            // shell: floor everywhere, walls round the edge (north wall = face row + top row)
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    var c = new Vector3Int(x, y, 0);
                    tpl.floor.SetTile(c, m.floor);
                    bool edge = x == 0 || x == W - 1 || y == 0 || y >= H - 2;
                    if (edge) tpl.walls.SetTile(c, (y == H - 2 && x > 0 && x < W - 1) ? m.face : m.wall);
                }
            foreach (Side s in Enum.GetValues(typeof(Side)))
                foreach (var c in RoomTemplate.DoorCells(s)) tpl.walls.SetTile(c, null);

            // interior
            for (int row = 0; row < layout.Length && row < H - 3; row++)
            {
                string line = layout[row];
                int y = H - 3 - row;
                for (int col = 0; col < line.Length && col < W - 2; col++)
                {
                    int x = 1 + col;
                    var c = new Vector3Int(x, y, 0);
                    char ch = line[col];
                    if ((ch == '#' || ch == 'o' || ch == '~') && KeepClear(x, y)) ch = '.';
                    switch (ch)
                    {
                        case '#': case 'o': tpl.walls.SetTile(c, m.wall); break;
                        case '~': tpl.walls.SetTile(c, m.hazard); break;
                        case 'E': Spawn(root, SpawnKind.Enemy, x, y); break;
                        case 'X': Spawn(root, SpawnKind.Elite, x, y); break;
                        case 'f': Spawn(root, SpawnKind.Forage, x, y); break;
                        case 'c': Spawn(root, SpawnKind.Campfire, x, y); break;
                        case 'L': Spawn(root, SpawnKind.Ladder, x, y); break;
                        case 'P': Spawn(root, SpawnKind.Exit, x, y); break;
                        case 'S': Spawn(root, SpawnKind.PlayerStart, x, y); break;
                    }
                }
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab != null ? prefab.GetComponent<RoomTemplate>() : null;
        }

        /// Cells right inside each doorway stay walkable whatever the layout says.
        static bool KeepClear(int x, int y)
        {
            int cx = RoomTemplate.Width / 2, cy = (RoomTemplate.Height - 3) / 2 + 1;
            if (Mathf.Abs(x - cx) <= 2 && (y <= 2 || y >= RoomTemplate.Height - 4)) return true;
            if (Mathf.Abs(y - cy) <= 2 && (x <= 2 || x >= RoomTemplate.Width - 3)) return true;
            return false;
        }

        static Tilemap MakeTilemap(GameObject grid, string name, int order, bool collider)
        {
            var go = new GameObject(name);
            go.transform.SetParent(grid.transform, false);
            var tm = go.AddComponent<Tilemap>();
            var r = go.AddComponent<TilemapRenderer>();
            r.sortingOrder = order;
            if (collider) go.AddComponent<TilemapCollider2D>();
            return tm;
        }

        static void Spawn(GameObject root, SpawnKind kind, int x, int y)
        {
            var go = new GameObject("Spawn · " + kind);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = new Vector3(x + 0.5f, y + 0.3f, 0);
            go.AddComponent<SpawnPoint>().kind = kind;
        }

        // ───────────────────────── monsters ─────────────────────────
        static IEnumerable<EnemyProfile> DefaultEnemies()
        {
            yield return new EnemyProfile { monsterId = "slime", archetype = EnemyArchetype.Chaser, moveSpeed = 1.8f, attackRange = 0.9f, windup = 0.45f, cooldownMult = 1.1f };
            yield return new EnemyProfile { monsterId = "boar", archetype = EnemyArchetype.Charger, moveSpeed = 2.2f, attackRange = 1.0f, windup = 0.6f, cooldownMult = 1.3f };
            yield return new EnemyProfile { monsterId = "bat", archetype = EnemyArchetype.Flyer, moveSpeed = 3.2f, attackRange = 2.4f, windup = 0.3f, cooldownMult = 0.9f, scale = 1.5f };
            yield return new EnemyProfile { monsterId = "crab", archetype = EnemyArchetype.Chaser, moveSpeed = 2.9f, attackRange = 1.0f, windup = 0.3f, cooldownMult = 1.0f };
            yield return new EnemyProfile { monsterId = "wolf", archetype = EnemyArchetype.Chaser, moveSpeed = 3.3f, attackRange = 1.0f, windup = 0.35f, cooldownMult = 1.3f, hits = 2 };
            yield return new EnemyProfile { monsterId = "shaman", archetype = EnemyArchetype.Caster, moveSpeed = 2.0f, attackRange = 6f, windup = 0.6f, cooldownMult = 1.5f, projectileSpeed = 6f, healsAllies = true };
            yield return new EnemyProfile { monsterId = "dragon", archetype = EnemyArchetype.Brute, moveSpeed = 1.7f, attackRange = 1.4f, windup = 0.7f, cooldownMult = 1.2f, projectileSpeed = 7f, scale = 1.2f };
        }

        // ───────────────────────── scene ─────────────────────────
        static void WireScene(DungeonSettings settings)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var boot = UnityEngine.Object.FindAnyObjectByType<GameBootstrap>();
            if (boot == null) { Debug.LogWarning("[Hearth & Hollow] Open the game scene and run Build Dungeon Kit again to assign Dungeon Settings."); return; }
            var so = new SerializedObject(boot);
            var prop = so.FindProperty("dungeon");
            if (prop != null && prop.objectReferenceValue != settings)
            {
                prop.objectReferenceValue = settings;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(boot.gameObject.scene);
                EditorSceneManager.SaveScene(boot.gameObject.scene);
            }
        }
    }
}
