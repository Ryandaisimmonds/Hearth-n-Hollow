# Hearth & Hollow — art & style guide

Everything the game draws lives under `Assets/HearthHollow`. Nothing is loaded from outside it.

## Where things are

| What | Where | How the game finds it |
|---|---|---|
| **Colours** for every screen | `UI/Theme.uss` | `--hh-…` variables, used by `HearthHollow.uss` |
| **All UI styling** (panels, buttons, cards, HUD) | `UI/HearthHollow.uss` | has a CONTENTS list at the top |
| **UI skin** (panels, buttons, ribbons, frames, bars, word art, icons) | `Art/ThirdParty/GUI-TheStone/` | referenced by path from `HearthHollow.uss` and by the Art Library |
| **Characters + monsters** (sprite sheets) | `Art/ThirdParty/Top Down Monsters Vol 1/<Name>/` | Art Library → Monsters / Classes |
| **Which sprite each game id uses** | `Art/ArtLibrary.asset` | the game asks by id: `slime`, `weapon_2`, `coin`, `herb_stew`… |
| **Dungeon tiles & props** (one set per route) | `Dungeon/Themes/<route>/*.png` | the route's `Theme … .asset` |
| **Room layouts** | `Dungeon/Rooms/*.prefab` | Dungeon Settings / theme room lists |
| **Your own new art** | `Art/Custom/<Characters, Monsters, Food, Icons, UI, Dungeon>/` | point an Art Library slot or theme field at it |
| **Font** | `UI/Fonts/Changa SDF.asset` | `.hh-root` in `HearthHollow.uss` |

The original Photoshop/Illustrator sources for the UI skin are still in the pack folder:
`Assets/Layer Lab/GUI-TheStone/PSD,AI/`. The rest of both packs (unused pieces, demo scenes) is still in
`Assets/Layer Lab` and `Assets/100 Top Down Monsters - Pixel Art - Vol 1` — nothing in the game uses those folders
any more, so you can delete them if you don't want the extra pieces.

## Quick recipes

**Recolour the UI** — open `UI/Theme.uss`, change a value (e.g. `--hh-gold`), save. Every screen updates, even in Play mode.
Colours with transparency (`rgba(...)`) are still written directly in `HearthHollow.uss` (search for the section).

**Repaint a panel, button or icon** — edit the PNG in `Art/ThirdParty/GUI-TheStone/...` in place and save; Unity
re-imports it. Keep the canvas size. Panels and buttons are **9-sliced**: if you move the painted border, fix the
border in the Sprite Editor (select the PNG → Sprite Editor → green border handles). In the USS, `-unity-slice-scale`
shrinks the corners on screen — don't hand-write `-unity-slice-*` sizes for these sprites.

**Repaint a monster or character** — edit its PNG in place, keeping the same grid:
- Sheets: `<Name>_Idle.png`, `<Name>_Move.png`, `<Name>_Attack.png`.
- Frames are **128×128** cells. Rows top→bottom face **Down, Left, Right, Up**; columns are the frames
  (idle 4, move 6, attack 6 for most monsters).
- Draw the figure standing on the same spot in every frame: the game puts the **lowest pixel of the idle frames** on the
  ground, and uses the idle frames' height for hit boxes and health bars.
- Import settings: Point filter, no compression, Pixels Per Unit 100, Sprite Mode Multiple (already set).

**Add a new monster/character look** — copy a folder from `Top Down Monsters Vol 1` into `Art/Custom/Monsters`
(new PNGs dropped there get the right pixel settings automatically), slice each sheet in the Sprite Editor
(Grid by Cell Size 128×128), then in `Art/ArtLibrary.asset` drag the frames into that monster's
Idle Sheet / Move Sheet / Attack Sheet lists (row by row: Down, Left, Right, Up) and set the column counts.

**Give food real icons** — food is still emoji. Drop PNGs in `Art/Custom/Food`, then in `Art/ArtLibrary.asset`
fill the **Ingredients** / **Recipes** slots (every ingredient and recipe already has an empty slot by id).
Empty slots keep showing the emoji. Route pictures work the same way (**Routes**).

**Repaint a dungeon** — every route has its own tiles in `Dungeon/Themes/<route>/`: `Floor`, `Floor Alt`, `Wall`,
`Wall Face`, `Hazard`, three `Decor` tiles (all **32×32**, 32 px per tile), plus props `Campfire`, `Exit`, `Forage`,
`Ladder` (32×32) and `Door Bars` (96×32). Edit them in place. Colours, light and decor density are on the
`Theme … .asset` in the same folder. These started as generated placeholders; **Build Dungeon Kit** never overwrites
files that already exist, so your edits are safe (delete a file and re-run it to get the placeholder back).

## Import presets (already set up)
- `Art/Presets/Pixel Sprite Sheet (characters, monsters)` — Point, uncompressed, Multiple, 100 PPU.
  Used automatically for new files in `Art/Custom/Characters` and `Art/Custom/Monsters`.
- `Art/Presets/Pixel Sprite (tiles, props, food, icons)` — Point, uncompressed, Single, 32 PPU.
  Used automatically for new files in `Art/Custom/Dungeon`, `Art/Custom/Food` and `Art/Custom/Icons`.
- `Art/Custom/UI` keeps Unity's smooth default (the UI skin is painted, not pixel art).
To apply one to an existing file: select it → Inspector → the slider icon (top right) → pick the preset.

## Careful with
- **Hearth & Hollow → Build Art Library** re-fills the slots it knows about from `Art/ThirdParty`. If you've swapped
  a monster or icon in the Art Library by hand, re-running it puts the original back for those ids.
- Move or rename art **inside Unity's Project window**, not in Finder — Unity then keeps every link. (The style
  sheet points at the UI skin by path, so if you move a UI-skin PNG, update its `url(...)` in `HearthHollow.uss`.)
- Pack licences: `Art/ThirdParty/GUI-TheStone/README LayerLab_UserAssetGuide.txt` and
  `Art/ThirdParty/Top Down Monsters Vol 1/Read Me (pack licence).txt`.
