# Collapse the content history into the current design

Status: done
Type: task
Blocked by: 02

## Why
(Ryan, 12 Nov: codebase review. Plan: https://claude.ai/artifact/AkXEjDN6dMgThVfQw6CWnG)

`DefaultContent.Create()` builds the original design and then patches it through 44 numbered steps (one step "replaces the recipes above"). There are no players' saves to protect yet, so the history is only cost.

## Acceptance criteria
- [x] `Create()` builds the current design directly, in readable sections (classes, moves, ingredients, monsters, areas, recipes, gear, kitchen, tuning)
- [x] The result is identical to today's content: a test compares old `Create()` output with the new one field by field before the old code is deleted
- [x] Content version resets to 1; the upgrade mechanism stays for future steps
- [x] `GameContent.asset` regenerated; Ryan's save still loads
- [x] Content that the slice and Design v2 cut for good is dropped, not carried
- [x] All tests pass; play-checked; no gameplay or balance change

## Where
Core/DefaultContent*.cs, Content/GameContent.asset. **Ask Ryan first** which non-slice content (other classes, story chapters 2+) to keep.

## Comments

(Claude, 12 Nov) Done. Kept all content, as Ryan asked (Warden, Ranger, Lamplighter, Emberfist, story chapters, side dungeons, mutators, still hidden by the slice gate). The 12 `DefaultContent*.cs` files (2,531 lines that built the original design and then patched it through 44 numbered steps) are replaced by `Scripts/Core/Content/`: `DefaultContent.Create.cs` (Create, Upgrade, two constants) plus five data files (Tuning, Classes, Food, World, Gear) that write the current design out directly, one `Add(new … { … })` per item, with only the fields that differ from a type's defaults. Those files were generated once by a new editor tool (`EditorTools/ContentCodegen`: `Dump` for exact comparisons, `Init/Method` for C#) and are now hand-edited source. **Verified identical** field by field (5,786 values) to both the old `Create()` and the live `GameContent.asset`. The one difference between those two: the asset had `enemiesPerRoomMax = 3`, while the old `Create()` said 4. The game plays the asset, so the baseline keeps **3**. Deviation from the ticket: the version continues at **45** (the collapsed baseline) instead of resetting to 1, so an older asset reads as older rather than newer. `Upgrade()` now replaces anything below 45 with the defaults, and future changes add one step each above it. The asset was upgraded to v45 with unchanged data. ClassLensTests' old upgrade-step test now checks that old content is reset to the current design. 175/175 tests pass; play-checked town, Frost Hollow and a Fog Wolf with no errors.
