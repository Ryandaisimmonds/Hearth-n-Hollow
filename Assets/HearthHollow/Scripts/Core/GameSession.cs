// Hearth & Hollow — GameSession: the single facade the UI talks to.
// Orchestrates Economy / Meals / Bag / StatCalc / RunRules / the dungeon. Pure C#.
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
        internal TuningData T { get { return Content.tuning; } }

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
            Cooking.MergeIngredientGrades(Profile);   // ingredients lost their grades (saveVersion 10)
            Cooking.DropUnknownMeals(Content, Profile);   // meals whose recipe is gone
            if (Profile.saveVersion < 7) { ClassRework7(); UpgradeProgression(); }   // class rework: a fresh start for every class (GameSessionClasses.cs)
            if (Profile.saveVersion < 8) Profile.saveVersion = 8;
            if (Profile.knownElements == null) Profile.knownElements = new System.Collections.Generic.List<KnownElement>();
            if (Profile.saveVersion < 9) Profile.saveVersion = 9;   // Design v2: element discovery starts empty   // Design v2: no levels, XP or skill trees; their old fields drop out on the next save
            UpgradeArcProgress();                     // deepest floors / bosses / learned moves (saveVersion 11)
            GearRefund = Gear.ShareGear(Content, Profile);   // one shared utility set; power gear refunded (saveVersion 12)
            if (Profile.seenIngredients == null) Profile.seenIngredients = new System.Collections.Generic.List<string>();
            Routes.SeedSeen(Content, Profile);               // ingredients you've had reveal recipes (saveVersion 13)
            Story.UpgradeSave();                              // Old Wren's thread (saveVersion 14); shorter areas, Frost Hollow (saveVersion 15)
            UpgradeProgression();                     // loadouts follow the move pool
            EnsureJoinedSelection();
            if (Profile.keyItems == null) Profile.keyItems = new System.Collections.Generic.List<string>();   // story key items (content v4)
            if (Profile.storyFlags == null) Profile.storyFlags = new System.Collections.Generic.List<string>();   // story beats
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
        internal void Recompute() { Stats = StatCalc.Compute(Content, Profile, Run); }

        private void Emit(GameEvent e)
        {
            if (e != null && Run != null && Loggable(e.type) && !string.IsNullOrEmpty(e.text)) Log(e.text);   // the run log
            if (e != null && EventRaised != null) EventRaised(e);
        }
        private void Emit(List<GameEvent> list) { if (list == null) return; foreach (var e in list) Emit(e); }
        internal void Info(string text) { Emit(new GameEvent(GameEventType.Info, 0, false, text)); }

        internal void Touch(bool save)
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
        /// Coins refunded when gear became one shared utility set; 0 once it's been shown.
        public int GearRefund { get; set; }

        public CookedMeal Cook(string recipeId, CookChoice choice)
        {
            if (Phase != Phase.Hearth) { Info("Cook at the hearth"); return null; }
            string reason;
            var r = Content.Recipe(recipeId);
            var meal = Cooking.Cook(Content, Profile, r, choice, _rng, out reason);
            if (meal == null) { Info(reason); return null; }
            Emit(new GameEvent(GameEventType.Info, meal.grade, false,
                Cooking.GradeGem(meal.grade) + " " + Cooking.GradeName(meal.grade) + " " + r.emoji + " " + r.name + " — into the meal pantry"));
            Story.OnCooked();
            Touch(true);
            return meal;
        }

        /// Pack a cooked meal from the meal pantry onto the tray.
        /// Cooked meals with an empty element slot (meal pantry first, then the tray).
        public List<CookedMeal> SeasonableMeals()
        {
            var list = new List<CookedMeal>();
            foreach (var m in Profile.mealPantry) if (m != null && string.IsNullOrEmpty(m.accelId)) list.Add(m);
            foreach (var m in Profile.packed) if (m != null && string.IsNullOrEmpty(m.accelId)) list.Add(m);
            return list;
        }

        public bool SeasonMeal(CookedMeal m, string ingId, out string reason)
        {
            if (Phase != Phase.Hearth) { reason = "Only in town"; return false; }
            if (!Cooking.Season(Content, Profile, m, ingId, out reason)) return false;
            Story.OnSeasoned(m);
            var r = Content.Recipe(m.recipeId); var ing = Content.Ingredient(ingId);
            Info("🧂 " + (r != null ? r.name : "Meal") + " seasoned: " + Cooking.SlotText(ing));
            Touch(true);
            return true;
        }

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
            int caught = T.fishCatchCount + Cooking.ClampGrade(grade);
            Emit(new GameEvent(GameEventType.Loot, caught, false, "+" + caught + " " + (ing != null ? ing.emoji + " " + ing.name : "fish")));
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

        // ───── ladder choice: what you'd risk ─────
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

        public bool StartRun(string routeId) { return StartRun(routeId, 1); }

        /// Start a run, optionally from a shortcut floor (one past a boss you've beaten).
        public bool StartRun(string routeId, int startFloor)
        {
            if (Phase != Phase.Hearth) return false;
            if (!ClassAvailable(Profile.selectedClass)) { Info(ClassJoinHint(Profile.selectedClass)); return false; }
            if (!Slice.ShowsRoute(routeId) && routeId != FallRoute) { Info("Not in this build yet"); return false; }   // the fall's nameless dungeon is hidden
            string block = Routes.EnterBlock(Content, Profile, routeId, Profile.selectedClass);
            if (block != null) { Info(block); return false; }
            RouteOffer offer = routeId == FallRoute ? null : Hearth.routeOffers.Find(o => o.routeId == routeId);   // the fall's dungeon never rolls Rich / Lucky / Dangerous
            if (offer == null) offer = new RouteOffer { routeId = routeId, mod = RouteMod.None };
            bool storm = Hearth.campEvent == CampEvent.Storm;

            Run = RunRules.CreateRun(Content, Profile, offer, storm);
            ClearRunLog();   // a fresh log each run
            Run.firstDive = Story.FirstDive;
            Run.eaten = Cooking.Packed(Content, Profile);   // the packed cooked meals (quality + element ingredients)
            Run.infusions = Cooking.Infusions(Content, Run.eaten);   // every meal's infusion; attacks without their own element carry them all
            Run.element = Run.infusions.Count > 0 ? Run.infusions[0] : Element.None;
            Run.guards = Cooking.Guards(Content, Run.eaten);      // elements the meals guard against
            if (Run.infusions.Count > 0) Log(Elements.List(Run.infusions) + " Your attacks are infused with " + Elements.Names(Run.infusions));
            foreach (var g in Run.guards) Log(Elements.Emoji(g) + " Your meals guard you against " + Elements.Name(g));
            Run.storm = storm;
            Run.harvestMoon = Hearth.campEvent == CampEvent.HarvestMoon;
            Emit(Meals.PackForRun(Content, Run));   // the tray is eaten at camp: buffs last the whole run
            // Profile.tray is kept until the run ends, so quitting mid-run doesn't lose packed meals.
            Profile.runsStarted++;
            Recompute();
            Run.hp = Stats.maxHp;
            if (!DungeonRules.StartFloors(Content, Profile, routeId).Contains(startFloor)) startFloor = 1;
            BeginDungeon(startFloor);
            LastSummary = null;
            var r = Content.Route(routeId);
            Info("Setting out: " + (r != null ? r.emoji + " " + r.name : routeId) + (offer.mod != RouteMod.None ? "  (" + offer.mod + ")" : ""));
            if (Run.firstDive)
            {
                if (Story.Falling) Log(Story.FallIntro);   // the UI shows it as a banner once the dungeon is on screen
                else Info("🌫️ The fog is thick and the monsters are restless. Just get as far as you can.");
            }

            Touch(true);
            return true;
        }

        private void DropLoot(bool elite)
        {
            DropMaterial(elite, false);   // building materials for kitchen upgrades
            var route = Content.Route(Run.routeId);
            if (route == null) return;
            if (!elite && !_rng.Chance(T.monsterDropChance + Gear.Perk(Content, Profile, TrinketPerk.LootOnKill))) return;   // the Magpie Spoon raises the drop chance
            string id = PickRouteIngredient();   // gather ingredients + this floor's deeper drops
            if (id == null) return;
            // hazard routes: sometimes the matching guard (nettle in poison dungeons, ginger in the cold…)
            if (route.hazard != Hazard.None && _rng.Chance(T.guardDropChance))
            {
                var guards = Routes.CounterAccelerators(Content, route.hazard);
                if (guards.Count > 0) id = _rng.Pick(guards).id;
            }
            int count = (elite ? 2 : 1) * (Run.mod == RouteMod.Lucky ? 2 : 1);
            AddLoot(id, count, elite);
        }

        /// Building materials (kitchen upgrades): a chance from normal monsters, a few from elites and bosses.
        private void DropMaterial(bool elite, bool boss)
        {
            var route = Content.Route(Run.routeId);
            if (route == null || route.materialIds == null || route.materialIds.Count == 0) return;
            int floor = Run.floor;
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
            int left = Bag.Add(Run, id, count, T.stackSize);
            int got = count - left;
            if (got > 0 && Routes.See(Profile, id) && Content.Ingredient(id) != null && !Kitchen.IsMaterial(Content, id))
                Info("📖 New ingredient: " + Content.Ingredient(id).emoji + " " + Content.Ingredient(id).name + " — new recipes to try at the cooking area");
            var ing = Content.Ingredient(id);
            string name = ing != null ? ing.emoji + " " + ing.name : id;
            if (got > 0) Emit(new GameEvent(GameEventType.Loot, got, true, "+" + got + " " + name));
            if (left > 0 && InDungeon) { DropOnFloor(id, left); Emit(new GameEvent(GameEventType.BagFull, left, true, "Bag full — " + left + " " + name + " dropped on the floor (E to swap)")); }
            else if (left > 0) Emit(new GameEvent(GameEventType.BagFull, left, true, "Bag full — " + left + " " + name + " left behind"));
        }

        public void ResolveGather(float marker01)
        {
            if (Phase != Phase.Gather || Run == null || Run.gather == null) return;
            var g = Run.gather;
            var grade = RunRules.EvaluateGather(g, marker01);
            int yield = RunRules.GatherYield(T, grade, Run.mod);
            if (yield > 0 && Run.harvestMoon) yield += T.harvestMoonGatherBonus;
            if (yield > 0) AddLoot(g.ingredientId, yield, false, false, grade == GatherGrade.Perfect);   // a perfect pick is more likely to be Prime
            Emit(new GameEvent(GameEventType.Info, yield, true,
                grade == GatherGrade.Perfect ? "Perfect pick! ✨" : grade == GatherGrade.Safe ? "Nice pick" : "It slipped away…"));
            Run.gather = null;
            Phase = Phase.Dungeon;
            Touch(false);
        }

        public void LeaveRest()
        {
            if (Phase != Phase.Rest) return;
            Phase = Phase.Dungeon;
            Touch(false);
        }

        public void FleeHome()
        {
            if (!InRun) return;
            Run.gather = null;
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
            if (InDungeon) { BagDrop(slot); return; }   // in the dungeon it lands on the floor, so nothing's lost by accident
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
            Story.OnRunEnd();                    // the opening arc, Wren's thread and the fall take note
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
            Story.OnHome();   // the fall's fade ends, the arc may grow the town, and you may wake up there for the first time
            // Today's camp event is announced by the UI's camp-event pop-up (no toast).
            Touch(true);
        }
    }
}
