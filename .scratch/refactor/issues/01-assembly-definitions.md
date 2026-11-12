# Add assembly definitions

Status: done
Type: task

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

Everything compiles into one default assembly. Nothing stops Core picking up a Unity dependency by accident, every edit recompiles everything, and play-mode tests can't reference the game code.

## Acceptance criteria
- [x] `HearthHollow.Core` asmdef over `Scripts/Core` with no engine references
- [x] `HearthHollow.Game` asmdef over Dungeon, Town, UI, Runtime and Data (they reference each other, so one assembly for now)
- [x] `HearthHollow.Editor` asmdef (editor only) over `Scripts/Editor`
- [x] `HearthHollow.Tests` asmdef over `Tests/Editor`, referencing the above
- [x] Project compiles with no errors; all tests pass; the game plays
- [x] All tests pass; play-checked; no gameplay or balance change

## Where
Scripts/*, Tests/Editor. Third-party packs that the game code references may need their own asmdef references.

## Comments

(Claude, 12 Nov) Done. Four assemblies: `HearthHollow.Core` (Scripts/Core, **no engine references**, so a stray `using UnityEngine` in the rules is now a compile error), `HearthHollow.Game` (Dungeon, Town, UI, Runtime, Data; refs Core, URP runtime + 2D runtime, Input System), `HearthHollow.Editor` (editor-only), `HearthHollow.Tests` (editor-only, test runner + NUnit, `UNITY_INCLUDE_TESTS`). No code changes were needed: nothing in the game code references the third-party packs' scripts. 175/175 tests pass; play-checked (bootstrap, art and audio libraries, a Meadow floor) with no errors. Unblocks 17 and 18 (a play-mode test can now reference the game).
