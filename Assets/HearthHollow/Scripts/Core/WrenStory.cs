// Hearth & Hollow — Old Wren's guiding thread through the slice (ticket 35). Pure C#.
// Wren tells you what to do next at every step, and comes over by herself when she has something new to say:
//   (ticket 42: a new game opens with a fall into Meadow Hollow, so you meet Wren on waking up after it)
//   the fall → [Wren] cook a meal → show it to Wren → beat the Bramble Sow (bottom of Meadow Hollow, floor 3) →
//   [Wren] Chili Ridge for chilies → [Wren] season a meal with chili at the spice rack → Frost Hollow opens →
//   beat the Hoarfrost Alpha at the bottom → done. (Ticket 47 reshaped the areas.)
using System.Collections.Generic;

namespace HearthHollow.Core
{
    /// Append only (saved as an int).
    public enum WrenStep { Meet, FirstDive, AfterDive, CookMeal, ShowMeal, FirstBoss, HearHint, Ridge, RidgeBack, SeasonMeal, Alpha, Done }

    public partial class Profile
    {
        public int wrenStep;   // ticket 35: Old Wren's guiding thread (WrenStep)
    }

    public partial class RunState
    {
        public bool firstRidge;   // ticket 35: your first trip to Chili Ridge is one short, easy floor
    }

    public partial class GameSession
    {
        public WrenStep Wren { get { return (WrenStep)Profile.wrenStep; } }
        void SetWren(WrenStep s) { if ((int)s > Profile.wrenStep) Profile.wrenStep = (int)s; }

        /// Wren has something new to say and comes over by herself (at home in town).
        public bool WrenTalkDue
        {
            get
            {
                if (Phase != Phase.Hearth) return false;
                var w = Wren;
                return w == WrenStep.Meet || w == WrenStep.AfterDive || w == WrenStep.RidgeBack || WrenFireHintDue;
            }
        }

        /// Ticket 42: the Route board's meal tray stays hidden until Wren has told you about packing meals.
        public bool TrayShown { get { return !Slice.On || Wren >= WrenStep.FirstBoss; } }

        /// Ticket 40: why you can't set out yet: the tray must be full (any meals, Camp Bread included). Not on the first dive.
        public string TrayBlock
        {
            get
            {
                if (!Slice.On || Profile.arcStage == 0 || !TrayShown) return null;
                int n = Profile.tray.Count, max = Content.tuning.traySize;
                if (n < max) return "Pack " + max + " meals to set out (" + n + "/" + max + ")";
                var missing = Tray.Missing(Content, Profile.tray);   // ticket 44: an old tray of three Health meals, say
                return missing.Count > 0 ? "Your tray needs " + Tray.Need(missing) : null;
            }
        }

        /// What to do next, in one line (town HUD, Wren's reminders).
        public string WrenObjective
        {
            get
            {
                switch (Wren)
                {
                    case WrenStep.Meet: return "👵 Talk to Old Wren";
                    case WrenStep.FirstDive: return "🌼 Head into Meadow Hollow from the 🗺️ Route board by the gate";
                    case WrenStep.AfterDive: return "👵 Talk to Old Wren";
                    case WrenStep.CookMeal: return "🍳 Cook a meal at the cooking area, then show Old Wren";
                    case WrenStep.ShowMeal: return "👵 Show Old Wren the meal you cooked";
                    case WrenStep.FirstBoss: return "⚔️ Pack your meals at the Route board and beat the beast at the bottom of Meadow Hollow";
                    case WrenStep.HearHint: return "👵 Tell Old Wren about the Bramble Sow";
                    case WrenStep.Ridge: return "🌶️ Go to Chili Ridge (Route board, side paths) and bring back chilies";
                    case WrenStep.RidgeBack: return "👵 Show Old Wren the chilies";
                    case WrenStep.SeasonMeal: return "🧂 Season one of your meals with a chili at the spice rack";
                    case WrenStep.Alpha: return "❄️ Take your Fire meal to Frost Hollow and beat the Hoarfrost Alpha at the bottom";
                }
                return "🔥 The Hollow's yours: cook, explore, build up the town";
            }
        }

