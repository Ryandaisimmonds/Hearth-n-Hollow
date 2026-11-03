// Hearth & Hollow — GameSession API for move loadouts (camp only). Pure C#. Levels and skill trees were cut (ticket 04).
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public partial class GameSession
    {
        /// Keeps every class's loadout valid. Called on load.
        private void UpgradeProgression()
        {
            foreach (var cp in Profile.classes) Progression.EnsureLoadout(Content, Profile, cp.classId);
            if (Profile.saveVersion < 3) Profile.saveVersion = 3;
        }

        public bool CanEditSkills { get { return !InRun; } }

        public MoveData[] Loadout(ClassId cls) { return Progression.Loadout(Content, Profile, cls); }
        public MoveData UltimateMove(ClassId cls) { return Progression.Ultimate(Content, Profile, cls); }

        // ───── the move pool (ticket 10): arc steps unlock moves, learned in town ─────
        public bool ReadyToLearn(ClassId cls, string moveId) { return Progression.ReadyToLearn(Content, Profile, cls, Content.Move(moveId)); }
        public List<MoveData> ReadyMoves(ClassId cls) { return Progression.ReadyMoves(Content, Profile, cls); }
        public bool LearnMove(ClassId cls, string moveId)
        {
            if (!CanEditSkills) return false;
            if (!Progression.Learn(Content, Profile, cls, moveId)) return false;
            var m = Content.Move(moveId);
            Info("✨ Learned " + (m != null ? m.name : moveId) + "!");
            Touch(true);
            return true;
        }

        /// Runs an arc-step record (floor reached, boss beaten) and announces any move it unlocks.
        private void ArcStep(System.Func<bool> record)
        {
            if (Run == null) { record(); return; }
            var before = Progression.ReadyMoves(Content, Profile, Run.classId).ConvertAll(x => x.id);
            if (!record()) return;
            foreach (var m in Progression.ReadyMoves(Content, Profile, Run.classId))
                if (!before.Contains(m.id))
                    Info("✨ New move unlocked: " + m.emoji + " " + m.name + " — learn it at " + Progression.LearnAtText(Progression.Unlock(Content, m.id)) + " when you're home");
        }
        private void ReachedFloor() { if (Run != null) ArcStep(() => Progression.RecordFloor(Profile, Run.routeId, Run.floor)); }
        private void BeatBoss() { if (Run != null) ArcStep(() => Progression.RecordBoss(Profile, Run.routeId, Run.floor)); }

        /// saveVersion 11: older saves only know cleared routes (3 floors each back then).
        private void UpgradeArcProgress()
        {
            if (Profile.deepestFloors == null) Profile.deepestFloors = new List<RouteFloor>();
            if (Profile.bossesBeaten == null) Profile.bossesBeaten = new List<string>();
            if (Profile.learnedMoves == null) Profile.learnedMoves = new List<string>();
            if (Profile.saveVersion >= 11) return;
            if (Profile.clearedRouteIds != null)
                foreach (var id in Profile.clearedRouteIds) Progression.RecordFloor(Profile, id, Math.Max(1, T.dungeonFloors));
            Profile.saveVersion = 11;
        }
        public List<MoveData> SlotChoices(ClassId cls, LoadSlot slot) { return Progression.SlotChoices(Content, Profile, cls, slot); }
        public string EquipBlock(ClassId cls, LoadSlot slot, string moveId)
        {
            if (!CanEditSkills) return "Only at camp";
            return Progression.EquipBlock(Content, Profile, cls, slot, Content.Move(moveId));
        }
        public bool EquipMove(ClassId cls, LoadSlot slot, string moveId)
        {
            if (!CanEditSkills) return false;
            if (!Progression.Equip(Content, Profile, cls, slot, moveId)) return false;
            Touch(true);
            return true;
        }

        /// Bonuses for the class in the current run (or the selected class at camp).
        public CombatMods Mods
        {
            get
            {
                ClassId cls = Run != null ? Run.classId : Profile.selectedClass;
                var m = Progression.Mods(Content, Profile, cls);
                // meals add speed and heal-on-kill (cooking v9): the run's eaten meals, or the packed tray at camp
                Cooking.AddMealMods(Content, Content.Class(cls), Run != null ? Run.eaten : Cooking.Packed(Content, Profile), m);
                return m;
            }
        }
    }
}
