// Hearth & Hollow — ticket 65: "What got you". The run keeps its last few hits (who, how hard, HP left) so the
// summary can show what happened when you fall.
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    [Serializable] public class HitNote { public string who; public int dmg; public int hpAfter; }

    public partial class RunState
    {
        public List<HitNote> lastHits = new List<HitNote>();   // ticket 65: newest last, at most DeathRecap.Keep
    }

    public static class DeathRecap
    {
        public const int Keep = 5;

        public static void Note(RunState run, string who, int dmg)
        {
            if (run == null || dmg <= 0) return;
            if (run.lastHits == null) run.lastHits = new List<HitNote>();
            run.lastHits.Add(new HitNote { who = string.IsNullOrEmpty(who) ? "Something in the dark" : who, dmg = dmg, hpAfter = Math.Max(0, run.hp) });
            while (run.lastHits.Count > Keep) run.lastHits.RemoveAt(0);
        }

        /// Who landed the last blow (null = nobody noted).
        public static string Killer(List<HitNote> hits) { return hits != null && hits.Count > 0 ? hits[hits.Count - 1].who : null; }

        /// Whoever did the most damage across the recap, and how much.
        public static string Worst(List<HitNote> hits, out int total)
        {
            total = 0; string best = null;
            if (hits == null) return null;
            var sums = new Dictionary<string, int>();
            foreach (var h in hits) { int v; sums.TryGetValue(h.who, out v); sums[h.who] = v + h.dmg; }
            foreach (var kv in sums) if (kv.Value > total) { total = kv.Value; best = kv.Key; }
            return best;
        }
    }
}
