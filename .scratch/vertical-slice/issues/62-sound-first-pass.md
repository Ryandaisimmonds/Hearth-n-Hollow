# Sound, first pass

Status: done
Type: task
Blocked by: 61

## Why
(Ryan, 12 Nov: "run with all the above" while he tests the slice.) The game has no audio. Generate and wire: hits, pickups, cooking sizzle and bar, dig, a gentle town loop, a dungeon ambience.

## Acceptance criteria
- [x] Audio manager + volume setting
- [x] SFX set
- [x] Town and dungeon loops
- [x] Tests where it makes sense; play-checked; no balance changes beyond what's noted

## Comments

(Claude, 12 Nov) Done. The project already had the Ninja Adventure pack's audio (188 CC0 clips: music, jingles, effects), so I used that rather than generating sound. `Hearth & Hollow > Build Audio Library` maps ids to clips in `Resources/HHAudio/AudioLibrary.asset` (26 sounds, 9 loops; swap any clip in the Inspector). `Runtime/GameAudio` (made by GameBootstrap) plays one-shots by id with pitch jitter and repeat guards, and a sizzle loop on the stove. Music follows where you are with a 1.5 s crossfade: town Calm Village, Meadow Clearing, Chili Ridge Road, Frost Hollow Mystical, the fall Curse, and Fight while a boss lives. There's an ambience loop too (wind in Frost Hollow, rain on storm days). Wired: every button click, swing, hit/crit, element hits, kill/boss kill, hurt, boss entrance, room clear, dig + chest, loot, coins, level up, run cleared, death, cooking sizzle + Plain/Fine/Prime result, and the feast. Settings has Music and Effects volumes (Off/25/50/75/100, saved in PlayerPrefs). Tests: GameAudioTests (166/166). Play-checked: town music, then crossfade to Frost Hollow plus wind.