        /// Wren's words when you talk to her (or she comes over). Moves the thread on where talking is the step.
        public List<string> WrenTalk()
        {
            var pages = new List<string>();
            switch (Wren)
            {
                case WrenStep.Meet:
                    pages.Add("Well now, a new face by my fire! I'm Wren. This was a town once, before the fog came up out of the Hollow and the Hearth went cold. Look at it: barely an ember.");
                    pages.Add("Somebody has to go down there and bring back what the fog took. The Route board by the gate leads into 🌼 Meadow Hollow. The fog's thick today, so don't be a hero: get as far as you can, then come back to me.");
                    Profile.metWren = true;
                    SetWren(WrenStep.FirstDive);
                    break;
                case WrenStep.AfterDive:
                    if (!Profile.metWren)   // ticket 42: you've never met: she found you after the fall
                    {
                        pages.Add("Easy, easy. Don't try to sit up yet. I found you at the edge of the fog, out cold, and carried you up here. I'm Wren.");
                        pages.Add("Purple light, you say? Rooms full of things that kept coming? That's no part of the Hollow I know. Whatever it was, it spat you out half dead, and on an empty stomach too. No wonder it chewed you up.");
                        pages.Add("This was a town once, before the fog came up out of the Hollow and the Hearth went cold. You'll stay a while. And if you're going back down anywhere, you'll eat first.");
                        Profile.metWren = true;
                    }
                    else pages.Add("There you are, and still in one piece, more or less. Sit. Eat. That fog takes it out of you, and you went down on an empty stomach.");
                    pages.Add("That's the trick to the Hollow: food. What you eat before you go down is what keeps you on your feet. The kitchen's yours now. 🍳 Cook something proper at the cooking area, then bring it here and show me.");
                    SetWren(WrenStep.CookMeal);
                    break;
                case WrenStep.ShowMeal:
                    pages.Add("Now that smells like a proper meal! You'll make a cook of yourself yet.");
                    pages.Add("When you set out, pack your meals on the 🧺 tray at the Route board. You eat them at the gate, and they keep you going the whole trip. Better food, deeper floors.");
                    pages.Add("With that in you, go deeper. Something big and bristly snores at the bottom of Meadow Hollow, three floors down. Beat it and the Hollow will start to give a little. And mind: there's no way back up but the bottom.");
                    SetWren(WrenStep.FirstBoss);
                    break;
                case WrenStep.RidgeBack:
                    pages.Add("Chilies! Look at the colour on them. Good work.");
                    pages.Add("Now, don't just eat them raw. Take one to the 🧂 spice rack by the stove and season one of your meals with it. Every hit you land will carry 🔥 Fire, for the whole trip.");
                    pages.Add("That Hoarfrost Alpha can't abide fire. Season a meal, and I'll tell you the way to Frost Hollow."); 
                    SetWren(WrenStep.SeasonMeal);
                    break;
                default:
                    pages.Add(WrenReminder());
                    break;
            }
            Touch(true);
            return pages;
        }

        string WrenReminder()
        {
            switch (Wren)
            {
                case WrenStep.FirstDive: return "The Route board's by the gate, love. Meadow Hollow. Get as far as you can, then come back to me.";
                case WrenStep.CookMeal: return "Cook something at the cooking area first, then bring it to show me.";
                case WrenStep.FirstBoss: return "The beast at the bottom of the Hollow won't beat itself. Pack your meals at the Route board as you set out.";
                case WrenStep.Ridge: return "Chili Ridge is on the Route board, under side paths. Bring me back some chilies.";
                case WrenStep.SeasonMeal: return "The spice rack's right by the stove. Season a meal with a chili.";
                case WrenStep.Alpha: return "That's a meal with some fire in it! The cold up in Frost Hollow can't abide fire. It's on your Route board now: pack your meal and go and beat the Alpha at the bottom.";
                case WrenStep.Done: return "The Alpha's beaten and the Hearth's warming up. Look at you. Keep cooking, keep exploring, and help me build this town back up.";
            }
            return WrenObjective;
        }

