# Boss entrances

Status: done
Type: task
Blocked by: 56

## Why
(Ryan, 12 Nov: "run with all the above" while he tests the slice.) A short title card with the boss's name, a camera push-in and lights flaring when you walk into a boss room.

## Acceptance criteria
- [x] Title card
- [x] Camera push-in
- [x] Light flare
- [x] Tests where it makes sense; play-checked; no balance changes beyond what's noted

## Comments

(Claude, 12 Nov) Done. When a boss spawns, RoomInstance calls `DungeonController.BossEntrance`. A title card ("— GUARDIAN —", the boss's name, "Guardian of <area>") slides in over the lower third and fades out over 2.8 s. The camera leans towards the boss and pushes in 22%, from whatever size it was, then eases back out (`IntroPush`). The room flares in the boss's element colour with a ring, the braziers ignite outward from the boss, the screen shakes, and there's a 1.2 s slow-mo beat. Test: BossEntranceTests (160/160). Play-checked on the Bramble Sow; the camera returns to its normal size afterwards.
