# Remove levels, XP and skill trees

Status: ready-for-agent
Type: task

## Why
Design v2: no level grind or stat bonuses; skills come from class arcs (ticket 10).

## Acceptance criteria
- [ ] XP, levels 1–50, level-up stat gains, skill points and the 21-node trees no longer exist in play; Skills tab shows the loadout only (filled in by ticket 10).
- [ ] Save migration (`saveVersion` +1) drops level/XP/tree data cleanly; a fresh save starts correctly.
- [ ] Run summary and HUD no longer show XP/levels. Dev panel level buttons removed.
- [ ] Tests updated and passing.

## Where (Assets/HearthHollow/Scripts)
Core/Progression.cs, GameSessionSkills.cs, Economy.cs, StatCalc.cs, UI/SkillsTab.cs, RunSummaryScreen.cs, SettingsOverlay.cs.

## Comments
