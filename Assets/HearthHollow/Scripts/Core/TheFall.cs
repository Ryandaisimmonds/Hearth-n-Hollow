// Hearth & Hollow — ticket 42: the game opens with a fall. Pure C#.
// A new game doesn't start in town: you slip at the edge of the Hollow and wake in the fog of Meadow Hollow. Every room
// you fight through is tougher than the last (DungeonRules.Enemy), so it ends in a fall. You wake up in town for the
// first time, and Old Wren comes over: she found you, and she tells you about food.
namespace HearthHollow.Core
{
    public partial class RunState
    {
        public int fightRooms;   // ticket 42: rooms whose fight has started this run (the first dive gets tougher with each)
    }

    public partial class GameSession
    {
        bool _falling;

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
            try { ok = StartRun(MeadowRoute); }
            finally { _falling = false; }
            if (ok) SetWren(WrenStep.FirstDive);
            return ok;
        }

        public const string FallIntro = "🌫️ The mossy bank gives way and you tumble down into the fog. Meadow Hollow. No way back up: find a way out!";

        /// Called on the way home after a run (ReturnToHearth): the first time, you wake up in town.
        void WrenOnHome()
        {
            if (Slice.On && Wren == WrenStep.AfterDive && !Profile.metWren)
                Info("🔥 You wake up by a little fire, in a town you've never seen. Someone carried you out of the fog.");
        }
    }
}
