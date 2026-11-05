// Hearth & Hollow — edit-mode tests for ticket 42: a new game opens with a fall into Meadow Hollow.
using HearthHollow.Core;
using HearthHollow.Dungeon;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class TheFallTests
    {
        GameContent _c;
        [SetUp] public void SetUp() { _c = DefaultContent.Create(); Slice.On = true; }

        [Test]
        public void NewGame_FallsIn_WakesInTown_ThenMeetsWren()
        {
            var s = new GameSession(_c, null, 91);
            Assert.IsTrue(s.FallDue);
            Assert.IsTrue(s.BeginTheFall());
            Assert.AreEqual(Phase.Dungeon, s.Phase);
            Assert.IsTrue(s.Run.firstDive);
            Assert.AreEqual(WrenStep.FirstDive, s.Wren);
            Assert.IsFalse(s.Profile.metWren, "you haven't met her yet");
            Assert.IsFalse(s.BeginTheFall(), "only once");

            s.DungeonDamagePlayer(99999f);   // the fog wins
            Assert.AreEqual(Phase.RunEnd, s.Phase);
            s.ReturnToHearth();
            Assert.AreEqual(Phase.Hearth, s.Phase);
            Assert.AreEqual(WrenStep.AfterDive, s.Wren);
            Assert.IsTrue(s.WrenTalkDue, "Wren comes over when you wake");
            Assert.IsFalse(s.TrayShown, "no meal tray yet");
            var pages = s.WrenTalk();
            StringAssert.Contains("I'm Wren", pages[0]);
            Assert.IsTrue(s.Profile.metWren);
            Assert.AreEqual(WrenStep.CookMeal, s.Wren);
            Assert.IsTrue(s.AreaOpen("kitchen"));
            Assert.IsFalse(s.FallDue);
        }

        [Test]
        public void TrayShows_OnceWrenExplainsIt()
        {
            var s = new GameSession(_c, null, 92);
            s.Profile.arcStage = 1;
            s.Profile.wrenStep = (int)WrenStep.ShowMeal;
            Assert.IsFalse(s.TrayShown);
            Assert.IsNull(s.TrayBlock, "no tray, nothing to fill");
            s.Profile.wrenStep = (int)WrenStep.FirstBoss;
            Assert.IsTrue(s.TrayShown);
            Assert.IsNotNull(s.TrayBlock);
        }

        [Test]
        public void TheFall_IsANamelessHiddenDungeon_ThatSpeedsUp()
        {
            var s = new GameSession(_c, null, 94);
            Assert.IsTrue(s.BeginTheFall());
            Assert.AreEqual(GameSession.FallRoute, s.Run.routeId);
            Assert.AreEqual("???", _c.Route(GameSession.FallRoute).name);
            Assert.IsFalse(Slice.ShowsRoute(GameSession.FallRoute), "never on the Route board");
            s.Run.fightRooms = 1;
            Assert.AreEqual(1f, s.FallEnemySpeed, 0.001f);
            var first = s.DungeonEnemy("wolf", false, false);
            s.Run.fightRooms = 2;
            Assert.AreEqual(1f, s.FallEnemySpeed, 0.001f, "ticket 49: rooms 1–2 are the tutorial: no ramp yet");
            s.Run.fightRooms = 6;
            Assert.Greater(s.FallEnemySpeed, 1.3f, "faster each room after that");
            var sixth = s.DungeonEnemy("wolf", false, false);
            Assert.Greater(sixth.damage, first.damage * 4f, "and they hit much harder");
        }

        [Test]
        public void Gauntlet_IsALineOfFights()
        {
            var plan = DungeonGenerator.Gauntlet(5, 14);
            Assert.AreEqual(14, plan.rooms.Count);
            Assert.AreEqual(RoomKind.Start, plan.start.kind);
            for (int i = 1; i < plan.rooms.Count; i++) Assert.AreEqual(RoomKind.Combat, plan.rooms[i].kind, "room " + i);
            Assert.IsTrue(plan.rooms[plan.rooms.Count - 1].fog, "ticket 49: it ends in the fog room, no exit");
            for (int i = 0; i < plan.rooms.Count - 1; i++) Assert.IsFalse(plan.rooms[i].fog);
            foreach (var r in plan.rooms)
            {
                int doors = 0; for (int d = 0; d < 4; d++) if (r.doors[d]) doors++;
                Assert.LessOrEqual(doors, 2, "a single line, no branches");
            }
        }

        [Test]
        public void TheFall_HasNoLoot_AndEndsInAFade_NotASummary()   // ticket 49
        {
            var s = new GameSession(_c, null, 95);
            Assert.IsTrue(s.BeginTheFall());
            Assert.AreEqual(7, _c.tuning.fallRooms, "the wake-up room + 6");
            int coins = s.Run.coinsEarned;
            s.DungeonEnemyKilled(s.DungeonEnemy("slime", false, false));
            Assert.AreEqual(coins, s.Run.coinsEarned, "no coins");
            Assert.AreEqual(0, s.FloorLoot.Count);
            for (int i = 0; i < s.Run.bag.Count; i++) Assert.IsTrue(Bag.IsEmpty(s.Run.bag[i]), "no loot");
            for (int i = 0; i < 40 && s.Phase == Phase.Dungeon; i++) s.FallFogBite();   // the fog room
            Assert.AreEqual(Phase.RunEnd, s.Phase, "the fog always wins");
            Assert.IsTrue(s.FallEnding, "the UI fades to town instead of showing a summary");
            s.ReturnToHearth();
            Assert.IsFalse(s.FallEnding);
            Assert.AreEqual(WrenStep.AfterDive, s.Wren);
        }

        [Test]
        public void NoFall_ForAnOngoingGame()
        {
            var s = new GameSession(_c, null, 93);
            s.Profile.runsStarted = 2;
            Assert.IsFalse(s.FallDue);
        }
    }
}
