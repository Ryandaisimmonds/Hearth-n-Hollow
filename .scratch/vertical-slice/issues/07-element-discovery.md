# Hidden weaknesses and discovery

Status: done
Type: task
Blocked by: 06

## Why
An enemy's weaknesses/resistances start hidden; learn them by hitting with that element or being told.

## Acceptance criteria
- [x] `Profile.knownElements[monsterId]` persists what's known (save migration).
- [x] Hitting an enemy with an element reveals whether it's weak/resistant/neutral to it; a story hook can reveal one directly (used by ticket 15).
- [x] Known weak/resist show as small icons above the enemy; unknown shows nothing.
- [x] Tests for reveal rules.

## Where (Assets/HearthHollow/Scripts)
Core (Profile), EnemyController, enemy UI (reuse the old pip anchor).

## Comments
- 3 Nov 2026 (Claude): Done.
  - `Core/Discovery.cs`: `Profile.knownElements` (list of monsterId + element), `Knows`, `Learn`, `KnownOf`.
    `GameSession.DiscoverElement(monsterId, element)` learns it and saves straight away;
    `KnowsElement` reads it. saveVersion 9 (the list starts empty).
  - What teaches you: any hit with an element (melee or projectile) and any element touch with no damage
    (Flare's Light, a brazier's fire, Sunshield). Knowledge is per kind of monster and kept forever; neutral
    results are learned too (so you know not to bother). Story hook for ticket 15: `DiscoverElement`.
  - Shadow creatures always show a Light diamond (a rule, not a per-kind fact).
  - Over the head: one diamond per known weakness in the element's colour, one faded square per known
    resistance. They fade with the shadow silhouette, and every monster of the kind redraws on a discovery
    (`EnemyController.DiscoveryVersion`). First discovery pops "🔥 Weakness found!" / "Resistance found".
  - Not built: a bestiary screen (out of the slice). Icon size is small (0.11 world units); easy to tune.
  - Tests: `DiscoveryTests` (4, incl. save and load). 53/53 pass. In play: two Fog Wolves showed nothing,
    then a Fire hit gave both a Fire diamond, a Frost hit added a Frost square, a Water hit added nothing;
    screenshot checked. No console errors. Save backed up and restored.
