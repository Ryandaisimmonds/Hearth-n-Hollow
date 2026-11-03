# Dungeon event log (Esc)

Status: needs-triage
Type: task
Blocked by: none

## Why
(Ryan, 3 Nov) A lot pops up in the dungeon (discoveries, loot, guards, combos) and it's easy to miss. A log means nothing gets lost.

## Acceptance criteria
- [ ] Every dungeon pop-up worth knowing (element discovered, loot / bag full, boss hint, guarded / infused hits, floor reached, meal effects) is also written to a run log.
- [ ] Pressing Escape in the dungeon opens the pause menu with a scrollable log of this run, newest first.
- [ ] The log is cleared at the start of each run; it does not go in the save.

## Where (Assets/HearthHollow/Scripts)
Core (a small RunLog on RunState or GameSession), Dungeon/EnemyController.cs + PlayerController.cs (pop-ups), UI pause / Esc handling.

## Comments
