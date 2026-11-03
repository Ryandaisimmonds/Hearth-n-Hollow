# Old Wren's Fire hint after the floor-5 boss

Status: done
Type: task
Blocked by: 07, 13, 14

## Why
The slice's story beat: beat the floor-5 boss, Wren tells you the floor-10 boss is weak to Fire.

## Acceptance criteria
- [x] On the first return home after the Meadow floor-5 boss, Wren has a new dialogue: the floor-10 boss fears fire; chilies grow on Chili Ridge.
- [x] That dialogue reveals the floor-10 boss's Fire weakness (ticket 07) and unlocks Chili Ridge on the Route board.
- [x] Saved so it plays once.

## Where (Assets/HearthHollow/Scripts)
Town (Wren), UI/DialogueOverlay.cs, Core story flags.

## Comments

- (Claude, 3 Nov) Done. `GameSessionStory.cs`: `WrenFireHintDue` (at home, Meadow floor-5 boss beaten, flag not set), `WrenFireHintPages()` (4 pages: the Sow, the Hoarfrost Alpha fears fire, chilies only on Chili Ridge, pack ginger), `GiveWrenFireHint()` (sets `wren_fire_hint`, teaches the Alpha's Fire weakness via `DiscoverElement`, reveals Chili Ridge, saves). In town, Old Wren starts the conversation by herself shortly after you arrive (or when you talk to her); the last page offers "🗺️ Open the route board". Tests: `WrenFireHintTests`, incl. a real run (boss kill on floor 5 → exit → home → due) and save round-trip (97/97 pass). Also added backlog ticket 22 (larger UI text).
