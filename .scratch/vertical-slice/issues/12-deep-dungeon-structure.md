# Deep dungeons: many floors, boss every 5, shortcuts

Status: done
Type: task
Blocked by: 05

## Why
Replace fixed 3-floor routes with deep biomes.

## Acceptance criteria
- [x] `RouteData` (or a new `DungeonData`) has a floor count; a boss on every 5th floor; the ladder choice on every other floor.
- [x] Beating a boss unlocks a shortcut: future runs can start from that floor (saved per dungeon).
- [x] A run lasts until you head home or fall; the death penalty (keep 25%) stays.
- [x] `floorLight` extends to deeper floors; shadow creature chance by floor extends.
- [x] Tests for floor/boss/shortcut rules.

## Where (Assets/HearthHollow/Scripts)
Core/DungeonRules.cs, Routes.cs, GameSessionDungeon.cs, DungeonController.cs, UI/LadderChoiceOverlay.cs, RouteScreen.cs.

## Comments

- (Claude, 3 Nov) Done. Content v30: `RouteData.floors`, `bossEvery`, `bossIds` (boss 1, boss 2… for ticket 13). Meadow 10 floors / boss every 5; Chili Ridge 5. Rules in `DungeonRules`: `Floors`, `IsBossFloor` (every 5th + always the last), `HomeChoiceOn` (odd floors + boss floors: an exit room and the deeper-or-home ladder; on even floors the ladder just goes down), `StartFloors` (1 + the floor after each beaten boss), capped growth (`ExtraRooms` ≤ 3, `ExtraEnemies` ≤ 4). A non-final boss room gets both an exit and a ladder once the boss is down. Shortcut chips ("Start from Floor 1 / ⏬ Floor 6 (shortcut)") on the expedition panel; `StartRun(routeId, startFloor)`. Death keeps 25% as before. DungeonSettings asset: `floorLight` over 10 floors (1 → 0.3), `shadowChance` 0 until floor 6 then 12–30%. Tests: `DeepDungeonTests` (84/84 pass).
