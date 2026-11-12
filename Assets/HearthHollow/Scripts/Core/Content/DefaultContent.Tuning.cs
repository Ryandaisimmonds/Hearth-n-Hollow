// Hearth & Hollow — default content: tuning, the starting pantry and the story's title.
// The current design, written out directly (refactor 03 collapsed the old 44 upgrade steps into this).
// Edit freely: these files ARE the design now. Pure C#, no UnityEngine.
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public static partial class DefaultContent
    {
        /// Every number the rules read (only the ones that differ from TuningData's defaults), plus what a new game starts with.
        static void SetTuning(GameContent c)
        {
            var t = c.tuning;
            t.monsterAtkPerDiff = 2f;
            t.monsterDropChance = 0.21f;
            t.enemiesPerRoomMax = 3;
            t.materialDropChance = 0.09f;
            t.materialDropPerFloor = 0.06f;
            // ── starterPantry (4) ──
            c.starterPantry.Add(new IngredientCost { ingredientId = "herbs", count = 4 });
            c.starterPantry.Add(new IngredientCost { ingredientId = "wheat", count = 4 });
            c.starterPantry.Add(new IngredientCost { ingredientId = "berries", count = 3 });
            c.starterPantry.Add(new IngredientCost { ingredientId = "mushroom", count = 3 });
            c.storyTitle = "The Everflame";
            c.storyPrologue = "Every hearth in the valley was lit from the Everflame, a fire that has burned in the Hollow Heart for a thousand winters. This autumn it guttered. The fog crept in, the hearths went cold, and the old roads closed. Old Wren, keeper of the camp, has one map left: the Ember Path. Cook well, travel light, and bring the flame home.";
        }
    }
}
