// Hearth & Hollow — ticket 53: a balance simulator. It "plays" each slice area in code, floor by floor, with the game's
// real numbers (your stats with a tray, every area's monsters and their attack timings from the Dungeon Settings,
// rooms per floor, healing, the Cold drain, elements) and writes a report: how far you get, HP left, deaths.
// It's a model, not the game: positioning and dodging are reduced to a few assumptions, listed at the top of the report.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HearthHollow.Core;
using HearthHollow.Dungeon;
using NUnit.Framework;
using UnityEditor;

namespace HearthHollow.Tests
{
    public static class BalanceSim
    {
        // ── assumptions (the bits a real fight decides by skill) ──
        public const float SwingsLanded = 0.85f;     // your swings that connect
        public const float Cleave = 1.3f;            // enemies an average swing hits
        public const float MonsterHitsLanded = 0.5f; // monster attacks that reach you (you move, dodge, kite)
        public const int MaxAttackers = 3;           // monsters on you at once
        public const float FightRoomShare = 0.7f;    // the rest are forage / campfire / start rooms
        public const int Trials = 300;

        public class Tray { public string name; public List<CookedMeal> meals = new List<CookedMeal>(); }

        public class AreaResult
        {
            public string area, tray; public int floors;
            public float deathRate; public int[] diedOnFloor; public float[] hpLeftPct; public float avgMinutes;
        }

        public static List<Tray> Trays()
        {
            Func<int, string, Tray> t = (g, n) => new Tray { name = n, meals = {
                new CookedMeal { recipeId = Ids.Recipe.HerbStew, grade = g }, new CookedMeal { recipeId = Ids.Recipe.MushroomSkewer, grade = g }, new CookedMeal { recipeId = Ids.Recipe.HerbButty, grade = g } } };
            var fire = t(1, "Fine + 🔥 Fire"); fire.meals[1].accelId = Ids.Ingredient.Chili;
            var warm = t(1, "Fine + 🔥 Fire + 🫚 Ginger"); warm.meals[1].accelId = Ids.Ingredient.Chili; warm.meals[0].accelId = Ids.Ingredient.Ginger;
            return new List<Tray> { new Tray { name = "Hungry" }, t(0, "Plain"), t(1, "Fine"), t(2, "Prime"), fire, warm };
        }

        public static AreaResult Run(GameContent c, DungeonSettings ds, string routeId, Tray tray, int seed)
        {
            var p = Economy.NewProfile(c);
            p.selectedClass = ClassId.Cook; p.arcStage = 2;
            var eaten = Cooking.ToEaten(c, tray.meals);
            var st = StatCalc.ComputeWith(c, p, ClassId.Cook, eaten);
            var mods = new CombatMods();
            Cooking.AddMealMods(c, c.Class(ClassId.Cook), eaten, mods);
            var infusions = Cooking.Infusions(c, eaten);
            var guards = Cooking.Guards(c, eaten);
            var route = c.Route(routeId);
            var atk = c.Move("c_ladle");
            float swing = atk != null && atk.cooldown > 0f ? atk.cooldown : 0.5f;
            float power = atk != null && atk.power > 0f ? atk.power : 1f;
            int floors = DungeonRules.Floors(c, routeId);
            float mitig = Routes.Mitigation(c, eaten, route != null ? route.hazard : Hazard.None);

            var res = new AreaResult { area = route != null ? route.name : routeId, tray = tray.name, floors = floors, diedOnFloor = new int[floors + 1], hpLeftPct = new float[floors + 1] };
            var reached = new int[floors + 1];
            var rng = new Random(seed);
            float totalTime = 0f; int deaths = 0;
            for (int trial = 0; trial < Trials; trial++)
            {
                float hp = st.maxHp, time = 0f; bool dead = false;
                for (int floor = 1; floor <= floors && !dead; floor++)
                {
                    var run = new RunState { routeId = routeId, floor = floor, floorsTotal = floors, dungeon = true, infusions = infusions };
                    var pool = DungeonRules.NormalPool(c, run);
                    int rooms = c.tuning.roomsPerFloorMin + rng.Next(Math.Max(1, c.tuning.roomsPerFloorMax - c.tuning.roomsPerFloorMin + 1)) + DungeonRules.ExtraRooms(floor);
                    int fights = Math.Max(2, (int)Math.Round(rooms * FightRoomShare));
                    bool boss = DungeonRules.IsBossFloor(c, routeId, floor);
                    for (int room = 0; room < fights && !dead; room++)
                    {
                        bool bossRoom = boss && room == fights - 1;
                        var foes = new List<EnemyStats>();
                        if (bossRoom) { foes.Add(DungeonRules.Enemy(c, run, DungeonRules.BossMonster(c, run), true, true)); for (int i = 0; i < 2; i++) foes.Add(DungeonRules.Enemy(c, run, Pick(pool, rng), false, false)); }
                        else
                        {
                            int n = DungeonRules.EnemyCount(c.tuning, floor, new Rng(rng.Next()));
                            if (route != null && route.enemyMult > 0f && route.enemyMult < 1f) n = Math.Max(1, (int)Math.Round(n * route.enemyMult));
                            if (rng.NextDouble() < c.tuning.dungeonEliteChance) { foes.Add(DungeonRules.Enemy(c, run, Pick(pool, rng), true, false)); n--; }
                            for (int i = 0; i < n; i++) foes.Add(DungeonRules.Enemy(c, run, Pick(pool, rng), false, false));
                        }
                        dead = Fight(c, ds, st, mods, infusions, guards, power, swing, foes, ref hp, ref time, rng);
                        if (!dead) hp = Math.Min(st.maxHp, hp + Math.Max(1f, st.maxHp * c.tuning.roomClearHealPct));   // ticket 39
                        // the Cold / Poison drain while you walk the floor (a tick every hazardTickSeconds)
                        if (!dead && route != null && route.hazard != Hazard.None && route.hazardPct > 0f)
                        {
                            float walk = 6f;
                            hp -= st.maxHp * route.hazardPct * (1f - mitig) * (walk / Math.Max(0.5f, c.tuning.hazardTickSeconds));
                            time += walk;
                            if (hp <= 0f) dead = true;
                        }
                    }
                    if (dead) { res.diedOnFloor[floor]++; deaths++; }
                    else { reached[floor]++; res.hpLeftPct[floor] += hp / st.maxHp; }
                }
                totalTime += time;
            }
            for (int f = 1; f <= floors; f++) res.hpLeftPct[f] = reached[f] > 0 ? res.hpLeftPct[f] / reached[f] : 0f;
            res.deathRate = deaths / (float)Trials;
            res.avgMinutes = totalTime / Trials / 60f;
            return res;
        }

