# Move the balance simulator to an Editor menu

Status: ready-for-agent
Type: task
Blocked by: 01

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`BalanceSimulator` is a tool living in the tests folder; you have to run a test to get the report.

## Acceptance criteria
- [ ] `Hearth & Hollow > Write Balance Report` menu item writes `.scratch/vertical-slice/balance-report.md`
- [ ] The test stays as a thin check (Prime dies no more often than Hungry)
- [ ] All tests pass; play-checked; no gameplay or balance change

## Where
Tests/Editor/BalanceSimulator.cs, Scripts/Editor.

## Comments
