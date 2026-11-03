// Hearth & Hollow — GameSession API for move loadouts (camp only). Pure C#. Levels and skill trees were cut (ticket 04).
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
