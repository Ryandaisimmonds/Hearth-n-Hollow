// Hearth & Hollow — the default content: the game's design as data. Create() builds it from the sections beside
// this file (Tuning, Classes, Food, World, Gear), which ARE the design: edit them directly.
// Used to fill GameContent.asset ("Reset To Defaults"), by the tests, and as the fallback at runtime.
// Refactor 03 (Nov 2026) replaced 44 numbered upgrade steps with this; the result was checked identical, field by
// field, to both the old Create() and the live GameContent.asset. Pure C#, no UnityEngine.
using System.Collections.Generic;
using System.Reflection;

namespace HearthHollow.Core
{
    public static partial class DefaultContent
    {
        /// The content version Create() builds. 45 = the collapsed baseline. A later design change adds a step to
        /// Upgrade() below and bumps this, so an edited GameContent.asset keeps its edits and gains the change.
        public const int LatestContentVersion = 46;
        const int Baseline = 45;

        /// Every class starts on the same small base HP; meals carry a run (Design v2).
        public const int MinimalBaseHp = 14;
        /// Story flag: Old Wren has told you the Hoarfrost Alpha is weak to Fire (reveals Chili Ridge).
        public const string WrenFireHint = Ids.Flag.WrenFireHint;

        public static GameContent Create()
        {
            var c = new GameContent();
            SetTuning(c);    // DefaultContent.Tuning.cs
            AddClasses(c);   // DefaultContent.Classes.cs
            AddFood(c);      // DefaultContent.Food.cs
            AddWorld(c);     // DefaultContent.World.cs
            AddGear(c);      // DefaultContent.Gear.cs
            c.contentVersion = LatestContentVersion;
            return c;
        }

        /// Brings a content asset up to date; safe to call on every load. Content from before the baseline is too old
        /// to patch, so it's replaced with the defaults. Returns true if anything changed.
        public static bool Upgrade(GameContent c)
        {
            if (c == null || c.contentVersion >= LatestContentVersion) return false;
            if (c.contentVersion < Baseline) { CopyInto(Create(), c); return true; }
            if (c.contentVersion < 46) { MonsterFighting46(c); c.contentVersion = 46; }
            // Future design changes go here, one step each: if (c.contentVersion < 47) { SomeChange47(c); c.contentVersion = 47; }
            return true;
        }

        /// 46 (refactor 04): how each monster moves and fights moved from the Dungeon Settings asset into the content.
        static void MonsterFighting46(GameContent c)
        {
            var d = Create();
            foreach (var m in c.monsters)
            {
                var src = d.monsters.Find(x => x.id == m.id);
                if (src == null) continue;
                m.archetype = src.archetype; m.moveSpeed = src.moveSpeed; m.attackRange = src.attackRange; m.windup = src.windup;
                m.cooldownMult = src.cooldownMult; m.projectileSpeed = src.projectileSpeed; m.hits = src.hits; m.healsAllies = src.healsAllies;
                m.lightReaction = src.lightReaction; m.element = src.element; m.weakTo = new List<Element>(src.weakTo); m.resists = new List<Element>(src.resists);
                m.circles = src.circles; m.swoops = src.swoops; m.splits = src.splits;
            }
        }

        /// Overwrites every field of `to` with `from`'s (keeps the object, so anything holding it sees the new data).
        static void CopyInto(GameContent from, GameContent to)
        {
            foreach (var f in typeof(GameContent).GetFields(BindingFlags.Public | BindingFlags.Instance)) f.SetValue(to, f.GetValue(from));
        }
    }
}
