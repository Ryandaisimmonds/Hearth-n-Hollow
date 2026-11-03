// Hearth & Hollow — opening arc content (contentVersion 24). Pure C#.
// Everyone now starts at OpeningArc.baseSpeed (75%) of the walk speed, so the permanent speed sources are doubled:
// fully kitted (Windstep Boots + Feather Charm + Thornsilk Band + 5 ranks of a speed node) ends close to today's top speed.
// Meals are left alone: they're for one run. Only numbers still at their old defaults are changed (your edits are kept).
using System;

namespace HearthHollow.Core
{
    public static partial class DefaultContent
    {
        const float SpeedGearMult = 2f;

        static void Speed24(GameContent c)
        {
            // boots: 0 / 4 / 7 / 10 / 14 % → 0 / 8 / 14 / 20 / 28 %
            float[] oldBoots = { 0f, 0.04f, 0.07f, 0.10f, 0.14f };
            for (int i = 0; i < c.bootsTiers.Count && i < oldBoots.Length; i++)
                if (Near(c.bootsTiers[i].value, oldBoots[i])) c.bootsTiers[i].value = oldBoots[i] * SpeedGearMult;

            // speed charms: Feather Charm 8 % → 16 %, Thornsilk Band 10 % → 20 %
            foreach (var t in c.trinkets)
            {
                if (t.perk != TrinketPerk.MoveSpeed) continue;
                if ((t.id == "feather" && Near(t.perkValue, 0.08f)) || (t.id == "thornband" && Near(t.perkValue, 0.10f)))
                    t.perkValue *= SpeedGearMult;
            }

            // speed nodes: 3 % → 6 % per rank
            foreach (var n in c.skillNodes)
            {
                if (n.effect != NodeEffect.MoveSpeed || !Near(n.valuePerRank, 0.03f)) continue;
                n.valuePerRank = 0.06f;
                if (n.description == "Move 3% faster per rank.") n.description = "Move 6% faster per rank.";
            }
        }

        static bool Near(float a, float b) { return Math.Abs(a - b) < 0.0005f; }
    }
}
