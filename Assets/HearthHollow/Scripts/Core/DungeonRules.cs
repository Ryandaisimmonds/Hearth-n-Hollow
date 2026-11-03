// Hearth & Hollow — rules for the real-time dungeon (rooms, floors, enemies). Pure C#, no UnityEngine.
// The Unity side (Scripts/Dungeon) owns positions, physics and timing; everything that touches
// progress — damage numbers, rewards, loot, HP, floors, run end — goes through these rules and GameSession.
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public partial class RunState
    {
        public bool dungeon;              // true = real-time rooms; false = the old fog grid (headless sim)
        public int floor = 1;             // 1-based
        public int floorsTotal = 3;
        public int dungeonSeed;           // the Unity generator seeds each floor from this + floor
    }

    /// Stats for one spawned enemy (difficulty-scaled, same formulas as the old turn-based fights).
    [Serializable] public class EnemyStats
    {
        public string monsterId; public bool elite; public bool boss;
        public int difficulty; public int maxHp; public float damage;
    }

    public static class DungeonRules
    {
        const float EliteHpMult = 1.5f, EliteAtkMult = 1.25f;

        /// Route base + one per floor below the first + one when Dangerous / stormy.
        public static int Difficulty(GameContent c, RunState run)
        {
            RouteData route = c.Route(run.routeId);
            int d = (route != null ? route.baseDifficulty : 0) + Math.Max(0, run.floor - 1);
            if (run.storm || run.mod == RouteMod.Dangerous) d += 1;
            return d;
        }

        public static EnemyStats Enemy(GameContent c, RunState run, string monsterId, bool elite, bool boss)
        {
            TuningData t = c.tuning;
            MonsterData m = c.Monster(monsterId);
            int diff = Difficulty(c, run);
            float hp = (t.monsterHpBase + diff * t.monsterHpPerDiff) * (m != null ? m.hpMult : 1f);
            float atk = t.monsterAtkBase + diff * t.monsterAtkPerDiff;
            if (elite || boss) { hp *= EliteHpMult; atk *= EliteAtkMult; }
            if (boss) { hp *= t.bossHpMult; atk *= t.bossAtkMult; }
            if (Routes.Has(c.Route(run.routeId), Mutator.Giants)) hp *= t.giantHp;
            if (Routes.Has(c.Route(run.routeId), Mutator.Swarm) && !boss) hp *= t.swarmHp;
            return new EnemyStats
            {
                monsterId = monsterId, elite = elite || boss, boss = boss, difficulty = diff,
                maxHp = Math.Max(1, (int)Math.Round(hp, MidpointRounding.AwayFromZero)),
                damage = atk
            };
        }

        /// Monsters that can appear as ordinary enemies on this route.
        public static List<string> NormalPool(GameContent c, RunState run)
        {
            var list = new List<string>();
            RouteData route = c.Route(run.routeId);
            if (route != null)
            {
                foreach (var id in route.monsterIds) { var m = c.Monster(id); if (m != null && !m.eliteOnly) list.Add(id); }
                foreach (var id in DeepMonsters(c, route, run.floor)) { var m = c.Monster(id); if (m != null && !m.eliteOnly && !list.Contains(id)) list.Add(id); }
            }
            if (list.Count == 0) foreach (var m in c.monsters) if (!m.eliteOnly) list.Add(m.id);
            if (list.Count == 0 && c.monsters.Count > 0) list.Add(c.monsters[0].id);
            return list;
        }

        /// Any route monster can be an elite (elite-only ones such as the drake included).
        public static List<string> ElitePool(GameContent c, RunState run)
        {
            var list = new List<string>();
            RouteData route = c.Route(run.routeId);
            if (route != null) foreach (var id in route.monsterIds) if (c.Monster(id) != null) list.Add(id);
            if (route != null) foreach (var id in DeepMonsters(c, route, run.floor)) if (c.Monster(id) != null && !list.Contains(id)) list.Add(id);
            if (list.Count == 0) list.AddRange(NormalPool(c, run));
            return list;
        }

        /// Monsters that join this route from this floor down (ticket 13).
        public static List<string> DeepMonsters(GameContent c, RouteData route, int floor)
        {
            var list = new List<string>();
            if (route != null && route.floorMonsters != null)
                foreach (var fm in route.floorMonsters) if (fm != null && floor >= fm.minFloor && !list.Contains(fm.monsterId)) list.Add(fm.monsterId);
            return list;
        }

        /// One ingredient this route gives on this floor: a gather ingredient (weight 1 each) or a floor drop it has reached.
        public static string PickIngredient(GameContent c, RouteData route, int floor, float roll01)
        {
            if (route == null) return null;
            var ids = new List<string>(); var w = new List<float>();
            foreach (var id in route.gatherIngredientIds) { ids.Add(id); w.Add(1f); }
            if (route.floorDrops != null)
                foreach (var d in route.floorDrops)
                    if (d != null && floor >= d.minFloor && c.Ingredient(d.ingredientId) != null && d.weight > 0f) { ids.Add(d.ingredientId); w.Add(d.weight); }
            if (ids.Count == 0) return null;
            float total = 0f; foreach (var x in w) total += x;
            float r = Math.Max(0f, Math.Min(0.99999f, roll01)) * total;
            for (int i = 0; i < ids.Count; i++) { if (r < w[i]) return ids[i]; r -= w[i]; }
            return ids[ids.Count - 1];
        }

        // ───────────── deep dungeons (Design v2, ticket 12) ─────────────
        public static int Floors(GameContent c, string routeId)
        {
            RouteData r = c.Route(routeId);
            return r != null && r.floors > 0 ? r.floors : Math.Max(1, c.tuning.dungeonFloors);
        }

        /// A boss every <c>bossEvery</c> floors, and always on the last floor.
        public static bool IsBossFloor(GameContent c, string routeId, int floor)
        {
            if (floor >= Floors(c, routeId)) return true;
            RouteData r = c.Route(routeId);
            int every = r != null ? r.bossEvery : 0;
            return every > 0 && floor > 0 && floor % every == 0;
        }

        /// The go-deeper-or-home choice (and an exit) comes every other floor — odd floors — and after every boss.
        /// On the floors between, the ladder just goes down.
        public static bool HomeChoiceOn(GameContent c, string routeId, int floor)
        {
            return floor % 2 == 1 || IsBossFloor(c, routeId, floor);
        }

        /// Floors a run can start from: 1, plus the floor after each boss you've beaten (the shortcuts).
        public static List<int> StartFloors(GameContent c, Profile p, string routeId)
        {
            var list = new List<int> { 1 };
            int n = Floors(c, routeId);
            for (int f = 1; f < n; f++)
                if (IsBossFloor(c, routeId, f) && Progression.BeatBoss(p, routeId, f) && !list.Contains(f + 1)) list.Add(f + 1);
            return list;
        }

        /// Extra rooms and enemies grow slowly with depth (capped) so floor 10 isn't a slog.
        public static int ExtraRooms(int floor) { return Math.Min(3, Math.Max(0, floor - 1)); }
        public static int ExtraEnemies(int floor) { return Math.Min(4, Math.Max(0, floor - 1) / 2); }

        /// This floor's guardian: the route's bossIds entry for it if set, else the route's elite-only monster, else its toughest.
        public static string BossMonster(GameContent c, RunState run)
        {
            RouteData route = c.Route(run.routeId);
            if (route != null && route.bossIds != null && route.bossIds.Count > 0)
            {
                int k = route.bossEvery > 0 ? Math.Max(1, run.floor / route.bossEvery) - 1 : route.bossIds.Count - 1;
                if (run.floor >= Floors(c, run.routeId)) k = route.bossIds.Count - 1;
                k = Math.Min(k, route.bossIds.Count - 1);
                if (c.Monster(route.bossIds[k]) != null) return route.bossIds[k];
            }
            if (route == null || route.monsterIds.Count == 0) { var pool = NormalPool(c, run); return pool.Count > 0 ? pool[pool.Count - 1] : null; }
            for (int i = route.monsterIds.Count - 1; i >= 0; i--) { var m = c.Monster(route.monsterIds[i]); if (m != null && m.eliteOnly) return m.id; }
            return route.monsterIds[route.monsterIds.Count - 1];
        }

        /// How many enemies an event room spawns on this floor.
        public static int EnemyCount(TuningData t, int floor, Rng rng)
        {
            return rng.Range(Math.Min(t.enemiesPerRoomMin, t.enemiesPerRoomMax), Math.Max(t.enemiesPerRoomMin, t.enemiesPerRoomMax)) + ExtraEnemies(floor);
        }

        /// One player hit: stat damage × move multiplier, with a crit roll.
        public static float PlayerHitDamage(TuningData t, PlayerStats s, float mult, Rng rng, out bool crit)
        {
            float dmg = (s != null ? s.damage : 1f) * mult;
            crit = rng.Chance(s != null ? s.critChance : 0f);
            if (crit) dmg *= t.critMult;
            return dmg;
        }

        /// Damage the player actually takes from a raw monster hit (armour, then an optional guard).
        public static int IncomingDamage(float raw, PlayerStats s, float extraMult)
        {
            float v = Combat.ApplyArmour(raw, s != null ? s.armourPct : 0f) * extraMult;
            int d = (int)Math.Round(v, MidpointRounding.AwayFromZero);
            return d < 1 ? 1 : d;
        }

        /// Share of the route's clear bonus earned by taking an exit on <paramref name="floor"/>.
        public static float ClearShare(RunState run)
        {
            if (run.outcome == RunOutcome.Cleared) return 1f;
            if (run.outcome != RunOutcome.Returned) return 0f;
            int total = Math.Max(1, run.floorsTotal);
            return Math.Min(1f, Math.Max(0, run.floor) / (float)total);
        }
    }
}
