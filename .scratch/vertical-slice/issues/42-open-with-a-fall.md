# Open the game with a fall into the dungeon

Status: done
Type: task
Blocked by: 41

## Why
(Ryan, 4 Nov) The meal choice shouldn't show when you first go to town. Instead: you fall into the dungeon and wake up inside it; each room's monsters are tougher until you fall. You then wake up in town for the first time, speak to Wren, and she tells you about food.

## What
- [x] A new game (slice) opens in Meadow Hollow, not town: no class picker, no Wren yet (`GameSession.BeginTheFall`, also after Reset in Settings)
- [x] Banner on landing: "The mossy bank gives way and you tumble down into the fog…"
- [x] Each room you fight in is tougher: room 1 ×0.8 HP / ×0.6 damage, +0.45 / +0.65 per room after (`tuning.firstDive*`, content v38)
- [x] Fall → wake up in town for the first time; Wren comes over, introduces herself (she found you) and tells you about food
- [x] Route board's meal tray hidden until Wren explains packing (after you show her your first meal); no tray requirement before that

## Comments
- (Claude, 4 Nov) Done. Tests: `TheFallTests` (3), `ArcTests.FirstDive_HasNoLadder_AndToughMonsters` updated for the ramp. 127/127 pass. Play-checked from a fresh save: lands in Meadow Hollow with the banner, fall → town → Wren's "I found you at the edge of the fog" talk.
