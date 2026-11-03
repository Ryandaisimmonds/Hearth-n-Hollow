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
            Cooking.UpgradeSave(Profile);           // meal pantry + packed cooked meals (saveVersion 6)
            Cooking.DropUnknownMeals(Content, Profile);   // meals whose recipe is gone
            if (Profile.saveVersion < 7) { ClassRework7(); UpgradeProgression(); }   // class rework: a fresh start for every class (GameSessionClasses.cs)
            if (Profile.saveVersion < 8) Profile.saveVersion = 8;
            if (Profile.knownElements == null) Profile.knownElements = new System.Collections.Generic.List<KnownElement>();
            if (Profile.saveVersion < 9) Profile.saveVersion = 9;   // Design v2 (ticket 07): element discovery starts empty   // Design v2 (ticket 04): no levels, XP or skill trees; their old fields drop out on the next save
            EnsureJoinedSelection();
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
            if (!Slice.HasClass(id)) { Info("Not in this build yet"); return; }
            if (!ClassJoined(id)) { Info(ClassJoinHint(id)); return; }
            Profile.selectedClass = id;
            Touch(true);
        }

        // ───── cooking (v9): cook ahead into the meal pantry, pack cooked meals on the tray ─────
        /// The recipe's ingredients are in the pantry (some grade of each) and the meal pantry has room.
        public bool CanCook(string recipeId, out string reason)
        {
            if (Phase != Phase.Hearth) { reason = "Cook at the hearth"; return false; }
            var r = Content.Recipe(recipeId);
            if (r == null) { reason = "Unknown recipe"; return false; }
            if (Cooking.MealPantryFull(Content, Profile)) { reason = "The meal pantry is full — pack or throw out a meal first"; return false; }
            if (r.always) { reason = r.name + " is always available — no need to cook it"; return false; }
            if (!Kitchen.Unlocked(Content, Profile, r)) { reason = "Needs the " + r.station + " at level " + r.stationLevel; return false; }
            if (!Cooking.HaveIngredients(Content, Profile, r)) { reason = "Not enough ingredients"; return false; }
            reason = null; return true;
        }

        public bool CanCook(string recipeId, CookChoice choice, out string reason)
        {
            if (Phase != Phase.Hearth) { reason = "Cook at the hearth"; return false; }
            return Cooking.CanCook(Content, Profile, Content.Recipe(recipeId), choice, out reason);
        }

        /// Cook a meal with the chosen ingredient grades and accelerator. Returns the cooked meal (null if it couldn't).
        public CookedMeal Cook(string recipeId, CookChoice choice)
        {
            if (Phase != Phase.Hearth) { Info("Cook at the hearth"); return null; }
            string reason;
            var r = Content.Recipe(recipeId);
            var meal = Cooking.Cook(Content, Profile, r, choice, _rng, out reason);
            if (meal == null) { Info(reason); return null; }
            Emit(new GameEvent(GameEventType.Info, meal.grade, false,
                Cooking.GradeGem(meal.grade) + " " + Cooking.GradeName(meal.grade) + " " + r.emoji + " " + r.name + " — into the meal pantry"));
            Touch(true);
            return meal;
        }

        /// Pack a cooked meal from the meal pantry onto the tray.
        public bool PackMeal(int pantryIndex)
        {
            if (Phase != Phase.Hearth) return false;
            string reason;
            if (!Cooking.Pack(Content, Profile, pantryIndex, out reason)) { Info(reason); return false; }
            Touch(true); return true;
        }

        // ───── town fishing pond: one quick catch a day ─────
        public bool CanFish { get { return Phase == Phase.Hearth && Fishing.CanFish(Profile); } }

        public bool LandCatch(int grade)
        {
            if (!CanFish) { Info("You've had today's catch — come back after your next expedition"); return false; }
            Fishing.Catch(Content, Profile, grade);
            var ing = Content.Ingredient(T.fishIngredient);
            Emit(new GameEvent(GameEventType.Loot, T.fishCatchCount, false, "+" + T.fishCatchCount + " " + Cooking.GradeGem(grade) + " " + Cooking.IngGradeName(grade) + " " + (ing != null ? ing.emoji + " " + ing.name : "fish")));
            Touch(true);
            return true;
        }

        // ───── kitchen upgrades (contentVersion 13) ─────
        public bool UpgradeStation(Station st, out string reason)
        {
            if (Phase != Phase.Hearth) { reason = "Upgrade the kitchen at camp"; return false; }
            bool ok = Kitchen.UpgradeStation(Content, Profile, st, out reason);
            if (ok)
            {
                int lv = Kitchen.Level(Content, Profile, st);
                var dishes = Kitchen.DishesAt(Content, st, lv);
                var names = new List<string>();
                foreach (var d in dishes) names.Add(d.emoji + " " + d.name);
                Emit(new GameEvent(GameEventType.LevelUp, lv, false, st + " level " + lv + (names.Count > 0 ? " — new: " + string.Join(", ", names.ToArray()) : "")));
            }
            else if (!string.IsNullOrEmpty(reason)) Info(reason);
            Touch(ok);
            return ok;
        }

        public bool UpgradePantry(out string reason)
        {
            if (Phase != Phase.Hearth) { reason = "Upgrade the kitchen at camp"; return false; }
            bool ok = Kitchen.UpgradePantry(Content, Profile, out reason);
            if (ok) Emit(new GameEvent(GameEventType.LevelUp, 0, false, "Meal pantry: " + Kitchen.PantrySlots(Content, Profile) + " shelves"));
            else if (!string.IsNullOrEmpty(reason)) Info(reason);
            Touch(ok);
            return ok;
        }

        // ───── ladder choice (contentVersion 14): what's below, and what you'd risk ─────
        /// Fine / Prime chance for a normal pickup on <paramref name="floor"/> of the current route.
        public void DropOddsOnFloor(int floor, out float fine, out float prime)
        {
            var rt = Run != null ? Content.Route(Run.routeId) : null;
            Cooking.DropOdds(T, floor, rt != null ? rt.baseDifficulty : 0, false, false, false, Run != null && Run.mod == RouteMod.Lucky, out fine, out prime);
        }

        /// Pack an always-available meal (Camp Bread) onto the tray.
        public bool PackAlwaysMeal(string recipeId)
        {
            if (Phase != Phase.Hearth) return false;
            string reason;
            if (!Cooking.PackAlways(Content, Profile, recipeId, out reason)) { Info(reason); return false; }
            Touch(true); return true;
        }

        public bool PackAlwaysMealInto(string recipeId, int slot)
        {
            if (!PackAlwaysMeal(recipeId)) return false;
            int last = Profile.packed.Count - 1;
            if (slot >= 0 && slot < last) MoveTrayMeal(last, slot);
            return true;
        }

        /// Pack into a given tray slot (drag a meal onto the tray).
        public bool PackMealInto(int pantryIndex, int slot)
        {
            if (!PackMeal(pantryIndex)) return false;
            int last = Profile.packed.Count - 1;
            if (slot >= 0 && slot < last) MoveTrayMeal(last, slot);
            return true;
        }

        /// Take a packed meal off the tray, back into the meal pantry.
        public void DiscardTrayMeal(int slot)
        {
            if (Phase != Phase.Hearth) return;
            if (Cooking.Unpack(Content, Profile, slot)) Touch(true);
        }

        /// Throw a cooked meal away.
        public void ThrowOutMeal(int pantryIndex)
        {
            if (Phase != Phase.Hearth || pantryIndex < 0 || pantryIndex >= Profile.mealPantry.Count) return;
            var r = Content.Recipe(Profile.mealPantry[pantryIndex].recipeId);
            Cooking.Discard(Profile, pantryIndex);
            Info("Threw out " + (r != null ? r.emoji + " " + r.name : "a meal"));
            Touch(true);
        }

        public void MoveTrayMeal(int from, int to)
        {
            if (Phase != Phase.Hearth) return;
            Cooking.MovePacked(Profile, from, to);
            Touch(true);
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
            if (Phase != Phase.Hearth) { reason = "Upgrades are bought at camp"; return false; }
            bool ok = Gear.Buy(Content, Profile, Hearth, slot, out reason);
            GearTier now = Gear.Current(Content, Profile, slot, Profile.selectedClass);
            return Shop(ok, reason, now != null ? now.name + " — " + Gear.ValueText(slot, now) : "New gear!");
        }
        public bool BuyTrinket(string id, out string reason)
        {
            if (Phase != Phase.Hearth) { reason = "Upgrades are bought at camp"; return false; }
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
            if (!ClassAvailable(Profile.selectedClass)) { Info(ClassJoinHint(Profile.selectedClass)); return false; }
            if (!Slice.ShowsRoute(routeId)) { Info("Not in this build yet"); return false; }
            string block = Routes.EnterBlock(Content, Profile, routeId, Profile.selectedClass);
            if (block != null) { Info(block); return false; }
            RouteOffer offer = Hearth.routeOffers.Find(o => o.routeId == routeId) ?? new RouteOffer { routeId = routeId, mod = RouteMod.None };
            bool storm = Hearth.campEvent == CampEvent.Storm;

            Run = Explore.CreateRun(Content, Profile, offer, storm, _rng);
            Run.eaten = Cooking.Packed(Content, Profile);   // the packed cooked meals (grades + accelerators)
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
            DropMaterial(f.elite, false);   // building materials for kitchen upgrades
            var route = Content.Route(Run.routeId);
            if (route == null || route.gatherIngredientIds.Count == 0) return;
            if (!f.elite && !_rng.Chance(T.monsterDropChance)) return;
            string id = _rng.Pick(route.gatherIngredientIds);
            // hazard routes: sometimes the matching guard (nettle in poison dungeons, ginger in the cold…)
            if (route.hazard != Hazard.None && _rng.Chance(T.guardDropChance))
            {
                var guards = Routes.CounterAccelerators(Content, route.hazard);
                if (guards.Count > 0) id = _rng.Pick(guards).id;
            }
            int count = (f.elite ? 2 : 1) * (Run.mod == RouteMod.Lucky ? 2 : 1);
            AddLoot(id, count, f.elite);
        }

        /// Building materials (kitchen upgrades): a chance from normal monsters, a few from elites and bosses.
        private void DropMaterial(bool elite, bool boss)
        {
            var route = Content.Route(Run.routeId);
            if (route == null || route.materialIds == null || route.materialIds.Count == 0) return;
            int floor = Run.dungeon ? Run.floor : 1;
            int n = boss ? T.bossMaterials : elite ? T.eliteMaterials + (floor >= 3 ? 1 : 0)
                  : (_rng.Chance(T.materialDropChance + T.materialDropPerFloor * Math.Max(0, floor - 1)) ? 1 : 0);
            // deeper floors lean to the route's own (better) material — the first in its list
            string mat = floor >= 2 && _rng.Chance(0.7f) ? route.materialIds[0] : _rng.Pick(route.materialIds);
            if (n > 0) AddLoot(mat, n);
        }

        private void AddLoot(string id, int count, bool elite = false, bool boss = false, bool perfect = false)
        {
            if (count <= 0) return;
            var rt = Content.Route(Run.routeId);
            int grade = Kitchen.IsMaterial(Content, id) ? 0   // materials have no grade
                : Cooking.RollDropGrade(T, Run.dungeon ? Run.floor : 1, rt != null ? rt.baseDifficulty : 0, elite, boss, perfect, Run.mod == RouteMod.Lucky, _rng);
            int left = Bag.Add(Run, id, count, T.stackSize, grade);
            int got = count - left;
            var ing = Content.Ingredient(id);
            string name = (grade > 0 ? Cooking.GradeGem(grade) + " " + Cooking.IngGradeName(grade) + " " : "") + (ing != null ? ing.emoji + " " + ing.name : id);
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
            if (yield > 0) AddLoot(g.ingredientId, yield, false, false, grade == GatherGrade.Perfect);   // a perfect pick is more likely to be Prime
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
            Profile.packed.Clear(); Profile.tray.Clear();   // the tray was eaten at set-out (kept until now so quitting mid-run keeps it)
            var joinedBefore = JoinedNow();
            LastSummary = Economy.SettleRun(Content, Profile, Run, Stats);
            AnnounceJoins(joinedBefore);   // story: a class may have joined your camp
            if (outcome == RunOutcome.Died) ArcEvent(ArcTrigger.FirstDeath);   // opening arc: Wren feeds you, the kitchen opens
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
            ArcEvent(ArcTrigger.HomeAfterPit);   // opening arc: home after the pit, the camp grows into a town
            // Today's camp event is announced by the UI's camp-event pop-up (no toast).
            Touch(true);
        }
    }
}
