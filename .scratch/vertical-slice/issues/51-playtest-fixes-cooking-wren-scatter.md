# Playtest fixes: cooking card, Wren before setting out, loot scatter

Status: done
Type: task
Blocked by: 50

## Why
(Ryan, 5 Nov) After the cooking game a button comes up: make it a temporary box that goes after a couple of seconds or on a click, and take you back to the cooking area. Back from the first dungeon you can't pack meals before speaking to Wren, but you can still go on adventures: stop that. Several items dropping at once need to fall away from each other.

## What
- [x] Cooking result: no button. The card goes after ~2.2 s, or on a click / Enter, back to the cooking area's recipe list (not out to town)
- [x] No setting out (town gate, Route board Enter) until Wren's had her say: "Talk to Old Wren first" → "Cook a meal and show Old Wren first" → "Show Old Wren your meal first", and whenever she has something new to tell you (`GameSession.WrenSetOutBlock`)
- [x] Drops that land on the same spot hop out to their own spots (a ring of free walkable spots) with a little bounce

## Comments
- (Claude, 12 Nov) Done. Cooking card closes itself after ~2.2 s (or on a click / Enter) and leaves you on the recipe list. `GameSession.WrenSetOutBlock` blocks the town gate and the Route board's Enter until Wren's had her say ("Talk to Old Wren first" → "Cook a meal and show Old Wren first" → "Show Old Wren your meal first", or whenever she has something new). `FloorLootView.SpreadOut`: drops that land together hop to their own spots on a ring of free floor. Test: `TheFallTests.NoSettingOut_UntilWrenHasHadHerSay`. 155/155 pass. Play-checked all three (six drops from one chest spot spread into a ring).
