// Hearth & Hollow — elements (Design v2, ticket 06). Pure C#, no UnityEngine.
// Fire, Frost, Nature and Water form a loop: each is strong against the next (Fire > Frost > Nature > Water > Fire).
// Light sits outside the loop and is only strong against Shadow (shadow creatures are always weak to it).
// Hitting a weakness does ×1.5 damage, a resistance ×0.5. Moves can carry an element; attacks without one take
// the run's infused element (from meals, ticket 08). Enemies have weaknesses and resistances, by default their
// own element's place in the loop: weak to what beats it, resistant to itself.
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public enum Element { None, Fire, Frost, Nature, Water, Light }

    public static class Elements
    {
        public const float WeakMult = 1.5f, ResistMult = 0.5f;

        /// The element <paramref name="e"/> is strong against (None for Light / None).
        public static Element StrongAgainst(Element e)
        {
            switch (e)
            {
                case Element.Fire: return Element.Frost;
                case Element.Frost: return Element.Nature;
                case Element.Nature: return Element.Water;
                case Element.Water: return Element.Fire;
                default: return Element.None;
            }
        }

        /// The element that is strong against <paramref name="e"/> (None for Light / None).
        public static Element WeakTo(Element e)
        {
            switch (e)
            {
                case Element.Frost: return Element.Fire;
                case Element.Nature: return Element.Frost;
                case Element.Water: return Element.Nature;
                case Element.Fire: return Element.Water;
                default: return Element.None;
            }
        }

        public static bool InLoop(Element e) { return e == Element.Fire || e == Element.Frost || e == Element.Nature || e == Element.Water; }

        /// Damage multiplier for an attack of <paramref name="attack"/> on something with these weaknesses / resistances.
        public static float Multiplier(Element attack, ICollection<Element> weak, ICollection<Element> resist)
        {
            if (attack == Element.None) return 1f;
            if (weak != null && weak.Contains(attack)) return WeakMult;
            if (resist != null && resist.Contains(attack)) return ResistMult;
            return 1f;
        }

        /// Fills weaknesses / resistances from an enemy's own element: weak to what beats it, resistant to itself.
        public static void Defaults(Element own, ICollection<Element> weak, ICollection<Element> resist)
        {
            if (!InLoop(own)) return;
            var w = WeakTo(own);
            if (!weak.Contains(w)) weak.Add(w);
            if (!resist.Contains(own)) resist.Add(own);
        }

        /// The old Break & Boost hit types carried over: Fire and Light keep their element, the physical ones have none.
        public static Element FromHitType(HitType t) { return t == HitType.Fire ? Element.Fire : t == HitType.Light ? Element.Light : Element.None; }

        public static string Name(Element e) { return e == Element.None ? "" : e.ToString(); }
        public static string Emoji(Element e)
        {
            switch (e)
            {
                case Element.Fire: return "🔥";
                case Element.Frost: return "❄️";
                case Element.Nature: return "🌿";
                case Element.Water: return "💧";
                case Element.Light: return "✨";
                default: return "";
            }
        }
    }
}
