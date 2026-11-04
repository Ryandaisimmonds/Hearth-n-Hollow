// Hearth & Hollow — Old Wren's guiding thread through the slice (ticket 35). Pure C#.
// Wren tells you what to do next at every step, and comes over by herself when she has something new to say:
//   Meet → Meadow Hollow (first dive) → [Wren] cook a meal → show it to Wren → beat the floor-5 boss →
//   [Wren] Chili Ridge (a short, easy first trip) → [Wren] season a meal with chili at the spice rack →
//   take Fire food down to the Hoarfrost Alpha → done.
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
                    case WrenStep.FirstBoss: return "⚔️ Pack your meals at the Route board and beat the beast on Meadow Hollow floor 5";
                    case WrenStep.HearHint: return "👵 Tell Old Wren about the Bramble Sow";
                    case WrenStep.Ridge: return "🌶️ Go to Chili Ridge (Route board, side paths) and bring back chilies";
                    case WrenStep.RidgeBack: return "👵 Show Old Wren the chilies";
                    case WrenStep.SeasonMeal: return "🧂 Season one of your meals with a chili at the spice rack";
                    case WrenStep.Alpha: return "❄️ Take your Fire meal down to the Hoarfrost Alpha, Meadow Hollow floor 10";
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
                    pages.Add("There you are, and still in one piece, more or less. Sit. Eat. That fog takes it out of you, and you went down on an empty stomach.");
                    pages.Add("That's the trick to the Hollow: food. What you eat before you go down is what keeps you on your feet. The kitchen's yours now. 🍳 Cook something proper at the cooking area, then bring it here and show me.");
                    SetWren(WrenStep.CookMeal);
                    break;
                case WrenStep.ShowMeal:
                    pages.Add("Now that smells like a proper meal! You'll make a cook of yourself yet.");
                    pages.Add("When you set out, pack your meals on the 🧺 tray at the Route board. You eat them at the gate, and they keep you going the whole trip. Better food, deeper floors.");
                    pages.Add("With that in you, go deeper. Something big and bristly snores on the fifth floor of Meadow Hollow. Beat it and the Hollow will start to give a little. And mind: there's no easy way back up once you're down there.");
                    SetWren(WrenStep.FirstBoss);
                    break;
                case WrenStep.RidgeBack:
                    pages.Add("Chilies! Look at the colour on them. Good work.");
                    pages.Add("Now, don't just eat them raw. Take one to the 🧂 spice rack by the stove and season one of your meals with it. Every hit you land will carry 🔥 Fire, for the whole trip.");
                    pages.Add("That Hoarfrost Alpha at the bottom of the Hollow can't abide fire. Season a meal, pack it, and go and show it who's boss. You can start from floor 6 now you've beaten the Sow.");
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
                case WrenStep.FirstBoss: return "The beast on floor 5 won't beat itself. Pack your meals at the Route board as you set out.";
                case WrenStep.Ridge: return "Chili Ridge is on the Route board, under side paths. Bring me back some chilies.";
                case WrenStep.SeasonMeal: return "The spice rack's right by the stove. Season a meal with a chili.";
                case WrenStep.Alpha: return "That's a meal with some fire in it! Pack it at the Route board and take it down to floor 10. Start from floor 6 to save your legs.";
                case WrenStep.Done: return "The Alpha's beaten and the Hearth's warming up. Look at you. Keep cooking, keep exploring, and help me build this town back up.";
            }
            return WrenObjective;
        }

        // ───── the thread moves on with what you do ─────
        void WrenOnRunEnd()
        {
            if (Wren == WrenStep.FirstDive) SetWren(WrenStep.AfterDive);
            if (Run != null && Run.firstRidge && Wren <= WrenStep.Ridge && Wren >= WrenStep.FirstBoss) SetWren(WrenStep.RidgeBack);
        }
        void WrenOnCooked() { if (Wren == WrenStep.CookMeal) SetWren(WrenStep.ShowMeal); }
        void WrenOnBoss(string routeId, int floor)
        {
            if (routeId == MeadowRoute && floor == 5 && Wren < WrenStep.HearHint) SetWren(WrenStep.HearHint);
            if (routeId == MeadowRoute && floor >= DungeonRules.Floors(Content, MeadowRoute)) SetWren(WrenStep.Done);
        }
        void WrenOnSeasoned(CookedMeal m)
        {
            if (Wren == WrenStep.SeasonMeal && m != null && Cooking.InfusionOf(Content, m.accelId) == Element.Fire) SetWren(WrenStep.Alpha);
        }

        /// saveVersion 14: work out where an older save is on the thread.
        void UpgradeWren()
        {
            if (Profile.saveVersion >= 14) return;
            var p = Profile;
            WrenStep s;
            if (Progression.BeatBoss(p, MeadowRoute, DungeonRules.Floors(Content, MeadowRoute))) s = WrenStep.Done;
            else if (Progression.DeepestFloor(p, RidgeRoute) > 0) s = WrenStep.SeasonMeal;
            else if (Routes.HasFlag(p, DefaultContent.WrenFireHint)) s = WrenStep.Ridge;
            else if (Progression.BeatBoss(p, MeadowRoute, 5)) s = WrenStep.HearHint;
            else if (p.arcStage >= 1) s = WrenStep.FirstBoss;
            else s = p.metWren ? WrenStep.FirstDive : WrenStep.Meet;
            p.wrenStep = (int)s;
            p.saveVersion = 14;
        }
    }
}
