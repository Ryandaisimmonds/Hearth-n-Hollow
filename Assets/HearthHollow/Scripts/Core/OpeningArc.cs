// Hearth & Hollow — the opening arc: a staged tutorial that grows a tiny camp into a village. Pure C#, no UnityEngine.
// The stages live in the Dungeon Settings asset (DungeonSettings.arc) so they can be tuned in the Inspector:
//   0 Arrival     — a dead fire, Old Wren and the route board. Slow, no dash.
//   1 Fed         — your first death: Wren feeds you and the kitchen opens.
//   2 Deeper      — your first ladder down: old boots (the dash) and the Smithy / Workshop.
//   3 The Pit     — a cleared room caves in; you fall into a hidden cache of equipment.
//   4 Hollow      — home again after the pit: the camp becomes a basic town.
// Speed: everyone starts at baseSpeed × the walk speed; boots, charms, skill nodes and meals build it back up.
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    /// What moves the arc on to its next stage (each stage names the one event that reaches it).
    public enum ArcTrigger { None, FirstDeath, FirstLadder, PitFall, HomeAfterPit }

    [Serializable] public class ArcStage
    {
        public string name;
        public ArcTrigger reachedBy;
        public string toast;                                  // shown when the stage is reached
        public List<string> opens = new List<string>();      // town areas this stage opens (hearth, wren, board, kitchen, smithy, workshop, town…)
        public bool giveStarterFood;                          // the content's starter pantry goes into your pantry
        public bool giveFirstBoots;                           // every class gets the first boots (tier 1); the dash unlocks from here
        public int giveCoins;
    }

    [Serializable] public class OpeningArc
    {
        public bool enabled = true;                           // off = every area open, dash always on (speed rules still apply)
        public float baseSpeed = 0.75f;                       // × the walk speed, before boots, charms, skill nodes and meals
        public bool dashNeedsBoots = true;                    // no dash until the stage that gives the first boots (or any class owns boots)
        public float pitRadius = 0.55f;                       // how close to the pit's centre you have to step to fall in
        public List<GearSlot> pitCache = new List<GearSlot> { GearSlot.Lantern };   // the cache's equipment (next tier each; Design v2: shared utility gear only)
        public List<ArcStage> stages = Defaults();

        public static List<ArcStage> Defaults()
        {
            return new List<ArcStage>
            {
                new ArcStage { name = "Arrival", reachedBy = ArcTrigger.None, opens = new List<string> { "hearth", "wren", "board" },
                    toast = "" },
                new ArcStage { name = "Fed", reachedBy = ArcTrigger.FirstDeath, opens = new List<string> { "kitchen" }, giveStarterFood = true,
                    toast = "🍲 Old Wren drags you back to the fire and feeds you. \"Eat first, then go back down.\" The kitchen is open." },
                new ArcStage { name = "Deeper", reachedBy = ArcTrigger.FirstLadder, opens = new List<string> { "smithy", "workshop" }, giveFirstBoots = true,
                    toast = "🥾 Some old walking boots by the ladder. Now you can roll (Space / B). The Smithy and Workshop will open at camp." },
                new ArcStage { name = "The Pit", reachedBy = ArcTrigger.PitFall, opens = new List<string>(),
                    toast = "🕳️ The floor gives way! You land in a forgotten cache…" },
                new ArcStage { name = "Hollow", reachedBy = ArcTrigger.HomeAfterPit, opens = new List<string> { "town" }, giveCoins = 50,
                    toast = "🏘️ With what you found below, the camp starts to grow into a proper little town." },
            };
        }
    }

    public partial class Profile
    {
        public int arcStage;   // the opening arc's stage (OpeningArc.cs): 0 = just arrived
    }

    public partial class RunState
    {
        public bool pitTaken;  // opening arc: already fell through the pit this run
        public bool pitRoom;   // the next floor starts in the pit's cache room
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
