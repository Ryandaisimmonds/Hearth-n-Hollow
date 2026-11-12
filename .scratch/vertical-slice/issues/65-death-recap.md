# What got you

Status: done
Type: task
Blocked by: 64

## Why
(Ryan, 12 Nov: "run with all the above" while he tests the slice.) On death, a short recap: the last few hits and who landed them.

## Acceptance criteria
- [x] Track last hits
- [x] Recap on the run summary
- [x] Tests where it makes sense; play-checked; no balance changes beyond what's noted

## Comments

(Claude, 12 Nov) Done. Added `Core/DeathRecap`. Each run keeps its last 5 hits (`RunState.lastHits`: who, damage, HP left). The source now runs through every way you take damage: monster bites and charges, thrown shots (`Projectile.Source`), the area hazard ("🥶 The cold") and the fall's fog. Monsters show as e.g. "🐺 Fog Wolf", "⭐ Elite Fog Wolf" or "👑 Bramble Sow". On a death, the run summary shows a "💀 What got you" panel with the hits in order (HP left after each), the last blow highlighted, and who did most of the damage. Tests: DeathRecapTests (172/172). Play-checked a real death to a Moss Slime and an Elite Fog Wolf.
