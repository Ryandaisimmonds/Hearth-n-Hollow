# Remove the thrown torch; Flare is the one light button

Status: ready-for-agent
Type: task

## Why
Design v2: one light button (Flare). The torch didn't add enough.

## Acceptance criteria
- [ ] `ThrownTorch`, `_torchCd`, `torchCooldown/torchSeconds`, the R/LB binding and the torch HUD badge are removed.
- [ ] Braziers light from fire hits, Flare and room clears only; brazier puzzles still solvable (in-fight Flare or fire hits).
- [ ] Flare stays on F / LT with its current cooldown rules.
- [ ] Tests pass, no console errors.

## Where (Assets/HearthHollow/Scripts)
Dungeon/PlayerController.cs, DungeonController.cs, RoomLights.cs, DungeonInput.cs, DungeonHud.cs.

## Comments
