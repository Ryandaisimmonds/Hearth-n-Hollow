# Write a README; trim ticket comments

Status: ready-for-agent
Type: task

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`README.md` is one line, and about 350 code comments reference ticket numbers that mean nothing without the tracker.

## Acceptance criteria
- [ ] README: what the game is, how to open and run it, where things live (Core / Dungeon / Town / UI), how to run tests and the builders
- [ ] Ticket-number comments are reworded to say why (or removed) whenever a file is touched by another refactor ticket; no separate sweep
- [ ] All tests pass; play-checked; no gameplay or balance change

## Where
README.md.

## Comments
