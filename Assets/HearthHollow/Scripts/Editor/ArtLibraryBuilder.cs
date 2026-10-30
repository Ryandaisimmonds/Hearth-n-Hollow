// Hearth & Hollow — "Hearth & Hollow/Build Art Library": fills Assets/HearthHollow/Art/ArtLibrary.asset
// from the installed packs (Layer Lab "GUI-TheStone" + "100 Top Down Monsters - Pixel Art - Vol 1").
// Re-run any time; it only fills slots it can find and keeps any sprite you've set by hand for ids it
// doesn't map. Monster/class frames are auto-cropped to their visible pixels.
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using HearthHollow.Data;
using UnityEditor;
using UnityEngine;

namespace HearthHollow.EditorTools
{
    public static class ArtLibraryBuilder
    {
        const string LibPath = "Assets/HearthHollow/Art/ArtLibrary.asset";
        const string LL = "Assets/HearthHollow/Art/ThirdParty/GUI-TheStone/";   // moved from Assets/Layer Lab (Oct 2026)
        const string ITEM = LL + "Components/Icon_ItemIcons/ItemIcon_128/Icon_ItemIcon_";
        const string DEMO = LL + "Demo/Demo_Icon/";
        const string ETC = LL + "Components/UI_Etc/";
        const string ACT = LL + "Components/ActionText/";
        const string MON = "Assets/HearthHollow/Art/ThirdParty/Top Down Monsters Vol 1/";   // moved from the pack folder

        // id → sprite path. Edit freely; ids are what the UI asks for.
        static readonly string[,] IconMap =
        {
            { "coin", ETC + "ResourceBar_Icon_Coin.png" },
            { "hp", ITEM + "Potion_Red.png" },
            { "damage", ITEM + "Sword_A.png" },
            { "armour", ITEM + "Shield_A.png" },
            { "crit", ITEM + "Target.png" },
            { "gather", ITEM + "Clover.png" },
            { "coinpct", ITEM + "Gold.png" },
            { "xp", DEMO + "Icon_ImageIcon_Star01_On.png" },
            { "day", ITEM + "Sandglass.png" },
            { "lock", ITEM + "Lock.png" },
            { "elite", ITEM + "Crown.png" },
            { "skull", ITEM + "Skull.png" },
            { "trophy", ITEM + "Trophy.png" },
            { "chest", DEMO + "Icon_MenuIcon_Chest.png" },
            { "bag", ITEM + "Treasure.png" },
            { "map", ITEM + "Scroll.png" },
            { "cook", ITEM + "Chicken.png" },
            { "pantry", ITEM + "Gift.png" },
            // camp events
            { "event_merchant", ITEM + "Shop.png" },
            { "event_armourer", ITEM + "Anvil.png" },
            { "event_forager", ITEM + "Gift_Blue.png" },
            { "event_harvest", ITEM + "Medal_Gold.png" },
            { "event_storm", ITEM + "Energy.png" },
            { "event_none", ITEM + "Candle.png" },
            // grid tiles
            { "tile_gather", ITEM + "Clover.png" },
            { "tile_rest", ITEM + "Candle.png" },
            { "tile_exit", ITEM + "Soulgem.png" },
            { "tile_start", DEMO + "Icon_MenuIcon_Falg.png" },
            // fight actions & skills
            { "action_strike", ITEM + "Sword_B.png" },
            { "action_guard", ITEM + "Shield_B.png" },
            { "action_lucky", ITEM + "Clover.png" },
            { "action_flee", ITEM + "Talaria.png" },
            { "skill_cleave", DEMO + "Icon_SkillIcon_Attack.png" },
            { "skill_volley", DEMO + "Icon_SkillIcon_Arrow.png" },
            { "skill_heartysnack", DEMO + "Icon_SkillIcon_Potion.png" },
            // big headline images
            { "text_victory", ACT + "ActionText_Victory.png" },
            { "text_complete", ACT + "ActionText_Complete.png" },
            { "text_defeat", ACT + "ActionText_Defeat.png" },
            { "text_levelup", ACT + "ActionText_LevelUp1.png" },
            { "text_win", ACT + "ActionText_Win.png" },
            { "text_great", ACT + "ActionText_Great.png" },
            { "text_go", ACT + "ActionText_Go.png" },
        };

        static readonly string[,] RouteMap =
        {
            { "meadow", ITEM + "Clover.png" }, { "berry", ITEM + "Purplegem.png" }, { "salt", ITEM + "Gem.png" },
            { "cavern", ITEM + "Pickax.png" }, { "chili", ITEM + "Bomb.png" }, { "ember", ITEM + "Crown.png" },
        };

