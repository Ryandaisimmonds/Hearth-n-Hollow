// Ticket 65: the run remembers its last few hits, and a death's summary shows them.
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class DeathRecapTests
    {
        [Test]
        public void TheLastFewHits_AreKept_NewestLast()
        {
            var run = new RunState { hp = 50 };
            for (int i = 1; i <= 7; i++) { run.hp -= i; DeathRecap.Note(run, "wolf " + i, i); }
            Assert.AreEqual(DeathRecap.Keep, run.lastHits.Count);
            Assert.AreEqual("wolf 7", DeathRecap.Killer(run.lastHits));
            Assert.AreEqual("wolf 3", run.lastHits[0].who);
            DeathRecap.Note(run, null, 0);
            Assert.AreEqual("wolf 7", DeathRecap.Killer(run.lastHits), "no damage, no note");
        }

        [Test]
        public void ADeath_ShowsWhatGotYou()
        {
            var c = DefaultContent.Create();
            var s = new GameSession(c, null, 5);
            s.Profile.selectedClass = ClassId.Cook;
            Assert.IsTrue(s.StartRun("meadow"));
            if (!s.InDungeon) Assert.Ignore("this build's runs aren't dungeon runs");
            int guard = 0;
            while (s.InDungeon && guard++ < 200) s.DungeonDamagePlayer(5f, "🐺 Fog Wolf");
            Assert.IsNotNull(s.LastSummary);
            Assert.AreEqual(RunOutcome.Died, s.LastSummary.outcome);
            Assert.Greater(s.LastSummary.lastHits.Count, 0);
            Assert.AreEqual("🐺 Fog Wolf", DeathRecap.Killer(s.LastSummary.lastHits));
            int total; Assert.AreEqual("🐺 Fog Wolf", DeathRecap.Worst(s.LastSummary.lastHits, out total));
        }
    }
}
