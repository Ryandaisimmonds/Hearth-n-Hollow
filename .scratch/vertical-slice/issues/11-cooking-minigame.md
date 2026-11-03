# Short optional cooking minigame

Status: ready-for-agent
Type: task
Blocked by: 08

## Why
Extra effort gives a small quality bonus; skipping never punishes.

## Acceptance criteria
- [ ] A 1–2 s timing tap in the stove pop-up nudges the quality roll; a Quick cook button skips it.
- [ ] Never affects tier. Keyboard, mouse and gamepad. Tunable in GameContent.

## Where (Assets/HearthHollow/Scripts)
UI/CookOverlay.cs, Core/Cooking.cs (roll input).

## Comments
