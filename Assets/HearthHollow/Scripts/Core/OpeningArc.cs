// Hearth & Hollow — the opening arc: a staged tutorial that grows a dead campfire into a little town. Pure C#.
// Design v2 (ticket 18) trims it to the slice's beats. The stages live in the Dungeon Settings asset (DungeonSettings.arc):
//   0 Arrival — a dead fire, Old Wren and the Route board. No dash (no boots yet).
//   1 Fed     — home from your first dive (fallen or not): Wren feeds you and the kitchen and larder open. Cook a first meal.
//   2 Town    — home after your first boss (Meadow Hollow floor 5): the camp grows into a town; the smithy (boots = the dash),
//               workshop and the plot for the Lamp-maker open.
// The pit, its cache and the 75% start speed were cut.
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    /// What moves the arc on to its next stage. Append only (FirstDeath / FirstLadder / PitFall / HomeAfterPit are retired).
    public enum ArcTrigger { None, FirstDeath, FirstLadder, PitFall, HomeAfterPit, FirstRunEnd, HomeAfterBoss }

    [Serializable] public class ArcStage
    {
        public string name;
        public ArcTrigger reachedBy;
        public string toast;                                  // shown when the stage is reached
        public List<string> opens = new List<string>();      // town areas this stage opens (kitchen, larder, smithy, workshop, lampmaker…)
        public bool giveStarterFood;                          // the content's starter pantry goes into your pantry
        public bool giveFirstBoots;                           // the first boots (tier 1); the dash unlocks from here (unused in Design v2: buy them)
        public int giveCoins;
    }

    [Serializable] public class OpeningArc
    {
        public bool enabled = true;                           // off = every area open, dash always on
        public bool dashNeedsBoots = true;                    // no dash until you own boots
        public List<ArcStage> stages = Defaults();

        public static List<ArcStage> Defaults()
        {
            return new List<ArcStage>
            {
                new ArcStage { name = "Arrival", reachedBy = ArcTrigger.None, opens = new List<string> { "hearth", "wren", "board" },
                    toast = "" },
                new ArcStage { name = "Fed", reachedBy = ArcTrigger.FirstRunEnd, opens = new List<string> { "kitchen", "larder" }, giveStarterFood = true,
                    toast = "🍲 Old Wren sits you down by the fire and feeds you. \"Eat first, then go back down.\" The kitchen is open: cook a meal and pack it on your tray." },
                new ArcStage { name = "Town", reachedBy = ArcTrigger.HomeAfterBoss, opens = new List<string> { "smithy", "workshop", "lampmaker" }, giveCoins = 50,
                    toast = "🏘️ Word of your win spreads. The camp grows into a little town: the smithy (buy boots to dash), the workshop and an empty plot for a lamp-maker are open." },
            };
        }
    }

    public partial class Profile
    {
        public int arcStage;   // the opening arc's stage (OpeningArc.cs): 0 = just arrived
    }


    public static class Arc
    {
        public static int LastStage(OpeningArc a) { return a != null && a.stages != null ? a.stages.Count - 1 : 0; }
        public static bool Finished(OpeningArc a, Profile p) { return a == null || !a.enabled || p == null || p.arcStage >= LastStage(a); }

        public static ArcStage Current(OpeningArc a, Profile p)
        {
            if (a == null || a.stages == null || a.stages.Count == 0 || p == null) return null;
            return a.stages[Math.Max(0, Math.Min(p.arcStage, a.stages.Count - 1))];
        }

        /// The event that reaches the next stage (None once the arc is done or switched off).
        public static ArcTrigger NextTrigger(OpeningArc a, Profile p)
        {
            if (a == null || !a.enabled || p == null || a.stages == null) return ArcTrigger.None;
            int next = p.arcStage + 1;
            return next < a.stages.Count ? a.stages[next].reachedBy : ArcTrigger.None;
        }

        /// Is a town area open? Areas no stage mentions are always open; so is everything when the arc is off.
        public static bool IsOpen(OpeningArc a, Profile p, string area)
        {
            if (a == null || !a.enabled || p == null || a.stages == null || string.IsNullOrEmpty(area)) return true;
            bool mentioned = false;
            for (int i = 0; i < a.stages.Count; i++)
            {
                var s = a.stages[i];
                if (s == null || s.opens == null || !s.opens.Contains(area)) continue;
                mentioned = true;
                if (i <= p.arcStage) return true;
            }
            return !mentioned;
        }

        public static bool DashUnlocked(OpeningArc a, Profile p, ClassId cls)
        {
            if (a == null || !a.enabled || !a.dashNeedsBoots || p == null) return true;
            if (Gear.TierIndex(p, GearSlot.Boots, cls) >= 1) return true;
            for (int i = 0; i <= Math.Min(p.arcStage, LastStage(a)); i++) if (a.stages[i] != null && a.stages[i].giveFirstBoots) return true;
            return false;
        }

        /// Moves to the next stage if <paramref name="t"/> is the event it waits for, and hands out its gifts.
        /// Returns the stage reached, or null if nothing happened.
        public static ArcStage Advance(GameContent c, OpeningArc a, Profile p, ArcTrigger t)
        {
            if (t == ArcTrigger.None || NextTrigger(a, p) != t) return null;
            p.arcStage++;
            var s = a.stages[p.arcStage];
            if (s.giveStarterFood && c.starterPantry != null)
                foreach (var ic in c.starterPantry) Economy.AddToPantry(p, ic.ingredientId, ic.count);
            if (s.giveFirstBoots)
                if (p.bootsTier < 1 && c.bootsTiers.Count > 1) p.bootsTier = 1;   // shared boots (ticket 16)
            if (s.giveCoins > 0) p.coins += s.giveCoins;
            return s;
        }
    }
}
