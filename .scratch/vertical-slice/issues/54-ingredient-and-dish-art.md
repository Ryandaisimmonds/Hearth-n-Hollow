# Pixel art for every ingredient and dish

Status: done
Type: task
Blocked by: 53

## Why
(Ryan, 12 Nov: "run with all the above" while he tests the slice.) None of the 22 ingredients or 34 dishes has art; everything falls back to emoji. Draw them in code like the element icons and register them in the Art Library so every screen picks them up (pantry, tray, stove, bag, floor loot).

## Acceptance criteria
- [x] 22 ingredient icons
- [x] Dish icons (at least every slice recipe; the rest by station/base shape)
- [x] Registered so ArtLibrary.GetIngredient / GetRecipe return them
- [x] Floor loot shows the ingredient instead of the pouch
- [x] Tests where it makes sense; play-checked; no balance changes beyond what's noted

## Comments

(Claude, 12 Nov) Done. All 25 ingredients (including the 5 materials) and all 24 dishes are drawn as 16×16 pixel sprites. `Tools/FoodArt/food_art.py` holds the letter maps and palettes; it writes `Scripts/UI/FoodArtData.cs` and a preview sheet, `Tools/FoodArt/food_sheet.png`. `FoodArt.Install` (called from GameBootstrap) makes `ArtLibrary.GetIngredient/GetRecipe` fall back to the drawn art, so a hand-made sprite in the Art Library still wins. A dish with no drawing of its own borrows its station's look (bowl, skewer or sandwich). Tier-2 dishes and Golden Wheat get a little sparkle. Floor loot shows the ingredient instead of the pouch, and its tag now shows the name. Tests: FoodArtTests (158/158). Play-checked the larder and the cooking area.
