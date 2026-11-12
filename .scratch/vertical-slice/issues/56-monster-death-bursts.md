# Monster deaths in their own colours

Status: done
Type: task
Blocked by: 55

## Why
(Ryan, 12 Nov: "run with all the above" while he tests the slice.) Each monster bursts in its own colour and element: fire leaves embers, frost shatters into ice, nature drops leaves.

## Acceptance criteria
- [x] Per-monster/element death burst
- [x] Tests where it makes sense; play-checked; no balance changes beyond what's noted

## Comments

(Claude, 12 Nov) Done. Added `DungeonFx.DeathBurst`. Every monster throws spinning chunks and a puff in its own colour (`DungeonFx.AverageColour` reads the sprite once and caches it; the wolf comes out a pale grey-green). On top of that, its element adds a flourish: fire gets rising flames and embers, frost shatters into spinning ice shards with a ring, nature scatters fluttering leaves, water splashes droplets, and light twinkles. The death light and sparks are tinted to match, and bosses get a bigger version plus the old slow-mo. FxSprite gained `spin` and `sway`. Play-checked by killing a frost wolf, a water crab and a fire bat in Frost Hollow, plus a side-by-side burst of each element; no errors. No new test (pure visuals); 159/159 still pass.