        static readonly string[,] GearMap =
        {
            { "weapon_0", ITEM + "Hammer.png" }, { "weapon_1", ITEM + "Pickax.png" }, { "weapon_2", ITEM + "Sword_A.png" },
            { "weapon_3", ITEM + "Sword_B.png" }, { "weapon_4", ITEM + "Soulgem.png" },
            { "armour_0", ITEM + "Shield_C.png" }, { "armour_1", ITEM + "Shield_D.png" }, { "armour_2", ITEM + "Shield_A.png" },
            { "armour_3", ITEM + "Shield_B.png" }, { "armour_4", ITEM + "Shield_B.png" },
            { "backpack", ITEM + "Treasure.png" },
        };

        // game id → monster pack folder
        static readonly string[,] MonsterMap =
        {
            { "slime", "Slime" }, { "boar", "Bovine Boar" }, { "bat", "Bee" }, { "crab", "Snake Viper" },
            { "wolf", "Canine Wolf" }, { "shaman", "Mage Witch Doctor" }, { "dragon", "Dragon" },
        };
        static readonly string[,] ClassMap =
        {
            { "Warden", "Knight" }, { "Ranger", "Knight Archer" }, { "Cook", "Peasant Male Brown" },
        };

        [MenuItem("Hearth & Hollow/Build Art Library", false, 3)]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder("Assets/HearthHollow/Art")) AssetDatabase.CreateFolder("Assets/HearthHollow", "Art");
            var lib = AssetDatabase.LoadAssetAtPath<ArtLibrary>(LibPath);
            if (lib == null) { lib = ScriptableObject.CreateInstance<ArtLibrary>(); AssetDatabase.CreateAsset(lib, LibPath); }

