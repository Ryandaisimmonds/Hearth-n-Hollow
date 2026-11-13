# Centralise the slice checks

Status: done
Type: task

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`Slice.On` is checked in 36 places. Each is a one-off rule about what's in the build.

## Acceptance criteria
- [x] One `Features` object answers named questions (e.g. `HasClassSelect`, `HasMutators`, `ProgressRoutes`)
- [x] `Slice.On` read in one place only
- [x] Slice on and off both behave as before (tests cover both)
- [x] All tests pass; play-checked; no gameplay or balance change

## Where
Core/Slice.cs and callers.

## Comments

(Claude, 12 Nov) Done. New Features class (in Core/Slice.cs) answers 14 named questions: AllRoutes, AllClasses, AllTrinkets, AllIngredients, ThirdTrinketSlot, Mutators, CampEvents, TownIsHome, ClassSelect, OpensWithFall, WrenGuides, FullTrayToSetOut, HearthGrowsWithAreas, SlimRouteBoard. It is the only reader of Slice.On (apart from the dev panel's own switch button). All 30-odd game checks now ask Features. New SliceTests cover both settings: every answer with the slice on and off, camp events rolling when off, and no fall or Wren gate when off. 182/182 EditMode and 1/1 PlayMode pass. Town and Route board screenshots match the ones from before. The smoke test's town shot now waits for the wake-up fade.
