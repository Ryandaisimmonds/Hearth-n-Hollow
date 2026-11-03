// Hearth & Hollow — edit-mode tests for ticket 20: the run log.
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class RunLogTests
    {
        GameContent _c;
        [SetUp] public void SetUp() { _c = DefaultContent.Create(); }

        GameSession Fresh() { var s = new GameSession(_c, null, 41); s.Profile.selectedClass = ClassId.Cook; s.Profile.arcStage = 1; return s; }   // past the first dive

        [Test]
        public void Log_RecordsTheRun_AndStartsFreshEachRun()
        {
            var s = Fresh();
            s.Log("before any run");
            Assert.AreEqual(0, s.RunLog.Count, "nothing is logged outside a run");
            Assert.IsTrue(s.StartRun("meadow"));
            Assert.Greater(s.RunLog.Count, 0, "setting out is logged");
            s.DungeonDescend();
            var lines = new System.Collections.Generic.List<RunLogLine>(s.RunLog);
            Assert.IsTrue(lines.Exists(l => l.floor == 2 && l.text.ToLower().Contains("floor 2")), "the ladder down is logged");
            Assert.IsTrue(lines.Exists(l => l.text.Contains("Butter Slide")), "an unlocked move is logged");
            s.Log("🔥 Fog Wolf is weak to Fire"); s.Log("🔥 Fog Wolf is weak to Fire");
            Assert.AreEqual("🔥 Fog Wolf is weak to Fire", s.RunLog[s.RunLog.Count - 1].text);
            Assert.AreNotEqual(s.RunLog[s.RunLog.Count - 2].text, s.RunLog[s.RunLog.Count - 1].text, "repeats aren't logged twice");
            s.DungeonExit(); s.ReturnToHearth();
            Assert.IsTrue(s.StartRun("meadow"));
            Assert.IsFalse(new System.Collections.Generic.List<RunLogLine>(s.RunLog).Exists(l => l.text.Contains("Fog Wolf")), "a new run starts a new log");
        }

        [Test]
        public void Log_IsNotSaved()
        {
            var s = Fresh();
            s.StartRun("meadow");
            s.Log("secret");
            Assert.IsFalse(UnityEngine.JsonUtility.ToJson(s.Profile).Contains("secret"));
        }
    }
}