        static string Pick(List<string> pool, Random rng) { return pool.Count > 0 ? pool[rng.Next(pool.Count)] : Ids.Monster.Slime; }

        /// One room: you whittle them down one at a time; up to MaxAttackers hit back. True if you fall.
        static bool Fight(GameContent c, DungeonSettings ds, PlayerStats st, CombatMods mods, List<Element> infusions, List<Element> guards,
                          float power, float swing, List<EnemyStats> foes, ref float hp, ref float time, Random rng)
        {
            var hpLeft = new List<float>(); foreach (var f in foes) hpLeft.Add(f.maxHp);
            float dt = 0.1f, swingT = 0f, regenT = 0f;
            var atkT = new float[foes.Count];
            for (int i = 0; i < foes.Count; i++) atkT[i] = (float)rng.NextDouble() * 1.2f;
            while (true)
            {
                int alive = 0; foreach (var h in hpLeft) if (h > 0f) alive++;
                if (alive == 0) return false;
                time += dt;
                // you
                swingT -= dt;
                if (swingT <= 0f)
                {
                    swingT = swing;
                    if (rng.NextDouble() < SwingsLanded)
                    {
                        bool crit = rng.NextDouble() < st.critChance;
                        float dmg = st.damage * power * (crit ? c.tuning.critMult : 1f);
                        float spread = Cleave;
                        for (int i = 0; i < hpLeft.Count && spread > 0f; i++)
                        {
                            if (hpLeft[i] <= 0f) continue;
                            var prof = c.Monster(foes[i].monsterId);
                            var weak = new List<Element>(); var res = new List<Element>();
                            if (prof != null) { if (prof.weakTo != null) weak.AddRange(prof.weakTo); if (prof.resists != null) res.AddRange(prof.resists); if (weak.Count == 0 && res.Count == 0) Elements.Defaults(prof.element, weak, res); }
                            bool cancel; float em = Elements.Multiplier(infusions, weak, res, out cancel);
                            float share = Math.Min(1f, spread);
                            hpLeft[i] -= dmg * em * share;
                            spread -= 1f;
                            if (hpLeft[i] <= 0f && mods.healOnKill > 0) hp = Math.Min(st.maxHp, hp + mods.healOnKill);
                        }
                    }
                }
                // meals that heal over time (ticket 38)
                if (mods.regen > 0f) { regenT += dt; if (regenT >= 10f) { regenT -= 10f; hp = Math.Min(st.maxHp, hp + mods.regen); } }
                // them
                int attackers = 0;
                for (int i = 0; i < foes.Count; i++)
                {
                    if (hpLeft[i] <= 0f) continue;
                    if (attackers++ >= MaxAttackers) continue;
                    var prof = c.Monster(foes[i].monsterId);
                    atkT[i] -= dt;
                    if (atkT[i] > 0f) continue;
                    atkT[i] = c.tuning.monsterAttackCooldown * (prof != null ? prof.cooldownMult : 1f) * (0.85f + (float)rng.NextDouble() * 0.35f) + (prof != null ? prof.windup : 0.4f);
                    int hits = prof != null ? Math.Max(1, prof.hits) : 1;
                    for (int h = 0; h < hits; h++)
                    {
                        if (rng.NextDouble() > MonsterHitsLanded) continue;
                        float guard = prof != null && guards.Contains(prof.element) ? 0.5f : 1f;
                        hp -= DungeonRules.IncomingDamage(foes[i].damage, st, guard);
                    }
                    if (hp <= 0f) return true;
                }
                if (time > 60f * 60f) return false;   // safety
            }
        }

