# Shared utility gear and slice trinkets

Status: done
Type: task
Blocked by: 04

## Why
Gear is utility, shared across classes.

## Acceptance criteria
- [x] Gear slots: bag (carry more), boots (speed; first boots unlock the dash), lantern (light reach). Weapon element is out of scope for the slice.
- [x] No per-class tiers; one shared set (save migration).
- [x] 3–4 trinkets that bend rules for fun (e.g. Flare recharges on kills).
- [x] Smithy shows the new gear.

## Where (Assets/HearthHollow/Scripts)
Core/Gear.cs, DefaultContentGear.cs, UI (Upgrades), PlayerController speed/dash.

## Comments

- (Claude, 3 Nov) Done. The smithy sells three shared tracks (`Gear.Shop`): **Boots** (speed; the first pair unlocks the dash), **Lantern** (new, content v33: Tallow Stub → Hearthglow Lantern, +15–60% carried-light reach) and **Bag**. Tiers live on `Profile` (`bootsTier`, `lanternTier`, `bagTier`). Weapon / body / head stay at their first tier; buying them is refused. saveVersion 12 (`Gear.ShareGear`) refunds every weapon / armour / helmet purchase plus the extra boots (keeps the best pair any class had); the smithy shows the refund once. Slice trinkets (`TrinketData.slice`, the only ones sold / dropped while Slice.On): **Ember Locket** (each kill −3 s Flare cooldown, 60), **Magpie Spoon** (+20% ingredient drops, 90), **Pocket Hearth** (once a run, a killing blow leaves you on 1 HP, 140), **Honey Locket** (heal 1 per kill, 100). The opening arc's pit cache now gives a lantern tier. Tests: `UtilityGearTests` (103/103 pass).
- Note for Ryan's save: 835 coins refunded; damage back to the base 4, armour 0, no helmet HP.
