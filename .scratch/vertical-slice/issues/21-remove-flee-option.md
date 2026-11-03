# Remove the "Flee home" option from the menu

Status: done
Type: task
Blocked by: none

## Why
(Ryan, 3 Nov) Fleeing from the pause menu skips the ladder choice: you can walk out of any floor with your whole bag at no risk. With deep dungeons, going home should only happen at an exit (odd floors and after bosses), or by falling.

## Acceptance criteria
- [x] The Settings / pause menu (Esc) no longer offers "Flee home" during a dungeon run (`UI/SettingsOverlay.cs`, the `_flee` button in the run section).
- [x] No other way to leave a run mid-floor except an exit or falling. (The old fight overlay and fog-grid screen also have Flee buttons, but those screens aren't used in the slice; hide or remove them with them.)
- [x] Decide what quitting the game mid-run does (today the run resumes on relaunch; keep that).

## Where (Assets/HearthHollow/Scripts)
UI/SettingsOverlay.cs (and `GameSession.FleeHome` if nothing else uses it).

## Comments
- (Claude, 3 Nov) Done. The Esc menu no longer has Flee home; its run section is now the run log, with a note that the way home is an exit (odd floors and after a boss). The old fight overlay / fog-grid screens (not used with the real-time dungeon) keep theirs; `FleeHome` stays only as the error fallback when a floor can't be built. Quitting mid-run still resumes the run on relaunch.
