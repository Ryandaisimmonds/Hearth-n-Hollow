# Split DungeonController: camera, lighting, boss intro

Status: done
Type: task

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`DungeonController` is 795 lines mixing floor building, room flow, camera, room lighting, the fall's fog and the boss entrance. **Do after the playtest.**

## Acceptance criteria
- [x] Camera (follow, shake, push-in), room lighting and the boss intro are their own components
- [x] `DungeonController` keeps floor building and room flow; under ~450 lines
- [x] No behaviour change
- [x] All tests pass; play-checked; no gameplay or balance change

## Where
Dungeon/DungeonController.cs.

## Comments

(Claude, 12 Nov) Done. DungeonController went from 803 to about 450 lines and keeps floor building and room flow. The rest is its own piece: DungeonCamera (follow, shake, push-in, putting the camera back after a run), RoomLighting (the room light, the light you carry and its meal tints, the flood from big moves), BossIntro (the title card, flare and slow-mo; IntroPush is now BossIntro.PushAt), FallGuide (the fall's gloom, fog and tutorial lines), LightSetup (the static Light2D helpers, used all over the dungeon and town), and DungeonController.Hud.cs (prompt, status, boss bar, map). These are plain classes driven from the controller's Update/LateUpdate, so the order things happen in is unchanged. The smoke test now also spawns the Bramble Sow and checks its entrance: the title card is up, the camera pushes in and comes back. That test passed on the old code before the split and on the new code after; the fight and boss-intro screenshots match. 182/182 EditMode and 1/1 PlayMode pass.
