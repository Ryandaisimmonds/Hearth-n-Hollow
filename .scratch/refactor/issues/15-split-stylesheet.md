# Split the stylesheet per screen

Status: ready-for-agent
Type: task

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`HearthHollow.uss` is one 1,800-line file.

## Acceptance criteria
- [ ] One file per screen or area (base, town, kitchen, route board, dungeon HUD, summary, overlays), all added in GameBootstrap or imported from one root sheet
- [ ] No visual change (screenshots of each screen before and after)
- [ ] All tests pass; play-checked; no gameplay or balance change

## Where
UI/HearthHollow.uss, Runtime/GameBootstrap.cs.

## Comments
