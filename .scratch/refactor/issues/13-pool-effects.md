# Pool DungeonFx and share materials

Status: ready-for-agent
Type: task

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`DungeonFx.Spawn` creates and destroys a GameObject per particle; the death bursts and hit effects (tickets 56, 61) add many more. Materials are loaded in five places, and `Telegraph` reloads its material on every call.

## Acceptance criteria
- [ ] `FxSprite` objects come from a pool and return to it
- [ ] One `FxMaterials` holder (glow, flat, rim, unlit), loaded once
- [ ] A boss death in a full room creates no new GameObjects after warm-up (checked in the profiler)
- [ ] Effects look the same
- [ ] All tests pass; play-checked; no gameplay or balance change

## Where
Dungeon/DungeonFx.cs, Dungeon/Telegraph.cs, Dungeon/EnemyGlow.cs, Dungeon/AreaAtmosphere.cs.

## Comments
