# Meal and stat rework: flat + %, big quality, typed tray slots

Status: done
Type: task
Blocked by: 43

## Why
(Ryan & playtest, 5 Nov) A meal's +33% attack is only 1 point. A good meal should make you kick arse; a normal one should feel normal. Food should roughly double you, and it has to keep making sense as the game gets harder.

## Decisions (grilling, 5 Nov)
- Scaling model: every area has a tier; its monsters are tuned to it. Tier-N meals put you on par for tier N+1. Meals get stronger mostly through recipe tiers (new ingredients); kitchen level is a small extra (wider cooking-bar zones, ticket 45).
- Rule of thumb: hungry = you lose; Plain tray of the right tier = fair; Prime = you dominate; a tier below = a struggle.
- Stats: flat for attack, HP and armour; % for attack%, HP%, speed, crit, gathering, coins.
- Combo maths: all flat bonuses added to the base first, then all % summed and applied once (base 3 +2 = 5, +50% = 7.5). % add together, never multiply.
- Quality multiplies each meal's own numbers: Plain ×1, Fine ×1.5, Prime ×2 (was ×1 / ×1.1 / ×1.2).
- Tier-1 meals give one stat; later tiers give two.
- Tray slots are typed: 🗡️ Attack, ❤️ Health, ✨ Free. The first two need a meal of that type; Free takes anything. A two-stat meal fits either slot it covers.
- Camp Bread fits any slot but gives very little (+3 HP, or +1 attack in the Attack slot).

## Starting numbers (tune after the next playtest)
- [x] Tier-1 Plain: about +2 attack, or +8 HP, or +25% attack
- [x] Three Plain meals ≈ double your power; three Prime ≈ triple
- [x] Meadow Hollow tuned so a Plain tray is fair and hungry loses by floor 2
- [x] Frost Hollow tuned so a Fine tier-1 tray with a Fire infusion is fair

## Acceptance criteria
- [x] Every slice recipe converted to one stat (tier 1) or two (tier 2)
- [x] Stat calc: flat then %, as above; tray shows "Attack 3 → 5 → 7.5"
- [x] Quality ×1 / ×1.5 / ×2
- [x] Typed tray slots on the Route board, with Camp Bread allowed anywhere
- [x] The 3-meal gate (ticket 40) still works with typed slots
- [x] Content upgrade step; tests

## Comments
- (Claude, 5 Nov) Done. New stats `Attack` (flat) and `MaxHpPct`; StatCalc adds flat first, then all % once. Quality ×1 / ×1.5 / ×2. Every recipe reworked to one stat (tier 1) or two (tier 2): Pot = ❤️, BBQ = 🗡️, Sandwich = ✨ utility (content v40). Typed slots are a rule, not positions (`Core/Tray.cs`): the tray needs an Attack and a Health meal; the Free slot takes anything; pantry chips that would block it are greyed with the reason. Camp Bread fits anywhere (+1 attack in 🗡️, +3 HP elsewhere). Route board shows the three slots and "Attack 4 → 8.4 · HP 14 → 27". Healing now comes from Berry Porridge, the tier-2 stew and clearing rooms (bread and Herb Stew lost their Regen). Monster numbers unchanged; tune them after ticket 47's area reshape. Tests: `TrayTests` (7), CookingTests and HealingTests updated. 136/136 pass.
