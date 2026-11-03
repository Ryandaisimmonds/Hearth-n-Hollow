// Hearth & Hollow — the run log (ticket 20): everything worth knowing that pops up in the dungeon is also written
// here, so nothing is missed. Shown in the Esc menu during a run, newest first. Cleared each run; never saved.
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public class RunLogLine { public int floor; public string text; }

    public partial class GameSession
    {
        readonly List<RunLogLine> _runLog = new List<RunLogLine>();
        const int RunLogMax = 200;

        /// This run's log, oldest first.
        public IList<RunLogLine> RunLog { get { return _runLog; } }

        /// Write a line to the run log (the Unity side calls this for its own pop-ups: discoveries, guards…).
        public void Log(string text)
        {
            if (Run == null || string.IsNullOrEmpty(text)) return;
            if (_runLog.Count > 0 && _runLog[_runLog.Count - 1].text == text && _runLog[_runLog.Count - 1].floor == Run.floor) return;   // no spam from repeats
            _runLog.Add(new RunLogLine { floor = Run.dungeon ? Run.floor : 0, text = text });
            if (_runLog.Count > RunLogMax) _runLog.RemoveAt(0);
        }

        void ClearRunLog() { _runLog.Clear(); }

        /// Which session events go in the log (story, loot, meals, combos, the run's end — not every coin or hit).
        static bool Loggable(GameEventType t)
        {
            switch (t)
            {
                case GameEventType.Info: case GameEventType.Loot: case GameEventType.MealEaten: case GameEventType.Synergy:
                case GameEventType.BagFull: case GameEventType.RunCleared: case GameEventType.RunFled: case GameEventType.PlayerDied:
                    return true;
            }
            return false;
        }
    }
}
