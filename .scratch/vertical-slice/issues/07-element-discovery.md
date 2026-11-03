# Hidden weaknesses and discovery

Status: ready-for-agent
Type: task
Blocked by: 06

## Why
An enemy's weaknesses/resistances start hidden; learn them by hitting with that element or being told.

## Acceptance criteria
- [ ] `Profile.knownElements[monsterId]` persists what's known (save migration).
- [ ] Hitting an enemy with an element reveals whether it's weak/resistant/neutral to it; a story hook can reveal one directly (used by ticket 15).
- [ ] Known weak/resist show as small icons above the enemy; unknown shows nothing.
- [ ] Tests for reveal rules.

## Where (Assets/HearthHollow/Scripts)
Core (Profile), EnemyController, enemy UI (reuse the old pip anchor).

## Comments
