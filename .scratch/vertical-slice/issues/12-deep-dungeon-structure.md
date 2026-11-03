# Deep dungeons: many floors, boss every 5, shortcuts

Status: ready-for-agent
Type: task
Blocked by: 05

## Why
Replace fixed 3-floor routes with deep biomes.

## Acceptance criteria
- [ ] `RouteData` (or a new `DungeonData`) has a floor count; a boss on every 5th floor; the ladder choice on every other floor.
- [ ] Beating a boss unlocks a shortcut: future runs can start from that floor (saved per dungeon).
- [ ] A run lasts until you head home or fall; the death penalty (keep 25%) stays.
- [ ] `floorLight` extends to deeper floors; shadow creature chance by floor extends.
- [ ] Tests for floor/boss/shortcut rules.

## Where (Assets/HearthHollow/Scripts)
Core/DungeonRules.cs, Routes.cs, GameSessionDungeon.cs, DungeonController.cs, UI/LadderChoiceOverlay.cs, RouteScreen.cs.

## Comments
