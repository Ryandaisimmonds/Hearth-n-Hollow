// Hearth & Hollow — everything the real-time dungeon needs: themes, the shared room pool,
// marker tiles and how each monster moves and fights. Built by "Hearth & Hollow > Build Dungeon Kit".
// Damage / HP / rewards come from the content asset's tuning (difficulty-scaled), not from here.
using System;
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace HearthHollow.Dungeon
{
    public enum EnemyArchetype
    {
        Chaser,   // walks at you and swings when close
        Charger,  // winds up, then rushes in a straight line
        Flyer,    // weaves around, darts in to sting
        Caster,   // keeps its distance and throws orbs; may heal allies
        Brute     // big and slow: breath fan + charge (bosses / drakes)
    }

    public enum LightReaction { None, Shy, Drawn }

    [Serializable] public class EnemyProfile
    {
        public string monsterId;
        public EnemyArchetype archetype = EnemyArchetype.Chaser;
        [Tooltip("Tiles per second.")] public float moveSpeed = 2.2f;
        [Tooltip("Starts an attack inside this distance.")] public float attackRange = 1.0f;
        [Tooltip("Seconds of telegraph before a hit lands.")] public float windup = 0.4f;
        [Tooltip("Multiplier on the tuning's monster attack cooldown.")] public float cooldownMult = 1f;
        [Tooltip("Projectile speed for casters / breath.")] public float projectileSpeed = 6f;
        [Tooltip("Hits per attack (wolves bite twice).")] public int hits = 1;
        [Tooltip("Heals the weakest ally now and then (Bog Shaman).")] public bool healsAllies;
        [Tooltip("Visual size multiplier.")] public float scale = 1f;

        [Header("Elements (Design v2)")]
        [Tooltip("The monster's own element. With no lists below, it's weak to what beats it in the loop (Fire > Frost > Nature > Water > Fire) and resists itself.")] public Element element;
        [Tooltip("Weak to these (×1.5 damage). Leave empty to use the element's default.")] public List<Element> weakTo = new List<Element>();
        [Tooltip("Resists these (×0.5 damage). Leave empty to use the element's default.")] public List<Element> resists = new List<Element>();

        [Header("In the dark (light plan step 2)")]
        [Tooltip("Where the glowing eyes sit, as a fraction of the figure's height.")] [Range(0f, 1f)] public float eyeHeight = 0.62f;
        [Tooltip("Gap between the eyes (world units, before size).")] public float eyeSpread = 0.11f;
        public Color eyeColor = new Color(1f, 0.35f, 0.2f);
        [Tooltip("Rim on the silhouette when it's in the dark.")] public Color rimColor = new Color(0.55f, 0.72f, 1f);
        [Tooltip("A light this monster always carries (alpha 0 = none).")] public Color carryLight = new Color(0, 0, 0, 0);
        [Tooltip("Shy: keeps out of torch / brazier / Flare light and is bolder in the dark. Drawn: goes to the nearest bright light (a lure).")]
        public LightReaction lightReaction = LightReaction.None;
        public float carryLightRadius = 3f;
    }

    [CreateAssetMenu(menuName = "Hearth & Hollow/Dungeon Settings", fileName = "DungeonSettings")]
    public class DungeonSettings : ScriptableObject
    {
        [Header("Themes (one per route)")]
        public List<DungeonTheme> themes = new List<DungeonTheme>();
        [Tooltip("Used for any route without its own theme.")]
        public DungeonTheme fallbackTheme;

        [Header("Shared room pool (used by every theme)")]
        public List<RoomTemplate> rooms = new List<RoomTemplate>();

        [Header("Marker tiles painted in room prefabs")]
        public TileBase markerFloor;
        public TileBase markerWall;
        public TileBase markerWallFace;
        public TileBase markerHazard;

        [Header("Town (Hearth's Hollow)")]
        public TownArt townArt;

        [Header("Monsters")]
        public List<EnemyProfile> enemies = new List<EnemyProfile>();

        [Header("Feel")]
        [Tooltip("Player walk speed (tiles per second).")] public float playerSpeed = 5.2f;
        [Tooltip("Walking speed in town, as a multiple of playerSpeed (the town's big: get about quicker).")] public float townSpeedMult = 1.5f;
        public float dashSpeed = 15f;
        public float dashSeconds = 0.16f;

        [Header("Opening arc (tutorial stages, start speed, dash lock, the pit)")]
        [Tooltip("Stages from a dead campfire to a small town. baseSpeed scales playerSpeed for everyone before boots, charms, skill nodes and meals.")]
        public OpeningArc arc = new OpeningArc();
        [Tooltip("Character art: source pixels per world unit (tiles are 32 px).")] public float characterPixelsPerUnit = 32f;
        [Tooltip("Dungeon camera half-height in tiles. 6 = a room fills the screen (Moonlighter-style).")] public float cameraSize = 6f;
        [Tooltip("Town camera half-height in tiles.")] public float townCameraSize = 8.4f;
        [Tooltip("Multiplies every theme's room light (lower = darker rooms, so fire, moves and embers stand out).")]
        [Range(0.02f, 1f)] public float roomLight = 0.2f;
        [Tooltip("Rooms with nothing to fight (start, forage, campfire, and any room once cleared) light up to this × the normal room light, after a short pause.")]
        [Range(1f, 4f)] public float litRoomBoost = 1.8f;
        [Tooltip("Seconds after you walk in (or clear the room) before it lights up, and how long the light takes to come up.")]
        public float lightUpDelay = 0.6f, lightUpSeconds = 1.4f;
        [Tooltip("The warm light you always carry: intensity and how far it reaches.")] public float carriedLight = 1.7f, carriedLightRadius = 13f;
        [Tooltip("How much your carried light takes on your class colour (0 = plain firelight, 1 = fully the class colour).")] public float carriedLightClassTint = 0.25f;
        [Tooltip("Higher = the light gathers round you and fades out sooner; lower = a flatter pool that reaches the edge.")] [Range(0f, 1f)] public float carriedLightFalloff = 0.55f;
        [Tooltip("Walls catch light from further away than the floor: a second, wider light that only lights the Walls layer (intensity, reach × the carried light's).")]
        public float wallGlow = 1f, wallGlowReach = 1.5f;
        [Tooltip("Monsters in less light than this show as a dark silhouette with a rim and glowing eyes; fully coloured above silhouetteAbove.")]
        public float silhouetteBelow = 0.3f, silhouetteAbove = 0.9f;

        [Header("Darkness by depth (light plan step 5)")]
        [Tooltip("Room light × this on floor 1, 2, 3 (later floors use the last value).")] public float[] floorLight = { 1f, 0.65f, 0.4f };
        [Tooltip("Room light × this in a boss room until the boss is down.")] [Range(0.05f, 1f)] public float bossRoomLight = 0.3f;
        [Tooltip("Caves (mushroom / crystal / lava routes) start darker: room light × this.")] [Range(0.1f, 1f)] public float caveLight = 0.8f;

        [Header("Your light, fed by your packed meals (step 5)")]
        [Tooltip("Per Sandwich meal (× grade): reach +this × your light's radius.")] public float mealReach = 0.1f;
        [Tooltip("Per BBQ meal (× grade): brightness +this, and a redder, hotter colour.")] public float mealHeat = 0.08f;
        [Tooltip("Pot meals steady the flicker and shorten Flare's cooldown by this many seconds each (× grade).")] public float mealSteady = 1.5f;

        [Header("Flare (F / LT) — a burst of light round you (step 5)")]
        public float flareCooldown = 20f, flareMinCooldown = 10f, flareSeconds = 4f, flareRadius = 12f, flareIntensity = 2.4f;

        [Header("Monsters and light (light plan step 6)")]
        [Tooltip("Light-shy monsters in the dark: speed × this and attack cooldown ÷ this.")] public float shyBoldness = 1.25f;
        [Tooltip("Light-shy monsters circle round a light for about this long, then lose patience and push through it.")] public float shyNerveSeconds = 2.5f;
        [Tooltip("Speed × this while a light-shy monster pushes through light.")] public float shyBraveSpeed = 0.55f;

        [Tooltip("Chance a monster is a shadow creature on floor 1, 2, 3… (caves × 1.5; never bosses or elites).")] public float[] shadowChance = { 0.05f, 0.12f, 0.2f };
        [Tooltip("Shadow creatures: damage taken × this while in the dark, + this bonus while lit.")] public float shadowDarkDamage = 0.5f, shadowLitBonus = 0.5f;
        [Tooltip("Flare stuns shadow creatures within this share of its radius, for this long.")] public float flareStunReach = 0.6f, flareStunSeconds = 2.5f;
        [Tooltip("Out of a fight (calm or cleared room), Flare's cooldown is only this long, so you can sweep for secrets.")] public float flareCalmCooldown = 2f;


        [Header("Class rework (31 Oct)")]
        [Tooltip("Every player hit area is this much bigger.")] public float playerReach = 1.3f;
        [Tooltip("Aim assist cone (degrees) for melee / ranged moves that don't set their own.")] public float meleeAimAssist = 40f, rangedAimAssist = 30f;
        [Tooltip("Seconds after a combo swing finishes in which the next press continues the chain.")] public float comboWindow = 0.6f;
        [Tooltip("Ultimate: seconds taken off its cooldown for each hit you land (each enemy counts at most every 0.15 s).")] public float ultChargePerHit = 0.5f;
        [Tooltip("Aim assist cone multiplier while aiming with the mouse (the cursor already points where you want).")] public float mouseAimAssist = 0.35f;
        [Header("Emberfist Heat")]

        [Header("Secrets (light plan step 7)")]
        [Tooltip("Chance a room hides a forage spot that only light shows.")] public float secretForageChance = 0.3f;
        [Tooltip("Chance a room has a cracked wall with a chest behind it.")] public float secretWallChance = 0.25f;
        [Tooltip("Chance a calm room (forage / rest) is a brazier puzzle.")] public float puzzleChance = 0.35f;
        [Tooltip("Seconds a lit puzzle brazier burns before going out, by floor (0 = stays lit).")] public float[] puzzleBurnByFloor = { 0f, 30f, 18f };
        [Tooltip("Flare shows every secret in its reach for this long.")] public float secretRevealSeconds = 8f;
        [Tooltip("Your own light gives a faint glint of a secret within this many tiles.")] public float secretGlintReach = 2.5f;

        [Header("Monster lights")]
        public Color eliteLantern = new Color(1f, 0.78f, 0.35f);
        public Color bossLight = new Color(1f, 0.38f, 0.18f);
        [Range(0f, 1f)] public float wallGlowFalloff = 0.3f;

        public DungeonTheme ThemeFor(string routeId)
        {
            foreach (var t in themes) if (t != null && t.routeId == routeId) return t;
            return fallbackTheme != null ? fallbackTheme : (themes.Count > 0 ? themes[0] : null);
        }

        public EnemyProfile ProfileFor(string monsterId)
        {
            foreach (var e in enemies) if (e != null && e.monsterId == monsterId) return e;
            return new EnemyProfile { monsterId = monsterId };
        }
    }
}
