# Cook skill pool, loadout and 2 Ultimates via the Cook's arc

Status: needs-triage
Type: task
Blocked by: 04

## Why
Classes have a small skill pool + 2–3 Ultimates, unlocked by arc steps, no stat bonuses.

## Acceptance criteria
- [ ] Proposed pool (Ryan to confirm): attack Pan & Ladle; skills Skewer Toss, Flambé, Hot Pot, Cookfire; dash Butter Slide; Ultimates Grand Feast + one new.
- [ ] Each skill/Ultimate has an unlock requirement (an arc step, e.g. "reach Meadow floor 3", "beat the floor-5 boss") and is then learned in town (training yard or kitchen).
- [ ] Loadout: attack + 2 skills + dash + 1 Ultimate, chosen in town.
- [ ] Most of the pool unlocks within the slice.

## Where (Assets/HearthHollow/Scripts)
Core/Progression.cs (rewrite), DefaultContentMoves.cs, DefaultContentUltimates.cs, UI/SkillsTab.cs.

## Comments
