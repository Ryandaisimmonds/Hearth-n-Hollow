// Hearth & Hollow — ultimates (contentVersion 20). Pure C#.
// Every class gets one ultimate on its own button (C / I / right-stick click). It has a long cooldown (45 s) that
// every hit you land shortens (DungeonSettings.ultChargePerHit). Skills recharge faster to match.
// Numbers by kind:
//   Sunfall    range = leap, radius = ring, duration = stun, value = +damage for 6 s afterwards
//   Starfall   range = distance (or the cursor), radius, duration, value = +damage taken (Spotlit) while inside
//   GrandFeast radius = fire ring, duration = cookfire seconds, value = heal
//   Dawn       radius = reach (the whole room), duration = stun, value = shield pips knocked off
//   Supernova  radius = blast, duration = stun, value = seconds your Heat stays full afterwards
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public static partial class DefaultContent
    {
        static MoveData Ult(GameContent c, string id, ClassId owner, MoveKind kind, string name, string emoji, string desc,
                            HitType hit, float power, float range, float radius, float duration, float value, float light)
        {
            var m = c.moves.Find(x => x.id == id);
            if (m == null) { m = new MoveData { id = id }; c.moves.Add(m); }
            m.owner = owner; m.slot = MoveSlot.Ultimate; m.kind = kind; m.name = name; m.emoji = emoji; m.description = desc;
            m.hitType = hit; m.hitType2 = hit; m.power = power; m.cooldown = 45f; m.range = range; m.radius = radius;
            m.duration = duration; m.value = value; m.lightRadius = light; m.lightIntensity = 2.6f; m.aimAssist = 0f;
            var cls = c.classes.Find(x => x.id == owner);
            if (cls != null) cls.ultimate = id;
            return m;
        }

        static void Ultimates20(GameContent c)
        {
            if (c.moves == null) return;
            bool first = !c.moves.Exists(x => x.slot == MoveSlot.Ultimate);

            Ult(c, "w_ult", ClassId.Warden, MoveKind.Sunfall, "Sunfall", "☀️",
                "Leap to where you're aiming and bring the sun down: a huge ring of Light that hits and stuns everything in it, then a second shockwave. +50% damage for 6 s afterwards.",
                HitType.Light, 3.0f, 6f, 3.4f, 2f, 0.5f, 11f);
            Ult(c, "r_ult", ClassId.Ranger, MoveKind.Starfall, "Starfall", "🌠",
                "Call down a storm of falling stars where you're aiming for 3 s (Light). Everything caught in it is Spotlit and takes +30% damage.",
                HitType.Light, 0.7f, 6f, 3.4f, 3f, 0.3f, 10f);
            Ult(c, "c_ult", ClassId.Cook, MoveKind.GrandFeast, "Grand Feast", "🍲",
                "Bang the big pot: a ring of fire blasts and slows everything around you (Fire), you heal 30, and a great cookfire burns for 8 s, healing you while you stand in its light.",
                HitType.Fire, 2.0f, 1f, 3.2f, 8f, 30f, 9f);
            Ult(c, "l_ult", ClassId.Lamplighter, MoveKind.Dawn, "Dawn", "🌅",
                "Raise your lamp and call up the dawn: every brazier lights, the room floods with light, and every enemy in it takes a Light hit that knocks off 3 shield pips and stuns it.",
                HitType.Light, 2.0f, 1f, 12f, 1.5f, 3f, 14f);
            Ult(c, "e_ult", ClassId.Emberfist, MoveKind.Supernova, "Supernova", "💥",
                "Pull in every ember: your Heat fills, then a huge explosion of fire around you (Fire) that stuns, and your Heat stays full for 6 s.",
                HitType.Fire, 3.2f, 1f, 3.6f, 1.5f, 6f, 12f);

            // skills recharge ~30% faster to go with the ultimates (only the first time)
            if (first)
                foreach (var m in c.moves)
                    if (m.slot == MoveSlot.Skill && m.cooldown > 1f)
                        m.cooldown = UnityEngineFree.Round05(m.cooldown * 0.7f);
        }

        static class UnityEngineFree
        {
            public static float Round05(float v) { return (float)System.Math.Round(v * 2.0, System.MidpointRounding.AwayFromZero) / 2f; }
        }
    }
}
