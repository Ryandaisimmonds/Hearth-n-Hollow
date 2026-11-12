// Refactor 05: every id constant names something that exists in the content.
using System.Reflection;
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class IdsTests
    {
        static System.Collections.Generic.IEnumerable<string> Consts(System.Type t)
        {
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Static))
                if (f.IsLiteral && f.FieldType == typeof(string)) yield return (string)f.GetRawConstantValue();
        }

        [Test]
        public void EveryIdConstant_ExistsInTheContent()
        {
            var c = DefaultContent.Create();
            foreach (var id in Consts(typeof(Ids.Area))) Assert.IsNotNull(c.Route(id), "area " + id);
            foreach (var id in Consts(typeof(Ids.Monster))) Assert.IsNotNull(c.Monster(id), "monster " + id);
            foreach (var id in Consts(typeof(Ids.Ingredient))) Assert.IsNotNull(c.Ingredient(id), "ingredient " + id);
            foreach (var id in Consts(typeof(Ids.Recipe))) Assert.IsNotNull(c.Recipe(id), "recipe " + id);
        }

        [Test]
        public void EveryAreaMonsterIngredientAndDish_HasAConstant()
        {
            var c = DefaultContent.Create();
            var areas = new System.Collections.Generic.List<string>(Consts(typeof(Ids.Area)));
            var mons = new System.Collections.Generic.List<string>(Consts(typeof(Ids.Monster)));
            var ings = new System.Collections.Generic.List<string>(Consts(typeof(Ids.Ingredient)));
            var recs = new System.Collections.Generic.List<string>(Consts(typeof(Ids.Recipe)));
            foreach (var r in c.routes) CollectionAssert.Contains(areas, r.id, "add Ids.Area for " + r.id);
            foreach (var m in c.monsters) CollectionAssert.Contains(mons, m.id, "add Ids.Monster for " + m.id);
            foreach (var i in c.ingredients) CollectionAssert.Contains(ings, i.id, "add Ids.Ingredient for " + i.id);
            foreach (var r in c.recipes) CollectionAssert.Contains(recs, r.id, "add Ids.Recipe for " + r.id);
        }
    }
}
