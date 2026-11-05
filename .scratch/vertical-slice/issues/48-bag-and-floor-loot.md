# The bag: fewer drops, loot on the floor, swap box, drop with X

Status: done
Type: task
Blocked by: 47

## Why
(Playtest, 5 Nov) The bag is full by floor 2–3 and you get more than you need. If you can't carry it, it should drop on the ground, and you should be able to drop loot you don't want.

## Decisions (grilling, 5 Nov)
- Bag size unchanged (it's set by the bag upgrades: 6 slots to start, not 15 as first written); monster drops about 40% fewer (the Backpack stays a later upgrade).
- With a full bag, loot lands on the floor (glowing) and stays until you leave that floor.
- Pressing E on floor loot opens the bag with a "holding" box: drag it onto a slot to swap, or move with WASD and press E. The swapped-out item goes on the floor.
- Closing the bag without swapping puts the held item back on the floor where it was.
- In the bag, X (or dragging an item out) drops it at your feet.

## Acceptance criteria
- [x] Drop rates trimmed
- [x] Floor loot that persists per floor
- [x] Holding box with drag and keyboard swap
- [x] X / drag-out to drop
- [x] Tests for the bag rules

## Comments
- (Claude, 5 Nov) Done (content v42).
  - Drops: monster ingredient drops 35% → 21%, building materials 15% (+10%/floor) → 9% (+6%/floor).
  - Floor loot lives in the run (`Core/FloorLoot.cs`: `FloorItem`, `RunState.floorLoot`). Overflow from a kill lands where the monster fell; anything else (forage, chests, X) at your feet. It's cleared when you take the ladder.
  - Drawn by `Dungeon/FloorLootView.cs` as a glowing, bobbing pouch with an emoji tag above it (e.g. "🌿 ×5"), since the ingredients have no sprites yet; the prompt reads "E Pick up 🌿 Wild Herbs ×5".
  - E picks it up: straight in if it fits (partly, if only some fits). With a full bag, the bag opens with a "Holding …" box. Click or drag it onto a slot, or move with WASD/arrows and press E, to swap; the swapped-out stack lies where the held one was. Tab/Esc leaves it on the floor.
  - In the bag: drag stacks between slots; right-click, X, or drag a stack out of the bag to drop it at your feet. Discard no longer deletes anything in the dungeon.
  - Tests: `FloorLootTests` (6). 153/153 pass. Play-checked: pouch + tag + prompt, the holding box, keyboard swap (the old stack landed on the floor).
