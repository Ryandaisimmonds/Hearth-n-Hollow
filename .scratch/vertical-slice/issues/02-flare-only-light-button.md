# Remove the thrown torch; Flare is the one light button

Status: done
Type: task

## Why
Design v2: one light button (Flare). The torch didn't add enough.

## Acceptance criteria
- [x] `ThrownTorch`, `_torchCd`, `torchCooldown/torchSeconds`, the R/LB binding and the torch HUD badge are removed.
- [x] Braziers light from fire hits, Flare and room clears only; brazier puzzles still solvable (in-fight Flare or fire hits).
- [x] Flare stays on F / LT with its current cooldown rules.
- [x] Tests pass, no console errors.

## Where (Assets/HearthHollow/Scripts)
Dungeon/PlayerController.cs, DungeonController.cs, RoomLights.cs, DungeonInput.cs, DungeonHud.cs.

## Comments
- 3 Nov 2026 (Claude): Done.
  - Removed: the player's torch throw (R / LB), its cooldown, the HUD torch badge, the help-text mention and
    the torch settings (`torchCooldown`, `torchSeconds`, `torchRange`).
  - Kept: the `ThrownTorch` component itself, because the Ranger's Ember Arrow uses it as its burning light.
    It's no longer something the player throws.
  - Braziers still light from hits, Flare and room clears; puzzles are solvable with hits or an in-fight Flare.
  - Checked: clean compile; 38/38 EditMode tests; in play, Flare fired, Ember Arrow left its burning light,
    braziers lit, no console errors. Save backed up and restored.
