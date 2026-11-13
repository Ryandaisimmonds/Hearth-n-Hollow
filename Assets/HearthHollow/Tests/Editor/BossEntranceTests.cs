// Ticket 57: the camera pushes in on a boss and eases back out.
using HearthHollow.Dungeon;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class BossEntranceTests
    {
        [Test]
        public void ThePushIn_RisesHoldsAndEasesBack()
        {
            Assert.AreEqual(0f, BossIntro.PushAt(-1f));
            Assert.AreEqual(0f, BossIntro.PushAt(0f), 1e-4);
            Assert.Greater(BossIntro.PushAt(0.3f), 0f);
            Assert.AreEqual(1f, BossIntro.PushAt(DungeonHudState.IntroLength * 0.5f), 1e-4);
            Assert.Less(BossIntro.PushAt(DungeonHudState.IntroLength - 0.2f), 1f);
            Assert.AreEqual(0f, BossIntro.PushAt(DungeonHudState.IntroLength + 1f));
        }
    }
}
