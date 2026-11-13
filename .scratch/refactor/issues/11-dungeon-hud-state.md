# Make DungeonHud one state object

Status: done
Type: task

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`DungeonHud` is 32 public static fields used to pass data from the dungeon to the HUD. Statics survive between runs and are easy to forget to reset.

## Acceptance criteria
- [x] One `DungeonHudState` instance owned by the dungeon and handed to the screen; `Reset()` is just a new instance
- [x] No static mutable fields left in `DungeonHud`
- [x] HUD looks and behaves the same
- [x] All tests pass; play-checked; no gameplay or balance change

## Where
Dungeon/DungeonHud.cs, UI/DungeonScreen.cs, and their callers.

## Comments

(Claude, 12 Nov) Done. New DungeonHudState (instance fields, the map and the loot tags). DungeonController owns one (Hud), and each new floor gets a fresh one instead of a Reset(). Floating numbers and the hurt flash are events on DungeonController that DungeonScreen subscribes to while it's shown. DungeonHud is now only the stateless Number() helper, so no static mutable fields are left. The screen redraws the map when the state object changes too. The smoke test now also saves screenshots of the key screens to Temp/ClaudeShots. Before/after shots of a fight and a cleared room match: same HUD, map, ability bar, status line and pickup toasts. 177/177 EditMode and 1/1 PlayMode pass.
