# Move the story logic out of GameSession

Status: ready-for-agent
Type: task
Blocked by: 02

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`GameSession` is spread over 11 partial files with about 190 public members. The story (Old Wren's thread, the arc, the fall) is the most self-contained part.

## Acceptance criteria
- [ ] A `Story` class owns Wren's steps, the arc and the fall; `GameSession` forwards the few calls the UI needs
- [ ] WrenStoryTests, TheFallTests and SliceAutoplayTests pass unchanged (apart from call sites)
- [ ] All tests pass; play-checked; no gameplay or balance change

## Where
Core/WrenStory.cs, Core/TheFall.cs, Core/GameSessionArc.cs, Core/GameSessionStory.cs.

## Comments
