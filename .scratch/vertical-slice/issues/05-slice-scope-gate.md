# Gate the game to the slice

Status: done
Type: task
Blocked by: 04

## Why
Only the Cook, Meadow Hollow and Chili Ridge are reachable in the slice; everything else is hidden, not deleted.

## Acceptance criteria
- [x] A `SliceMode` (on by default) hides other classes, old routes, mutators, camp events and the old camp screen from normal play.
- [x] Dev panel can toggle it off to reach old content for reference.
- [x] Route board shows only slice dungeons (placeholders until tickets 12–14).

## Where (Assets/HearthHollow/Scripts)
GameSession, Routes.cs, RouteScreen.cs, ClassSelectScreen.cs, TownController, SettingsOverlay.cs.

## Comments
- 3 Nov 2026 (Claude): Done.
  - New `Core/Slice.cs`: `Slice.On` (default on), slice routes `meadow`, `chili`, slice classes `Cook`.
    GameBootstrap reads it from PlayerPrefs (`hh_slice`); the dev panel has a "Vertical slice: ON/OFF" toggle.
  - While on: only the Cook can be selected or start a run (`GameSession.ClassAvailable`), a save on another
    class falls back to the Cook; class select shows only the Cook; other classes don't stand in town.
    The Route board shows only Meadow and Chili Ridge, and other routes can't be started. No route mutators,
    no camp events (new days roll Calm). The game opens in the town; the signpost back to the camp screen and
    the Settings "back to the camp screen" button are hidden.
  - Chili Ridge keeps its old unlock rule for now; ticket 15 unlocks it through Wren.
  - Also cleaned two ticket-04 leftovers: the "+N XP" in the route info and the XP chip on the dungeon HUD;
    the "Double XP" route modifier no longer rolls.
  - Tests: new `SliceTests` (5). 44/44 pass. In play: opens in town as the Cook, no camp event, no signpost,
    no class NPCs; Route board shows only the two slice routes; a run out and back returns to town.
    No console errors. Save backed up and restored.
