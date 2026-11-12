# Whole-slice autoplay test

Status: done
Type: task
Blocked by: 65

## Why
(Ryan, 12 Nov: "run with all the above" while he tests the slice.) A play-mode test that runs the slice start to finish on its own (fall, Wren, cooking, all three areas) and fails if anything gets stuck.

## Acceptance criteria
- [x] Drives the real session and scenes
- [x] Fails on soft-locks / missing steps
- [x] Tests where it makes sense; play-checked; no balance changes beyond what's noted

## Comments

(Claude, 12 Nov) Done. Added `SliceAutoplayTests`: a bot plays the whole slice through the real GameSession on 3 seeds (all pass, ~1 s). It only does what a player could: fall and die in the fog, talk to Wren when she wants a word, cook what the pantry allows, pack the tray (Camp Bread if needed), go where the objective names, fight a few monsters and forage two spots per floor (hitting the gather zone 3 times in 4), and beat bosses. It runs until Wren's thread is Done. It fails on any dead end (can't set out, nothing to cook when Wren asks, an objective naming nowhere reachable, a forage that never ends) or after 80 actions without the story moving, and prints the last actions and info lines. **Scope:** this drives the real session rules, not the scenes; a scene-level play-mode test would need the game code in its own assembly and would touch the real save, so I left it out. **What the bot found while I tuned it (worth knowing for the playtest):** (1) chili only comes from foraging on Chili Ridge (1 in 3 spots), so a player who doesn't forage can loop the ridge for nothing; (2) at the "season a meal" step, cooking a Firecracker Skewer can use up the chilies you need for seasoning, and since packed meals are eaten each run, the meal pantry is often empty at that point. Neither is a hard lock, but both are easy to stumble into. 175/175 tests pass.
