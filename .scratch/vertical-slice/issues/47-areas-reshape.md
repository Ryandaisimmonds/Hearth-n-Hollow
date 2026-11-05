# Areas, not dungeons: Meadow 3 floors, Chili 2, new Frost Hollow

Status: done
Type: task
Blocked by: 44

## Why
(Playtest, 5 Nov) Five floors before anything happens is too long. Dungeons should be areas you go through (a deep dark forest, a mine): some long, some short, some rich in resources, some lacking.

## Decisions (grilling, 5 Nov)
- Meadow Hollow: 3 floors, the Bramble Sow on floor 3. A balanced mix of monsters and resources.
- Chili Ridge: 2 floors, rich in resources, light on monsters, no boss. Its special one-floor first trip (ticket 35) goes.
- Frost Hollow (new, existing Frost Hollow look): 3 floors, colder and harder, the Hoarfrost Alpha on floor 3. Drops one or two new ingredients that unlock tier-2 versions of existing meals.
- The way home in short areas is the bottom: beat the boss, or reach the exit in a boss-free area. Portals only in long areas (5+ floors).
- Floor shortcuts only in long areas (none in the slice). In long areas, the deepest unlocked shortcut is the default start; you pick floor 1 to go back.
- Wren's thread: the fall → wake, food → cook and show Wren → beat the Sow (Meadow floor 3) → Chili Ridge for chilies → season a meal → Frost Hollow opens ("the cold up there can't abide fire") → beat the Alpha.

## Acceptance criteria
- [x] Floors, bosses and exits as above; content upgrade step
- [x] Frost Hollow route, its monsters, ingredients and tier-2 recipes
- [x] Portal and shortcut rules by area length
- [x] Wren's steps, objectives and lines rewritten; old saves mapped onto the new thread
- [x] Tests updated (Meadow, Chili, Wren thread)

## Comments
- (Claude, 5 Nov) Done (content v41, saveVersion 15).
  - Meadow Hollow: 3 floors, the Bramble Sow on 3, bats from floor 2, ginger from floor 2.
  - Chili Ridge: 2 floors, `noBoss`, `enemyMult` 0.6, gather 0.45; its last floor ends in an exit (`DungeonRules.EndsInExit`). The one-floor first trip is gone.
  - Frost Hollow: the old unused route repurposed. 3 floors, Hoarfrost Alpha on 3, Cold hazard, wolves/crabs/bats (shamans from 2), and the tier-2 ingredients (starmoss, wild honeycomb, golden wheat). Opens with flag `wren_frost` when you season a Fire meal.
  - Portals and shortcuts only in areas of 5+ floors (`LongArea`); the deepest shortcut is the default start.
  - Wren's lines, objectives and the fire hint rewritten for floor 3 and Frost Hollow; the Ridge step moves on after any Ridge trip; the Alpha step ends in Frost Hollow.
  - Skill unlocks moved off missing floors: Grand Feast = beat the Sow, Hot Pot = Frost Hollow floor 2, Picnic = Chili floor 2, Five-Alarm Chili = beat the Alpha. The locked-slot hint prefers the earlier area.
  - Old saves: "meadow:5" → "meadow:3", "meadow:10" → "frosthollow:3"; a save on the Alpha step gets the Frost Hollow flag.
  - Side fix: classes outside the slice no longer announce "joins your camp" (clearing the shorter Ridge triggered the Emberfist).
  - Tests: DeepDungeon, Meadow, Chili, MovePool, PlaytestTwo, WrenFireHint, WrenStory rewritten for the new areas. 147/147 pass. Play-checked: board, Chili floor 2 exit (no ladder/boss), Frost Hollow's ice cave.
  - Monster numbers not retuned yet; worth a playtest with the new meals first.
