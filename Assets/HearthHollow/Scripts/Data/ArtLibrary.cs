// Hearth & Hollow — Art Library: maps every game id to a sprite so all art is editable in the Inspector.
// Built automatically from the installed packs by "Hearth & Hollow/Build Art Library"; any slot left
// empty falls back to the emoji in the content data, so you can drop new art in at any time.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HearthHollow.Data
{
    [CreateAssetMenu(menuName = "Hearth & Hollow/Art Library", fileName = "ArtLibrary")]
    public class ArtLibrary : ScriptableObject
    {
        [Serializable] public class Icon { public string id; public Sprite sprite; }

        /// An animated pixel character (monster or class). `idle`/`attack` are the front-facing row (UI);
        /// the *Sheet lists hold every frame row by row — rows face Down, Left, Right, Up — for the dungeon.
        /// `crop` is the visible area inside each frame cell (pixels, from the cell's bottom-left).
        [Serializable] public class Anim
        {
            public string id;
            public List<Sprite> idle = new List<Sprite>();
            public List<Sprite> attack = new List<Sprite>();
            public float fps = 6f;
            public Rect crop;
            [Header("All four directions (rows: Down, Left, Right, Up)")]
            public List<Sprite> idleSheet = new List<Sprite>();
            public List<Sprite> moveSheet = new List<Sprite>();
            public List<Sprite> attackSheet = new List<Sprite>();
            public int idleCols = 4, moveCols = 6, attackCols = 6;
            [Tooltip("Extra size for small sprites (e.g. 16px Ninja Adventure characters drawn ×2.25 to stand ~1 tile tall). 1 = as drawn.")]
            public float scale = 1f;

            /// Frame list for one direction row (0 Down, 1 Left, 2 Right, 3 Up); falls back sensibly.
            public Sprite Frame(int state, int dir, int frame)
            {
                List<Sprite> sheet = state == 1 ? moveSheet : state == 2 ? attackSheet : idleSheet;
                int cols = state == 1 ? moveCols : state == 2 ? attackCols : idleCols;
                if (sheet == null || sheet.Count == 0 || cols <= 0)
                {
                    sheet = state == 2 && attack.Count > 0 ? attack : idle; cols = sheet.Count; dir = 0;
                    if (sheet.Count == 0) return null;
                }
                int rows = Mathf.Max(1, sheet.Count / cols);
                int r = Mathf.Clamp(dir, 0, rows - 1);
                int i = r * cols + ((frame % cols) + cols) % cols;
                return i < sheet.Count ? sheet[i] : sheet[0];
            }
            public int Cols(int state) { return state == 1 ? moveCols : state == 2 ? attackCols : idleCols; }
        }

        [Header("UI / stat / tile / event icons (ids listed in the README section of the builder)")]
        public List<Icon> icons = new List<Icon>();
        [Header("Content art by id (empty = use the emoji from the content asset)")]
        public List<Icon> ingredients = new List<Icon>();
        public List<Icon> recipes = new List<Icon>();
        public List<Icon> routes = new List<Icon>();
        public List<Icon> gear = new List<Icon>();      // weapon_0..weapon_4, armour_0..armour_4, backpack
        [Header("Animated characters")]
        public List<Anim> monsters = new List<Anim>();  // by monster id
        public List<Anim> bosses = new List<Anim>();    // by monster id: drawn when that monster is a route's boss (Ninja Adventure bosses)
        public List<Anim> classes = new List<Anim>();   // by class id: Warden / Ranger / Cook

        /// The library the running game uses (set by GameBootstrap).
        public static ArtLibrary Active;

        static Sprite Find(List<Icon> list, string id)
        {
            if (list == null || string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < list.Count; i++) if (list[i] != null && list[i].id == id) return list[i].sprite;
            return null;
        }
        static Anim FindAnim(List<Anim> list, string id)
        {
            if (list == null || string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < list.Count; i++) if (list[i] != null && list[i].id == id && list[i].idle.Count > 0) return list[i];
            return null;
        }

        public Sprite IconSprite(string id) { return Find(icons, id); }
        public Sprite Ingredient(string id) { return Find(ingredients, id); }
        public Sprite Recipe(string id) { return Find(recipes, id); }
        public Sprite Route(string id) { return Find(routes, id); }
        public Sprite Gear(string id) { return Find(gear, id); }
        public Anim Monster(string id) { return FindAnim(monsters, id); }
        public Anim Boss(string id) { return FindAnim(bosses, id); }
        public static Anim GetBoss(string id) { return Active != null ? Active.Boss(id) : null; }
        public Anim Class(string id) { return FindAnim(classes, id); }

        // ── static shortcuts for the UI (null-safe when no library is assigned) ──
        public static Sprite GetIcon(string id) { return Active != null ? Active.IconSprite(id) : null; }
        public static Sprite GetIngredient(string id) { return Active != null ? Active.Ingredient(id) : null; }
        public static Sprite GetRecipe(string id) { return Active != null ? Active.Recipe(id) : null; }
        public static Sprite GetRoute(string id) { return Active != null ? Active.Route(id) : null; }
        public static Sprite GetGear(string id) { return Active != null ? Active.Gear(id) : null; }
        public static Anim GetMonster(string id) { return Active != null ? Active.Monster(id) : null; }
        public static Anim GetClass(string id) { return Active != null ? Active.Class(id) : null; }

        // Cropped runtime sprites, cached per source frame.
        static readonly Dictionary<Sprite, Sprite> _cropCache = new Dictionary<Sprite, Sprite>();

        /// The frame cut down to the anim's crop rect (pixel art sits small inside big cells).
        public static Sprite Cropped(Anim a, Sprite frame)
        {
            if (frame == null) return null;
            if (a == null || a.crop.width <= 0f || a.crop.height <= 0f) return frame;
            Sprite s;
            if (_cropCache.TryGetValue(frame, out s) && s != null) return s;
            Rect r = frame.rect;
            var rect = new Rect(r.x + a.crop.x, r.y + a.crop.y, a.crop.width, a.crop.height);
            s = Sprite.Create(frame.texture, rect, new Vector2(0.5f, 0.5f), frame.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            s.name = frame.name + "_crop";
            _cropCache[frame] = s;
            return s;
        }
    }
}
