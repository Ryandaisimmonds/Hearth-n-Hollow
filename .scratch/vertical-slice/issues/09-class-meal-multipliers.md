# Minimal base stats and class meal multipliers

Status: done
Type: task
Blocked by: 08

## Why
Power comes from food; classes are a lens on it.

## Acceptance criteria
- [x] Base stats are minimal (a Camp Bread run survives floor 1).
- [x] `ClassData` gets per-buff multipliers (strong ~×1.25, weak ~×0.8), replacing favourite station ×1.5. Cook: every meal ×1.1.
- [x] Stove and tray show the result for the current class ("+12 HP"), not the multiplier.
- [x] Tests for the stat calc.

- [x] (Ryan, 3 Nov) Meal cards (meal pantry, tray, route board) take the colour of their element-slot element, for easy reading.

## Where (Assets/HearthHollow/Scripts)
Core/StatCalc.cs, Types.cs (ClassData), Cooking.cs, UI/CookOverlay.cs, HearthScreen.cs.

## Comments

- (Claude, 3 Nov) Done. Content v28 (`Classes28`): every class starts on 14 HP, ×1 damage, ×1 gather (14 = the Cook's old base, which already survives floor 1 on Camp Bread). `ClassData.mealStatMults` is a per-stat lens: Warden health/armour ×1.25, speed ×0.8; Ranger speed/crit/gathering ×1.25, health ×0.8; Lamplighter heal-on-kill/coins ×1.25, armour ×0.8; Emberfist damage ×1.25, gathering ×0.8; Cook every meal ×1.1 (`mealBuffMult`). Favourite stations are gone (fields, ⭐ marks, filter chip). Meal rows, pantry, tray, stove and route board show the class's result ("+19.8 max HP"); the class card and camp header say "Gets more from … · less from …". Meal cards, tray slots and route-board rows are tinted in their element-slot element's colour (`UIKit.ElementTint`, USS `el-tint--*`). Tests: `ClassLensTests` (67/67 pass).
