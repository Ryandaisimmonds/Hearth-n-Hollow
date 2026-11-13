# Split PlayerController: one handler per move

Status: done
Type: task

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`PlayerController` is 1,520 lines; about 880 of them are one `switch` over every `MoveKind`. Adding or tuning a move means editing the biggest file in the game. **Do after the vertical-slice playtest** (it touches combat feel).

## Acceptance criteria
- [x] Each move kind is its own small class (or a table of handlers) behind one interface; `PlayerController` keeps input, movement, timers and getting hit
- [x] No behaviour change: every Cook move play-checked; other classes' moves compile and run
- [x] `PlayerController` under ~500 lines
- [x] All tests pass; play-checked; no gameplay or balance change

## Where
Dungeon/PlayerController.cs, Dungeon/ClassFx.cs.

## Comments

(Claude, 12 Nov) Done. Each move kind is now a handler in a lookup table. There are 35 skills, one method each (UseCleave, UsePepperToss, ...), in PlayerSkills.<Ultimates|Warden|Ranger|Lamplighter|Emberfist|Cook>.cs, registered in PlayerSkills.cs. There are 7 attacks (AttackCombo, AttackHeavy, ..., with AttackSwing as the fallback) in PlayerAttacks.cs. The dash went to PlayerDash.cs and the shared move helpers (colours, light, aim assist, fans, slams, beams, ongoing effects) to PlayerMoveKit.cs. PlayerController is from 1,522 to 528 lines: input, movement, timers, the HUD feed, getting hit, and the UseSkill/AttackHit dispatch that does what every move shares. These are partial-class files of PlayerController (they share its state), not separate types. The code was moved by a script, case by case, unchanged. The smoke test now plays every move in the content against a target: all classes' attacks, dashes, skills and ultimates. Old code: 15 attacks, 6 dashes, 28 skills used (Spotlight has nothing to spot). New code: the same counts, with no errors. Its time limit went up to 5 minutes (it takes about 2.5). 182/182 EditMode and 1/1 PlayMode pass.
