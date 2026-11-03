// Hearth & Hollow — Design v2 content changes (contentVersion 25+). Pure C#.
// v25 (tickets 01–03): Darkness is no longer a route hazard (deeper floors are simply darker), and move
// descriptions lose the Boost, shield-pip and Heat text for systems that were cut.
// v26 (ticket 06): every move gets an element from its old hit type (Fire and Light keep theirs; the rest have none).
// v27 (ticket 08): cooking v10. The element slot (infuse / guard) replaces accelerator stats, quality multipliers
// shrink to ×1.1 / ×1.2, three tier-2 dishes need ingredients found deep, combos become light utility bonuses,
// and XP bonuses (XP was cut in ticket 04) become coin bonuses.
// v28 (ticket 09): classes are a lens on food. Everyone starts on the same minimal base stats; each class gets
// some meal stats ×1.25 and others ×0.8 (the Cook gets every meal ×1.1 instead). Favourite stations go.
// v29 (ticket 10): the Cook's move pool. Pan & Ladle, Skewer Toss and Roll from the start; Butter Slide, Flambé,
// Grand Feast, Hot Pot, Cookfire and a second Ultimate (Five-Alarm Chili) unlock along the arc, learned in town.
// The Cook's other old moves (Skewer, Frying Pan, Rolling Pin) leave the pool.
// v30 (ticket 12): deep dungeons. The slice's Meadow is 10 floors with a boss every 5; Chili Ridge is 5 floors.
// v31 (ticket 13): Meadow Hollow. Nature monsters up top, Lantern Bees from floor 3 and Fog Wolves from floor 6;
// the Bramble Sow (Nature) guards floor 5 and the Hoarfrost Alpha (Frost: weak to Fire, resists Frost) floor 10.
// Ginger (Frost guard) drops from floor 4; the three tier-2 ingredients from floor 6; timber is its material.
// v32 (ticket 14): Chili Ridge. A 5-floor side dungeon, hidden until Old Wren's directions (flag "wren_fire_hint",
// ticket 15). Fire-leaning monsters, the Ridge Drake on floor 5, chilies (Fire infusion) and mint (Fire guard), ember coal.
// v33 (ticket 16): utility gear. A lantern track (light reach) joins boots and bag; four slice trinkets that bend rules.
// v34 (ticket 17): the Lamp-maker, three stages built with Meadow timber and Ridge ember coal (light reach + Flare).
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HearthHollow.Core
{
    public static partial class DefaultContent
    {
        static void DesignV2Cuts25(GameContent c)
        {
            foreach (var r in c.routes)
                if (r != null && r.hazard == Hazard.Darkness) { r.hazard = Hazard.None; r.hazardPct = 0f; }

            foreach (var m in c.moves)
            {
                if (m == null || string.IsNullOrEmpty(m.description)) continue;
                string d = m.description;
                d = Regex.Replace(d, @"\s*Boost:[^.]*\.", "");                       // "Boost: a bigger wheel."
                d = Regex.Replace(d, @" that knocks off \d+ shield pips", "");
                d = d.Replace(" (+20 Heat)", "");
                d = d.Replace(" Every hit stokes your Heat.", "");
                m.description = d.Trim();
            }
            foreach (var k in c.classes)
                if (k != null && k.description != null)
                    k.description = k.description.Replace(" Every hit stokes her Heat, and her light grows from a candle to a bonfire.", "");
            var det = c.Move("e_detonate");
            if (det != null) det.description = "One big explosion of fire around you (Fire).";
            var sn = c.Move("e_ult");
            if (sn != null) sn.description = "Pull in every ember, then a huge explosion of fire around you (Fire) that stuns.";
        }

        public const int MinimalBaseHp = 14;

        static void Classes28(GameContent c)
        {
            foreach (var k in c.classes)
            {
                if (k == null) continue;
                k.baseMaxHp = MinimalBaseHp; k.damageMult = 1f; k.gatherZoneMult = 1f; k.mealBuffMult = 1f;
                k.mealStatMults = new List<StatMod>();
                switch (k.id)
                {
                    case ClassId.Warden:
                        Lens(k, 1.25f, BuffStat.MaxHp, BuffStat.Armour); Lens(k, 0.8f, BuffStat.MoveSpeedPct);
                        k.description = "A steady guardian of the campfire. Gets the most out of hearty food (health, armour); light, quick food does less for her.";
                        break;
                    case ClassId.Ranger:
                        Lens(k, 1.25f, BuffStat.MoveSpeedPct, BuffStat.CritPct, BuffStat.GatherZonePct); Lens(k, 0.8f, BuffStat.MaxHp);
                        k.description = "Knows the wilds by heart. Trail food sharpens her (speed, crit, gathering); stodgy food does less for her.";
                        break;
                    case ClassId.Cook:
                        k.mealBuffMult = 1.1f;
                        k.description = "Every meal tastes like home: every meal stat is a little stronger for the Cook.";
                        break;
                    case ClassId.Lamplighter:
                        Lens(k, 1.25f, BuffStat.HealOnKill, BuffStat.CoinPct); Lens(k, 0.8f, BuffStat.Armour);
                        k.description = "Keeper of the old lamps. Her light magic seeks out its targets. Restoring food suits her (heal on kill, coins); armour food does less for her.";
                        break;
                    case ClassId.Emberfist:
                        Lens(k, 1.25f, BuffStat.DamagePct); Lens(k, 0.8f, BuffStat.GatherZonePct);
                        k.description = "A wandering monk who fights with burning fists. Fiery food hits hardest for her (damage); she's too impatient to gather well.";
                        break;
                }
            }
        }

        static void Lens(ClassData k, float mult, params BuffStat[] stats)
        {
            foreach (var st in stats) k.mealStatMults.Add(new StatMod { stat = st, value = mult });
        }

        static void CookPool29(GameContent c)
        {
            if (c.moveUnlocks == null) c.moveUnlocks = new List<MoveUnlock>();
            var cook = c.classes.Find(x => x.id == ClassId.Cook);
            string sig = cook != null ? cook.ultimate : "c_ult";
            Ult(c, "c_ult2", ClassId.Cook, MoveKind.Supernova, "Five-Alarm Chili", "🌶️",
                "Bite the hottest chili in the bag: a moment later a huge burst of fire erupts around you (Fire), hitting and stunning everything nearby.",
                HitType.Fire, 3.0f, 1f, 3.4f, 1.5f, 0f, 12f);
            var u2 = c.Move("c_ult2");
            if (u2 != null) { u2.element = Element.Fire; u2.cooldown = 22.5f; }
            if (cook != null) cook.ultimate = string.IsNullOrEmpty(sig) ? "c_ult" : sig;   // Grand Feast stays the signature

            Pool(c, "c_ladle", UnlockKind.Start, null, 0, null);
            Pool(c, "c_snack", UnlockKind.Start, null, 0, null);
            Pool(c, "c_slide", UnlockKind.ReachFloor, "meadow", 2, "kitchen");
            Pool(c, "c_pepper", UnlockKind.ReachFloor, "meadow", 3, "kitchen");
            Pool(c, "c_ult", UnlockKind.ReachFloor, "meadow", 4, "yard");
            Pool(c, "c_hotpot", UnlockKind.BeatBoss, "meadow", 5, "kitchen");
            Pool(c, "c_picnic", UnlockKind.ReachFloor, "chili", 3, "kitchen");
            Pool(c, "c_ult2", UnlockKind.BeatBoss, "chili", 5, "yard");
        }

        static void Pool(GameContent c, string moveId, UnlockKind kind, string routeId, int floor, string learnAt)
        {
            if (c.Move(moveId) == null) return;
            var u = c.moveUnlocks.Find(x => x != null && x.moveId == moveId);
            if (u == null) { u = new MoveUnlock { moveId = moveId }; c.moveUnlocks.Add(u); }
            u.kind = kind; u.routeId = routeId; u.floor = floor; u.learnAt = learnAt;
        }

        static void DeepDungeons30(GameContent c)
        {
            var meadow = c.Route("meadow");
            if (meadow != null) { meadow.floors = 10; meadow.bossEvery = 5; }
            var chili = c.Route("chili");
            if (chili != null) { chili.floors = 5; chili.bossEvery = 5; }
        }

        static void MeadowHollow31(GameContent c)
        {
            AddMonster(c, "bramble_sow", "Bramble Sow", "🐗", "boar", MonsterBehaviour.None, 1.3f);
            AddMonster(c, "hoarfrost_alpha", "Hoarfrost Alpha", "🐺", "wolf", MonsterBehaviour.DoubleHit, 1.5f);
            var r = c.Route("meadow");
            if (r == null) return;
            r.name = "Meadow Hollow";
            r.description = "Sunlit grass and sleepy slimes up top. It goes down a long way, and gets colder the deeper you go.";
            r.storyIntro = "The meadow dips into a hollow, and the hollow goes down — ten floors of it. Something bristly snores on the fifth. Something colder waits at the bottom.";
            r.floors = 10; r.bossEvery = 5;
            r.monsterIds = new List<string> { "slime", "boar" };
            r.bossIds = new List<string> { "bramble_sow", "hoarfrost_alpha" };
            r.floorMonsters = new List<FloorMonster>
            {
                new FloorMonster { monsterId = "bat", minFloor = 3 },
                new FloorMonster { monsterId = "wolf", minFloor = 6 },
            };
            r.gatherIngredientIds = new List<string> { "herbs", "wheat", "mushroom", "berries" };
            r.floorDrops = new List<FloorDrop>
            {
                new FloorDrop { ingredientId = "ginger", minFloor = 4, weight = 0.6f },
                new FloorDrop { ingredientId = "starmoss", minFloor = 6, weight = 0.35f },
                new FloorDrop { ingredientId = "wild_honeycomb", minFloor = 6, weight = 0.35f },
                new FloorDrop { ingredientId = "golden_wheat", minFloor = 6, weight = 0.35f },
            };
            r.floorDrops.RemoveAll(d => c.Ingredient(d.ingredientId) == null);
            if (r.materialIds == null || r.materialIds.Count == 0) r.materialIds = new List<string> { "timber" };
        }

        static void AddMonster(GameContent c, string id, string name, string emoji, string artId, MonsterBehaviour b, float hp)
        {
            var m = c.monsters.Find(x => x.id == id);
            if (m == null) { m = new MonsterData { id = id }; c.monsters.Add(m); }
            m.name = name; m.emoji = emoji; m.artId = artId; m.behaviour = b; m.hpMult = hp; m.eliteOnly = true;   // bosses only, never in normal pools
        }

        public const string WrenFireHint = "wren_fire_hint";

        static void ChiliRidge32(GameContent c)
        {
            AddMonster(c, "ridge_drake", "Ridge Drake", "🐉", "dragon", MonsterBehaviour.WindUp, 1.3f);
            var r = c.Route("chili");
            if (r == null) return;
            r.name = "Chili Ridge";
            r.description = "Red hills where chilies grow wild. Hot-tempered things nest up here.";
            r.storyIntro = "Old Wren's directions bring you up into the red hills. Chilies grow wild here, and so do tempers. Something hot-blooded nests at the top.";
            r.chapter = 0;                       // a side dungeon you trek to, not a story chapter
            r.unlockedByRouteId = ""; r.requiredKeyItem = "";
            r.requiredFlag = WrenFireHint;
            r.lockHint = "Old Wren might know the way";
            r.classLocked = false; r.hazard = Hazard.None; r.hazardPct = 0f; r.mutators.Clear();
            r.floors = 5; r.bossEvery = 5; r.baseDifficulty = 3; r.clearReward = 40;
            r.monsterIds = new List<string> { "bat", "boar" };
            r.floorMonsters = new List<FloorMonster> { new FloorMonster { monsterId = "shaman", minFloor = 3 } };
            r.bossIds = new List<string> { "ridge_drake" };
            r.gatherIngredientIds = new List<string> { "chili", "garlic", "wheat" };
            r.floorDrops = new List<FloorDrop> { new FloorDrop { ingredientId = "mint", minFloor = 2, weight = 0.5f } };
            r.floorDrops.RemoveAll(d => c.Ingredient(d.ingredientId) == null);
            r.materialIds = new List<string> { "ember_coal" };
        }

        static void UtilityGear33(GameContent c)
        {
            if (c.lanternTiers == null) c.lanternTiers = new List<GearTier>();
            if (c.lanternTiers.Count == 0)
            {
                Gear(c.lanternTiers, 0, "Tallow Stub", 0, 0f);
                Gear(c.lanternTiers, 1, "Tin Lantern", 40, 0.15f);
                Gear(c.lanternTiers, 2, "Brass Lantern", 90, 0.3f);
                Gear(c.lanternTiers, 3, "Glass Lantern", 160, 0.45f);
                Gear(c.lanternTiers, 4, "Hearthglow Lantern", 260, 0.6f);
            }
            foreach (var t in c.trinkets) t.slice = false;
            Trinket33(c, "ember_locket", "Ember Locket", "🔥", "Every kill takes 3 s off Flare's cooldown.", TrinketPerk.FlareOnKill, 3f, 60);
            Trinket33(c, "magpie_spoon", "Magpie Spoon", "🥄", "Monsters drop ingredients a lot more often.", TrinketPerk.LootOnKill, 0.2f, 90);
            Trinket33(c, "pocket_hearth", "Pocket Hearth", "🕯️", "Once a run, a blow that would fell you leaves you on 1 HP instead.", TrinketPerk.SecondWind, 1f, 140);
            var honey = c.trinkets.Find(x => x.id == "honeylocket");
            if (honey != null) { honey.slice = true; honey.inWorkshop = true; honey.price = 100; honey.shopChapter = 1; }
        }

        static void Trinket33(GameContent c, string id, string name, string emoji, string desc, TrinketPerk perk, float value, int price)
        {
            var t = c.trinkets.Find(x => x.id == id);
            if (t == null) { t = new TrinketData { id = id }; c.trinkets.Add(t); }
            t.name = name; t.emoji = emoji; t.description = desc; t.perk = perk; t.perkValue = value;
            t.price = price; t.inWorkshop = true; t.shopChapter = 1; t.slice = true;
            if (t.buffs == null) t.buffs = new List<StatMod>();
        }

        static void LampMaker34(GameContent c)
        {
            if (c.lampMaker == null) c.lampMaker = new List<LampStage>();
            if (c.lampMaker.Count > 0) return;
            c.lampMaker.Add(new LampStage { level = 1, name = "Lamp-maker's stall", price = 30, materials = new List<IngredientCost> { Cost("timber", 6) },
                lightReach = 0.1f, flareRadius = 0.1f, flareCooldownCut = 0f,
                description = "A stall and a lamp-maker who trims your wick properly. Your light reaches a little further, and Flare a little wider." });
            c.lampMaker.Add(new LampStage { level = 2, name = "Lamp-maker's workshop", price = 80, materials = new List<IngredientCost> { Cost("timber", 10), Cost("ember_coal", 4) },
                lightReach = 0.2f, flareRadius = 0.2f, flareCooldownCut = 3f,
                description = "A proper workshop with a coal kiln for glass. Brighter carried light, and Flare comes back quicker." });
            c.lampMaker.Add(new LampStage { level = 3, name = "Lighthouse workshop", price = 160, materials = new List<IngredientCost> { Cost("timber", 12), Cost("ember_coal", 8) },
                lightReach = 0.35f, flareRadius = 0.35f, flareCooldownCut = 6f,
                description = "A tall workshop with a lamp on top you can see from the gate. The best light in the valley." });
        }

        static void Elements26(GameContent c)
        {
            foreach (var m in c.moves)
            {
                if (m == null) continue;
                m.element = Elements.FromHitType(m.hitType);
                var e2 = Elements.FromHitType(m.hitType2);
                m.element2 = e2 != m.element ? e2 : Element.None;
            }
        }

        static void Cooking27(GameContent c)
        {
            // ── the element slot: infuse or guard (the old stat boosts go) ──
            SlotRole(c, "chili", Element.Fire, Element.None);
            SlotRole(c, "fire_honey", Element.Fire, Element.None);
            SlotRole(c, "frost_berry", Element.Frost, Element.None);
            SlotRole(c, "nettle", Element.Nature, Element.None);
            SlotRole(c, "salt", Element.Water, Element.None);
            SlotRole(c, "fish", Element.Water, Element.None);
            SlotRole(c, "glowcap", Element.Light, Element.None);
            SlotRole(c, "mint", Element.None, Element.Fire);
            SlotRole(c, "ginger", Element.None, Element.Frost);
            SlotRole(c, "garlic", Element.None, Element.Nature);
            SlotRole(c, "herbs", Element.None, Element.Nature);
            SlotRole(c, "wheat", Element.None, Element.Water);
            foreach (var i in c.ingredients) if (i != null && i.accelBuffs != null) i.accelBuffs.Clear();

            // ── quality: a small bump, not a power track ──
            if (Near(c.tuning.gradeMultUncommon, 1.25f)) c.tuning.gradeMultUncommon = 1.1f;
            if (Near(c.tuning.gradeMultRare, 1.5f)) c.tuning.gradeMultRare = 1.2f;

            // ── no XP any more: XP bonuses become coin bonuses ──
            foreach (var r in c.recipes) XpToCoins(r.buffs);
            foreach (var t in c.trinkets) XpToCoins(t.buffs);
            foreach (var sy in c.synergies) XpToCoins(sy.bonus);

            // ── combos: fun, light utility bonuses with little effect on power ──
            Combo(c, "balanced_feast", "One meal from each station: +10% coins.", BuffStat.CoinPct, 0.1f);
            Combo(c, "comfort_food", "3 of the same meal: +5% speed.", BuffStat.MoveSpeedPct, 0.05f);
            Combo(c, "twin_flame", "Eat 2 fiery meals: +15% gather zone.", BuffStat.GatherZonePct, 0.15f);
            Combo(c, "stone_stomach", "Eat 2 hearty meals: +1 HP per kill.", BuffStat.HealOnKill, 1f);
            Combo(c, "slow_simmer", "All Pot meals: +1 HP per kill.", BuffStat.HealOnKill, 1f);
            Combo(c, "grill_master", "All BBQ meals: +15% coins.", BuffStat.CoinPct, 0.15f);
            Combo(c, "packed_lunch", "All Sandwich meals: +20% gather zone.", BuffStat.GatherZonePct, 0.2f);

            // ── deep ingredients and the tier-2 dishes that need them (ticket 13 places the drops) ──
            Deep(c, "starmoss", "Starmoss", "🪴");
            Deep(c, "wild_honeycomb", "Wild Honeycomb", "🍯");
            Deep(c, "golden_wheat", "Golden Wheat", "🌾");
            Tier2(c, "herb_stew", "herb_stew_2", "Hearty Herb Stew", "starmoss", 1.75f, "A thick stew with Starmoss from deep in the Meadow.");
            Tier2(c, "mushroom_skewer", "mushroom_skewer_2", "Honey-Glazed Skewer", "wild_honeycomb", 1.75f, "Mushroom skewers glazed with wild honeycomb.");
            Tier2(c, "herb_butty", "herb_butty_2", "Golden Herb Butty", "golden_wheat", 1.75f, "A herb butty on golden-wheat bread.");
        }


        static void SlotRole(GameContent c, string id, Element infuse, Element guard)
        {
            var i = c.Ingredient(id);
            if (i == null) return;
            i.infuse = infuse; i.guard = guard;
        }

        static void XpToCoins(List<StatMod> mods)
        {
            if (mods == null) return;
            var xp = mods.Find(m => m.stat == BuffStat.XpPct);
            if (xp == null) return;
            mods.Remove(xp);
            var coins = mods.Find(m => m.stat == BuffStat.CoinPct);
            if (coins != null) coins.value += xp.value; else mods.Add(new StatMod { stat = BuffStat.CoinPct, value = xp.value });
        }

        static void Combo(GameContent c, string id, string text, BuffStat stat, float value)
        {
            var sy = c.synergies.Find(x => x.id == id);
            if (sy == null) return;
            sy.allBuffMultiplier = 1f; sy.sameRecipeBonus = 0f;
            sy.bonus = new List<StatMod> { new StatMod { stat = stat, value = value } };
            sy.description = text;
        }

        static void Deep(GameContent c, string id, string name, string emoji)
        {
            if (c.Ingredient(id) != null) return;
            c.ingredients.Add(new IngredientData { id = id, name = name, emoji = emoji, tier = 2, deep = true });
        }

        static void Tier2(GameContent c, string baseId, string id, string name, string deepId, float mult, string description)
        {
            var b = c.Recipe(baseId);
            if (b == null || c.Recipe(id) != null) return;
            var r = new RecipeData
            {
                id = id, name = name, emoji = b.emoji, station = b.station, description = description,
                rarity = b.rarity, stationLevel = b.stationLevel, tier = 2, upgradeOf = baseId,
                tags = new List<string>(b.tags ?? new List<string>()),
            };
            foreach (var ic in b.ingredients) r.ingredients.Add(new IngredientCost { ingredientId = ic.ingredientId, count = ic.count });
            r.ingredients.Add(new IngredientCost { ingredientId = deepId, count = 1 });
            foreach (var m in b.buffs) r.buffs.Add(new StatMod { stat = m.stat, value = m.value * mult });
            c.recipes.Insert(c.recipes.IndexOf(b) + 1, r);
        }
    }
}
