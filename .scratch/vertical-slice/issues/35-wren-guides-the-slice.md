# Old Wren guides you through the slice

Status: done
Type: task
Blocked by: none

## Why
(Ryan, 4 Nov, playtest) The player needs more direction. Wren should say what to do next: head into the first area; on the next visit, cook a meal and come back to show her; after floor 5, go to the chili dungeon, whose first trip is simple, quick and easy so you come back; then Wren says to season a meal with fire for a bonus.

## Comments
- (Claude, 4 Nov) Done. `Core/WrenStory.cs`: `Profile.wrenStep` (saveVersion 14 places older saves) runs Meet → FirstDive → AfterDive → CookMeal → ShowMeal → FirstBoss → HearHint → Ridge → RidgeBack → SeasonMeal → Alpha → Done. Wren comes over by herself when she has news (the start, home after the first dive, the floor-5 hint, home from the Ridge); showing her your meal is done by talking to her. Each conversation ends with a 📌 line and a button to where you go next (Route board / cooking area / spice rack). The town HUD always shows the 📌 objective at the top. The first Chili Ridge trip (`RunState.firstRidge`) is one floor, monsters ×0.6, the far room is the way home, every monster drops something and 70% of drops are chilies. Seasoning a meal with chili moves you on to the Hoarfrost Alpha. Tests: `WrenStoryTests` walks the whole thread (120/120 pass).
