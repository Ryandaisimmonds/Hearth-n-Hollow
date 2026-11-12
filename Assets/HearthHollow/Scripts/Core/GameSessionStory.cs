// Hearth & Hollow — the slice's story beats in town. Pure C#.
// Ticket 15: after you beat the Meadow Hollow floor-5 boss, the first time you're home Old Wren tells you the
// floor-10 boss fears fire and where chilies grow. That teaches its Fire weakness and reveals Chili Ridge. Once only.
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public partial class GameSession
    {
        public const string MeadowRoute = Ids.Area.Meadow, RidgeRoute = Ids.Area.Chili, FrostRoute = Ids.Area.FrostHollow;
        public const string WrenFrostFlag = Ids.Flag.WrenFrost;   // ticket 47: Wren has told you the way to Frost Hollow

        /// Ticket 47: Meadow Hollow's boss floor (the Bramble Sow, at the bottom).
        public int MeadowBossFloor { get { return DungeonRules.Floors(Content, MeadowRoute); } }

        /// Wren has the Fire hint to give: the Bramble Sow is down and she hasn't told you yet.
        public bool WrenFireHintDue
        {
            get
            {
                return Phase == Phase.Hearth && !Routes.HasFlag(Profile, DefaultContent.WrenFireHint)
                    && Progression.BeatBoss(Profile, MeadowRoute, MeadowBossFloor);
            }
        }

        /// The Hoarfrost Alpha, at the bottom of Frost Hollow (ticket 47; it used to be the Meadow's floor-10 boss).
        public string MeadowDeepBoss
        {
            get
            {
                string route = Content.Route(FrostRoute) != null ? FrostRoute : MeadowRoute;
                return DungeonRules.BossMonster(Content, new RunState { routeId = route, floor = DungeonRules.Floors(Content, route), dungeon = true });
            }
        }

        public List<string> WrenFireHintPages()
        {
            var boss = Content.Monster(MeadowDeepBoss);
            string bossName = boss != null ? boss.name : "the beast at the bottom";
            var ridge = Content.Route(RidgeRoute);
            string ridgeName = ridge != null ? ridge.name : "Chili Ridge";
            var chili = Content.Ingredient(Ids.Ingredient.Chili);
            return new List<string>
            {
                "Back from the Hollow with bramble in your hair! You beat that old Sow at the bottom, didn't you? Good. Then it's time you heard about the cold.",
                "Past the Hollow there's a colder place, Frost Hollow, and the " + bossName + " prowls at the bottom of it. Frost through and through: frost won't touch it, but it can't abide fire. 🔥 Hit it with Fire and it'll feel it, half again.",
                "Fire food's what you want. " + (chili != null ? chili.emoji + " " + chili.name + "s" : "Chilies") + " put Fire in your attacks. They grow wild up on " + ridgeName + ", past the east road. It's a quiet place, not much lives up there, so gather what you can. I've marked it on your route board.",
                "Off you go to Chili Ridge, then. Come back with the chilies and I'll show you what to do with them.",
            };
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

        /// Wren gives the hint: the boss's Fire weakness is known and Chili Ridge opens. True the first time.
        public bool GiveWrenFireHint()
        {
            if (!WrenFireHintDue) return false;
            Routes.SetFlag(Profile, DefaultContent.WrenFireHint);
            SetWren(WrenStep.Ridge);   // ticket 35
            DiscoverElement(MeadowDeepBoss, Element.Fire);
            var ridge = Content.Route(RidgeRoute);
            Info("🗺️ " + (ridge != null ? ridge.emoji + " " + ridge.name : "Chili Ridge") + " is on the route board. 🔥 The " + (Content.Monster(MeadowDeepBoss) != null ? Content.Monster(MeadowDeepBoss).name : "floor-10 boss") + " is weak to Fire.");
            Touch(true);
            return true;
        }
    }
}
