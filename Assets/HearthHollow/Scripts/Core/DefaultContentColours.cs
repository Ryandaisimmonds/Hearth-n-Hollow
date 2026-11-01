// Hearth & Hollow — class colour themes + brighter, bigger basic moves (contentVersion 21). Pure C#.
//   Warden      sunlight gold + pale sun        Ranger   emerald + moonlit mint
//   Cook        hearth orange + butter yellow   Lamplighter  azure + violet
//   Emberfist   crimson + magenta flame
// Every move gets its own colour and a bigger light; the simple moves (attacks, dashes, the plain area skills) reach further.
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public static partial class DefaultContent
    {
        static readonly string[,] MoveColours21 =
        {
            // Warden — sunlight
            { "w_sweep", "FFB000" }, { "w_lunge", "FFC21A" }, { "w_maul", "FFD000" }, { "w_cleave", "FF8C00" }, { "w_slam", "FFC800" },
            { "w_warcry", "FFA000" }, { "w_shieldwall", "FFD54A" }, { "w_whirl", "FFB300" }, { "w_charge", "FFC21A" }, { "w_ult", "FFC000" },
            // Ranger — emerald and moonlight
            { "r_shot", "00E676" }, { "r_spread", "2BFF88" }, { "r_longbow", "00E5C0" }, { "r_volley", "7CFF00" }, { "r_trap", "00FFC8" },
            { "r_mark", "00FFA0" }, { "r_rain", "1DE9B6" }, { "r_smoke", "00C896" }, { "r_vault", "00E676" }, { "r_ult", "00FFB0" },
            // Cook — hearth fire and butter
            { "c_ladle", "FF7A00" }, { "c_skewer", "FF9500" }, { "c_pan", "FFC400" }, { "c_snack", "FF8C00" }, { "c_pepper", "FF3D00" },
            { "c_hotpot", "FF6A00" }, { "c_picnic", "FFAB00" }, { "c_pin", "FFC400" }, { "c_slide", "FFD600" }, { "c_ult", "FF8F00" },
            // Lamplighter — azure and violet
            { "l_wisp", "2EA8FF" }, { "l_lance", "4FC3FF" }, { "l_swarm", "A040FF" }, { "l_lantern", "3D8BFF" }, { "l_sunbeam", "40C4FF" },
            { "l_prism", "B04DFF" }, { "l_kindle", "5C6CFF" }, { "l_blink", "9B5CFF" }, { "l_ult", "8A4DFF" },
            // Emberfist — crimson and magenta flame
            { "e_fists", "FF3B3B" }, { "e_kick", "FF5FA8" }, { "e_palm", "FF2E4E" }, { "e_phoenix", "FF4A2A" }, { "e_wheel", "FF5FA8" },
            { "e_detonate", "FF2E2E" }, { "e_counter", "FF7A9E" }, { "e_flicker", "FF3B6A" }, { "e_ult", "FF2E6A" },
        };

        /// contentVersion 22: the first palette was too pale (white with a tinge) — re-apply the stronger colours.
        static void Colours22(GameContent c) { ApplyMoveColours(c); }

        static void ApplyMoveColours(GameContent c)
        {
            if (c.moves == null) return;
            for (int i = 0; i < MoveColours21.GetLength(0); i++)
            {
                var m = c.moves.Find(x => x.id == MoveColours21[i, 0]);
                if (m != null) m.lightColour = MoveColours21[i, 1];
            }
        }

        static void Colours21(GameContent c)
        {
            if (c.moves == null) return;
            ApplyMoveColours(c);
            foreach (var m in c.moves)
            {
                // light everywhere: every move throws a decent light
                float minR = m.slot == MoveSlot.Attack ? 4f : m.slot == MoveSlot.Dash ? 3.5f : m.slot == MoveSlot.Skill ? 5.5f : 9f;
                if (m.lightRadius < minR) m.lightRadius = minR;
                if (m.lightIntensity < 1.9f) m.lightIntensity = 1.9f;
                // the simple moves get bigger
                switch (m.kind)
                {
                    case MoveKind.Combo: m.radius *= 1.25f; m.range *= 1.15f; break;
                    case MoveKind.Swing: m.radius *= 1.25f; m.range *= 1.15f; break;
                    case MoveKind.Lunge: m.radius *= 1.3f; m.range *= 1.25f; break;
                    case MoveKind.Heavy: m.radius *= 1.3f; m.range *= 1.1f; break;
                    case MoveKind.Cleave: case MoveKind.FlameWheel: case MoveKind.Whirlwind: m.radius *= 1.2f; break;
                    case MoveKind.Smoke: m.radius *= 1.2f; break;
                }
            }
        }
    }
}
