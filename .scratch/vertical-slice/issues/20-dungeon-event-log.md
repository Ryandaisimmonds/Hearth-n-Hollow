# Dungeon event log (Esc)

Status: done
Type: task
Blocked by: none

## Why
(Ryan, 3 Nov) A lot pops up in the dungeon (discoveries, loot, guards, combos) and it's easy to miss. A log means nothing gets lost.

## Acceptance criteria
- [x] Every dungeon pop-up worth knowing (element discovered, loot / bag full, boss hint, guarded / infused hits, floor reached, meal effects) is also written to a run log.
- [x] Pressing Escape in the dungeon opens the pause menu with a scrollable log of this run, newest first.
- [x] The log is cleared at the start of each run; it does not go in the save.

## Where (Assets/HearthHollow/Scripts)
Core (a small RunLog on RunState or GameSession), Dungeon/EnemyController.cs + PlayerController.cs (pop-ups), UI pause / Esc handling.

## Comments
- (Claude, 3 Nov) Done. `Core/RunLog.cs`: `GameSession.Log(text)` + `RunLog` (per run, max 200 lines, consecutive repeats skipped, never saved). Auto-logged: Info toasts (floors, unlocks, story), loot, meals eaten, combos, bag full, the run's end; plus the run's infusion and guards at set-out. The Unity side logs element discoveries ("🔥 Fog Wolf is weak to Fire"), guarded hits and what your meals do to your light. Esc in the dungeon opens the menu with "📜 This run so far": a scrolling log, newest first, grouped by floor. Tests: `RunLogTests`.
