# Split PlayerController: one handler per move

Status: ready-for-agent
Type: task

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`PlayerController` is 1,520 lines; about 880 of them are one `switch` over every `MoveKind`. Adding or tuning a move means editing the biggest file in the game. **Do after the vertical-slice playtest** (it touches combat feel).

## Acceptance criteria
- [ ] Each move kind is its own small class (or a table of handlers) behind one interface; `PlayerController` keeps input, movement, timers and getting hit
- [ ] No behaviour change: every Cook move play-checked; other classes' moves compile and run
- [ ] `PlayerController` under ~500 lines
- [ ] All tests pass; play-checked; no gameplay or balance change

## Where
Dungeon/PlayerController.cs, Dungeon/ClassFx.cs.

## Comments
