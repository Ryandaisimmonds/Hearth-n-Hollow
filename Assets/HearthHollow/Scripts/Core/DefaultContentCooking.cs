// Hearth & Hollow — cooking overhaul content (contentVersion 9). Pure C#.
// 21 dishes replace the old 30 (Camp Bread is always available, the other 20 are cooked), by station role: 🍲 Pot = defence & healing · 🍖 BBQ = damage · 🥪 Sandwich = speed & utility.
// Every dish number is the COMMON value; Uncommon and Rare meals multiply it (tuning.gradeMult*). Ingredients are Plain / Fine / Prime.
// Hazard guards moved from special dishes to accelerators: put nettle / mint / ginger / glowcap in any meal's accelerator slot.
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public static partial class DefaultContent
    {
        static void BuildCooking9(GameContent c)
        {
            c.recipes.Clear();

            // ───── 🍲 Pot: defence & healing (max HP, armour, heal on kill) ─────
            Dish(c, "herb_stew", "Herb Stew", "🍲", Station.Pot, new[] { Cost("herbs", 2), Cost("mushroom", 1) },
                new[] { Mod(BuffStat.MaxHp, 18) }, new[] { "stone", "veg" },
                "A thick, earthy stew that sits in your belly like a warm stone. Sturdier all day.");
            Dish(c, "berry_porridge", "Berry Porridge", "🥣", Station.Pot, new[] { Cost("wheat", 1), Cost("berries", 2) },
                new[] { Mod(BuffStat.MaxHp, 10), Mod(BuffStat.HealOnKill, 1) }, new[] { "sweet" },
                "Creamy oats swirled with bramble berries. Every win feels like a second breakfast.");
            Dish(c, "marsh_chowder", "Marsh Chowder", "🍜", Station.Pot, new[] { Cost("fish", 2), Cost("salt", 1) },
                new[] { Mod(BuffStat.MaxHp, 16), Mod(BuffStat.Armour, 2) }, new[] { "stone", "fish" },
                "Salty, hearty and steaming. Tastes of foggy mornings.");
            Dish(c, "nettle_soup", "Nettle Soup", "🥬", Station.Pot, new[] { Cost("nettle", 2), Cost("herbs", 1) },
                new[] { Mod(BuffStat.MaxHp, 10), Mod(BuffStat.HealOnKill, 2) }, new[] { "veg" },
                "Sharp, green and cleansing. Patches you up between fights.");
            Dish(c, "truffle_risotto", "Truffle Risotto", "🍚", Station.Pot, new[] { Cost("truffle", 1), Cost("mushroom", 1), Cost("wheat", 1) },
                new[] { Mod(BuffStat.Armour, 3), Mod(BuffStat.MaxHp, 12) }, new[] { "stone", "veg" },
                "Slow-stirred by lantern light. Rich enough to shrug off a blow.");
            Dish(c, "lantern_soup", "Lantern Egg Soup", "🍵", Station.Pot, new[] { Cost("cave_egg", 1), Cost("ginger", 1), Cost("salt", 1) },
                new[] { Mod(BuffStat.MaxHp, 28), Mod(BuffStat.Armour, 1) }, new[] { "stone" },
                "Glowing egg ribbons in a ginger broth. Fortifies body and spirit.");
            Dish(c, "drake_hotpot", "Drake Hotpot", "🍛", Station.Pot, new[] { Cost("ember_meat", 2), Cost("ginger", 1), Cost("herbs", 1) },
                new[] { Mod(BuffStat.MaxHp, 24), Mod(BuffStat.Armour, 2), Mod(BuffStat.HealOnKill, 2) }, new[] { "stone", "fire" },
                "Drake haunch simmered all night. The heartiest thing in the valley.");

            // ───── 🍖 BBQ: damage (damage, crit) ─────
            Dish(c, "grilled_fish", "Salt-Crust Fish", "🐟", Station.BBQ, new[] { Cost("fish", 1), Cost("salt", 1) },
                new[] { Mod(BuffStat.DamagePct, 0.2f), Mod(BuffStat.CritPct, 0.08f) }, new[] { "smoky", "fish" },
                "Baked in a salt shell on the coals. Sharpens the senses.");
            Dish(c, "mushroom_skewer", "Mushroom Skewer", "🍢", Station.BBQ, new[] { Cost("mushroom", 2), Cost("herbs", 1) },
                new[] { Mod(BuffStat.DamagePct, 0.3f), Mod(BuffStat.CritPct, 0.04f) }, new[] { "smoky", "veg" },
                "Charred caps on a stick, herby and smoky.");
            Dish(c, "truffle_caps", "Charred Truffle Caps", "🍄", Station.BBQ, new[] { Cost("truffle", 1), Cost("mushroom", 2) },
                new[] { Mod(BuffStat.DamagePct, 0.3f), Mod(BuffStat.CritPct, 0.1f) }, new[] { "smoky", "veg" },
                "Truffle shavings melting over blackened caps. Every swing finds the gap.");
            Dish(c, "glowcap_kebab", "Glowcap Kebab", "🍡", Station.BBQ, new[] { Cost("glowcap", 1), Cost("mushroom", 1), Cost("garlic", 1) },
                new[] { Mod(BuffStat.DamagePct, 0.4f), Mod(BuffStat.CritPct, 0.06f) }, new[] { "smoky", "veg" },
                "Glowcaps charred until they sparkle.");
            Dish(c, "firecracker_skewer", "Firecracker Skewer", "🌶️", Station.BBQ, new[] { Cost("chili", 2), Cost("mushroom", 1) },
                new[] { Mod(BuffStat.DamagePct, 0.55f) }, new[] { "fire", "smoky" },
                "Pops and crackles on the grill — and in your mouth.");
            Dish(c, "honey_ribs", "Fire-Honey Ribs", "🍖", Station.BBQ, new[] { Cost("ember_meat", 1), Cost("fire_honey", 2) },
                new[] { Mod(BuffStat.DamagePct, 0.4f), Mod(BuffStat.CritPct, 0.14f) }, new[] { "fire", "sweet" },
                "Sticky, spicy-sweet glaze. Every bite feels lucky.");
            Dish(c, "ember_steak", "Ember Steak", "🥩", Station.BBQ, new[] { Cost("ember_meat", 2), Cost("garlic", 1), Cost("chili", 1) },
                new[] { Mod(BuffStat.DamagePct, 0.75f), Mod(BuffStat.CritPct, 0.05f) }, new[] { "fire" },
                "Seared over drake-coals. Pure, glowing strength.");

            // ───── 🥪 Sandwich: speed & utility (move speed, gather zone, coins, XP) ─────
            Dish(c, "bread", "Camp Bread", "🍞", Station.Sandwich, new IngredientCost[0],
                new[] { Mod(BuffStat.MoveSpeedPct, 0.03f), Mod(BuffStat.GatherZonePct, 0.1f) }, new[] { "plain" },
                "Free — there's always a loaf by the fire. Simple, light, better than nothing. Always Plain.");
            c.Recipe("bread").always = true;   // always available: never cooked, never runs out (see FixBread)
            Dish(c, "herb_butty", "Herb Butty", "🥪", Station.Sandwich, new[] { Cost("wheat", 2), Cost("herbs", 1) },
                new[] { Mod(BuffStat.GatherZonePct, 0.3f), Mod(BuffStat.MoveSpeedPct, 0.05f) }, new[] { "veg" },
                "Fresh bread and a fistful of herbs. A forager's favourite.");
            Dish(c, "bramble_jam", "Bramble Jam Sandwich", "🍓", Station.Sandwich, new[] { Cost("wheat", 1), Cost("berries", 2) },
                new[] { Mod(BuffStat.CoinPct, 0.2f), Mod(BuffStat.XpPct, 0.1f) }, new[] { "sweet" },
                "Sticky fingers, sunny mood. Coins seem to find you.");
            Dish(c, "marsh_butty", "Marsh Fish Butty", "🥖", Station.Sandwich, new[] { Cost("wheat", 1), Cost("fish", 1), Cost("salt", 1) },
                new[] { Mod(BuffStat.CoinPct, 0.25f), Mod(BuffStat.MoveSpeedPct, 0.05f) }, new[] { "fish" },
                "Crispy fish in a crusty roll. Proper wanderer's food.");
            Dish(c, "trail_wrap", "Minty Trail Wrap", "🌯", Station.Sandwich, new[] { Cost("wheat", 1), Cost("mint", 1), Cost("herbs", 1) },
                new[] { Mod(BuffStat.MoveSpeedPct, 0.1f), Mod(BuffStat.GatherZonePct, 0.15f) }, new[] { "veg" },
                "Cool mint and herbs rolled tight. Puts a spring in your step.");
            Dish(c, "glow_egg_sandwich", "Glow-Egg Sandwich", "🥚", Station.Sandwich, new[] { Cost("cave_egg", 1), Cost("truffle", 1), Cost("wheat", 1) },
                new[] { Mod(BuffStat.XpPct, 0.3f), Mod(BuffStat.GatherZonePct, 0.2f) }, new[] { "stone" },
                "Shimmering egg and truffle shavings. Oddly enlightening.");
            Dish(c, "frostberry_toast", "Frostberry Toast", "🥞", Station.Sandwich, new[] { Cost("wheat", 1), Cost("frost_berry", 2), Cost("fire_honey", 1) },
                new[] { Mod(BuffStat.MoveSpeedPct, 0.12f), Mod(BuffStat.CoinPct, 0.15f), Mod(BuffStat.XpPct, 0.15f) }, new[] { "sweet" },
                "Frostberries and fire honey on hot toast. You'll be halfway down the trail before it's cold.");

            // ───── accelerators: one optional slot on every meal; always the base boost (accelerators have no grade) ─────
            Accel(c, "garlic", new[] { Mod(BuffStat.DamagePct, 0.1f) }, Hazard.None);
            Accel(c, "fire_honey", new[] { Mod(BuffStat.CritPct, 0.04f) }, Hazard.None);
            Accel(c, "salt", new[] { Mod(BuffStat.Armour, 1) }, Hazard.None);
            Accel(c, "herbs", new[] { Mod(BuffStat.MaxHp, 6) }, Hazard.None);
            Accel(c, "frost_berry", new[] { Mod(BuffStat.MoveSpeedPct, 0.04f) }, Hazard.None);
            Accel(c, "nettle", new StatMod[0], Hazard.Poison);
            Accel(c, "mint", new StatMod[0], Hazard.Heat);
            Accel(c, "ginger", new StatMod[0], Hazard.Cold);
            Accel(c, "glowcap", new StatMod[0], Hazard.Darkness);
        }

        /// contentVersion 10/11: Camp Bread is an ALWAYS-AVAILABLE meal (RecipeData.always) — a permanent card beside the
        /// meal pantry that never runs out, never uses a pantry slot and is packed straight onto the tray as a Common meal.
        /// (Version 10 briefly made it an endless ingredient instead; this undoes that: the bread ingredient goes and the
        /// sandwiches get their wheat recipes back.) Safe to run more than once.
        static void FixBread(GameContent c)
        {
            var fresh = Create();   // the current default menu (Camp Bread as an always-available meal)
            c.ingredients.RemoveAll(i => i.id == "bread");
            foreach (var fr in fresh.recipes)
            {
                if (fr.station != Station.Sandwich) continue;
                var r = c.Recipe(fr.id);
                if (r != null && r.ingredients.Exists(x => x.ingredientId == "bread")) r.ingredients = new List<IngredientCost>(fr.ingredients);
            }
            var bread = c.Recipe("bread");
            if (bread == null)
            {
                bread = fresh.Recipe("bread");
                int at = c.recipes.FindIndex(x => x.station == Station.Sandwich);
                if (at < 0) c.recipes.Add(bread); else c.recipes.Insert(at, bread);
            }
            bread.always = true;
            bread.ingredients.Clear();
        }

        /// contentVersion 12: meal grades are Plain / Fine / Prime like ingredients — fix Camp Bread's text if unchanged.
        static void RenameGrades12(GameContent c)
        {
            var b = c.Recipe("bread");
            if (b != null && b.description == "Free — there's always a loaf by the fire. Plain, light, better than nothing. Always Common.")
                b.description = "Free — there's always a loaf by the fire. Simple, light, better than nothing. Always Plain.";
        }

        static void Dish(GameContent c, string id, string name, string emoji, Station st, IngredientCost[] cost, StatMod[] buffs, string[] tags, string desc)
        {
            Rec(c, id, name, emoji, st, cost, buffs, tags, desc);
        }

        static void Accel(GameContent c, string ingredientId, StatMod[] buffs, Hazard guard)
        {
            var ing = c.Ingredient(ingredientId);
            if (ing == null) return;
            ing.accelBuffs = new List<StatMod>(buffs);
            ing.accelGuard = guard;
        }
    }
}
