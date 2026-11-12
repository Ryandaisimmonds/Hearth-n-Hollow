# Atmosphere for each area

Status: done
Type: task
Blocked by: 54

## Why
(Ryan, 12 Nov: "run with all the above" while he tests the slice.) Meadow: drifting pollen and fireflies. Chili Ridge: ember sparks and heat shimmer. Frost Hollow: falling snow and frosty breath. The fall: real rolling fog in the world instead of a flat overlay.

## Acceptance criteria
- [x] Per-area ambient particles
- [x] Frost breath puffs on the player in Frost Hollow
- [x] Fall fog as world-space fog sprites that thicken room by room
- [x] Tests where it makes sense; play-checked; no balance changes beyond what's noted

## Comments

(Claude, 12 Nov) Done. New `AreaAtmosphere` (added by DungeonController) runs a pooled set of fading sprites per area. Meadow gets drifting pollen and blinking fireflies. Chili Ridge gets rising embers and faint heat shimmer. Frost Hollow (and any Cold route) gets falling snow and a breath puff from the cook every ~1.7 s. The fall gets 16 big soft fog banks rolling across the view in world space, as thick as `DungeonHud.Fog01` (so they thicken room by room and pour in in the fog room). The flat screen veil is now 60% as strong. Nothing touches gameplay. Added `DungeonFx.Soft` (a soft blob), `PlayerController.FacingDir` and Resources/HHFx/Unlit Sprite.mat. Test: AreaAtmosphereTests (159/159). Play-checked all four areas with screenshots.
