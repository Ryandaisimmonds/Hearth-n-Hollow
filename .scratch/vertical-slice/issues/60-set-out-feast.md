# The set-out eating moment

Status: done
Type: task
Blocked by: 59

## Why
(Ryan, 12 Nov: "run with all the above" while he tests the slice.) A short moment at the gate where your three meals pop and their bonuses fly into your stats.

## Acceptance criteria
- [x] Feast moment on entering the dungeon
- [x] Skippable
- [x] Tests where it makes sense; play-checked; no balance changes beyond what's noted

## Comments

(Claude, 12 Nov) Done. Added `UI/FeastMoment`. Pressing Enter on the route board with meals packed now plays a ~2.8 s feast before the run starts. Each meal pops onto a wooden table with a sparkle burst, showing its art, grade and bonuses; then 🗡️/❤️ glyphs arc from each meal into your Attack and HP, which count up from bare to fed (e.g. 4 → 16, 14 → 46) with the gain in green. A click, Enter or any key skips straight to the run; closing the screen cancels it. With nothing packed there's no feast. UiParticles gained `Fly`. Test: FeastTests (163/163). Play-checked mid-feast and end.