            var missing = new List<string>();
            Fill(lib.icons, IconMap, missing);
            Fill(lib.routes, RouteMap, missing);
            Fill(lib.gear, GearMap, missing);
            FillAnims(lib.monsters, MonsterMap, missing);
            FillAnims(lib.classes, ClassMap, missing);
            // ingredients / recipes: the packs have no food art — slots are listed so you can drop sprites in.
            EnsureIds(lib.ingredients, "ingredients");
            EnsureIds(lib.recipes, "recipes");

            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            Debug.Log("[Hearth & Hollow] Art Library built at " + LibPath + (missing.Count > 0 ? " — missing: " + string.Join(", ", missing.ToArray()) : " — all mapped."));
            Selection.activeObject = lib;
        }

        /// The library used by the build (created if needed). Called by the scene builder.
        public static ArtLibrary EnsureLibrary()
        {
            var lib = AssetDatabase.LoadAssetAtPath<ArtLibrary>(LibPath);
            if (lib == null) { Build(); lib = AssetDatabase.LoadAssetAtPath<ArtLibrary>(LibPath); }
            return lib;
        }

        static void Fill(List<ArtLibrary.Icon> list, string[,] map, List<string> missing)
        {
            for (int i = 0; i < map.GetLength(0); i++)
            {
                string id = map[i, 0];
                var sp = LoadSprite(map[i, 1]);
                if (sp == null) { missing.Add(id); continue; }
                var e = list.Find(x => x.id == id);
                if (e == null) { e = new ArtLibrary.Icon { id = id }; list.Add(e); }
                e.sprite = sp;
            }
        }

        static void EnsureIds(List<ArtLibrary.Icon> list, string kind)
        {
            var content = AssetDatabase.LoadAssetAtPath<GameContentAsset>("Assets/HearthHollow/Content/GameContent.asset");
            if (content == null || content.content == null) return;
            var c = content.content;
            if (kind == "ingredients") foreach (var x in c.ingredients) { if (!list.Exists(e => e.id == x.id)) list.Add(new ArtLibrary.Icon { id = x.id }); }
            else foreach (var x in c.recipes) { if (!list.Exists(e => e.id == x.id)) list.Add(new ArtLibrary.Icon { id = x.id }); }
        }

        static Sprite LoadSprite(string path)
        {
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) return sp;
            foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(path)) if (o is Sprite) return (Sprite)o;
            return null;
        }

        static void FillAnims(List<ArtLibrary.Anim> list, string[,] map, List<string> missing)
        {
            for (int i = 0; i < map.GetLength(0); i++)
            {
                string id = map[i, 0], folder = MON + map[i, 1];
                string idlePath = FindSheet(folder, "_Idle"), attackPath = FindSheet(folder, "_Attack");
                if (idlePath == null) { missing.Add(id); continue; }
                var a = list.Find(x => x.id == id);
                if (a == null) { a = new ArtLibrary.Anim { id = id }; list.Add(a); }
                a.idle = FrontRow(idlePath);
                a.attack = attackPath != null ? FrontRow(attackPath) : new List<Sprite>();
                a.fps = 6f;
                a.crop = ComputeCrop(a);
                // every direction (rows Down, Left, Right, Up) for the real-time dungeon
                string movePath = FindSheet(folder, "_Move");
                a.idleSheet = AllRows(idlePath, out a.idleCols);
                a.attackSheet = attackPath != null ? AllRows(attackPath, out a.attackCols) : new List<Sprite>();
                a.moveSheet = movePath != null ? AllRows(movePath, out a.moveCols) : new List<Sprite>();
            }
        }

        /// Every frame of a sheet, top row first, left to right; `cols` = frames per row.
        static List<Sprite> AllRows(string path, out int cols)
        {
            var all = new List<Sprite>();
            foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(path)) if (o is Sprite) all.Add((Sprite)o);
            all.Sort((a, b) => a.rect.y != b.rect.y ? b.rect.y.CompareTo(a.rect.y) : a.rect.x.CompareTo(b.rect.x));
            cols = 0;
            if (all.Count > 0) { float topY = all[0].rect.y; foreach (var s in all) if (Mathf.Approximately(s.rect.y, topY)) cols++; }
            return all;
        }

        static string FindSheet(string folder, string tag)
        {
            string abs = Path.Combine(Application.dataPath, folder.Substring("Assets/".Length));
            if (!Directory.Exists(abs)) return null;
            string[] files = Directory.GetFiles(abs, "*.png");
            System.Array.Sort(files);
            string numbered = null;   // e.g. Bovine_Boar_Attack_1 when there's no plain _Attack sheet
            foreach (var f in files)
            {
                string n = Path.GetFileNameWithoutExtension(f);
                if (n.EndsWith(tag)) return folder + "/" + Path.GetFileName(f);
                if (numbered == null && n.Contains(tag + "_")) numbered = folder + "/" + Path.GetFileName(f);
            }
            return numbered;
        }

        /// The sheet's top row (front-facing), frames in order.
        static List<Sprite> FrontRow(string path)
        {
            var all = new List<Sprite>();
            foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(path)) if (o is Sprite) all.Add((Sprite)o);
            if (all.Count == 0) return all;
            float topY = float.MinValue;
            foreach (var s in all) if (s.rect.y > topY) topY = s.rect.y;
            var row = all.FindAll(s => Mathf.Approximately(s.rect.y, topY));
            row.Sort((a, b) => a.rect.x.CompareTo(b.rect.x));
            return row;
        }

        /// Union of visible pixels across all frames (cell-local), padded and made square.
        static Rect ComputeCrop(ArtLibrary.Anim a)
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
            float cellW = 0, cellH = 0;
            var frames = new List<Sprite>(a.idle); frames.AddRange(a.attack);
            var cache = new Dictionary<Texture2D, Color32[]>();
            foreach (var f in frames)
            {
                Texture2D tex = f.texture;
                Color32[] px;
                if (!cache.TryGetValue(tex, out px)) { px = ReadPixels(tex); cache[tex] = px; }
                Rect r = f.rect; cellW = r.width; cellH = r.height;
                for (int y = 0; y < (int)r.height; y++)
                    for (int x = 0; x < (int)r.width; x++)
                    {
                        if (px[((int)r.y + y) * tex.width + (int)r.x + x].a <= 10) continue;
                        if (x < minX) minX = x; if (x > maxX) maxX = x;
                        if (y < minY) minY = y; if (y > maxY) maxY = y;
                    }
            }
            if (maxX < 0) return new Rect(0, 0, cellW, cellH);
            const int pad = 2;
            float w = maxX - minX + 1 + pad * 2, h = maxY - minY + 1 + pad * 2;
            float size = Mathf.Max(w, h);
            float cx = (minX + maxX + 1) * 0.5f, cy = (minY + maxY + 1) * 0.5f;
            float x0 = Mathf.Clamp(Mathf.Round(cx - size / 2f), 0, cellW - size);
            float y0 = Mathf.Clamp(Mathf.Round(cy - size / 2f), 0, cellH - size);
            size = Mathf.Min(size, Mathf.Min(cellW, cellH));
            return new Rect(Mathf.Max(0, x0), Mathf.Max(0, y0), size, size);
        }

        static Color32[] ReadPixels(Texture2D tex)
        {
            var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(tex, rt);
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var copy = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0); copy.Apply();
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
            var px = copy.GetPixels32();
            Object.DestroyImmediate(copy);
            return px;
        }
    }
}
#endif