        // ───── the thread moves on with what you do ─────
        void WrenOnRunEnd()
        {
            if (Wren == WrenStep.FirstDive) SetWren(WrenStep.AfterDive);
            if (Run != null && Run.routeId == RidgeRoute && Wren == WrenStep.Ridge) SetWren(WrenStep.RidgeBack);   // ticket 47: any trip to the Ridge
        }
        void WrenOnCooked() { if (Wren == WrenStep.CookMeal) SetWren(WrenStep.ShowMeal); }
        void WrenOnBoss(string routeId, int floor)
        {
            if (routeId == MeadowRoute && floor >= MeadowBossFloor && Wren < WrenStep.HearHint) SetWren(WrenStep.HearHint);   // the Bramble Sow
            if (routeId == FrostRoute && floor >= DungeonRules.Floors(Content, FrostRoute)) SetWren(WrenStep.Done);          // the Hoarfrost Alpha
        }
        void WrenOnSeasoned(CookedMeal m)
        {
            if (Wren == WrenStep.SeasonMeal && m != null && Cooking.InfusionOf(Content, m.accelId) == Element.Fire)
            {
                SetWren(WrenStep.Alpha);
                OpenFrostHollow();
            }
        }

        /// Ticket 47: Wren tells you the way: Frost Hollow appears on the Route board.
        void OpenFrostHollow()
        {
            if (Routes.HasFlag(Profile, WrenFrostFlag)) return;
            Routes.SetFlag(Profile, WrenFrostFlag);
            var r = Content.Route(FrostRoute);
            Info("🗺️ " + (r != null ? r.emoji + " " + r.name : "Frost Hollow") + " is on the route board. The Hoarfrost Alpha waits at the bottom.");
        }

        /// saveVersion 15 (ticket 47): the areas got shorter. Meadow's floor-5 boss is now its floor-3 boss, and the
        /// old floor-10 Alpha lives at the bottom of Frost Hollow.
        void UpgradeAreas()
        {
            if (Profile.saveVersion >= 15) return;
            var p = Profile;
            if (p.bossesBeaten == null) p.bossesBeaten = new List<string>();
            int meadowBoss = MeadowBossFloor, frostBoss = DungeonRules.Floors(Content, FrostRoute);
            var mapped = new List<string>();
            foreach (var key in p.bossesBeaten)
            {
                if (key == MeadowRoute + ":5") mapped.Add(MeadowRoute + ":" + meadowBoss);
                else if (key == MeadowRoute + ":10") mapped.Add(FrostRoute + ":" + frostBoss);
                else mapped.Add(key);
            }
            p.bossesBeaten.Clear();
            foreach (var k in mapped) if (!p.bossesBeaten.Contains(k)) p.bossesBeaten.Add(k);
            if (Wren >= WrenStep.Alpha) Routes.SetFlag(p, WrenFrostFlag);
            p.saveVersion = 15;
        }

        /// saveVersion 14: work out where an older save is on the thread.
        void UpgradeWren()
        {
            if (Profile.saveVersion >= 14) return;
            var p = Profile;
            WrenStep s;
            if (Progression.BeatBoss(p, MeadowRoute, 10) || Progression.BeatBoss(p, FrostRoute, DungeonRules.Floors(Content, FrostRoute))) s = WrenStep.Done;
            else if (Progression.DeepestFloor(p, RidgeRoute) > 0) s = WrenStep.SeasonMeal;
            else if (Routes.HasFlag(p, DefaultContent.WrenFireHint)) s = WrenStep.Ridge;
            else if (Progression.BeatBoss(p, MeadowRoute, 5) || Progression.BeatBoss(p, MeadowRoute, MeadowBossFloor)) s = WrenStep.HearHint;
            else if (p.arcStage >= 1) s = WrenStep.FirstBoss;
            else s = p.metWren ? WrenStep.FirstDive : WrenStep.Meet;
            p.wrenStep = (int)s;
            p.saveVersion = 14;
        }
    }
}
