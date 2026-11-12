# Boss attack warnings

Status: done
Type: task
Blocked by: 62

## Why
(Ryan, 12 Nov: "run with all the above" while he tests the slice.) Red ground markers before big boss attacks so dying feels fair.

## Acceptance criteria
- [x] Telegraph markers before boss charges/slams
- [x] Tests where it makes sense; play-checked; no balance changes beyond what's noted

## Comments

(Claude, 12 Nov) Done. Added `Dungeon/Telegraph`: red floor markers that fill up over a boss's windup and flash as the attack lands. A boss charge (Charger/Flyer/Brute) shows a lane as long as the charge carries; a Brute's five-shot breath shows a fan of five rays (it now decides breath vs charge at windup, so the marker is right); a caster shows a lane; a chaser's bite shows a disc. Markers are unlit so they read in dark boss rooms, and they vanish if the boss is interrupted or dies. **⚠ Two small fairness changes for Ryan's playtest:** a boss now attacks along the direction it showed (aimed at windup instead of re-aimed at release), and a boss always winds up for at least 0.6 s (`BossTelegraphMin`; the Bramble Sow's is already 0.6). Normal monsters are unchanged. Test: BossTelegraphTests (167/167). Play-checked on the Bramble Sow (lane, fan and disc in the screenshot).
