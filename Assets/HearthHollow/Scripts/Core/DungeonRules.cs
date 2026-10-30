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
                foreach (var id in route.monsterIds) { var m = c.Monster(id); if (m != null && !m.eliteOnly) list.Add(id); }
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
            if (list.Count == 0) list.AddRange(NormalPool(c, run));
            return list;
        }

        /// The final floor's guardian: the route's elite-only monster if it has one, else its last (toughest) monster.
        public static string BossMonster(GameContent c, RunState run)
        {
            RouteData route = c.Route(run.routeId);
            if (route == null || route.monsterIds.Count == 0) { var pool = NormalPool(c, run); return pool.Count > 0 ? pool[pool.Count - 1] : null; }
            for (int i = route.monsterIds.Count - 1; i >= 0; i--) { var m = c.Monster(route.monsterIds[i]); if (m != null && m.eliteOnly) return m.id; }
            return route.monsterIds[route.monsterIds.Count - 1];
        }

        /// How many enemies an event room spawns on this floor.
        public static int EnemyCount(TuningData t, int floor, Rng rng)
        {
            return rng.Range(Math.Min(t.enemiesPerRoomMin, t.enemiesPerRoomMax), Math.Max(t.enemiesPerRoomMin, t.enemiesPerRoomMax)) + Math.Max(0, floor - 1);
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
