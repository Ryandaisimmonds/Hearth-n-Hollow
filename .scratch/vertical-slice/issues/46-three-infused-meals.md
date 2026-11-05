# Up to three infused meals: multi-element attacks that can cancel

Status: done
Type: task
Blocked by: 44

## Why
(Playtest, 5 Nov) All infused (element) meals should be eatable at once, so you can take three.

## Decisions (grilling, 5 Nov)
- Remove "only one infused meal per tray".
- Your attacks carry every infused element at once.
- Damage against a monster: weakness only ×1.5 · resistance only ×0.5 · at least one weakness and one resistance = ×1 with a "cancelled" pop · two weaknesses still ×1.5.
- Attack particles show every element's colour (Fire + Frost = orange and blue).
- Guards against elements stack as now.

## Acceptance criteria
- [x] Up to three infusions in a tray
- [x] Multi-element hits with the rule above, and "Weak!", "Resists" and "Cancelled" pops
- [x] Discovery: a hit teaches you each carried element's result on that monster
- [x] Mixed-colour attack flair
- [x] Spice rack and Route board text updated; tests for the damage rule

## Comments
- (Claude, 5 Nov) Done. The one-infusion rule is gone from packing, seasoning and the spice rack. `RunState.infusions` holds every infused element (`Run.element` stays as the first, for old code). A hit that takes the meals' element is judged on all of them (`Elements.Multiplier(list, …)`): weakness only ×1.5, resistance only ×0.5, both = ×1 with a "Cancelled" pop, two weaknesses ×1.5. Each carried element is discovered on contact, and discovery messages stack one line each. Hits burst in every element's colour, the aura motes take turns, and the HUD shows "🔥❄️ Infused". Tests: `ElementMixTests` (6), Cooking/Seasoning tests updated. 145/145 pass. Play-checked with Fire + Frost: slime (weak to Frost) 6→3, bat (resists Fire) 6→5.
