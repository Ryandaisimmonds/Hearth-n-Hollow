// Hearth & Hollow — the story: the opening arc, Old Wren's guiding thread and the opening fall (refactor 16). Pure C#.
// GameSession owns one Story (Session.Story) and calls its On… hooks when things happen (a run ends, a meal is cooked,
// a boss falls). The UI asks it what Wren says next, whether you may set out, and whether the fall is due.
//
// Wren's thread:
//   the fall → [Wren] cook a meal → show it to Wren → beat the Bramble Sow (bottom of Meadow Hollow) →
//   [Wren] Chili Ridge for chilies → [Wren] season a meal with chili at the spice rack → Frost Hollow opens →
//   beat the Hoarfrost Alpha at the bottom → done.
// The fall: a new game doesn't start in town. You wake up in a nameless dungeon ("???", FallRoute):
// a line of fight rooms that get harder (DungeonRules.Enemy, FallEnemySpeed) until you fall, then wake up in town.
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    /// Append only (saved as an int).
    public enum WrenStep { Meet, FirstDive, AfterDive, CookMeal, ShowMeal, FirstBoss, HearHint, Ridge, RidgeBack, SeasonMeal, Alpha, Done }

    public partial class Profile
    {
        public int wrenStep;   // Old Wren's guiding thread (WrenStep)
    }

    public partial class RunState
    {
        public int fightRooms;    // rooms whose fight has started this run (the first dive gets tougher with each)
    }

    public sealed class Story
    {
        public const string MeadowRoute = Ids.Area.Meadow, RidgeRoute = Ids.Area.Chili, FrostRoute = Ids.Area.FrostHollow;
        public const string WrenFrostFlag = Ids.Flag.WrenFrost;   // Wren has told you the way to Frost Hollow
        /// The nameless opening dungeon. Hidden from the Route board; only the fall goes there.
        public const string FallRoute = Ids.Area.Fall;
        public const string FallIntro = "🌀 You wake on cold stone, somewhere strange. Purple light, and no way back the way you came. Something is moving in the next room…";

        readonly GameSession _s;
        OpeningArc _arc = new OpeningArc();

        internal Story(GameSession session) { _s = session; }

        Profile P { get { return _s.Profile; } }
        GameContent C { get { return _s.Content; } }
        RunState Run { get { return _s.Run; } }

        // ───────────────────────── the opening arc (OpeningArc.cs) ─────────────────────────
        /// Set from the Dungeon Settings asset at start-up (GameBootstrap); a default arc otherwise.
        public OpeningArc Arc { get { return _arc; } set { _arc = value ?? new OpeningArc(); } }
        public ArcStage ArcCurrent { get { return HearthHollow.Core.Arc.Current(_arc, P); } }
        public bool ArcFinished { get { return HearthHollow.Core.Arc.Finished(_arc, P); } }
        /// A town area (kitchen, smithy, workshop, …) is open at this stage of the arc.
        public bool AreaOpen(string area) { return HearthHollow.Core.Arc.IsOpen(_arc, P, area); }
        public bool DashUnlocked { get { return HearthHollow.Core.Arc.DashUnlocked(_arc, P, Run != null ? Run.classId : P.selectedClass); } }
        /// This run is the opening's first dive (it gets harder room by room, so it ends in a fall).
        internal bool FirstDive { get { return !HearthHollow.Core.Arc.Finished(_arc, P) && P.arcStage == 0 && _arc.enabled; } }

        internal void OnArcEvent(ArcTrigger t)
        {
            var s = HearthHollow.Core.Arc.Advance(C, _arc, P, t);
            if (s == null) return;
            if (!string.IsNullOrEmpty(s.toast)) _s.Info(s.toast);
            _s.Recompute();
        }

        public void DevSetArcStage(int stage)
        {
            P.arcStage = Math.Max(0, Math.Min(stage, HearthHollow.Core.Arc.LastStage(_arc)));
            var s = ArcCurrent;
            _s.Info("Dev: opening arc → stage " + P.arcStage + (s != null ? " (" + s.name + ")" : ""));
            _s.Touch(true);
        }

        // ───────────────────────── Old Wren's thread ─────────────────────────────────
        public WrenStep Step { get { return (WrenStep)P.wrenStep; } }
        void SetStep(WrenStep s) { if ((int)s > P.wrenStep) P.wrenStep = (int)s; }

        /// Wren has something new to say and comes over by herself (at home in town).
        public bool TalkDue
        {
            get
            {
                if (_s.Phase != Phase.Hearth) return false;
                var w = Step;
                return w == WrenStep.Meet || w == WrenStep.AfterDive || w == WrenStep.RidgeBack || FireHintDue;
            }
        }

        /// Why you can't set out yet because of Old Wren (null = go ahead). Back from the fall she has things to tell
        /// you, then a meal to cook and show her, before you go anywhere. Only in the slice; not on the first dive.
        public string SetOutBlock
        {
            get
            {
                if (!Features.WrenGuides) return null;
                switch (Step)
                {
                    case WrenStep.Meet: case WrenStep.AfterDive: return "Talk to Old Wren first";
                    case WrenStep.CookMeal: return "Cook a meal and show Old Wren first";
                    case WrenStep.ShowMeal: return "Show Old Wren your meal first";
                }
                return TalkDue ? "Old Wren wants a word first" : null;
            }
        }

        /// The Route board's meal tray stays hidden until Wren has told you about packing meals.
        public bool TrayShown { get { return !Features.WrenGuides || Step >= WrenStep.FirstBoss; } }

        /// What to do next, in one line (town HUD, Wren's reminders).
        public string Objective
        {
            get
            {
                switch (Step)
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
        public List<string> Talk()
        {
            var pages = new List<string>();
            switch (Step)
            {
                case WrenStep.Meet:
                    pages.Add("Well now, a new face by my fire! I'm Wren. This was a town once, before the fog came up out of the Hollow and the Hearth went cold. Look at it: barely an ember.");
                    pages.Add("Somebody has to go down there and bring back what the fog took. The Route board by the gate leads into 🌼 Meadow Hollow. The fog's thick today, so don't be a hero: get as far as you can, then come back to me.");
                    P.metWren = true;
                    SetStep(WrenStep.FirstDive);
                    break;
                case WrenStep.AfterDive:
                    if (!P.metWren)   // you've never met: she found you after the fall
                    {
                        pages.Add("Easy, easy. Don't try to sit up yet. I found you at the edge of the fog, out cold, and carried you up here. I'm Wren.");
                        pages.Add("Purple light, you say? Rooms full of things that kept coming? That's no part of the Hollow I know. Whatever it was, it spat you out half dead, and on an empty stomach too. No wonder it chewed you up.");
                        pages.Add("This was a town once, before the fog came up out of the Hollow and the Hearth went cold. You'll stay a while. And if you're going back down anywhere, you'll eat first.");
                        P.metWren = true;
                    }
                    else pages.Add("There you are, and still in one piece, more or less. Sit. Eat. That fog takes it out of you, and you went down on an empty stomach.");
                    pages.Add("That's the trick to the Hollow: food. What you eat before you go down is what keeps you on your feet. The kitchen's yours now. 🍳 Cook something proper at the cooking area, then bring it here and show me.");
                    SetStep(WrenStep.CookMeal);
                    break;
                case WrenStep.ShowMeal:
                    pages.Add("Now that smells like a proper meal! You'll make a cook of yourself yet.");
                    pages.Add("When you set out, pack your meals on the 🧺 tray at the Route board. You eat them at the gate, and they keep you going the whole trip. Better food, deeper floors.");
                    pages.Add("With that in you, go deeper. Something big and bristly snores at the bottom of Meadow Hollow, three floors down. Beat it and the Hollow will start to give a little. And mind: there's no way back up but the bottom.");
                    SetStep(WrenStep.FirstBoss);
                    break;
                case WrenStep.RidgeBack:
                    pages.Add("Chilies! Look at the colour on them. Good work.");
                    pages.Add("Now, don't just eat them raw. Take one to the 🧂 spice rack by the stove and season one of your meals with it. Every hit you land will carry 🔥 Fire, for the whole trip.");
                    pages.Add("That Hoarfrost Alpha can't abide fire. Season a meal, and I'll tell you the way to Frost Hollow.");
                    SetStep(WrenStep.SeasonMeal);
                    break;
                default:
                    pages.Add(Reminder());
                    break;
            }
            _s.Touch(true);
            return pages;
        }

        string Reminder()
        {
            switch (Step)
            {
                case WrenStep.FirstDive: return "The Route board's by the gate, love. Meadow Hollow. Get as far as you can, then come back to me.";
                case WrenStep.CookMeal: return "Cook something at the cooking area first, then bring it to show me.";
                case WrenStep.FirstBoss: return "The beast at the bottom of the Hollow won't beat itself. Pack your meals at the Route board as you set out.";
                case WrenStep.Ridge: return "Chili Ridge is on the Route board, under side paths. Bring me back some chilies.";
                case WrenStep.SeasonMeal: return "The spice rack's right by the stove. Season a meal with a chili.";
                case WrenStep.Alpha: return "That's a meal with some fire in it! The cold up in Frost Hollow can't abide fire. It's on your Route board now: pack your meal and go and beat the Alpha at the bottom.";
                case WrenStep.Done: return "The Alpha's beaten and the Hearth's warming up. Look at you. Keep cooking, keep exploring, and help me build this town back up.";
            }
            return Objective;
        }

        // ───────────────────────── Wren's Fire hint ──────────────────────────────────────────────
        // After you beat the Bramble Sow, the first time you're home Old Wren tells you the Hoarfrost Alpha fears fire
        // and where chilies grow. That teaches its Fire weakness and reveals Chili Ridge. Once only.

        /// Meadow Hollow's boss floor (the Bramble Sow, at the bottom).
        public int MeadowBossFloor { get { return DungeonRules.Floors(C, MeadowRoute); } }

        /// Wren has the Fire hint to give: the Bramble Sow is down and she hasn't told you yet.
        public bool FireHintDue
        {
            get
            {
                return _s.Phase == Phase.Hearth && !Routes.HasFlag(P, DefaultContent.WrenFireHint)
                    && Progression.BeatBoss(P, MeadowRoute, MeadowBossFloor);
            }
        }

        /// The Hoarfrost Alpha, at the bottom of Frost Hollow (it used to be the Meadow's floor-10 boss).
        public string DeepBoss
        {
            get
            {
                string route = C.Route(FrostRoute) != null ? FrostRoute : MeadowRoute;
                return DungeonRules.BossMonster(C, new RunState { routeId = route, floor = DungeonRules.Floors(C, route), dungeon = true });
            }
        }

        public List<string> FireHintPages()
        {
            var boss = C.Monster(DeepBoss);
            string bossName = boss != null ? boss.name : "the beast at the bottom";
            var ridge = C.Route(RidgeRoute);
            string ridgeName = ridge != null ? ridge.name : "Chili Ridge";
            var chili = C.Ingredient(Ids.Ingredient.Chili);
            return new List<string>
            {
                "Back from the Hollow with bramble in your hair! You beat that old Sow at the bottom, didn't you? Good. Then it's time you heard about the cold.",
                "Past the Hollow there's a colder place, Frost Hollow, and the " + bossName + " prowls at the bottom of it. Frost through and through: frost won't touch it, but it can't abide fire. 🔥 Hit it with Fire and it'll feel it, half again.",
                "Fire food's what you want. " + (chili != null ? chili.emoji + " " + chili.name + "s" : "Chilies") + " put Fire in your attacks. They grow wild up on " + ridgeName + ", past the east road. It's a quiet place, not much lives up there, so gather what you can. I've marked it on your route board.",
                "Off you go to Chili Ridge, then. Come back with the chilies and I'll show you what to do with them.",
            };
        }

        /// Wren gives the hint: the boss's Fire weakness is known and Chili Ridge opens. True the first time.
        public bool GiveFireHint()
        {
            if (!FireHintDue) return false;
            Routes.SetFlag(P, DefaultContent.WrenFireHint);
            SetStep(WrenStep.Ridge);
            _s.DiscoverElement(DeepBoss, Element.Fire);
            var ridge = C.Route(RidgeRoute);
            _s.Info("🗺️ " + (ridge != null ? ridge.emoji + " " + ridge.name : "Chili Ridge") + " is on the route board. 🔥 The " + (C.Monster(DeepBoss) != null ? C.Monster(DeepBoss).name : "floor-10 boss") + " is weak to Fire.");
            _s.Touch(true);
            return true;
        }

        /// Wren tells you the way: Frost Hollow appears on the Route board.
        void OpenFrostHollow()
        {
            if (Routes.HasFlag(P, WrenFrostFlag)) return;
            Routes.SetFlag(P, WrenFrostFlag);
            var r = C.Route(FrostRoute);
            _s.Info("🗺️ " + (r != null ? r.emoji + " " + r.name : "Frost Hollow") + " is on the route board. The Hoarfrost Alpha waits at the bottom.");
        }

        // ───────────────────────── the opening fall ──────────────────────────────────────────────
        /// True while BeginFall is starting the run (the run log opens with FallIntro).
        internal bool Falling { get; private set; }

        /// In the opening fall's nameless dungeon right now.
        public bool InFall { get { return Run != null && Run.routeId == FallRoute; } }

        /// The fall has just ended: the UI fades to black and wakes you in town (no run summary).
        public bool FallEnding { get; private set; }

        /// × monster move speed on the first dive: faster with every room you fight in (so they reach you).
        public float FallEnemySpeed
        {
            get
            {
                if (Run == null || !Run.firstDive) return 1f;
                int k = DungeonRules.FirstDiveRamp(Run);
                return Math.Min(C.tuning.firstDiveSpeedMax, 1f + k * C.tuning.firstDiveSpeedPerRoom);
            }
        }

        /// The fog room. It drains you, a bite at a time, until you fall.
        public void FallFogBite()
        {
            if (!_s.InDungeon || !InFall || Run.hp <= 0) return;
            var st = _s.Stats;
            _s.DungeonDamagePlayer(Math.Max(2f, st.maxHp * C.tuning.fallFogBitePct) / Math.Max(0.1f, 1f - st.armourPct), "🌫️ The fog");   // armour doesn't help against the fog
        }

        /// A brand-new game in the slice: nothing done yet, so it opens in the dungeon.
        public bool FallDue
        {
            get { return Features.OpensWithFall && _s.Phase == Phase.Hearth && Step == WrenStep.Meet && P.runsStarted == 0 && P.arcStage == 0; }
        }

        /// Start the opening fall (no town, no Wren, no meals yet). False if it isn't due.
        public bool BeginFall()
        {
            if (!FallDue) return false;
            if (!_s.ClassAvailable(P.selectedClass) && Slice.Classes.Length > 0) P.selectedClass = Slice.Classes[0];
            P.tray.Clear();
            Falling = true;
            bool ok;
            try { ok = _s.StartRun(C.Route(FallRoute) != null ? FallRoute : MeadowRoute); }
            finally { Falling = false; }
            if (ok) SetStep(WrenStep.FirstDive);
            return ok;
        }

        // ───────────────────────── hooks: the story moves on with what you do ─────────────────────────
        /// A run has ended (before the summary): the arc, Wren and the fall each take note.
        internal void OnRunEnd()
        {
            OnArcEvent(ArcTrigger.FirstRunEnd);   // opening arc: home from the first dive (fallen or not), Wren feeds you, the kitchen opens
            if (Step == WrenStep.FirstDive) SetStep(WrenStep.AfterDive);   // Wren has something to say when you're home
            if (Run != null && Run.routeId == RidgeRoute && Step == WrenStep.Ridge) SetStep(WrenStep.RidgeBack);   // any trip to the Ridge
            if (Run != null && Run.routeId == FallRoute) FallEnding = true;   // the fall fades straight to town
        }

        /// Back in town after a run: the fall's fade is over, the arc may grow the town, and the first time you wake up there.
        internal void OnHome()
        {
            FallEnding = false;
            if (P.bossesBeaten != null && P.bossesBeaten.Count > 0) OnArcEvent(ArcTrigger.HomeAfterBoss);   // opening arc: home after your first boss, the camp grows into a town
            if (Features.OpensWithFall && Step == WrenStep.AfterDive && !P.metWren)   // waking up in town after the opening fall
                _s.Info("🔥 You wake up by a little fire, in a town you've never seen. Someone carried you out of the fog.");
        }

        internal void OnCooked() { if (Step == WrenStep.CookMeal) SetStep(WrenStep.ShowMeal); }

        internal void OnBoss(string routeId, int floor)
        {
            if (routeId == MeadowRoute && floor >= MeadowBossFloor && Step < WrenStep.HearHint) SetStep(WrenStep.HearHint);   // the Bramble Sow
            if (routeId == FrostRoute && floor >= DungeonRules.Floors(C, FrostRoute)) SetStep(WrenStep.Done);              // the Hoarfrost Alpha
        }

        internal void OnSeasoned(CookedMeal m)
        {
            if (Step == WrenStep.SeasonMeal && m != null && Cooking.InfusionOf(C, m.accelId) == Element.Fire)
            {
                SetStep(WrenStep.Alpha);
                OpenFrostHollow();
            }
        }

        // ───────────────────────── old saves ─────────────────────────
        /// saveVersion 14 and 15: where an older save is on Wren's thread, and the shorter areas.
        internal void UpgradeSave()
        {
            UpgradeWren();
            UpgradeAreas();
        }

        /// saveVersion 14: work out where an older save is on the thread.
        void UpgradeWren()
        {
            if (P.saveVersion >= 14) return;
            var p = P;
            WrenStep s;
            if (Progression.BeatBoss(p, MeadowRoute, 10) || Progression.BeatBoss(p, FrostRoute, DungeonRules.Floors(C, FrostRoute))) s = WrenStep.Done;
            else if (Progression.DeepestFloor(p, RidgeRoute) > 0) s = WrenStep.SeasonMeal;
            else if (Routes.HasFlag(p, DefaultContent.WrenFireHint)) s = WrenStep.Ridge;
            else if (Progression.BeatBoss(p, MeadowRoute, 5) || Progression.BeatBoss(p, MeadowRoute, MeadowBossFloor)) s = WrenStep.HearHint;
            else if (p.arcStage >= 1) s = WrenStep.FirstBoss;
            else s = p.metWren ? WrenStep.FirstDive : WrenStep.Meet;
            p.wrenStep = (int)s;
            p.saveVersion = 14;
        }

        /// saveVersion 15: the areas got shorter. Meadow's floor-5 boss is now its floor-3 boss, and the
        /// old floor-10 Alpha lives at the bottom of Frost Hollow.
        void UpgradeAreas()
        {
            if (P.saveVersion >= 15) return;
            var p = P;
            if (p.bossesBeaten == null) p.bossesBeaten = new List<string>();
            int meadowBoss = MeadowBossFloor, frostBoss = DungeonRules.Floors(C, FrostRoute);
            var mapped = new List<string>();
            foreach (var key in p.bossesBeaten)
            {
                if (key == MeadowRoute + ":5") mapped.Add(MeadowRoute + ":" + meadowBoss);
                else if (key == MeadowRoute + ":10") mapped.Add(FrostRoute + ":" + frostBoss);
                else mapped.Add(key);
            }
            p.bossesBeaten.Clear();
            foreach (var k in mapped) if (!p.bossesBeaten.Contains(k)) p.bossesBeaten.Add(k);
            if (Step >= WrenStep.Alpha) Routes.SetFlag(p, WrenFrostFlag);
            p.saveVersion = 15;
        }
    }
}
