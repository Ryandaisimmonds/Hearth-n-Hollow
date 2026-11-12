# Tidy the third-party packs

Status: ready-for-human
Type: task

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

Third-party packs are 250 MB of the 275 MB in `Assets` and about 13,000 tracked files. Layer Lab and the monster pack sit at the top level with the used parts also copied under `HearthHollow/Art/ThirdParty`.

## Acceptance criteria
- [ ] Ryan decides: delete unused pack content, or keep it all? Git LFS or not?
- [ ] All packs live under one `Assets/ThirdParty` folder; no duplicates
- [ ] Art Library, Audio Library and the dungeon kit still resolve every asset (rebuild them and check for missing)
- [ ] All tests pass; play-checked; no gameplay or balance change

## Where
Assets/*, Scripts/Editor/*Builder.cs. Moving packs changes many paths in one commit; do it on a quiet day.

## Comments
