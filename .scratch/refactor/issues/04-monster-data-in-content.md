# Move monster gameplay data into the content

Status: done
Type: task
Blocked by: 03

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

Monster gameplay values sit in two places: the content (`MonsterData`) and the Dungeon Settings asset (`EnemyProfile`: archetype, speed, range, windup, hits, element, weak/resist). The balance simulator has to read both, and tests can't see the asset's half.

## Acceptance criteria
- [x] Gameplay fields live on `MonsterData`; `EnemyProfile` keeps only visuals (scale, eyes, rim, carried light)
- [x] Wolf / bat / slime habits become data flags (`circles`, `swoops`, `splitsInto`) instead of matching on the monster's name (`MonsterHabits`)
- [x] The values are unchanged: a test checks every monster's numbers before and after
- [x] Balance simulator reads content only
- [x] All tests pass; play-checked; no gameplay or balance change

## Where
Core/Types.cs, Core/DungeonRules.cs (MonsterHabits), Dungeon/DungeonSettings.cs, Dungeon/EnemyController.cs, Tests/Editor/BalanceSimulator.cs.

## Comments

(Claude, 12 Nov) Done. How each monster moves and fights now lives on `MonsterData` in the content: archetype, speed, range, windup, cooldown, projectile speed, hits, healing allies, light reaction, element, weak/resist, and the habit flags `circles`, `swoops` and `splits`. The values were copied from the Dungeon Settings asset for all 10 monsters (incl. the three bosses) and checked identical. `EnemyProfile` keeps only looks (scale, eyes, rim, carried light). `EnemyArchetype` and `LightReaction` moved to Core. `EnemyController` reads `Data` (fighting) and `Profile` (looks). `MonsterHabits` no longer matches on names: wolves/bats/slimes are flagged in data. The balance simulator and the Meadow/Chili tests read the content, so the sim no longer needs the Unity asset for monsters. The content version is now **46**, with an upgrade step that copies the fighting values into older content, and the asset was upgraded. The Dungeon Kit builder now seeds looks only. The balance report is unchanged apart from its version line. 175/175 tests pass; play-checked in Frost Hollow: a wolf (Chaser, Frost, circles), a bat (Flyer, swoops, scale 1.5) and the Hoarfrost Alpha spawned with the right data, and a Moss Slime still split into two Little Moss Slimes.
