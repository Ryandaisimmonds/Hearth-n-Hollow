# Scene-level play-mode smoke test

Status: done
Type: task
Blocked by: 01

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

The autoplay bot (vertical-slice ticket 66) drives the rules, not the scenes. `SaveService.DirectoryOverride` already lets a test use its own save folder, so a real scene test is safe once the game code has its own assembly.

## Acceptance criteria
- [x] A PlayMode test loads the game scene with a temp save folder, plays the fall to its end, walks the town, opens each station, cooks a meal, sets out for Meadow Hollow and clears a room
- [x] Fails on any error in the console or a step that doesn't finish in time
- [x] Ryan's real save is never touched
- [x] All tests pass; play-checked; no gameplay or balance change

## Where
Tests/PlayMode (new), Runtime/SaveService.cs.

## Comments

(Claude, 12 Nov) Done. New Tests/PlayMode/SmokeTest.cs (assembly HearthHollow.PlayTests): loads the game scene with saves in a temp folder, plays the fall to its end (the fog), lets Wren have her say, walks up to and uses all 17 other town spots, cooks a meal, shows Wren, packs the tray, uses the gate, sets out for Meadow Hollow and clears the first fight room. Any console error fails it (the test framework does that), every step has a time limit, and teardown removes the game before switching saves back, then checks the real save's timestamp is unchanged. It skips itself if the slice is switched off in the dev panel. Two small hooks for it: GameBootstrap.UI and a public TownController.Use. Runs in about 15 s (Test Runner > PlayMode). 1/1 PlayMode and 177/177 EditMode pass; Ryan's save confirmed byte-identical afterwards.
