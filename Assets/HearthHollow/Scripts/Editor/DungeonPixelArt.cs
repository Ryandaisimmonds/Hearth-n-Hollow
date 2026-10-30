// Hearth & Hollow — tiny procedural pixel-art painter for the dungeon kit's PLACEHOLDER tiles and props.
// Everything here is replaceable: swap any tile/sprite on a DungeonTheme for real art and these are unused.
using System;
using UnityEngine;

namespace HearthHollow.EditorTools
{
    public static class DungeonPixelArt
    {
        public const int S = 32;

        // ───────────── helpers ─────────────
        public static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 2147483647;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        /// Tileable value noise (period = S).
        public static float Noise(float x, float y, int cell, int seed)
        {
            int n = Math.Max(1, S / cell);
            float fx = x / cell, fy = y / cell;
            int x0 = (int)Math.Floor(fx), y0 = (int)Math.Floor(fy);
            float tx = fx - x0, ty = fy - y0;
            tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
            float a = Hash(Mod(x0, n), Mod(y0, n), seed), b = Hash(Mod(x0 + 1, n), Mod(y0, n), seed);
            float c = Hash(Mod(x0, n), Mod(y0 + 1, n), seed), d = Hash(Mod(x0 + 1, n), Mod(y0 + 1, n), seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        static int Mod(int a, int n) { int r = a % n; return r < 0 ? r + n : r; }

        public static Color32 C(int r, int g, int b, int a = 255) { return new Color32((byte)r, (byte)g, (byte)b, (byte)a); }

        public static Color32 Shade(Color32 c, float k)
        {
            return new Color32((byte)Mathf.Clamp(c.r * k, 0, 255), (byte)Mathf.Clamp(c.g * k, 0, 255), (byte)Mathf.Clamp(c.b * k, 0, 255), c.a);
        }

        public static Color32 Mix(Color32 a, Color32 b, float t)
        {
            return new Color32((byte)Mathf.Lerp(a.r, b.r, t), (byte)Mathf.Lerp(a.g, b.g, t), (byte)Mathf.Lerp(a.b, b.b, t), (byte)Mathf.Lerp(a.a, b.a, t));
        }

        public static Color32[] Blank(int w = S, int h = S)
        {
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 0);
            return px;
        }

        static void Set(Color32[] px, int w, int x, int y, Color32 c)
        {
            int h = px.Length / w;
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            px[y * w + x] = c;
        }

        static void Disc(Color32[] px, int w, float cx, float cy, float r, Color32 c)
        {
            int h = px.Length / w;
            for (int y = (int)(cy - r - 1); y <= (int)(cy + r + 1); y++)
                for (int x = (int)(cx - r - 1); x <= (int)(cx + r + 1); x++)
                    if ((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy) <= r * r && x >= 0 && y >= 0 && x < w && y < h)
                        px[y * w + x] = c;
        }

        // ───────────── floors ─────────────
        public enum FloorStyle { Speckle, Planks, Cobbles, Cracks }

        public static Color32[] Floor(Color32 baseC, FloorStyle style, int seed, Color32 accent, float accentAmount)
        {
            var px = Blank();
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float n = Noise(x, y, 8, seed) * 0.6f + Noise(x, y, 4, seed + 7) * 0.4f;
                    float k = 0.86f + n * 0.26f;
                    if (Hash(x, y, seed + 3) < 0.05f) k *= 0.85f;
                    if (Hash(x, y, seed + 4) < 0.03f) k *= 1.15f;
                    px[y * S + x] = Shade(baseC, k);
                }
            switch (style)
            {
                case FloorStyle.Planks:
                    for (int y = 0; y < S; y++)
                        for (int x = 0; x < S; x++)
                        {
                            int row = y / 8;
                            int seam = (row % 2 == 0) ? 0 : 16;
                            if (y % 8 == 0) px[y * S + x] = Shade(baseC, 0.55f);
                            else if (x == seam) px[y * S + x] = Shade(baseC, 0.65f);
                            else if (Hash(x / 3, y, seed + row) < 0.25f) px[y * S + x] = Shade(px[y * S + x], 0.92f);
                        }
                    break;
                case FloorStyle.Cobbles:
                {
                    var pts = new Vector2[6];
                    for (int i = 0; i < pts.Length; i++) pts[i] = new Vector2(Hash(i, 1, seed) * S, Hash(i, 2, seed) * S);
                    for (int y = 0; y < S; y++)
                        for (int x = 0; x < S; x++)
                        {
                            float d1 = 999, d2 = 999;
                            foreach (var p in pts)
                                for (int ox = -1; ox <= 1; ox++) for (int oy = -1; oy <= 1; oy++)
                                {
                                    float d = Vector2.Distance(new Vector2(x, y), p + new Vector2(ox * S, oy * S));
                                    if (d < d1) { d2 = d1; d1 = d; } else if (d < d2) d2 = d;
                                }
                            if (d2 - d1 < 1.6f) px[y * S + x] = Shade(baseC, 0.6f);
                            else if (d1 < 3f) px[y * S + x] = Shade(px[y * S + x], 1.08f);
                        }
                    break;
                }
                case FloorStyle.Cracks:
                {
                    var rng = new System.Random(seed);
                    for (int c = 0; c < 3; c++)
                    {
                        int x = rng.Next(S), y = rng.Next(S);
                        for (int i = 0; i < 14; i++)
                        {
                            Set(px, S, Mod(x, S), Mod(y, S), Shade(baseC, 0.55f));
                            x += rng.Next(3) - 1; y += rng.Next(2) == 0 ? 1 : 0;
                        }
                    }
                    break;
                }
            }
            // accent specks (flowers, glints, embers…)
            if (accentAmount > 0f)
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                        if (Hash(x, y, seed + 99) < accentAmount) px[y * S + x] = accent;
            return px;
        }

