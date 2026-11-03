# Trim the opening to six beats

Status: ready-for-agent
Type: task
Blocked by: 13, 15, 16

## Why
Teach "you ate, now you're stronger" in the first ten minutes.

## Acceptance criteria
- [ ] Beats: dead fire + Wren → first short dive → die or escape, Wren feeds you, kitchen opens, first meal → second dive → first boss (Meadow floor 5), camp grows → boots/dash from the smithy.
- [ ] Pit and pit cache removed; 75% start speed removed; dash lock tied to first boots.
- [ ] Arc tests updated.

## Where (Assets/HearthHollow/Scripts)
Core/OpeningArc.cs, GameSessionArc.cs, Dungeon/PitCache.cs, DungeonSettings.arc, ArcTests.

## Comments
