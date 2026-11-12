# Delete the old grid game

Status: done
Type: task

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

The turn-based fog-grid mode is still wired in as a fallback (`UseDungeon = false`). `HollowScreen`, `FightOverlay`, `Explore`, `Combat` and `GatherOverlay` total about 1,400 lines, and `Phase.Explore` / `Phase.Fight` are referenced 18 times.

## Acceptance criteria
- [x] `UseDungeon` and the grid fallback in GameBootstrap removed (a missing dungeon kit is an error, not a different game)
- [x] `HollowScreen` and `FightOverlay` deleted; `Phase.Explore` / `Phase.Fight` and their `GameUI` layout branches removed
- [x] The parts the dungeon still uses are kept and moved somewhere sensible: foraging (`Explore.StartGather`, `GatherOverlay`), kill coins (`Combat.KillCoins`), rest
- [x] Tests that only covered the grid game removed; the rest pass
- [x] Old saves still load
- [x] All tests pass; play-checked; no gameplay or balance change

## Where
Core/Explore.cs, Core/Combat.cs, Core/GameSession.cs, UI/HollowScreen.cs, UI/FightOverlay.cs, UI/GameUI.cs, Runtime/GameBootstrap.cs.

## Comments

(Claude, 12 Nov) Done. Removed the fog-grid game: `HollowScreen` and `FightOverlay` deleted; `GameSession.MoveTo / CanMoveTo / Fight / CanUseSkill` gone; `UseDungeon` gone (a missing dungeon kit is now an error, not a different game); `Tile`, `TileType`, `FightState`, `FightAction` and the RunState grid fields (`tiles`, `width/height`, `px/py`, `fight`, `skillCooldown`) removed. What the dungeon still used moved into a new `Core/RunRules`: `CreateRun` (no grid), the forage minigame, rest, `ApplyArmour`, `KillCoins / KillXp` (now take elite + difficulty instead of a FightState). `Phase` keeps its old numbers (`Hearth=0, Gather=3, Rest=4, RunEnd=5, Dungeon=6`) so nothing stored by number shifts. GameUI lost both screens, the fight outro and the hollow theme; the rest overlay's heal now counts the campfire heal in the dungeon. About 1,000 lines gone. Left for later: the old `.hollow` / `.fight` styles (ticket 15) and the grid tuning fields and `MonsterBehaviour` / `SkillKind` content fields (ticket 03). 175/175 tests pass; play-checked a Meadow run with the forage overlay, a campfire rest, fleeing and the summary; your save still loads.
