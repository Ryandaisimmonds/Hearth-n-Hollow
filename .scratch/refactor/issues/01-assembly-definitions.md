# Add assembly definitions

Status: ready-for-agent
Type: task

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

Everything compiles into one default assembly. Nothing stops Core picking up a Unity dependency by accident, every edit recompiles everything, and play-mode tests can't reference the game code.

## Acceptance criteria
- [ ] `HearthHollow.Core` asmdef over `Scripts/Core` with no engine references
- [ ] `HearthHollow.Game` asmdef over Dungeon, Town, UI, Runtime and Data (they reference each other, so one assembly for now)
- [ ] `HearthHollow.Editor` asmdef (editor only) over `Scripts/Editor`
- [ ] `HearthHollow.Tests` asmdef over `Tests/Editor`, referencing the above
- [ ] Project compiles with no errors; all tests pass; the game plays
- [ ] All tests pass; play-checked; no gameplay or balance change

## Where
Scripts/*, Tests/Editor. Third-party packs that the game code references may need their own asmdef references.

## Comments
