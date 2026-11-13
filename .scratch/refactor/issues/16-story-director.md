# Move the story logic out of GameSession

Status: done
Type: task
Blocked by: 02

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`GameSession` is spread over 11 partial files with about 190 public members. The story (Old Wren's thread, the arc, the fall) is the most self-contained part.

## Acceptance criteria
- [x] A `Story` class owns Wren's steps, the arc and the fall; `GameSession` forwards the few calls the UI needs
- [x] WrenStoryTests, TheFallTests and SliceAutoplayTests pass unchanged (apart from call sites)
- [x] All tests pass; play-checked; no gameplay or balance change

## Where
Core/WrenStory.cs, Core/TheFall.cs, Core/GameSessionArc.cs, Core/GameSessionStory.cs.

## Comments

(Claude, 12 Nov) Done. New Core/Story.cs owns the opening arc, Old Wren's thread (steps, talk, objective, set-out block, Fire hint, Frost Hollow) and the opening fall, plus their save upgrades. GameSession owns one (Session.Story), calls five hooks (OnRunEnd, OnHome, OnCooked, OnBoss, OnSeasoned) and forwards only Wren, InFall, FallRoute, AreaOpen, DashUnlocked, Arc and ArcStage. WrenStory.cs, TheFall.cs and GameSessionArc.cs are gone; GameSessionStory.cs is now the forwarders plus TrayBlock and BuildLampMaker. Call sites moved to Session.Story.X (TalkDue, Talk, Objective, SetOutBlock, FireHintDue, GiveFireHint, BeginFall, FallFogBite, ...); test bodies unchanged apart from call sites. 177/177 tests pass. Play-checked a brand-new game: opens in the fall, the fog drops you, it fades to town and Wren finds you; Ryan's save restored.
