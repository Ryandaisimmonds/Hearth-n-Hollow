# Cut "lit = weak spot", Heat and the Darkness hazard

Status: ready-for-agent
Type: task

## Why
Light keeps its look and secrets, loses the overlapping combat rules.

## Acceptance criteria
- [ ] `litDamageBonus` removed (or 0 and unused).
- [ ] Heat meter (`AddHeat`, `HeatUpdate`, `HeatGlow`, Detonate's Heat cost) removed; Emberfist is out of the slice anyway.
- [ ] Darkness no longer a `Hazard` for routes; depth darkness (`floorLight`) stays and extends past floor 3 (see ticket 12).
- [ ] Light-shy / light-drawn kept as-is (flavour). Tests pass.

## Where (Assets/HearthHollow/Scripts)
DungeonSettings.cs, EnemyController.cs, PlayerController.cs, Core/Types.cs (Hazard), DefaultContent routes.

## Comments
