# Cooking v10: no ingredient grades, quality + tiers, element slot

Status: done
Type: task
Blocked by: 06

## Why
Design v2 cooking: recipe + quality + optional element ingredient.

## Acceptance criteria
- [x] Ingredients lose grades (migration merges stacks). Drop odds by grade removed.
- [x] Each cook rolls a quality (3 levels, small bonus, e.g. ×1 / ×1.1 / ×1.2); the base chance is tunable.
- [x] Recipes can have a tier-2 version that needs a deep ingredient; tier, not quality, gives the big jump.
- [x] The accelerator slot becomes the element slot: an ingredient either infuses (sets the run element) or guards (resists an element). Only one infusion per tray.
- [x] Combos give light fun bonuses (e.g. warmer lantern, +1 forage) with little power.
- [x] Content version +1 with upgrade step; tests for the quality roll, tiers and element slot.

## Where (Assets/HearthHollow/Scripts)
Core/Cooking.cs, Meals.cs, DefaultContentCooking.cs, DefaultContentKitchen.cs, UI/CookOverlay.cs, PantryOverlay.cs, HearthScreen.cs.

## Comments
- 3 Nov 2026 (Claude): Done.
  - Ingredient grades: gone. The pantry and bag ignore grades; saveVersion 10 merges each ingredient's
    stacks (`Cooking.MergeIngredientGrades`). Drop-grade rolls and the ladder screen's Fine/Prime odds removed
    (the ladder now says deeper floors hold rarer ingredients and counts rare ones in your haul). Fishing: a
    better catch gives more fish instead of better fish.
  - Quality: Plain / Fine / Prime rolled per cook from `qualityFineChance` 20% / `qualityPrimeChance` 5%, plus
    the station level's bonus and `CookChoice.qualityNudge` (ticket 11's minigame, `qualityNudgeFine/Prime`).
    Multipliers ×1.1 / ×1.2 (were ×1.25 / ×1.5).
  - Tiers: `RecipeData.tier` / `upgradeOf`. Three tier-2 dishes (×1.75 stats) each need a deep ingredient:
    Hearty Herb Stew (Starmoss), Honey-Glazed Skewer (Wild Honeycomb), Golden Herb Butty (Golden Wheat).
    The deep ingredients exist (`IngredientData.deep`); ticket 13 makes them drop.
  - Element slot: `IngredientData.infuse` / `guard`. Infuse: chili and fire honey (Fire), frostberry (Frost),
    nettle (Nature), salt and fish (Water), glowcap (Light). Guard: mint (Fire), ginger (Frost), garlic and herbs
    (Nature), wheat (Water). The old accelerator stat boosts are gone. One infused meal per tray (packing a
    second is refused). Starting a run sets `RunState.element` (infusion) and `RunState.guards`.
    Monster attacks (melee and shots) carry the monster's element; a guarded one does ×0.5 (`guardMult`) and
    pops "❄️ Guarded". Hazard guards (`accelGuard`) are kept for the old hazard routes.
  - Combos: no ×1.5 or extra-helping maths; each gives one small utility bonus (coins, gather zone, speed,
    HP per kill).
  - XP: every XP bonus (recipes, combos, trinkets) became a coin bonus; the cooking filter's "XP" is now "Speed".
  - Screens: the stove pop-up rewritten (needs vs have, element slot with infuse/guard text, quality odds, what
    each quality gives, the tier-2 hint); the Larder shows counts and each ingredient's slot role; tray shows
    "Infused" / "Guard" badges; dungeon HUD shows them; tier-2 dishes marked ★2.
  - Content v27 (`Cooking27`), asset upgraded.
  - Tests: `CookingTests` (9) + the bag test updated. 62/62 pass. In play on Ryan's save: stacks merged
    (saveVersion 10), stove screenshot checked, cooked a chili stew and a ginger butty, packed both, started a run:
    infusion Fire, guard Frost, pan swing became Fire, a 10-damage Frost hit did 5, HUD showed both. No console
    errors. Save backed up and restored.
  - Noticed for ticket 09: the Cook's Herb Stew gives +33.75 HP (×1.5 favourite × ×1.25 Cook bonus): class
    multipliers replace that.
