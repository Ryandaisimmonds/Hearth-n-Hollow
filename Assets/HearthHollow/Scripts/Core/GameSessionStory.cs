// Hearth & Hollow — GameSession: the story (Story.cs) and the town calls that go with it. Pure C#.
// The opening arc, Old Wren's thread and the opening fall live in Story; GameSession owns one and calls its hooks.
// The few story answers used all over the game are forwarded here; for the rest, ask Session.Story.

namespace HearthHollow.Core
{
    public partial class GameSession
    {
        Story _story;
        /// The opening arc, Old Wren's guiding thread and the opening fall.
        public Story Story { get { return _story ?? (_story = new Story(this)); } }

        /// Set from the Dungeon Settings asset at start-up (GameBootstrap); a default arc otherwise.
        public OpeningArc Arc { get { return Story.Arc; } set { Story.Arc = value; } }
        public int ArcStage { get { return Profile.arcStage; } }
        public WrenStep Wren { get { return Story.Step; } }
        public bool InFall { get { return Story.InFall; } }
        public const string FallRoute = Story.FallRoute;
        /// A town area (kitchen, smithy, workshop, …) is open at this stage of the arc.
        public bool AreaOpen(string area) { return Story.AreaOpen(area); }
        public bool DashUnlocked { get { return Story.DashUnlocked; } }

        /// × the walk speed (the opening's 75% start was cut in Design v2; kept at 1 for callers).
        public float ArcSpeed { get { return 1f; } }
        /// Boots, charms and skill nodes for the selected class (no meals): for walking about town.
        public CombatMods TownMods { get { return Progression.Mods(Content, Profile, Profile.selectedClass); } }

        /// Ticket 40: why you can't set out yet: the tray must be full (any meals, Camp Bread included). Not on the first dive.
        public string TrayBlock
        {
            get
            {
                if (!Slice.On || Profile.arcStage == 0 || !Story.TrayShown) return null;
                int n = Profile.tray.Count, max = Content.tuning.traySize;
                if (n < max) return "Pack " + max + " meals to set out (" + n + "/" + max + ")";
                var missing = Tray.Missing(Content, Profile.tray);   // ticket 44: an old tray of three Health meals, say
                return missing.Count > 0 ? "Your tray needs " + Tray.Need(missing) : null;
            }
        }

        /// Build the next stage of the Lamp-maker (in town, with coins and biome materials). Ticket 17.
        public bool BuildLampMaker(out string reason)
        {
            if (Phase != Phase.Hearth) { reason = "Only in town"; return false; }
            if (!LampMaker.Build(Content, Profile, out reason)) return false;
            var s = LampMaker.Current(Content, Profile);
            Info("🏮 " + (s != null ? s.name : "The Lamp-maker") + " is built! " + LampMaker.EffectText(s));
            Touch(true);
            return true;
        }
    }
}
