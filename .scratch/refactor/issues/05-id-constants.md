# One place for area, ingredient and flag ids

Status: ready-for-agent
Type: task
Blocked by: 03

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

Ids are string literals everywhere: `"chili"` appears 40 times, `"meadow"` 26, `"wolf"` 21, `"frosthollow"` 17. A typo compiles and fails silently.

## Acceptance criteria
- [ ] An `Ids` class (areas, ingredients, monsters, recipes used by rules, story flags) in Core
- [ ] Game code and tests use the constants; no bare literals for those ids outside the content definition
- [ ] A test checks every constant exists in the content
- [ ] All tests pass; play-checked; no gameplay or balance change

## Where
All of Scripts and Tests.

## Comments
