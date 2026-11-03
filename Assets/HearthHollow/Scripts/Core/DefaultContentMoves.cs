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
            if (c.contentVersion < 9) { BuildCooking9(c); c.contentVersion = 9; }  // cooking overhaul: 21 dishes + accelerators (DefaultContentCooking.cs)
            if (c.contentVersion < 11) { FixBread(c); c.contentVersion = 11; }  // Camp Bread: an always-available meal (DefaultContentCooking.cs)
            if (c.contentVersion < 12) { RenameGrades12(c); c.contentVersion = 12; }  // grades are Plain / Fine / Prime for meals too (DefaultContentCooking.cs)
            if (c.contentVersion < 13) { BuildKitchen13(c); c.contentVersion = 13; }  // kitchen upgrades + building materials (DefaultContentKitchen.cs)
            if (c.contentVersion < 14) { RiskReward14(c); c.contentVersion = 14; }  // real drop odds + falling costs the bag (DefaultContentKitchen.cs)
            if (c.contentVersion < 15) { Rework15(c); c.contentVersion = 15; }  // class rework: every move lights the world, new kits (below)
            if (c.contentVersion < 16) { Lamplighter16(c); c.contentVersion = 16; }  // the Lamplighter class (below)
            if (c.contentVersion < 17) { Emberfist17(c); c.contentVersion = 17; }  // the Emberfist class (below)
            if (c.contentVersion < 18) { Joins18(c); c.contentVersion = 18; }  // who joins your camp when (below)
            if (c.contentVersion < 19) { Balance19(c); c.contentVersion = 19; }  // class balance pass + alternate attacks (below)
            if (c.contentVersion < 20) { Ultimates20(c); c.contentVersion = 20; }  // ultimates + faster skills (DefaultContentUltimates.cs)
            if (c.contentVersion < 21) { Colours21(c); c.contentVersion = 21; }  // class colour themes + bigger, brighter basic moves (DefaultContentColours.cs)
            if (c.contentVersion < 22) { Colours22(c); c.contentVersion = 22; }  // stronger, more saturated class colours (DefaultContentColours.cs)
            if (c.contentVersion < 23) { UltCooldown23(c); c.contentVersion = 23; }  // ultimates recharge twice as fast (DefaultContentUltimates.cs)
            if (c.contentVersion < 24) { Speed24(c); c.contentVersion = 24; }  // opening arc: slower start, speed gear doubled (DefaultContentArc.cs)
            if (c.contentVersion < 25) { DesignV2Cuts25(c); c.contentVersion = 25; }
            if (c.contentVersion < 26) { Elements26(c); c.contentVersion = 26; }  // moves carry an element (DefaultContentDesignV2.cs)
            if (c.contentVersion < 27) { Cooking27(c); c.contentVersion = 27; }  // cooking v10: element slot, quality, tiers, light combos (DefaultContentDesignV2.cs)  // Design v2 cuts: no Darkness hazard, no Heat/Boost/pips in move text (DefaultContentDesignV2.cs)
            if (c.contentVersion < 28) { Classes28(c); c.contentVersion = 28; }  // classes are a lens on food: minimal base stats, per-stat meal multipliers (DefaultContentDesignV2.cs)
            if (c.contentVersion < 29) { CookPool29(c); c.contentVersion = 29; }  // the Cook's move pool, unlocked along the arc (DefaultContentDesignV2.cs)
            if (c.contentVersion < 30) { DeepDungeons30(c); c.contentVersion = 30; }  // deep dungeons: Meadow 10 floors, Chili Ridge 5, a boss every 5 (DefaultContentDesignV2.cs)
            if (c.contentVersion < 31) { MeadowHollow31(c); c.contentVersion = 31; }  // Meadow Hollow: named bosses, deeper monsters and drops (DefaultContentDesignV2.cs)
            if (c.contentVersion < 32) { ChiliRidge32(c); c.contentVersion = 32; }  // Chili Ridge: 5 floors, Ridge Drake, the chili source, revealed by Wren (DefaultContentDesignV2.cs)
            if (c.contentVersion < 33) { UtilityGear33(c); c.contentVersion = 33; }  // utility gear: lantern track + four slice trinkets (DefaultContentDesignV2.cs)
            if (c.contentVersion < 34) { LampMaker34(c); c.contentVersion = 34; }  // the Lamp-maker: a town building for light and Flare (DefaultContentDesignV2.cs)
            if (c.contentVersion < 35) { NormalSpeed35(c); c.contentVersion = 35; }  // no slow start: boot speeds un-doubled (DefaultContentDesignV2.cs)
            return true;
        }

        static void Upgrade2(GameContent c)
        {
            if (c.moves == null) c.moves = new List<MoveData>();
            if (c.skillNodes == null) c.skillNodes = new List<SkillNodeData>();
            if (c.moves.Count == 0 && c.skillNodes.Count == 0) BuildMovesAndTrees(c);
            c.contentVersion = 2;
        }

        static void BuildMovesAndTrees(GameContent c)
        {
            BuildMoves(c);
            Rework15(c);
            SetClassKit(c, ClassId.Warden, "w_sweep", "w_cleave", "roll", "Blade", "Bulwark", "Fury");
            SetClassKit(c, ClassId.Ranger, "r_shot", "r_volley", "roll", "Marksman", "Wayfinder", "Trickster");
            SetClassKit(c, ClassId.Cook, "c_ladle", "c_snack", "roll", "Brawler", "Comfort", "Spice");
            BuildTrees(c);
            Lamplighter16(c);
            Emberfist17(c);
            Joins18(c);
            Balance19(c);
            Ultimates20(c);
            Colours21(c);
            Colours22(c);
            UltCooldown23(c);
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

        // ───────────── class rework, stage 2 (contentVersion 15) ─────────────
        // Same move ids (so loadouts and trees still work), new kits: every move lights the world, reaches further,
        // has a hit type for Break, and Boosts into a bigger shape. Extra numbers per new kind:
        //   Combo      count = hits in the chain, power per hit, value = finisher ×power, duration = finisher stun, hitType2 = finisher's type
        //   Sunshield  range = light cone length, radius = cone half-angle°, duration, value = damage taken
        //   LeapSlam   range = leap distance, radius = ring, duration = stun
        //   EmberArrow range = speed, radius = burn radius, duration = burn time, value = burn power per half second
        //   FlareArrow range = speed, radius = burst radius, duration = shadow stun   Spotlight range, duration, value = +damage taken
        //   Cookfire   radius, duration, value = HP per half second   SkewerToss range = speed, count / spread
        static MoveData Re(GameContent c, string id, MoveKind kind, string name, string emoji, string desc,
                           float power, float cooldown, float range, float radius, float duration, float value, HitType type)
        {
            var m = c.moves.Find(x => x.id == id);
            if (m == null) return null;
            m.kind = kind; m.name = name; m.emoji = emoji; m.description = desc; m.power = power; m.cooldown = cooldown;
            m.range = range; m.radius = radius; m.duration = duration; m.value = value; m.hitType = type;
            return m;
        }

        static void Fx(MoveData m, BoostShape shape, float step, float lightRadius = 0f, string colour = "", int count = 1, float spread = 0f, float intensity = 1.4f)
        {
            if (m == null) return;
            m.boostShape = shape; m.boostStep = step; m.lightRadius = lightRadius; m.lightColour = colour;
            m.count = count; m.spread = spread; m.lightIntensity = intensity;
        }

        static HitType DefaultHitType(MoveData m)
        {
            switch (m.kind)
            {
                case MoveKind.Swing: return m.owner == ClassId.Cook ? HitType.Blunt : HitType.Slash;
                case MoveKind.Heavy: case MoveKind.Shockwave: case MoveKind.RollingPin: case MoveKind.ChargeDash: case MoveKind.Trap: return HitType.Blunt;
                case MoveKind.Lunge: case MoveKind.Arrow: case MoveKind.SpreadArrow: case MoveKind.PierceArrow: case MoveKind.Volley:
                case MoveKind.ArrowRain: case MoveKind.Vault: case MoveKind.SkewerToss: return HitType.Pierce;
                case MoveKind.PepperToss: case MoveKind.HotPot: case MoveKind.EmberArrow: case MoveKind.Cookfire: return HitType.Fire;
                case MoveKind.Sunshield: case MoveKind.LeapSlam: case MoveKind.FlareArrow: case MoveKind.Spotlight: return HitType.Light;
                default: return HitType.Slash;
            }
        }

        static void Rework15(GameContent c)
        {
            foreach (var x in c.moves) { x.hitType = DefaultHitType(x); if (x.lightIntensity <= 0f) x.lightIntensity = 1.4f; if (x.count < 1) x.count = 1; if (x.aimAssist == 0f) x.aimAssist = -1f; }
            MoveData m;
            // Warden: the shield that carries the flame
            m = Re(c, "w_sweep", MoveKind.Combo, "Blazing Combo", "🔥", "Two wide flaming sword arcs, then a Lantern Bash (Blunt) that stuns. Boost: +1 bash per level.", 0.9f, 0.36f, 0.95f, 1.05f, 0.35f, 1.8f, HitType.Slash);
            Fx(m, BoostShape.Hits, 1f, 0f, "FFC870", 3); if (m != null) m.hitType2 = HitType.Blunt;
            m = Re(c, "w_shieldwall", MoveKind.Sunshield, "Sunshield", "☀️", "Raise your lantern-shield for 2 s: take 70% less damage, bounce shots back, and throw a cone of light that lights every enemy in it (a Light hit). Boost: lasts longer and burns brighter.", 0f, 10f, 5f, 35f, 2f, 0.3f, HitType.Light);
            Fx(m, BoostShape.Duration, 0.5f, 6f, "FFF0C0");
            m = Re(c, "w_slam", MoveKind.LeapSlam, "Dawnbreaker", "🌅", "Leap ahead (onto the nearest enemy) and slam down: a ring of Light that hits and stuns everything around you. Boost: a bigger ring and a second shockwave.", 1.6f, 8f, 3.5f, 1.8f, 0.8f, 0f, HitType.Light);
            Fx(m, BoostShape.Radius, 0.25f, 7f, "FFE8B0", 1, 0f, 2f);
            m = Re(c, "w_cleave", MoveKind.Cleave, "Flame Cleave", "🔥", "Spin your blade in a ring of fire: ×1.75 damage to everything around you (a Fire hit). Boost: a bigger ring.", 1.75f, 6f, 1f, 2.2f, 0f, 0f, HitType.Fire);
            Fx(m, BoostShape.Radius, 0.25f, 0f, "FF9A40");
            m = Re(c, "w_warcry", MoveKind.WarCry, "Rally", "📯", "Roar and blaze: +40% damage for 6 seconds, and your light flares up.", 0f, 14f, 1f, 1f, 6f, 0.4f, HitType.Fire);
            Fx(m, BoostShape.Damage, 0f, 6f, "FFB060");
            Fx(c.moves.Find(x => x.id == "w_lunge"), BoostShape.Radius, 0.2f);
            Fx(c.moves.Find(x => x.id == "w_maul"), BoostShape.Radius, 0.25f);
            // Ranger: fire archer
            m = Re(c, "r_shot", MoveKind.Arrow, "Triple Shot", "🏹", "Three arrows in a fan, each carrying its own light. Boost: +2 arrows per level.", 0.6f, 0.42f, 13f, 1f, 0f, 0f, HitType.Pierce);
            Fx(m, BoostShape.Count, 2f, 0f, "", 3, 9f);
            m = c.moves.Find(x => x.id == "r_spread"); if (m != null) m.description = "Five arrows in a wide fan. Weaker each, hard to dodge. Boost: +2 arrows per level.";
            Fx(m, BoostShape.Count, 2f, 0f, "", 5, 12f);
            Fx(c.moves.Find(x => x.id == "r_longbow"), BoostShape.Damage, 0f, 3f);
            m = Re(c, "r_volley", MoveKind.EmberArrow, "Ember Arrow", "🔥", "A fire arrow that sticks where it lands and burns like a torch for 8 s: lights braziers and burns anything standing there (Fire). Boost: splits into more arrows.", 1f, 6f, 15f, 1f, 8f, 0.25f, HitType.Fire);
            Fx(m, BoostShape.Count, 1f, 4f, "FF8A30", 1, 12f);
            m = Re(c, "r_trap", MoveKind.FlareArrow, "Flare Arrow", "✨", "An arrow that bursts into a ball of light: hits everything nearby (Light), stuns shadow creatures, and shows secrets and weaknesses. Boost: a bigger burst.", 1.1f, 9f, 15f, 1.8f, 2f, 0f, HitType.Light);
            Fx(m, BoostShape.Radius, 0.3f, 6f, "FFF4D0", 1, 0f, 2.2f);
            Re(c, "r_mark", MoveKind.Spotlight, "Spotlight", "🔦", "Pin a beam of light on the nearest enemy for 8 s: it's lit, takes +30% damage and shows all its weaknesses.", 0f, 10f, 9f, 1f, 8f, 0.3f, HitType.Light);
            m = Re(c, "r_rain", MoveKind.ArrowRain, "Arrow Storm", "🌧️", "A storm of arrows over a wide area ahead for 2 seconds, lighting it up. Boost: a bigger storm.", 0.45f, 12f, 3f, 2.6f, 2f, 0f, HitType.Pierce);
            Fx(m, BoostShape.Radius, 0.3f, 7f);
            m = c.moves.Find(x => x.id == "r_vault"); if (m != null) m.description = "Flip backwards and loose a fan of 3 arrows at whatever you were facing.";
            Fx(m, BoostShape.Damage, 0f, 0f, "", 3, 12f);
            // Cook: the fire-keeper
            m = Re(c, "c_ladle", MoveKind.Combo, "Pan & Ladle", "🍳", "A ladle bonk, then a flaming pan (Fire) that stuns. Boost: +1 flaming pan per level.", 1f, 0.38f, 0.85f, 0.95f, 0.5f, 1.6f, HitType.Blunt);
            Fx(m, BoostShape.Hits, 1f, 0f, "FF9A40", 2); if (m != null) m.hitType2 = HitType.Fire;
            m = Re(c, "c_snack", MoveKind.SkewerToss, "Skewer Toss", "🍢", "Throw 3 flaming skewers in a fan (Pierce). Boost: +2 skewers per level.", 0.8f, 5f, 13f, 1f, 0f, 0f, HitType.Pierce);
            Fx(m, BoostShape.Count, 2f, 0f, "FF8A30", 3, 11f);
            m = Re(c, "c_pepper", MoveKind.PepperToss, "Flambé", "🔥", "Blast a wide cone of flame (Fire): hurts and slows enemies by half for 3 s, with a huge burst of light. Boost: leaves the ground burning.", 0.9f, 7f, 3.6f, 40f, 3f, 0.5f, HitType.Fire);
            Fx(m, BoostShape.Damage, 0f, 7f, "FF9A40");
            m = c.moves.Find(x => x.id == "c_hotpot"); if (m != null) m.description = "Lob a pot ahead. It leaves a glowing, scalding pool for 4 seconds (Fire).";
            Fx(m, BoostShape.Damage, 0f, 0f, "FF9A40");
            m = Re(c, "c_picnic", MoveKind.Cookfire, "Cookfire", "🔥", "Drop a cookfire for 8 s: heals 2 every half second while you stand in its light. Light-shy monsters keep away and it lights braziers. Boost: bigger, and heals more.", 0f, 16f, 1f, 2.2f, 8f, 2f, HitType.Fire);
            Fx(m, BoostShape.Radius, 0.3f, 0f, "FFA050");
            Fx(c.moves.Find(x => x.id == "c_skewer"), BoostShape.Radius, 0.2f);
            Fx(c.moves.Find(x => x.id == "c_pan"), BoostShape.Radius, 0.25f);
            // skill-tree nodes that unlock a move take its new name
            foreach (var n in c.skillNodes)
            {
                if (n.effect != NodeEffect.UnlockMove) continue;
                var mv = c.moves.Find(x => x.id == n.moveId);
                if (mv == null) continue;
                n.name = mv.name; n.emoji = mv.emoji; n.description = "New " + mv.slot.ToString().ToLower() + ": " + mv.description;
            }
        }

        // ───────────── the Lamplighter (contentVersion 16) ─────────────
        //   Wisp       range = speed, count / spread, homing     Lantern  range = seek range, duration, value = seconds between wisps
        //   Sunbeam    range = length, radius = half width, duration, value = seconds between hits
        //   Prism      range = speed, radius = splash             Kindle   value = heal per brazier (max 12)     Blink  range = distance
        static void Lamplighter16(GameContent c)
        {
            var L = ClassId.Lamplighter;
            if (!c.classes.Exists(x => x.id == L))
                c.classes.Add(new ClassData
                {
                    id = L, name = "Lamplighter", emoji = "🏮", role = "Mage",
                    description = "Keeper of the old lamps. Fragile, but her light magic seeks out its targets and breaks guards with ease. Slow-cooked comfort suits her: pot meals are 50% stronger.",
                    baseMaxHp = 11, damageMult = 1f, gatherZoneMult = 1f, mealBuffMult = 1f,
                });
            if (c.moves.Exists(x => x.id == "l_wisp")) return;
            MoveData m;
            m = M(c, "l_wisp", L, MoveSlot.Attack, MoveKind.Wisp, "Wisp Bolts", "✨", "Two homing motes of light that seek the nearest enemy. Boost: +2 wisps per level.", 0.65f, 0.48f, 9f);
            m.hitType = HitType.Light; Fx(m, BoostShape.Count, 2f, 3f, "CFE8FF", 2, 18f); m.homing = 300f;
            m = M(c, "l_lantern", L, MoveSlot.Skill, MoveKind.Lantern, "Lantern", "🏮", "Hang a floating lantern for 10 s: a strong light that looses a homing wisp at the nearest enemy every second. Boost: +1 lantern per level.", 0.5f, 12f, 7f, 1f, 10f, 1f);
            m.hitType = HitType.Light; Fx(m, BoostShape.Count, 1f, 6f, "FFE6A8", 1, 0f, 1.6f);
            m = M(c, "l_sunbeam", L, MoveSlot.Skill, MoveKind.Sunbeam, "Sunbeam", "☀️", "Channel a beam of sunlight for 1.5 s and steer it as you move: it hits everything in its line again and again (Light). Boost: longer and wider.", 0.35f, 9f, 7f, 0.45f, 1.5f, 0.15f);
            m.hitType = HitType.Light; Fx(m, BoostShape.Radius, 0.3f, 4f, "FFF3C4", 1, 0f, 1.8f);
            m = M(c, "l_prism", L, MoveSlot.Skill, MoveKind.Prism, "Prism", "🔮", "A crystal bolt that splits on impact: it strikes its target with every one of its weaknesses at once (great for Breaking) and showers the enemies nearby. Boost: knocks off more shield.", 1.2f, 7f, 14f, 2f);
            m.hitType = HitType.Light; Fx(m, BoostShape.Damage, 0f, 4f, "E0C8FF");
            m = M(c, "l_kindle", L, MoveSlot.Skill, MoveKind.Kindle, "Kindle", "🕯️", "Light every brazier in the room at once and flood it with warm light; heal 3 HP for each brazier (up to 12).", 0f, 16f, 1f, 1f, 0f, 3f);
            m.hitType = HitType.Light; Fx(m, BoostShape.Damage, 0f, 8f, "FFD08A");
            m = M(c, "l_blink", L, MoveSlot.Dash, MoveKind.Blink, "Blink", "💫", "Vanish in a flash and reappear a few steps away, leaving a light behind.", 0f, 1.1f, 3.2f);
            m.hitType = HitType.Light; Fx(m, BoostShape.Damage, 0f, 3f, "CFE8FF");
            SetClassKit(c, L, "l_wisp", "l_lantern", "l_blink", "Radiance", "Glow", "Prism");

            // tree: Radiance (attacks) · Glow (survival) · Prism (skills)
            Stat(c, L, 0, 0, 1, "l_focus", "Focus", "🎯", "+2% crit chance per rank.", BuffStat.CritPct, 0.02f, 5, null);
            N(c, L, 0, 1, 5, "l_bright", "Brighter Wisps", "✨", "Attacks hit 5% harder per rank.", NodeEffect.AttackPower, 0.05f, 3, "l_focus");
            Stat(c, L, 0, 2, 10, "l_kindled", "Kindled Mind", "🔥", "+2% damage per rank.", BuffStat.DamagePct, 0.02f, 5, "l_bright");
            Stat(c, L, 0, 3, 20, "l_clarity", "Clarity", "💎", "+2% crit chance per rank.", BuffStat.CritPct, 0.02f, 5, "l_kindled");
            N(c, L, 0, 4, 25, "l_beacon", "Beacon", "🗼", "Attacks hit 5% harder per rank.", NodeEffect.AttackPower, 0.05f, 3, "l_clarity");
            Stat(c, L, 0, 5, 35, "l_brilliance", "Brilliance", "🌟", "+2% damage per rank.", BuffStat.DamagePct, 0.02f, 5, "l_beacon");
            N(c, L, 0, 6, 45, "l_dawn", "First Light", "🌅", "Attacks hit 15% harder.", NodeEffect.AttackPower, 0.15f, 1, "l_brilliance");

            Stat(c, L, 1, 0, 1, "l_warm", "Warm Hands", "🧤", "+1 max HP per rank.", BuffStat.MaxHp, 1f, 5, null);
            Stat(c, L, 1, 1, 10, "l_ward", "Lamp Ward", "🛡️", "+1 armour per rank (5% less damage each).", BuffStat.Armour, 1f, 5, "l_warm");
            N(c, L, 1, 2, 15, "l_flicker", "Flicker", "💫", "Dash recharges 8% faster per rank.", NodeEffect.DashCooldown, 0.08f, 3, "l_ward");
            N(c, L, 1, 3, 20, "l_drift", "Drifting Light", "🍃", "Move 3% faster per rank.", NodeEffect.MoveSpeed, 0.03f, 5, "l_flicker");
            N(c, L, 1, 4, 30, "l_afterglow", "Afterglow", "🕯️", "Heal 1 HP per rank whenever you defeat an enemy.", NodeEffect.HealOnKill, 1f, 3, "l_drift");
            Stat(c, L, 1, 5, 40, "l_hearthlight", "Hearthlight", "🏡", "+2 max HP per rank.", BuffStat.MaxHp, 2f, 3, "l_afterglow");
            Stat(c, L, 1, 6, 50, "l_everflame", "Everflame", "🔥", "+2 armour.", BuffStat.Armour, 2f, 1, "l_hearthlight");

            N(c, L, 2, 0, 1, "l_trim", "Trimmed Wick", "✂️", "Skills recharge 4% faster per rank.", NodeEffect.SkillCooldown, 0.04f, 5, null);
            Move(c, L, 2, 1, 5, "l_n_sunbeam", "l_sunbeam", "l_trim");
            N(c, L, 2, 2, 10, "l_lens", "Lens", "🔍", "Skills hit 5% harder per rank.", NodeEffect.SkillPower, 0.05f, 5, "l_n_sunbeam");
            Move(c, L, 2, 3, 15, "l_n_prism", "l_prism", "l_lens");
            Move(c, L, 2, 4, 30, "l_n_kindle", "l_kindle", "l_n_prism");
            N(c, L, 2, 5, 35, "l_oil", "Lamp Oil", "🛢️", "Skills recharge 5% faster per rank.", NodeEffect.SkillCooldown, 0.05f, 3, "l_n_kindle");
            N(c, L, 2, 6, 45, "l_radiant", "Radiant", "☀️", "Skills hit 15% harder.", NodeEffect.SkillPower, 0.15f, 1, "l_oil");
        }

        // ───────────── the Emberfist (contentVersion 17) ─────────────
        //   Combo (Ember Fists) as above     Phoenix  range = rush distance, radius = uppercut, duration = stun
        //   FlameWheel  radius, count = spins     Detonate  radius = size at 0 Heat, range = + size at full Heat, value = + ×power at full Heat
        //   Counter  duration = stance, radius = strike-back size     Flicker  power = singe on contact
        static void Emberfist17(GameContent c)
        {
            var F = ClassId.Emberfist;
            if (!c.classes.Exists(x => x.id == F))
                c.classes.Add(new ClassData
                {
                    id = F, name = "Emberfist", emoji = "👊", role = "Brawler",
                    description = "A wandering monk who fights with burning fists. Every hit stokes her Heat, and her light grows from a candle to a bonfire. Grilled food fuels her: BBQ meals are 50% stronger.",
                    baseMaxHp = 14, damageMult = 1.05f, gatherZoneMult = 1f, mealBuffMult = 1f,
                });
            if (c.moves.Exists(x => x.id == "e_fists")) return;
            MoveData m;
            m = M(c, "e_fists", F, MoveSlot.Attack, MoveKind.Combo, "Ember Fists", "👊", "A fast four-punch chain; the last punch bursts into flame (Fire). Every hit stokes your Heat. Boost: +1 fire burst per level.", 0.7f, 0.22f, 0.8f, 0.85f, 0.25f, 1.6f);
            m.hitType = HitType.Blunt; m.hitType2 = HitType.Fire; Fx(m, BoostShape.Hits, 1f, 0f, "FF8A3D", 4);
            m = M(c, "e_phoenix", F, MoveSlot.Skill, MoveKind.Phoenix, "Rising Phoenix", "🦅", "Rush forward through everything in your way and finish with a flaming uppercut that stuns, leaving fire behind (Fire). Boost: rush further.", 1.2f, 6f, 3.5f, 1.3f, 0.6f);
            m.hitType = HitType.Fire; Fx(m, BoostShape.Radius, 0.25f, 5f, "FF7A2A");
            m = M(c, "e_wheel", F, MoveSlot.Skill, MoveKind.FlameWheel, "Flame Wheel", "🌀", "A spinning kick that hits everything around you twice (Slash). Boost: a bigger wheel.", 0.9f, 7f, 1f, 1.6f);
            m.hitType = HitType.Slash; Fx(m, BoostShape.Radius, 0.25f, 0f, "FF9A40", 2);
            m = M(c, "e_detonate", F, MoveSlot.Skill, MoveKind.Detonate, "Detonate", "💥", "Spend all your Heat in one explosion of fire: the more Heat, the bigger and harder it hits (Fire). Needs at least 20 Heat.", 1f, 8f, 2.3f, 1.2f, 0f, 2.5f);
            m.hitType = HitType.Fire; Fx(m, BoostShape.Damage, 0f, 0f, "FF6A20");
            m = M(c, "e_counter", F, MoveSlot.Skill, MoveKind.Counter, "Cinder Step", "🔥", "Take a fighting stance for a moment: if anything hits you, you take no damage and strike back in a burst of fire (+20 Heat). Boost: a longer stance.", 1.8f, 6f, 1f, 1.5f, 0.8f);
            m.hitType = HitType.Fire; Fx(m, BoostShape.Duration, 0.5f, 0f, "FFB060");
            m = M(c, "e_flicker", F, MoveSlot.Dash, MoveKind.Flicker, "Flicker", "💨", "A lightning-quick step that leaves a flame afterimage and singes anything you pass through.", 0.3f, 0.6f);
            m.hitType = HitType.Fire; Fx(m, BoostShape.Damage, 0f, 0f, "FF8A3D");
            SetClassKit(c, F, "e_fists", "e_phoenix", "e_flicker", "Fists", "Ember", "Inferno");

            // tree: Fists (attacks) · Ember (survival) · Inferno (skills)
            Stat(c, F, 0, 0, 1, "e_knuckles", "Iron Knuckles", "💪", "+2% damage per rank.", BuffStat.DamagePct, 0.02f, 5, null);
            N(c, F, 0, 1, 5, "e_jab", "Quick Jab", "👊", "Attacks hit 5% harder per rank.", NodeEffect.AttackPower, 0.05f, 3, "e_knuckles");
            Stat(c, F, 0, 2, 10, "e_pressure", "Pressure Points", "🎯", "+2% crit chance per rank.", BuffStat.CritPct, 0.02f, 5, "e_jab");
            Stat(c, F, 0, 3, 20, "e_stoke", "Stoked", "🔥", "+2% damage per rank.", BuffStat.DamagePct, 0.02f, 5, "e_pressure");
            N(c, F, 0, 4, 25, "e_hammer", "Hammer Fist", "🔨", "Attacks hit 5% harder per rank.", NodeEffect.AttackPower, 0.05f, 3, "e_stoke");
            Stat(c, F, 0, 5, 35, "e_focus", "Inner Focus", "🧘", "+2% crit chance per rank.", BuffStat.CritPct, 0.02f, 5, "e_hammer");
            N(c, F, 0, 6, 45, "e_dragonfist", "Dragon Fist", "🐉", "Attacks hit 15% harder.", NodeEffect.AttackPower, 0.15f, 1, "e_focus");

            Stat(c, F, 1, 0, 1, "e_hardy", "Hardy", "🪨", "+1 max HP per rank.", BuffStat.MaxHp, 1f, 5, null);
            Stat(c, F, 1, 1, 10, "e_wraps", "Hand Wraps", "🧣", "+1 armour per rank (5% less damage each).", BuffStat.Armour, 1f, 5, "e_hardy");
            N(c, F, 1, 2, 15, "e_light", "Light Feet", "🍃", "Move 3% faster per rank.", NodeEffect.MoveSpeed, 0.03f, 5, "e_wraps");
            N(c, F, 1, 3, 20, "e_quickstep", "Quickstep", "💨", "Dash recharges 8% faster per rank.", NodeEffect.DashCooldown, 0.08f, 3, "e_light");
            N(c, F, 1, 4, 30, "e_feast", "Burning Appetite", "🍖", "Heal 1 HP per rank whenever you defeat an enemy.", NodeEffect.HealOnKill, 1f, 3, "e_quickstep");
            Stat(c, F, 1, 5, 40, "e_tough", "Tempered", "⚒️", "+2 max HP per rank.", BuffStat.MaxHp, 2f, 3, "e_feast");
            Stat(c, F, 1, 6, 50, "e_unburnt", "Unburnt", "🛡️", "+2 armour.", BuffStat.Armour, 2f, 1, "e_tough");

            N(c, F, 2, 0, 1, "e_breath", "Steady Breath", "🌬️", "Skills recharge 4% faster per rank.", NodeEffect.SkillCooldown, 0.04f, 5, null);
            Move(c, F, 2, 1, 5, "e_n_wheel", "e_wheel", "e_breath");
            N(c, F, 2, 2, 10, "e_kindling", "Kindling", "🪵", "Skills hit 5% harder per rank.", NodeEffect.SkillPower, 0.05f, 5, "e_n_wheel");
            Move(c, F, 2, 3, 15, "e_n_detonate", "e_detonate", "e_kindling");
            Move(c, F, 2, 4, 30, "e_n_counter", "e_counter", "e_n_detonate");
            N(c, F, 2, 5, 35, "e_bellows", "Bellows", "🔥", "Skills recharge 5% faster per rank.", NodeEffect.SkillCooldown, 0.05f, 3, "e_n_counter");
            N(c, F, 2, 6, 45, "e_inferno", "Inferno", "🌋", "Skills hit 15% harder.", NodeEffect.SkillPower, 0.15f, 1, "e_bellows");
        }

        // ───────────── who joins your camp (contentVersion 18) ─────────────
        // A new game starts with the Cook. The others join the first time you clear their route.
        static void Joins18(GameContent c)
        {
            Join(c, ClassId.Cook, null, null);
            Join(c, ClassId.Ranger, "meadow", "They'd been tracking the meadow slimes, and they liked your bread.");
            Join(c, ClassId.Warden, "salt", "They held the marsh boardwalk alone until you came.");
            Join(c, ClassId.Lamplighter, "firefly", "They tend the Firefly Glade's old lamps, and their light is yours now.");
            Join(c, ClassId.Emberfist, "chili", "A wandering monk you met on the red mud of Chili Ridge.");
        }

        static void Join(GameContent c, ClassId id, string route, string text)
        {
            var cd = c.classes.Find(x => x.id == id);
            if (cd == null) return;
            cd.joinRoute = route; cd.joinText = text;
        }

        // ───────────── class balance pass (contentVersion 19) ─────────────
        // Basic attacks evened out: single-target damage per second at level 1 (move power × class damage ÷ cooldown, before gear).
        //   Warden ~3.0 (the tank) · Emberfist ~2.9, ~3.3 at full Heat · Cook ~2.5 · Lamplighter ~2.5 (homing) · Ranger ~2.3 (safe range; splits on groups)
        // The Lamplighter and Emberfist get two alternate attacks each (tree: Lv5 and Lv25, like the other classes).
        static void Balance19(GameContent c)
        {
            var L = ClassId.Lamplighter; var F = ClassId.Emberfist; MoveData m;
            m = c.moves.Find(x => x.id == "r_shot"); if (m != null) m.power = 0.65f;
            m = c.moves.Find(x => x.id == "l_wisp"); if (m != null) m.power = 0.6f;
            m = c.moves.Find(x => x.id == "e_fists"); if (m != null) m.power = 0.6f;
            m = c.moves.Find(x => x.id == "c_hotpot");
            if (m != null) { m.boostShape = BoostShape.Radius; m.boostStep = 0.25f; m.description = "Lob a pot ahead. It leaves a glowing, scalding pool for 4 seconds (Fire). Boost: a bigger pool."; }

            if (!c.moves.Exists(x => x.id == "l_lance"))
            {
                m = M(c, "l_lance", L, MoveSlot.Attack, MoveKind.Wisp, "Light Lance", "🔆", "A fast bolt of light that pierces every enemy in a line. Boost: +1 lance per level.", 1.25f, 0.6f, 18f, 1f, 0f, 1f);
                m.hitType = HitType.Light; Fx(m, BoostShape.Count, 1f, 3f, "FFF6D8", 1, 6f);
                m = M(c, "l_swarm", L, MoveSlot.Attack, MoveKind.Wisp, "Ember Swarm", "🔥", "Loose four little homing embers (Fire): weak alone, deadly together. Boost: +2 embers per level.", 0.32f, 0.55f, 7f);
                m.hitType = HitType.Fire; Fx(m, BoostShape.Count, 2f, 2.5f, "FF9A40", 4, 22f); m.homing = 420f;
                m = M(c, "e_kick", F, MoveSlot.Attack, MoveKind.Lunge, "Ember Kick", "🦶", "A long, sweeping kick that hits everything in a line (Slash). Boost: a wider kick.", 1.15f, 0.45f, 1.5f, 0.7f);
                m.hitType = HitType.Slash; Fx(m, BoostShape.Radius, 0.2f, 0f, "FF8A3D");
                m = M(c, "e_palm", F, MoveSlot.Attack, MoveKind.Heavy, "Burning Palm", "🖐️", "A slow, crushing palm strike that stuns (Blunt). Boost: a bigger blast.", 2.1f, 0.95f, 0.9f, 1.0f, 0.6f);
                m.hitType = HitType.Blunt; Fx(m, BoostShape.Radius, 0.25f, 0f, "FF7A2A");
                ToMove(c, "l_bright", "l_lance"); ToMove(c, "l_beacon", "l_swarm");
                ToMove(c, "e_jab", "e_kick"); ToMove(c, "e_hammer", "e_palm");
            }
        }

        /// Turns a skill-tree node into one that unlocks <paramref name="moveId"/>.
        static void ToMove(GameContent c, string nodeId, string moveId)
        {
            var n = c.skillNodes.Find(x => x.id == nodeId); var mv = c.moves.Find(x => x.id == moveId);
            if (n == null || mv == null) return;
            n.effect = NodeEffect.UnlockMove; n.moveId = moveId; n.maxRank = 1; n.valuePerRank = 0f;
            n.name = mv.name; n.emoji = mv.emoji; n.description = "New " + mv.slot.ToString().ToLower() + ": " + mv.description;
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
