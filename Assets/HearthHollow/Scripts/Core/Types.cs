// Hearth & Hollow — shared core types (the CONTRACT).
// Pure C#: NO UnityEngine references anywhere in Scripts/Core.
// All classes are [Serializable] with public fields so Unity's serializer
// and JsonUtility can store them (no Dictionaries, no properties-only state).
// State classes are `partial` so a system can add private helper members in its
// own file — but do not rename or remove anything declared here.
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    // ───────────────────────── Enums ─────────────────────────
    public enum ClassId { Warden, Ranger, Cook, Lamplighter, Emberfist }
    public enum Station { Pot, BBQ, Sandwich }
    public enum TileType { Empty, Start, Exit, Monster, Elite, Gather, Rest }
    public enum FightAction { Strike, Guard, Lucky, Skill }
    public enum MonsterBehaviour { None, DoubleHit, Heal, WindUp }
    public enum RouteKind { Trail, Hunt }
    /// A dungeon-wide hazard. Poison/Cold/Heat drain HP (never below 1); Cold also slows you, Heat bites harder while you
    /// stand still. Meals that counter it soften or stop it. Darkness is no longer used (Design v2: deeper floors are just darker).
    public enum Hazard { None, Poison, Cold, Heat, Darkness }

    /// A fun dungeon-wide rule (no meal needed). Several can stack on one route.
    public enum Mutator { None, Swift, Frantic, BigBangs, GlassHearts, Giants, Swarm }
    public enum RouteMod { None, Double, Rich, Dangerous, Lucky }
    public enum CampEvent { None, Merchant, Storm, Armourer, Forager, HarvestMoon }
    public enum Phase { Hearth, Explore, Fight, Gather, Rest, RunEnd, Dungeon }   // Dungeon = real-time rooms (Explore/Fight = old fog grid)
    public enum RunOutcome { None, Cleared, Fled, Died, Returned }   // Returned = took an exit before the final floor
    public enum GatherGrade { Miss, Safe, Perfect }
    public enum SkillKind { Cleave, Volley, HeartySnack }
    /// Gear tracks bought in the Workshop. Weapon, Body (armour points), Head (max HP), Boots (move speed) are per class;
    /// the Bag is shared. Append only.
    public enum GearSlot { Weapon, Body, Head, Boots, Bag }
    /// How rare a meal is (shown on its card, used to sort and filter the meal grid). Append only.
    public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }
    /// A trinket's special effect on top of its stat mods. Append only.
    public enum TrinketPerk { None, MoveSpeed, DashCooldown, SkillCooldown, HealOnKill, AttackPower, SkillPower, HazardWard }

    // ── moves & skill trees (real-time dungeon) ──
    /// Which loadout slot a move fits: Attack (J), Skill (K or U), Dash (Space).
    public enum MoveSlot { Attack, Skill, Dash, Ultimate }   // Ultimate: one per class on its own button (contentVersion 20)
    /// The four loadout slots.
    public enum LoadSlot { Attack, Skill1, Skill2, Dash }
    /// How a move behaves in the dungeon (implemented by the Unity PlayerController). Append only — saved by value.
    public enum MoveKind
    {
        Swing, Lunge, Heavy, Arrow, SpreadArrow, PierceArrow,                 // attacks
        Roll, ChargeDash, Vault, SlickDash,                                   // dashes
        Cleave, Shockwave, WarCry, ShieldWall, Whirlwind,                     // Warden skills
        Volley, Trap, Mark, ArrowRain, Smoke,                                 // Ranger skills
        Snack, PepperToss, HotPot, Picnic, RollingPin,                        // Cook skills
        Combo, Sunshield, LeapSlam, EmberArrow, FlareArrow, Spotlight, Cookfire, SkewerToss,  // class rework (contentVersion 15). Append only.
        Wisp, Lantern, Sunbeam, Prism, Kindle, Blink,                         // Lamplighter (contentVersion 16)
        Phoenix, FlameWheel, Detonate, Counter, Flicker, Sunfall, Starfall, GrandFeast, Dawn, Supernova                       // Emberfist (contentVersion 17)
    }
    /// What a skill-tree node does. Append only.
    public enum NodeEffect
    {
        UnlockMove,      // unlocks moveId for the loadout
        Stat,            // + stat (BuffStat) per rank, like a permanent meal
        AttackPower,     // +% power of attack moves per rank
        SkillPower,      // +% power of skill moves per rank
        SkillCooldown,   // −% skill cooldowns per rank
        DashCooldown,    // −% dash cooldown per rank
        MoveSpeed,       // +% move speed per rank
        HealOnKill       // + HP per kill per rank
    }

    /// Stats a meal buff can change. Percent stats are fractions (0.10 = +10%).
    public enum BuffStat
    {
        DamagePct,     // +% outgoing damage
        MaxHp,         // +flat max HP (and heals the same amount when eaten)
        Heal,          // UNUSED since meals became pre-run buffs (kept so saved enum values don't shift)
        Armour,        // +armour points (5% reduction per point, cap from tuning)
        CritPct,       // +crit chance (fraction)
        GatherZonePct, // widens gather safe+perfect zones by %
        CoinPct,       // +% coins earned this run
        XpPct,         // +% XP earned this run
        MoveSpeedPct,  // +% move speed this run (cooking v9)
        HealOnKill     // + HP healed per kill this run (cooking v9)
    }

    public enum GameEventType
    {
        PlayerHit, PlayerMiss, PlayerCrit, MonsterHit, MonsterHeal, MonsterWindUp,
        PlayerHeal, PlayerGuard, Riposte, MonsterDied, PlayerDied,
        Loot, Coins, Xp, LevelUp, MealEaten, Synergy, TileRevealed,
        RunCleared, RunFled, BagFull, Info
    }

    // ───────────────────────── Content (designer data) ─────────────────────────
    [Serializable] public class StatMod { public BuffStat stat; public float value; }

    [Serializable] public class IngredientData
    {
        public string id; public string name; public string emoji;
        public int tier;         // 0 common .. 3 rare (merchant price scales with tier)
        // Accelerator (cooking v9): an ingredient with a boost can go in a meal's optional accelerator slot.
        public List<StatMod> accelBuffs = new List<StatMod>();   // the boost (accelerators have no grade)
        public bool material;   // a building material (contentVersion 13): found in dungeons, spent on kitchen upgrades, never cooked or sold
        public Hazard accelGuard;                                // the meal also guards against this hazard (old hazard routes)
        public Element infuse;   // Design v2 element slot: in a meal, the run's attacks take this element
        public Element guard;    // …or the meal guards you: attacks of this element hit you for half
        public bool deep;        // only found deep in a dungeon (tier-2 dishes need these)
    }

    [Serializable] public class IngredientCost { public string ingredientId; public int count = 1; }

    [Serializable] public class RecipeData
    {
        public string id; public string name; public string emoji; public Station station;
        public List<IngredientCost> ingredients = new List<IngredientCost>();
        public List<StatMod> buffs = new List<StatMod>();
        public List<string> tags = new List<string>(); // e.g. "fire", "stone", "veg" — used by synergies
        public string description;
        public Hazard counters;   // eating this meal protects against that dungeon hazard
        public Rarity rarity;     // contentVersion 8
        public int stationLevel = 1;   // the station level that unlocks this dish (contentVersion 13; 1 = from the start)
        public bool always;       // always available (Camp Bread, contentVersion 11): never cooked, never runs out, packed straight onto the tray as a Common meal
        public int tier = 1;      // Design v2: 2 = the stronger version of upgradeOf, needing an ingredient found deep
        public string upgradeOf;  // the base recipe's id (tier 2 only)
    }

    /// A tray synergy. Condition evaluated over meals EATEN this run.
    [Serializable] public class SynergyData
    {
        public string id; public string name; public string emoji; public string description;
        public bool requireAllStations;   // Balanced Feast: one meal from each station
        public string requiredTag;        // or N meals sharing this tag
        public int requiredCount;
        public bool requireSameRecipe;    // or N helpings of the very same meal (Comfort Food)
        public bool useStation;           // or N meals from one station (all Pot / all BBQ / all Sandwich)
        public Station requiredStation;
        public float sameRecipeBonus;     // Comfort Food: the repeated meal's buffs count this much extra
        public float allBuffMultiplier = 1f;               // e.g. 1.5 for Balanced Feast
        public List<StatMod> bonus = new List<StatMod>();  // extra flat bonus when active
    }

    [Serializable] public class SkillData
    {
        public SkillKind kind; public string name; public string emoji; public string description;
        public int cooldownTurns = 3;
        public float value;   // Cleave: dmg mult 1.75 · Volley: per-hit mult 0.75 (2 hits) · HeartySnack: heal 12
    }

    [Serializable] public class ClassData
    {
        public ClassId id; public string name; public string emoji; public string role; public string description;
        public int baseMaxHp = 20;
        public float damageMult = 1f;       // Warden 1.15
        public float gatherZoneMult = 1f;   // Ranger wider golden (perfect) zone, e.g. 1.6
        public float mealBuffMult = 1f;     // every meal stat for this class (Design v2: Cook 1.1)
        public List<StatMod> mealStatMults = new List<StatMod>();   // per-stat lens on meals (value = multiplier: strong 1.25, weak 0.8)
        public SkillData skill = new SkillData();   // old turn-based fights (fog grid) only
        // real-time dungeon: the moves a fresh character starts with, and the names of its three tree branches
        public string starterAttack, starterSkill, starterDash;
        public List<string> branchNames = new List<string>();
        // story (class rework): joins your camp the first time you clear this route (empty = with you from the start)
        public string joinRoute;
        public string ultimate;   // move id on the Ultimate button (contentVersion 20)
        public string joinText;   // a line for the "joins your camp" toast
    }

    /// Break & Boost: what kind of hit a move deals. Monsters are weak to one or two kinds.
    public enum HitType { Slash, Pierce, Blunt, Fire, Light }
    /// What each Boost level adds to a move besides damage.
    public enum BoostShape { Damage, Count, Radius, Hits, Duration }

    /// One move a loadout slot can hold. Numbers mean different things per kind (see DefaultContent.BuildMoves).
    [Serializable] public class MoveData
    {
        public string id; public string name; public string emoji; public string description;
        public ClassId owner; public bool anyClass;     // anyClass: every class can use it (the basic Roll)
        public MoveSlot slot; public MoveKind kind;
        public float power = 1f;      // damage × player damage
        public float cooldown = 1f;   // seconds (attacks: time between swings)
        public float range = 1f;      // reach / distance / speed, per kind
        public float radius = 1f;     // area size, per kind
        public float duration;        // stun / buff / zone length, per kind
        public float value;           // extra number, per kind (heal, slow, bonus…)
        // class rework (31 Oct): every move lights the world, reaches further and Boosts into a bigger shape
        public HitType hitType;                 // Break: the kind of hit it deals
        public HitType hitType2;                // a combo's finisher / a move's second kind of hit
        public Element element;                 // Design v2: the move's element (None = takes the run's infused element)
        public Element element2;                // a combo finisher's element (None = same as element)
        public float lightRadius;               // how far its light reaches (0 = from the move's size)
        public float lightIntensity = 1.4f;
        public string lightColour = "";        // hex, e.g. "FFB060" (empty = the class's colour)
        public int count = 1;                   // projectiles / hits per use
        public float spread;                    // degrees between projectiles in a fan
        public float homing;                    // degrees per second a projectile turns towards a target (0 = flies straight)
        public float aimAssist = -1f;           // cone in degrees that snaps aim to the nearest enemy (-1 = the default for melee / ranged)
        public BoostShape boostShape;           // what each Boost level adds besides damage
        public float boostStep;                 // how much per level (Count: +N projectiles, Radius: +x share, …)
    }

    /// A node in a class's skill tree. Costs `cost` points per rank; needs the level and the node above it.
    [Serializable] public class SkillNodeData
    {
        public string id; public ClassId classId;
        public int branch;            // 0..2 (column)
        public int row;               // order down the branch
        public int minLevel = 1;
        public string name; public string emoji; public string description;
        public NodeEffect effect;
        public string moveId;         // UnlockMove
        public BuffStat stat;         // Stat
        public float valuePerRank;    // Stat / AttackPower / … (fractions for %)
        public int maxRank = 1;
        public int cost = 1;
        public string requires;       // node id that needs ≥1 rank first (empty = root)
    }

    [Serializable] public class MonsterData
    {
        public string id; public string name; public string emoji;
        public MonsterBehaviour behaviour; public float hpMult = 1f;  // dragon 1.5
        public bool eliteOnly;
    }

    [Serializable] public class RouteData
    {
        public string id; public string name; public string emoji; public RouteKind kind; public string description;
        public int baseDifficulty;          // added to each tile's req offset
        public float gatherChance = 0.2f;   // share of tiles that are gather tiles
        public float coinMult = 1f;
        public int clearReward = 20;        // coins on portal clear (+ rooms×5)
        public int clearXp = 15;            // XP on portal clear (+ rooms×3)
        public string unlockedByRouteId;    // null/empty = unlocked from start; else clear that route first
        public List<string> monsterIds = new List<string>();
        public List<string> gatherIngredientIds = new List<string>();
        public List<string> materialIds = new List<string>();   // building materials monsters here drop (contentVersion 13)
        // ── story & gates (contentVersion 4) ──
        public int chapter;                 // > 0 = a chapter of the main story (the Ember Path); 0 = side dungeon
        public string requiredKeyItem;      // also need this key item to enter (found in another dungeon)
        public string rewardKeyItem;        // clearing this route the first time gives this key item
        public bool classLocked;            // only requiredClass may enter
        public ClassId requiredClass;
        public Hazard hazard;               // dungeon-wide hazard (drains HP, see TuningData.hazard*)
        public float hazardPct;             // share of max HP lost per hazard tick before meals soften it (Darkness: unused)
        public List<Mutator> mutators = new List<Mutator>();   // fun rules for this dungeon (contentVersion 5)
        public string storyIntro;           // shown on the route board
        public string storyOutro;           // shown after the first clear
    }

    /// A story item kept forever once found (e.g. the Firefly Lantern that lights the Cavern).
    [Serializable] public class KeyItemData { public string id; public string name; public string emoji; public string description; }

    [Serializable] public class GearTier { public int tier; public string name; public int price; public float value; }

    /// A kitchen upgrade step (contentVersion 13): station levels and meal-pantry shelves. Costs coins + dungeon materials.
    [Serializable] public class KitchenTier
    {
        public int level; public string name; public int price;
        public List<IngredientCost> materials = new List<IngredientCost>();
        public float value;   // pantry: shelf count · station: unused (better-meal chance comes from tuning.stationJackpotPerLevel)
    }
    // weapon value = base damage · armour value = armour points

    /// A trinket: worn in a trinket slot (2, a 3rd after a story chapter). Found on first clears, rare boss/elite drops,
    /// sold in the Workshop (unlocked by story chapter) or by the travelling merchant.
    [Serializable] public class TrinketData
    {
        public string id; public string name; public string emoji; public string description;
        public List<StatMod> buffs = new List<StatMod>();   // like a permanent meal (MaxHp, DamagePct, CritPct, Armour, …)
        public TrinketPerk perk; public float perkValue;     // HazardWard: counts as perkValue counter meals vs wardHazard
        public Hazard wardHazard;
        public int price;              // Workshop / merchant price (0 = never sold)
        public bool inWorkshop;        // sold in the Workshop once shopChapter is reached
        public int shopChapter = 1;    // story chapter you must have reached (1 = from the start)
        public bool merchant;          // the travelling merchant can stock it
        public bool drop;              // bosses / elites can drop it
        public string fromRouteId;     // first clear of this route gives it
    }

    [Serializable] public class MerchantOffer { public string ingredientId; public int count; public int price; public bool sold; }

    /// Every tunable number. Defaults = Numbers & Tuning page.
    [Serializable] public class TuningData
    {
        public int gridWidth = 9, gridHeight = 7;
        public float restChance = 0.08f, eliteChance = 0.10f;
        public int monsterReqMin = 0, monsterReqMax = 3, eliteReqMin = 2, eliteReqMax = 6;
        public int roomCoinBonus = 5, roomXpBonus = 3;

        // (Levels, XP and skill points were cut in Design v2, ticket 04: meals are the power.)

        public float armourPerPoint = 0.05f, armourCap = 0.60f;
        public float baseCritChance = 0.10f, critMult = 2f;

        public float monsterHpBase = 6, monsterHpPerDiff = 3;
        public float monsterAtkBase = 2, monsterAtkPerDiff = 1.5f;
        public int killCoinBase = 8, killCoinPerDiff = 4;
        public int killXpBase = 8, killXpPerDiff = 2;
        public float eliteRewardMult = 2f;
        public float monsterDropChance = 0.35f;

        public float strikeHit = 0.60f, strikeHitAfterMiss = 0.80f;
        public int guardHeal = 5; public float guardDamageTaken = 0.5f, riposteMult = 1.5f;
        public float luckyDamageMult = 0.5f, luckyCritMult = 2f;

        public float shamanHealPct = 0.25f, dragonWindUpChance = 0.5f, dragonWindUpMult = 2f;

        public int traySize = 3;
        public float secondHelping = 0.5f, thirdHelping = 0.25f;

        public int bagSlots = 15, bagSlotsBackpack = 30, stackSize = 5;
        public int backpackPrice = 120;

        public float merchantChance = 0.30f, stormChance = 0.20f;
        public float armourerChance = 0.15f, foragerChance = 0.10f, harvestMoonChance = 0.10f;
        public float armourerDiscount = 0.30f;   // gear costs 30% less while the armourer visits
        public int foragerGiftCount = 3;          // free ingredients left by a passing forager
        public int harvestMoonGatherBonus = 1;    // +1 per successful gather on a harvest-moon run
        public float routeModChance = 0.35f;
        public float restHealPct = 0.30f;

        // gather minigame (bar 0..1, zones centred on a random point each attempt)
        public float gatherSafeWidth = 0.30f, gatherPerfectWidth = 0.10f, gatherBarSpeed = 1.2f;
        public int gatherYieldSafe = 1, gatherYieldPerfect = 2;

        public float deathXpMult = 0.5f;

        // ── real-time dungeon ──
        public int dungeonFloors = 3;                         // floors per route; clearing the last one clears the route
        public int roomsPerFloorMin = 7, roomsPerFloorMax = 10;
        public int enemiesPerRoomMin = 2, enemiesPerRoomMax = 4;   // + (floor - 1)
        public float dungeonEliteChance = 0.15f;              // chance an event room adds an elite
        public float bossHpMult = 2.5f, bossAtkMult = 1.2f, bossRewardMult = 2f;
        // a dungeon run has ~8× the kills of an old grid run, so each kill pays less
        public float dungeonKillCoinMult = 0.25f, dungeonKillXpMult = 0.35f;
        public float skillCooldownSeconds = 6f;               // class special
        public float dashCooldownSeconds = 0.7f;
        public float hurtInvulnSeconds = 0.6f;                // i-frames after the player is hit
        public float monsterAttackCooldown = 1.3f;            // seconds between a monster's attacks
        public float cleaveRadius = 2.2f;                     // Warden special (world units)
        public float guardSeconds = 3f, guardDamageMult = 0.5f;   // Cook special: Hearty Snack's guard
        // ── dungeon hazards (poison / cold / heat) ──
        public float hazardTickSeconds = 2f;                  // a hazard bites this often
        public float hazardMitigationPerMeal = 0.5f;          // each counter meal eaten cuts it by this much (2 = immune)
        public float coldSlow = 0.25f;                        // Cold: move this much slower (also softened by meals)
        public float heatStillMult = 2f;                      // Heat: bites this much harder while you stand still
        // ── mutators (fun dungeon rules) ──
        public float swiftSpeed = 2f, franticSpeed = 3f;      // everyone walks this much faster
        public float bigBangDamage = 2f, bigBangRadius = 1.25f;   // area hits (swings, cleaves, zones) hit harder and wider
        public float glassMult = 2f;                          // Glass Hearts: everyone deals this much more damage
        public float giantHp = 1.75f, giantSize = 1.45f, giantSpeed = 0.75f;
        public float swarmCount = 2f, swarmHp = 0.55f;        // Swarm: more monsters, each weaker
        // ── trinkets (contentVersion 7) ──
        public int trinketSlots = 2;                          // slots from the start
        public string trinketSlotRoute = "cavern";            // clearing this route opens one more slot
        public float trinketBossDrop = 0.15f, trinketEliteDrop = 0.03f;   // chance a boss / elite drops a trinket you don't own
        public float merchantTrinketChance = 0.4f;            // chance the travelling merchant has a trinket for sale
        // ── cooking v9: grades (ingredients Plain / Fine / Prime, meals Common / Uncommon / Rare), the rarity roll, accelerators, the meal pantry ──
        public float gradeMultUncommon = 1.1f, gradeMultRare = 1.2f;   // a meal's stats × its quality (Fine / Prime): small on purpose, tiers are the big jump
        public float qualityFineChance = 0.2f, qualityPrimeChance = 0.05f;   // Design v2: a cook's chance of a Fine / Prime meal (plus the station level's bonus)
        public float qualityNudgeFine = 0.15f, qualityNudgePrime = 0.1f;   // the cooking minigame's best result adds these (ticket 11)
        public float guardMult = 0.5f;   // damage taken from a guarded element
        public float jackpotChance = 0.05f;                    // chance a cooked meal lands one grade higher than rolled
        public int mealPantrySlots = 6;                        // cooked meals kept at camp
        // ingredient grade drops (contentVersion 14): chance per pickup that it's Fine ("Uncommon") / Prime ("Rare").
        // Rises with each floor down and the route's difficulty; elites, bosses, perfect gathers and Lucky routes add Prime.
        public float dropUncommon = 0.10f, dropUncommonPerFloor = 0.15f, dropUncommonPerDifficulty = 0.03f;
        public float dropRare = 0f, dropRarePerFloor = 0.08f, dropRarePerDifficulty = 0.015f;
        public float dropEliteRareBonus = 0.10f, dropBossRareBonus = 0.35f, dropPerfectGatherRare = 0.15f, dropLuckyRare = 0.05f;
        public float guardDropChance = 0.25f;                 // hazard routes: a monster's ingredient drop is the matching guard accelerator (nettle, mint…)
        public float deathKeepFraction = 0.25f;               // falling: you keep this share of each stack in the bag (fleeing / heading home keeps it all)
        // ── kitchen upgrades (contentVersion 13) ──
        public float stationJackpotPerLevel = 0.03f;          // each station level above 1 adds this to that station's better-meal (jackpot) chance
        public float materialDropChance = 0.15f, materialDropPerFloor = 0.10f;   // chance a normal monster drops a building material (+ per floor down)
        public int eliteMaterials = 1, bossMaterials = 3;      // elites always drop this many, bosses this many
        // ── town fishing pond: one quick catch a day ──
        public string fishIngredient = "fish";
        public int fishCatchCount = 2;
        public float fishGoodWidth = 0.34f, fishPerfectWidth = 0.12f;   // timing bar: good zone → Fine, perfect (centre) → Prime, else Plain
        public float fishBarSpeed = 1.1f;
    }

    [Serializable] public class GameContent
    {
        public TuningData tuning = new TuningData();
        public List<ClassData> classes = new List<ClassData>();
        public List<IngredientData> ingredients = new List<IngredientData>();
        public List<RecipeData> recipes = new List<RecipeData>();
        public List<SynergyData> synergies = new List<SynergyData>();
        public List<MonsterData> monsters = new List<MonsterData>();
        public List<RouteData> routes = new List<RouteData>();
        public List<GearTier> weaponTiers = new List<GearTier>();
        public List<GearTier> armourTiers = new List<GearTier>();
        public List<IngredientCost> starterPantry = new List<IngredientCost>();
        public int starterCoins = 30;
        public List<MoveData> moves = new List<MoveData>();
        public List<SkillNodeData> skillNodes = new List<SkillNodeData>();
        public int contentVersion;   // 2 = moves + skill trees + level 50; 3 = meals-first rebalance; 4 = story + side dungeons; 5 = darkness, mutators, 4 more side dungeons; 6 = bag tiers; 7 = head/boots + trinkets; 8 = meal rarity (DefaultContent.Upgrade)
        public List<KeyItemData> keyItems = new List<KeyItemData>();
        public List<GearTier> bagTiers = new List<GearTier>();   // bag upgrades: value = slots (contentVersion 6)
        public string storyTitle;
        public string storyPrologue;
        public List<GearTier> headTiers = new List<GearTier>();    // Workshop: value = max HP (contentVersion 7)
        public List<GearTier> bootsTiers = new List<GearTier>();   // Workshop: value = move speed fraction (0.05 = +5%)
        public List<TrinketData> trinkets = new List<TrinketData>();
        public List<KitchenTier> stationTiers = new List<KitchenTier>();   // levels 1..5 for every station (index 0 = level 1, free)
        public List<KitchenTier> pantryTiers = new List<KitchenTier>();    // meal-pantry shelves (value = slots; index 0 = the starting size)
    }

    // ───────────────────────── Persistent profile ─────────────────────────
    [Serializable] public class ItemStack { public string ingredientId; public int count; public int grade; }   // ingredient grade 0 Plain · 1 Fine · 2 Prime

    /// A cooked meal waiting in the meal pantry (or packed on the tray). Its grade was rolled when cooked.
    [Serializable] public class CookedMeal
    {
        public string recipeId; public int grade;
        public string accelId; public int accelGrade;   // optional accelerator (empty = none); accelGrade unused (accelerators have no grade)
    }

    [Serializable] public class NodeRank { public string nodeId; public int rank; }

    [Serializable] public class ClassProgress
    {
        public ClassId classId;   // (level, xp and the skill tree were cut in Design v2, ticket 04)
        public int weaponTier; public int armourTier;   // indexes into content tiers (armour = Body)
        public int headTier, bootsTier;                 // Workshop head / boots tiers (saveVersion 5; 0 for old saves)
        public string loadAttack, loadSkill1, loadSkill2, loadDash;   // equipped move ids (Skill2 may be borrowed)
    }

    /// Combat bonuses from the skill tree, used by the real-time dungeon.
    [Serializable] public class CombatMods
    {
        public float attackPower = 1f, skillPower = 1f;          // multipliers on move power
        public float skillCooldown = 1f, dashCooldown = 1f;      // multipliers on cooldowns
        public float moveSpeed = 1f;
        public int healOnKill;
    }

    [Serializable] public partial class Profile
    {
        public int coins;
        public bool hasBackpack;
        public ClassId selectedClass;
        public List<ClassProgress> classes = new List<ClassProgress>();
        public List<ItemStack> pantry = new List<ItemStack>();     // unlimited camp storage
        public List<string> tray = new List<string>();             // recipe ids, max traySize
        public List<string> clearedRouteIds = new List<string>();
        public int runsStarted, runsCleared, day = 1;
        public HearthState camp;   // today's camp (event, merchant stock, route mods) — saved so relaunching doesn't reroll it
        public int campDay;        // day the saved camp belongs to
        public int saveVersion;    // 2 = tray meals only reserve ingredients; they're spent at set-out · 3 = skill trees + loadouts · 4 = bag tiers
        public List<string> keyItems = new List<string>();   // story key items found (kept forever)
        public List<KnownElement> knownElements = new List<KnownElement>();   // element discovery (saveVersion 9): how each element works on each kind of monster
        public int bagTier;          // index into content bagTiers (saveVersion 4; replaces hasBackpack)
        public List<string> trinkets = new List<string>();          // trinkets owned (kept forever)
        public List<string> equippedTrinkets = new List<string>();  // worn, shared by every class (max = Gear.TrinketSlots)
        public List<CookedMeal> mealPantry = new List<CookedMeal>();  // cooked meals kept at camp (saveVersion 6)
        public List<CookedMeal> packed = new List<CookedMeal>();      // the tray: cooked meals packed for the next run (tray mirrors their recipe ids)
        public List<int> stationTier = new List<int>();               // kitchen: index per Station (Pot, BBQ, Sandwich); 0 = level 1
        public int pantryTier;                                        // kitchen: index into content pantryTiers
        public int lastFishDay;                                       // town fishing pond: the day of the last catch (one catch a day)
        public bool metWren;                                          // town: Old Wren has given her first-meeting tutorial
    }

    // ───────────────────────── Camp (per visit) ─────────────────────────
    [Serializable] public class RouteOffer { public string routeId; public RouteMod mod; }

    [Serializable] public partial class HearthState
    {
        public CampEvent campEvent;
        public List<MerchantOffer> merchant = new List<MerchantOffer>();
        public List<RouteOffer> routeOffers = new List<RouteOffer>(); // one per route; locked ones still listed
        public List<ItemStack> gift = new List<ItemStack>();        // Forager event: ingredients added to the pantry
        public string merchantTrinket; public int merchantTrinketPrice; public bool merchantTrinketSold;   // merchant's trinket (if any)
    }

    // ───────────────────────── Run ─────────────────────────
    [Serializable] public class Tile
    {
        public TileType type; public bool revealed; public bool resolved;
        public int difficulty;          // final difficulty (route base + req offset [+ mod])
        public string monsterId;        // Monster / Elite tiles
    }

    [Serializable] public class EatenMeal { public string recipeId; public float potency; public int grade; public string accelId; public int accelGrade; } // potency after diminishing returns

    [Serializable] public class PlayerStats   // derived, recomputed by StatCalc
    {
        public int maxHp; public float damage; public float armourPct;
        public float critChance; public float gatherZoneMult; public float coinMult; public float xpMult;
    }

    [Serializable] public partial class FightState
    {
        public string monsterId; public bool elite; public int difficulty;
        public int monsterHp, monsterMaxHp; public float monsterAtk;
        public bool lastStrikeMissed;     // next Strike uses strikeHitAfterMiss
        public bool monsterWindingUp;     // dragon: next attack ×windUpMult
        public int skillCooldown;         // turns until skill usable (0 = ready)
        public int turn;
        public bool over; public bool playerWon;
    }

    [Serializable] public partial class GatherState
    {
        public string ingredientId;      // what this tile yields
        public float safeCenter, safeWidth, perfectWidth; // perfect zone centred inside safe zone
        public float barSpeed;
    }

    [Serializable] public class BagSlot { public string ingredientId; public int count; public int grade; } // empty: ingredientId null/"" and count 0

    [Serializable] public partial class RunState
    {
        public string routeId; public RouteMod mod; public ClassId classId; public bool storm; public bool harvestMoon;
        public Element element;   // the run's infused element (from a meal, ticket 08); attacks without their own element use it
        public List<Element> guards = new List<Element>();   // elements the run's meals guard against: those attacks hit you for half
        public int width, height;
        public List<Tile> tiles = new List<Tile>();   // index = y*width + x
        public int px, py;                              // player position
        public int hp;
        public List<string> tray = new List<string>();  // unused during a run: the tray is turned into buffs at set-out
        public List<EatenMeal> eaten = new List<EatenMeal>(); // meals eaten at camp before setting out (their buffs last the whole run)
        public List<string> activeSynergyIds = new List<string>();
        public List<BagSlot> bag = new List<BagSlot>();
        public int roomsCleared, kills;
        public int coinsEarned, killXp;   // accumulated during run (not yet banked)
        public FightState fight;          // non-null while Phase == Fight
        public GatherState gather;        // non-null while Phase == Gather
        public RunOutcome outcome;
        public int skillCooldown;         // carried between fights
        public List<string> trinketsDropped = new List<string>();   // trinkets dropped this run (already added to the profile)
        public int torches = -1;          // (unused since torches went unlimited on a cooldown — kept so saves load)
    }

    [Serializable] public class RunSummary
    {
        public RunOutcome outcome; public string routeId; public ClassId classId;
        public int roomsCleared, kills;
        public int coinsFromKills, clearCoins, totalCoins;
        public List<ItemStack> goods = new List<ItemStack>();
        public List<string> unlockedRouteIds = new List<string>();
        public int floorReached, floorsTotal;   // real-time dungeon (0 for old grid runs)
        public bool firstClear;                 // first time this route was cleared
        public string keyItemFound;             // key item given by this clear (null = none)
        public string storyText;                // the route's story outro on a first clear
        public List<string> trinketsFound = new List<string>();   // trinkets found this run (drops + first-clear reward)
        public List<ItemStack> lost = new List<ItemStack>();       // bag contents lost in a fall (contentVersion 14)
    }

    [Serializable] public class GameEvent
    {
        public GameEventType type; public float amount; public bool onPlayer; public string text;
        public GameEvent() { }
        public GameEvent(GameEventType t, float a = 0, bool onPlayer = false, string text = null)
        { type = t; amount = a; this.onPlayer = onPlayer; this.text = text; }
    }

    /// Seeded RNG so runs are reproducible and Core stays Unity-free.
    [Serializable] public class Rng
    {
        public int seed; [NonSerialized] private Random _r;
        public Rng(int seed) { this.seed = seed; _r = new Random(seed); }
        private Random R => _r ?? (_r = new Random(seed));
        public float Value() => (float)R.NextDouble();                 // [0,1)
        public int Range(int minIncl, int maxIncl) => R.Next(minIncl, maxIncl + 1);
        public bool Chance(float p) => R.NextDouble() < p;
        public T Pick<T>(IList<T> list) => list[R.Next(list.Count)];
    }

    /// Lookups shared by all systems.
    public static class ContentLookup
    {
        public static ClassData Class(this GameContent c, ClassId id) => c.classes.Find(x => x.id == id);
        public static RecipeData Recipe(this GameContent c, string id) => c.recipes.Find(x => x.id == id);
        public static IngredientData Ingredient(this GameContent c, string id) => c.ingredients.Find(x => x.id == id);
        public static MonsterData Monster(this GameContent c, string id) => c.monsters.Find(x => x.id == id);
        public static RouteData Route(this GameContent c, string id) => c.routes.Find(x => x.id == id);
        public static ClassProgress Progress(this Profile p, ClassId id)
        {
            var cp = p.classes.Find(x => x.classId == id);
            if (cp == null) { cp = new ClassProgress { classId = id }; p.classes.Add(cp); }
            return cp;
        }
        public static Tile TileAt(this RunState r, int x, int y) => r.tiles[y * r.width + x];
        public static bool InBounds(this RunState r, int x, int y) => x >= 0 && y >= 0 && x < r.width && y < r.height;
    }
}
