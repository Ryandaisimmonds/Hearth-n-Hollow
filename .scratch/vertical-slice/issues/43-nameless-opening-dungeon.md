# The opening fall: a nameless dungeon of nothing but fights

Status: done
Type: task
Blocked by: 42

## Why
(Ryan, 4 Nov) Make a new area with no name: you just wake up in a weird dungeon, each next room is only enemies, harder and harder each room. Enemies should hit even harder or move faster so they can hit you more easily.

## What
- [x] New hidden route `nowhere`, shown as "🌀 ???" (never on the Route board, no Rich/Lucky/Dangerous roll); content v39
- [x] Borrows the Hollow Heart's purple, mushroom-lit look
- [x] Layout: a start room, then a single line of 13 fight rooms, no forage/campfires/branches (`DungeonGenerator.Gauntlet`); the far end is an exit as a safety net
- [x] Each room: monsters hit harder (+85% per room), move faster (+12% per room, up to ×2), and there are more of them (+1 every 2 rooms)
- [x] Monsters: slime, bat, crab, wolf, shaman
- [x] New landing banner; Wren's first talk mentions the purple light ("no part of the Hollow I know")

## Comments
- (Claude, 4 Nov) Done. Tests: `TheFallTests` (5). 129/129 pass. Play-checked from a fresh save: wakes in "???", 14-room line; by room 5 it's 5 monsters at ×1.48 speed hitting for 8 each against 14 HP.
