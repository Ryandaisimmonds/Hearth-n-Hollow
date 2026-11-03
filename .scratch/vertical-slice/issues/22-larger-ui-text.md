# Larger UI text for easier reading

Status: done
Type: task
Blocked by: none

## Why
(Ryan, 3 Nov) Everything should be larger and much easier to read.

## Acceptance criteria
- [x] Raise the base font sizes across the UI (camp/town menus, cooking, route board, Skills tab, dungeon HUD, overlays) so text reads comfortably at normal play distance.
- [x] Small/secondary text (`small`, `muted`, meal-card buffs, tray sub-lines, tooltips) gets the biggest bump.
- [x] Cards, chips and slots grow to fit, with no clipped or overlapping text at 1080p and above.
- [x] Ideally one place to tune it (a UI scale or text-size setting in Settings).

## Where (Assets/HearthHollow)
UI/HearthHollow.uss (font sizes, card sizes), the PanelSettings scale, UI/SettingsOverlay.cs (optional text-size slider).

## Comments
- (Claude, 3 Nov) Done. Settings → Display → **Text size**: Normal / Large / Extra large (×1, ×1.15, ×1.3 of the whole UI; default Large; remembered in PlayerPrefs `hh_text_size`). It scales a runtime copy of the PanelSettings (smaller reference resolution), so the asset isn't edited. Small text was also bumped in the stylesheet (9→11, 10→12, 11→13, 12→13 px). The Settings menu now scrolls below its title so big sizes never push it off screen. Checked the cooking area, route board and the in-dungeon menu at Large and Extra large. Also: the route board says "Back to town" in the slice.
