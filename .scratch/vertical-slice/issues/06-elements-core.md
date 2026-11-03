# Element system core

Status: ready-for-agent
Type: task
Blocked by: 01

## Why
Elements replace hit types: weakness ×1.5, resistance ×0.5.

## Acceptance criteria
- [ ] Pure C# in Core: `Element { None, Fire, Frost, Nature, Water, Light }`, a chart (Fire>Frost>Nature>Water>Fire; Light only vs Shadow), and `Elements.Multiplier(attack, target)`.
- [ ] Enemy profiles get `weak[]` and `resist[]`; moves can carry an element; the player has a run element (from meals, ticket 08) applied to attacks.
- [ ] Damage uses the multiplier; shadow creatures are weak to Light.
- [ ] Unit tests for the chart and multipliers.

## Where (Assets/HearthHollow/Scripts)
New Core/Elements.cs; EnemyProfile in DungeonSettings; EnemyController.TakeHit; PlayerController damage path.

## Comments
