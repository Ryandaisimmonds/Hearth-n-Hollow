# Cook skill pool, loadout and 2 Ultimates via the Cook's arc

Status: done
Type: task
Blocked by: 04

## Why
Classes have a small skill pool + 2–3 Ultimates, unlocked by arc steps, no stat bonuses.

## Acceptance criteria
- [x] Pool (confirmed by Ryan, 3 Nov): attack Pan & Ladle; skills Skewer Toss, Flambé, Hot Pot, Cookfire; dash Butter Slide; Ultimates Grand Feast + one new.
- [x] Each skill/Ultimate has an unlock requirement (an arc step, e.g. "reach Meadow floor 3", "beat the floor-5 boss") and is then learned in town (training yard or kitchen).
- [x] Loadout: attack + 2 skills + dash + 1 Ultimate, chosen in town.
- [x] Most of the pool unlocks within the slice.

## Where (Assets/HearthHollow/Scripts)
Core/Progression.cs (rewrite), DefaultContentMoves.cs, DefaultContentUltimates.cs, UI/SkillsTab.cs.

## Comments

- (Claude, 3 Nov) Done. Content v29 (`CookPool29`) + `GameContent.moveUnlocks`. Start: Pan & Ladle, Skewer Toss, Roll. Arc: Butter Slide (Meadow floor 2, kitchen), Flambé (Meadow floor 3, kitchen), Grand Feast (Meadow floor 4, yard), Hot Pot (beat Meadow floor-5 boss, kitchen), Cookfire (Chili floor 3, kitchen), new Ultimate **Five-Alarm Chili** 🌶️ (beat Chili floor-5 boss, yard; a delayed Fire burst that stuns). Skewer / Frying Pan / Rolling Pin left the pool. Profile tracks `deepestFloors`, `bossesBeaten`, `learnedMoves` (saveVersion 11; old saves count cleared routes as their last floor). Reaching a floor / killing a boss in a run announces newly unlocked moves; they're learned with a Learn button in the Skills tab (camp only). Loadout gains an Ultimate slot (C / I); none until one is learned. The real dungeon still has 3 floors, so Grand Feast and the boss unlocks become reachable with ticket 12. Tests: `MovePoolTests` (74/74 pass).
