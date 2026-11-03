# Cut "lit = weak spot", Heat and the Darkness hazard

Status: done
Type: task

## Why
Light keeps its look and secrets, loses the overlapping combat rules.

## Acceptance criteria
- [x] `litDamageBonus` removed (or 0 and unused).
- [x] Heat meter (`AddHeat`, `HeatUpdate`, `HeatGlow`, Detonate's Heat cost) removed; Emberfist is out of the slice anyway.
- [x] Darkness no longer a `Hazard` for routes; depth darkness (`floorLight`) stays and extends past floor 3 (see ticket 12).
- [x] Light-shy / light-drawn kept as-is (flavour). Tests pass.

## Where (Assets/HearthHollow/Scripts)
DungeonSettings.cs, EnemyController.cs, PlayerController.cs, Core/Types.cs (Hazard), DefaultContent routes.

## Comments
- 3 Nov 2026 (Claude): Done.
  - "Lit = weak spot": `litDamageBonus` removed. Shadow creatures still take less in the dark and more when lit.
  - Heat: the meter, its damage bonus, its light growth, embers, HUD text and settings are gone.
    Detonate is now a fixed-size blast (what 60 Heat used to give); Supernova and Cinder Step just do their hit.
  - Darkness hazard: no longer applied (the world-light cut and shrunken carried light are gone); `Session.DungeonLight`
    and the darkness tuning removed. The `Hazard.Darkness` enum value stays so saved data loads; glowcap's
    guard and the Firefly Jar ward are now inert until ticket 08 reworks accelerators into elements.
  - Content v25 (`DefaultContentDesignV2.cs`): routes lose Darkness (Hollow Heart, Glowcap Grotto); move and
    class text loses Boost / shield-pip / Heat lines. Runs on fresh content too. Asset upgraded and saved.
  - Depth darkness (`floorLight`) untouched; it already uses its last value past floor 3 (ticket 12 extends it).
  - New test `DesignV2_CutSystemsAreGoneFromContent` (it caught fresh content still getting Darkness - fixed).
  - Checked: clean compile; 39/39 EditMode tests; in play, all moves fired including Detonate, Cinder Step
    and Supernova, no console errors. Save backed up and restored.
