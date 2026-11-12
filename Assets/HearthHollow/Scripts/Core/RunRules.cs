// Hearth & Hollow — the rules of a run that aren't about the dungeon's rooms: setting out (the RunState), the
// forage minigame, resting, armour, and what a kill is worth. Pure C#, no UnityEngine.
// (Refactor 02: what the dungeon still used from the old fog grid's Explore and Combat.)
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public static class RunRules
    {
        // ───────────────────────── Run creation ─────────────────────────
        public static RunState CreateRun(GameContent c, Profile p, RouteOffer offer, bool storm)
        {
            var run = new RunState();
            run.routeId = offer.routeId;
            run.mod = offer.mod;
            run.storm = storm;
            run.classId = p.selectedClass;
            run.hp = 0; // session sets from stats
            run.tray = new List<string>(p.tray);
            run.outcome = RunOutcome.None;
            int slots = Economy.BagSlots(c, p);
            run.bag = new List<BagSlot>(slots);
            for (int i = 0; i < slots; i++) run.bag.Add(new BagSlot { ingredientId = "", count = 0 });
            return run;
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

        // ───────────────────────── Armour ─────────────────────────
        public static float ApplyArmour(float raw, float armourPct)
        {
            float a = armourPct;
            if (a < 0f) a = 0f;
            if (a > 1f) a = 1f;
            return raw * (1f - a);
        }

        // ───────────────────────── Rewards ─────────────────────────
        public static int KillCoins(GameContent c, RunState run, PlayerStats stats, bool elite, int difficulty)
        {
            TuningData t = c.tuning;
            RouteData route = run != null ? c.Route(run.routeId) : null;
            float v = t.killCoinBase + difficulty * t.killCoinPerDiff;
            v *= route != null ? route.coinMult : 1f;
            v *= stats != null ? stats.coinMult : 1f;
            if (elite) v *= t.eliteRewardMult;
            RouteMod mod = run != null ? run.mod : RouteMod.None;
            if (mod == RouteMod.Rich) v *= 1.5f;
            if (mod == RouteMod.Dangerous) v *= 1.5f;
            return Math.Max(0, (int)Math.Round(v, MidpointRounding.AwayFromZero));
        }

        public static int KillXp(GameContent c, RunState run, bool elite, int difficulty)
        {
            TuningData t = c.tuning;
            float v = t.killXpBase + difficulty * t.killXpPerDiff;
            if (elite) v *= t.eliteRewardMult;
            RouteMod mod = run != null ? run.mod : RouteMod.None;
            if (mod == RouteMod.Double) v *= 2f;
            if (mod == RouteMod.Dangerous) v *= 1.5f;
            return Math.Max(0, (int)Math.Round(v, MidpointRounding.AwayFromZero));
        }

        static float Clamp(float v, float lo, float hi) { return v < lo ? lo : (v > hi ? hi : v); }
    }
}
