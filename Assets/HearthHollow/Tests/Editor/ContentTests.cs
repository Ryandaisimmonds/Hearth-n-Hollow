// Hearth & Hollow — edit-mode tests: the default content and the content asset are complete and consistent.
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HearthHollow.Tests
{
    public class ContentTests
    {
        GameContent _c;

        [SetUp]
        public void SetUp() { _c = DefaultContent.Create(); }

        static void NoDuplicates(IEnumerable<string> ids, string what)
        {
            var seen = new HashSet<string>();
            foreach (var id in ids)
            {
                Assert.IsFalse(string.IsNullOrEmpty(id), what + " with an empty id");
                Assert.IsTrue(seen.Add(id), "duplicate " + what + " id: " + id);
            }
        }

        [Test]
        public void DefaultContent_IsAtTheLatestVersion()
        {
            Assert.AreEqual(DefaultContent.LatestContentVersion, _c.contentVersion);
            Assert.IsFalse(DefaultContent.Upgrade(_c), "Upgrade should do nothing on up-to-date content");
        }

        [Test]
        public void Ids_AreUnique()
        {
            NoDuplicates(_c.moves.ConvertAll(m => m.id), "move");
            NoDuplicates(_c.ingredients.ConvertAll(i => i.id), "ingredient");
            NoDuplicates(_c.recipes.ConvertAll(r => r.id), "recipe");
            NoDuplicates(_c.monsters.ConvertAll(m => m.id), "monster");
            NoDuplicates(_c.routes.ConvertAll(r => r.id), "route");
            NoDuplicates(_c.skillNodes.ConvertAll(n => n.id), "skill node");
            NoDuplicates(_c.trinkets.ConvertAll(t => t.id), "trinket");
        }

        [Test]
        public void EveryClass_HasItsStarterMovesAndAnUltimate()
        {
            Assert.AreEqual(System.Enum.GetValues(typeof(ClassId)).Length, _c.classes.Count, "a ClassId has no ClassData");
            foreach (var cls in _c.classes)
            {
                AssertMove(cls.starterAttack, cls, MoveSlot.Attack);
                AssertMove(cls.starterSkill, cls, MoveSlot.Skill);
                AssertMove(cls.starterDash, cls, MoveSlot.Dash);
                var ult = AssertMove(cls.ultimate, cls, MoveSlot.Ultimate);
                Assert.That(ult.cooldown, Is.InRange(10f, 30f), cls.id + "'s ultimate should be usable about once a room or two");
            }
        }

        MoveData AssertMove(string id, ClassData cls, MoveSlot slot)
        {
            Assert.IsFalse(string.IsNullOrEmpty(id), cls.id + " has no " + slot + " move");
            var m = _c.moves.Find(x => x.id == id);
            Assert.IsNotNull(m, cls.id + "'s " + slot + " move '" + id + "' doesn't exist");
            Assert.AreEqual(slot, m.slot, "move '" + id + "' is in the wrong slot");
            return m;
        }

        [Test]
        public void Moves_HaveSaneNumbers()
        {
            foreach (var m in _c.moves)
            {
                Assert.Greater(m.cooldown, 0f, m.id + " has no cooldown");
                Assert.GreaterOrEqual(m.power, 0f, m.id + " has negative power");
                Assert.GreaterOrEqual(m.count, 1, m.id + " fires nothing");
                if (!string.IsNullOrEmpty(m.lightColour))
                {
                    Color col;
                    Assert.IsTrue(ColorUtility.TryParseHtmlString("#" + m.lightColour, out col), m.id + " has a bad light colour: " + m.lightColour);
                }
            }
        }

        [Test]
        public void Recipes_UseRealIngredients()
        {
            foreach (var r in _c.recipes)
                foreach (var cost in r.ingredients)
                    Assert.IsNotNull(_c.ingredients.Find(i => i.id == cost.ingredientId), r.id + " needs unknown ingredient " + cost.ingredientId);
        }

        [Test]
        public void Routes_UseRealMonstersAndIngredients()
        {
            foreach (var route in _c.routes)
            {
                foreach (var id in route.monsterIds)
                    Assert.IsNotNull(_c.monsters.Find(m => m.id == id), route.id + " spawns unknown monster " + id);
                foreach (var id in route.gatherIngredientIds)
                    Assert.IsNotNull(_c.ingredients.Find(i => i.id == id), route.id + " gathers unknown ingredient " + id);
                foreach (var id in route.materialIds)
                    Assert.IsNotNull(_c.ingredients.Find(i => i.id == id), route.id + " drops unknown material " + id);
            }
        }

        [Test]
        public void ContentAsset_IsUpToDate()
        {
            var guids = AssetDatabase.FindAssets("t:GameContentAsset");
            Assert.IsNotEmpty(guids, "no GameContent asset in the project");
            var asset = AssetDatabase.LoadAssetAtPath<GameContentAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            Assert.IsNotNull(asset.content);
            Assert.AreEqual(DefaultContent.LatestContentVersion, asset.content.contentVersion,
                "the content asset hasn't been upgraded — the built game would upgrade it at start-up instead");
        }

        [Test]
        public void ArtLibrary_HasEveryClassAndMonster()
        {
            var guids = AssetDatabase.FindAssets("t:ArtLibrary");
            Assert.IsNotEmpty(guids, "no Art Library asset in the project");
            var lib = AssetDatabase.LoadAssetAtPath<ArtLibrary>(AssetDatabase.GUIDToAssetPath(guids[0]));
            var missing = new List<string>();
            foreach (var cls in _c.classes) if (lib.Class(cls.id.ToString()) == null) missing.Add("class " + cls.id);
            foreach (var m in _c.monsters) if (lib.Monster(m.id) == null) missing.Add("monster " + m.id);
            Assert.IsEmpty(missing, "no art for: " + string.Join(", ", missing));
        }
    }
}
