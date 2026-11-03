# Remove Break & Boost

Status: ready-for-agent
Type: task

## Why
Cut hit types, shield pips, weakness diamonds and Boost. Elements (ticket 06) replace them.

## Acceptance criteria
- [ ] `HitType`, `BoostShape`, `MoveData.hitType/hitType2/boostShape/boostStep` no longer drive gameplay (keep fields only if needed so old content loads).
- [ ] `EnemyController.ShieldHit/Break`, shield pip UI and `KnownWeak` removed; Prism no longer forces pips.
- [ ] `PlayerController.BoostUpdate/SpendBoost`, BP, the Shift/RT charge and the HUD Boost badge removed.
- [ ] Flare and other moves still work without pips; no console errors; EditMode tests pass.

## Where (Assets/HearthHollow/Scripts)
Core/Types.cs, Dungeon/PlayerController.cs, Dungeon/EnemyController.cs, Dungeon/DungeonHud.cs, DungeonSettings "Break & Boost".

## Comments
