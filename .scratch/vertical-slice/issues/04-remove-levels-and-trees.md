# Remove levels, XP and skill trees

Status: done
Type: task

## Why
Design v2: no level grind or stat bonuses; skills come from class arcs (ticket 10).

## Acceptance criteria
- [x] XP, levels 1–50, level-up stat gains, skill points and the 21-node trees no longer exist in play; Skills tab shows the loadout only (filled in by ticket 10).
- [x] Save migration (`saveVersion` +1) drops level/XP/tree data cleanly; a fresh save starts correctly.
- [x] Run summary and HUD no longer show XP/levels. Dev panel level buttons removed.
- [x] Tests updated and passing.

## Where (Assets/HearthHollow/Scripts)
Core/Progression.cs, GameSessionSkills.cs, Economy.cs, StatCalc.cs, UI/SkillsTab.cs, RunSummaryScreen.cs, SettingsOverlay.cs.

## Comments
- 3 Nov 2026 (Claude): Done.
  - Removed from play: XP, levels, level-up stat gains (HP and damage), skill points, skill trees, the respec,
    the level-gated L slot and skill borrowing. `ClassProgress` lost `level`, `xp`, `tree`; `TuningData` lost the
    level/XP/skill-point tuning; `RunSummary` lost its XP and level fields.
  - Skills tab is loadout-only (equipped moves + choices + details). For now every move a class owns can be
    equipped; ticket 10 gates them behind the class arc.
  - HUD, camp player panel, class select, route board, town HUD, run summary and class-swap dialogue no longer
    show levels or XP. Dev panel "+10 levels" removed; Max everything no longer buys trees.
  - Save: `saveVersion` 8. Old level/XP/tree fields are ignored on load and drop out on the next save.
    Ryan's save loaded and migrated cleanly in a test (then restored; it migrates on the next Play).
    A copy of the pre-wipe save is in persistentDataPath/claude-backup-ticket04-before-wipe/.
  - Left for later: content still carries `skillNodes`, route `clearXp`, kill XP and the meal `XpPct` buff,
    which now do nothing. Ticket 08 should repurpose or remove the XP meal buff; ticket 10 replaces the trees.
  - Tests: XP-curve test replaced by `Loadout_NoLevels_EveryOwnSkillCanGoInEitherSkillSlot`; Max-everything
    test no longer checks levels. 39/39 pass. In play: camp, Skills tab and a run to the summary screen with
    no console errors.
