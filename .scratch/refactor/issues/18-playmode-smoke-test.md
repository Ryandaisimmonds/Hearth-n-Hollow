# Scene-level play-mode smoke test

Status: ready-for-agent
Type: task
Blocked by: 01

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

The autoplay bot (vertical-slice ticket 66) drives the rules, not the scenes. `SaveService.DirectoryOverride` already lets a test use its own save folder, so a real scene test is safe once the game code has its own assembly.

## Acceptance criteria
- [ ] A PlayMode test loads the game scene with a temp save folder, plays the fall to its end, walks the town, opens each station, cooks a meal, sets out for Meadow Hollow and clears a room
- [ ] Fails on any error in the console or a step that doesn't finish in time
- [ ] Ryan's real save is never touched
- [ ] All tests pass; play-checked; no gameplay or balance change

## Where
Tests/PlayMode (new), Runtime/SaveService.cs.

## Comments
