# Town as the hub for the slice

Status: done
Type: task
Blocked by: 05, 16

## Why
The town is where the payoff lives.

## Acceptance criteria
- [x] Hearth, kitchen, larder, smithy, Route board and Old Wren work as the hub; the old camp screen isn't used in the slice flow.
- [x] One upgrade area the player chooses to build and pays for with biome materials: the Lamp-maker (Ryan, 3 Nov). It upgrades the lantern's light reach and Flare (radius, cooldown); a few stages, built on one of the fenced plots.
- [x] Runs start only at the Route board; you return to the town.

## Where (Assets/HearthHollow/Scripts)
Scripts/Town/*, GameUI.cs, HearthScreen station mode.

## Comments

- (Claude, 3 Nov) Done. Hub: in the slice you're always back in the town when a run ends (GameUI.OnChanged), the camp screen's signpost is gone (ticket 05), station windows have no Set out, so runs start only at the Route board by the gate. **Lamp-maker** (content v34, `Core/LampMaker.cs`, `Town/TownLampMaker.cs`): the NE fenced plot shows "Empty plot — Build the Lamp-maker"; talking there opens a dialogue with the next stage, its cost (with what you have) and a 🔨 Build button. Stages: Lamp-maker's stall (30 + 6 timber: +10% light, Flare +10% wider), Lamp-maker's workshop (80 + 10 timber + 4 ember coal: +20%, +20%, Flare 3 s quicker), Lighthouse workshop (160 + 12 timber + 8 ember coal: +35%, +35%, 6 s quicker). Each stage draws a bigger building (stall → stone workshop with a glass kiln → a lighthouse lamp on top). Light reach stacks with the lantern (`Gear.LightReach`); Flare radius / cooldown in DungeonController. Town labels now follow renamed spots. Tests: `LampMakerTests` (107/107 pass).
