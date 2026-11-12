# Meals: every dish gives attack and health, plus a special

Status: done
Type: task
Blocked by: 51

## Why
(Ryan, 5 Nov) It's hard to tell which meal is attack and which is health, and which way to go. Make every meal give a health and attack bonus, with the "special stat" as what people pay attention to: eat whatever you want; the better the meal, the better the base; extra % on top depending on what you eat.

## Decisions (5 Nov)
- Q1: every meal gives both 🗡️ attack and ❤️ HP, with a small tilt by station: BBQ a bit more attack, Pot a bit more HP, Sandwich even. Tier 1 Plain: BBQ +2 🗡️ +3 ❤️ · Pot +1 🗡️ +6 ❤️ · Sandwich +1 🗡️ +4 ❤️. Tier 2 ×1.5; Camp Bread half a Sandwich. Quality (×1 / ×1.5 / ×2) scales everything.
- Q2: the special by station: 🍲 Pot = healing / defence (+% HP, HP over time, HP per kill, armour), 🔥 BBQ = fighting (+% attack, crit), 🥪 Sandwich = utility (speed, gathering, coins). One special at tier 1, two at tier 2.
- Q3: the tray is back to any three meals (ticket 44's typed slots go). Repeats of the same dish still count less.
- Q4: each meal shows its station icon; its line reads the special first, in bold, then the base. Colour stays for elements.

## Acceptance criteria
- [x] `Tray.Base` by station and tier; recipe buffs hold only the special (content v44)
- [x] Typed slots removed; tray accepts any three
- [x] Meal lines: "<b>+20% attack</b> · 🗡️ +2 ❤️ +3" on the Route board, stove, pantry and recipe list; station icon on tray rows and pantry chips
- [x] Recipe filter by special
- [x] Tests

## Comments
- (Claude, 12 Nov) Done (content v44). `Tray.Base`: BBQ +2 🗡️ +3 ❤️, Pot +1 🗡️ +6 ❤️, Sandwich +1 🗡️ +4 ❤️ at tier 1 Plain; tier 2 ×1.5; Camp Bread half a Sandwich, no special. Recipe buffs now hold only the special. Typed slots gone: any three meals. `UIKit.MealSummary` shows "<b>special</b> · 🗡️ +x ❤️ +y" everywhere meals are listed; tray rows and pantry chips carry 🍲 / 🔥 / 🥪; the recipe filter is by special. Tests: TrayTests rewritten, CookingTests / ClassLensTests updated. 155/155 pass. Play-checked: cooking card, recipe list, Route board tray. Note: Fine stew + Prime skewer + Plain butty = 4 → 16 attack, 14 → 46 HP. Monsters haven't been retuned since ticket 44, so expect it to feel easy: tune after the next playtest.
