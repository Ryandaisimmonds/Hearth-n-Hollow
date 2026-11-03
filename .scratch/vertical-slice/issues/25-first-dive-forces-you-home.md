# The first dive is short and brutal

Status: done
Type: task
Blocked by: none

## Why
(Ryan, 3 Nov, playtest) On the first run I was still far too strong and could go deeper; the first run should push for a death and force you home.

## Acceptance criteria
- [x] First dive (opening arc stage 0): enemies much tougher (HP and damage), tunable.
- [x] No ladder on the first dive: the far room is the way home instead.

## Comments
- (Claude, 3 Nov) Done. `RunState.firstDive` (opening arc stage 0): monsters ×1.6 HP and ×2.2 damage (`tuning.firstDiveHpMult/AtkMult`), no ladder (the far room is the way home), and a warning when you set out.
