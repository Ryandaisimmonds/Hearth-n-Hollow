# Hearth & Hollow — notes for Claude

Unity 6, URP 2D. A cosy roguelite: cook meals at camp, then run real-time dungeon floors.
Full history of past sessions is on the Notion handover page `409b771b02298134b81afa22b57325a0` (if Notion is connected).

## Design v2 is the source of truth (3 Nov 2026)
The game was redesigned from the ground up on 3 Nov. Build to **Hearth & Hollow — Design v2** (Claude Doc: https://claude.ai/artifact/M2gLZFSAhMSaxzD6jh8mTJ). The decision log is `.scratch/design-reset/decisions.md`.
Much of the "Game design" section below describes the build as it stands, which predates Design v2 (e.g. Break & Boost, levels and skill trees, the thrown torch and ingredient grades are being cut). Where they disagree, Design v2 wins.

## Working rules (Ryan's)
- Ryan is often playing in the editor. Ask before stopping play mode, and always stop it before script changes.
- Back up the save (`Application.persistentDataPath/hearth_hollow_save.json` and its `.bak`) before any play-test that touches it, and restore both afterwards.
- Screenshots go in `Temp/ClaudeShots`, never `Assets/`.
- After script changes: refresh/compile, read the console, run the EditMode tests.
- Update the handover notes (Notion page above, or this file) at the end of each session.

## Code map (`Assets/HearthHollow/Scripts`)
- `Core/`: pure C# game rules, no UnityEngine. `GameSession` (partial: `GameSession*.cs`) is the one facade the UI talks to.
  Content lives in `DefaultContent*.cs`, upgraded through `DefaultContent.Upgrade`. Bump `LatestContentVersion` (now 25) for any content change, then upgrade `Content/GameContent.asset`.
- `Runtime/`: `GameBootstrap` (entry point) and `SaveService` (atomic save with `.bak` fallback; `DirectoryOverride` is for tests).
- `Dungeon/`: real-time rooms. `DungeonController`, `PlayerController`, `RoomInstance`, `RoomLights`, `Interactable` (incl. the Pit), `PitCache`, `ShadowSafeLight`, `DungeonSettings` (one settings asset: feel, lights, and `arc`).
- `Town/`: the walkable town (`TownController`, `TownPlayer`, `TownVisuals`, `TownWorkAreas`). It's opened from Settings → Visit Hearth's Hollow.
- `UI/`: UI Toolkit screens. The dev panel is in `SettingsOverlay.cs`.
- `Tests/Editor/`: 38 EditMode NUnit tests (save, content, rules, opening arc). Run them from Window → General → Test Runner.

## Systems worth knowing
- **Lighting:** only `DungeonController.CastShadows(light)` turns shadows on, and it adds `ShadowSafeLight`. That nudges still lights out of walls and fades shadows on moving lights inside walls. New `Light2D`s must set `shadowsEnabled = false` (URP defaults to true).
- **Opening arc** (`Core/OpeningArc.cs`, `GameSessionArc.cs`): stages are 0 Arrival → 1 first death (fed, kitchen) → 2 first ladder (boots = dash, smithy/workshop) → 3 fall into the pit (cache room) → 4 home (town).
  - Base speed is 0.75, and speed gear is doubled in content v24.
  - Not done yet: hiding town stations by stage (use `Session.AreaOpen("kitchen")` etc.).
- **Dev panel:** Max everything, arc stage buttons, coins, pantry, levels, story, weather.

## Open ideas / next steps
- Wire the town to the arc stages (strip the camp down at stage 0) once the town plan settles what's visible per stage.
- Give the pit's cache room its own look; right now it's a normal room with a chest.
- Matt Pocock skills are set up (3 Nov): see "Agent skills" below.
- Map the Ninja Adventure assets to the town plan; do a build and performance pass.

## Game design (the whole picture)

**Premise.** You arrive at a dead campfire in a hollow. Run by run, the tiny camp grows into a mini village (Hearth's Hollow). The Hearth fire grows as you clear chapters of the main story, the Ember Path.

**Core loop.**
1. **Camp / town:** cook meals, pack the tray (3 meals), and upgrade the kitchen, gear and skills.
2. **Route board:** pick a route. Each route can roll a modifier (e.g. Lucky), and the day can bring a camp event: Storm, Harvest Moon, Merchant, Armourer or Forager.
3. **Dungeon:** real-time rooms over several floors. You fight, forage (a timing minigame), rest at campfires and find secrets. At each ladder you choose: go deeper (harder and richer) or head home.
4. **Settle:** coins, XP, ingredients and building materials come home. If you fall (die), you keep only 25% of each bag stack; heading home keeps everything.

**Meals are the power.** The player is weak alone, and meals carry a run.
- **Stations:** Pot, BBQ and Sandwich. Each upgrades to level 5 with coins and dungeon materials, and higher levels unlock better dishes.
- **Grades:** Plain, Fine or Prime, for both ingredients and meals. Accelerator ingredients add a boost.
- **Buffs:** the tray is eaten at set-out and its buffs last the whole run. Synergies reward mixing dishes (e.g. a Balanced Feast).
- **Hazard meals:** they counter route hazards (poison, cold, heat, darkness).
- **Always available:** Camp Bread.
- **Class favourites:** each class has a favourite station whose meals count extra.

**Classes** (only the Cook at the start; the others join when you first clear their route):

| Class | Joins after |
|---|---|
| Cook | the start |
| Ranger | Meadow Trail |
| Warden | Salt Marsh |
| Lamplighter | Firefly Glade |
| Emberfist | Chili Ridge |

- **Kit:** each class has an attack, a skill (with an alternative), a dash and an ultimate, plus a 3-branch skill tree with level cap 50.
- **Colours:** each class has its own strong, saturated colour theme. Emberfist was the reference for 'enough colour'.
- **Break & Boost:** moves deal a hit type (Slash, Pierce, Blunt, Fire, Light), and monsters are weak to one or two types. Boost levels grow a move's shape: count, radius, hits or duration.
- **Ultimates:** ready at the start of every run, 22.5 s cooldown, and every hit you land takes 0.5 s off it. The aim is roughly one ultimate per room or two.
- **Other cooldowns:** skills were made about 30% faster for more fun.

**Light is the theme.**
- Rooms are dark and the player carries a light tinted by class colour. Every move lights the world, and basic moves are big and bright.
- Braziers flare on when a room is cleared. Fire hits and thrown torches (R) light them.
- Secrets are found with light: cracked walls hide chests, brazier puzzles give a tier-2 chest, and hidden forage appears.
- Shadow creatures appear more the deeper you go.
- Darkness is a hazard on some routes.

**Story and variety.**
- The Ember Path is told in chapters. Side dungeons give key items.
- Mutators are fun route rules: Swift, Frantic, Giants, Swarm, Big Bangs and Glass Hearts.
- Bosses have their own Ninja Adventure sprites.

**Gear (Workshop / Smithy).**
- Weapon, body, head, boots and bag tiers are per class; the bag is shared.
- Trinkets: 2 slots, bought, found as drops or given for first clears.
- Speed comes ONLY from boots, charms, skill nodes and meals ('we don't need to blast through the dungeons').

**Controls** (keyboard + mouse, modelled on Hades; see `Dungeon/DungeonInput.cs` for exact bindings):
- WASD moves, the mouse aims (with a reticle), and skills land at the cursor.
- Left click / J attacks; the attack button sits first on the HUD with big text.
- Skill 2 is on L (keyboard) or Q (mouse), the ultimate on I or C, the torch on R, the dash on Space, and E interacts.
- Clicks on the UI never attack.

**Town (Hearth's Hollow).**
- A walkable town that will replace the old camp screen; that screen is kept for testing.
- Each station (kitchen, smithy, workshop, etc.) opens its own centred window over the game. Runs start only at the Route board by the gate.
- Movement is 1.5× in town. You can dash and swing; critters you hit jump and fly off.
- Work areas have props, motion and working townsfolk.
- Old Wren gives the first-meeting tutorial. The fishing pond allows one catch a day. Weather follows the day's event.

**Pacing / onboarding (the opening arc).**
- Start slow and fragile: 75% walk speed and no dash.
- Die easily, get fed, go deeper, find a ladder, fall into a pit and find equipment, come home, then the camp becomes a town.
- Later runs feel fast through gear, not through a global speed ramp.
- Recommended build approach: a vertical slice first (style test + the first arc beats), then grow the town piece by piece toward the end goal.
- The town redesign plan (lists of buildings, upgrade stages per area, future buildings) is in the project root: `Hearth's Hollow — Town Redesign Plan.docx` (exported from the Claude Doc). Read it before any town work.

**Art.** Ninja Adventure asset pack (chibi, 16 px).
- Characters are scaled 1.8 (the dragon 2.56), then 20% smaller to fit rooms.
- The Cook uses the Villager sprite, because Master had no legs.
- To rebuild the art after changes: Hearth & Hollow → Build Art Library.

## Gotchas
- **Mac build colours:** a purple, flat Mac build was caused by URP stripping. Keep `Strip2DUnusedVariants` and `StripUnusedPostProcessingVariantsAndResources` OFF in the URP Global Settings, because all lights and the volume are created at runtime.
- **`execute_code` blocks:** the MCP's `execute_code` refuses code containing file or asset delete calls. Move files to `Temp/` instead.
- **Edits through `execute_code`:** use exact-match multi-edits, normalising line endings first.

## Agent skills

### Issue tracker

Issues live as local markdown files under `.scratch/` in this repo. See `docs/agents/issue-tracker.md`.

### Triage labels

Default labels (needs-triage, needs-info, ready-for-agent, ready-for-human, wontfix), recorded as a `Status:` line in each issue file. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `CONTEXT.md` plus `docs/adr/` at the repo root. See `docs/agents/domain.md`.
