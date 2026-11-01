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
    }
}
