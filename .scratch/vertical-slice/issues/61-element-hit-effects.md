# Element effects on hits

Status: done
Type: task
Blocked by: 60

## Why
(Ryan, 12 Nov: "run with all the above" while he tests the slice.) Fire leaves a short burn, Frost a blue slow tint, Nature a leaf burst (visual first; any gameplay numbers small and flagged).

## Acceptance criteria
- [x] Fire burn flicker
- [x] Frost tint + slight slow
- [x] Nature leaves
- [x] Tests where it makes sense; play-checked; no balance changes beyond what's noted

## Comments

(Claude, 12 Nov) Done. Every elemental hit now leaves a mark (`EnemyController.ElementMark`); an infused hit marks every element it carries. Fire burns for 1.2 s: the monster flickers orange and sheds little flames. This is visual only, with no damage over time. Frost chills for 1.5 s with a blue tint, a spray of ice chips and a **slight slow (15%, `ChillSlow` 0.85)**. **⚠ This is the one gameplay number** in this ticket; it's flagged here for Ryan's playtest. Nature throws a burst of fluttering leaves, and water splashes droplets. The tint sits above the old slow tint. Test: ElementHitTests (164/164). Play-checked fire, frost and nature hits on Moss Slimes (tints confirmed).
