# Gate the game to the slice

Status: ready-for-agent
Type: task
Blocked by: 04

## Why
Only the Cook, Meadow Hollow and Chili Ridge are reachable in the slice; everything else is hidden, not deleted.

## Acceptance criteria
- [ ] A `SliceMode` (on by default) hides other classes, old routes, mutators, camp events and the old camp screen from normal play.
- [ ] Dev panel can toggle it off to reach old content for reference.
- [ ] Route board shows only slice dungeons (placeholders until tickets 12–14).

## Where (Assets/HearthHollow/Scripts)
GameSession, Routes.cs, RouteScreen.cs, ClassSelectScreen.cs, TownController, SettingsOverlay.cs.

## Comments
