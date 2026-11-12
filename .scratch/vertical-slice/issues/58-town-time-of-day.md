# Town by time of day

Status: done
Type: task
Blocked by: 57

## Why
(Ryan, 12 Nov: "run with all the above" while he tests the slice.) The town shifts with the day count (lanterns light in the evening) and the Hearth glows brighter as you clear chapters.

## Acceptance criteria
- [x] Day/evening cycle by day count
- [x] Lanterns
- [x] Hearth glow by progress
- [x] Tests where it makes sense; play-checked; no balance changes beyond what's noted

## Comments

(Claude, 12 Nov) Done. Added `Core/TownClock`. The town turns morning → afternoon → evening as the days go by (day 1, 2, 3, then round again). The global light follows: cool in the morning, golden in the afternoon, a warm dusk at 45% in the evening. Rain and harvest-moon days keep their own looks. Lanterns now stand from the start in the slice (4, plus 4 per area cleared). They're unlit in the morning, faint in the afternoon, and full in the evening, on rain and at night. The Hearth's size and colour now follow the slice's three areas (`TownClock.Progress`) instead of the full game's chapters, and it burns 35% brighter at dusk. Its message now says "x of 3 areas cleared". The town HUD shows e.g. "🌇 Day 3 · Evening". Tests: TownClockTests (162/162). Play-checked morning against evening.
