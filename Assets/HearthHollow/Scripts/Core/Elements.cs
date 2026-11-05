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
        /// Ticket 46: a hit carrying several elements. Weakness only ×1.5 · resistance only ×0.5 · a weakness and a
        /// resistance cancel (×1, <paramref name="cancelled"/> true) · two weaknesses are still ×1.5.
        public static float Multiplier(IList<Element> attack, ICollection<Element> weak, ICollection<Element> resist, out bool cancelled)
        {
            cancelled = false;
            if (attack == null || attack.Count == 0) return 1f;
            if (attack.Count == 1) return Multiplier(attack[0], weak, resist);
            bool w = false, r = false;
            foreach (var e in attack)
            {
                if (e == Element.None) continue;
                if (weak != null && weak.Contains(e)) w = true;
                else if (resist != null && resist.Contains(e)) r = true;
            }
            if (w && r) { cancelled = true; return 1f; }
            return w ? WeakMult : r ? ResistMult : 1f;
        }

        /// "🔥❄️" for a list of elements.
        public static string List(IList<Element> els) { var s = ""; if (els != null) foreach (var e in els) s += Emoji(e); return s; }
        /// "Fire and Frost" / "Fire, Frost and Water".
        public static string Names(IList<Element> els)
        {
            var n = new List<string>();
            if (els != null) foreach (var e in els) if (e != Element.None) n.Add(Name(e));
            if (n.Count <= 1) return n.Count == 1 ? n[0] : "";
            return string.Join(", ", n.GetRange(0, n.Count - 1).ToArray()) + " and " + n[n.Count - 1];
        }

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
