# Centralise the slice checks

Status: ready-for-agent
Type: task

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`Slice.On` is checked in 36 places. Each is a one-off rule about what's in the build.

## Acceptance criteria
- [ ] One `Features` object answers named questions (e.g. `HasClassSelect`, `HasMutators`, `ProgressRoutes`)
- [ ] `Slice.On` read in one place only
- [ ] Slice on and off both behave as before (tests cover both)
- [ ] All tests pass; play-checked; no gameplay or balance change

## Where
Core/Slice.cs and callers.

## Comments
