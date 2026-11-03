# Cooking v10: no ingredient grades, quality + tiers, element slot

Status: ready-for-agent
Type: task
Blocked by: 06

## Why
Design v2 cooking: recipe + quality + optional element ingredient.

## Acceptance criteria
- [ ] Ingredients lose grades (migration merges stacks). Drop odds by grade removed.
- [ ] Each cook rolls a quality (3 levels, small bonus, e.g. ×1 / ×1.1 / ×1.2); the base chance is tunable.
- [ ] Recipes can have a tier-2 version that needs a deep ingredient; tier, not quality, gives the big jump.
- [ ] The accelerator slot becomes the element slot: an ingredient either infuses (sets the run element) or guards (resists an element). Only one infusion per tray.
- [ ] Combos give light fun bonuses (e.g. warmer lantern, +1 forage) with little power.
- [ ] Content version +1 with upgrade step; tests for the quality roll, tiers and element slot.

## Where (Assets/HearthHollow/Scripts)
Core/Cooking.cs, Meals.cs, DefaultContentCooking.cs, DefaultContentKitchen.cs, UI/CookOverlay.cs, PantryOverlay.cs, HearthScreen.cs.

## Comments
