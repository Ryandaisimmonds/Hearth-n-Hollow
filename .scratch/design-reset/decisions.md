# Design reset — decisions so far

Started 3 Nov 2026. Rethinking Hearth & Hollow from the ground up, reusing the systems already built,
checking each one against the end goal. Decisions are Ryan's; this file records them as they're made.

## 1. North star
After a session the player says: "I can't wait to get back to town and see what meals I can make next,
and what I can upgrade to make the next run better, deeper, and get better loot."
- The payoff lives in town (cook, upgrade). The dungeon is how you earn it.
- Test for every dungeon system: does it make the trip home more exciting?

## 2. Where the fun is in a run
- Mostly run decisions (what to grab, when to push deeper, when to leave), Moonlighter-style.
- Combat is simple but somewhat deep: punchy and readable, a few satisfying tools.
- Bosses are the set pieces that need thought and planning.

## 3. Bosses
- Both: prep in town decides how hard the fight is; the fight tests reading patterns.
- Weighted towards prep: learn the boss, then cook and gear to counter it.

## 4. Elements (replaces hit types, shield pips and Boost)
- In-depth but easy. Starting set, extend later if needed:

| Element | Strong vs | Weak vs | Infuse (offence) | Guard (defence) |
|---|---|---|---|---|
| Fire | Frost | Water | chili, fire honey | mint |
| Frost | Nature | Fire | frostberry | ginger |
| Nature (poison) | Water | Frost | nettle | garlic / herbs |
| Water | Fire | Nature | marsh fish, salt | bread / grain |
| Light | Shadow only | - | glowcap | - |

- Weakness x1.5 damage, resistance x0.5.
- Meals do both: cook a resistance (guard) or cook an element into your attacks (infuse). One infused element per run.
- Discovery: an enemy's weaknesses and resistances are hidden until you hit it with that element
  or find information about it (e.g. Old Wren, notes). Once known, they show visually on the enemy
  and fill in a bestiary chart.

## 5. Classes and progression
- Keep the classes, with light progression (route D). No level-50 grind, no stat/fighting bonuses from skills.
- Each class has only a few skills/moves. A skill is unlocked by meeting a requirement
  ("to unlock this skill, do X"), then learned in town. Unlocks are moves, not numbers.
- No class should outshine another: power is still down to what you cook.
- Base stats are minimal for everyone. Classes differ by multipliers on meal effects
  (e.g. a fighter gets x1.2 from health meals and x0.8 from magic meals). This replaces/generalises
  the old "favourite station x1.5".
- Town upgrades carry the progression, alongside the moves/skills.

## 6. Light toolkit
- One light button: Flare (a burst of light that reveals secrets, stuns shadow creatures and lights braziers).
- The thrown torch is removed: it didn't add enough to the game.
- Keep the Ultimate (one per class).
- Braziers still light when a room is cleared, or with fire hits and Flare; the carried lantern stays.

## 7. Shape of a run: fewer, deeper dungeons
- A handful of biome dungeons (e.g. Meadow, Marsh, Caverns, Ember, Frost, Hollow Heart), each ~10-15 floors.
- A boss every 5 floors; beating it unlocks a shortcut to start from there.
- A run is a few floors at a time; "deeper" literally means a lower floor than last time.
- The ladder choice (go deeper / go home with the haul) stays at the heart of each floor.
- Biomes line up with elements (a Frost biome means packing Fire infusions and ginger guards).
- Side dungeons can stay as a few special one-offs (e.g. for key items). Replaces the 18 short routes.

## 8. What's inside a meal
- Recipes by station: keep (Pot = defence/heal, BBQ = damage, Sandwich = speed/utility).
- Two separate ways a meal gets better:
  - Quality (small): each cook has a chance to come out as a better version of the same meal,
    for a small bonus. 3 qualities per meal. The cooking minigame nudges this chance.
  - Tier (big): a stronger version of the dish that needs ingredients only found deeper.
    This is what keeps "go deeper" as the way to real power.
  - Ingredients have no grades (confirmed 3 Nov): depth gives new and rarer ingredients instead.
