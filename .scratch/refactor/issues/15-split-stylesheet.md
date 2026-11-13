# Split the stylesheet per screen

Status: done
Type: task

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`HearthHollow.uss` is one 1,800-line file.

## Acceptance criteria
- [x] One file per screen or area (base, town, kitchen, route board, dungeon HUD, summary, overlays), all added in GameBootstrap or imported from one root sheet
- [x] No visual change (screenshots of each screen before and after)
- [x] All tests pass; play-checked; no gameplay or balance change

## Where
UI/HearthHollow.uss, Runtime/GameBootstrap.cs.

## Comments

(Claude, 12 Nov) Done. HearthHollow.uss is now a root sheet of @imports (the same asset GameBootstrap already points at) over 17 sheets in UI/Styles/, in the old cascade order. The per-ticket patches at the end went to their screen's sheet; the element-colour rules went to a last sheet (element-tints) so they still win. Removed 92 dead rules from the old grid game and turn-based fight (tile, grid, hud__, fight__, monster-card, action--, float-text-- variants, header--hollow). One rule needed a stronger selector to keep its old win (.expedition__pick-chip.filter-chip). Before/after screenshots of town, cooking, cook pop-up, pantry, route board, settings, dungeon and summary match (only animation differs). 177/177 tests pass.
