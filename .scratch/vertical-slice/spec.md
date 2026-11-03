# Spec: Vertical slice (Design v2)

Source of truth: Hearth & Hollow — Design v2 (https://claude.ai/artifact/M2gLZFSAhMSaxzD6jh8mTJ),
section "Vertical slice". Decision log: `.scratch/design-reset/decisions.md`.

## Goal
Prove the whole Design v2 loop in finished form, small: cook → dive → ladder choice → home → cook better,
plus the trek to a second dungeon for the food that beats a boss.

## Success test
A new player, in about 60–90 minutes:
1. plays the opening (dead fire → first dive → fed by Wren → kitchen opens → second dive),
2. beats the Meadow Hollow floor-5 boss,
3. hears from Old Wren that the floor-10 boss is weak to Fire,
4. treks to Chili Ridge for chilies,
5. cooks Fire-infused food, beats the floor-10 boss,
6. and wants one more run.

## In scope
- **Main dungeon:** Meadow Hollow, 10 floors; bosses at floor 5 (Nature) and floor 10 (Frost, weak to Fire);
  beating floor 5 unlocks a shortcut to start there.
- **Second dungeon:** Chili Ridge, ~5 floors, boss at floor 5; the only source of chilies (Fire infusion).
  Opens after the Meadow floor-5 boss, when Wren gives the Fire hint.
- **Elements:** Fire, Frost, Nature (the full chart's code supports Water and Light too). Weakness ×1.5,
  resistance ×0.5. Hidden until discovered (hit with that element, or told); then shown on the enemy.
- **Class:** the Cook only. Minimal base stats, meal multipliers, a small skill pool + 2 Ultimates, loadout,
  skills unlocked by the first steps of the Cook's arc.
- **Cooking:** 3 stations, a few dishes each, one tier-2 dish per station (needs a deep Meadow ingredient);
  quality roll (3 qualities, small bonus) + optional timing minigame; element slot (chili infuses Fire,
  ginger guards Frost); combos as light fun bonuses; Camp Bread; meal pantry + tray of 3. No ingredient grades.
- **Gear:** shared across classes: bag, boots (dash), lantern; 3–4 fun trinkets.
- **Light:** darkness by depth, meal-shaped lantern, Flare (one light button), braziers, shadow creatures
  from floor 6, secrets.
- **Town:** the Hearth, kitchen, larder, smithy, Route board, Old Wren, plus one area the player chooses
  to upgrade with biome materials. The town is the hub.
- **Opening:** the six trimmed beats (no pit, normal speed).

## Out of scope (hide, don't delete, unless a ticket says remove)
Other classes, other biomes and the 18 old routes, class swapping, camp events, mutators, the old camp screen
in the slice flow, fishing changes, full bestiary UI.

## Removals (Design v2 cuts)
Break & Boost (hit types, shield pips, Boost), Heat, the thrown torch, "lit = weak spot", Darkness as a
hazard, levels/XP/skill trees, per-class gear tiers, ingredient grades, the pit beat and 75% start speed.

## Ground rules for every ticket
- Follow CLAUDE.md working rules (play mode, save backup, tests, console).
- Content changes: bump `LatestContentVersion` (now 24) with an upgrade step. Save changes: bump
  `saveVersion` with a migration. Ryan's existing save may be reset for the slice if a ticket says so.
- Keep the game playable after each ticket; run the EditMode tests; add tests for new pure-C# rules in Core.

## Tickets
See `issues/`. Order = build order; `Blocked by:` lines give the hard dependencies.