- Accelerator slot becomes the element slot (infuse or guard, see 4).
- Combos stay but are reworked into fun bonuses with little effect on power.
- Station levels 1-5: keep. Meal pantry + cook-ahead: keep. Camp Bread: keep.
- Cooking minigame: yes, short and optional; extra effort gives a small quality bonus.

## 9. Gear
- Gear is utility, not power: bag (carry more home), boots (speed, dash), lantern (light reach),
  weapon (sets your element / attack shape). Big damage and armour numbers come from meals.
- Gear is shared across classes: smithy upgrades apply to everyone, so a new class never starts over.
- Trinkets stay, as fun rule-benders that make the gameplay enjoyable, not as raw power.

## 10. Town growth
- Both story and building:
  - The main buildings and town areas open through the story (biome bosses as milestones, new townsfolk arrive).
  - Specific areas are then down to upgrades: the player chooses and pays with coins + dungeon materials.
- Each biome drops its own building material, so picking a dungeon is also a town-planning choice.

## 11. The opening
- Keep the premise (a dead campfire that grows into a village), trim the beats:
  arrive at the dead fire -> first short dive -> die or escape, Wren feeds you, kitchen opens,
  cook your first meal -> second dive with that meal (feel that food = power) ->
  beat the first boss, the camp becomes a small town. Dash/boots come from the smithy as the first upgrade.
- The pit beat is cut.
- The 75% start speed is dropped.

## 12. Light in the dungeon
- Keep: rooms get darker with depth, and the carried lantern is shaped by meals.
- Keep: braziers flare on when a room is cleared.
- Keep: enemies drawn as silhouettes with glowing eyes in the dark.
- Cut: "lit = weak spot" damage bonus (overlaps element weaknesses; nearly always on).
- Keep: shadow creatures (near-invisible until lit, weak to Light) - the reason Flare and Light matter.
- Simplify: light-shy / light-drawn behaviour stays as flavour on a couple of enemy types, not a system.
- Keep: secrets found with Flare (cracked walls, hidden forage, brazier puzzles) - they're loot.
- Fold in: Darkness is no longer a separate route hazard; deeper floors are simply darker.

## 13. Story structure
- Main story runs through the biome dungeons, with a few key items (route B).
- Several main dungeons can be open at once, not a strict one-after-another chain.
- Bosses can require trekking to a different dungeon to gather specific resources first
  (e.g. an ingredient for the meal that counters that boss).
- Side dungeons unlock through play.
- Each class has its own story arc: progressing it means taking that class to specific
  dungeons/floors. This gives a reason to play each class.

## 14. Class story arcs (route A)
- Each class's arc unlocks that class's moves; the main story never requires a specific class.
- Each class has a small pool of skills plus 2-3 Ultimates; the player picks which to equip (a loadout),
  so it isn't just the basics. (Refines 5 and 6: still no stat bonuses, still one Ultimate equipped at a time.)
- Most skills unlock fairly early; the arc keeps adding skills and Ultimates as you learn more about the character.

## 15. Extras
- Death penalty: keep (fall = keep 25% of each bag stack; heading home keeps everything).
- Camp events: keep, as town life (visitors, weather).
- Route mutators: cut for now; may return later as optional challenge floors.
- Fishing pond: keep. Forage minigame: keep, simple. Rest/campfire rooms: keep.
- Gamble rooms: leave out.
- Old camp screen: retire once the town does everything.

## 16. Vertical slice: add a second dungeon (3 Nov)
- The slice gets a second, smaller dungeon. The main dungeon's boss is much easier with Fire food,
  and the Fire accelerator ingredient only drops in that second dungeon. This tests the
  "trek elsewhere to prepare for a boss" loop. The details will change as the slice is built.
