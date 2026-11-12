# Split HearthScreen: one class per tab

Status: ready-for-agent
Type: task

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`HearthScreen` is 1,092 lines covering Cooking, Upgrades, Workshop and Skills. **Do after the playtest.**

## Acceptance criteria
- [ ] One class per tab; `HearthScreen` only hosts and switches them
- [ ] No visual or behaviour change (screenshots before and after)
- [ ] All tests pass; play-checked; no gameplay or balance change

## Where
UI/HearthScreen.cs.

## Comments
