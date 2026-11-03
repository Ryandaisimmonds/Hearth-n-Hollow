// Hearth & Hollow — GameSession: the opening arc (OpeningArc.cs) — stages, the town areas they open and the dash lock. Pure C#.
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

        /// × the walk speed (the opening's 75% start was cut in Design v2; kept at 1 for callers).
        public float ArcSpeed { get { return 1f; } }
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
