# Minimal base stats and class meal multipliers

Status: ready-for-agent
Type: task
Blocked by: 08

## Why
Power comes from food; classes are a lens on it.

## Acceptance criteria
- [ ] Base stats are minimal (a Camp Bread run survives floor 1).
- [ ] `ClassData` gets per-buff multipliers (strong ~×1.25, weak ~×0.8), replacing favourite station ×1.5. Cook: every meal ×1.1.
- [ ] Stove and tray show the result for the current class ("+12 HP"), not the multiplier.
- [ ] Tests for the stat calc.

## Where (Assets/HearthHollow/Scripts)
Core/StatCalc.cs, Types.cs (ClassData), Cooking.cs, UI/CookOverlay.cs, HearthScreen.cs.

## Comments
