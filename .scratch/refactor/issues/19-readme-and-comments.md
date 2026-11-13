# Write a README; trim ticket comments

Status: done
Type: task

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`README.md` is one line, and about 350 code comments reference ticket numbers that mean nothing without the tracker.

## Acceptance criteria
- [x] README: what the game is, how to open and run it, where things live (Core / Dungeon / Town / UI), how to run tests and the builders
- [x] Ticket-number comments are reworded to say why (or removed) whenever a file is touched by another refactor ticket; no separate sweep
- [x] All tests pass; play-checked; no gameplay or balance change

## Where
README.md.

## Comments

(Claude, 12 Nov) Done. README.md now covers what the game is, how to open and run it, where things live, the tests (EditMode and the PlayMode smoke test) and every Hearth & Hollow menu item. The ticket-number comments in Story.cs and GameSessionStory.cs (touched by refactor 16) now say why instead (29 removed). The rest follow the rule as tickets 06-13 touch their files. CLAUDE.md's code map is updated too (Story, Ids, Core/Content, content v46, UI/Styles, the smoke test, the balance menu). Comment-only code change; 177/177 EditMode pass.
