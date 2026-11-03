# Element system core

Status: done
Type: task
Blocked by: 01

## Why
Elements replace hit types: weakness ×1.5, resistance ×0.5.

## Acceptance criteria
- [x] Pure C# in Core: `Element { None, Fire, Frost, Nature, Water, Light }`, a chart (Fire>Frost>Nature>Water>Fire; Light only vs Shadow), and `Elements.Multiplier(attack, target)`.
- [x] Enemy profiles get `weak[]` and `resist[]`; moves can carry an element; the player has a run element (from meals, ticket 08) applied to attacks.
- [x] Damage uses the multiplier; shadow creatures are weak to Light.
- [x] Unit tests for the chart and multipliers.

## Where (Assets/HearthHollow/Scripts)
New Core/Elements.cs; EnemyProfile in DungeonSettings; EnemyController.TakeHit; PlayerController damage path.

## Comments
- 3 Nov 2026 (Claude): Done.
  - `Core/Elements.cs`: `Element { None, Fire, Frost, Nature, Water, Light }`, the loop (Fire > Frost > Nature >
    Water > Fire; Light only vs Shadow), `Multiplier` (weak ×1.5, resist ×0.5), `Defaults` (weak to what beats
    it, resists itself), names and emoji.
  - Moves: `MoveData.element` / `element2` (combo finisher). Content v26 maps the old hit types: Fire and Light
    keep theirs, physical hits get None. 29 moves carry an element.
  - Run infusion: `RunState.element` / `GameSession.RunElement` (set by ticket 08's meals). A move without its
    own element uses the infusion; a move with one keeps it.
  - Enemies: `EnemyProfile.element` + optional `weakTo` / `resists` lists (empty = the element's defaults).
    Set in DungeonSettings: slime and boar Nature, bat (Lantern Bee) and dragon Fire, crab (Marsh Viper) and
    shaman Water, wolf Frost. Shadow creatures always add a Light weakness.
  - Damage: `EnemyController.TakeHit(..., element)` applies the multiplier and pops "🔥 Weak!" / "Resists".
    Both damage paths (melee `Hit` and `Projectile`) pass the element. Non-damage touches (Flare, braziers,
    Sunshield) call `ElementContact(element)`, an empty hook for ticket 07's discovery.
  - Prism now strikes with the target's first weakness.
  - Cleanup: the HitType runtime plumbing became Element, and the dead shield-pip parameters from ticket 01
    are gone (Hit, SetHit, ComboHit, Slam, FlareBurst, HoverLantern, Projectile).
  - Not yet: no element/weakness icons above enemies (ticket 07), no element on the HUD for the infusion (08).
  - Tests: `ElementsTests` (5). 49/49 pass. In play: a 10-damage hit on a Fog Wolf did 15 Fire / 5 Frost /
    10 none / 10 Water; a Frost infusion turned the pan swing Frost while Flambé stayed Fire; all 49 moves and
    Flare fired with no console errors. Save backed up and restored.
