// Hearth & Hollow — move loadouts. Pure C#, no UnityEngine.
// Design v2 (ticket 04): no levels, XP, skill points or skill trees. A loadout holds Attack (J), Skill 1 (K),
// Skill 2 (L) and Dash. For now a class can equip any move it owns; ticket 10 gates moves behind each
// class's story arc. (Content still carries the old skillNodes data; nothing reads it.)
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public static class Progression
    {
        // ───────────── lookups ─────────────
        public static MoveData Move(this GameContent c, string id)
        {
            if (string.IsNullOrEmpty(id) || c.moves == null) return null;
            return c.moves.Find(x => x.id == id);
        }

        // ───────────── moves & loadout ─────────────
        public static bool IsStarter(GameContent c, MoveData m, ClassId cls)
        {
            if (m == null) return false;
            if (m.anyClass) return true;
            var cd = c.Class(cls);
            return cd != null && m.owner == cls && (m.id == cd.starterAttack || m.id == cd.starterSkill || m.id == cd.starterDash);
        }

        /// Does this class own the move? (Until ticket 10 every move a class has is available.)
        public static bool Owns(GameContent c, Profile p, ClassId cls, MoveData m)
        {
            if (m == null) return false;
            return IsStarter(c, m, cls) || m.owner == cls;
        }

        static MoveSlot KindOf(LoadSlot s) { return s == LoadSlot.Attack ? MoveSlot.Attack : s == LoadSlot.Dash ? MoveSlot.Dash : MoveSlot.Skill; }

        /// Why a move can't go in a slot (null = it can).
        public static string EquipBlock(GameContent c, Profile p, ClassId cls, LoadSlot slot, MoveData m)
        {
            var cp = p.Progress(cls);
            if (m == null) return "Unknown move";
            if (m.slot != KindOf(slot)) return "Goes in the " + m.slot.ToString().ToLower() + " slot";
            if (!Owns(c, p, cls, m)) return m.owner != cls && !m.anyClass ? "Another class's move" : "Not unlocked yet";
            // the K skill into L only works as a swap with a skill K can hold (not empty)
            if (slot == LoadSlot.Skill2 && cp.loadSkill1 == m.id && !Owns(c, p, cls, c.Move(cp.loadSkill2))) return "Already in your K slot";
            return null;   // otherwise the same skill in the other skill slot is fine: Equip swaps them
        }

        /// Options for a slot: the class's own moves of that kind (and anyClass moves).
        public static List<MoveData> SlotChoices(GameContent c, Profile p, ClassId cls, LoadSlot slot)
        {
            var list = new List<MoveData>();
            var kind = KindOf(slot);
            if (c.moves == null) return list;
            foreach (var m in c.moves) if (m.slot == kind && (m.anyClass || m.owner == cls)) list.Add(m);
            return list;
        }

        public static string Equipped(ClassProgress cp, LoadSlot slot)
        {
            switch (slot)
            {
                case LoadSlot.Attack: return cp.loadAttack;
                case LoadSlot.Skill1: return cp.loadSkill1;
                case LoadSlot.Skill2: return cp.loadSkill2;
                default: return cp.loadDash;
            }
        }
        static void Set(ClassProgress cp, LoadSlot slot, string id)
        {
            switch (slot)
            {
                case LoadSlot.Attack: cp.loadAttack = id; break;
                case LoadSlot.Skill1: cp.loadSkill1 = id; break;
                case LoadSlot.Skill2: cp.loadSkill2 = id; break;
                default: cp.loadDash = id; break;
            }
        }

        public static bool Equip(GameContent c, Profile p, ClassId cls, LoadSlot slot, string moveId)
        {
            var cp = p.Progress(cls);
            if (slot == LoadSlot.Skill2 && string.IsNullOrEmpty(moveId)) { cp.loadSkill2 = null; return true; }   // clear the U slot
            var m = c.Move(moveId);
            if (EquipBlock(c, p, cls, slot, m) != null) return false;
            // the same skill in both slots → swap them instead
            if (slot == LoadSlot.Skill1 && cp.loadSkill2 == moveId) cp.loadSkill2 = cp.loadSkill1;
            else if (slot == LoadSlot.Skill2 && cp.loadSkill1 == moveId) cp.loadSkill1 = cp.loadSkill2;
            Set(cp, slot, moveId);
            EnsureLoadout(c, p, cls);
            return true;
        }

        /// Fills empty slots with the starter kit and drops anything no longer allowed. Returns true if changed.
        public static bool EnsureLoadout(GameContent c, Profile p, ClassId cls)
        {
            var cp = p.Progress(cls);
            var cd = c.Class(cls);
            if (cd == null) return false;
            bool changed = false;
            changed |= Fix(c, p, cls, cp, LoadSlot.Attack, cd.starterAttack);
            changed |= Fix(c, p, cls, cp, LoadSlot.Skill1, cd.starterSkill);
            changed |= Fix(c, p, cls, cp, LoadSlot.Dash, cd.starterDash);
            // Skill 2: may be empty; must be valid and not the same as Skill 1
            string s2 = cp.loadSkill2;
            if (!string.IsNullOrEmpty(s2))
            {
                var m = c.Move(s2);
                if (m == null || m.slot != MoveSlot.Skill || s2 == cp.loadSkill1 || !Owns(c, p, cls, m))
                { cp.loadSkill2 = null; changed = true; }
            }
            return changed;
        }

        static bool Fix(GameContent c, Profile p, ClassId cls, ClassProgress cp, LoadSlot slot, string starter)
        {
            string cur = Equipped(cp, slot);
            var m = c.Move(cur);
            if (m != null && m.slot == KindOf(slot) && Owns(c, p, cls, m)) return false;
            if (cur == starter) return false;
            Set(cp, slot, starter);
            return true;
        }

        /// The four equipped moves (Skill2 null when empty).
        public static MoveData[] Loadout(GameContent c, Profile p, ClassId cls)
        {
            EnsureLoadout(c, p, cls);
            var cp = p.Progress(cls);
            return new[]
            {
                c.Move(cp.loadAttack), c.Move(cp.loadSkill1),
                c.Move(cp.loadSkill2),
                c.Move(cp.loadDash)
            };
        }

        // ───────────── bonuses ─────────────
        /// Combat bonuses from gear and trinkets (skill trees were cut in Design v2).
        public static CombatMods Mods(GameContent c, Profile p, ClassId cls)
        {
            var m = new CombatMods();
            if (p == null) return m;
            // Workshop boots and worn trinkets
            float atk = Gear.Perk(c, p, TrinketPerk.AttackPower), skl = Gear.Perk(c, p, TrinketPerk.SkillPower);
            float scd = Gear.Perk(c, p, TrinketPerk.SkillCooldown), dcd = Gear.Perk(c, p, TrinketPerk.DashCooldown);
            float spd = Gear.BootsSpeed(c, p, cls) + Gear.Perk(c, p, TrinketPerk.MoveSpeed);
            int heal = (int)Math.Round(Gear.Perk(c, p, TrinketPerk.HealOnKill));
            m.attackPower = 1f + atk; m.skillPower = 1f + skl;
            m.skillCooldown = Math.Max(0.4f, 1f - scd); m.dashCooldown = Math.Max(0.4f, 1f - dcd);
            m.moveSpeed = 1f + spd; m.healOnKill = heal;
            return m;
        }

    }
}
