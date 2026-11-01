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
            { "Warden", "Knight" }, { "Ranger", "Knight Archer" },   // Cook: Ninja Adventure "Villager" (NinjaClassMap below)
            { "Lamplighter", "Mage Priest" }, { "Emberfist", "Mage Monk" },   // class rework (31 Oct), from the 100 Monsters pack
        };

        // ───── Ninja Adventure (Assets/Ninja Adventure - Asset Pack, CC0) ─────
        // Each character has SeparateAnim/Idle.png (64×16), Walk.png (64×64) and Attack.png (64×16): 16px cells, one COLUMN per
        // direction (Down, Up, Left, Right) and walk frames down the rows. They're sliced here and re-ordered into the
        // Anim layout (rows Down, Left, Right, Up; frames across), and drawn ×NinjaScale so they stand about a tile tall.
        // Monster folders (Actor/Monster/<Name>/<Name>.png or SpriteSheet.png, 64×64) use the same layout as Walk.png:
        // one column per direction, 4 walk frames down the rows. Map entries: { our id, folder under Actor/, scale }.
        // An entry whose folder is missing is skipped, so the older art (ClassMap / MonsterMap) stays as the fallback.
        const string NINJA = "Assets/Ninja Adventure - Asset Pack/Actor/";
        const float NinjaScale = 1.8f;   // was 2.25, cut 20% (31 Oct) so figures sit better in the rooms
        static readonly string[,] NinjaClassMap =
        {
            { "Cook", "Character/Villager", "1.8" },
            { "Warden", "Character/Knight", "1.8" },
            { "Ranger", "Character/Hunter", "1.8" },
            { "Lamplighter", "Character/SorcererOrange", "1.8" },
            { "Emberfist", "Character/FighterRed", "1.8" },
            // town folk (TownController looks these up by name, falling back to a tinted class)
            { "Wren", "Character/Woman", "1.8" },
            { "Merchant", "Character/Noble", "1.8" },
            { "Armourer", "Character/KnightGold", "1.8" },
            // townsfolk at work (TownWorkAreas)
            { "TownSmith", "Character/Caveman2", "1.8" },
            { "TownCarpenter", "Character/Villager3", "1.8" },
            { "TownCook", "Character/OldMan2", "1.8" },
            { "TownTrainer", "Character/SamuraiBlue", "1.8" },
        };
        static readonly string[,] NinjaMonsterMap =
        {
            { "slime", "Monster/Slime4", "1.8" },        // Moss Slime
            { "boar", "Monster/Beast", "1.8" },          // Bramble Boar
            { "bat", "Monster/LanternRed", "1.8" },      // Lantern Bee
            { "crab", "Monster/Snake3", "1.8" },         // Marsh Viper
            { "wolf", "Monster/Beast2", "1.8" },         // Fog Wolf
            { "shaman", "Character/Shaman", "1.8" },     // Bog Shaman
            { "dragon", "Monster/DragonYellow", "2.56" },  // Ember Drake (elite: bigger)
        };

        static void FillNinja(List<ArtLibrary.Anim> list, string[,] map, List<string> missing)
        {
            int[] colFor = { 0, 2, 3, 1 };   // our rows Down, Left, Right, Up → Ninja columns Down(0), Up(1), Left(2), Right(3)
            for (int i = 0; i < map.GetLength(0); i++)
            {
                string id = map[i, 0], folder = NINJA + map[i, 1], dir = folder + "/SeparateAnim/";
                float scale = map.GetLength(1) > 2 ? float.Parse(map[i, 2], System.Globalization.CultureInfo.InvariantCulture) : NinjaScale;
                Sprite[,] idle, walk, atk;
                var sheets = new List<string>();
                if (File.Exists(dir + "Idle.png"))   // characters: separate Idle / Walk / Attack sheets
                {
                    idle = NinjaCells(dir + "Idle.png"); walk = NinjaCells(dir + "Walk.png"); atk = NinjaCells(dir + "Attack.png");
                    sheets.Add(dir + "Idle.png"); sheets.Add(dir + "Walk.png"); sheets.Add(dir + "Attack.png");
                }
                else   // monsters: one walk sheet; idle bobs on its first two frames, attack lunges on the third
                {
                    string sheet = null;
                    if (Directory.Exists(folder))
                        foreach (var f in Directory.GetFiles(folder, "*.png"))
                            if (!Path.GetFileName(f).StartsWith("Faceset")) { sheet = f.Replace('\\', '/'); break; }
                    walk = sheet != null ? NinjaCells(sheet) : null;
                    idle = null; atk = null;
                    if (walk != null)
                    {
                        idle = new Sprite[4, 2]; atk = new Sprite[4, 1];
                        for (int c = 0; c < 4; c++) { idle[c, 0] = walk[c, 0]; idle[c, 1] = walk[c, 1]; atk[c, 0] = walk[c, Mathf.Min(2, walk.GetLength(1) - 1)]; }
                        sheets.Add(sheet);
                    }
                }
                if (idle == null || walk == null) { missing.Add(id + " (Ninja " + map[i, 1] + ")"); continue; }
                var a = list.Find(x => x.id == id);
                if (a == null) { a = new ArtLibrary.Anim { id = id }; list.Add(a); }
                a.idleSheet = new List<Sprite>(); a.moveSheet = new List<Sprite>(); a.attackSheet = new List<Sprite>();
                int walkRows = walk.GetLength(1);
                foreach (int c in colFor)
                {
                    for (int r = 0; r < idle.GetLength(1); r++) a.idleSheet.Add(idle[c, r]);
                    for (int r = 0; r < walkRows; r++) a.moveSheet.Add(walk[c, r]);
                    a.attackSheet.Add(atk != null ? atk[c, 0] : idle[c, 0]);
                }
                a.idleCols = idle.GetLength(1); a.moveCols = walkRows; a.attackCols = 1;
                a.idle = new List<Sprite> { idle[0, 0] };
                a.attack = new List<Sprite> { atk != null ? atk[0, 0] : idle[0, 0] };
                a.fps = 6f; a.crop = new Rect(0, 0, 16, 16); a.scale = scale;
                // normal maps, like the rest of the art, so the sprites shade in the dungeon light instead of glowing flat-white
                foreach (var sheet in sheets)
                    if (AssetImporter.GetAtPath(sheet) != null) NormalMapBuilder.Make(sheet);
            }
        }

        /// Slices a Ninja Adventure sheet into 16px cells (pixel-art import settings) and returns them as [column, row from the top].
        // ── Ninja Adventure bosses (Actor/Boss/<Name>/Idle.png, Walk/Jump.png, Attack.png…): one row of big frames each ──
        // Frames are as wide as the strip divides into nearest the strip's height (GiantSlime 62×52, Tengu 82×82…). The same
        // frames are used for all four facings. Each cell this many px tall draws at ×1 before the boss's own ×1.7.
        const float BossTargetPx = 40f;
        static readonly string[,] NinjaBossMap =
        {
            { "slime", "GiantSlime" },     // Moss Slime → Giant Slime
            { "boar", "GiantRacoon" },     // Bramble Boar → Giant Raccoon
            { "bat", "GiantFlam" },        // Lantern Bee → flame spirit
            { "crab", "GiantFrog2" },      // Marsh Viper → Giant Frog
            { "wolf", "GiantSpirit" },     // Fog Wolf → Giant Spirit
            { "shaman", "TenguRed" },      // Bog Shaman → Tengu
            { "dragon", "DemonCyclop2" },  // Ember Drake → Cyclops demon
        };

        static void FillNinjaBosses(List<ArtLibrary.Anim> list, string[,] map, List<string> missing)
        {
            for (int i = 0; i < map.GetLength(0); i++)
            {
                string id = map[i, 0], folder = NINJA + "Boss/" + map[i, 1];
                string idleP = BossSheet(folder, "Idle");
                int cw = 0, ch = 0;
                var idle = idleP != null ? NinjaStrip(idleP, 0, out cw, out ch) : null;
                if (idle == null || idle.Count == 0) { missing.Add("boss " + id + " (Ninja " + map[i, 1] + ")"); continue; }
                var used = new List<string> { idleP };
                List<Sprite> walk = null, atk = null;
                foreach (var pre in new[] { "Walk", "Jump" })
                {
                    string p = BossSheet(folder, pre); if (p == null) continue;
                    int w2, h2; var s = NinjaStrip(p, cw, out w2, out h2);
                    if (s != null && s.Count > 0 && w2 == cw && h2 == ch) { walk = s; used.Add(p); break; }
                }
                foreach (var pre in new[] { "Attack", "Shoot", "Charge" })
                {
                    string p = BossSheet(folder, pre); if (p == null) continue;
                    int w2, h2; var s = NinjaStrip(p, cw, out w2, out h2);
                    if (s != null && s.Count > 0 && w2 == cw && h2 == ch) { atk = s; used.Add(p); break; }
                }
                if (walk == null) walk = idle;
                if (atk == null) atk = idle;
                var a = list.Find(x => x.id == id);
                if (a == null) { a = new ArtLibrary.Anim { id = id }; list.Add(a); }
                a.idleSheet = Rows4(idle); a.moveSheet = Rows4(walk); a.attackSheet = Rows4(atk);
                a.idleCols = idle.Count; a.moveCols = walk.Count; a.attackCols = atk.Count;
                a.idle = new List<Sprite>(idle); a.attack = new List<Sprite>(atk);
                a.fps = 8f; a.crop = new Rect(0, 0, cw, ch); a.scale = BossTargetPx / Mathf.Max(1f, ch);
                foreach (var s in used) if (AssetImporter.GetAtPath(s) != null) NormalMapBuilder.Make(s);
            }
        }

        static List<Sprite> Rows4(List<Sprite> frames) { var l = new List<Sprite>(); for (int r = 0; r < 4; r++) l.AddRange(frames); return l; }

        /// The boss sheet whose name is `prefix` (or starts with it; never the right-facing attack copy).
        static string BossSheet(string folder, string prefix)
        {
            if (!Directory.Exists(folder)) return null;
            string loose = null;
            foreach (var f in Directory.GetFiles(folder, "*.png"))
            {
                string n = Path.GetFileNameWithoutExtension(f);
                if (n == prefix) return f.Replace('\\', '/');
                if (loose == null && n.StartsWith(prefix) && !n.EndsWith("Right")) loose = f.Replace('\\', '/');
            }
            return loose;
        }

        /// Slices one row of big frames (pixel-art import, ppu 16, pivot bottom-centre). `wantW` > 0 forces that frame width.
        static List<Sprite> NinjaStrip(string path, int wantW, out int cw, out int ch)
        {
            cw = 0; ch = 0;
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (imp == null || tex == null) return null;
            int w = tex.width, h = tex.height;
            int best = w;
            if (wantW > 0 && w % wantW == 0) best = wantW;
            else { float bd = float.MaxValue; for (int n = 1; n <= 64; n++) if (w % n == 0) { int c = w / n; float d = Mathf.Abs(c - h); if (d < bd) { bd = d; best = c; } } }
            cw = best; ch = h;
            int frames = Mathf.Max(1, w / cw);
            bool dirty = imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Multiple || imp.filterMode != FilterMode.Point
                         || imp.textureCompression != TextureImporterCompression.Uncompressed || !Mathf.Approximately(imp.spritePixelsPerUnit, 16f) || imp.mipmapEnabled;
#pragma warning disable 0618
            if (imp.spritesheet == null || imp.spritesheet.Length != frames || (imp.spritesheet.Length > 0 && !Mathf.Approximately(imp.spritesheet[0].rect.width, cw))) dirty = true;
            if (dirty)
            {
                imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Multiple;
                imp.filterMode = FilterMode.Point; imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.spritePixelsPerUnit = 16f; imp.mipmapEnabled = false;
                var metas = new List<SpriteMetaData>();
                for (int c = 0; c < frames; c++)
                    metas.Add(new SpriteMetaData { name = Path.GetFileNameWithoutExtension(path) + "_f" + c, rect = new Rect(c * cw, 0, cw, h),
                                                    alignment = (int)SpriteAlignment.BottomCenter, pivot = new Vector2(0.5f, 0f) });
                imp.spritesheet = metas.ToArray();
                imp.SaveAndReimport();
            }
#pragma warning restore 0618
            var list = new List<Sprite>();
            foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(path)) { var s = o as Sprite; if (s != null) list.Add(s); }
            list.Sort((x, y) => x.rect.x.CompareTo(y.rect.x));
            return list;
        }

        static Sprite[,] NinjaCells(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return null;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) return null;
            int cols = tex.width / 16, rows = tex.height / 16;
            bool dirty = imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Multiple || imp.filterMode != FilterMode.Point
                         || imp.textureCompression != TextureImporterCompression.Uncompressed || !Mathf.Approximately(imp.spritePixelsPerUnit, 16f) || imp.mipmapEnabled;
