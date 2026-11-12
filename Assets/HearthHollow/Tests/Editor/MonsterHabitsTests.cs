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
            var c = DefaultContent.Create();
            var slime = new EnemyStats { monsterId = Ids.Monster.Slime, maxHp = 10, damage = 2f, difficulty = 3 };
            Assert.IsTrue(MonsterHabits.Splits(c.Monster(Ids.Monster.Slime), slime));
            var kid = MonsterHabits.Split(slime);
            Assert.IsTrue(kid.split);
            Assert.Less(kid.maxHp, slime.maxHp);
            Assert.Less(kid.damage, slime.damage);
            Assert.Less(kid.sizeMult, 1f);
            Assert.IsFalse(MonsterHabits.Splits(c.Monster(Ids.Monster.Slime), kid), "little ones don't split again");
            Assert.IsFalse(MonsterHabits.Splits(c.Monster(Ids.Monster.Slime), new EnemyStats { monsterId = Ids.Monster.Slime, boss = true }), "a slime boss doesn't split");
            Assert.IsFalse(MonsterHabits.Splits(c.Monster(Ids.Monster.Wolf), new EnemyStats { monsterId = Ids.Monster.Wolf }));
        }

        [Test]
        public void WolvesCircle_BatsSwoop()
        {
            var c = DefaultContent.Create();
            Assert.IsTrue(c.Monster(Ids.Monster.Wolf).circles);
            Assert.IsFalse(c.Monster(Ids.Monster.Bat).circles);
            Assert.IsTrue(c.Monster(Ids.Monster.Bat).swoops);
            Assert.IsTrue(c.Monster(Ids.Monster.Slime).splits);
        }

        [Test]
        public void ALittleSlime_GivesNoCoinsOrLoot()
        {
            var c = DefaultContent.Create();
            var s = new GameSession(c, null, 3);
            s.Profile.selectedClass = ClassId.Cook;
            Assert.IsTrue(s.StartRun(Ids.Area.Meadow));
            if (!s.InDungeon) Assert.Ignore("this build's runs aren't dungeon runs");
            int coins = s.Run.coinsEarned, bag = s.Run.bag.Count, kills = s.Run.kills;
            s.DungeonEnemyKilled(MonsterHabits.Split(new EnemyStats { monsterId = Ids.Monster.Slime, maxHp = 10, damage = 2f, difficulty = 3 }));
            Assert.AreEqual(coins, s.Run.coinsEarned);
            Assert.AreEqual(bag, s.Run.bag.Count);
            Assert.AreEqual(kills + 1, s.Run.kills);
        }
    }
}
