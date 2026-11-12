# Move monster gameplay data into the content

Status: ready-for-agent
Type: task
Blocked by: 03

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

Monster gameplay values sit in two places: the content (`MonsterData`) and the Dungeon Settings asset (`EnemyProfile`: archetype, speed, range, windup, hits, element, weak/resist). The balance simulator has to read both, and tests can't see the asset's half.

## Acceptance criteria
- [ ] Gameplay fields live on `MonsterData`; `EnemyProfile` keeps only visuals (scale, eyes, rim, carried light)
- [ ] Wolf / bat / slime habits become data flags (`circles`, `swoops`, `splitsInto`) instead of matching on the monster's name (`MonsterHabits`)
- [ ] The values are unchanged: a test checks every monster's numbers before and after
- [ ] Balance simulator reads content only
- [ ] All tests pass; play-checked; no gameplay or balance change

## Where
Core/Types.cs, Core/DungeonRules.cs (MonsterHabits), Dungeon/DungeonSettings.cs, Dungeon/EnemyController.cs, Tests/Editor/BalanceSimulator.cs.

## Comments
