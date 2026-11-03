# Chili Ridge content (5 floors)

Status: done
Type: task
Blocked by: 12

## Why
The slice's second dungeon and the only chili source.

## Acceptance criteria
- [x] About 5 floors, Chili theme; boss at floor 5; locked until ticket 15 unlocks it.
- [x] Chilies (Fire infusion) drop here and nowhere else; plus its own building material.

## Where (Assets/HearthHollow/Scripts)
DefaultContent, DungeonTheme chili, Routes.

## Comments

- (Claude, 3 Nov) Done. Content v32 (`ChiliRidge32`): **Chili Ridge**, a 5-floor side dungeon (existing chili theme with lava cracks), base difficulty 3. Monsters: Lantern Bee + boar, Bog Shaman from floor 3. Boss: **Ridge Drake** (floor 5, Fire, drake art ×1.3). Drops: chili, garlic, wheat; mint (Fire guard) from floor 2; ember coal as the material. Hidden until the story flag `wren_fire_hint` (new `Profile.storyFlags`, `RouteData.requiredFlag` / `lockHint`, `Routes.SetFlag`); until then the board shows "??? · side path — Old Wren might know the way". Ticket 15 sets the flag. Chilies drop only here among the slice routes (two hidden old routes still list them; they're out of the slice). Tests: `ChiliRidgeTests` (93/93 pass).
