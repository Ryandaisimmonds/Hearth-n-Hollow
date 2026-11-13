# Tidy the third-party packs

Status: ready-for-human
Type: task

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

Third-party packs are 250 MB of the 275 MB in `Assets` and about 13,000 tracked files. Layer Lab and the monster pack sit at the top level with the used parts also copied under `HearthHollow/Art/ThirdParty`.

## Acceptance criteria
- [x] Ryan decides: delete unused pack content, or keep it all? Git LFS or not?
- [x] All packs live under one `Assets/ThirdParty` folder; no duplicates
- [x] Art Library, Audio Library and the dungeon kit still resolve every asset (rebuild them and check for missing)
- [x] All tests pass; play-checked; no gameplay or balance change

## Where
Assets/*, Scripts/Editor/*Builder.cs. Moving packs changes many paths in one commit; do it on a quiet day.

## Comments

(Claude, 12 Nov) Done. Ryan said go ahead (13 Nov): delete unused packs, keep Ninja Adventure and Cainos, no Git LFS. Deleted Layer Lab (76 MB), 100 Top Down Monsters (39 MB), the second Cainos download (Pixel Art Top Down - Basic v1.2.3) and Unity's Welcome folder: about 7,500 files, none referenced by the game or the builders (checked by GUID across Assets/HearthHollow and by the builders' paths). Moved Ninja Adventure, Cainos and the used Layer Lab / monster copies into Assets/ThirdParty with Unity, so references keep their GUIDs; 6,225 files show as renames. The builders' path constants, the UI stylesheets' image URLs and the READMEs were updated. Rebuilt the Art Library, Audio Library and Dungeon Kit: all mapped, and the only change is three empty slots for meals added since the last build. Every asset reference in Assets/HearthHollow resolves. UI screenshots match. 182/182 EditMode and 1/1 PlayMode pass. Git LFS: not adopted. The packs are already in history, LFS isn't installed and it isn't needed at this size; revisit if big binaries become routine.
