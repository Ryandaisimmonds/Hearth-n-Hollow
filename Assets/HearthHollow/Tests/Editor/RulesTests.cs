// Hearth & Hollow — edit-mode tests: the game rules (bag, pantry, levels, cooking rolls, the session flow).
using HearthHollow.Core;
using NUnit.Framework;
using UnityEngine;

namespace HearthHollow.Tests
{
    public class RulesTests
    {
        GameContent _c;

        [SetUp]
        public void SetUp() { _c = DefaultContent.Create(); }

        // ───── bag ─────
        [Test]
        public void Bag_StacksUpToTheStackSize_AndReturnsWhatDidntFit()
        {
            var run = new RunState();
            Bag.Init(run, 3);
            Assert.AreEqual(0, Bag.Add(run, "herb", 12, 5));        // 5 + 5 + 2
            Assert.AreEqual(0, Bag.FreeSlots(run));
            Assert.AreEqual(2, run.bag[2].count);
            Assert.AreEqual(2, Bag.Add(run, "herb", 5, 5));         // only 3 more fit
            Assert.IsTrue(Bag.IsFull(run, 5));
        }

        [Test]
        public void Bag_IngredientsHaveNoGrades_TheSameOneStacks()
        {
            var run = new RunState();
            Bag.Init(run, 2);
            Bag.Add(run, "herb", 1, 5, 0);
            Bag.Add(run, "herb", 1, 5, 2);   // an old caller passing a grade: ignored (Design v2)
            Assert.AreEqual(1, Bag.FreeSlots(run), "herbs are herbs: they stack");
        }

        [Test]
        public void Bag_DiscardEmptiesTheSlot()
        {
            var run = new RunState();
            Bag.Init(run, 2);
            Bag.Add(run, "herb", 3, 5);
            Bag.Discard(run, 0);
            Assert.IsTrue(Bag.IsEmpty(run.bag[0]));
            Assert.AreEqual(2, Bag.FreeSlots(run));
        }

        // ───── pantry and levels ─────
        [Test]
        public void Pantry_AddAndRemove()
        {
            var p = Economy.NewProfile(_c);
            string id = _c.ingredients[0].id;
            int start = Economy.PantryCount(p, id);
            Economy.AddToPantry(p, id, 4);
            Assert.AreEqual(start + 4, Economy.PantryCount(p, id));
            Assert.IsFalse(Economy.RemoveFromPantry(p, id, start + 5), "removed more than there was");
            Assert.AreEqual(start + 4, Economy.PantryCount(p, id), "a failed remove changed the pantry");
            Assert.IsTrue(Economy.RemoveFromPantry(p, id, start + 4));
            Assert.AreEqual(0, Economy.PantryCount(p, id));
        }

        [Test]
        public void Loadout_NoLevels_EveryOwnedSkillCanGoInEitherSkillSlot()
        {
            var s = new GameSession(_c, null, 1);
            foreach (var cd in _c.classes)
            {
                Assert.IsNotNull(s.Loadout(cd.id)[0], cd.id + " has no attack equipped");
                foreach (var m in s.SlotChoices(cd.id, LoadSlot.Skill2))
                {
                    if (m.owner != cd.id || m.id == s.Profile.Progress(cd.id).loadSkill1) continue;
                    if (!Progression.Owns(_c, s.Profile, cd.id, m)) continue;   // ticket 10: locked pool moves are learned first
                    Assert.IsNull(s.EquipBlock(cd.id, LoadSlot.Skill2, m.id), cd.id + " can't put " + m.id + " in the L slot");
                }
            }
        }

        [Test]
        public void NewProfile_HasEveryClass_AndStarterCoins()
        {
            var s = new GameSession(_c, null, 1);
            Assert.AreEqual(_c.classes.Count, s.Profile.classes.Count);
            Assert.AreEqual(_c.starterCoins, s.Profile.coins);
        }

        // ───── cooking rolls ─────
        [Test]
        public void CookingRoll_StaysInRange()
        {
            var odds = new[] { 0.6f, 0.3f, 0.1f };
            Assert.AreEqual(0, Cooking.Roll(odds, 0f));
            Assert.AreEqual(0, Cooking.Roll(odds, 0.59f));
            Assert.AreEqual(1, Cooking.Roll(odds, 0.61f));
            Assert.AreEqual(2, Cooking.Roll(odds, 0.95f));
            Assert.AreEqual(2, Cooking.Roll(odds, 0.99999f));
        }

        [Test]
        public void ArmourNeverHealsOrAmplifies()
        {
            foreach (var armour in new[] { 0f, 0.25f, 0.5f, 0.9f, 1f })
            {
                float dmg = Combat.ApplyArmour(10f, armour);
                Assert.That(dmg, Is.InRange(0f, 10f), "armour " + armour);
            }
        }

        // ───── session flow ─────
        [Test]
        public void SameSeed_GivesTheSameCamp()
        {
            var a = new GameSession(DefaultContent.Create(), null, 42);
            var b = new GameSession(DefaultContent.Create(), null, 42);
            Assert.AreEqual(JsonUtility.ToJson(a.Hearth), JsonUtility.ToJson(b.Hearth));
        }

        [Test]
        public void StartRun_LeavesTheHearth_OnAnUnlockedRoute()
        {
            var s = new GameSession(_c, null, 1);
            var route = _c.routes.Find(r => s.IsRouteUnlocked(r.id));
            Assert.IsNotNull(route, "a new player has no route to play");
            Assert.IsTrue(s.StartRun(route.id), "couldn't start " + route.id);
            Assert.AreNotEqual(Phase.Hearth, s.Phase);
            Assert.IsNotNull(s.Run);
        }

        [Test]
        public void LockedRoute_CantBeStarted()
        {
            var s = new GameSession(_c, null, 1);
            var locked = _c.routes.Find(r => !s.IsRouteUnlocked(r.id));
            if (locked == null) Assert.Ignore("every route is unlocked from the start");
            Assert.IsFalse(s.StartRun(locked.id));
            Assert.AreEqual(Phase.Hearth, s.Phase);
        }

        [Test]
        public void DevMaxEverything_MaxesClassesGearKitchenAndTray()
        {
            var s = new GameSession(_c, null, 1);
            s.DevMaxEverything();
            foreach (var cd in _c.classes)
            {
                Assert.IsTrue(s.ClassJoined(cd.id), cd.id + " hasn't joined");
                foreach (var slot in Gear.Shop)
                    Assert.IsNull(Gear.Next(_c, s.Profile, slot, cd.id), cd.id + " " + slot + " isn't maxed");
            }
            Assert.AreEqual(_c.tuning.traySize, s.Profile.packed.Count);
            foreach (var m in s.Profile.packed) Assert.AreEqual(Cooking.Grades - 1, m.grade, "tray meals should be Prime");
            Assert.AreEqual(s.Profile.packed.Count, s.Profile.tray.Count);
            Assert.AreEqual(_c.trinkets.Count, s.Profile.trinkets.Count);
        }

        [Test]
        public void ChangingClass_AsksForASave()
        {
            var s = new GameSession(_c, null, 1);
            int saves = 0;
            s.ProfileSaveRequested += () => saves++;
            s.SelectClass(s.Profile.selectedClass);
            Assert.AreEqual(1, saves);
        }
    }
}
