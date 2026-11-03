// Hearth & Hollow — move loadouts. Pure C#, no UnityEngine.
// Design v2 (ticket 04): no levels, XP, skill points or skill trees. A loadout holds Attack (J), Skill 1 (K),
// Skill 2 (L), Dash and an Ultimate (C). Ticket 10: each class has a small move pool (GameContent.moveUnlocks);
// a move is known from the start or unlocked by an arc step (reach a floor, beat a boss), then learned in town.
// A class with no pool entries (the hidden classes) still owns every move it has.
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

        // ───────────── the move pool (ticket 10) ─────────────
        public static MoveUnlock Unlock(GameContent c, string moveId)
        {
            return c.moveUnlocks != null ? c.moveUnlocks.Find(u => u != null && u.moveId == moveId) : null;
        }

        /// Does this class use a move pool? (Classes without one own all their moves.)
        public static bool HasPool(GameContent c, ClassId cls)
        {
            if (c.moveUnlocks == null) return false;
            foreach (var u in c.moveUnlocks) { var m = u != null ? c.Move(u.moveId) : null; if (m != null && m.owner == cls && !m.anyClass) return true; }
            return false;
        }

        /// Is the move part of this class's kit at all (shown in the Skills tab, locked or not)?
        public static bool InPool(GameContent c, ClassId cls, MoveData m)
        {
            if (m == null) return false;
            if (m.anyClass) return true;
            if (m.owner != cls) return false;
            return !HasPool(c, cls) || Unlock(c, m.id) != null;
        }

        /// Does this class own the move (can equip it)?
        public static bool Owns(GameContent c, Profile p, ClassId cls, MoveData m)
        {
            if (m == null) return false;
            if (m.anyClass) return true;
            if (m.owner != cls) return false;
            if (!HasPool(c, cls)) return true;
            var u = Unlock(c, m.id);
            if (u == null) return false;
            return u.kind == UnlockKind.Start || (p != null && p.learnedMoves != null && p.learnedMoves.Contains(m.id));
        }

        public static int DeepestFloor(Profile p, string routeId)
        {
            if (p == null || p.deepestFloors == null) return 0;
            var r = p.deepestFloors.Find(x => x != null && x.routeId == routeId);
            return r != null ? r.floor : 0;
        }

        public static bool BeatBoss(Profile p, string routeId, int floor)
        {
            return p != null && p.bossesBeaten != null && p.bossesBeaten.Contains(routeId + ":" + floor);
        }

        /// Records reaching a floor. True if it's a new deepest.
        public static bool RecordFloor(Profile p, string routeId, int floor)
        {
            if (p == null || string.IsNullOrEmpty(routeId)) return false;
            if (p.deepestFloors == null) p.deepestFloors = new List<RouteFloor>();
            var r = p.deepestFloors.Find(x => x != null && x.routeId == routeId);
            if (r == null) { p.deepestFloors.Add(new RouteFloor { routeId = routeId, floor = floor }); return true; }
            if (floor <= r.floor) return false;
            r.floor = floor; return true;
        }

        /// Records a boss kill. True the first time.
        public static bool RecordBoss(Profile p, string routeId, int floor)
        {
            if (p == null || string.IsNullOrEmpty(routeId)) return false;
            if (p.bossesBeaten == null) p.bossesBeaten = new List<string>();
            string key = routeId + ":" + floor;
            if (p.bossesBeaten.Contains(key)) return false;
            p.bossesBeaten.Add(key); return true;
        }

        public static bool RequirementMet(Profile p, MoveUnlock u)
        {
            if (u == null) return false;
            switch (u.kind)
            {
                case UnlockKind.Start: return true;
                case UnlockKind.ReachFloor: return DeepestFloor(p, u.routeId) >= u.floor;
                case UnlockKind.BeatBoss: return BeatBoss(p, u.routeId, u.floor);
            }
            return false;
        }

        /// Unlocked by the arc but not learned yet.
        public static bool ReadyToLearn(GameContent c, Profile p, ClassId cls, MoveData m)
        {
            if (m == null || Owns(c, p, cls, m) || !InPool(c, cls, m)) return false;
            return RequirementMet(p, Unlock(c, m.id));
        }

        public static List<MoveData> ReadyMoves(GameContent c, Profile p, ClassId cls)
        {
            var list = new List<MoveData>();
            if (c.moves != null) foreach (var m in c.moves) if (ReadyToLearn(c, p, cls, m)) list.Add(m);
            return list;
        }

        /// Learn an unlocked move (in town). The first Ultimate learned goes straight into the Ultimate slot.
        public static bool Learn(GameContent c, Profile p, ClassId cls, string moveId)
        {
            var m = c.Move(moveId);
            if (!ReadyToLearn(c, p, cls, m)) return false;
            if (p.learnedMoves == null) p.learnedMoves = new List<string>();
            p.learnedMoves.Add(moveId);
            EnsureLoadout(c, p, cls);
            return true;
        }

        public static string RouteName(GameContent c, string routeId)
        {
            var r = c.routes != null ? c.routes.Find(x => x.id == routeId) : null;
            return r != null ? r.name : routeId;
        }

        /// "Reach Meadow Trail floor 3" / "Beat the Meadow Trail floor-5 boss".
        public static string RequirementText(GameContent c, MoveUnlock u)
        {
            if (u == null) return "Not part of this class's kit";
            switch (u.kind)
            {
                case UnlockKind.ReachFloor: return "Reach " + RouteName(c, u.routeId) + " floor " + u.floor;
                case UnlockKind.BeatBoss: return "Beat the " + RouteName(c, u.routeId) + " floor-" + u.floor + " boss";
            }
            return "Known from the start";
        }

        public static string LearnAtText(MoveUnlock u)
        {
            return u != null && u.learnAt == "yard" ? "the training yard" : "the kitchen";
        }

        static MoveSlot KindOf(LoadSlot s)
        {
            return s == LoadSlot.Attack ? MoveSlot.Attack : s == LoadSlot.Dash ? MoveSlot.Dash : s == LoadSlot.Ultimate ? MoveSlot.Ultimate : MoveSlot.Skill;
        }

        /// Why a move can't go in a slot (null = it can).
        public static string EquipBlock(GameContent c, Profile p, ClassId cls, LoadSlot slot, MoveData m)
        {
            var cp = p.Progress(cls);
            if (m == null) return "Unknown move";
            if (m.slot != KindOf(slot)) return "Goes in the " + m.slot.ToString().ToLower() + " slot";
            if (!Owns(c, p, cls, m))
            {
                if (m.owner != cls && !m.anyClass) return "Another class's move";
                var u = Unlock(c, m.id);
                return RequirementMet(p, u) ? "Learn it at " + LearnAtText(u) : RequirementText(c, u);
            }
            // the K skill into L only works as a swap with a skill K can hold (not empty)
            if (slot == LoadSlot.Skill2 && cp.loadSkill1 == m.id && !Owns(c, p, cls, c.Move(cp.loadSkill2))) return "Already in your K slot";
            return null;   // otherwise the same skill in the other skill slot is fine: Equip swaps them
        }

        /// Options for a slot: the class's pool moves of that kind (and anyClass moves), locked ones included.
        public static List<MoveData> SlotChoices(GameContent c, Profile p, ClassId cls, LoadSlot slot)
        {
            var list = new List<MoveData>();
            var kind = KindOf(slot);
            if (c.moves == null) return list;
            foreach (var m in c.moves) if (m.slot == kind && InPool(c, cls, m)) list.Add(m);
            return list;
        }

        public static string Equipped(ClassProgress cp, LoadSlot slot)
        {
            switch (slot)
            {
                case LoadSlot.Attack: return cp.loadAttack;
                case LoadSlot.Skill1: return cp.loadSkill1;
                case LoadSlot.Skill2: return cp.loadSkill2;
                case LoadSlot.Ultimate: return cp.loadUltimate;
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
                case LoadSlot.Ultimate: cp.loadUltimate = id; break;
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
            // Ultimate: must be owned; else the class's signature one, else any owned, else none
            var um = c.Move(cp.loadUltimate);
            if (um == null || um.slot != MoveSlot.Ultimate || !Owns(c, p, cls, um))
            {
                string pick = null;
                var sig = c.Move(cd.ultimate);
                if (sig != null && Owns(c, p, cls, sig)) pick = sig.id;
                else if (c.moves != null) { var any = c.moves.Find(x => x.slot == MoveSlot.Ultimate && Owns(c, p, cls, x)); if (any != null) pick = any.id; }
                if (pick != cp.loadUltimate) { cp.loadUltimate = pick; changed = true; }
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

        /// The equipped Ultimate (null until one is learned).
        public static MoveData Ultimate(GameContent c, Profile p, ClassId cls)
        {
            EnsureLoadout(c, p, cls);
            return c.Move(p.Progress(cls).loadUltimate);
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