#pragma warning disable 0618
            if (imp.spritesheet == null || imp.spritesheet.Length != cols * rows) dirty = true;
            if (dirty)
            {
                imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Multiple;
                imp.filterMode = FilterMode.Point; imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.spritePixelsPerUnit = 16f; imp.mipmapEnabled = false;
                var metas = new List<SpriteMetaData>();
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                        metas.Add(new SpriteMetaData { name = Path.GetFileNameWithoutExtension(path) + "_c" + c + "_r" + r, rect = new Rect(c * 16, tex.height - (r + 1) * 16, 16, 16),
                                                        alignment = (int)SpriteAlignment.BottomCenter, pivot = new Vector2(0.5f, 0f) });
                imp.spritesheet = metas.ToArray();
                imp.SaveAndReimport();
            }
#pragma warning restore 0618
            var cells = new Sprite[cols, rows];
            foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
            {
                var s = o as Sprite; if (s == null) continue;
                int c = Mathf.RoundToInt(s.rect.x / 16f), r = Mathf.RoundToInt((tex.height - s.rect.y - 16f) / 16f);
                if (c >= 0 && c < cols && r >= 0 && r < rows) cells[c, r] = s;
            }
            return cells;
        }

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
            FillNinja(lib.classes, NinjaClassMap, missing);
            FillNinja(lib.monsters, NinjaMonsterMap, missing);
            if (lib.bosses == null) lib.bosses = new List<ArtLibrary.Anim>();
            FillNinjaBosses(lib.bosses, NinjaBossMap, missing);   // Ninja Adventure bosses: big side-view strips   // Ninja Adventure characters (16px, sliced and re-ordered here)
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
                a.fps = 6f; a.scale = 1f;
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
