// Hearth & Hollow — Design v2 content changes (contentVersion 25+). Pure C#.
// v25 (tickets 01–03): Darkness is no longer a route hazard (deeper floors are simply darker), and move
// descriptions lose the Boost, shield-pip and Heat text for systems that were cut.
// v26 (ticket 06): every move gets an element from its old hit type (Fire and Light keep theirs; the rest have none).
using System.Text.RegularExpressions;

namespace HearthHollow.Core
{
    public static partial class DefaultContent
    {
        static void DesignV2Cuts25(GameContent c)
        {
            foreach (var r in c.routes)
                if (r != null && r.hazard == Hazard.Darkness) { r.hazard = Hazard.None; r.hazardPct = 0f; }

            foreach (var m in c.moves)
            {
                if (m == null || string.IsNullOrEmpty(m.description)) continue;
                string d = m.description;
                d = Regex.Replace(d, @"\s*Boost:[^.]*\.", "");                       // "Boost: a bigger wheel."
                d = Regex.Replace(d, @" that knocks off \d+ shield pips", "");
                d = d.Replace(" (+20 Heat)", "");
                d = d.Replace(" Every hit stokes your Heat.", "");
                m.description = d.Trim();
            }
            foreach (var k in c.classes)
                if (k != null && k.description != null)
                    k.description = k.description.Replace(" Every hit stokes her Heat, and her light grows from a candle to a bonfire.", "");
            var det = c.Move("e_detonate");
            if (det != null) det.description = "One big explosion of fire around you (Fire).";
            var sn = c.Move("e_ult");
            if (sn != null) sn.description = "Pull in every ember, then a huge explosion of fire around you (Fire) that stuns.";
        }

        static void Elements26(GameContent c)
        {
            foreach (var m in c.moves)
            {
                if (m == null) continue;
                m.element = Elements.FromHitType(m.hitType);
                var e2 = Elements.FromHitType(m.hitType2);
                m.element2 = e2 != m.element ? e2 : Element.None;
            }
        }
    }
}
