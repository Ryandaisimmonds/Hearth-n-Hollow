// Hearth & Hollow — settings / dev-mode actions on the session: full reset and cheats. Pure C#.
namespace HearthHollow.Core
{
    public partial class GameSession
    {
        /// Wipe all progress and start over as a brand-new profile. Abandons any run in progress.
        public void ResetGame()
        {
            Profile = Economy.NewProfile(Content);
            Profile.saveVersion = 4;
            UpgradeProgression();
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
                int have = Economy.PantryCount(Profile, id);
                if (have < count) Economy.AddToPantry(Profile, id, count - have);
            }
            Info("Dev: pantry stocked — " + count + " of every ingredient.");
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
