# Going home is rare: portals and bosses

Status: done
Type: task
Blocked by: none

## Why
(Ryan, 3 Nov, playtest) Going home must be hard: only rare portals or a boss. Answers: every boss opens an exit; about 1 in 5 floors has a portal (never floor 1).

## Acceptance criteria
- [x] No exit on ordinary floors; a way-home portal on ~20% of floors (seeded per floor, not floor 1).
- [x] Every boss room has an exit (and a ladder unless it's the bottom).
- [x] Ladder choice / prompts / messages say where the next way home could be.

## Comments
- (Claude, 3 Nov) Done. `DungeonRules.HomeChoiceOn(c, route, floor, seed)`: every boss floor, plus a seeded portal on ~20% of floors (`tuning.portalChance`, never floor 1). Ordinary floors have no exit and the ladder just goes down. Floor messages say when a portal is on the floor; the exit reads "✨ Step through the portal"; the ladder screen says the only ways home below are portals and the boss.
