// Hearth & Hollow — ticket 42/43: the game opens with a fall. Pure C#.
// A new game doesn't start in town: you wake up in a strange, nameless dungeon ("???", route FallRoute): a straight line
// of rooms that are nothing but fights (DungeonGenerator.Gauntlet). Every room's monsters hit harder and move faster
// than the last (DungeonRules.Enemy, FallEnemySpeed), so it ends in a fall. You wake up in town for the first time,
// and Old Wren comes over: she found you, and she tells you about food.
namespace HearthHollow.Core
{
    public partial class RunState
    {
        public int fightRooms;   // ticket 42: rooms whose fight has started this run (the first dive gets tougher with each)
    }

    public partial class GameSession
    {
        bool _falling;

        /// Ticket 43: the nameless opening dungeon. Hidden from the Route board; only the fall goes there.
        public const string FallRoute = "nowhere";

        /// Ticket 43: × monster move speed on the first dive: faster with every room you fight in (so they reach you).
        public float FallEnemySpeed
        {
            get
            {
                if (Run == null || !Run.firstDive) return 1f;
                int k = System.Math.Max(0, Run.fightRooms - 1);
                return System.Math.Min(T.firstDiveSpeedMax, 1f + k * T.firstDiveSpeedPerRoom);
            }
        }

        /// A brand-new game in the slice: nothing done yet, so it opens in the dungeon.
        public bool FallDue
        {
            get { return Slice.On && UseDungeon && Phase == Phase.Hearth && Wren == WrenStep.Meet && Profile.runsStarted == 0 && Profile.arcStage == 0; }
        }

        /// Start the opening fall into Meadow Hollow (no town, no Wren, no meals yet). False if it isn't due.
        public bool BeginTheFall()
        {
            if (!FallDue) return false;
            if (!ClassAvailable(Profile.selectedClass) && Slice.Classes.Length > 0) Profile.selectedClass = Slice.Classes[0];
            Profile.tray.Clear();
            _falling = true;
            bool ok;
            try { ok = StartRun(Content.Route(FallRoute) != null ? FallRoute : MeadowRoute); }
            finally { _falling = false; }
            if (ok) SetWren(WrenStep.FirstDive);
            return ok;
        }

        public const string FallIntro = "🌀 You wake on cold stone, somewhere strange. Purple light, and no way back the way you came. Something is moving in the next room…";

        /// Called on the way home after a run (ReturnToHearth): the first time, you wake up in town.
        void WrenOnHome()
        {
            if (Slice.On && Wren == WrenStep.AfterDive && !Profile.metWren)
                Info("🔥 You wake up by a little fire, in a town you've never seen. Someone carried you out of the fog.");
        }
    }
}
