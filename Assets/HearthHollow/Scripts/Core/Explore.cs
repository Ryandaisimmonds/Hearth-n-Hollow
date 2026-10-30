// Hearth & Hollow — Exploration (fog grid) and Gather minigame logic.
// Pure C#: no UnityEngine. Agent B. See CONTRACT.md "Explore & gather".
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public static class Explore
    {
        // Cosy floor: every grid gets at least this many of each (converted from monster tiles if rolls give none).
        const int MinGatherTiles = 2;
        const int MinRestTiles = 1;

        // ───────────────────────── Run creation ─────────────────────────
        public static RunState CreateRun(GameContent c, Profile p, RouteOffer offer, bool storm, Rng rng)
        {
            TuningData t = c.tuning;
            RouteData route = c.Route(offer.routeId);

            var run = new RunState();
            run.routeId = offer.routeId;
            run.mod = offer.mod;
            run.storm = storm;
            run.classId = p.selectedClass;
            run.width = Math.Max(2, t.gridWidth);
            run.height = Math.Max(1, t.gridHeight);
            run.hp = 0; // session sets from stats
            run.tray = new List<string>(p.tray);
            run.outcome = RunOutcome.None;

            int slots = Economy.BagSlots(c, p);
            run.bag = new List<BagSlot>(slots);
            for (int i = 0; i < slots; i++) run.bag.Add(new BagSlot { ingredientId = "", count = 0 });

            bool dangerous = storm || offer.mod == RouteMod.Dangerous;
            int baseDiff = (route != null ? route.baseDifficulty : 0) + (dangerous ? 1 : 0);
            float eliteChance = t.eliteChance * (dangerous ? 2f : 1f);
            float gatherChance = route != null ? route.gatherChance : 0.2f;

            // Monster pools
            var normalPool = new List<string>();
            var elitePool = new List<string>();
            if (route != null)
            {
                for (int i = 0; i < route.monsterIds.Count; i++)
                {
                    string id = route.monsterIds[i];
                    MonsterData m = c.Monster(id);
                    if (m == null) continue;
                    elitePool.Add(id);
                    if (!m.eliteOnly) normalPool.Add(id);
                }
            }
            if (normalPool.Count == 0)
            {
                for (int i = 0; i < c.monsters.Count; i++) if (!c.monsters[i].eliteOnly) normalPool.Add(c.monsters[i].id);
                if (normalPool.Count == 0) for (int i = 0; i < c.monsters.Count; i++) normalPool.Add(c.monsters[i].id);
            }
            if (elitePool.Count == 0) elitePool.AddRange(normalPool);

            int sx = 0, sy = run.height / 2;
            int ex = run.width - 1, ey = rng.Range(0, run.height - 1);

            run.tiles = new List<Tile>(run.width * run.height);
            for (int y = 0; y < run.height; y++)
            {
                for (int x = 0; x < run.width; x++)
                {
                    var tile = new Tile();
                    tile.difficulty = baseDiff;
                    if (x == sx && y == sy) { tile.type = TileType.Start; tile.revealed = true; tile.resolved = true; }
                    else if (x == ex && y == ey) { tile.type = TileType.Exit; }
                    else
                    {
                        // One roll partitioned so each chance is an absolute share of tiles.
                        float roll = rng.Value();
                        if (roll < t.restChance) tile.type = TileType.Rest;
                        else if (roll < t.restChance + gatherChance) tile.type = TileType.Gather;
                        else if (roll < t.restChance + gatherChance + eliteChance) MakeElite(tile, t, baseDiff, elitePool, rng);
                        else MakeMonster(tile, t, baseDiff, normalPool, rng);
                    }
                    run.tiles.Add(tile);
                }
            }

            EnsureMinimum(run, TileType.Gather, MinGatherTiles, rng);
            EnsureMinimum(run, TileType.Rest, MinRestTiles, rng);

            run.px = sx; run.py = sy;
            Reveal(run, sx, sy, 1);
            return run;
        }

        static void MakeMonster(Tile tile, TuningData t, int baseDiff, List<string> pool, Rng rng)
        {
            tile.type = TileType.Monster;
            tile.difficulty = baseDiff + rng.Range(t.monsterReqMin, Math.Max(t.monsterReqMin, t.monsterReqMax));
            tile.monsterId = pool.Count > 0 ? rng.Pick(pool) : null;
        }

        static void MakeElite(Tile tile, TuningData t, int baseDiff, List<string> pool, Rng rng)
        {
            tile.type = TileType.Elite;
            tile.difficulty = baseDiff + rng.Range(t.eliteReqMin, Math.Max(t.eliteReqMin, t.eliteReqMax));
            tile.monsterId = pool.Count > 0 ? rng.Pick(pool) : null;
        }

        // Convert random Monster tiles (then Elite tiles if needed) into `type` until `min` exist.
        static void EnsureMinimum(RunState run, TileType type, int min, Rng rng)
        {
            int have = 0;
            for (int i = 0; i < run.tiles.Count; i++) if (run.tiles[i].type == type) have++;
            if (have >= min) return;

            var candidates = new List<int>();
            for (int i = 0; i < run.tiles.Count; i++) if (run.tiles[i].type == TileType.Monster) candidates.Add(i);
            if (candidates.Count < min - have)
                for (int i = 0; i < run.tiles.Count; i++) if (run.tiles[i].type == TileType.Elite) candidates.Add(i);

            while (have < min && candidates.Count > 0)
            {
                int k = rng.Range(0, candidates.Count - 1);
                Tile tile = run.tiles[candidates[k]];
                candidates.RemoveAt(k);
                tile.type = type;
                tile.monsterId = null;
                have++;
            }
        }

        // ───────────────────────── Movement & fog ─────────────────────────
        public static bool CanMoveTo(RunState run, int x, int y)
        {
            if (run == null || run.fight != null || run.gather != null) return false;
            if (run.outcome != RunOutcome.None) return false;
            if (!run.InBounds(x, y)) return false;
            int d = Math.Abs(x - run.px) + Math.Abs(y - run.py);
            return d == 1;
        }

        public static List<GameEvent> MoveTo(RunState run, int x, int y)
        {
            var events = new List<GameEvent>();
            if (!CanMoveTo(run, x, y)) return events;
            run.px = x; run.py = y;
            RevealInternal(run, x, y, 1, events);
            return events;
        }

        public static void Reveal(RunState run, int x, int y, int radius)
        {
            RevealInternal(run, x, y, radius, null);
        }

        // Square (Chebyshev) reveal. Emits TileRevealed (amount = tile index, text = "x,y") for newly revealed tiles.
        static void RevealInternal(RunState run, int cx, int cy, int radius, List<GameEvent> events)
        {
            if (radius < 0) radius = 0;
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    if (!run.InBounds(x, y)) continue;
                    Tile tile = run.TileAt(x, y);
                    if (tile.revealed) continue;
                    tile.revealed = true;
                    if (events != null)
                        events.Add(new GameEvent(GameEventType.TileRevealed, y * run.width + x, false, x + "," + y));
                }
            }
        }

        // ───────────────────────── Gather minigame ─────────────────────────
        public static GatherState StartGather(GameContent c, RunState run, PlayerStats stats, Rng rng)
        {
            TuningData t = c.tuning;
            float statMult = (stats != null && stats.gatherZoneMult > 0f) ? stats.gatherZoneMult : 1f;
            ClassData cls = c.Class(run.classId);
            float clsMult = (cls != null && cls.gatherZoneMult > 0f) ? cls.gatherZoneMult : 1f;

            float safe = Clamp(t.gatherSafeWidth * statMult, 0f, 1f);
            float perfect = Clamp(t.gatherPerfectWidth * clsMult * statMult, 0f, safe);

            var g = new GatherState();
            g.safeWidth = safe;
            g.perfectWidth = perfect;
            float lo = safe / 2f, hi = 1f - safe / 2f;
            g.safeCenter = hi > lo ? lo + rng.Value() * (hi - lo) : 0.5f;
            g.barSpeed = t.gatherBarSpeed;

            RouteData route = c.Route(run.routeId);
            if (route != null && route.gatherIngredientIds.Count > 0) g.ingredientId = rng.Pick(route.gatherIngredientIds);
            else if (c.ingredients.Count > 0) g.ingredientId = rng.Pick(c.ingredients).id;
            return g;
        }

        public static GatherGrade EvaluateGather(GatherState g, float marker01)
        {
            float d = Math.Abs(marker01 - g.safeCenter);
            const float eps = 1e-5f;
            if (d <= g.perfectWidth / 2f + eps) return GatherGrade.Perfect;
            if (d <= g.safeWidth / 2f + eps) return GatherGrade.Safe;
            return GatherGrade.Miss;
        }

        public static int GatherYield(TuningData t, GatherGrade grade, RouteMod mod)
        {
            int n;
            if (grade == GatherGrade.Perfect) n = t.gatherYieldPerfect;
            else if (grade == GatherGrade.Safe) n = t.gatherYieldSafe;
            else n = 0;
            if (mod == RouteMod.Lucky) n *= 2;
            return n;
        }

        // Triangle wave 0→1→0; barSpeed = bar-lengths per second (one full sweep 0→1 takes 1/barSpeed s).
        public static float MarkerAt(GatherState g, float timeSeconds)
        {
            float speed = g.barSpeed > 0f ? g.barSpeed : 1f;
            double phase = (double)Math.Abs(timeSeconds) * speed;
            phase = phase - 2.0 * Math.Floor(phase / 2.0); // [0,2)
            double v = phase <= 1.0 ? phase : 2.0 - phase;
            return (float)v;
        }

        // ───────────────────────── Rest ─────────────────────────
        // Heals restHealPct of max HP (rounded, min 1), clamped to max. Returns HP actually healed.
        public static int ApplyRest(GameContent c, RunState run, PlayerStats stats)
        {
            int max = stats != null ? stats.maxHp : run.hp;
            int heal = (int)Math.Round(max * c.tuning.restHealPct, MidpointRounding.AwayFromZero);
            if (heal < 1) heal = 1;
            int before = run.hp;
            run.hp = Math.Min(max, run.hp + heal);
            if (run.hp < before) run.hp = before; // never reduce hp if already above max
            return run.hp - before;
        }

        static float Clamp(float v, float lo, float hi) { return v < lo ? lo : (v > hi ? hi : v); }
    }
}
