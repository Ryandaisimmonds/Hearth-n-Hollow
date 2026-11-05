# Projectiles over water; buried chests instead of bush forage

Status: done
Type: task
Blocked by: 47

## Why
(Playtest, 5 Nov) Fire should fly over water. Hidden forage should be something other than a bush: uncover a chest in the ground, like the cracked walls.

## Decisions (grilling, 5 Nov)
- Every projectile flies over water (walls still block everything).
- Hidden forage becomes a buried chest: a faint patch of disturbed earth you dig up with E. Flare (F) makes nearby patches sparkle.
- Inside: mostly the area's ingredients, with about a 25% chance of coins or a material.

## Acceptance criteria
- [x] Projectiles pass over water tiles
- [x] Dig spots replace bush forage, with Flare sparkle
- [x] Loot as above; tests

## Comments
- (Claude, 5 Nov) Done.
  - Projectiles: water, lava and pits share the walls tilemap with real walls. A projectile entering it checks the tile (`RoomInstance.SolidAt` / `IsHazardTile`); over hazard tiles it flies on, and inside the same collider it still stops at the first real wall (checked every frame). Boomerangs bounce as before. Play-checked in Meadow's ring-of-water room: an arrow crossed the water onto dry floor and stopped at the far wall.
  - Buried chests replace the hidden bush (`HiddenForage`): a faint patch of turned earth (drawn in code, `DungeonFx.DirtPatch`), fully visible and sparkling when Flare or strong light is on it. Stand on it: "⛏️ Dig here"; E digs it up ("📦 Buried chest!").
  - Inside: two of the area's ingredients, plus a 25% chance of a few coins or a building material (`tuning.buriedBonusChance`).
  - Tests: `BuriedChestTests`. 155/155 pass.
