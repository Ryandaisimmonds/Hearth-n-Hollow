# Stove juice

Status: done
Type: task
Blocked by: 58

## Why
(Ryan, 12 Nov: "run with all the above" while he tests the slice.) Steam and sizzle particles while cooking, sparkles while the bar is in the gold, a gold burst on a Prime meal.

## Acceptance criteria
- [x] Steam/sizzle
- [x] Gold-zone sparkle
- [x] Prime burst
- [x] Tests where it makes sense; play-checked; no balance changes beyond what's noted

## Comments

(Claude, 12 Nov) Done. Added `UI/UiParticles`, a small particle layer for UI Toolkit (soft blobs, ✦/✧ glyphs, bursts) placed over the stove's box. While the heat bar runs, steam rises off the whole bar and hot spatters jump off the marker; there are more and hotter spatters the closer it is to gold. In the gold, the marker glows (`gather-bar__marker--gold`) and throws sparkles. On the result card, Prime gets a 46-piece gold burst, a pop of the picture and a small shake; Fine gets a smaller warm burst; every meal steams. Visual only; the grade rules are unchanged. Play-checked a cook in progress and a Prime result. No new test (pure UI); 162/162 pass.
