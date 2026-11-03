# Remove the "Flee home" option from the menu

Status: needs-triage
Type: task
Blocked by: none

## Why
(Ryan, 3 Nov) Fleeing from the pause menu skips the ladder choice: you can walk out of any floor with your whole bag at no risk. With deep dungeons, going home should only happen at an exit (odd floors and after bosses), or by falling.

## Acceptance criteria
- [ ] The Settings / pause menu (Esc) no longer offers "Flee home" during a dungeon run (`UI/SettingsOverlay.cs`, the `_flee` button in the run section).
- [ ] No other way to leave a run mid-floor except an exit or falling. (The old fight overlay and fog-grid screen also have Flee buttons, but those screens aren't used in the slice; hide or remove them with them.)
- [ ] Decide what quitting the game mid-run does (today the run resumes on relaunch; keep that).

## Where (Assets/HearthHollow/Scripts)
UI/SettingsOverlay.cs (and `GameSession.FleeHome` if nothing else uses it).

## Comments
