// Ticket 64: wolves circle, bats swoop, slimes split into two little ones worth nothing.
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class MonsterHabitsTests
    {
        [Test]
        public void ASlime_SplitsIntoTwoLittleOnes_WorthNothing()
        {
            var slime = new EnemyStats { monsterId = "slime", maxHp = 10, damage = 2f, difficulty = 3 };
            Assert.IsTrue(MonsterHabits.Splits(slime));
            var kid = MonsterHabits.Split(slime);
            Assert.IsTrue(kid.split);
            Assert.Less(kid.maxHp, slime.maxHp);
            Assert.Less(kid.damage, slime.damage);
            Assert.Less(kid.sizeMult, 1f);
            Assert.IsFalse(MonsterHabits.Splits(kid), "little ones don't split again");
            Assert.IsFalse(MonsterHabits.Splits(new EnemyStats { monsterId = "slime", boss = true }), "a slime boss doesn't split");
            Assert.IsFalse(MonsterHabits.Splits(new EnemyStats { monsterId = "wolf" }));
        }

        [Test]
        public void WolvesCircle_BatsSwoop()
        {
            Assert.IsTrue(MonsterHabits.Circles("wolf"));
            Assert.IsFalse(MonsterHabits.Circles("bat"));
            Assert.IsTrue(MonsterHabits.Swoops("bat"));
        }

        [Test]
        public void ALittleSlime_GivesNoCoinsOrLoot()
        {
            var c = DefaultContent.Create();
            var s = new GameSession(c, null, 3);
            s.Profile.selectedClass = ClassId.Cook;
            Assert.IsTrue(s.StartRun("meadow"));
            if (!s.InDungeon) Assert.Ignore("this build's runs aren't dungeon runs");
            int coins = s.Run.coinsEarned, bag = s.Run.bag.Count, kills = s.Run.kills;
            s.DungeonEnemyKilled(MonsterHabits.Split(new EnemyStats { monsterId = "slime", maxHp = 10, damage = 2f, difficulty = 3 }));
            Assert.AreEqual(coins, s.Run.coinsEarned);
            Assert.AreEqual(bag, s.Run.bag.Count);
            Assert.AreEqual(kills + 1, s.Run.kills);
        }
    }
}