        public static string Report(GameContent c, DungeonSettings ds)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Balance report (simulated)");
            sb.AppendLine();
            sb.AppendLine("Generated by `BalanceSimTests` (ticket 53) from the game's real numbers: content v" + c.contentVersion + ". Re-run the test after any tuning change.");
            sb.AppendLine();
            sb.AppendLine("**Assumptions** (a real fight decides these by skill): " + (int)(SwingsLanded * 100) + "% of your swings land, an average swing hits " + Cleave + " enemies, " + (int)(MonsterHitsLanded * 100) + "% of monster attacks reach you, at most " + MaxAttackers + " monsters on you at once, " + (int)(FightRoomShare * 100) + "% of rooms are fights, " + Trials + " runs per row. The Cook with starting gear.");
            sb.AppendLine();
            sb.AppendLine("**Trays:** Hungry = no meals. Plain / Fine / Prime = Herb Stew + Mushroom Skewer + Herb Butty at that quality. Fine + 🔥 Fire = the Fine tray with a chili on the skewer.");
            sb.AppendLine();
            var p = Economy.NewProfile(c); p.selectedClass = ClassId.Cook;
            sb.AppendLine("| Tray | Attack | HP |"); sb.AppendLine("|---|---|---|");
            foreach (var t in Trays()) { var s = StatCalc.ComputeWith(c, p, ClassId.Cook, Cooking.ToEaten(c, t.meals)); sb.AppendLine("| " + t.name + " | " + s.damage.ToString("0.#") + " | " + s.maxHp + " |"); }
            sb.AppendLine();
            foreach (var area in Slice.RouteIds)
            {
                var r0 = c.Route(area);
                sb.AppendLine("## " + (r0 != null ? r0.emoji + " " + r0.name : area) + "  (" + DungeonRules.Floors(c, area) + " floors" + (r0 != null && r0.hazard != Hazard.None ? ", " + r0.hazard : "") + ")");
                sb.AppendLine();
                int floors = DungeonRules.Floors(c, area);
                var head = new StringBuilder("| Tray | Dies | ");
                var line = new StringBuilder("|---|---|");
                for (int f = 1; f <= floors; f++) { head.Append("Floor " + f + (DungeonRules.IsBossFloor(c, area, f) ? " (boss)" : "") + " | "); line.Append("---|"); }
                head.Append("Minutes |"); line.Append("---|");
                sb.AppendLine(head.ToString()); sb.AppendLine(line.ToString());
                int seed = 1;
                foreach (var t in Trays())
                {
                    var res = Run(c, ds, area, t, 1000 + seed++);
                    var row = new StringBuilder("| " + t.name + " | " + (int)Math.Round(res.deathRate * 100) + "% | ");
                    for (int f = 1; f <= floors; f++)
                    {
                        int diedHere = (int)Math.Round(res.diedOnFloor[f] * 100f / Trials);
                        row.Append(res.hpLeftPct[f] > 0f ? (int)Math.Round(res.hpLeftPct[f] * 100) + "% HP left" : "—");
                        if (diedHere > 0) row.Append(" · " + diedHere + "% die");
                        row.Append(" | ");
                    }
                    row.Append(res.avgMinutes.ToString("0.0") + " |");
                    sb.AppendLine(row.ToString());
                }
                sb.AppendLine();
            }
            sb.AppendLine("## How to read it");
            sb.AppendLine();
            sb.AppendLine("The plan's targets (ticket 44): **hungry loses by Meadow floor 2**, a **Plain** tray is a fair fight (gets through, but hurt), **Prime** dominates, and **Frost Hollow** wants a Fine tray with Fire.");
            return sb.ToString();
        }
    }

    public class BalanceSimTests
    {
        [Test]
        public void WriteTheBalanceReport()
        {
            var c = DefaultContent.Create();
            var ds = AssetDatabase.LoadAssetAtPath<DungeonSettings>("Assets/HearthHollow/Dungeon/DungeonSettings.asset");
            Assert.IsNotNull(ds);
            string md = BalanceSim.Report(c, ds);
            string path = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", ".scratch", "vertical-slice", "balance-report.md"));
            File.WriteAllText(path, md);
            UnityEngine.Debug.Log("[Balance] report written to " + path);
            // sanity: food matters
            var hungry = BalanceSim.Run(c, ds, Ids.Area.Meadow, BalanceSim.Trays()[0], 7);
            var prime = BalanceSim.Run(c, ds, Ids.Area.Meadow, BalanceSim.Trays()[3], 7);
            Assert.GreaterOrEqual(hungry.deathRate, prime.deathRate, "a Prime tray never does worse than going hungry");
        }
    }
}
