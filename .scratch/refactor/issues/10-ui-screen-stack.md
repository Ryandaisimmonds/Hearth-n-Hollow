# Replace GameUI's open-flags with a screen stack

Status: ready-for-agent
Type: task
Blocked by: 02

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`GameUI` tracks 12 separate "is open" booleans, and `ApplyLayout` is 30 lines of combinations of them. Every new overlay adds another flag to every line. **Do after the playtest.**

## Acceptance criteria
- [ ] One stack (or enum) of open overlays replaces the booleans; Esc pops the top
- [ ] `ApplyLayout` derives visibility from phase + stack
- [ ] Every overlay opens and closes as before (town, kitchen, larder, spice rack, route board, dialogue, fishing, ladder choice, settings)
- [ ] All tests pass; play-checked; no gameplay or balance change

## Where
UI/GameUI.cs, UI/UIScreen.cs.

## Comments
