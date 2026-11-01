// Hearth & Hollow — GameSession API for the real-time dungeon. Called by Scripts/Dungeon (Unity side).
// Pure C#. Flow: StartRun → Phase.Dungeon (floor 1) → rooms/events → ladder (floor+1) or exit → RunEnd.
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public partial class GameSession
    {
        /// Real-time dungeon runs (default). Set false to play the old fog grid (the headless sim does).
        public bool UseDungeon = true;

        public bool InDungeon { get { return Phase == Phase.Dungeon && Run != null && Run.dungeon; } }

        /// Damage multiplier on incoming hits while the Cook's Hearty Snack guard is up (set by the Unity side).
        public float DungeonGuardMult = 1f;

        private void BeginDungeon()
        {
            Run.dungeon = true;
            Run.floor = 1;
            Run.floorsTotal = Math.Max(1, T.dungeonFloors);
            Run.dungeonSeed = _rng.Range(1, int.MaxValue - 1);
            Run.tiles.Clear();          // no fog grid in the dungeon
            DungeonGuardMult = 1f;
            Phase = Phase.Dungeon;
        }

        // ───────────── queries ─────────────
        public int DungeonDifficulty { get { return Run != null ? DungeonRules.Difficulty(Content, Run) : 0; } }
        public bool DungeonOnLastFloor { get { return Run != null && Run.floor >= Run.floorsTotal; } }

        public EnemyStats DungeonEnemy(string monsterId, bool elite, bool boss) { return DungeonRules.Enemy(Content, Run, monsterId, elite, boss); }

        /// One player hit (basic attack or special) — stat damage × <paramref name="mult"/>, rolled for crit.
        public float DungeonRollDamage(float mult, out bool crit)
        {
            if (DungeonHas(Mutator.GlassHearts)) mult *= T.glassMult;
            return DungeonRules.PlayerHitDamage(T, Stats, mult, _rng, out crit);
        }

        public bool DungeonSkillUnlocked
        {
            get { return Run != null && Profile.Progress(Run.classId).level >= T.skillUnlockLevel; }
        }

        // ───────────── player ─────────────
        /// A monster hit the player for <paramref name="raw"/>. Returns damage taken; ends the run at 0 HP.
        public int DungeonDamagePlayer(float raw)
        {
            if (!InDungeon) return 0;
            if (DungeonHas(Mutator.GlassHearts)) raw *= T.glassMult;
            int dmg = DungeonRules.IncomingDamage(raw, Stats, DungeonGuardMult);
            Run.hp -= dmg;
            Emit(new GameEvent(GameEventType.MonsterHit, dmg, true, "-" + dmg));
            if (Run.hp <= 0)
            {
                Run.hp = 0;
                Emit(new GameEvent(GameEventType.PlayerDied, 0, true, "You fall…"));
                Info("You limp home…");
                EndRun(RunOutcome.Died);
                return dmg;
            }
            Touch(false);
            return dmg;
        }

        public int DungeonHealPlayer(int amount)
        {
            if (!InDungeon || amount <= 0) return 0;
            int before = Run.hp;
            Run.hp = Math.Min(Stats.maxHp, Run.hp + amount);
            int healed = Run.hp - before;
            if (healed > 0) Emit(new GameEvent(GameEventType.PlayerHeal, healed, true, "+" + healed));
            Touch(false);
            return healed;
        }

        // ───────────── hazards (poison / cold / heat) ─────────────
        public Hazard DungeonHazard
        {
            get { var r = Run != null ? Content.Route(Run.routeId) : null; return Routes.HazardActive(r) ? r.hazard : Hazard.None; }
        }

        /// 0..1 — how much the eaten counter meals soften this run's hazard (1 = blocked).
        public float DungeonHazardMitigation { get { return Run == null ? 0f : Routes.Mitigation(Content, Profile, Run.eaten, DungeonHazard); } }

        /// Cold: fraction of move speed lost right now (0 elsewhere).
        public float DungeonHazardSlow { get { return DungeonHazard == Hazard.Cold ? T.coldSlow * (1f - DungeonHazardMitigation) : 0f; } }

        /// Darkness: 0 = pitch black (no counter meal) … 1 = you can see fine. Always 1 elsewhere.
        public float DungeonLight { get { return DungeonHazard == Hazard.Darkness ? DungeonHazardMitigation : 1f; } }

        /// One hazard bite (call every TuningData.hazardTickSeconds). Never drops HP below 1. Returns HP lost.
        /// <paramref name="standingStill"/>: Heat bites harder (heatStillMult) when the player isn't moving.
        public int DungeonHazardTick(bool standingStill = false)
        {
            if (!InDungeon) return 0;
            Hazard h = DungeonHazard;
            if (h == Hazard.None || h == Hazard.Darkness) return 0;   // darkness hides things, it doesn't hurt
            var r = Content.Route(Run.routeId);
            float pct = r.hazardPct * (1f - DungeonHazardMitigation) * (1f + 0.25f * Math.Max(0, Run.floor - 1));   // deeper floors bite harder
            if (h == Hazard.Heat && standingStill) pct *= T.heatStillMult;
            if (pct <= 0f || Run.hp <= 1) return 0;
            int dmg = Math.Max(1, (int)Math.Round(Stats.maxHp * pct));
            dmg = Math.Min(dmg, Run.hp - 1);
            Run.hp -= dmg;
            Touch(false);
            return dmg;
        }

        // ───────────── mutators (fun dungeon rules) ─────────────
        RouteData RunRoute { get { return Run != null ? Content.Route(Run.routeId) : null; } }
        public bool DungeonHas(Mutator m) { return InDungeon && Routes.Has(RunRoute, m); }

        /// Walking speed for everyone (player and monsters).
        public float MutatorSpeed { get { return DungeonHas(Mutator.Frantic) ? T.franticSpeed : DungeonHas(Mutator.Swift) ? T.swiftSpeed : 1f; } }
        public float MutatorAreaDamage { get { return DungeonHas(Mutator.BigBangs) ? T.bigBangDamage : 1f; } }
        public float MutatorAreaRadius { get { return DungeonHas(Mutator.BigBangs) ? T.bigBangRadius : 1f; } }
        public float MutatorEnemyScale { get { return DungeonHas(Mutator.Giants) ? T.giantSize : 1f; } }
        public float MutatorEnemySpeed { get { return MutatorSpeed * (DungeonHas(Mutator.Giants) ? T.giantSpeed : 1f); } }
        public float MutatorEnemyCount { get { return DungeonHas(Mutator.Swarm) ? T.swarmCount : 1f; } }

        /// HUD badges for the active mutators, e.g. "💨 Swift  💥 Big Bangs".
        public string DungeonMutatorText
        {
            get
            {
                var r = RunRoute;
                if (!InDungeon || r == null || r.mutators == null || r.mutators.Count == 0) return null;
                var parts = new System.Collections.Generic.List<string>();
                foreach (var m in r.mutators) if (m != Mutator.None) parts.Add(Routes.MutatorEmoji(m) + " " + Routes.MutatorName(m));
                return parts.Count > 0 ? string.Join("  ", parts.ToArray()) : null;
            }
        }

        /// HUD badge, e.g. "☠️ Poison · halved".
        public string DungeonHazardText
        {
            get
            {
                Hazard h = DungeonHazard;
                if (h == Hazard.None) return null;
                float m = DungeonHazardMitigation;
                return Routes.HazardEmoji(h) + " " + Routes.HazardName(h) + (m >= 0.999f ? " · blocked" : m > 0f ? " · " + (int)Math.Round(m * 100) + "% less" : "");
            }
        }

        // ───────────── enemies & rooms ─────────────
        public void DungeonEnemyKilled(EnemyStats e)
        {
            if (!InDungeon || e == null) return;
            Run.kills++;
            var f = new FightState { monsterId = e.monsterId, elite = e.elite, difficulty = e.difficulty };
            int coins = Math.Max(1, (int)Math.Round(Combat.KillCoins(Content, Run, Stats, f) * T.dungeonKillCoinMult));
            int xp = Math.Max(1, (int)Math.Round(Combat.KillXp(Content, Run, f) * T.dungeonKillXpMult));
            if (e.boss) { coins = (int)Math.Round(coins * T.bossRewardMult); xp = (int)Math.Round(xp * T.bossRewardMult); }
            Run.coinsEarned += coins; Run.killXp += xp;
            Emit(new GameEvent(GameEventType.Coins, coins, false, "+" + coins));
            Emit(new GameEvent(GameEventType.Xp, xp, false, "+" + xp + " XP"));
            DropLoot(f);
            if (e.boss) { string ing = PickRouteIngredient(); if (ing != null) AddLoot(ing, 2, false, true); DropMaterial(false, true); }
            string trinket = Gear.RollDrop(Content, Profile, e.boss, e.elite, _rng);   // rare: kept straight away, even if you fall
            if (trinket != null && Gear.Give(Content, Profile, trinket))
            {
                if (Run.trinketsDropped == null) Run.trinketsDropped = new List<string>();
                Run.trinketsDropped.Add(trinket);
                var td = Gear.Trinket(Content, trinket);
                Emit(new GameEvent(GameEventType.Loot, 0, true, "Trinket! " + td.emoji + " " + td.name));
            }
            int heal = Mods.healOnKill;   // skill tree: Second Wind / Leftovers
            if (heal > 0 && Run.hp > 0 && Run.hp < Stats.maxHp) DungeonHealPlayer(heal);
            Touch(false);
        }

        private string PickRouteIngredient()
        {
            var route = Content.Route(Run.routeId);
            return route != null && route.gatherIngredientIds.Count > 0 ? _rng.Pick(route.gatherIngredientIds) : null;
        }

        /// An event room's enemies are all down (its doors open again).
        public void DungeonRoomCleared()
        {
            if (!InDungeon) return;
            Run.roomsCleared++;
            Touch(false);
        }

        // ───────────── secrets (light plan step 7) ─────────────
        /// Hidden forage found by light: two of the route's ingredients, with elite odds of a good grade.
        public void DungeonHiddenForage()
        {
            if (!InDungeon) return;
            string ing = PickRouteIngredient();
            if (ing != null) AddLoot(ing, 2, true);
            Touch(false);
        }

        /// A secret chest: tier 1 behind a cracked wall, tier 2 for a brazier puzzle. Coins, ingredients, materials, a shot at a trinket.
        public void DungeonSecretChest(int tier)
        {
            if (!InDungeon) return;
            tier = Math.Max(1, tier);
            var pool = DungeonRules.NormalPool(Content, Run);
            int baseCoins = 10;
            if (pool.Count > 0) baseCoins = Combat.KillCoins(Content, Run, Stats, new FightState { monsterId = pool[0], elite = true, difficulty = DungeonDifficulty });
            int coins = Math.Max(5, (int)Math.Round(baseCoins * T.dungeonKillCoinMult * (1.5f + tier) * (Run.mod == RouteMod.Lucky ? 1.5f : 1f)));
            Run.coinsEarned += coins;
            Emit(new GameEvent(GameEventType.Coins, coins, false, "+" + coins));
            string ing = PickRouteIngredient();
            if (ing != null) AddLoot(ing, 1 + tier, true, tier >= 2);
            DropMaterial(true, false);
            string trinket = Gear.RollDrop(Content, Profile, tier >= 2, true, _rng);
            if (trinket != null && Gear.Give(Content, Profile, trinket))
            {
                if (Run.trinketsDropped == null) Run.trinketsDropped = new List<string>();
                Run.trinketsDropped.Add(trinket);
                var td = Gear.Trinket(Content, trinket);
                Emit(new GameEvent(GameEventType.Loot, 0, true, "Trinket! " + td.emoji + " " + td.name));
            }
            Touch(false);
        }

        // ───────────── interactables ─────────────
        /// A forage spot: opens the gather minigame (GatherOverlay → ResolveGather returns to the dungeon).
        public bool DungeonStartGather()
        {
            if (!InDungeon) return false;
            Run.gather = Explore.StartGather(Content, Run, Stats, _rng);
            Phase = Phase.Gather;
            Touch(false);
            return true;
        }

        /// A campfire: heals restHealPct of max HP and shows the rest overlay (LeaveRest returns to the dungeon).
        public bool DungeonRest()
        {
            if (!InDungeon) return false;
            int healed = Explore.ApplyRest(Content, Run, Stats);
            Emit(new GameEvent(GameEventType.PlayerHeal, healed, true, "Rested by the fire"));
            Phase = Phase.Rest;
            Touch(false);
            return true;
        }

        /// Ladder: down to the next floor (harder, richer). The Unity side builds the new floor on Changed.
        public bool DungeonDescend()
        {
            if (!InDungeon || Run.floor >= Run.floorsTotal) return false;
            Run.floor++;
            Info("🪜 Floor " + Run.floor + " of " + Run.floorsTotal + (Run.floor == Run.floorsTotal ? " — something big waits below" : ""));
            Touch(false);
            return true;
        }

        /// Exit: go home with the bag. On the last floor this clears the route; earlier it's a partial return.
        public void DungeonExit()
        {
            if (!InDungeon) return;
            bool final = Run.floor >= Run.floorsTotal;
            Emit(new GameEvent(GameEventType.RunCleared, 0, true, final ? "Route cleared!" : "Home with your haul"));
            EndRun(final ? RunOutcome.Cleared : RunOutcome.Returned);
        }
    }
}
