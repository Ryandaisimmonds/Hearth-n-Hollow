// Hearth & Hollow — GameSession API for levels, skill trees and loadouts (camp only). Pure C#.
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public partial class GameSession
    {
        /// Upgrades the profile to skill trees + loadouts and keeps them valid. Called on load.
        private void UpgradeProgression()
        {
            foreach (var cp in Profile.classes)
            {
                if (cp.tree == null) cp.tree = new List<NodeRank>();
                if (cp.level > T.levelCap) cp.level = T.levelCap;
                Progression.Sanitize(Content, cp);
            }
            foreach (var cp in Profile.classes) Progression.EnsureLoadout(Content, Profile, cp.classId);
            if (Profile.saveVersion < 3) Profile.saveVersion = 3;
        }

        public bool CanEditSkills { get { return !InRun; } }

        public int SkillPointsFree(ClassId cls) { return Progression.PointsFree(Content, Profile.Progress(cls)); }
        public int SkillPointsEarned(ClassId cls) { return Progression.PointsEarned(Content, Profile.Progress(cls)); }
        public int NodeRank(ClassId cls, string nodeId) { return Progression.Rank(Profile.Progress(cls), nodeId); }
        public string NodeBlock(ClassId cls, string nodeId)
        {
            if (!CanEditSkills) return "Only at camp";
            return Progression.BlockReason(Content, Profile.Progress(cls), Content.Node(nodeId));
        }

        public bool BuyNode(ClassId cls, string nodeId)
        {
            if (!CanEditSkills) return false;
            if (!Progression.Buy(Content, Profile, cls, nodeId)) return false;
            var n = Content.Node(nodeId);
            if (n != null && n.effect == NodeEffect.UnlockMove)
            {
                var m = Content.Move(n.moveId);
                if (m != null) Info(m.emoji + " New move: " + m.name + " — equip it in your loadout");
            }
            Touch(true);
            return true;
        }

        public void RespecTree(ClassId cls)
        {
            if (!CanEditSkills) return;
            Progression.Respec(Content, Profile, cls);
            Info("Skill points refunded");
            Touch(true);
        }

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
