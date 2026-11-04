# Pack a full tray before setting out; slim the route panel

Status: done
Type: task
Blocked by: 39

## Why
(Ryan, 4 Nov) You can miss making a meal. On the Route board, drop the chosen-chapter blurb on the right (they already know it) and make the player pack 3 meals before heading out, except on the first encounter.

## What
- [x] Right panel in the slice: just the route name, any lock/class warning and the shortcut chips
- [x] Tray comes first, above your cook
- [x] Enter is disabled until the tray is full ("🧺 Pack 3 meals to set out (n/3)"); Enter key shows a toast
- [x] Not on the first dive (arcStage 0)
- [x] Camp Bread can always fill the tray, so nobody gets stuck

## Comments
- (Claude, 4 Nov) Done. `GameSession.TrayBlock` holds the rule (UI-only, StartRun unchanged). Test: `HealingTests.SettingOut_NeedsAFullTray_ExceptTheFirstDive` (124/124 pass). Play-checked on the Route board.
