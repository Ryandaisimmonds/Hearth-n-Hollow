# Pool DungeonFx and share materials

Status: done
Type: task

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`DungeonFx.Spawn` creates and destroys a GameObject per particle; the death bursts and hit effects (tickets 56, 61) add many more. Materials are loaded in five places, and `Telegraph` reloads its material on every call.

## Acceptance criteria
- [x] `FxSprite` objects come from a pool and return to it
- [x] One `FxMaterials` holder (glow, flat, rim, unlit), loaded once
- [x] A boss death in a full room creates no new GameObjects after warm-up (checked in the profiler)
- [x] Effects look the same
- [x] All tests pass; play-checked; no gameplay or balance change

## Where
Dungeon/DungeonFx.cs, Dungeon/Telegraph.cs, Dungeon/EnemyGlow.cs, Dungeon/AreaAtmosphere.cs.

## Comments

(Claude, 12 Nov) Done. DungeonFx now reuses its one-shot sprites (Spawn, Ghost) and fading lights (Flash with a lifetime, wall glow included) from pools under an fx_pool object instead of making and destroying a GameObject each time. FxSprite and FxLight go back to the pool when they fade. Lights riding on a sprite go back with it. A pooled fire that cast shadows has its ShadowSafeLight removed on reuse. The floor-embers light cap counts through a Done callback instead of OnDestroy. Lights that stay (life 0: arrows, reticle, beams) are still made as before. New FxMaterials holds Glow, Flat, Rim and Unlit, each loaded once; Telegraph no longer reloads its material on every call, and EnemyGlow, AreaAtmosphere and all the other GlowMat users read FxMaterials. Instead of a profiler check, the smoke test now checks it directly: a boss dies in every element at once with sparks, rings, slashes and floor embers; after one warm-up round, the second round makes no new GameObjects (DungeonFx.Created stays at 1211). Effects render as before (smoke_8_bursts screenshot). 182/182 EditMode and 1/1 PlayMode pass.
