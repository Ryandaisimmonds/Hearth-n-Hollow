// Hearth & Hollow — Design v2 content changes (contentVersion 25+). Pure C#.
// v25 (tickets 01–03): Darkness is no longer a route hazard (deeper floors are simply darker), and move
// descriptions lose the Boost, shield-pip and Heat text for systems that were cut.
// v26 (ticket 06): every move gets an element from its old hit type (Fire and Light keep theirs; the rest have none).
// v27 (ticket 08): cooking v10. The element slot (infuse / guard) replaces accelerator stats, quality multipliers
// shrink to ×1.1 / ×1.2, three tier-2 dishes need ingredients found deep, combos become light utility bonuses,
// and XP bonuses (XP was cut in ticket 04) become coin bonuses.
// v28 (ticket 09): classes are a lens on food. Everyone starts on the same minimal base stats; each class gets
// some meal stats ×1.25 and others ×0.8 (the Cook gets every meal ×1.1 instead). Favourite stations go.
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
