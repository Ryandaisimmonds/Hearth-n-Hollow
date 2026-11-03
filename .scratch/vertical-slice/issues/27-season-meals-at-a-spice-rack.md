# Add the element ingredient after cooking, at a spice rack

Status: done
Type: task
Blocked by: 08

## Why
(Ryan, 3 Nov, playtest) Adding the accelerator (element slot) while cooking is too many options at once, and it's harder to see the specific bonus. Do it afterwards, at another workbench.

## Acceptance criteria
- [x] The stove only cooks: no element slot there.
- [x] A spice rack by the cooking area: pick a cooked meal, then an element ingredient; the effect is spelled out large (infuse: your attacks become Fire this run; guard: Frost hits you for half).
- [x] One element ingredient per meal; it's spent when you season; a seasoned meal's card shows the element (colour tint, ticket 09).
- [x] Opens with the kitchen (opening arc).

## Comments
- (Claude, 3 Nov) Done. The stove no longer has the element slot (it points you to the spice rack). New town spot **🧂 Spice rack** beside the cooking area (opens with the kitchen) → `SeasonOverlay`: 1) pick an unseasoned cooked meal (pantry or tray), 2) pick an element ingredient; a big tinted panel spells out the effect ("🔥 Infuses Fire — your attacks… hit as Fire; strong against ❄️ Frost monsters"); 🧂 Season spends one. `Cooking.Season` / `GameSession.SeasonMeal` keep the one-infusion-per-tray rule. Tests: `SeasoningTests` (115/115 pass).
