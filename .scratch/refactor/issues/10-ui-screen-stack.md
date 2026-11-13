# Replace GameUI's open-flags with a screen stack

Status: done
Type: task
Blocked by: 02

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`GameUI` tracks 12 separate "is open" booleans, and `ApplyLayout` is 30 lines of combinations of them. Every new overlay adds another flag to every line. **Do after the playtest.**

## Acceptance criteria
- [x] One stack (or enum) of open overlays replaces the booleans; Esc pops the top
- [x] `ApplyLayout` derives visibility from phase + stack
- [x] Every overlay opens and closes as before (town, kitchen, larder, spice rack, route board, dialogue, fishing, ladder choice, settings)
- [x] All tests pass; play-checked; no gameplay or balance change

## Where
UI/GameUI.cs, UI/UIScreen.cs.

## Comments

(Claude, 12 Nov) Done. GameUI keeps one list of what's open: places (Town, a Station over it, the Route board, class select) and pop-ups (camp event, larder, stove, spice rack, fishing, dialogue), plus the ladder and settings. ApplyLayout works out visibility from the phase and that list. Pop-ups no longer have a fixed pecking order: the newest one shows, closing it uncovers the one beneath, and Esc goes to whichever is on top. A run starting clears the home-only layers in one go. The public Open/Close API is unchanged. The smoke test now opens and closes every overlay one by one (larder, stove, spice rack, fishing, dialogue, notice board, settings, kitchen, route board, and the ladder in the dungeon), stacks two pop-ups, and drives Esc. That test passed against the old GameUI first, then against the new one. Kitchen, larder and spice rack screenshots match. 182/182 EditMode and 1/1 PlayMode pass.
