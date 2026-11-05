# The opening fall, polished: tutorial, visible fog, the fog room, a fade

Status: done
Type: task
Blocked by: 43

## Why
(Ryan, 5 Nov) The fall works but needs to feel intended, not unfair: the player should feel capable first, see the place turn on them, and lose to something that isn't a cheap hit.

## Decisions (grilling, 5 Nov)
- 6 rooms: rooms 1–2 the tutorial, 3–5 the visible ramp, 6 the fog room.
- Room 1: one slime, prompts for moving (WASD) and attacking (click). Room 2: two slimes in a darker room, prompts for your skill (right-click) and Flare (F). Prompts fade once you've done each action.
- Locked-skill hints hidden during the fall.
- Visible escalation from room 3: the light dims, the purple deepens, the fog thickens.
- Room 6 has no monster: the fog pours in, the screen fills and your HP drains until you fall.
- No loot in the fall.
- The first death skips "You fall…" and the run summary: fade to black ("…the fog closes in"), wake by Wren's fire.
- "???" stays unexplained for now.

## Acceptance criteria
- [x] Room layout and monsters as above
- [x] Tutorial prompts
- [x] Escalating light and fog
- [x] Fog room
- [x] No drops; fade straight to town
- [x] Tests

## Comments
- (Claude, 5 Nov) Done (content v43).
  - Layout: the wake-up room + 6: two tutorial rooms, three rising ones, then the fog room. `RoomNode.fog` marks the last; there's no exit any more.
  - Tutorial: room 1 is one slime, room 2 two slimes in a dimmer room. The line reads "🕹️ WASD to move" → "🗡️ Click to attack" (J on keys) → "✨ Right-click for your skill" → "🔆 F for Flare", each going once you've done it. The ramp (`DungeonRules.FirstDiveRamp`) starts at room 3.
  - Escalation: from room 3 the room light dims (down to half) and a purple fog veil thickens (`DungeonHud.Fog01`, up to 40% before the fog room).
  - The fog room: no monsters, doors shut, "🌫️ The fog pours in…", the veil fills the screen and you lose 12% of max HP every half second (armour doesn't help) until you fall.
  - No loot in the fall: no coins, XP, drops, materials, trinkets or hidden secrets. The locked-skill hint line is hidden.
  - The end: "…the fog closes in" instead of "You fall…"; no run summary. A fade to black with that line, then you wake in town (`GameSession.FallEnding`, `GameUI.FadeOutOfTheFall`) and Wren comes over.
  - Tests: TheFallTests updated (tutorial rooms have no ramp, the line ends in the fog) + `TheFall_HasNoLoot_AndEndsInAFade_NotASummary`. 154/154 pass. Play-checked from a fresh save: banner + "WASD to move", room 1 one 5 HP slime, room 4 purple and dark with three faster monsters, the fog room, the fade, waking to Wren.
