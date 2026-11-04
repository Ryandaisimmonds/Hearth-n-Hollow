# Heal a share of your HP for every room you clear

Status: done
Type: task
Blocked by: 38

## Why
(Ryan, 4 Nov) Instead of slowly healing out of a fight, each cleared room gives a % of health back, so you don't just stand there waiting.

## Comments
- (Claude, 4 Nov) Done. The out-of-fight trickle (ticket 38) is gone. `GameSession.DungeonRoomCleared` heals `tuning.roomClearHealPct` (15%) of max HP, shown as "💗 +N" over you and in the run log. Meal Regen, forage snacks, campfires and the Honey Locket stay. Test: `HealingTests.ClearingARoom_HealsAShare` (123/123 pass).
