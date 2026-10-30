// Hearth & Hollow — GameSession: the single facade the UI talks to.
// Orchestrates Economy / Meals / Bag / StatCalc / Explore / Combat. Pure C#.
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public partial class GameSession
    {
        public GameContent Content { get; private set; }
        public Profile Profile { get; private set; }
        public HearthState Hearth { get; private set; }
        public RunState Run { get; private set; }
        public RunSummary LastSummary { get; private set; }
        public Phase Phase { get; private set; }
        public PlayerStats Stats { get; private set; }

        public event Action Changed;
        public event Action<GameEvent> EventRaised;
        public event Action ProfileSaveRequested;

        private readonly Rng _rng;
        private TuningData T { get { return Content.tuning; } }

        public GameSession(GameContent content, Profile profile, int seed)
        {
            if (content == null) throw new ArgumentNullException("content");
            Content = content;
            DefaultContent.Upgrade(Content);   // older content assets gain moves, trees and the level-50 tuning
            Profile = profile ?? Economy.NewProfile(content);
            foreach (var cd in content.classes) Profile.Progress(cd.id); // ensure entries exist
            if (Profile.saveVersion < 2) { Meals.RefundTray(Content, Profile); Profile.saveVersion = 2; }
            UpgradeProgression();   // skill trees + loadouts (saveVersion 3), kept valid every load
            Economy.UpgradeBag(Content, Profile);   // bag tiers (saveVersion 4)
            if (Profile.keyItems == null) Profile.keyItems = new System.Collections.Generic.List<string>();   // story key items (content v4)
            _rng = new Rng(seed);
            // Reuse today's saved camp so relaunching doesn't reroll the merchant / events.
            if (Profile.camp != null && Profile.campDay == Profile.day && Profile.camp.routeOffers != null && Profile.camp.routeOffers.Count > 0)
                Hearth = Profile.camp;
            else
                RollNewCamp();
            Phase = Phase.Hearth;
            Recompute();
        }

        /// Roll a fresh camp visit, remember it in the profile, and hand over any Forager gift.
        private void RollNewCamp()
        {
            Hearth = Economy.RollCamp(Content, Profile, _rng);
            Profile.camp = Hearth;
            Profile.campDay = Profile.day;
            for (int i = 0; i < Hearth.gift.Count; i++)
                Economy.AddToPantry(Profile, Hearth.gift[i].ingredientId, Hearth.gift[i].count);
        }

        /// Base stats (class + level + gear, no meals) — for the hearth's "before meals" preview.
        public PlayerStats BaseStats { get { return StatCalc.ComputeBase(Content, Profile); } }

        // ───────────────────────── helpers ─────────────────────────
        private void Recompute() { Stats = StatCalc.Compute(Content, Profile, Run); }

        private void Emit(GameEvent e) { if (e != null && EventRaised != null) EventRaised(e); }
        private void Emit(List<GameEvent> list) { if (list == null) return; foreach (var e in list) Emit(e); }
        private void Info(string text) { Emit(new GameEvent(GameEventType.Info, 0, false, text)); }

        private void Touch(bool save)
        {
            Recompute();
            if (save && ProfileSaveRequested != null) ProfileSaveRequested();
            if (Changed != null) Changed();
        }

        private bool InRun { get { return Run != null && Phase != Phase.Hearth && Phase != Phase.RunEnd; } }

        // ───────────────────────── Hearth ─────────────────────────
        public void SelectClass(ClassId id)
        {
            if (Phase != Phase.Hearth) return;
            Profile.selectedClass = id;
            Touch(true);
        }

        public bool CanCook(string recipeId, out string reason)
        {
            if (Phase != Phase.Hearth) { reason = "Cook at the hearth"; return false; }
            return Meals.CanCook(Content, Profile, recipeId, out reason);
        }

        public bool Cook(string recipeId)
        {
            string reason;
            if (!CanCook(recipeId, out reason)) { Info(reason); return false; }
            bool ok = Meals.Cook(Content, Profile, recipeId);
            if (ok)
            {
                var r = Content.Recipe(recipeId);
                Emit(new GameEvent(GameEventType.Info, 0, false, "Added " + (r != null ? r.emoji + " " + r.name : recipeId) + " to the tray"));
            }
            Touch(ok);
            return ok;
        }

        /// Cook a recipe straight into a tray slot (drag a recipe card onto the tray).
        public bool CookInto(string recipeId, int slot)
        {
            if (!Cook(recipeId)) return false;
            int last = Profile.tray.Count - 1;
            if (slot >= 0 && slot < last) MoveTrayMeal(last, slot);
            return true;
        }

        public void DiscardTrayMeal(int slot)
        {
            if (Phase != Phase.Hearth || slot < 0 || slot >= Profile.tray.Count) return;
            var r = Content.Recipe(Profile.tray[slot]);
            Profile.tray.RemoveAt(slot);   // nothing was spent yet, so its ingredients are simply free again
            Info("Removed " + (r != null ? r.emoji + " " + r.name : "meal") + " — ingredients back in the pantry");
            Touch(true);
        }

        public void MoveTrayMeal(int from, int to)
        {
            var tray = Phase == Phase.Hearth ? Profile.tray : null;
            if (tray == null || from < 0 || from >= tray.Count || from == to) return;
            if (to < 0) to = 0; if (to >= tray.Count) to = tray.Count - 1;
            var item = tray[from]; tray.RemoveAt(from); tray.Insert(to, item);
            Touch(Phase == Phase.Hearth);
        }

        public bool BuyWeapon(out string reason) { return Shop(Economy.BuyWeapon(Content, Profile, Hearth, out reason), reason, "New weapon!"); }
        public bool BuyArmour(out string reason) { return Shop(Economy.BuyArmour(Content, Profile, Hearth, out reason), reason, "New armour!"); }
        /// Today's price for a gear tier (Armourer event discount applied).
        public int GearPrice(GearTier tier) { return Economy.GearPrice(Content, Hearth, tier); }
        public bool BuyBag(out string reason)
        {
            var next = Economy.NextBag(Content, Profile);
            return Shop(Economy.BuyBag(Content, Profile, Hearth, out reason), reason, next != null ? next.name + " — " + Economy.BagSlots(Content, Profile) + " bag slots" : "New bag!");
        }
        /// Workshop: next tier of a gear track for the selected class (bag: shared).
        public bool BuyGear(GearSlot slot, out string reason)
        {
            if (Phase != Phase.Hearth) { reason = "The Workshop is at camp"; return false; }
            bool ok = Gear.Buy(Content, Profile, Hearth, slot, out reason);
            GearTier now = Gear.Current(Content, Profile, slot, Profile.selectedClass);
            return Shop(ok, reason, now != null ? now.name + " — " + Gear.ValueText(slot, now) : "New gear!");
        }
        public bool BuyTrinket(string id, out string reason)
        {
            if (Phase != Phase.Hearth) { reason = "The Workshop is at camp"; return false; }
            var t = Gear.Trinket(Content, id);
            return Shop(Gear.BuyTrinket(Content, Profile, id, out reason), reason, t != null ? t.emoji + " " + t.name + "!" : "New trinket!");
        }
        public bool BuyMerchantTrinket(out string reason)
        {
            if (Phase != Phase.Hearth) { reason = "The merchant is at camp"; return false; }
            var t = Hearth != null ? Gear.Trinket(Content, Hearth.merchantTrinket) : null;
            return Shop(Gear.BuyMerchantTrinket(Content, Profile, Hearth, out reason), reason, t != null ? t.emoji + " " + t.name + "!" : "New trinket!");
        }
        /// Wear / take off a trinket (camp only).
        public bool EquipTrinket(string id, out string reason)
        {
            if (Phase != Phase.Hearth) { reason = "Change trinkets at camp"; return false; }
            if (!Gear.Equip(Content, Profile, id, out reason)) return false;
            Touch(true); return true;
        }
        public void UnequipTrinket(string id)
        {
            if (Phase != Phase.Hearth) return;
            if (Gear.Unequip(Profile, id)) Touch(true);
        }
        public int TrinketSlots { get { return Gear.TrinketSlots(Content, Profile); } }

        public bool BuyMerchant(int index, out string reason)
        {
            if (Phase != Phase.Hearth) { reason = "The merchant is at camp"; return false; }
            return Shop(Economy.BuyMerchant(Content, Profile, Hearth, index, out reason), reason, "Bought from the merchant");
        }

        private bool Shop(bool ok, string reason, string success)
        {
            if (ok) Emit(new GameEvent(GameEventType.Coins, 0, false, success));
            else if (!string.IsNullOrEmpty(reason)) Info(reason);
            Touch(ok);
            return ok;
        }

        public bool IsRouteUnlocked(string routeId) { return Economy.IsRouteUnlocked(Content, Profile, routeId); }

        public bool StartRun(string routeId)
        {
            if (Phase != Phase.Hearth) return false;
            string block = Routes.EnterBlock(Content, Profile, routeId, Profile.selectedClass);
            if (block != null) { Info(block); return false; }
            RouteOffer offer = Hearth.routeOffers.Find(o => o.routeId == routeId) ?? new RouteOffer { routeId = routeId, mod = RouteMod.None };
            bool storm = Hearth.campEvent == CampEvent.Storm;

            foreach (var msg in Meals.PayForTray(Content, Profile)) Info(msg);   // ingredients are spent now
            Run = Explore.CreateRun(Content, Profile, offer, storm, _rng);
            Run.storm = storm;
            Run.harvestMoon = Hearth.campEvent == CampEvent.HarvestMoon;
            Emit(Meals.PackForRun(Content, Run));   // the tray is eaten at camp: buffs last the whole run
            Run.skillCooldown = 0;
            // Profile.tray is kept until the run ends, so quitting mid-run doesn't lose packed meals.
            Profile.runsStarted++;
            Recompute();
            Run.hp = Stats.maxHp;
            if (UseDungeon) BeginDungeon(); else Phase = Phase.Explore;
            LastSummary = null;
            var r = Content.Route(routeId);
            Info("Setting out: " + (r != null ? r.emoji + " " + r.name : routeId) + (offer.mod != RouteMod.None ? "  (" + offer.mod + ")" : ""));
            Touch(true);
            return true;
        }

        // ───────────────────────── Hollow ─────────────────────────
        public bool CanMoveTo(int x, int y) { return Phase == Phase.Explore && Run != null && Explore.CanMoveTo(Run, x, y); }

        public bool MoveTo(int x, int y)
        {
            if (!CanMoveTo(x, y)) return false;
            Emit(Explore.MoveTo(Run, x, y));
            var tile = Run.TileAt(x, y);
            if (!tile.resolved)
            {
                switch (tile.type)
                {
                    case TileType.Monster:
                    case TileType.Elite:
                        Run.fight = Combat.StartFight(Content, tile, Run, Run.skillCooldown);
                        Phase = Phase.Fight;
                        var m = Content.Monster(tile.monsterId);
                        Info((tile.type == TileType.Elite ? "👑 " : "") + (m != null ? m.emoji + " " + m.name : "A monster") + " blocks the way!");
                        break;
                    case TileType.Gather:
                        Run.gather = Explore.StartGather(Content, Run, Stats, _rng);
                        Phase = Phase.Gather;
                        break;
                    case TileType.Rest:
                        int healed = Explore.ApplyRest(Content, Run, Stats);
                        tile.resolved = true; Run.roomsCleared++;
                        Emit(new GameEvent(GameEventType.PlayerHeal, healed, true, "Rested by the fire"));
                        Phase = Phase.Rest;
                        break;
                    case TileType.Exit:
                        tile.resolved = true;
                        Emit(new GameEvent(GameEventType.RunCleared, 0, true, "Through the portal!"));
                        EndRun(RunOutcome.Cleared);
                        return true;
                    default:
                        tile.resolved = true;
                        break;
                }
            }
            else if (tile.type == TileType.Exit)
            {
                EndRun(RunOutcome.Cleared);
                return true;
            }
            Touch(false);
            return true;
        }

        public bool CanUseSkill() { return Phase == Phase.Fight && Run != null && Run.fight != null && Combat.CanUseSkill(Content, Profile, Run); }

        public void Fight(FightAction action)
        {
            if (Phase != Phase.Fight || Run == null || Run.fight == null) return;
            if (action == FightAction.Skill && !CanUseSkill()) { Info("Skill isn't ready"); return; }
            var f = Run.fight;
            Emit(Combat.PlayerTurn(Content, Profile, Run, Stats, action, _rng));
            if (!f.over) { Touch(false); return; }

            Run.skillCooldown = f.skillCooldown;
            if (f.playerWon)
            {
                var tile = Run.TileAt(Run.px, Run.py);
                tile.resolved = true;
                Run.kills++; Run.roomsCleared++;
                int coins = Combat.KillCoins(Content, Run, Stats, f);
                int xp = Combat.KillXp(Content, Run, f);
                Run.coinsEarned += coins; Run.killXp += xp;
                Emit(new GameEvent(GameEventType.Coins, coins, true, "+" + coins + " coins"));
                Emit(new GameEvent(GameEventType.Xp, xp, true, "+" + xp + " XP"));
                DropLoot(f);
                Run.fight = null;
                Phase = Phase.Explore;
                Touch(false);
            }
            else
            {
                Run.fight = null;
                Info("You limp home…");
                EndRun(RunOutcome.Died);
            }
        }

        private void DropLoot(FightState f)
        {
            var route = Content.Route(Run.routeId);
            if (route == null || route.gatherIngredientIds.Count == 0) return;
            if (!f.elite && !_rng.Chance(T.monsterDropChance)) return;
            string id = _rng.Pick(route.gatherIngredientIds);
            int count = (f.elite ? 2 : 1) * (Run.mod == RouteMod.Lucky ? 2 : 1);
            AddLoot(id, count);
        }

        private void AddLoot(string id, int count)
        {
            if (count <= 0) return;
            int left = Bag.Add(Run, id, count, T.stackSize);
            int got = count - left;
            var ing = Content.Ingredient(id);
            string name = ing != null ? ing.emoji + " " + ing.name : id;
            if (got > 0) Emit(new GameEvent(GameEventType.Loot, got, true, "+" + got + " " + name));
            if (left > 0) Emit(new GameEvent(GameEventType.BagFull, left, true, "Bag full — " + left + " " + name + " left behind"));
        }

        public void ResolveGather(float marker01)
        {
            if (Phase != Phase.Gather || Run == null || Run.gather == null) return;
            var g = Run.gather;
            var grade = Explore.EvaluateGather(g, marker01);
            int yield = Explore.GatherYield(T, grade, Run.mod);
            if (yield > 0 && Run.harvestMoon) yield += T.harvestMoonGatherBonus;
            if (!Run.dungeon)
            {
                var tile = Run.TileAt(Run.px, Run.py);
                tile.resolved = true; Run.roomsCleared++;
            }
            if (yield > 0) AddLoot(g.ingredientId, yield);
            Emit(new GameEvent(GameEventType.Info, yield, true,
                grade == GatherGrade.Perfect ? "Perfect pick! ✨" : grade == GatherGrade.Safe ? "Nice pick" : "It slipped away…"));
            Run.gather = null;
            Phase = Run.dungeon ? Phase.Dungeon : Phase.Explore;
            Touch(false);
        }

        public void LeaveRest()
        {
            if (Phase != Phase.Rest) return;
            Phase = Run != null && Run.dungeon ? Phase.Dungeon : Phase.Explore;
            Touch(false);
        }

        public void FleeHome()
        {
            if (!InRun) return;
            Run.fight = null; Run.gather = null;
            Emit(new GameEvent(GameEventType.RunFled, 0, true, "You slip back to the hearth"));
            EndRun(RunOutcome.Fled);
        }

        public void BagMove(int from, int to)
        {
            if (Run == null || !InRun) return;
            if (from < 0 || to < 0 || from >= Run.bag.Count || to >= Run.bag.Count || from == to) return;
            Bag.Move(Run, from, to, T.stackSize);
            Touch(false);
        }

        public void BagDiscard(int slot)
        {
            if (Run == null || !InRun || slot < 0 || slot >= Run.bag.Count) return;
            Bag.Discard(Run, slot);
            Touch(false);
        }

        private void EndRun(RunOutcome outcome)
        {
            Run.outcome = outcome;
            Recompute();  // stats including meals (xpMult) for settlement
            Profile.tray.Clear();   // the tray was eaten at set-out (kept until now so quitting mid-run refunds it)
            int levelBefore = Profile.Progress(Run.classId).level;
            LastSummary = Economy.SettleRun(Content, Profile, Run, Stats);
            if (LastSummary != null && LastSummary.levelAfter > levelBefore)
                Emit(new GameEvent(GameEventType.LevelUp, LastSummary.levelAfter, true, "Level " + LastSummary.levelAfter + "!"));
            Phase = Phase.RunEnd;
            Touch(true);
        }

        // ───────────────────────── Run end ─────────────────────────
        public void ReturnToHearth()
        {
            if (Phase != Phase.RunEnd) return;
            Run = null;
            RollNewCamp();
            Phase = Phase.Hearth;
            // Today's camp event is announced by the UI's camp-event pop-up (no toast).
            Touch(true);
        }
    }
}
