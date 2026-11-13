// Hearth & Hollow — the player's skills (K / L / the ultimate), one handler per MoveKind. UseSkill does what every
// skill shares (aim, light, the element) and then looks the move up here. The handlers live in PlayerSkills.<class>.cs.
using System;
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Dungeon
{
    public partial class PlayerController
    {
        delegate bool SkillUse(PlayerController p, MoveData m, Vector2 dir, float mult);

        /// Each skill's handler: true if it was used (false = nothing happened, e.g. Hunter's Mark with nobody to mark).
        static readonly Dictionary<MoveKind, SkillUse> Skills = new Dictionary<MoveKind, SkillUse>
        {
            { MoveKind.Sunfall, (p, m, d, x) => p.UseSunfall(m, d, x) },
            { MoveKind.Starfall, (p, m, d, x) => p.UseStarfall(m, d, x) },
            { MoveKind.GrandFeast, (p, m, d, x) => p.UseGrandFeast(m, d, x) },
            { MoveKind.Dawn, (p, m, d, x) => p.UseDawn(m, d, x) },
            { MoveKind.Supernova, (p, m, d, x) => p.UseSupernova(m, d, x) },
            { MoveKind.Cleave, (p, m, d, x) => p.UseCleave(m, d, x) },
            { MoveKind.Shockwave, (p, m, d, x) => p.UseShockwave(m, d, x) },
            { MoveKind.WarCry, (p, m, d, x) => p.UseWarCry(m, d, x) },
            { MoveKind.ShieldWall, (p, m, d, x) => p.UseShieldWall(m, d, x) },
            { MoveKind.Whirlwind, (p, m, d, x) => p.UseWhirlwind(m, d, x) },
            { MoveKind.Sunshield, (p, m, d, x) => p.UseSunshield(m, d, x) },
            { MoveKind.LeapSlam, (p, m, d, x) => p.UseLeapSlam(m, d, x) },
            { MoveKind.Volley, (p, m, d, x) => p.UseVolley(m, d, x) },
            { MoveKind.Trap, (p, m, d, x) => p.UseTrap(m, d, x) },
            { MoveKind.Mark, (p, m, d, x) => p.UseMark(m, d, x) },
            { MoveKind.ArrowRain, (p, m, d, x) => p.UseArrowRain(m, d, x) },
            { MoveKind.Smoke, (p, m, d, x) => p.UseSmoke(m, d, x) },
            { MoveKind.EmberArrow, (p, m, d, x) => p.UseEmberArrow(m, d, x) },
            { MoveKind.FlareArrow, (p, m, d, x) => p.UseFlareArrow(m, d, x) },
            { MoveKind.Spotlight, (p, m, d, x) => p.UseSpotlight(m, d, x) },
            { MoveKind.Lantern, (p, m, d, x) => p.UseLantern(m, d, x) },
            { MoveKind.Sunbeam, (p, m, d, x) => p.UseSunbeam(m, d, x) },
            { MoveKind.Prism, (p, m, d, x) => p.UsePrism(m, d, x) },
            { MoveKind.Kindle, (p, m, d, x) => p.UseKindle(m, d, x) },
            { MoveKind.Phoenix, (p, m, d, x) => p.UsePhoenix(m, d, x) },
            { MoveKind.FlameWheel, (p, m, d, x) => p.UseFlameWheel(m, d, x) },
            { MoveKind.Detonate, (p, m, d, x) => p.UseDetonate(m, d, x) },
            { MoveKind.Counter, (p, m, d, x) => p.UseCounter(m, d, x) },
            { MoveKind.Snack, (p, m, d, x) => p.UseSnack(m, d, x) },
            { MoveKind.PepperToss, (p, m, d, x) => p.UsePepperToss(m, d, x) },
            { MoveKind.HotPot, (p, m, d, x) => p.UseHotPot(m, d, x) },
            { MoveKind.Picnic, (p, m, d, x) => p.UsePicnic(m, d, x) },
            { MoveKind.SkewerToss, (p, m, d, x) => p.UseSkewerToss(m, d, x) },
            { MoveKind.Cookfire, (p, m, d, x) => p.UseCookfire(m, d, x) },
            { MoveKind.RollingPin, (p, m, d, x) => p.UseRollingPin(m, d, x) },
        };
    }
}
