# Small monster behaviours

Status: done
Type: task
Blocked by: 63

## Why
(Ryan, 12 Nov: "run with all the above" while he tests the slice.) Frost Hollow wolves circle in packs, bats swoop, slimes split.

## Acceptance criteria
- [x] Wolf pack circling
- [x] Bat swoop
- [x] Slime split (small slimes, no extra loot)
- [x] Tests where it makes sense; play-checked; no balance changes beyond what's noted

## Comments

(Claude, 12 Nov) Done. Added `Core/MonsterHabits`. **Wolves** circle you ~2.6 units out while they wait their turn: each picks a direction, and their first attack is staggered by 0.4–1.6 s so a pack takes turns darting in rather than mobbing. **Bats** swoop: their dart now bows out to one side and back in a cosine arc, ending on the same line, so the reach is the same. **Slimes** (not bosses) split into two Little slimes when they die. Each little slime has 35% HP, 50% damage and 0.62 size, pops in fast, and joins the room so it must be beaten to clear it. They give **no coins, xp or loot** (`EnemyStats.split`; they still count as kills) and don't split again. Tests: MonsterHabitsTests (170/170). Play-checked in Meadow: a Moss Slime split into two Little Moss Slimes (hp 2), and three Fog Wolves held off and took turns. Note for the playtest: splitting adds a little extra fight to slime rooms.
