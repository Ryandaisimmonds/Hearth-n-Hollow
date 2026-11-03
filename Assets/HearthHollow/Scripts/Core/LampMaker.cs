// Hearth & Hollow — the Lamp-maker (ticket 17): the slice's town building that you choose to build on a fenced plot
// and pay for with biome materials (Meadow timber, Ridge ember coal). Each stage widens your carried light and
// makes Flare bigger and quicker. Pure C#.
using System;

namespace HearthHollow.Core
{
    public static class LampMaker
    {
        public static int Stage(Profile p) { return p != null ? Math.Max(0, p.lampMakerStage) : 0; }
        public static bool Built(Profile p) { return Stage(p) > 0; }

        public static LampStage Current(GameContent c, Profile p)
        {
            int s = Stage(p);
            if (s <= 0 || c.lampMaker == null || c.lampMaker.Count == 0) return null;
            return c.lampMaker[Math.Min(s, c.lampMaker.Count) - 1];
        }

        public static LampStage Next(GameContent c, Profile p)
        {
            int s = Stage(p);
            return c.lampMaker != null && s < c.lampMaker.Count ? c.lampMaker[s] : null;
        }

        public static bool CanBuild(GameContent c, Profile p, out string reason)
        {
            var next = Next(c, p);
            if (next == null) { reason = "The Lamp-maker is finished"; return false; }
            return Kitchen.CanAfford(c, p, next, out reason);
        }

        /// Build the next stage: pays its coins and materials.
        public static bool Build(GameContent c, Profile p, out string reason)
        {
            if (!CanBuild(c, p, out reason)) return false;
            var next = Next(c, p);
            p.coins -= next.price;
            if (next.materials != null) foreach (var m in next.materials) Economy.RemoveFromPantry(p, m.ingredientId, m.count);
            p.lampMakerStage = Stage(p) + 1;
            return true;
        }

        public static float LightReach(GameContent c, Profile p) { var s = Current(c, p); return s != null ? s.lightReach : 0f; }
        public static float FlareRadius(GameContent c, Profile p) { var s = Current(c, p); return s != null ? s.flareRadius : 0f; }
        public static float FlareCooldownCut(GameContent c, Profile p) { var s = Current(c, p); return s != null ? s.flareCooldownCut : 0f; }

        /// "+20% light · Flare +20% wider, 3 s quicker".
        public static string EffectText(LampStage s)
        {
            if (s == null) return "";
            string t = "+" + (int)Math.Round(s.lightReach * 100) + "% light";
            if (s.flareRadius > 0f) t += " · Flare +" + (int)Math.Round(s.flareRadius * 100) + "% wider";
            if (s.flareCooldownCut > 0f) t += ", " + (int)Math.Round(s.flareCooldownCut) + " s quicker";
            return t;
        }

        public static string CostText(GameContent c, LampStage s)
        {
            if (s == null) return "";
            string t = s.price + " coins";
            if (s.materials != null)
                foreach (var m in s.materials)
                {
                    var ing = c.Ingredient(m.ingredientId);
                    t += " + " + m.count + " " + (ing != null ? ing.emoji + " " + ing.name : m.ingredientId);
                }
            return t;
        }
    }
}
