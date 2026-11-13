# Hearth & Hollow

A cosy roguelite cooking adventure, made in Unity 6 (URP 2D). You cook hearty meals in a little town by a dying
fire, pack them on a tray, and head into fog-filled dungeons to fight monsters and gather ingredients. Then you race
back home before your bag (and your luck) runs out. Meals are where your power comes from: what you eat before you
set out carries the whole trip.

The current build is the **vertical slice**: the opening fall, Old Wren's story thread, Meadow Hollow, Chili Ridge and
Frost Hollow. The design lives in **Hearth & Hollow — Design v2**. `CLAUDE.md` links to it and has working notes.

## Open and run

1. Open the project folder in **Unity 6000.6** (Unity Hub → Add → this folder).
2. Open `Assets/HearthHollow/Scenes/HearthHollow.unity` and press **Play**.
3. A new game opens with a fall into a nameless dungeon. Your save is
   `Application.persistentDataPath/hearth_hollow_save.json` (with a `.bak` of the save before it).
   To start over, use **Hearth & Hollow → Delete Save**.

**Controls:** WASD or the left stick to move, left click / J to attack, Space to dash, E to use, F to throw a
flare, Tab for the bag, Esc for settings. **Esc → dev panel** has cheats: coins, pantry, arc stage, story steps,
the slice switch and more.

## Where things live

Everything is under `Assets/HearthHollow/`.

| Folder | What's there |
|---|---|
| `Scripts/Core/` | The game rules, in pure C# with no Unity (assembly `HearthHollow.Core`). `GameSession` (the `GameSession*.cs` partials) is the one class the game talks to. `Story` holds the opening arc, Old Wren's thread and the fall. `Ids` has every area, monster, ingredient and recipe id. |
| `Scripts/Core/Content/` | All game content (classes, food, areas, monsters, gear, tuning) as code: `DefaultContent.*.cs`. Bump `LatestContentVersion` when you change it. |
| `Scripts/Runtime/` | `GameBootstrap` (the entry point in the scene), `SaveService` and `GameAudio`. |
| `Scripts/Dungeon/` | The real-time dungeon: `DungeonController`, rooms, the player, enemies, lights and effects. `DungeonSettings` is the one settings asset for how it looks and feels. |
| `Scripts/Town/` | The walkable town: `TownController` (spots and what they do), `TownLayout`, `TownPlayer`, and the visuals, weather and critters. |
| `Scripts/UI/` | UI Toolkit screens and overlays. `GameUI` is the hub, and the dev panel is in `SettingsOverlay.cs`. |
| `Scripts/Editor/` | Editor menus and builders (see below), plus the balance simulator. |
| `UI/` | Styles: `Theme.uss` (the colour palette) and `HearthHollow.uss`, which imports one sheet per screen from `UI/Styles/` in order. |
| `Content/`, `Dungeon/`, `Art/`, `Resources/` | The content asset, the Dungeon Settings asset, our own art and runtime-loaded assets. |

Every third-party pack lives in `Assets/ThirdParty/`: Ninja Adventure (characters, bosses, items, audio; CC0), Cainos Pixel Art Top Down Basic (the town), Layer Lab GUI-TheStone (the UI skin) and 100 Top Down Monsters Vol 1 (monsters). Only the parts the game uses are kept from the last two; each folder has its licence.
| `Tests/Editor/` | EditMode tests (NUnit) for the rules, content, saves and story, plus an autoplay bot that plays the whole slice in code. |
| `Tests/PlayMode/` | A smoke test that plays the real scene with a throwaway save: the fall, every town station, cooking, setting out, and clearing a room. |

Planning notes, tickets and reports are in `.scratch/` (`vertical-slice/`, `refactor/`, `design-reset/`).

## Tests

**Window → General → Test Runner**:
- **EditMode**: about 180 tests, run in under 10 seconds. Run them after any change to the rules or content.
- **PlayMode**: two smoke tests that load the real scene with a throwaway save (your real save is never touched).
  **Quick** (about 30 seconds: the fall, the town, cooking, setting out, a first room) after any change;
  **Full** (about 2.5 minutes, category `Full`: every overlay and station, a boss entrance, every monster and every
  move) before a commit. Both save screenshots of the key screens to `Temp/ClaudeShots`.

## Editor menus (Hearth & Hollow →)

| Menu item | What it does |
|---|---|
| Create Content Asset | Writes `Content/GameContent.asset` from the content in code. |
| Build Game Scene | Sets up the scene's `GameBootstrap`, UI document and panel settings. |
| Build Art Library / Build Audio Library | Gathers sprites and sounds into the libraries the game reads. |
| Build Dungeon Kit / Rebuild Room Layouts | Makes the Dungeon Settings asset, the room templates and monster looks. |
| Apply Cainos Art, Set Up HD-2D Look, Generate Normal Maps | Art and lighting passes. |
| Write Balance Report | Simulates every slice area with every tray and writes `.scratch/vertical-slice/balance-report.md`. |
| Delete Save | Deletes the save, so the next Play is a new game. |
