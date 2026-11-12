# Delete the old grid game

Status: ready-for-agent
Type: task

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

The turn-based fog-grid mode is still wired in as a fallback (`UseDungeon = false`). `HollowScreen`, `FightOverlay`, `Explore`, `Combat` and `GatherOverlay` total about 1,400 lines, and `Phase.Explore` / `Phase.Fight` are referenced 18 times.

## Acceptance criteria
- [ ] `UseDungeon` and the grid fallback in GameBootstrap removed (a missing dungeon kit is an error, not a different game)
- [ ] `HollowScreen` and `FightOverlay` deleted; `Phase.Explore` / `Phase.Fight` and their `GameUI` layout branches removed
- [ ] The parts the dungeon still uses are kept and moved somewhere sensible: foraging (`Explore.StartGather`, `GatherOverlay`), kill coins (`Combat.KillCoins`), rest
- [ ] Tests that only covered the grid game removed; the rest pass
- [ ] Old saves still load
- [ ] All tests pass; play-checked; no gameplay or balance change

## Where
Core/Explore.cs, Core/Combat.cs, Core/GameSession.cs, UI/HollowScreen.cs, UI/FightOverlay.cs, UI/GameUI.cs, Runtime/GameBootstrap.cs.

## Comments
