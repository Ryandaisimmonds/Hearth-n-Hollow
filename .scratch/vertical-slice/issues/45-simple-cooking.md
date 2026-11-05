# Simple cooking: bouncing bar decides quality, one meal and back

Status: done
Type: task
Blocked by: 44

## Why
(Playtest, 5 Nov) Far too much information on the cooking screen: it should be "you put this in, you get this, done". The bar should go back and forth. One meal per visit.

## Decisions (grilling, 5 Nov)
- The bar bounces back and forth at a steady speed until you stop it (no speed-up, no burning). It's a fun add-on, not the whole thing.
- The bar decides quality outright: gold zone = Prime, warm zone = Fine, anywhere else = Plain. Kitchen upgrades widen the zones.
- One meal per visit: pick a recipe → bar → result card ("Prime Herb Stew!") → back to the cooking area in town.
- The cooking screen keeps only: the recipe picture, ingredients (have/need), one line of what it gives, and the Cook button. Quality odds, kitchen-level bonuses, synergy lists and long tooltips go.

## Acceptance criteria
- [x] Bouncing bar, steady pace
- [x] Bar result sets the grade directly; kitchen level widens the zones
- [x] Result card, then the cooking screen closes back to town
- [x] Stripped-down recipe view (the four things above)
- [x] Tests for zone → grade, and zone width by kitchen level

## Comments
- (Claude, 5 Nov) Done. `CookOverlay` rewritten: the dish, "2 needed · have 11" lines, "You get: 🗡️ +2.2 attack", and Cook. Cook starts the heat bar, which bounces steadily until you stop it (click, Enter or Space). Where it stops sets the grade (`Cooking.CookGrade`): gold = Prime, warm = Fine, else Plain. Each kitchen level past 1 widens the gold zone by 2.5% and the warm zone by 5% (`Cooking.CookZones`). Then a result card ("🔵 Prime Mushroom Skewer! +4.4 attack"), and Done closes the stove and the station menu, back to town. Quick cook, the quality odds, the element slot and the long notes are gone. Tests: CookGameTests (bounce, bar → grade, zones by level, cook with the bar's grade). 139/139 pass; play-checked.
