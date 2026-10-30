// Hearth & Hollow — default moves and skill trees for the real-time dungeon (level 1–50). Pure C#.
// Move numbers by kind (power × player damage; cooldown in seconds):
//   Swing     range = reach, radius = hit radius
//   Lunge     range = step forward, radius = thrust radius
//   Heavy     range = reach, radius = hit radius, duration = stun
//   Arrow / PierceArrow  range = arrow speed
//   SpreadArrow          range = arrow speed, value = spread angle (3 arrows)
//   Roll / ChargeDash / Vault / SlickDash  cooldown; ChargeDash power on contact; Vault power = arrow; SlickDash duration + value = slow
//   Cleave    radius          Shockwave range = length, radius = width, duration = stun
//   WarCry    duration, value = +damage   ShieldWall duration, value = damage taken
//   Whirlwind radius, duration, value = seconds between hits
//   Volley    power per arrow (2, piercing)   Trap radius, duration = root, value = trap lifetime
//   Mark      range, duration, value = +damage taken   ArrowRain range = distance, radius, duration
//   Smoke     radius, duration, value = enemy slow (fraction)
//   Snack     value = heal, duration = guard   PepperToss range, radius = cone half-angle°, duration + value = slow
//   HotPot    range, radius, duration, value = seconds between burns   Picnic radius, duration, value = HP per half second
//   RollingPin range = distance out, value = speed
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public static partial class DefaultContent
    {
        /// Brings an older content asset up to date (adds moves, trees and the level-50 tuning). Safe to call every load.
        public static bool Upgrade(GameContent c)
        {
            if (c == null || c.contentVersion >= LatestContentVersion) return false;
            if (c.contentVersion < 2) Upgrade2(c);
            if (c.contentVersion < 3) Upgrade3(c);   // DefaultContentMeals.cs
            if (c.contentVersion < 4) { ApplyStory(c); c.contentVersion = 4; }   // DefaultContentStory.cs
            if (c.contentVersion < 5) { ApplyStory5(c); c.contentVersion = 5; }   // DefaultContentStory5.cs
            if (c.contentVersion < 6) { BuildBags(c); c.contentVersion = 6; }     // bag tiers
            if (c.contentVersion < 7) { BuildGear7(c); c.contentVersion = 7; }    // head + boots, trinkets (DefaultContentGear.cs)
            if (c.contentVersion < 8) { AssignRarity(c); c.contentVersion = 8; }  // meal rarity (DefaultContentGear.cs)
            return true;
        }

        static void Upgrade2(GameContent c)
        {
            if (c.moves == null) c.moves = new List<MoveData>();
            if (c.skillNodes == null) c.skillNodes = new List<SkillNodeData>();
            if (c.moves.Count == 0 && c.skillNodes.Count == 0) BuildMovesAndTrees(c);
            // only replace numbers still at the old defaults — anything you customised in the asset is kept
            var fresh = new TuningData(); var t = c.tuning;
            if (t.levelCap == 10) t.levelCap = fresh.levelCap;
            if (t.hpPerLevel == 2f) { t.hpPerLevelEarly = 2; t.hpPerLevel = 1f; }   // the v2 values; Upgrade3 then lowers them
            else t.hpPerLevelEarly = (int)t.hpPerLevel;
            if (t.skillUnlockLevel == 2) t.skillUnlockLevel = fresh.skillUnlockLevel;
            if (t.xpPerLevel == 20) t.xpPerLevel = fresh.xpPerLevel;
            c.contentVersion = 2;
        }

        static void BuildMovesAndTrees(GameContent c)
        {
            BuildMoves(c);
            SetClassKit(c, ClassId.Warden, "w_sweep", "w_cleave", "roll", "Blade", "Bulwark", "Fury");
            SetClassKit(c, ClassId.Ranger, "r_shot", "r_volley", "roll", "Marksman", "Wayfinder", "Trickster");
            SetClassKit(c, ClassId.Cook, "c_ladle", "c_snack", "roll", "Brawler", "Comfort", "Spice");
            BuildTrees(c);
            c.contentVersion = LatestContentVersion;
        }

        static void SetClassKit(GameContent c, ClassId id, string atk, string skill, string dash, string b0, string b1, string b2)
        {
            var cls = c.classes.Find(x => x.id == id);
            if (cls == null) return;
            cls.starterAttack = atk; cls.starterSkill = skill; cls.starterDash = dash;
            cls.branchNames = new List<string> { b0, b1, b2 };
        }

        static MoveData M(GameContent c, string id, ClassId owner, MoveSlot slot, MoveKind kind, string name, string emoji, string desc,
                          float power, float cooldown, float range = 1f, float radius = 1f, float duration = 0f, float value = 0f)
        {
            var m = new MoveData { id = id, owner = owner, slot = slot, kind = kind, name = name, emoji = emoji, description = desc,
                                   power = power, cooldown = cooldown, range = range, radius = radius, duration = duration, value = value };
            c.moves.Add(m);
            return m;
        }

        // ───────────── moves ─────────────
        static void BuildMoves(GameContent c)
        {
            var W = ClassId.Warden; var R = ClassId.Ranger; var K = ClassId.Cook;
            var A = MoveSlot.Attack; var S = MoveSlot.Skill; var D = MoveSlot.Dash;

            M(c, "roll", W, D, MoveKind.Roll, "Roll", "💨", "A quick dodge roll. You can't be hit mid-roll.", 0f, 0.7f).anyClass = true;

            // Warden
            M(c, "w_sweep", W, A, MoveKind.Swing, "Sweep", "⚔️", "A broad sword swing in front of you.", 1.0f, 0.42f, 0.85f, 0.85f);
            M(c, "w_lunge", W, A, MoveKind.Lunge, "Lunge", "🗡️", "Step forward with a long thrust. Great reach.", 1.2f, 0.55f, 1.5f, 0.7f);
            M(c, "w_maul", W, A, MoveKind.Heavy, "Maul", "🔨", "Slow, crushing blow. Stuns what it hits.", 2.3f, 1.0f, 0.9f, 1.0f, 0.6f);
            M(c, "w_cleave", W, S, MoveKind.Cleave, "Cleave", "🪓", "Spin your blade: ×1.75 damage to everything around you.", 1.75f, 6f, 1f, 2.2f);
            M(c, "w_slam", W, S, MoveKind.Shockwave, "Ground Slam", "💥", "A shockwave rolls forward, damaging and stunning everything in its path.", 1.4f, 8f, 4f, 0.9f, 1.2f);
            M(c, "w_warcry", W, S, MoveKind.WarCry, "War Cry", "📯", "Roar: +40% damage for 6 seconds.", 0f, 14f, 1f, 1f, 6f, 0.4f);
            M(c, "w_shieldwall", W, S, MoveKind.ShieldWall, "Shield Wall", "🛡️", "Brace: take 70% less damage for 3 seconds.", 0f, 12f, 1f, 1f, 3f, 0.3f);
            M(c, "w_whirl", W, S, MoveKind.Whirlwind, "Whirlwind", "🌪️", "Spin for 1.5 seconds, hitting everything nearby again and again. You can still move.", 0.5f, 10f, 1f, 1.6f, 1.5f, 0.25f);
            M(c, "w_charge", W, D, MoveKind.ChargeDash, "Shoulder Charge", "🐂", "Barge forward, hurting and shoving anything you run through.", 0.8f, 1.1f);

            // Ranger
            M(c, "r_shot", R, A, MoveKind.Arrow, "Shot", "🏹", "A single arrow.", 1.0f, 0.42f, 13f);
            M(c, "r_spread", R, A, MoveKind.SpreadArrow, "Spread Shot", "🎐", "Three arrows in a fan. Weaker each, hard to dodge.", 0.55f, 0.6f, 12f, 1f, 0f, 12f);
            M(c, "r_longbow", R, A, MoveKind.PierceArrow, "Longbow", "🎯", "A slow, heavy arrow that flies through every enemy in a line.", 1.6f, 0.85f, 18f);
            M(c, "r_volley", R, S, MoveKind.Volley, "Volley", "🏹", "Two piercing arrows for ×0.75 damage each.", 0.75f, 6f);
            M(c, "r_trap", R, S, MoveKind.Trap, "Snare Trap", "🪤", "Set a snare at your feet. The first enemy to step in is hurt and pinned for 2.5 seconds.", 1.0f, 9f, 1f, 0.9f, 2.5f, 12f);
            M(c, "r_mark", R, S, MoveKind.Mark, "Hunter's Mark", "🔖", "Mark the nearest enemy: it takes +50% damage for 6 seconds.", 0f, 10f, 8f, 1f, 6f, 0.5f);
            M(c, "r_rain", R, S, MoveKind.ArrowRain, "Arrow Rain", "🌧️", "Arrows fall on a spot ahead of you for 1.5 seconds.", 0.45f, 12f, 3f, 1.8f, 1.5f);
            M(c, "r_smoke", R, S, MoveKind.Smoke, "Smoke Bomb", "🌫️", "Enemies nearby are slowed by half for 3 seconds and you speed up.", 0f, 12f, 1f, 3f, 3f, 0.5f);
            M(c, "r_vault", R, D, MoveKind.Vault, "Back Vault", "🤸", "Flip backwards and loose an arrow at whatever you were facing.", 0.8f, 1.0f);

            // Cook
            M(c, "c_ladle", K, A, MoveKind.Swing, "Ladle Bonk", "🥄", "A quick bonk with the ladle.", 1.0f, 0.4f, 0.75f, 0.75f);
            M(c, "c_skewer", K, A, MoveKind.Lunge, "Skewer", "🍢", "Jab forward with a kebab skewer.", 1.1f, 0.5f, 1.3f, 0.6f);
            M(c, "c_pan", K, A, MoveKind.Heavy, "Frying Pan", "🍳", "A ringing pan to the head. Stuns.", 1.9f, 0.9f, 0.85f, 0.95f, 0.8f);
            M(c, "c_snack", K, S, MoveKind.Snack, "Hearty Snack", "🥐", "Heal 12 and take half damage for 3 seconds.", 0f, 6f, 1f, 1f, 3f, 12f);
            M(c, "c_pepper", K, S, MoveKind.PepperToss, "Pepper Toss", "🌶️", "Fling pepper in a cone: hurts and slows enemies by half for 3 seconds.", 0.6f, 7f, 3f, 35f, 3f, 0.5f);
            M(c, "c_hotpot", K, S, MoveKind.HotPot, "Hot Pot", "🍲", "Lob a pot ahead. It leaves a scalding puddle for 4 seconds.", 0.3f, 10f, 4f, 1.5f, 4f, 0.5f);
            M(c, "c_picnic", K, S, MoveKind.Picnic, "Picnic", "🧺", "Spread a blanket for 5 seconds. Stand on it to heal 2 HP every half second.", 0f, 18f, 1f, 2f, 5f, 2f);
            M(c, "c_pin", K, S, MoveKind.RollingPin, "Rolling Pin", "🪵", "Throw a rolling pin that flies out and comes back, hitting everything twice.", 0.9f, 6f, 5f, 1f, 0f, 10f);
            M(c, "c_slide", K, D, MoveKind.SlickDash, "Butter Slide", "🧈", "Slide along a buttery trail. Enemies on it are slowed for 3 seconds.", 0f, 1.0f, 1f, 1f, 3f, 0.6f);
        }

        // ───────────── skill trees ─────────────
        static SkillNodeData N(GameContent c, ClassId cls, int branch, int row, int level, string id, string name, string emoji, string desc,
                               NodeEffect effect, float perRank = 0f, int ranks = 1, string requires = null)
        {
            var n = new SkillNodeData { id = id, classId = cls, branch = branch, row = row, minLevel = level, name = name, emoji = emoji,
                                        description = desc, effect = effect, valuePerRank = perRank, maxRank = ranks, cost = 1, requires = requires };
            c.skillNodes.Add(n);
            return n;
        }
        static void Move(GameContent c, ClassId cls, int branch, int row, int level, string id, string moveId, string requires)
        {
            var m = c.moves.Find(x => x.id == moveId);
            var n = N(c, cls, branch, row, level, id, m != null ? m.name : moveId, m != null ? m.emoji : "✨",
                      "New " + (m != null ? m.slot.ToString().ToLower() : "move") + ": " + (m != null ? m.description : ""), NodeEffect.UnlockMove, 0f, 1, requires);
            n.moveId = moveId;
        }
        static void Stat(GameContent c, ClassId cls, int branch, int row, int level, string id, string name, string emoji, string desc,
                         BuffStat stat, float perRank, int ranks, string requires)
        {
            N(c, cls, branch, row, level, id, name, emoji, desc, NodeEffect.Stat, perRank, ranks, requires).stat = stat;
        }

        static void BuildTrees(GameContent c)
        {
            var W = ClassId.Warden; var R = ClassId.Ranger; var K = ClassId.Cook;

            // ── Warden: Blade (attacks) · Bulwark (toughness) · Fury (skills) ──
            Stat(c, W, 0, 0, 1, "w_whetstone", "Whetstone", "🪨", "+2% damage per rank.", BuffStat.DamagePct, 0.02f, 5, null);
            Move(c, W, 0, 1, 5, "w_n_lunge", "w_lunge", "w_whetstone");
            Stat(c, W, 0, 2, 10, "w_keen", "Keen Edge", "✨", "+2% crit chance per rank.", BuffStat.CritPct, 0.02f, 5, "w_n_lunge");
            N(c, W, 0, 3, 20, "w_honed", "Honed Strikes", "⚔️", "Attacks hit 5% harder per rank.", NodeEffect.AttackPower, 0.05f, 3, "w_keen");
            Move(c, W, 0, 4, 25, "w_n_maul", "w_maul", "w_honed");
            Stat(c, W, 0, 5, 35, "w_brutal", "Brutal Force", "💪", "+2% damage per rank.", BuffStat.DamagePct, 0.02f, 5, "w_n_maul");
            N(c, W, 0, 6, 45, "w_warlord", "Warlord's Edge", "👑", "Attacks hit 15% harder.", NodeEffect.AttackPower, 0.15f, 1, "w_brutal");

            Stat(c, W, 1, 0, 1, "w_hide", "Thick Hide", "🐗", "+1 max HP per rank.", BuffStat.MaxHp, 1f, 5, null);
            Stat(c, W, 1, 1, 10, "w_iron", "Iron Skin", "🔩", "+1 armour per rank (5% less damage each).", BuffStat.Armour, 1f, 5, "w_hide");
            Move(c, W, 1, 2, 15, "w_n_charge", "w_charge", "w_iron");
            Move(c, W, 1, 3, 20, "w_n_shieldwall", "w_shieldwall", "w_n_charge");
            N(c, W, 1, 4, 30, "w_secondwind", "Second Wind", "🌬️", "Heal 1 HP per rank whenever you defeat an enemy.", NodeEffect.HealOnKill, 1f, 3, "w_n_shieldwall");
            Stat(c, W, 1, 5, 40, "w_bastion", "Bastion", "🏰", "+2 max HP per rank.", BuffStat.MaxHp, 2f, 3, "w_secondwind");
            Stat(c, W, 1, 6, 50, "w_unbreakable", "Unbreakable", "🗿", "+2 armour.", BuffStat.Armour, 2f, 1, "w_bastion");

            N(c, W, 2, 0, 1, "w_rhythm", "Battle Rhythm", "🥁", "Skills recharge 4% faster per rank.", NodeEffect.SkillCooldown, 0.04f, 5, null);
            Move(c, W, 2, 1, 5, "w_n_slam", "w_slam", "w_rhythm");
            N(c, W, 2, 2, 10, "w_might", "Mighty Swings", "🪓", "Skills hit 5% harder per rank.", NodeEffect.SkillPower, 0.05f, 5, "w_n_slam");
            Move(c, W, 2, 3, 15, "w_n_warcry", "w_warcry", "w_might");
            Move(c, W, 2, 4, 30, "w_n_whirl", "w_whirl", "w_n_warcry");
            N(c, W, 2, 5, 35, "w_relentless", "Relentless", "🔥", "Skills recharge 5% faster per rank.", NodeEffect.SkillCooldown, 0.05f, 3, "w_n_whirl");
            N(c, W, 2, 6, 45, "w_berserker", "Berserker", "😤", "Skills hit 15% harder.", NodeEffect.SkillPower, 0.15f, 1, "w_relentless");

            // ── Ranger: Marksman (attacks) · Wayfinder (mobility & foraging) · Trickster (skills) ──
            Stat(c, R, 0, 0, 1, "r_steady", "Steady Aim", "🎯", "+2% crit chance per rank.", BuffStat.CritPct, 0.02f, 5, null);
            Move(c, R, 0, 1, 5, "r_n_spread", "r_spread", "r_steady");
            Stat(c, R, 0, 2, 10, "r_fletching", "Fletching", "🪶", "+2% damage per rank.", BuffStat.DamagePct, 0.02f, 5, "r_n_spread");
            N(c, R, 0, 3, 20, "r_quickdraw", "Quick Draw", "🏹", "Attacks hit 5% harder per rank.", NodeEffect.AttackPower, 0.05f, 3, "r_fletching");
            Move(c, R, 0, 4, 25, "r_n_longbow", "r_longbow", "r_quickdraw");
            Stat(c, R, 0, 5, 35, "r_deadeye", "Deadeye", "👁️", "+2% damage per rank.", BuffStat.DamagePct, 0.02f, 5, "r_n_longbow");
            N(c, R, 0, 6, 45, "r_hawkeye", "Hawkeye", "🦅", "Attacks hit 15% harder.", NodeEffect.AttackPower, 0.15f, 1, "r_deadeye");

            N(c, R, 1, 0, 1, "r_lightpack", "Light Pack", "🎒", "Move 3% faster per rank.", NodeEffect.MoveSpeed, 0.03f, 5, null);
            Stat(c, R, 1, 1, 10, "r_eye", "Forager's Eye", "🌿", "Gather zones 8% wider per rank.", BuffStat.GatherZonePct, 0.08f, 5, "r_lightpack");
            Move(c, R, 1, 2, 15, "r_n_vault", "r_vault", "r_eye");
            Move(c, R, 1, 3, 20, "r_n_mark", "r_mark", "r_n_vault");
            Stat(c, R, 1, 4, 30, "r_scavenger", "Scavenger", "🪙", "+5% coins per rank.", BuffStat.CoinPct, 0.05f, 3, "r_n_mark");
            Stat(c, R, 1, 5, 40, "r_hardened", "Trail Hardened", "🥾", "+2 max HP per rank.", BuffStat.MaxHp, 2f, 3, "r_scavenger");
            N(c, R, 1, 6, 50, "r_fleet", "Fleet Foot", "🍃", "Dash recharges 25% faster.", NodeEffect.DashCooldown, 0.25f, 1, "r_hardened");

            N(c, R, 2, 0, 1, "r_nimble", "Nimble Fingers", "🖐️", "Skills recharge 4% faster per rank.", NodeEffect.SkillCooldown, 0.04f, 5, null);
            Move(c, R, 2, 1, 5, "r_n_trap", "r_trap", "r_nimble");
            N(c, R, 2, 2, 10, "r_barbed", "Barbed Tips", "🪝", "Skills hit 5% harder per rank.", NodeEffect.SkillPower, 0.05f, 5, "r_n_trap");
            Move(c, R, 2, 3, 15, "r_n_rain", "r_rain", "r_barbed");
            Move(c, R, 2, 4, 30, "r_n_smoke", "r_smoke", "r_n_rain");
            N(c, R, 2, 5, 35, "r_pockets", "Hidden Pockets", "👝", "Skills recharge 5% faster per rank.", NodeEffect.SkillCooldown, 0.05f, 3, "r_n_smoke");
            N(c, R, 2, 6, 45, "r_mastertrap", "Master Trapper", "🧰", "Skills hit 15% harder.", NodeEffect.SkillPower, 0.15f, 1, "r_pockets");

            // ── Cook: Brawler (attacks) · Comfort (toughness & healing) · Spice (skills) ──
            Stat(c, K, 0, 0, 1, "c_grease", "Elbow Grease", "💪", "+2% damage per rank.", BuffStat.DamagePct, 0.02f, 5, null);
            Move(c, K, 0, 1, 5, "c_n_skewer", "c_skewer", "c_grease");
            Stat(c, K, 0, 2, 10, "c_tenderiser", "Tenderiser", "🔨", "+2% crit chance per rank.", BuffStat.CritPct, 0.02f, 5, "c_n_skewer");
            N(c, K, 0, 3, 20, "c_heavyhand", "Heavy Hand", "🥄", "Attacks hit 5% harder per rank.", NodeEffect.AttackPower, 0.05f, 3, "c_tenderiser");
            Move(c, K, 0, 4, 25, "c_n_pan", "c_pan", "c_heavyhand");
            Stat(c, K, 0, 5, 35, "c_fury", "Chef's Fury", "😠", "+2% damage per rank.", BuffStat.DamagePct, 0.02f, 5, "c_n_pan");
            N(c, K, 0, 6, 45, "c_ironchef", "Iron Chef", "👨‍🍳", "Attacks hit 15% harder.", NodeEffect.AttackPower, 0.15f, 1, "c_fury");

            Stat(c, K, 1, 0, 1, "c_wellfed", "Well Fed", "🍞", "+1 max HP per rank.", BuffStat.MaxHp, 1f, 5, null);
            Stat(c, K, 1, 1, 10, "c_apron", "Thick Apron", "🧥", "+1 armour per rank (5% less damage each).", BuffStat.Armour, 1f, 5, "c_wellfed");
            Move(c, K, 1, 2, 15, "c_n_slide", "c_slide", "c_apron");
            Move(c, K, 1, 3, 20, "c_n_picnic", "c_picnic", "c_n_slide");
            N(c, K, 1, 4, 30, "c_leftovers", "Leftovers", "🥡", "Heal 1 HP per rank whenever you defeat an enemy.", NodeEffect.HealOnKill, 1f, 3, "c_n_picnic");
            Stat(c, K, 1, 5, 40, "c_seconds", "Second Helpings", "🍲", "+2 max HP per rank.", BuffStat.MaxHp, 2f, 3, "c_leftovers");
            Stat(c, K, 1, 6, 50, "c_homecooking", "Home Cooking", "🏡", "+2 armour.", BuffStat.Armour, 2f, 1, "c_seconds");

            N(c, K, 2, 0, 1, "c_mise", "Mise en Place", "🔪", "Skills recharge 4% faster per rank.", NodeEffect.SkillCooldown, 0.04f, 5, null);
            Move(c, K, 2, 1, 5, "c_n_pepper", "c_pepper", "c_mise");
            N(c, K, 2, 2, 10, "c_seasoning", "Seasoning", "🧂", "Skills hit 5% harder per rank.", NodeEffect.SkillPower, 0.05f, 5, "c_n_pepper");
            Move(c, K, 2, 3, 15, "c_n_hotpot", "c_hotpot", "c_seasoning");
            Move(c, K, 2, 4, 30, "c_n_pin", "c_pin", "c_n_hotpot");
            N(c, K, 2, 5, 35, "c_preptime", "Prep Time", "⏲️", "Skills recharge 5% faster per rank.", NodeEffect.SkillCooldown, 0.05f, 3, "c_n_pin");
            N(c, K, 2, 6, 45, "c_signature", "Signature Dish", "🌟", "Skills hit 15% harder.", NodeEffect.SkillPower, 0.15f, 1, "c_preptime");
        }
    }
}
