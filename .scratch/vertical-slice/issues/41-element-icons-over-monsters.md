# Element icons over monsters instead of coloured diamonds

Status: done
Type: task
Blocked by: 40

## Why
(Ryan, 4 Nov) Use a fire icon instead of the red diamond over monsters, and the same for the other elements.

## What
- [x] 12×12 pixel-art icons drawn in code: 🔥 flame, ❄️ snowflake, 🌿 leaf, 💧 droplet, ✨ sparkle (`DungeonFx.ElementIcon`)
- [x] Weakness: big and bright
- [x] Resistance: faded, with a dark slash through it
- [x] No effect: small and greyed

## Comments
- (Claude, 4 Nov) Done. Replaces the diamond/square markers in `EnemyController.BuildElementIcons`. 124/124 tests pass; play-checked on Meadow Hollow with a boar (frost weak, nature resist, the rest no effect) and a bat.
