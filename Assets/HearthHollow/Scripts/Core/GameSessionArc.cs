// Hearth & Hollow — GameSession: the opening arc (OpeningArc.cs) — stages, start speed, the dash lock and the pit. Pure C#.
using System;

namespace HearthHollow.Core
{
    public partial class GameSession
    {
        OpeningArc _arc = new OpeningArc();
        /// Set from the Dungeon Settings asset at start-up (GameBootstrap); a default arc otherwise.
        public OpeningArc Arc { get { return _arc; } set { _arc = value ?? new OpeningArc(); } }

        public int ArcStage { get { return Profile.arcStage; } }
        public ArcStage ArcCurrent { get { return HearthHollow.Core.Arc.Current(_arc, Profile); } }
        public bool ArcFinished { get { return HearthHollow.Core.Arc.Finished(_arc, Profile); } }
        /// A town area (kitchen, smithy, workshop, …) is open at this stage of the arc.
        public bool AreaOpen(string area) { return HearthHollow.Core.Arc.IsOpen(_arc, Profile, area); }

        /// × the walk speed for everyone, before boots, charms, skill nodes and meals.
        public float ArcSpeed { get { return _arc.baseSpeed > 0f ? _arc.baseSpeed : 1f; } }
        public bool DashUnlocked { get { return HearthHollow.Core.Arc.DashUnlocked(_arc, Profile, Run != null ? Run.classId : Profile.selectedClass); } }
        /// Boots, charms and skill nodes for the selected class (no meals): for walking about town.
        public CombatMods TownMods { get { return Progression.Mods(Content, Profile, Profile.selectedClass); } }

        void ArcEvent(ArcTrigger t)
        {
            var s = HearthHollow.Core.Arc.Advance(Content, _arc, Profile, t);
            if (s == null) return;
            if (!string.IsNullOrEmpty(s.toast)) Info(s.toast);
            Recompute();
        }

        // ───── the pit (stage 3) ─────
        /// A cleared room should cave in on this floor: the arc waits for the pit, and there's a floor below to fall to.
        public bool DungeonPitWanted
        {
            get { return InDungeon && !Run.pitTaken && Run.floor < Run.floorsTotal && HearthHollow.Core.Arc.NextTrigger(_arc, Profile) == ArcTrigger.PitFall; }
        }

        /// Fall through the pit: down a floor, landing in the cache room. The Unity side rebuilds the floor on Changed.
        public bool DungeonFallIntoPit()
        {
            if (!DungeonPitWanted) return false;
            Run.pitTaken = true;
            Run.pitRoom = true;
            Run.floor++;
            ArcEvent(ArcTrigger.PitFall);
            Touch(true);
            return true;
        }

        /// The pit's cache: the next tier of each slot in OpeningArc.pitCache for the run's class, plus a treasure chest.
        public void DungeonPitCache()
        {
            if (!InDungeon) return;
            foreach (var slot in _arc.pitCache)
            {
                var t = Gear.Grant(Content, Profile, slot, Run.classId);
                if (t != null) Emit(new GameEvent(GameEventType.Loot, 0, true, Gear.SlotName(slot) + ": " + t.name + " (" + Gear.ValueText(slot, t) + ")"));
            }
            DungeonSecretChest(2);   // coins, ingredients, materials, a shot at a trinket
            Recompute();
            Touch(true);
        }

        // ───── dev ─────
        public void DevSetArcStage(int stage)
        {
            Profile.arcStage = Math.Max(0, Math.Min(stage, HearthHollow.Core.Arc.LastStage(_arc)));
            var s = ArcCurrent;
            Info("Dev: opening arc → stage " + Profile.arcStage + (s != null ? " (" + s.name + ")" : ""));
            Touch(true);
        }
    }
}
