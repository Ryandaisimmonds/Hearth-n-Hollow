# Split EnemyController: one behaviour per archetype

Status: ready-for-agent
Type: task
Blocked by: 04

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`EnemyController` is 808 lines with archetype `switch`es in thinking, moving, winding up, releasing and telegraphing. **Do after the playtest.**

## Acceptance criteria
- [ ] Chaser / Charger / Flyer / Caster / Brute are behaviour classes; `EnemyController` keeps health, status effects, hits and death
- [ ] Telegraph shape comes from the behaviour, not a separate switch
- [ ] No behaviour change: each archetype and the Bramble Sow play-checked
- [ ] All tests pass; play-checked; no gameplay or balance change

## Where
Dungeon/EnemyController.cs, Dungeon/Telegraph.cs.

## Comments
