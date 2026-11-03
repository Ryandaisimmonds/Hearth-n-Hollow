# Larger UI text for easier reading

Status: needs-triage
Type: task
Blocked by: none

## Why
(Ryan, 3 Nov) Everything should be larger and much easier to read.

## Acceptance criteria
- [ ] Raise the base font sizes across the UI (camp/town menus, cooking, route board, Skills tab, dungeon HUD, overlays) so text reads comfortably at normal play distance.
- [ ] Small/secondary text (`small`, `muted`, meal-card buffs, tray sub-lines, tooltips) gets the biggest bump.
- [ ] Cards, chips and slots grow to fit, with no clipped or overlapping text at 1080p and above.
- [ ] Ideally one place to tune it (a UI scale or text-size setting in Settings).

## Where (Assets/HearthHollow)
UI/HearthHollow.uss (font sizes, card sizes), the PanelSettings scale, UI/SettingsOverlay.cs (optional text-size slider).

## Comments
