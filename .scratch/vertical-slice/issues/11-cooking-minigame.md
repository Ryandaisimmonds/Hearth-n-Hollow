# Short optional cooking minigame

Status: done
Type: task
Blocked by: 08

## Why
Extra effort gives a small quality bonus; skipping never punishes.

## Acceptance criteria
- [x] A 1–2 s timing tap in the stove pop-up nudges the quality roll; a Quick cook button skips it.
- [x] Never affects tier. Keyboard, mouse and gamepad. Tunable in GameContent.

## Where (Assets/HearthHollow/Scripts)
UI/CookOverlay.cs, Core/Cooking.cs (roll input).

## Comments

- (Claude, 3 Nov) Done. **Cook** now plays a timing tap: a heat marker sweeps once across a bar in 1.6 s; stop it in the gold (full nudge: Fine +15%, Prime +10%) or the warm zone (half); missing or letting it run off the end cooks at normal odds. **Quick cook** skips it. Keys: Enter / Space = cook / tap, Q = quick cook, Esc cancels; gamepad A / X / B; click the bar works too. Only `CookChoice.qualityNudge` changes, so the dish and tier never do. Tunable in `TuningData` (`cookGameSeconds`, `cookGameTarget`, `cookGamePerfect`, `cookGameGood`, `cookGameGoodNudge`). The stove shows the perfect-timing odds. Tests: `CookGameTests` (78/78 pass).
