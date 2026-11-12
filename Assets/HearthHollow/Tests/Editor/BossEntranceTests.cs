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
            Assert.AreEqual(0f, DungeonController.IntroPush(-1f));
            Assert.AreEqual(0f, DungeonController.IntroPush(0f), 1e-4);
            Assert.Greater(DungeonController.IntroPush(0.3f), 0f);
            Assert.AreEqual(1f, DungeonController.IntroPush(DungeonHud.IntroLength * 0.5f), 1e-4);
            Assert.Less(DungeonController.IntroPush(DungeonHud.IntroLength - 0.2f), 1f);
            Assert.AreEqual(0f, DungeonController.IntroPush(DungeonHud.IntroLength + 1f));
        }
    }
}
