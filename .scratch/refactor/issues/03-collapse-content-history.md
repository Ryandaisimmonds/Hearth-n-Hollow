# Collapse the content history into the current design

Status: ready-for-agent
Type: task
Blocked by: 02

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`DefaultContent.Create()` builds the original design and then patches it through 44 numbered steps (one step "replaces the recipes above"). There are no players' saves to protect yet, so the history is only cost.

## Acceptance criteria
- [ ] `Create()` builds the current design directly, in readable sections (classes, moves, ingredients, monsters, areas, recipes, gear, kitchen, tuning)
- [ ] The result is identical to today's content: a test compares old `Create()` output with the new one field by field before the old code is deleted
- [ ] Content version resets to 1; the upgrade mechanism stays for future steps
- [ ] `GameContent.asset` regenerated; Ryan's save still loads
- [ ] Content that the slice and Design v2 cut for good is dropped, not carried
- [ ] All tests pass; play-checked; no gameplay or balance change

## Where
Core/DefaultContent*.cs, Content/GameContent.asset. **Ask Ryan first** which non-slice content (other classes, story chapters 2+) to keep.

## Comments
