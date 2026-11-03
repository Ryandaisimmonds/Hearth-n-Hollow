# Trim the opening to six beats

Status: done
Type: task
Blocked by: 13, 15, 16

## Why
Teach "you ate, now you're stronger" in the first ten minutes.

## Acceptance criteria
- [x] Beats: dead fire + Wren → first short dive → die or escape, Wren feeds you, kitchen opens, first meal → second dive → first boss (Meadow floor 5), camp grows → boots/dash from the smithy.
- [x] Pit and pit cache removed; 75% start speed removed; dash lock tied to first boots.
- [x] Arc tests updated.

## Where (Assets/HearthHollow/Scripts)
Core/OpeningArc.cs, GameSessionArc.cs, Dungeon/PitCache.cs, DungeonSettings.arc, ArcTests.

## Comments

- (Claude, 3 Nov) Done. The arc has three stages (OpeningArc.Defaults, also written into the DungeonSettings asset): **Arrival** (dead fire, Wren, Route board; no dash) → **Fed**, reached when your first run ends, fallen *or* escaped (`ArcTrigger.FirstRunEnd`): Wren feeds you, kitchen + larder open → **Town**, reached when you're home after your first boss (`HomeAfterBoss`): smithy, workshop and the Lamp-maker plot open, +50 coins. Boots are bought at the smithy and the first pair unlocks the dash (ladders no longer give boots). Town spots for areas not open yet show a toast instead. Removed: the pit, the pit cache (`PitCache.cs` deleted), `RunState.pitTaken/pitRoom`, `OpeningArc.baseSpeed/pitRadius/pitCache`; `ArcSpeed` is 1. Content v35 un-doubles the boot speeds (0/4/7/10/14%) now the slow start is gone. Old saves past the old arc count as finished. `ArcTests` rewritten (106/106 pass).
