# One place for area, ingredient and flag ids

Status: done
Type: task
Blocked by: 03

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

Ids are string literals everywhere: `"chili"` appears 40 times, `"meadow"` 26, `"wolf"` 21, `"frosthollow"` 17. A typo compiles and fails silently.

## Acceptance criteria
- [x] An `Ids` class (areas, ingredients, monsters, recipes used by rules, story flags) in Core
- [x] Game code and tests use the constants; no bare literals for those ids outside the content definition
- [x] A test checks every constant exists in the content
- [x] All tests pass; play-checked; no gameplay or balance change

## Where
All of Scripts and Tests.

## Comments

(Claude, 12 Nov) Done. New `Core/Ids` with nested `Area` (all 19), `Monster` (10), `Ingredient` (25), `Recipe` (24) and `Flag` (2). The existing constants (`GameSession.MeadowRoute / RidgeRoute / FrostRoute / FallRoute / WrenFrostFlag`, `DefaultContent.WrenFireHint`) now point at them. Bare literals were replaced in the game code (Slice, story, the dungeon's chili drop, trinket-slot route, area air, music, the fall's theme, the tutorial room) and across 32 test files. Ids that are both an area and an ingredient (`chili`, `salt`, `glowcap`) were resolved by context (pantry/accelerator/season calls = ingredient) and spot-checked by hand. New IdsTests: every constant exists in the content, and every area, monster, ingredient and dish has a constant, so adding content without an id fails a test. Deliberately left as strings: the editor builders' art/asset tables (keyed by id, the same kind of data as the content), audio track ids, town spot ids, and the content files themselves. 177/177 tests pass; play-checked Chili Ridge (area air and music resolve through the ids).
