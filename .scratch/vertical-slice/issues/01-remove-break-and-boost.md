# Remove Break & Boost

Status: done
Type: task

## Why
Cut hit types, shield pips, weakness diamonds and Boost. Elements (ticket 06) replace them.

## Acceptance criteria
- [x] `HitType`, `BoostShape`, `MoveData.hitType/hitType2/boostShape/boostStep` no longer drive gameplay (keep fields only if needed so old content loads).
- [x] `EnemyController.ShieldHit/Break`, shield pip UI and `KnownWeak` removed; Prism no longer forces pips.
- [x] `PlayerController.BoostUpdate/SpendBoost`, BP, the Shift/RT charge and the HUD Boost badge removed.
- [x] Flare and other moves still work without pips; no console errors; EditMode tests pass.

## Where (Assets/HearthHollow/Scripts)
Core/Types.cs, Dungeon/PlayerController.cs, Dungeon/EnemyController.cs, Dungeon/DungeonHud.cs, DungeonSettings "Break & Boost".

## Comments
- 3 Nov 2026 (Claude): Done.
  - Removed: shield pips and weakness icons, Break (stun + ×1.5), `KnownWeak`, the Boost resource/charge/spend,
    the Shift/RT input, the HUD Boost badge, the Break & Boost settings and `EnemyProfile.shields/weak`.
    Flambé's Boost-only burning ground went with it.
  - Kept on purpose for ticket 06: `HitType` on moves and the `_curType` plumbing, now ending in an empty
    `EnemyController.TypedHit(type, strength)` hook. Ticket 06 swaps HitType for Element there.
    `MoveData.boostShape/boostStep` fields still exist (now always level 0, inert); remove them with 06.
  - Prism now just lands a Light hit on its target (06: strike with the target's weakest element).
    Spotlight and Flare Arrow no longer reveal weaknesses (07 brings discovery back).
  - Checked: compiles with no errors or warnings; 38/38 EditMode tests pass; in play mode all 49 moves
    and Flare fired against 5 spawned monsters with no console errors. Save backed up and restored.
