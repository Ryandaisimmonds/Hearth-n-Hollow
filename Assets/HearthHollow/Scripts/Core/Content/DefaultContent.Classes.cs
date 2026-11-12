// Hearth & Hollow — default content: classes, their moves, how moves unlock, and the skill trees.
// The current design, written out directly (refactor 03 collapsed the old 44 upgrade steps into this).
// Edit freely: these files ARE the design now. Pure C#, no UnityEngine.
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public static partial class DefaultContent
    {
        /// The classes, every move, each class's move pool and unlock steps, and the skill-tree nodes.
        static void AddClasses(GameContent c)
        {
            // ── classes (5) ──
            c.classes.Add(new ClassData
            {
                name = "Warden",
                emoji = "⚔️",
                role = "Bruiser",
                description = "A steady guardian of the campfire. Gets the most out of hearty food (health, armour); light, quick food does less for her.",
                baseMaxHp = 14,
                mealStatMults = new List<StatMod>
                {
                    new StatMod { stat = BuffStat.MaxHp, value = 1.25f },
                    new StatMod { stat = BuffStat.Armour, value = 1.25f },
                    new StatMod { stat = BuffStat.MoveSpeedPct, value = 0.8f },
                },
                skill = new SkillData { name = "Cleave", emoji = "🪓", description = "One mighty swing for ×1.75 damage. Can crit.", value = 1.75f },
                starterAttack = "w_sweep",
                starterSkill = "w_cleave",
                starterDash = "roll",
                branchNames = new List<string> { "Blade", "Bulwark", "Fury" },
                joinRoute = "salt",
                ultimate = "w_ult",
                joinText = "They held the marsh boardwalk alone until you came.",
            });
            c.classes.Add(new ClassData
            {
                id = ClassId.Ranger,
                name = "Ranger",
                emoji = "🏹",
                role = "Forager",
                description = "Knows the wilds by heart. Trail food sharpens her (speed, crit, gathering); stodgy food does less for her.",
                baseMaxHp = 14,
                mealStatMults = new List<StatMod>
                {
                    new StatMod { stat = BuffStat.MoveSpeedPct, value = 1.25f },
                    new StatMod { stat = BuffStat.CritPct, value = 1.25f },
                    new StatMod { stat = BuffStat.GatherZonePct, value = 1.25f },
                    new StatMod { stat = BuffStat.MaxHp, value = 0.8f },
                },
                skill = new SkillData
                {
                    kind = SkillKind.Volley,
                    name = "Volley",
                    emoji = "🎯",
                    description = "Two quick arrows for ×0.75 damage each. Each can crit.",
                    value = 0.75f,
                },
                starterAttack = "r_shot",
                starterSkill = "r_volley",
                starterDash = "roll",
                branchNames = new List<string> { "Marksman", "Wayfinder", "Trickster" },
                joinRoute = "meadow",
                ultimate = "r_ult",
                joinText = "They'd been tracking the meadow slimes, and they liked your bread.",
            });
            c.classes.Add(new ClassData
            {
                id = ClassId.Cook,
                name = "Cook",
                emoji = "🍳",
                role = "Support",
                description = "Every meal tastes like home: every meal stat is a little stronger for the Cook.",
                baseMaxHp = 14,
                mealBuffMult = 1.1f,
                skill = new SkillData
                {
                    kind = SkillKind.HeartySnack,
                    name = "Hearty Snack",
                    emoji = "🥐",
                    description = "Munch a snack: heal 12 and guard this turn.",
                    value = 12f,
                },
                starterAttack = "c_ladle",
                starterSkill = "c_snack",
                starterDash = "roll",
                branchNames = new List<string> { "Brawler", "Comfort", "Spice" },
                ultimate = "c_ult",
            });
            c.classes.Add(new ClassData
            {
                id = ClassId.Lamplighter,
                name = "Lamplighter",
                emoji = "🏮",
                role = "Mage",
                description = "Keeper of the old lamps. Her light magic seeks out its targets. Restoring food suits her (heal on kill, coins); armour food does less for her.",
                baseMaxHp = 14,
                mealStatMults = new List<StatMod>
                {
                    new StatMod { stat = BuffStat.HealOnKill, value = 1.25f },
                    new StatMod { stat = BuffStat.CoinPct, value = 1.25f },
                    new StatMod { stat = BuffStat.Armour, value = 0.8f },
                },
                starterAttack = "l_wisp",
                starterSkill = "l_lantern",
                starterDash = "l_blink",
                branchNames = new List<string> { "Radiance", "Glow", "Prism" },
                joinRoute = "firefly",
                ultimate = "l_ult",
                joinText = "They tend the Firefly Glade's old lamps, and their light is yours now.",
            });
            c.classes.Add(new ClassData
            {
                id = ClassId.Emberfist,
                name = "Emberfist",
                emoji = "👊",
                role = "Brawler",
                description = "A wandering monk who fights with burning fists. Fiery food hits hardest for her (damage); she's too impatient to gather well.",
                baseMaxHp = 14,
                mealStatMults = new List<StatMod> { new StatMod { value = 1.25f }, new StatMod { stat = BuffStat.GatherZonePct, value = 0.8f } },
                starterAttack = "e_fists",
                starterSkill = "e_phoenix",
                starterDash = "e_flicker",
                branchNames = new List<string> { "Fists", "Ember", "Inferno" },
                joinRoute = "chili",
                ultimate = "e_ult",
                joinText = "A wandering monk you met on the red mud of Chili Ridge.",
            });
            // ── moves (50) ──
            c.moves.Add(new MoveData
            {
                id = "roll",
                name = "Roll",
                emoji = "💨",
                description = "A quick dodge roll. You can't be hit mid-roll.",
                anyClass = true,
                slot = MoveSlot.Dash,
                kind = MoveKind.Roll,
                power = 0f,
                cooldown = 0.7f,
                lightRadius = 3.5f,
                lightIntensity = 1.9f,
            });
            c.moves.Add(new MoveData
            {
                id = "w_sweep",
                name = "Blazing Combo",
                emoji = "🔥",
                description = "Two wide flaming sword arcs, then a Lantern Bash (Blunt) that stuns.",
                kind = MoveKind.Combo,
                power = 0.9f,
                cooldown = 0.36f,
                range = 1.0925f,
                radius = 1.3125f,
                duration = 0.35f,
                value = 1.8f,
                hitType2 = HitType.Blunt,
                lightRadius = 4f,
                lightIntensity = 1.9f,
                lightColour = "FFB000",
                count = 3,
                boostShape = BoostShape.Hits,
                boostStep = 1f,
            });
            c.moves.Add(new MoveData
            {
                id = "w_lunge",
                name = "Lunge",
                emoji = "🗡️",
                description = "Step forward with a long thrust. Great reach.",
                kind = MoveKind.Lunge,
                power = 1.2f,
                cooldown = 0.55f,
                range = 1.875f,
                radius = 0.909999967f,
                hitType = HitType.Pierce,
                lightRadius = 4f,
                lightIntensity = 1.9f,
                lightColour = "FFC21A",
                boostShape = BoostShape.Radius,
                boostStep = 0.2f,
            });
            c.moves.Add(new MoveData
            {
                id = "w_maul",
                name = "Maul",
                emoji = "🔨",
                description = "Slow, crushing blow. Stuns what it hits.",
                kind = MoveKind.Heavy,
                power = 2.3f,
                range = 0.99f,
                radius = 1.3f,
                duration = 0.6f,
                hitType = HitType.Blunt,
                lightRadius = 4f,
                lightIntensity = 1.9f,
                lightColour = "FFD000",
                boostShape = BoostShape.Radius,
                boostStep = 0.25f,
            });
            c.moves.Add(new MoveData
            {
                id = "w_cleave",
                name = "Flame Cleave",
                emoji = "🔥",
                description = "Spin your blade in a ring of fire: ×1.75 damage to everything around you (a Fire hit).",
                slot = MoveSlot.Skill,
                kind = MoveKind.Cleave,
                power = 1.75f,
                cooldown = 4f,
                radius = 2.64f,
                hitType = HitType.Fire,
                element = Element.Fire,
                lightRadius = 5.5f,
                lightIntensity = 1.9f,
                lightColour = "FF8C00",
                boostShape = BoostShape.Radius,
                boostStep = 0.25f,
            });
            c.moves.Add(new MoveData
            {
                id = "w_slam",
                name = "Dawnbreaker",
                emoji = "🌅",
                description = "Leap ahead (onto the nearest enemy) and slam down: a ring of Light that hits and stuns everything around you.",
                slot = MoveSlot.Skill,
                kind = MoveKind.LeapSlam,
                power = 1.6f,
                cooldown = 5.5f,
                range = 3.5f,
                radius = 1.8f,
                duration = 0.8f,
                hitType = HitType.Light,
                element = Element.Light,
                lightRadius = 7f,
                lightIntensity = 2f,
                lightColour = "FFC800",
                boostShape = BoostShape.Radius,
                boostStep = 0.25f,
            });
            c.moves.Add(new MoveData
            {
                id = "w_warcry",
                name = "Rally",
                emoji = "📯",
                description = "Roar and blaze: +40% damage for 6 seconds, and your light flares up.",
                slot = MoveSlot.Skill,
                kind = MoveKind.WarCry,
                power = 0f,
                cooldown = 10f,
                duration = 6f,
                value = 0.4f,
                hitType = HitType.Fire,
                element = Element.Fire,
                lightRadius = 6f,
                lightIntensity = 1.9f,
                lightColour = "FFA000",
            });
            c.moves.Add(new MoveData
            {
                id = "w_shieldwall",
                name = "Sunshield",
                emoji = "☀️",
                description = "Raise your lantern-shield for 2 s: take 70% less damage, bounce shots back, and throw a cone of light that lights every enemy in it (a Light hit).",
                slot = MoveSlot.Skill,
                kind = MoveKind.Sunshield,
                power = 0f,
                cooldown = 7f,
                range = 5f,
                radius = 35f,
                duration = 2f,
                value = 0.3f,
                hitType = HitType.Light,
                element = Element.Light,
                lightRadius = 6f,
                lightIntensity = 1.9f,
                lightColour = "FFD54A",
                boostShape = BoostShape.Duration,
                boostStep = 0.5f,
            });
            c.moves.Add(new MoveData
            {
                id = "w_whirl",
                name = "Whirlwind",
                emoji = "🌪️",
                description = "Spin for 1.5 seconds, hitting everything nearby again and again. You can still move.",
                slot = MoveSlot.Skill,
                kind = MoveKind.Whirlwind,
                power = 0.5f,
                cooldown = 7f,
                radius = 1.92000008f,
                duration = 1.5f,
                value = 0.25f,
                lightRadius = 5.5f,
                lightIntensity = 1.9f,
                lightColour = "FFB300",
            });
            c.moves.Add(new MoveData
            {
                id = "w_charge",
                name = "Shoulder Charge",
                emoji = "🐂",
                description = "Barge forward, hurting and shoving anything you run through.",
                slot = MoveSlot.Dash,
                kind = MoveKind.ChargeDash,
                power = 0.8f,
                cooldown = 1.1f,
                hitType = HitType.Blunt,
                lightRadius = 3.5f,
                lightIntensity = 1.9f,
                lightColour = "FFC21A",
            });
            c.moves.Add(new MoveData
            {
                id = "r_shot",
                name = "Triple Shot",
                emoji = "🏹",
                description = "Three arrows in a fan, each carrying its own light.",
                owner = ClassId.Ranger,
                kind = MoveKind.Arrow,
                power = 0.65f,
                cooldown = 0.42f,
                range = 13f,
                hitType = HitType.Pierce,
                lightRadius = 4f,
                lightIntensity = 1.9f,
                lightColour = "00E676",
                count = 3,
                spread = 9f,
                boostShape = BoostShape.Count,
                boostStep = 2f,
            });
            c.moves.Add(new MoveData
            {
                id = "r_spread",
                name = "Spread Shot",
                emoji = "🎐",
                description = "Five arrows in a wide fan. Weaker each, hard to dodge.",
                owner = ClassId.Ranger,
                kind = MoveKind.SpreadArrow,
                power = 0.55f,
                cooldown = 0.6f,
                range = 12f,
                value = 12f,
                hitType = HitType.Pierce,
                lightRadius = 4f,
                lightIntensity = 1.9f,
                lightColour = "2BFF88",
                count = 5,
                spread = 12f,
                boostShape = BoostShape.Count,
                boostStep = 2f,
            });
            c.moves.Add(new MoveData
            {
                id = "r_longbow",
                name = "Longbow",
                emoji = "🎯",
                description = "A slow, heavy arrow that flies through every enemy in a line.",
                owner = ClassId.Ranger,
                kind = MoveKind.PierceArrow,
                power = 1.6f,
                cooldown = 0.85f,
                range = 18f,
                hitType = HitType.Pierce,
                lightRadius = 4f,
                lightIntensity = 1.9f,
                lightColour = "00E5C0",
            });
            c.moves.Add(new MoveData
            {
                id = "r_volley",
                name = "Ember Arrow",
                emoji = "🔥",
                description = "A fire arrow that sticks where it lands and burns like a torch for 8 s: lights braziers and burns anything standing there (Fire).",
                owner = ClassId.Ranger,
                slot = MoveSlot.Skill,
                kind = MoveKind.EmberArrow,
                cooldown = 4f,
                range = 15f,
                duration = 8f,
                value = 0.25f,
                hitType = HitType.Fire,
                element = Element.Fire,
                lightRadius = 5.5f,
                lightIntensity = 1.9f,
                lightColour = "7CFF00",
                spread = 12f,
                boostShape = BoostShape.Count,
                boostStep = 1f,
            });
            c.moves.Add(new MoveData
            {
                id = "r_trap",
                name = "Flare Arrow",
                emoji = "✨",
                description = "An arrow that bursts into a ball of light: hits everything nearby (Light), stuns shadow creatures, and shows secrets and weaknesses.",
                owner = ClassId.Ranger,
                slot = MoveSlot.Skill,
                kind = MoveKind.FlareArrow,
                power = 1.1f,
                cooldown = 6.5f,
                range = 15f,
                radius = 1.8f,
                duration = 2f,
                hitType = HitType.Light,
                element = Element.Light,
                lightRadius = 6f,
                lightIntensity = 2.2f,
                lightColour = "00FFC8",
                boostShape = BoostShape.Radius,
                boostStep = 0.3f,
            });
            c.moves.Add(new MoveData
            {
                id = "r_mark",
                name = "Spotlight",
                emoji = "🔦",
                description = "Pin a beam of light on the nearest enemy for 8 s: it's lit, takes +30% damage and shows all its weaknesses.",
                owner = ClassId.Ranger,
                slot = MoveSlot.Skill,
                kind = MoveKind.Spotlight,
                power = 0f,
                cooldown = 7f,
                range = 9f,
                duration = 8f,
                value = 0.3f,
                hitType = HitType.Light,
                element = Element.Light,
                lightRadius = 5.5f,
                lightIntensity = 1.9f,
                lightColour = "00FFA0",
            });
            c.moves.Add(new MoveData
            {
                id = "r_rain",
                name = "Arrow Storm",
                emoji = "🌧️",
                description = "A storm of arrows over a wide area ahead for 2 seconds, lighting it up.",
                owner = ClassId.Ranger,
                slot = MoveSlot.Skill,
                kind = MoveKind.ArrowRain,
                power = 0.45f,
                cooldown = 8.5f,
                range = 3f,
                radius = 2.6f,
                duration = 2f,
                hitType = HitType.Pierce,
                lightRadius = 7f,
                lightIntensity = 1.9f,
                lightColour = "1DE9B6",
                boostShape = BoostShape.Radius,
                boostStep = 0.3f,
            });
            c.moves.Add(new MoveData
            {
                id = "r_smoke",
                name = "Smoke Bomb",
                emoji = "🌫️",
                description = "Enemies nearby are slowed by half for 3 seconds and you speed up.",
                owner = ClassId.Ranger,
                slot = MoveSlot.Skill,
                kind = MoveKind.Smoke,
                power = 0f,
                cooldown = 8.5f,
                radius = 3.60000014f,
                duration = 3f,
                value = 0.5f,
                lightRadius = 5.5f,
                lightIntensity = 1.9f,
                lightColour = "00C896",
            });
            c.moves.Add(new MoveData
            {
                id = "r_vault",
                name = "Back Vault",
                emoji = "🤸",
                description = "Flip backwards and loose a fan of 3 arrows at whatever you were facing.",
                owner = ClassId.Ranger,
                slot = MoveSlot.Dash,
                kind = MoveKind.Vault,
                power = 0.8f,
                hitType = HitType.Pierce,
                lightRadius = 3.5f,
                lightIntensity = 1.9f,
                lightColour = "00E676",
                count = 3,
                spread = 12f,
            });
            c.moves.Add(new MoveData
            {
                id = "c_ladle",
                name = "Pan & Ladle",
                emoji = "🍳",
                description = "A ladle bonk, then a flaming pan (Fire) that stuns.",
                owner = ClassId.Cook,
                kind = MoveKind.Combo,
                cooldown = 0.38f,
                range = 0.9775f,
                radius = 1.1875f,
                duration = 0.5f,
                value = 1.6f,
                hitType = HitType.Blunt,
                hitType2 = HitType.Fire,
                element2 = Element.Fire,
                lightRadius = 4f,
                lightIntensity = 1.9f,
                lightColour = "FF7A00",
                count = 2,
                boostShape = BoostShape.Hits,
                boostStep = 1f,
            });
            c.moves.Add(new MoveData
            {
                id = "c_skewer",
                name = "Skewer",
                emoji = "🍢",
                description = "Jab forward with a kebab skewer.",
                owner = ClassId.Cook,
                kind = MoveKind.Lunge,
                power = 1.1f,
                cooldown = 0.5f,
                range = 1.625f,
                radius = 0.780000031f,
                hitType = HitType.Pierce,
                lightRadius = 4f,
                lightIntensity = 1.9f,
                lightColour = "FF9500",
                boostShape = BoostShape.Radius,
                boostStep = 0.2f,
            });
            c.moves.Add(new MoveData
            {
                id = "c_pan",
                name = "Frying Pan",
                emoji = "🍳",
                description = "A ringing pan to the head. Stuns.",
                owner = ClassId.Cook,
                kind = MoveKind.Heavy,
                power = 1.9f,
                cooldown = 0.9f,
                range = 0.935000062f,
                radius = 1.2349999f,
                duration = 0.8f,
                hitType = HitType.Blunt,
                lightRadius = 4f,
                lightIntensity = 1.9f,
                lightColour = "FFC400",
                boostShape = BoostShape.Radius,
                boostStep = 0.25f,
            });
            c.moves.Add(new MoveData
            {
                id = "c_snack",
                name = "Skewer Toss",
                emoji = "🍢",
                description = "Throw 3 flaming skewers in a fan (Pierce).",
                owner = ClassId.Cook,
                slot = MoveSlot.Skill,
                kind = MoveKind.SkewerToss,
                power = 0.8f,
                cooldown = 3.5f,
                range = 13f,
                hitType = HitType.Pierce,
                lightRadius = 5.5f,
                lightIntensity = 1.9f,
                lightColour = "FF8C00",
                count = 3,
                spread = 11f,
                boostShape = BoostShape.Count,
                boostStep = 2f,
            });
            c.moves.Add(new MoveData
            {
                id = "c_pepper",
                name = "Flambé",
                emoji = "🔥",
                description = "Blast a wide cone of flame (Fire): hurts and slows enemies by half for 3 s, with a huge burst of light.",
                owner = ClassId.Cook,
                slot = MoveSlot.Skill,
                kind = MoveKind.PepperToss,
                power = 0.9f,
                cooldown = 5f,
                range = 3.6f,
                radius = 40f,
                duration = 3f,
                value = 0.5f,
                hitType = HitType.Fire,
                element = Element.Fire,
                lightRadius = 7f,
                lightIntensity = 1.9f,
                lightColour = "FF3D00",
            });
            c.moves.Add(new MoveData
            {
                id = "c_hotpot",
                name = "Hot Pot",
                emoji = "🍲",
                description = "Lob a pot ahead. It leaves a glowing, scalding pool for 4 seconds (Fire).",
                owner = ClassId.Cook,
                slot = MoveSlot.Skill,
                kind = MoveKind.HotPot,
                power = 0.3f,
                cooldown = 7f,
                range = 4f,
                radius = 1.5f,
                duration = 4f,
                value = 0.5f,
                hitType = HitType.Fire,
                element = Element.Fire,
                lightRadius = 5.5f,
                lightIntensity = 1.9f,
                lightColour = "FF6A00",
                boostShape = BoostShape.Radius,
                boostStep = 0.25f,
            });
            c.moves.Add(new MoveData
            {
                id = "c_picnic",
                name = "Cookfire",
                emoji = "🔥",
                description = "Drop a cookfire for 8 s: heals 2 every half second while you stand in its light. Light-shy monsters keep away and it lights braziers.",
                owner = ClassId.Cook,
                slot = MoveSlot.Skill,
                kind = MoveKind.Cookfire,
                power = 0f,
                cooldown = 11f,
                radius = 2.2f,
                duration = 8f,
                value = 2f,
                hitType = HitType.Fire,
                element = Element.Fire,
                lightRadius = 5.5f,
                lightIntensity = 1.9f,
                lightColour = "FFAB00",
                boostShape = BoostShape.Radius,
                boostStep = 0.3f,
            });
            c.moves.Add(new MoveData
            {
                id = "c_pin",
                name = "Rolling Pin",
                emoji = "🪵",
                description = "Throw a rolling pin that flies out and comes back, hitting everything twice.",
                owner = ClassId.Cook,
                slot = MoveSlot.Skill,
                kind = MoveKind.RollingPin,
                power = 0.9f,
                cooldown = 4f,
                range = 5f,
                value = 10f,
                hitType = HitType.Blunt,
                lightRadius = 5.5f,
                lightIntensity = 1.9f,
                lightColour = "FFC400",
            });
            c.moves.Add(new MoveData
            {
                id = "c_slide",
                name = "Butter Slide",
                emoji = "🧈",
                description = "Slide along a buttery trail. Enemies on it are slowed for 3 seconds.",
                owner = ClassId.Cook,
                slot = MoveSlot.Dash,
                kind = MoveKind.SlickDash,
                power = 0f,
                duration = 3f,
                value = 0.6f,
                lightRadius = 3.5f,
                lightIntensity = 1.9f,
                lightColour = "FFD600",
            });
            c.moves.Add(new MoveData
            {
                id = "l_wisp",
                name = "Wisp Bolts",
                emoji = "✨",
                description = "Two homing motes of light that seek the nearest enemy.",
                owner = ClassId.Lamplighter,
                kind = MoveKind.Wisp,
                power = 0.6f,
                cooldown = 0.48f,
                range = 9f,
                hitType = HitType.Light,
                element = Element.Light,
                lightRadius = 4f,
                lightIntensity = 1.9f,
                lightColour = "2EA8FF",
                count = 2,
                spread = 18f,
                homing = 300f,
                boostShape = BoostShape.Count,
                boostStep = 2f,
            });
            c.moves.Add(new MoveData
            {
                id = "l_lantern",
                name = "Lantern",
                emoji = "🏮",
                description = "Hang a floating lantern for 10 s: a strong light that looses a homing wisp at the nearest enemy every second.",
                owner = ClassId.Lamplighter,
                slot = MoveSlot.Skill,
                kind = MoveKind.Lantern,
                power = 0.5f,
                cooldown = 8.5f,
                range = 7f,
                duration = 10f,
                value = 1f,
                hitType = HitType.Light,
                element = Element.Light,
                lightRadius = 6f,
                lightIntensity = 1.9f,
                lightColour = "3D8BFF",
                boostShape = BoostShape.Count,
                boostStep = 1f,
            });
            c.moves.Add(new MoveData
            {
                id = "l_sunbeam",
                name = "Sunbeam",
                emoji = "☀️",
                description = "Channel a beam of sunlight for 1.5 s and steer it as you move: it hits everything in its line again and again (Light).",
                owner = ClassId.Lamplighter,
                slot = MoveSlot.Skill,
                kind = MoveKind.Sunbeam,
                power = 0.35f,
                cooldown = 6.5f,
                range = 7f,
                radius = 0.45f,
                duration = 1.5f,
                value = 0.15f,
                hitType = HitType.Light,
                element = Element.Light,
                lightRadius = 5.5f,
                lightIntensity = 1.9f,
                lightColour = "40C4FF",
                boostShape = BoostShape.Radius,
                boostStep = 0.3f,
            });
            c.moves.Add(new MoveData
            {
                id = "l_prism",
                name = "Prism",
                emoji = "🔮",
                description = "A crystal bolt that splits on impact: it strikes its target with every one of its weaknesses at once (great for Breaking) and showers the enemies nearby.",
                owner = ClassId.Lamplighter,
                slot = MoveSlot.Skill,
                kind = MoveKind.Prism,
                power = 1.2f,
                cooldown = 5f,
                range = 14f,
                radius = 2f,
                hitType = HitType.Light,
                element = Element.Light,
                lightRadius = 5.5f,
                lightIntensity = 1.9f,
                lightColour = "B04DFF",
            });
            c.moves.Add(new MoveData
            {
                id = "l_kindle",
                name = "Kindle",
                emoji = "🕯️",
                description = "Light every brazier in the room at once and flood it with warm light; heal 3 HP for each brazier (up to 12).",
                owner = ClassId.Lamplighter,
                slot = MoveSlot.Skill,
                kind = MoveKind.Kindle,
                power = 0f,
                cooldown = 11f,
                value = 3f,
                hitType = HitType.Light,
                element = Element.Light,
                lightRadius = 8f,
                lightIntensity = 1.9f,
                lightColour = "5C6CFF",
            });
            c.moves.Add(new MoveData
            {
                id = "l_blink",
                name = "Blink",
                emoji = "💫",
                description = "Vanish in a flash and reappear a few steps away, leaving a light behind.",
                owner = ClassId.Lamplighter,
                slot = MoveSlot.Dash,
                kind = MoveKind.Blink,
                power = 0f,
                cooldown = 1.1f,
                range = 3.2f,
                hitType = HitType.Light,
                element = Element.Light,
                lightRadius = 3.5f,
                lightIntensity = 1.9f,
                lightColour = "9B5CFF",
            });
            c.moves.Add(new MoveData
            {
                id = "e_fists",
                name = "Ember Fists",
                emoji = "👊",
                description = "A fast four-punch chain; the last punch bursts into flame (Fire).",
                owner = ClassId.Emberfist,
                kind = MoveKind.Combo,
                power = 0.6f,
                cooldown = 0.22f,
                range = 0.92f,
                radius = 1.0625f,
                duration = 0.25f,
                value = 1.6f,
                hitType = HitType.Blunt,
                hitType2 = HitType.Fire,
                element2 = Element.Fire,
                lightRadius = 4f,
                lightIntensity = 1.9f,
                lightColour = "FF3B3B",
                count = 4,
                boostShape = BoostShape.Hits,
                boostStep = 1f,
            });
            c.moves.Add(new MoveData
            {
                id = "e_phoenix",
                name = "Rising Phoenix",
                emoji = "🦅",
                description = "Rush forward through everything in your way and finish with a flaming uppercut that stuns, leaving fire behind (Fire).",
                owner = ClassId.Emberfist,
                slot = MoveSlot.Skill,
                kind = MoveKind.Phoenix,
                power = 1.2f,
                cooldown = 4f,
                range = 3.5f,
                radius = 1.3f,
                duration = 0.6f,
                hitType = HitType.Fire,
                element = Element.Fire,
                lightRadius = 5.5f,
                lightIntensity = 1.9f,
                lightColour = "FF4A2A",
                boostShape = BoostShape.Radius,
                boostStep = 0.25f,
            });
            c.moves.Add(new MoveData
            {
                id = "e_wheel",
                name = "Flame Wheel",
                emoji = "🌀",
                description = "A spinning kick that hits everything around you twice (Slash).",
                owner = ClassId.Emberfist,
                slot = MoveSlot.Skill,
                kind = MoveKind.FlameWheel,
                power = 0.9f,
                cooldown = 5f,
                radius = 1.92000008f,
                lightRadius = 5.5f,
                lightIntensity = 1.9f,
                lightColour = "FF5FA8",
                count = 2,
                boostShape = BoostShape.Radius,
                boostStep = 0.25f,
            });
            c.moves.Add(new MoveData
            {
                id = "e_detonate",
                name = "Detonate",
                emoji = "💥",
                description = "One big explosion of fire around you (Fire).",
                owner = ClassId.Emberfist,
                slot = MoveSlot.Skill,
                kind = MoveKind.Detonate,
                cooldown = 5.5f,
                range = 2.3f,
                radius = 1.2f,
                value = 2.5f,
                hitType = HitType.Fire,
                element = Element.Fire,
                lightRadius = 5.5f,
                lightIntensity = 1.9f,
                lightColour = "FF2E2E",
            });
            c.moves.Add(new MoveData
            {
                id = "e_counter",
                name = "Cinder Step",
                emoji = "🔥",
                description = "Take a fighting stance for a moment: if anything hits you, you take no damage and strike back in a burst of fire.",
                owner = ClassId.Emberfist,
                slot = MoveSlot.Skill,
                kind = MoveKind.Counter,
                power = 1.8f,
                cooldown = 4f,
                radius = 1.5f,
                duration = 0.8f,
                hitType = HitType.Fire,
                element = Element.Fire,
                lightRadius = 5.5f,
                lightIntensity = 1.9f,
                lightColour = "FF7A9E",
                boostShape = BoostShape.Duration,
                boostStep = 0.5f,
            });
            c.moves.Add(new MoveData
            {
                id = "e_flicker",
                name = "Flicker",
                emoji = "💨",
                description = "A lightning-quick step that leaves a flame afterimage and singes anything you pass through.",
                owner = ClassId.Emberfist,
                slot = MoveSlot.Dash,
                kind = MoveKind.Flicker,
                power = 0.3f,
                cooldown = 0.6f,
                hitType = HitType.Fire,
                element = Element.Fire,
                lightRadius = 3.5f,
                lightIntensity = 1.9f,
                lightColour = "FF3B6A",
            });
            c.moves.Add(new MoveData
            {
                id = "l_lance",
                name = "Light Lance",
                emoji = "🔆",
                description = "A fast bolt of light that pierces every enemy in a line.",
                owner = ClassId.Lamplighter,
                kind = MoveKind.Wisp,
                power = 1.25f,
                cooldown = 0.6f,
                range = 18f,
                value = 1f,
                hitType = HitType.Light,
                element = Element.Light,
                lightRadius = 4f,
                lightIntensity = 1.9f,
                lightColour = "4FC3FF",
                spread = 6f,
                boostShape = BoostShape.Count,
                boostStep = 1f,
            });
            c.moves.Add(new MoveData
            {
                id = "l_swarm",
                name = "Ember Swarm",
                emoji = "🔥",
                description = "Loose four little homing embers (Fire): weak alone, deadly together.",
                owner = ClassId.Lamplighter,
                kind = MoveKind.Wisp,
                power = 0.32f,
                cooldown = 0.55f,
                range = 7f,
                hitType = HitType.Fire,
                element = Element.Fire,
                lightRadius = 4f,
                lightIntensity = 1.9f,
                lightColour = "A040FF",
                count = 4,
                spread = 22f,
                homing = 420f,
                boostShape = BoostShape.Count,
                boostStep = 2f,
            });
            c.moves.Add(new MoveData
            {
                id = "e_kick",
                name = "Ember Kick",
                emoji = "🦶",
                description = "A long, sweeping kick that hits everything in a line (Slash).",
                owner = ClassId.Emberfist,
                kind = MoveKind.Lunge,
                power = 1.15f,
                cooldown = 0.45f,
                range = 1.875f,
                radius = 0.909999967f,
                lightRadius = 4f,
                lightIntensity = 1.9f,
                lightColour = "FF5FA8",
                boostShape = BoostShape.Radius,
                boostStep = 0.2f,
            });
            c.moves.Add(new MoveData
            {
                id = "e_palm",
                name = "Burning Palm",
                emoji = "🖐️",
                description = "A slow, crushing palm strike that stuns (Blunt).",
                owner = ClassId.Emberfist,
                kind = MoveKind.Heavy,
                power = 2.1f,
                cooldown = 0.95f,
                range = 0.99f,
                radius = 1.3f,
                duration = 0.6f,
                hitType = HitType.Blunt,
                lightRadius = 4f,
                lightIntensity = 1.9f,
                lightColour = "FF2E4E",
                boostShape = BoostShape.Radius,
                boostStep = 0.25f,
            });
            c.moves.Add(new MoveData
            {
                id = "w_ult",
                name = "Sunfall",
                emoji = "☀️",
                description = "Leap to where you're aiming and bring the sun down: a huge ring of Light that hits and stuns everything in it, then a second shockwave. +50% damage for 6 s afterwards.",
                slot = MoveSlot.Ultimate,
                kind = MoveKind.Sunfall,
                power = 3f,
                cooldown = 22.5f,
                range = 6f,
                radius = 3.4f,
                duration = 2f,
                value = 0.5f,
                hitType = HitType.Light,
                hitType2 = HitType.Light,
                element = Element.Light,
                lightRadius = 11f,
                lightIntensity = 2.6f,
                lightColour = "FFC000",
                aimAssist = 0f,
            });
            c.moves.Add(new MoveData
            {
                id = "r_ult",
                name = "Starfall",
                emoji = "🌠",
                description = "Call down a storm of falling stars where you're aiming for 3 s (Light). Everything caught in it is Spotlit and takes +30% damage.",
                owner = ClassId.Ranger,
                slot = MoveSlot.Ultimate,
                kind = MoveKind.Starfall,
                power = 0.7f,
                cooldown = 22.5f,
                range = 6f,
                radius = 3.4f,
                duration = 3f,
                value = 0.3f,
                hitType = HitType.Light,
                hitType2 = HitType.Light,
                element = Element.Light,
                lightRadius = 10f,
                lightIntensity = 2.6f,
                lightColour = "00FFB0",
                aimAssist = 0f,
            });
            c.moves.Add(new MoveData
            {
                id = "c_ult",
                name = "Grand Feast",
                emoji = "🍲",
                description = "Bang the big pot: a ring of fire blasts and slows everything around you (Fire), you heal 30, and a great cookfire burns for 8 s, healing you while you stand in its light.",
                owner = ClassId.Cook,
                slot = MoveSlot.Ultimate,
                kind = MoveKind.GrandFeast,
                power = 2f,
                cooldown = 22.5f,
                radius = 3.2f,
                duration = 8f,
                value = 30f,
                hitType = HitType.Fire,
                hitType2 = HitType.Fire,
                element = Element.Fire,
                lightRadius = 9f,
                lightIntensity = 2.6f,
                lightColour = "FF8F00",
                aimAssist = 0f,
            });
            c.moves.Add(new MoveData
            {
                id = "l_ult",
                name = "Dawn",
                emoji = "🌅",
                description = "Raise your lamp and call up the dawn: every brazier lights, the room floods with light, and every enemy in it takes a Light hit and stuns it.",
                owner = ClassId.Lamplighter,
                slot = MoveSlot.Ultimate,
                kind = MoveKind.Dawn,
                power = 2f,
                cooldown = 22.5f,
                radius = 12f,
                duration = 1.5f,
                value = 3f,
                hitType = HitType.Light,
                hitType2 = HitType.Light,
                element = Element.Light,
                lightRadius = 14f,
                lightIntensity = 2.6f,
                lightColour = "8A4DFF",
                aimAssist = 0f,
            });
            c.moves.Add(new MoveData
            {
                id = "e_ult",
                name = "Supernova",
                emoji = "💥",
                description = "Pull in every ember, then a huge explosion of fire around you (Fire) that stuns.",
                owner = ClassId.Emberfist,
                slot = MoveSlot.Ultimate,
                kind = MoveKind.Supernova,
                power = 3.2f,
                cooldown = 22.5f,
                radius = 3.6f,
                duration = 1.5f,
                value = 6f,
                hitType = HitType.Fire,
                hitType2 = HitType.Fire,
                element = Element.Fire,
                lightRadius = 12f,
                lightIntensity = 2.6f,
                lightColour = "FF2E6A",
                aimAssist = 0f,
            });
            c.moves.Add(new MoveData
            {
                id = "c_ult2",
                name = "Five-Alarm Chili",
                emoji = "🌶️",
                description = "Bite the hottest chili in the bag: a moment later a huge burst of fire erupts around you (Fire), hitting and stunning everything nearby.",
                owner = ClassId.Cook,
                slot = MoveSlot.Ultimate,
                kind = MoveKind.Supernova,
                power = 3f,
                cooldown = 22.5f,
                radius = 3.4f,
                duration = 1.5f,
                hitType = HitType.Fire,
                hitType2 = HitType.Fire,
                element = Element.Fire,
                lightRadius = 12f,
                lightIntensity = 2.6f,
                aimAssist = 0f,
            });
            // ── moveUnlocks (8) ──
            c.moveUnlocks.Add(new MoveUnlock { moveId = "c_ladle" });
            c.moveUnlocks.Add(new MoveUnlock { moveId = "c_snack" });
            c.moveUnlocks.Add(new MoveUnlock { moveId = "c_slide", kind = UnlockKind.ReachFloor, routeId = "meadow", floor = 2, learnAt = "kitchen" });
            c.moveUnlocks.Add(new MoveUnlock { moveId = "c_pepper", kind = UnlockKind.ReachFloor, routeId = "meadow", floor = 3, learnAt = "kitchen" });
            c.moveUnlocks.Add(new MoveUnlock { moveId = "c_ult", kind = UnlockKind.BeatBoss, routeId = "meadow", floor = 3, learnAt = "yard" });
            c.moveUnlocks.Add(new MoveUnlock { moveId = "c_hotpot", kind = UnlockKind.ReachFloor, routeId = "frosthollow", floor = 2, learnAt = "kitchen" });
            c.moveUnlocks.Add(new MoveUnlock { moveId = "c_picnic", kind = UnlockKind.ReachFloor, routeId = "chili", floor = 2, learnAt = "kitchen" });
            c.moveUnlocks.Add(new MoveUnlock { moveId = "c_ult2", kind = UnlockKind.BeatBoss, routeId = "frosthollow", floor = 3, learnAt = "yard" });
            // ── skillNodes (105) ──
            c.skillNodes.Add(new SkillNodeData
            {
                id = "w_whetstone",
                name = "Whetstone",
                emoji = "🪨",
                description = "+2% damage per rank.",
                effect = NodeEffect.Stat,
                valuePerRank = 0.02f,
                maxRank = 5,
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "w_n_lunge",
                row = 1,
                minLevel = 5,
                name = "Lunge",
                emoji = "🗡️",
                description = "New attack: Step forward with a long thrust. Great reach.",
                moveId = "w_lunge",
                requires = "w_whetstone",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "w_keen",
                row = 2,
                minLevel = 10,
                name = "Keen Edge",
                emoji = "✨",
                description = "+2% crit chance per rank.",
                effect = NodeEffect.Stat,
                stat = BuffStat.CritPct,
                valuePerRank = 0.02f,
                maxRank = 5,
                requires = "w_n_lunge",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "w_honed",
                row = 3,
                minLevel = 20,
                name = "Honed Strikes",
                emoji = "⚔️",
                description = "Attacks hit 5% harder per rank.",
                effect = NodeEffect.AttackPower,
                valuePerRank = 0.05f,
                maxRank = 3,
                requires = "w_keen",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "w_n_maul",
                row = 4,
                minLevel = 25,
                name = "Maul",
                emoji = "🔨",
                description = "New attack: Slow, crushing blow. Stuns what it hits.",
                moveId = "w_maul",
                requires = "w_honed",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "w_brutal",
                row = 5,
                minLevel = 35,
                name = "Brutal Force",
                emoji = "💪",
                description = "+2% damage per rank.",
                effect = NodeEffect.Stat,
                valuePerRank = 0.02f,
                maxRank = 5,
                requires = "w_n_maul",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "w_warlord",
                row = 6,
                minLevel = 45,
                name = "Warlord's Edge",
                emoji = "👑",
                description = "Attacks hit 15% harder.",
                effect = NodeEffect.AttackPower,
                valuePerRank = 0.15f,
                requires = "w_brutal",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "w_hide",
                branch = 1,
                name = "Thick Hide",
                emoji = "🐗",
                description = "+1 max HP per rank.",
                effect = NodeEffect.Stat,
                stat = BuffStat.MaxHp,
                valuePerRank = 1f,
                maxRank = 5,
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "w_iron",
                branch = 1,
                row = 1,
                minLevel = 10,
                name = "Iron Skin",
                emoji = "🔩",
                description = "+1 armour per rank (5% less damage each).",
                effect = NodeEffect.Stat,
                stat = BuffStat.Armour,
                valuePerRank = 1f,
                maxRank = 5,
                requires = "w_hide",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "w_n_charge",
                branch = 1,
                row = 2,
                minLevel = 15,
                name = "Shoulder Charge",
                emoji = "🐂",
                description = "New dash: Barge forward, hurting and shoving anything you run through.",
                moveId = "w_charge",
                requires = "w_iron",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "w_n_shieldwall",
                branch = 1,
                row = 3,
                minLevel = 20,
                name = "Sunshield",
                emoji = "☀️",
                description = "New skill: Raise your lantern-shield for 2 s: take 70% less damage, bounce shots back, and throw a cone of light that lights every enemy in it (a Light hit). Boost: lasts longer and burns brighter.",
                moveId = "w_shieldwall",
                requires = "w_n_charge",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "w_secondwind",
                branch = 1,
                row = 4,
                minLevel = 30,
                name = "Second Wind",
                emoji = "🌬️",
                description = "Heal 1 HP per rank whenever you defeat an enemy.",
                effect = NodeEffect.HealOnKill,
                valuePerRank = 1f,
                maxRank = 3,
                requires = "w_n_shieldwall",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "w_bastion",
                branch = 1,
                row = 5,
                minLevel = 40,
                name = "Bastion",
                emoji = "🏰",
                description = "+2 max HP per rank.",
                effect = NodeEffect.Stat,
                stat = BuffStat.MaxHp,
                valuePerRank = 2f,
                maxRank = 3,
                requires = "w_secondwind",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "w_unbreakable",
                branch = 1,
                row = 6,
                minLevel = 50,
                name = "Unbreakable",
                emoji = "🗿",
                description = "+2 armour.",
                effect = NodeEffect.Stat,
                stat = BuffStat.Armour,
                valuePerRank = 2f,
                requires = "w_bastion",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "w_rhythm",
                branch = 2,
                name = "Battle Rhythm",
                emoji = "🥁",
                description = "Skills recharge 4% faster per rank.",
                effect = NodeEffect.SkillCooldown,
                valuePerRank = 0.04f,
                maxRank = 5,
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "w_n_slam",
                branch = 2,
                row = 1,
                minLevel = 5,
                name = "Dawnbreaker",
                emoji = "🌅",
                description = "New skill: Leap ahead (onto the nearest enemy) and slam down: a ring of Light that hits and stuns everything around you. Boost: a bigger ring and a second shockwave.",
                moveId = "w_slam",
                requires = "w_rhythm",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "w_might",
                branch = 2,
                row = 2,
                minLevel = 10,
                name = "Mighty Swings",
                emoji = "🪓",
                description = "Skills hit 5% harder per rank.",
                effect = NodeEffect.SkillPower,
                valuePerRank = 0.05f,
                maxRank = 5,
                requires = "w_n_slam",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "w_n_warcry",
                branch = 2,
                row = 3,
                minLevel = 15,
                name = "Rally",
                emoji = "📯",
                description = "New skill: Roar and blaze: +40% damage for 6 seconds, and your light flares up.",
                moveId = "w_warcry",
                requires = "w_might",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "w_n_whirl",
                branch = 2,
                row = 4,
                minLevel = 30,
                name = "Whirlwind",
                emoji = "🌪️",
                description = "New skill: Spin for 1.5 seconds, hitting everything nearby again and again. You can still move.",
                moveId = "w_whirl",
                requires = "w_n_warcry",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "w_relentless",
                branch = 2,
                row = 5,
                minLevel = 35,
                name = "Relentless",
                emoji = "🔥",
                description = "Skills recharge 5% faster per rank.",
                effect = NodeEffect.SkillCooldown,
                valuePerRank = 0.05f,
                maxRank = 3,
                requires = "w_n_whirl",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "w_berserker",
                branch = 2,
                row = 6,
                minLevel = 45,
                name = "Berserker",
                emoji = "😤",
                description = "Skills hit 15% harder.",
                effect = NodeEffect.SkillPower,
                valuePerRank = 0.15f,
                requires = "w_relentless",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "r_steady",
                classId = ClassId.Ranger,
                name = "Steady Aim",
                emoji = "🎯",
                description = "+2% crit chance per rank.",
                effect = NodeEffect.Stat,
                stat = BuffStat.CritPct,
                valuePerRank = 0.02f,
                maxRank = 5,
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "r_n_spread",
                classId = ClassId.Ranger,
                row = 1,
                minLevel = 5,
                name = "Spread Shot",
                emoji = "🎐",
                description = "New attack: Five arrows in a wide fan. Weaker each, hard to dodge. Boost: +2 arrows per level.",
                moveId = "r_spread",
                requires = "r_steady",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "r_fletching",
                classId = ClassId.Ranger,
                row = 2,
                minLevel = 10,
                name = "Fletching",
                emoji = "🪶",
                description = "+2% damage per rank.",
                effect = NodeEffect.Stat,
                valuePerRank = 0.02f,
                maxRank = 5,
                requires = "r_n_spread",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "r_quickdraw",
                classId = ClassId.Ranger,
                row = 3,
                minLevel = 20,
                name = "Quick Draw",
                emoji = "🏹",
                description = "Attacks hit 5% harder per rank.",
                effect = NodeEffect.AttackPower,
                valuePerRank = 0.05f,
                maxRank = 3,
                requires = "r_fletching",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "r_n_longbow",
                classId = ClassId.Ranger,
                row = 4,
                minLevel = 25,
                name = "Longbow",
                emoji = "🎯",
                description = "New attack: A slow, heavy arrow that flies through every enemy in a line.",
                moveId = "r_longbow",
                requires = "r_quickdraw",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "r_deadeye",
                classId = ClassId.Ranger,
                row = 5,
                minLevel = 35,
                name = "Deadeye",
                emoji = "👁️",
                description = "+2% damage per rank.",
                effect = NodeEffect.Stat,
                valuePerRank = 0.02f,
                maxRank = 5,
                requires = "r_n_longbow",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "r_hawkeye",
                classId = ClassId.Ranger,
                row = 6,
                minLevel = 45,
                name = "Hawkeye",
                emoji = "🦅",
                description = "Attacks hit 15% harder.",
                effect = NodeEffect.AttackPower,
                valuePerRank = 0.15f,
                requires = "r_deadeye",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "r_lightpack",
                classId = ClassId.Ranger,
                branch = 1,
                name = "Light Pack",
                emoji = "🎒",
                description = "Move 6% faster per rank.",
                effect = NodeEffect.MoveSpeed,
                valuePerRank = 0.06f,
                maxRank = 5,
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "r_eye",
                classId = ClassId.Ranger,
                branch = 1,
                row = 1,
                minLevel = 10,
                name = "Forager's Eye",
                emoji = "🌿",
                description = "Gather zones 8% wider per rank.",
                effect = NodeEffect.Stat,
                stat = BuffStat.GatherZonePct,
                valuePerRank = 0.08f,
                maxRank = 5,
                requires = "r_lightpack",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "r_n_vault",
                classId = ClassId.Ranger,
                branch = 1,
                row = 2,
                minLevel = 15,
                name = "Back Vault",
                emoji = "🤸",
                description = "New dash: Flip backwards and loose a fan of 3 arrows at whatever you were facing.",
                moveId = "r_vault",
                requires = "r_eye",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "r_n_mark",
                classId = ClassId.Ranger,
                branch = 1,
                row = 3,
                minLevel = 20,
                name = "Spotlight",
                emoji = "🔦",
                description = "New skill: Pin a beam of light on the nearest enemy for 8 s: it's lit, takes +30% damage and shows all its weaknesses.",
                moveId = "r_mark",
                requires = "r_n_vault",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "r_scavenger",
                classId = ClassId.Ranger,
                branch = 1,
                row = 4,
                minLevel = 30,
                name = "Scavenger",
                emoji = "🪙",
                description = "+5% coins per rank.",
                effect = NodeEffect.Stat,
                stat = BuffStat.CoinPct,
                valuePerRank = 0.05f,
                maxRank = 3,
                requires = "r_n_mark",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "r_hardened",
                classId = ClassId.Ranger,
                branch = 1,
                row = 5,
                minLevel = 40,
                name = "Trail Hardened",
                emoji = "🥾",
                description = "+2 max HP per rank.",
                effect = NodeEffect.Stat,
                stat = BuffStat.MaxHp,
                valuePerRank = 2f,
                maxRank = 3,
                requires = "r_scavenger",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "r_fleet",
                classId = ClassId.Ranger,
                branch = 1,
                row = 6,
                minLevel = 50,
                name = "Fleet Foot",
                emoji = "🍃",
                description = "Dash recharges 25% faster.",
                effect = NodeEffect.DashCooldown,
                valuePerRank = 0.25f,
                requires = "r_hardened",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "r_nimble",
                classId = ClassId.Ranger,
                branch = 2,
                name = "Nimble Fingers",
                emoji = "🖐️",
                description = "Skills recharge 4% faster per rank.",
                effect = NodeEffect.SkillCooldown,
                valuePerRank = 0.04f,
                maxRank = 5,
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "r_n_trap",
                classId = ClassId.Ranger,
                branch = 2,
                row = 1,
                minLevel = 5,
                name = "Flare Arrow",
                emoji = "✨",
                description = "New skill: An arrow that bursts into a ball of light: hits everything nearby (Light), stuns shadow creatures, and shows secrets and weaknesses. Boost: a bigger burst.",
                moveId = "r_trap",
                requires = "r_nimble",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "r_barbed",
                classId = ClassId.Ranger,
                branch = 2,
                row = 2,
                minLevel = 10,
                name = "Barbed Tips",
                emoji = "🪝",
                description = "Skills hit 5% harder per rank.",
                effect = NodeEffect.SkillPower,
                valuePerRank = 0.05f,
                maxRank = 5,
                requires = "r_n_trap",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "r_n_rain",
                classId = ClassId.Ranger,
                branch = 2,
                row = 3,
                minLevel = 15,
                name = "Arrow Storm",
                emoji = "🌧️",
                description = "New skill: A storm of arrows over a wide area ahead for 2 seconds, lighting it up. Boost: a bigger storm.",
                moveId = "r_rain",
                requires = "r_barbed",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "r_n_smoke",
                classId = ClassId.Ranger,
                branch = 2,
                row = 4,
                minLevel = 30,
                name = "Smoke Bomb",
                emoji = "🌫️",
                description = "New skill: Enemies nearby are slowed by half for 3 seconds and you speed up.",
                moveId = "r_smoke",
                requires = "r_n_rain",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "r_pockets",
                classId = ClassId.Ranger,
                branch = 2,
                row = 5,
                minLevel = 35,
                name = "Hidden Pockets",
                emoji = "👝",
                description = "Skills recharge 5% faster per rank.",
                effect = NodeEffect.SkillCooldown,
                valuePerRank = 0.05f,
                maxRank = 3,
                requires = "r_n_smoke",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "r_mastertrap",
                classId = ClassId.Ranger,
                branch = 2,
                row = 6,
                minLevel = 45,
                name = "Master Trapper",
                emoji = "🧰",
                description = "Skills hit 15% harder.",
                effect = NodeEffect.SkillPower,
                valuePerRank = 0.15f,
                requires = "r_pockets",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "c_grease",
                classId = ClassId.Cook,
                name = "Elbow Grease",
                emoji = "💪",
                description = "+2% damage per rank.",
                effect = NodeEffect.Stat,
                valuePerRank = 0.02f,
                maxRank = 5,
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "c_n_skewer",
                classId = ClassId.Cook,
                row = 1,
                minLevel = 5,
                name = "Skewer",
                emoji = "🍢",
                description = "New attack: Jab forward with a kebab skewer.",
                moveId = "c_skewer",
                requires = "c_grease",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "c_tenderiser",
                classId = ClassId.Cook,
                row = 2,
                minLevel = 10,
                name = "Tenderiser",
                emoji = "🔨",
                description = "+2% crit chance per rank.",
                effect = NodeEffect.Stat,
                stat = BuffStat.CritPct,
                valuePerRank = 0.02f,
                maxRank = 5,
                requires = "c_n_skewer",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "c_heavyhand",
                classId = ClassId.Cook,
                row = 3,
                minLevel = 20,
                name = "Heavy Hand",
                emoji = "🥄",
                description = "Attacks hit 5% harder per rank.",
                effect = NodeEffect.AttackPower,
                valuePerRank = 0.05f,
                maxRank = 3,
                requires = "c_tenderiser",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "c_n_pan",
                classId = ClassId.Cook,
                row = 4,
                minLevel = 25,
                name = "Frying Pan",
                emoji = "🍳",
                description = "New attack: A ringing pan to the head. Stuns.",
                moveId = "c_pan",
                requires = "c_heavyhand",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "c_fury",
                classId = ClassId.Cook,
                row = 5,
                minLevel = 35,
                name = "Chef's Fury",
                emoji = "😠",
                description = "+2% damage per rank.",
                effect = NodeEffect.Stat,
                valuePerRank = 0.02f,
                maxRank = 5,
                requires = "c_n_pan",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "c_ironchef",
                classId = ClassId.Cook,
                row = 6,
                minLevel = 45,
                name = "Iron Chef",
                emoji = "👨‍🍳",
                description = "Attacks hit 15% harder.",
                effect = NodeEffect.AttackPower,
                valuePerRank = 0.15f,
                requires = "c_fury",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "c_wellfed",
                classId = ClassId.Cook,
                branch = 1,
                name = "Well Fed",
                emoji = "🍞",
                description = "+1 max HP per rank.",
                effect = NodeEffect.Stat,
                stat = BuffStat.MaxHp,
                valuePerRank = 1f,
                maxRank = 5,
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "c_apron",
                classId = ClassId.Cook,
                branch = 1,
                row = 1,
                minLevel = 10,
                name = "Thick Apron",
                emoji = "🧥",
                description = "+1 armour per rank (5% less damage each).",
                effect = NodeEffect.Stat,
                stat = BuffStat.Armour,
                valuePerRank = 1f,
                maxRank = 5,
                requires = "c_wellfed",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "c_n_slide",
                classId = ClassId.Cook,
                branch = 1,
                row = 2,
                minLevel = 15,
                name = "Butter Slide",
                emoji = "🧈",
                description = "New dash: Slide along a buttery trail. Enemies on it are slowed for 3 seconds.",
                moveId = "c_slide",
                requires = "c_apron",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "c_n_picnic",
                classId = ClassId.Cook,
                branch = 1,
                row = 3,
                minLevel = 20,
                name = "Cookfire",
                emoji = "🔥",
                description = "New skill: Drop a cookfire for 8 s: heals 2 every half second while you stand in its light. Light-shy monsters keep away and it lights braziers. Boost: bigger, and heals more.",
                moveId = "c_picnic",
                requires = "c_n_slide",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "c_leftovers",
                classId = ClassId.Cook,
                branch = 1,
                row = 4,
                minLevel = 30,
                name = "Leftovers",
                emoji = "🥡",
                description = "Heal 1 HP per rank whenever you defeat an enemy.",
                effect = NodeEffect.HealOnKill,
                valuePerRank = 1f,
                maxRank = 3,
                requires = "c_n_picnic",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "c_seconds",
                classId = ClassId.Cook,
                branch = 1,
                row = 5,
                minLevel = 40,
                name = "Second Helpings",
                emoji = "🍲",
                description = "+2 max HP per rank.",
                effect = NodeEffect.Stat,
                stat = BuffStat.MaxHp,
                valuePerRank = 2f,
                maxRank = 3,
                requires = "c_leftovers",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "c_homecooking",
                classId = ClassId.Cook,
                branch = 1,
                row = 6,
                minLevel = 50,
                name = "Home Cooking",
                emoji = "🏡",
                description = "+2 armour.",
                effect = NodeEffect.Stat,
                stat = BuffStat.Armour,
                valuePerRank = 2f,
                requires = "c_seconds",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "c_mise",
                classId = ClassId.Cook,
                branch = 2,
                name = "Mise en Place",
                emoji = "🔪",
                description = "Skills recharge 4% faster per rank.",
                effect = NodeEffect.SkillCooldown,
                valuePerRank = 0.04f,
                maxRank = 5,
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "c_n_pepper",
                classId = ClassId.Cook,
                branch = 2,
                row = 1,
                minLevel = 5,
                name = "Flambé",
                emoji = "🔥",
                description = "New skill: Blast a wide cone of flame (Fire): hurts and slows enemies by half for 3 s, with a huge burst of light. Boost: leaves the ground burning.",
                moveId = "c_pepper",
                requires = "c_mise",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "c_seasoning",
                classId = ClassId.Cook,
                branch = 2,
                row = 2,
                minLevel = 10,
                name = "Seasoning",
                emoji = "🧂",
                description = "Skills hit 5% harder per rank.",
                effect = NodeEffect.SkillPower,
                valuePerRank = 0.05f,
                maxRank = 5,
                requires = "c_n_pepper",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "c_n_hotpot",
                classId = ClassId.Cook,
                branch = 2,
                row = 3,
                minLevel = 15,
                name = "Hot Pot",
                emoji = "🍲",
                description = "New skill: Lob a pot ahead. It leaves a glowing, scalding pool for 4 seconds (Fire).",
                moveId = "c_hotpot",
                requires = "c_seasoning",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "c_n_pin",
                classId = ClassId.Cook,
                branch = 2,
                row = 4,
                minLevel = 30,
                name = "Rolling Pin",
                emoji = "🪵",
                description = "New skill: Throw a rolling pin that flies out and comes back, hitting everything twice.",
                moveId = "c_pin",
                requires = "c_n_hotpot",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "c_preptime",
                classId = ClassId.Cook,
                branch = 2,
                row = 5,
                minLevel = 35,
                name = "Prep Time",
                emoji = "⏲️",
                description = "Skills recharge 5% faster per rank.",
                effect = NodeEffect.SkillCooldown,
                valuePerRank = 0.05f,
                maxRank = 3,
                requires = "c_n_pin",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "c_signature",
                classId = ClassId.Cook,
                branch = 2,
                row = 6,
                minLevel = 45,
                name = "Signature Dish",
                emoji = "🌟",
                description = "Skills hit 15% harder.",
                effect = NodeEffect.SkillPower,
                valuePerRank = 0.15f,
                requires = "c_preptime",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "l_focus",
                classId = ClassId.Lamplighter,
                name = "Focus",
                emoji = "🎯",
                description = "+2% crit chance per rank.",
                effect = NodeEffect.Stat,
                stat = BuffStat.CritPct,
                valuePerRank = 0.02f,
                maxRank = 5,
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "l_bright",
                classId = ClassId.Lamplighter,
                row = 1,
                minLevel = 5,
                name = "Light Lance",
                emoji = "🔆",
                description = "New attack: A fast bolt of light that pierces every enemy in a line. Boost: +1 lance per level.",
                moveId = "l_lance",
                requires = "l_focus",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "l_kindled",
                classId = ClassId.Lamplighter,
                row = 2,
                minLevel = 10,
                name = "Kindled Mind",
                emoji = "🔥",
                description = "+2% damage per rank.",
                effect = NodeEffect.Stat,
                valuePerRank = 0.02f,
                maxRank = 5,
                requires = "l_bright",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "l_clarity",
                classId = ClassId.Lamplighter,
                row = 3,
                minLevel = 20,
                name = "Clarity",
                emoji = "💎",
                description = "+2% crit chance per rank.",
                effect = NodeEffect.Stat,
                stat = BuffStat.CritPct,
                valuePerRank = 0.02f,
                maxRank = 5,
                requires = "l_kindled",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "l_beacon",
                classId = ClassId.Lamplighter,
                row = 4,
                minLevel = 25,
                name = "Ember Swarm",
                emoji = "🔥",
                description = "New attack: Loose four little homing embers (Fire): weak alone, deadly together. Boost: +2 embers per level.",
                moveId = "l_swarm",
                requires = "l_clarity",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "l_brilliance",
                classId = ClassId.Lamplighter,
                row = 5,
                minLevel = 35,
                name = "Brilliance",
                emoji = "🌟",
                description = "+2% damage per rank.",
                effect = NodeEffect.Stat,
                valuePerRank = 0.02f,
                maxRank = 5,
                requires = "l_beacon",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "l_dawn",
                classId = ClassId.Lamplighter,
                row = 6,
                minLevel = 45,
                name = "First Light",
                emoji = "🌅",
                description = "Attacks hit 15% harder.",
                effect = NodeEffect.AttackPower,
                valuePerRank = 0.15f,
                requires = "l_brilliance",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "l_warm",
                classId = ClassId.Lamplighter,
                branch = 1,
                name = "Warm Hands",
                emoji = "🧤",
                description = "+1 max HP per rank.",
                effect = NodeEffect.Stat,
                stat = BuffStat.MaxHp,
                valuePerRank = 1f,
                maxRank = 5,
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "l_ward",
                classId = ClassId.Lamplighter,
                branch = 1,
                row = 1,
                minLevel = 10,
                name = "Lamp Ward",
                emoji = "🛡️",
                description = "+1 armour per rank (5% less damage each).",
                effect = NodeEffect.Stat,
                stat = BuffStat.Armour,
                valuePerRank = 1f,
                maxRank = 5,
                requires = "l_warm",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "l_flicker",
                classId = ClassId.Lamplighter,
                branch = 1,
                row = 2,
                minLevel = 15,
                name = "Flicker",
                emoji = "💫",
                description = "Dash recharges 8% faster per rank.",
                effect = NodeEffect.DashCooldown,
                valuePerRank = 0.08f,
                maxRank = 3,
                requires = "l_ward",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "l_drift",
                classId = ClassId.Lamplighter,
                branch = 1,
                row = 3,
                minLevel = 20,
                name = "Drifting Light",
                emoji = "🍃",
                description = "Move 6% faster per rank.",
                effect = NodeEffect.MoveSpeed,
                valuePerRank = 0.06f,
                maxRank = 5,
                requires = "l_flicker",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "l_afterglow",
                classId = ClassId.Lamplighter,
                branch = 1,
                row = 4,
                minLevel = 30,
                name = "Afterglow",
                emoji = "🕯️",
                description = "Heal 1 HP per rank whenever you defeat an enemy.",
                effect = NodeEffect.HealOnKill,
                valuePerRank = 1f,
                maxRank = 3,
                requires = "l_drift",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "l_hearthlight",
                classId = ClassId.Lamplighter,
                branch = 1,
                row = 5,
                minLevel = 40,
                name = "Hearthlight",
                emoji = "🏡",
                description = "+2 max HP per rank.",
                effect = NodeEffect.Stat,
                stat = BuffStat.MaxHp,
                valuePerRank = 2f,
                maxRank = 3,
                requires = "l_afterglow",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "l_everflame",
                classId = ClassId.Lamplighter,
                branch = 1,
                row = 6,
                minLevel = 50,
                name = "Everflame",
                emoji = "🔥",
                description = "+2 armour.",
                effect = NodeEffect.Stat,
                stat = BuffStat.Armour,
                valuePerRank = 2f,
                requires = "l_hearthlight",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "l_trim",
                classId = ClassId.Lamplighter,
                branch = 2,
                name = "Trimmed Wick",
                emoji = "✂️",
                description = "Skills recharge 4% faster per rank.",
                effect = NodeEffect.SkillCooldown,
                valuePerRank = 0.04f,
                maxRank = 5,
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "l_n_sunbeam",
                classId = ClassId.Lamplighter,
                branch = 2,
                row = 1,
                minLevel = 5,
                name = "Sunbeam",
                emoji = "☀️",
                description = "New skill: Channel a beam of sunlight for 1.5 s and steer it as you move: it hits everything in its line again and again (Light). Boost: longer and wider.",
                moveId = "l_sunbeam",
                requires = "l_trim",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "l_lens",
                classId = ClassId.Lamplighter,
                branch = 2,
                row = 2,
                minLevel = 10,
                name = "Lens",
                emoji = "🔍",
                description = "Skills hit 5% harder per rank.",
                effect = NodeEffect.SkillPower,
                valuePerRank = 0.05f,
                maxRank = 5,
                requires = "l_n_sunbeam",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "l_n_prism",
                classId = ClassId.Lamplighter,
                branch = 2,
                row = 3,
                minLevel = 15,
                name = "Prism",
                emoji = "🔮",
                description = "New skill: A crystal bolt that splits on impact: it strikes its target with every one of its weaknesses at once (great for Breaking) and showers the enemies nearby. Boost: knocks off more shield.",
                moveId = "l_prism",
                requires = "l_lens",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "l_n_kindle",
                classId = ClassId.Lamplighter,
                branch = 2,
                row = 4,
                minLevel = 30,
                name = "Kindle",
                emoji = "🕯️",
                description = "New skill: Light every brazier in the room at once and flood it with warm light; heal 3 HP for each brazier (up to 12).",
                moveId = "l_kindle",
                requires = "l_n_prism",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "l_oil",
                classId = ClassId.Lamplighter,
                branch = 2,
                row = 5,
                minLevel = 35,
                name = "Lamp Oil",
                emoji = "🛢️",
                description = "Skills recharge 5% faster per rank.",
                effect = NodeEffect.SkillCooldown,
                valuePerRank = 0.05f,
                maxRank = 3,
                requires = "l_n_kindle",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "l_radiant",
                classId = ClassId.Lamplighter,
                branch = 2,
                row = 6,
                minLevel = 45,
                name = "Radiant",
                emoji = "☀️",
                description = "Skills hit 15% harder.",
                effect = NodeEffect.SkillPower,
                valuePerRank = 0.15f,
                requires = "l_oil",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "e_knuckles",
                classId = ClassId.Emberfist,
                name = "Iron Knuckles",
                emoji = "💪",
                description = "+2% damage per rank.",
                effect = NodeEffect.Stat,
                valuePerRank = 0.02f,
                maxRank = 5,
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "e_jab",
                classId = ClassId.Emberfist,
                row = 1,
                minLevel = 5,
                name = "Ember Kick",
                emoji = "🦶",
                description = "New attack: A long, sweeping kick that hits everything in a line (Slash). Boost: a wider kick.",
                moveId = "e_kick",
                requires = "e_knuckles",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "e_pressure",
                classId = ClassId.Emberfist,
                row = 2,
                minLevel = 10,
                name = "Pressure Points",
                emoji = "🎯",
                description = "+2% crit chance per rank.",
                effect = NodeEffect.Stat,
                stat = BuffStat.CritPct,
                valuePerRank = 0.02f,
                maxRank = 5,
                requires = "e_jab",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "e_stoke",
                classId = ClassId.Emberfist,
                row = 3,
                minLevel = 20,
                name = "Stoked",
                emoji = "🔥",
                description = "+2% damage per rank.",
                effect = NodeEffect.Stat,
                valuePerRank = 0.02f,
                maxRank = 5,
                requires = "e_pressure",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "e_hammer",
                classId = ClassId.Emberfist,
                row = 4,
                minLevel = 25,
                name = "Burning Palm",
                emoji = "🖐️",
                description = "New attack: A slow, crushing palm strike that stuns (Blunt). Boost: a bigger blast.",
                moveId = "e_palm",
                requires = "e_stoke",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "e_focus",
                classId = ClassId.Emberfist,
                row = 5,
                minLevel = 35,
                name = "Inner Focus",
                emoji = "🧘",
                description = "+2% crit chance per rank.",
                effect = NodeEffect.Stat,
                stat = BuffStat.CritPct,
                valuePerRank = 0.02f,
                maxRank = 5,
                requires = "e_hammer",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "e_dragonfist",
                classId = ClassId.Emberfist,
                row = 6,
                minLevel = 45,
                name = "Dragon Fist",
                emoji = "🐉",
                description = "Attacks hit 15% harder.",
                effect = NodeEffect.AttackPower,
                valuePerRank = 0.15f,
                requires = "e_focus",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "e_hardy",
                classId = ClassId.Emberfist,
                branch = 1,
                name = "Hardy",
                emoji = "🪨",
                description = "+1 max HP per rank.",
                effect = NodeEffect.Stat,
                stat = BuffStat.MaxHp,
                valuePerRank = 1f,
                maxRank = 5,
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "e_wraps",
                classId = ClassId.Emberfist,
                branch = 1,
                row = 1,
                minLevel = 10,
                name = "Hand Wraps",
                emoji = "🧣",
                description = "+1 armour per rank (5% less damage each).",
                effect = NodeEffect.Stat,
                stat = BuffStat.Armour,
                valuePerRank = 1f,
                maxRank = 5,
                requires = "e_hardy",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "e_light",
                classId = ClassId.Emberfist,
                branch = 1,
                row = 2,
                minLevel = 15,
                name = "Light Feet",
                emoji = "🍃",
                description = "Move 6% faster per rank.",
                effect = NodeEffect.MoveSpeed,
                valuePerRank = 0.06f,
                maxRank = 5,
                requires = "e_wraps",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "e_quickstep",
                classId = ClassId.Emberfist,
                branch = 1,
                row = 3,
                minLevel = 20,
                name = "Quickstep",
                emoji = "💨",
                description = "Dash recharges 8% faster per rank.",
                effect = NodeEffect.DashCooldown,
                valuePerRank = 0.08f,
                maxRank = 3,
                requires = "e_light",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "e_feast",
                classId = ClassId.Emberfist,
                branch = 1,
                row = 4,
                minLevel = 30,
                name = "Burning Appetite",
                emoji = "🍖",
                description = "Heal 1 HP per rank whenever you defeat an enemy.",
                effect = NodeEffect.HealOnKill,
                valuePerRank = 1f,
                maxRank = 3,
                requires = "e_quickstep",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "e_tough",
                classId = ClassId.Emberfist,
                branch = 1,
                row = 5,
                minLevel = 40,
                name = "Tempered",
                emoji = "⚒️",
                description = "+2 max HP per rank.",
                effect = NodeEffect.Stat,
                stat = BuffStat.MaxHp,
                valuePerRank = 2f,
                maxRank = 3,
                requires = "e_feast",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "e_unburnt",
                classId = ClassId.Emberfist,
                branch = 1,
                row = 6,
                minLevel = 50,
                name = "Unburnt",
                emoji = "🛡️",
                description = "+2 armour.",
                effect = NodeEffect.Stat,
                stat = BuffStat.Armour,
                valuePerRank = 2f,
                requires = "e_tough",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "e_breath",
                classId = ClassId.Emberfist,
                branch = 2,
                name = "Steady Breath",
                emoji = "🌬️",
                description = "Skills recharge 4% faster per rank.",
                effect = NodeEffect.SkillCooldown,
                valuePerRank = 0.04f,
                maxRank = 5,
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "e_n_wheel",
                classId = ClassId.Emberfist,
                branch = 2,
                row = 1,
                minLevel = 5,
                name = "Flame Wheel",
                emoji = "🌀",
                description = "New skill: A spinning kick that hits everything around you twice (Slash). Boost: a bigger wheel.",
                moveId = "e_wheel",
                requires = "e_breath",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "e_kindling",
                classId = ClassId.Emberfist,
                branch = 2,
                row = 2,
                minLevel = 10,
                name = "Kindling",
                emoji = "🪵",
                description = "Skills hit 5% harder per rank.",
                effect = NodeEffect.SkillPower,
                valuePerRank = 0.05f,
                maxRank = 5,
                requires = "e_n_wheel",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "e_n_detonate",
                classId = ClassId.Emberfist,
                branch = 2,
                row = 3,
                minLevel = 15,
                name = "Detonate",
                emoji = "💥",
                description = "New skill: Spend all your Heat in one explosion of fire: the more Heat, the bigger and harder it hits (Fire). Needs at least 20 Heat.",
                moveId = "e_detonate",
                requires = "e_kindling",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "e_n_counter",
                classId = ClassId.Emberfist,
                branch = 2,
                row = 4,
                minLevel = 30,
                name = "Cinder Step",
                emoji = "🔥",
                description = "New skill: Take a fighting stance for a moment: if anything hits you, you take no damage and strike back in a burst of fire (+20 Heat). Boost: a longer stance.",
                moveId = "e_counter",
                requires = "e_n_detonate",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "e_bellows",
                classId = ClassId.Emberfist,
                branch = 2,
                row = 5,
                minLevel = 35,
                name = "Bellows",
                emoji = "🔥",
                description = "Skills recharge 5% faster per rank.",
                effect = NodeEffect.SkillCooldown,
                valuePerRank = 0.05f,
                maxRank = 3,
                requires = "e_n_counter",
            });
            c.skillNodes.Add(new SkillNodeData
            {
                id = "e_inferno",
                classId = ClassId.Emberfist,
                branch = 2,
                row = 6,
                minLevel = 45,
                name = "Inferno",
                emoji = "🌋",
                description = "Skills hit 15% harder.",
                effect = NodeEffect.SkillPower,
                valuePerRank = 0.15f,
                requires = "e_bellows",
            });
        }
    }
}
