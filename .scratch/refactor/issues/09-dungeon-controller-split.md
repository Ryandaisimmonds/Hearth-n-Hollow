# Split DungeonController: camera, lighting, boss intro

Status: ready-for-agent
Type: task

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`DungeonController` is 795 lines mixing floor building, room flow, camera, room lighting, the fall's fog and the boss entrance. **Do after the playtest.**

## Acceptance criteria
- [ ] Camera (follow, shake, push-in), room lighting and the boss intro are their own components
- [ ] `DungeonController` keeps floor building and room flow; under ~450 lines
- [ ] No behaviour change
- [ ] All tests pass; play-checked; no gameplay or balance change

## Where
Dungeon/DungeonController.cs.

## Comments
