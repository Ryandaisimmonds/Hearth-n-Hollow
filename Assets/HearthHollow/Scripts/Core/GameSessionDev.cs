// Hearth & Hollow — settings / dev-mode actions on the session: full reset and cheats. Pure C#.
namespace HearthHollow.Core
{
    public partial class GameSession
    {
        /// Save the profile now (e.g. after the town remembers something, like meeting Old Wren).
        public void SaveProfile() { Touch(true); }

        /// Wipe all progress and start over as a brand-new profile. Abandons any run in progress.
        public void ResetGame()
        {
            Profile = Economy.NewProfile(Content);
            Profile.saveVersion = 4;
            UpgradeProgression();
            Cooking.UpgradeSave(Profile);
            Run = null;
            LastSummary = null;
            RollNewCamp();
            Phase = Phase.Hearth;
            Info("Fresh start — all progress has been reset.");
            Touch(true);
        }

        /// Dev: give a class <paramref name="levels"/> levels (capped), so the skill tree can be tried out.
        public void DevAddLevels(ClassId cls, int levels)
        {
            var cp = Profile.Progress(cls);
            int before = cp.level;
            cp.level = System.Math.Min(T.levelCap, cp.level + levels);
            cp.xp = 0;
            Info("Dev: " + cls + " level " + before + " → " + cp.level);
            Touch(true);
        }

        /// Dev: mark the current story chapter cleared (and hand over any key item it gives), to jump ahead in the story.
        public void DevClearChapter()
        {
            var r = Routes.CurrentChapter(Content, Profile);
            if (r == null) { Info("Dev: the story is already finished"); return; }
            if (!Profile.clearedRouteIds.Contains(r.id)) Profile.clearedRouteIds.Add(r.id);
            if (!string.IsNullOrEmpty(r.requiredKeyItem) && !Profile.keyItems.Contains(r.requiredKeyItem)) Profile.keyItems.Add(r.requiredKeyItem);
            Info("Dev: cleared chapter " + r.chapter + " (" + r.name + ")");
            Touch(true);
        }

        /// Dev: give every story key item.
        public void DevGiveKeyItems()
        {
            if (Profile.keyItems == null) Profile.keyItems = new System.Collections.Generic.List<string>();
            foreach (var k in Content.keyItems) if (!Profile.keyItems.Contains(k.id)) Profile.keyItems.Add(k.id);
            Info("Dev: all key items given");
            Touch(true);
        }

        /// Dev: top every ingredient in the game up to at least <paramref name="count"/> in the pantry.
        public void DevFillPantry(int count)
        {
            for (int i = 0; i < Content.ingredients.Count; i++)
            {
                string id = Content.ingredients[i].id;
                for (int g = 0; g < (Content.ingredients[i].material ? 1 : Cooking.Grades); g++)   // materials have no grade
                {
                    int have = Economy.PantryCount(Profile, id, g);
                    if (have < count) Economy.AddToPantry(Profile, id, count - have, g);
                }
            }
            Info("Dev: pantry stocked — " + count + " of every ingredient at every grade.");
            Touch(true);
        }

        /// Dev: make today's camp event (and the town's weather / visitors) this one. Re-rolls the camp until it matches,
        /// so a merchant has real wares and a forager leaves a real basket. CampEvent.None = a calm day.
        public void DevSetCampEvent(CampEvent ev)
        {
            if (Phase != Phase.Hearth) { Info("Dev: change the day's event at camp"); return; }
            HearthState h = null;
            for (int i = 0; i < 500; i++) { h = Economy.RollCamp(Content, Profile, _rng); if (h.campEvent == ev) break; }
            if (h == null || h.campEvent != ev) { if (h == null) h = Hearth; h.campEvent = ev; }
            Hearth = h; Profile.camp = h; Profile.campDay = Profile.day;
            for (int i = 0; i < h.gift.Count; i++) Economy.AddToPantry(Profile, h.gift[i].ingredientId, h.gift[i].count);
            Info("Dev: today is now " + (ev == CampEvent.None ? "a calm day" : ev.ToString()));
            Touch(true);
        }

        /// Dev: move on a day without a run (new camp event, the pond bites again).
        public void DevNewDay()
        {
            if (Phase != Phase.Hearth) return;
            Profile.day++;
            RollNewCamp();
            Info("Dev: day " + Profile.day);
            Touch(true);
        }

        /// Dev: the fishing pond bites again today.
        public void DevResetFishing() { Profile.lastFishDay = 0; Info("Dev: you can fish again today"); Touch(true); }

        /// Dev: Old Wren gives her first-meeting tutorial again.
        public void DevResetWren() { Profile.metWren = false; Info("Dev: Old Wren will greet you as a newcomer again"); Touch(true); }

