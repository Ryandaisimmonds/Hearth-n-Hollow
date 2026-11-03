// Hearth & Hollow — element discovery (Design v2, ticket 07). Pure C#.
// An enemy's weaknesses and resistances start hidden. Hitting a kind of monster with an element (or touching it
// with Flare's Light or a brazier's fire) teaches you how that element works on it — weak, resistant or neither —
// for every monster of that kind, forever. Story beats can teach one directly (Old Wren, ticket 15).
using System.Collections.Generic;

namespace HearthHollow.Core
{
    [System.Serializable] public class KnownElement { public string monsterId; public Element element; }

    public static class Discovery
    {
        public static bool Knows(Profile p, string monsterId, Element e)
        {
            if (p == null || p.knownElements == null || string.IsNullOrEmpty(monsterId) || e == Element.None) return false;
            foreach (var k in p.knownElements) if (k.monsterId == monsterId && k.element == e) return true;
            return false;
        }

        /// Learns how <paramref name="e"/> works on <paramref name="monsterId"/>. True if it wasn't known before.
        public static bool Learn(Profile p, string monsterId, Element e)
        {
            if (p == null || string.IsNullOrEmpty(monsterId) || e == Element.None || Knows(p, monsterId, e)) return false;
            if (p.knownElements == null) p.knownElements = new List<KnownElement>();
            p.knownElements.Add(new KnownElement { monsterId = monsterId, element = e });
            return true;
        }

        /// The known ones out of a list (e.g. a monster's weaknesses), in order.
        public static List<Element> KnownOf(Profile p, string monsterId, IEnumerable<Element> list)
        {
            var r = new List<Element>();
            if (list != null) foreach (var e in list) if (Knows(p, monsterId, e)) r.Add(e);
            return r;
        }
    }
}
