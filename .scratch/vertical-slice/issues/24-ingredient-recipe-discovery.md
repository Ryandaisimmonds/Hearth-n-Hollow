# Finding an ingredient reveals its recipes

Status: done
Type: task
Blocked by: none

## Why
(Ryan, 3 Nov, playtest) When you get an ingredient you should unlock the recipes that contain it; ingredients you haven't found yet show as ?.

## Acceptance criteria
- [x] Profile remembers every ingredient you've ever had (pantry or bag).
- [x] A recipe shows once you've found any of its ingredients (and all of them can be found in the slice).
- [x] Ingredients not found yet show as ❓ on the recipe row and at the stove.

## Comments
- (Claude, 3 Nov) Done. `Profile.seenIngredients` (saveVersion 13 seeds it from the pantry and cooked meals). Any ingredient you get (pantry, bag, gift) is remembered; a recipe shows once you've found one of its ingredients, if all of them can be found in the slice. Unknown ingredients show as ❓ on recipe rows, tooltips and the stove. First time an ingredient lands in your bag: "📖 New ingredient: … — new recipes to try".