        // ───────────── walls ─────────────
        /// Seen from above: dark blocks with a lighter rim.
        public static Color32[] WallTop(Color32 baseC, int seed)
        {
            var px = Blank();
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int row = y / 8;
                    int off = row % 2 == 0 ? 0 : 8;
                    bool mortar = y % 8 == 0 || (x + off) % 16 == 0;
                    float n = 0.9f + Noise(x, y, 8, seed) * 0.2f;
                    px[y * S + x] = mortar ? Shade(baseC, 0.6f) : Shade(baseC, n * (y % 8 == 7 ? 1.12f : 1f));
                }
            return px;
        }

        /// The front face of the north wall: bricks, lit on top, shadowed at the floor.
        public static Color32[] WallFace(Color32 baseC, int seed)
        {
            var px = Blank();
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int row = y / 8;
                    int off = row % 2 == 0 ? 4 : 12;
                    bool mortar = y % 8 == 0 || (x + off) % 16 == 0;
                    float light = 0.8f + (y / (float)S) * 0.35f;
                    float n = 0.92f + Noise(x, y, 4, seed) * 0.16f;
                    Color32 c = mortar ? Shade(baseC, 0.62f) : Shade(baseC, light * n);
                    if (y < 4) c = Shade(c, 0.55f + y * 0.1f);   // contact shadow
                    px[y * S + x] = c;
                }
            return px;
        }

        // ───────────── hazards ─────────────
        public enum HazardStyle { Water, Lava, Chasm, Thorns, Mud }

        public static Color32[] Hazard(Color32 baseC, HazardStyle style, int seed)
        {
            var px = Blank();
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float n = Noise(x, y, 8, seed);
                    Color32 c = Shade(baseC, 0.85f + n * 0.3f);
                    switch (style)
                    {
                        case HazardStyle.Water:
                            if (Mathf.Abs(Mathf.Sin((x + y * 0.5f) * 0.4f + seed) ) > 0.97f && (y % 8) < 2) c = Shade(baseC, 1.45f);
                            break;
                        case HazardStyle.Lava:
                            c = Mix(Shade(baseC, 0.7f), C(255, 220, 90), Mathf.Clamp01(n * 1.4f - 0.4f));
                            break;
                        case HazardStyle.Chasm:
                            c = Shade(baseC, 0.6f + n * 0.5f);
                            break;
                        case HazardStyle.Thorns:
                            if (Hash(x, y, seed) < 0.12f) c = C(200, 190, 150);
                            else if (n > 0.6f) c = Shade(baseC, 1.25f);
                            break;
                        case HazardStyle.Mud:
                            if (Hash(x / 2, y / 2, seed) < 0.05f) c = Shade(baseC, 1.5f);
                            break;
                    }
                    px[y * S + x] = c;
                }
            return px;
        }

        // ───────────── decor (transparent) ─────────────
        public enum DecorStyle { Tuft, Flower, Pebbles, Mushroom, Crystal, Shell, Ember, Berries, Bones, Reed }

        public static Color32[] Decor(DecorStyle style, Color32 c1, Color32 c2, int seed)
        {
            var px = Blank();
            var rng = new System.Random(seed);
            float ox = 10 + rng.Next(12), oy = 8 + rng.Next(14);
            switch (style)
            {
                case DecorStyle.Tuft:
                case DecorStyle.Reed:
                    for (int i = 0; i < 5; i++)
                    {
                        int bx = (int)ox - 3 + i * 2; int h = 3 + rng.Next(style == DecorStyle.Reed ? 8 : 4);
                        for (int y = 0; y < h; y++) Set(px, S, bx + (y > h / 2 ? (i % 2 == 0 ? -1 : 1) : 0), (int)oy + y, y == h - 1 && style == DecorStyle.Reed ? c2 : Shade(c1, 0.8f + y * 0.05f));
                    }
                    break;
                case DecorStyle.Flower:
                    for (int i = 0; i < 2; i++)
                    {
                        float fx = ox + i * 6 - 3, fy = oy + (i % 2) * 4;
                        Set(px, S, (int)fx, (int)fy - 1, Shade(c1, 0.7f)); Set(px, S, (int)fx, (int)fy - 2, Shade(c1, 0.7f));
                        Disc(px, S, fx + 0.5f, fy + 0.5f, 1.6f, c2); Set(px, S, (int)fx, (int)fy, C(255, 230, 120));
                    }
                    break;
                case DecorStyle.Pebbles:
                    for (int i = 0; i < 3; i++) Disc(px, S, ox + rng.Next(9) - 4, oy + rng.Next(7) - 3, 1.2f + (float)rng.NextDouble() * 1.2f, Shade(c1, 0.85f + i * 0.1f));
                    break;
                case DecorStyle.Mushroom:
                    for (int y = 0; y < 3; y++) Set(px, S, (int)ox, (int)oy + y, C(230, 220, 200));
                    Disc(px, S, ox + 0.5f, oy + 4f, 2.6f, c1); Set(px, S, (int)ox - 1, (int)oy + 5, c2); Set(px, S, (int)ox + 1, (int)oy + 4, c2);
                    break;
                case DecorStyle.Crystal:
                    for (int y = 0; y < 8; y++)
                        for (int x = -3; x <= 3; x++)
                            if (Mathf.Abs(x) <= (y < 5 ? y * 0.7f : (8 - y) * 1.2f)) Set(px, S, (int)ox + x, (int)oy + y, x < 0 ? c1 : c2);
                    break;
                case DecorStyle.Shell:
                    for (int y = 0; y < 5; y++) for (int x = -y; x <= y; x++) if (y + Mathf.Abs(x) < 6) Set(px, S, (int)ox + x, (int)oy + 5 - y, (x % 2 == 0) ? c1 : c2);
                    break;
                case DecorStyle.Ember:
                    for (int i = 0; i < 4; i++) { var p = new Vector2(ox + rng.Next(10) - 5, oy + rng.Next(8) - 4); Set(px, S, (int)p.x, (int)p.y, c2); Set(px, S, (int)p.x + 1, (int)p.y, c1); }
                    break;
                case DecorStyle.Berries:
                    for (int i = 0; i < 4; i++) Set(px, S, (int)ox - 2 + i, (int)oy - 1, Shade(c1, 0.8f));
                    for (int i = 0; i < 4; i++) Disc(px, S, ox - 2 + i * 1.5f, oy + (i % 2) * 1.5f, 1.1f, c2);
                    break;
                case DecorStyle.Bones:
                    for (int x = -3; x <= 3; x++) Set(px, S, (int)ox + x, (int)oy, c1);
                    Set(px, S, (int)ox - 4, (int)oy + 1, c1); Set(px, S, (int)ox - 4, (int)oy - 1, c1);
                    Set(px, S, (int)ox + 4, (int)oy + 1, c1); Set(px, S, (int)ox + 4, (int)oy - 1, c1);
                    break;
            }
            return px;
        }

        // ───────────── markers (what room prefabs are painted with) ─────────────
        public static Color32[] Marker(Color32 fill, Color32 line, int pattern)
        {
            var px = Blank();
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    bool edge = x == 0 || y == 0 || x == S - 1 || y == S - 1;
                    bool pat = pattern == 1 ? (y % 8 == 0 || (x + (y / 8 % 2) * 8) % 16 == 0)       // bricks
                             : pattern == 2 ? ((x + y) % 8 == 0)                                         // hatching
                             : pattern == 3 ? (y % 8 == 0 || (x + (y / 8 % 2) * 8) % 16 == 0 || y > 26) // face
                             : false;
                    px[y * S + x] = edge ? Shade(fill, 0.75f) : pat ? line : fill;
                }
            return px;
        }

        // ───────────── props ─────────────
        public static Color32[] DoorBars(Color32 metal, out int w, out int h)
        {
            w = 96; h = 32;
            var px = Blank(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool bar = x % 10 >= 4 && x % 10 <= 6;
                    bool band = (y >= 6 && y <= 9) || (y >= 22 && y <= 25);
                    if (bar || band) px[y * w + x] = Shade(metal, bar && !band ? 0.9f + (x % 10 == 5 ? 0.25f : 0f) : 0.75f);
                }
            return px;
        }

        public static Color32[] Ladder(Color32 hole, Color32 wood)
        {
            var px = Blank();
            for (int y = 3; y < 29; y++) for (int x = 5; x < 27; x++) px[y * S + x] = Shade(hole, 0.6f + (y / 32f) * 0.4f);
            for (int y = 3; y < 30; y++) { Set(px, S, 10, y, wood); Set(px, S, 11, y, Shade(wood, 0.8f)); Set(px, S, 21, y, wood); Set(px, S, 22, y, Shade(wood, 0.8f)); }
            for (int y = 6; y < 29; y += 5) for (int x = 10; x <= 22; x++) Set(px, S, x, y, Shade(wood, 1.1f));
            return px;
        }

        public static Color32[] Portal(Color32 accent)
        {
            var px = Blank();
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = x + 0.5f - 16, dy = y + 0.5f - 16;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    if (r > 14.5f) continue;
                    float a = Mathf.Atan2(dy, dx);
                    float swirl = Mathf.Sin(a * 3f + r * 0.6f);
                    Color32 c = r > 12.5f ? Shade(accent, 0.7f) : Mix(C(255, 255, 255), accent, Mathf.Clamp01(r / 12f));
                    if (swirl > 0.6f && r < 12.5f) c = Shade(c, 1.25f);
                    c.a = (byte)(r > 12.5f ? 255 : 230);
                    px[y * S + x] = c;
                }
            return px;
        }

        public static Color32[] Campfire()
        {
            var px = Blank();
            Color32 log = C(120, 78, 44), logD = C(84, 52, 30);
            for (int i = 0; i < 18; i++) { Set(px, S, 7 + i, 5 + i / 3, i % 4 == 0 ? logD : log); Set(px, S, 7 + i, 6 + i / 3, logD); Set(px, S, 24 - i, 5 + i / 3, i % 4 == 0 ? logD : log); Set(px, S, 24 - i, 6 + i / 3, logD); }
            for (int y = 8; y < 26; y++)
            {
                float wdt = Mathf.Max(0f, 7f - Mathf.Abs(y - 13f) * 0.55f) * (y < 13 ? 1f : 1f - (y - 13) / 14f);
                for (int x = (int)(16 - wdt); x <= (int)(16 + wdt); x++)
                {
                    float t = Mathf.Abs(x + 0.5f - 16) / Mathf.Max(1f, wdt);
                    Set(px, S, x, y, t < 0.45f && y < 20 ? C(255, 236, 140) : t < 0.75f ? C(255, 160, 50) : C(220, 80, 30));
                }
            }
            return px;
        }

        public static Color32[] Bush(Color32 leaf, Color32 fruit, int seed)
        {
            var px = Blank();
            var rng = new System.Random(seed);
            Disc(px, S, 16, 12, 9, Shade(leaf, 0.75f));
            Disc(px, S, 11, 14, 6, leaf); Disc(px, S, 21, 14, 6, leaf); Disc(px, S, 16, 18, 6.5f, Shade(leaf, 1.15f));
            for (int i = 0; i < 6; i++) Disc(px, S, 9 + rng.Next(15), 9 + rng.Next(12), 1.3f, fruit);
            // little sparkle so it reads as "use me"
            Set(px, S, 24, 25, C(255, 255, 220)); Set(px, S, 23, 26, C(255, 255, 220)); Set(px, S, 25, 26, C(255, 255, 220)); Set(px, S, 24, 27, C(255, 255, 220));
            return px;
        }
    }
}
