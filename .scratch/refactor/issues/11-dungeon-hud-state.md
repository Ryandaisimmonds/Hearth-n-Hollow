# Make DungeonHud one state object

Status: ready-for-agent
Type: task

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`DungeonHud` is 32 public static fields used to pass data from the dungeon to the HUD. Statics survive between runs and are easy to forget to reset.

## Acceptance criteria
- [ ] One `DungeonHudState` instance owned by the dungeon and handed to the screen; `Reset()` is just a new instance
- [ ] No static mutable fields left in `DungeonHud`
- [ ] HUD looks and behaves the same
- [ ] All tests pass; play-checked; no gameplay or balance change

## Where
Dungeon/DungeonHud.cs, UI/DungeonScreen.cs, and their callers.

## Comments