        /// Dev: every kitchen station to its top level and the biggest pantry (max), or everything back to level 1.
        public void DevKitchen(bool max)
        {
            if (Profile.stationTier == null) Profile.stationTier = new System.Collections.Generic.List<int>();
            int n = System.Enum.GetValues(typeof(Station)).Length;
            while (Profile.stationTier.Count < n) Profile.stationTier.Add(0);
            for (int i = 0; i < n; i++) Profile.stationTier[i] = max ? Kitchen.MaxLevel(Content) - 1 : 0;
            Profile.pantryTier = max && Content.pantryTiers != null ? System.Math.Max(0, Content.pantryTiers.Count - 1) : 0;
            Info(max ? "Dev: kitchen fully upgraded" : "Dev: kitchen back to level 1");
            Touch(true);
        }

        /// Dev: <paramref name="count"/> of every building material.
        public void DevGiveMaterials(int count)
        {
            foreach (var ing in Content.ingredients) if (ing.material) Economy.AddToPantry(Profile, ing.id, count);
            Info("Dev: +" + count + " of every building material");
            Touch(true);
        }

        /// Dev: forget the story chapters cleared (key items stay), e.g. to see the Hearth fire small again.
        public void DevResetStory()
        {
            Profile.clearedRouteIds.RemoveAll(id => { var r = Content.Route(id); return r != null && r.chapter > 0; });
            Info("Dev: story chapters reset");
            Touch(true);
        }

        /// Dev: raise coins to at least <paramref name="coins"/>.
        public void DevSetCoins(int coins)
        {
            if (Profile.coins < coins) Profile.coins = coins;
            Info("Dev: coins set to " + Profile.coins + ".");
            Touch(true);
        }

        /// Dev: max everything — every class joins, reaches the level cap and fills its skill tree as far as its points go,
        /// every gear track and the bag at their top tier, all trinkets owned, the kitchen maxed, and a tray of the
        /// best Prime meals (the top dish of each station). Camp only.
        public void DevMaxEverything()
        {
            if (Phase != Phase.Hearth) { Info("Dev: max everything at camp"); return; }

            // every class joins your camp (their join route counts as cleared)
            foreach (var cd in Content.classes)
                if (!string.IsNullOrEmpty(cd.joinRoute) && !Profile.clearedRouteIds.Contains(cd.joinRoute)) Profile.clearedRouteIds.Add(cd.joinRoute);

            GearSlot[] tracks = { GearSlot.Weapon, GearSlot.Body, GearSlot.Head, GearSlot.Boots };
            foreach (var cd in Content.classes)
            {
                var cp = Profile.Progress(cd.id);
                cp.level = T.levelCap; cp.xp = 0;
                // skill tree: buy every rank the points allow, top rows first so later nodes unlock
                var nodes = Content.skillNodes.FindAll(n => n.classId == cd.id);
                nodes.Sort((x, y) => x.row != y.row ? x.row.CompareTo(y.row) : x.branch.CompareTo(y.branch));
                for (bool bought = true; bought;)
                {
                    bought = false;
                    foreach (var n in nodes) if (Progression.Buy(Content, Profile, cd.id, n.id)) bought = true;
                }
                Progression.EnsureLoadout(Content, Profile, cd.id);
                // gear: every track to its top tier
                foreach (var slot in tracks) while (Gear.Grant(Content, Profile, slot, cd.id) != null) { }
            }
            if (Content.bagTiers != null && Content.bagTiers.Count > 0) Profile.bagTier = Content.bagTiers.Count - 1;
            foreach (var t in Content.trinkets) Gear.Give(Content, Profile, t.id);

            // kitchen: every station and the pantry at the top level
            if (Profile.stationTier == null) Profile.stationTier = new System.Collections.Generic.List<int>();
            int stations = System.Enum.GetValues(typeof(Station)).Length;
            while (Profile.stationTier.Count < stations) Profile.stationTier.Add(0);
            for (int i = 0; i < stations; i++) Profile.stationTier[i] = Kitchen.MaxLevel(Content) - 1;
            Profile.pantryTier = Content.pantryTiers != null ? System.Math.Max(0, Content.pantryTiers.Count - 1) : 0;

            // the tray: the top dish of each station, Prime (anything already packed goes back to the meal pantry)
            foreach (var m in Profile.packed) Profile.mealPantry.Add(m);
            Profile.packed.Clear();
            var best = new System.Collections.Generic.List<RecipeData>();
            foreach (Station st in System.Enum.GetValues(typeof(Station)))
            {
                RecipeData top = null;
                foreach (var r in Content.recipes) if (!r.always && r.station == st && (top == null || r.stationLevel > top.stationLevel)) top = r;
                if (top != null) best.Add(top);
            }
            for (int i = 0; i < Content.tuning.traySize && best.Count > 0; i++)
                Profile.packed.Add(new CookedMeal { recipeId = best[i % best.Count].id, grade = Cooking.Grades - 1 });
            Cooking.SyncTray(Profile);

            EnsureJoinedSelection();
            Info("Dev: everything maxed — all classes, skills, gear, trinkets, kitchen, and a tray of Prime " +
                 string.Join(", ", best.ConvertAll(r => r.name).ToArray()));
            Touch(true);
        }
    }
}
