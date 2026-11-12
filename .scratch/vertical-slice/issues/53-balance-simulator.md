# Balance simulator and report

Status: done
Type: task
Blocked by: 52

## Why
(Ryan, 12 Nov: "run with all the above" while he tests the slice.) A test that plays fights in code (your cook + a tray vs each area's monsters, floor by floor) and writes a report: time to clear, HP left, deaths, for hungry / Plain / Fine / Prime trays. So playtests check numbers instead of guessing.

## Acceptance criteria
- [x] Headless fight sim using the real stats, monster stats, speeds and room counts
- [x] Trays: hungry, Plain, Fine, Prime (and an infused Fire tray vs Frost Hollow)
- [x] Report per area and floor; saved as a Markdown file in the repo
- [x] Targets from the plan flagged (hungry loses by Meadow floor 2, Plain is fair, Prime dominates)
- [x] Tests where it makes sense; play-checked; no balance changes beyond what's noted

## Comments

(Claude, 12 Nov) Done. `Tests/Editor/BalanceSimulator.cs` (`BalanceSim` + `BalanceSimTests.WriteTheBalanceReport`) plays 300 runs per tray per area from the real numbers and writes `.scratch/vertical-slice/balance-report.md`. Re-run that one test after any tuning change. Added a "Fine + Fire + Ginger" tray, since ginger is the Cold counter.

Findings (no tuning done):
- **Hungry**: dies on Meadow floor 2 (95%). On target.
- **Plain**: walks floors 1–2, then 66% die at the Meadow boss. Harsher than "a fair fight"; the boss is the whole difficulty.
- **Fine 6% / Prime 0%** deaths in Meadow. Prime dominates. On target.
- **Chili Ridge**: too easy. Anything fed gets through untouched (0% deaths, ~100% HP).
- **Frost Hollow**: 100% deaths for every tray, even Prime and Fine + Fire + Ginger. Ginger helps floor 1 (26% vs 59% die), but floor 2 and the boss still kill. That's the biggest balance problem, unless the playtest shows real players do much better than the sim's assumptions (starting gear, no skill ranks).
- **Fire on the skewer** made Meadow *worse* (20% vs 6%) for the same stats. Worth checking which Meadow monsters resist fire.
