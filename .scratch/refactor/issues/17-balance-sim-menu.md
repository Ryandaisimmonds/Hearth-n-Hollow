# Move the balance simulator to an Editor menu

Status: done
Type: task
Blocked by: 01

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`BalanceSimulator` is a tool living in the tests folder; you have to run a test to get the report.

## Acceptance criteria
- [x] `Hearth & Hollow > Write Balance Report` menu item writes `.scratch/vertical-slice/balance-report.md`
- [x] The test stays as a thin check (Prime dies no more often than Hungry)
- [x] All tests pass; play-checked; no gameplay or balance change

## Where
Tests/Editor/BalanceSimulator.cs, Scripts/Editor.

## Comments

(Claude, 12 Nov) Done. BalanceSim now lives in Scripts/Editor/BalanceSimulator.cs with a Hearth & Hollow > Write Balance Report menu item (writes .scratch/vertical-slice/balance-report.md and shows it in Finder). The test (Tests/Editor/BalanceSimTests.cs) is now only the check that Prime dies no more often than Hungry, so running tests no longer rewrites the report. Report regenerated: same numbers, new header line. 177/177 tests pass. Editor-only change, so nothing to see in play mode.
