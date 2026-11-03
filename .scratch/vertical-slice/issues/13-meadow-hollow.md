# Meadow Hollow content (10 floors)

Status: done
Type: task
Blocked by: 06, 08, 12

## Why
The slice's main dungeon.

## Acceptance criteria
- [x] 10 floors, Meadow theme; Nature-leaning monsters; boss at floor 5 (Nature) and floor 10 (Frost, weak to Fire, resists Frost).
- [x] Shadow creatures from floor 6; darker with depth.
- [x] Drops: Meadow ingredients, ginger (Frost guard) on deeper floors, one deep ingredient per station for the tier-2 dishes, and a Meadow building material.
- [x] Uses existing boss sprites where possible (placeholders fine).

## Where (Assets/HearthHollow/Scripts)
DefaultContent (routes, monsters, ingredients), DungeonSettings enemy profiles, Art Library boss map.

## Comments

- (Ryan, 3 Nov, playtest after ticket 12) "Working really well": low-health moments recovered slowly, close calls, reached floor 8.
- (Claude, 3 Nov) Done. Content v31 (`MeadowHollow31`): the route is now **Meadow Hollow** (10 floors). Monsters: slime + boar (Nature) from floor 1, Lantern Bee (Fire) from 3, Fog Wolf (Frost) from 6 (`RouteData.floorMonsters`). Bosses (`bossIds`): **Bramble Sow** (floor 5, Nature, boar art ×1.5, HP ×1.3) and **Hoarfrost Alpha** (floor 10, Frost, weak to Fire, resists Frost, wolf art ×1.6, HP ×1.5, bites twice). New `MonsterData.artId` lets named bosses use a base monster's sprites. Drops (`RouteData.floorDrops`, weighted): herbs, wheat, mushroom, berries always; ginger from floor 4; Starmoss / Wild Honeycomb / Golden Wheat from floor 6; timber as the material. Shadow creatures from floor 6 and darkening came with ticket 12. Tests: `MeadowHollowTests` (89/89 pass).
