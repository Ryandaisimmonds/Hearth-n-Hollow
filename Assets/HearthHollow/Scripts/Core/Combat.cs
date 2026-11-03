// Hearth & Hollow — Combat (Agent A). Pure C#, no UnityEngine.
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    // Extra fight state needed by Combat (added via partial, per CONTRACT).
    public partial class FightState
    {
        /// Turn on which the shaman last healed (-100 = never). Heal allowed at most once every other turn.
        public int monsterLastHealTurn = -100;
        /// Set by Guard / Hearty Snack: the next attacking action is a riposte (×riposteMult; a riposte Strike always hits).
        public bool riposteReady;
    }

    public static class Combat
    {
        const float EliteHpMult = 1.5f;
        const float EliteAtkMult = 1.25f;

        // ───────────────────────── Setup ─────────────────────────
        public static FightState StartFight(GameContent c, Tile tile, RunState run, int skillCooldown)
        {
            TuningData t = c.tuning;
            MonsterData m = tile.monsterId != null ? c.Monster(tile.monsterId) : null;
            bool elite = tile.type == TileType.Elite;
            float hpMult = m != null ? m.hpMult : 1f;
            int diff = tile.difficulty;

            float hp = (t.monsterHpBase + diff * t.monsterHpPerDiff) * hpMult * (elite ? EliteHpMult : 1f);
            float atk = (t.monsterAtkBase + diff * t.monsterAtkPerDiff) * (elite ? EliteAtkMult : 1f);

            FightState f = new FightState();
            f.monsterId = tile.monsterId;
            f.elite = elite;
            f.difficulty = diff;
            f.monsterMaxHp = Math.Max(1, (int)Math.Round(hp, MidpointRounding.AwayFromZero));
            f.monsterHp = f.monsterMaxHp;
            f.monsterAtk = atk;
            f.skillCooldown = Math.Max(0, skillCooldown);
            f.turn = 0;
            if (run != null) run.fight = f;
            return f;
        }

        public static bool CanUseSkill(GameContent c, Profile p, RunState run)
        {
            if (run == null || p == null) return false;
            int cd = run.fight != null ? run.fight.skillCooldown : run.skillCooldown;
            return cd <= 0;
        }

        public static float ApplyArmour(float raw, float armourPct)
        {
            float a = armourPct;
            if (a < 0f) a = 0f;
            if (a > 1f) a = 1f;
            return raw * (1f - a);
        }

        // ───────────────────────── Turn ─────────────────────────
        public static List<GameEvent> PlayerTurn(GameContent c, Profile p, RunState run, PlayerStats stats, FightAction action, Rng rng)
        {
            List<GameEvent> ev = new List<GameEvent>();
            FightState f = run != null ? run.fight : null;
            if (f == null || f.over)
            {
                ev.Add(new GameEvent(GameEventType.Info, 0, false, "No fight in progress."));
                return ev;
            }
            if (action == FightAction.Skill && !CanUseSkill(c, p, run))
            {
                ev.Add(new GameEvent(GameEventType.Info, 0, false, "Skill not ready."));
                return ev;
            }

            TuningData t = c.tuning;
            ClassData cls = c.Class(run.classId);
            SkillData skill = cls != null ? cls.skill : null;
            MonsterData mon = c.Monster(f.monsterId);
            MonsterBehaviour beh = mon != null ? mon.behaviour : MonsterBehaviour.None;

            bool guarding = false;
            bool usedSkill = false;
            float baseDmg = stats.damage;
            float crit = stats.critChance;
            bool attacking = action == FightAction.Strike || action == FightAction.Lucky
                || (action == FightAction.Skill && skill != null && skill.kind != SkillKind.HeartySnack);
            bool riposte = attacking && f.riposteReady;
            if (riposte)
            {
                baseDmg *= t.riposteMult;
                f.riposteReady = false;
                ev.Add(new GameEvent(GameEventType.Riposte, 0, false, "Riposte!"));
            }

            // ── Player action ──
            if (action == FightAction.Strike)
            {
                float hitChance = f.lastStrikeMissed ? t.strikeHitAfterMiss : t.strikeHit;
                if (riposte || rng.Chance(hitChance))
                {
                    f.lastStrikeMissed = false;
                    DealHit(t, f, ev, baseDmg, crit, rng);
                }
                else
                {
                    f.lastStrikeMissed = true;
                    ev.Add(new GameEvent(GameEventType.PlayerMiss, 0, false, "Miss"));
                }
            }
            else if (action == FightAction.Guard)
            {
                guarding = true;
                HealPlayer(run, stats, t.guardHeal, ev);
                ev.Add(new GameEvent(GameEventType.PlayerGuard, 0, true, "Guard"));
            }
            else if (action == FightAction.Lucky)
            {
                DealHit(t, f, ev, baseDmg * t.luckyDamageMult, crit * t.luckyCritMult, rng);
            }
            else // Skill
            {
                usedSkill = true;
                SkillKind kind = skill != null ? skill.kind : SkillKind.Cleave;
                float val = skill != null ? skill.value : 0f;
                if (kind == SkillKind.Cleave)
                {
                    DealHit(t, f, ev, baseDmg * val, crit, rng);
                }
                else if (kind == SkillKind.Volley)
                {
                    DealHit(t, f, ev, baseDmg * val, crit, rng);
                    if (f.monsterHp > 0) DealHit(t, f, ev, baseDmg * val, crit, rng);
                }
                else // HeartySnack: heal + counts as Guard this turn
                {
                    guarding = true;
                    HealPlayer(run, stats, (int)Math.Round(val, MidpointRounding.AwayFromZero), ev);
                    ev.Add(new GameEvent(GameEventType.PlayerGuard, 0, true, skill != null ? skill.name : "Hearty Snack"));
                }
                f.skillCooldown = skill != null ? Math.Max(0, skill.cooldownTurns) : 0;
            }

            // ── Monster response ──
            if (f.monsterHp <= 0)
            {
                Win(run, f, ev);
            }
            else
            {
                bool attacked = MonsterAct(t, f, run, stats, beh, guarding, ev, rng);
                if (run.hp <= 0)
                {
                    run.hp = 0;
                    f.over = true; f.playerWon = false;
                    ev.Add(new GameEvent(GameEventType.PlayerDied, 0, true, "You fall..."));
                }
                else if (guarding)
                {
                    f.riposteReady = true;   // tempo: the payoff lands on your next attack
                }
            }

            // ── Bookkeeping ──
            if (!usedSkill && f.skillCooldown > 0) f.skillCooldown--;
            f.turn++;
            if (f.over) run.skillCooldown = f.skillCooldown;
            return ev;
        }

        /// Returns true if the monster actually attacked this turn.
        static bool MonsterAct(TuningData t, FightState f, RunState run, PlayerStats stats, MonsterBehaviour beh,
                               bool guarding, List<GameEvent> ev, Rng rng)
        {
            if (beh == MonsterBehaviour.Heal)
            {
                bool low = f.monsterHp * 2 < f.monsterMaxHp;
                bool allowed = f.turn - f.monsterLastHealTurn >= 2;
                if (low && allowed)
                {
                    int heal = Math.Max(1, (int)Math.Round(f.monsterMaxHp * t.shamanHealPct, MidpointRounding.AwayFromZero));
                    int before = f.monsterHp;
                    f.monsterHp = Math.Min(f.monsterMaxHp, f.monsterHp + heal);
                    f.monsterLastHealTurn = f.turn;
                    ev.Add(new GameEvent(GameEventType.MonsterHeal, f.monsterHp - before, false, "Heals"));
                    return false;
                }
                MonsterHit(t, f, run, stats, 1f, guarding, ev);
                return true;
            }
            if (beh == MonsterBehaviour.WindUp)
            {
                if (f.monsterWindingUp)
                {
                    f.monsterWindingUp = false;
                    MonsterHit(t, f, run, stats, t.dragonWindUpMult, guarding, ev);
                    return true;
                }
                if (rng.Chance(t.dragonWindUpChance))
                {
                    f.monsterWindingUp = true;
                    ev.Add(new GameEvent(GameEventType.MonsterWindUp, 0, false, "Winding up!"));
                    return false;
                }
                MonsterHit(t, f, run, stats, 1f, guarding, ev);
                return true;
            }
            if (beh == MonsterBehaviour.DoubleHit)
            {
                MonsterHit(t, f, run, stats, 1f, guarding, ev);
                if (run.hp > 0) MonsterHit(t, f, run, stats, 1f, guarding, ev);
                return true;
            }
            MonsterHit(t, f, run, stats, 1f, guarding, ev);
            return true;
        }

        static void MonsterHit(TuningData t, FightState f, RunState run, PlayerStats stats, float mult, bool guarding, List<GameEvent> ev)
        {
            float raw = ApplyArmour(f.monsterAtk, stats.armourPct) * mult;
            if (guarding) raw *= t.guardDamageTaken;
            int dmg = RoundMin1(raw);
            run.hp = Math.Max(0, run.hp - dmg);
            ev.Add(new GameEvent(GameEventType.MonsterHit, dmg, true, mult > 1f ? "Big hit!" : null));
        }

        static void DealHit(TuningData t, FightState f, List<GameEvent> ev, float dmg, float critChance, Rng rng)
        {
            bool isCrit = rng.Chance(Math.Min(1f, Math.Max(0f, critChance)));
            if (isCrit) dmg *= t.critMult;
            int d = RoundMin1(dmg);
            f.monsterHp = Math.Max(0, f.monsterHp - d);
            ev.Add(new GameEvent(isCrit ? GameEventType.PlayerCrit : GameEventType.PlayerHit, d, false, isCrit ? "Crit!" : null));
        }

        static void HealPlayer(RunState run, PlayerStats stats, int amount, List<GameEvent> ev)
        {
            int before = run.hp;
            run.hp = Math.Min(stats.maxHp, run.hp + Math.Max(0, amount));
            if (run.hp < before) run.hp = before; // never reduce hp via a heal
            ev.Add(new GameEvent(GameEventType.PlayerHeal, run.hp - before, true, null));
        }

        static void Win(RunState run, FightState f, List<GameEvent> ev)
        {
            if (f.over) return;
            f.monsterHp = 0;
            f.over = true; f.playerWon = true;
            ev.Add(new GameEvent(GameEventType.MonsterDied, 0, false, f.monsterId));
        }

        static int RoundMin1(float v)
        {
            int r = (int)Math.Round(v, MidpointRounding.AwayFromZero);
            return r < 1 ? 1 : r;
        }

        // ───────────────────────── Rewards ─────────────────────────
        public static int KillCoins(GameContent c, RunState run, PlayerStats stats, FightState f)
        {
            TuningData t = c.tuning;
            RouteData route = run != null ? c.Route(run.routeId) : null;
            float v = t.killCoinBase + f.difficulty * t.killCoinPerDiff;
            v *= route != null ? route.coinMult : 1f;
            v *= stats != null ? stats.coinMult : 1f;
            if (f.elite) v *= t.eliteRewardMult;
            RouteMod mod = run != null ? run.mod : RouteMod.None;
            if (mod == RouteMod.Rich) v *= 1.5f;
            if (mod == RouteMod.Dangerous) v *= 1.5f;
            return Math.Max(0, (int)Math.Round(v, MidpointRounding.AwayFromZero));
        }

        public static int KillXp(GameContent c, RunState run, FightState f)
        {
            TuningData t = c.tuning;
            float v = t.killXpBase + f.difficulty * t.killXpPerDiff;
            if (f.elite) v *= t.eliteRewardMult;
            RouteMod mod = run != null ? run.mod : RouteMod.None;
            if (mod == RouteMod.Double) v *= 2f;
            if (mod == RouteMod.Dangerous) v *= 1.5f;
            return Math.Max(0, (int)Math.Round(v, MidpointRounding.AwayFromZero));
        }
    }
}
