// Hearth & Hollow — who's in your camp (class rework, stage 5). Pure C#.
// A new game starts with the Cook only; every other class joins your camp through the story, when you first clear
// the route in its ClassData.joinRoute (Ranger: Meadow Trail · Warden: Salt Marsh · Lamplighter: Firefly Glade ·
// Emberfist: Chili Ridge — set in DefaultContentMoves.Joins18, editable in the content asset).
// saveVersion 7: the class rework is a fresh start for every class (levels, XP, skill trees, loadouts, gear tiers).
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public partial class GameSession
    {
        /// Has this class joined your camp yet?
        public static bool ClassJoined(GameContent c, Profile p, ClassId id)
        {
            var cd = c != null ? c.Class(id) : null;
            if (cd == null || p == null) return false;
            return string.IsNullOrEmpty(cd.joinRoute) || p.clearedRouteIds.Contains(cd.joinRoute);
        }

        public bool ClassJoined(ClassId id) { return ClassJoined(Content, Profile, id); }

        /// For a locked class card: where they join, e.g. "Joins your camp after you clear 🌾 Meadow Trail".
        public string ClassJoinHint(ClassId id)
        {
            var cd = Content.Class(id);
            var r = cd != null && !string.IsNullOrEmpty(cd.joinRoute) ? Content.Route(cd.joinRoute) : null;
            return r != null ? "Joins your camp after you clear " + r.emoji + " " + r.name : "Not met yet";
        }

        List<ClassId> JoinedNow()
        {
            var list = new List<ClassId>();
            foreach (var cd in Content.classes) if (ClassJoined(cd.id)) list.Add(cd.id);
            return list;
        }

        /// After a run: a toast for every class that has just joined.
        void AnnounceJoins(List<ClassId> before)
        {
            foreach (var cd in Content.classes)
            {
                if (before.Contains(cd.id) || !ClassJoined(cd.id)) continue;
                Emit(new GameEvent(GameEventType.LevelUp, 0, true, cd.emoji + " The " + cd.name + " joins your camp!" +
                                   (string.IsNullOrEmpty(cd.joinText) ? "" : " " + cd.joinText)));
            }
        }

        /// saveVersion 7 (class rework): a fresh start for every class. Coins, pantry, story, kitchen and trinkets are kept.
        void ClassRework7()
        {
            Profile.classes.Clear();
            foreach (ClassId id in Enum.GetValues(typeof(ClassId))) Profile.Progress(id);
            Profile.selectedClass = ClassId.Cook;
            Profile.saveVersion = 7;
        }

        /// Never leave a class selected that hasn't joined (old saves, edited content).
        void EnsureJoinedSelection()
        {
            if (!ClassJoined(Profile.selectedClass)) Profile.selectedClass = ClassId.Cook;
        }
    }
}
