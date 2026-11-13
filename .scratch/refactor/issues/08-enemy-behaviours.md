# Split EnemyController: one behaviour per archetype

Status: done
Type: task
Blocked by: 04

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`EnemyController` is 808 lines with archetype `switch`es in thinking, moving, winding up, releasing and telegraphing. **Do after the playtest.**

## Acceptance criteria
- [x] Chaser / Charger / Flyer / Caster / Brute are behaviour classes; `EnemyController` keeps health, status effects, hits and death
- [x] Telegraph shape comes from the behaviour, not a separate switch
- [x] No behaviour change: each archetype and the Bramble Sow play-checked
- [x] All tests pass; play-checked; no gameplay or balance change

## Where
Dungeon/EnemyController.cs, Dungeon/Telegraph.cs.

## Comments

(Claude, 12 Nov) Done. New EnemyBehaviour.cs has one class per archetype: Chaser, Charger, Flyer, Caster, Brute. Each answers when to strike, its boss marker (the telegraph shape now comes from the behaviour), the strike, charge speed and time (and the bat's swoop), shot colour, and how it moves between attacks (wolves circling included). EnemyController keeps health, statuses, hits, death, light and the state clock, and lost its five archetype switches. The formulas moved as they were. One thing was easy to get wrong: a bat's swoop scales with the base charge speed, not the boosted one, and that's kept. The smoke test now fights one monster of each archetype plus the Bramble Sow as a boss. Each must move, wind up and strike within 10 s while you're kept alive. That check passed twice on the old code and three times on the new. The test's room search and spawn spot were made sturdier after two flaky runs. 182/182 EditMode and 1/1 PlayMode pass.
