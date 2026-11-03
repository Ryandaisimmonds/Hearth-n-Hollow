// Hearth & Hollow — the slice's story beats in town. Pure C#.
// Ticket 15: after you beat the Meadow Hollow floor-5 boss, the first time you're home Old Wren tells you the
// floor-10 boss fears fire and where chilies grow. That teaches its Fire weakness and reveals Chili Ridge. Once only.
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public partial class GameSession
    {
        public const string MeadowRoute = "meadow", RidgeRoute = "chili";

        /// Wren has the Fire hint to give: the Meadow floor-5 boss is down and she hasn't told you yet.
        public bool WrenFireHintDue
        {
            get
            {
                return Phase == Phase.Hearth && !Routes.HasFlag(Profile, DefaultContent.WrenFireHint)
                    && Progression.BeatBoss(Profile, MeadowRoute, 5);
            }
        }

        /// The floor-10 boss of the Meadow (the Hoarfrost Alpha).
        public string MeadowDeepBoss
        {
            get
            {
                var r = Content.Route(MeadowRoute);
                return DungeonRules.BossMonster(Content, new RunState { routeId = MeadowRoute, floor = DungeonRules.Floors(Content, MeadowRoute), dungeon = true });
            }
        }

        public List<string> WrenFireHintPages()
        {
            var boss = Content.Monster(MeadowDeepBoss);
            string bossName = boss != null ? boss.name : "the beast at the bottom";
            var ridge = Content.Route(RidgeRoute);
            string ridgeName = ridge != null ? ridge.name : "Chili Ridge";
            var chili = Content.Ingredient("chili");
            return new List<string>
            {
                "Back from the Hollow with bramble in your hair! You beat that old Sow on the fifth floor, didn't you? Good. Then it's time you heard what's waiting further down.",
                "At the very bottom, on the tenth floor, sleeps the " + bossName + ". Frost through and through: frost won't touch it, but it can't abide fire. 🔥 Hit it with Fire and it'll feel it, half again.",
                "Fire food's what you want. " + (chili != null ? chili.emoji + " " + chili.name + "s" : "Chilies") + " put Fire in your attacks: cook one into a meal's element slot. They only grow up on " + ridgeName + ", past the east road. I've marked it on your route board.",
                "And pack something warm. A bit of ginger in a meal will guard you from its bite. Off you go now, and mind the drake that nests up there.",
            };
        }

        /// Wren gives the hint: the boss's Fire weakness is known and Chili Ridge opens. True the first time.
        public bool GiveWrenFireHint()
        {
            if (!WrenFireHintDue) return false;
            Routes.SetFlag(Profile, DefaultContent.WrenFireHint);
            DiscoverElement(MeadowDeepBoss, Element.Fire);
            var ridge = Content.Route(RidgeRoute);
            Info("🗺️ " + (ridge != null ? ridge.emoji + " " + ridge.name : "Chili Ridge") + " is on the route board. 🔥 The " + (Content.Monster(MeadowDeepBoss) != null ? Content.Monster(MeadowDeepBoss).name : "floor-10 boss") + " is weak to Fire.");
            Touch(true);
            return true;
        }
    }
}
