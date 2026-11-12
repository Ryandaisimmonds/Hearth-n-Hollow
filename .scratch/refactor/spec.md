# Refactor and restructure

Source: the codebase review of 12 Nov 2026 (Claude Doc: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG).

## Goal
Make the code easier to change without changing how the game plays. Every ticket ends with all tests passing, a play-check, and no gameplay or balance change.

## Order
- **Now (mechanical, low risk):** 01 assemblies, 02 old grid game, 03 content history, then 04 and 05.
- **After the vertical-slice playtest (touches gameplay code):** 06 to 13.
- **Any time:** 14 to 19. Ticket 14 needs Ryan's decisions first.

## Tickets
- 01 Add assembly definitions
- 02 Delete the old grid game
- 03 Collapse the content history into the current design (blocked by 02)
- 04 Move monster gameplay data into the content (blocked by 03)
- 05 One place for area, ingredient and flag ids (blocked by 03)
- 06 Split PlayerController: one handler per move
- 07 Split HearthScreen: one class per tab
- 08 Split EnemyController: one behaviour per archetype (blocked by 04)
- 09 Split DungeonController: camera, lighting, boss intro
- 10 Replace GameUI's open-flags with a screen stack (blocked by 02)
- 11 Make DungeonHud one state object
- 12 Centralise the slice checks
- 13 Pool DungeonFx and share materials
- 14 Tidy the third-party packs — needs Ryan
- 15 Split the stylesheet per screen
- 16 Move the story logic out of GameSession (blocked by 02)
- 17 Move the balance simulator to an Editor menu (blocked by 01)
- 18 Scene-level play-mode smoke test (blocked by 01)
- 19 Write a README; trim ticket comments
