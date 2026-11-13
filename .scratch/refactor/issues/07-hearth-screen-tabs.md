# Split HearthScreen: one class per tab

Status: done
Type: task

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`HearthScreen` is 1,092 lines covering Cooking, Upgrades, Workshop and Skills. **Do after the playtest.**

## Acceptance criteria
- [x] One class per tab; `HearthScreen` only hosts and switches them
- [x] No visual or behaviour change (screenshots before and after)
- [x] All tests pass; play-checked; no gameplay or balance change

## Where
UI/HearthScreen.cs.

## Comments

(Claude, 12 Nov) Done. HearthScreen went from 1,092 to about 200 lines: the header, the tab bar, switching tabs, station mode and keys. Each tab is its own class on a small CampPage base (shared: the station list, SetIgnore, gear prefixes, emoji and pictures): CookingTab (kitchen bar, meal pantry, filters, recipe grid), GearTab (the Upgrades tab: gear, key items, trinkets), KitchenTab (the Workshop tab: station levels and shelves), plus the existing SkillsTab. The right-hand column is CampColumn. The code moved as it was. The smoke test now also takes shots of the smithy, workshop and training yard spots and the old camp screen (Cooking and Upgrades tabs, with the column). Taken before and after, the camp screens are pixel-identical and the town shots differ only where things move. 182/182 EditMode and 1/1 PlayMode pass.
