# Ways to heal slowly

Status: done
Type: task
Blocked by: none

## Why
(Ryan, 4 Nov, playtest) Health is the issue: only one meal heals bit by bit. Add ways to slowly heal.

## Comments
- (Claude, 4 Nov) Done. Content v37 + tuning: (1) catch your breath: out of a fight you heal 1 HP every 2.5 s up to 60% of max HP (`calmRegenSeconds`, `calmRegenCap`); (2) new meal stat **Regen** (HP every 10 s, fights included): Camp Bread +1, Herb Stew +2, Hearty Herb Stew +3, Berry Porridge +1 (plus its heal on kill), and the out-of-slice soups; (3) foraging is a snack: +2 HP (`forageHeal`). Campfires (30%) and the Honey Locket (heal on kill) stay. A Healing filter chip on the meal list. Tests: `HealingTests`.
