// Hearth & Hollow — levels 1–50, skill trees and move loadouts. Pure C#, no UnityEngine.
// One skill point per level (from level 2). Nodes unlock moves, add stats or boost moves.
// A loadout holds Attack (J), Skill 1 (K), Skill 2 (U, opens at secondSkillLevel) and Dash.
// Skill 2 may hold a skill borrowed from another class once that class reaches borrowLevel.
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
        public static SkillNodeData Node(this GameContent c, string id)
        {
            if (string.IsNullOrEmpty(id) || c.skillNodes == null) return null;
            return c.skillNodes.Find(x => x.id == id);
        }
        public static List<SkillNodeData> Tree(GameContent c, ClassId cls)
        {
            var list = new List<SkillNodeData>();
            if (c.skillNodes != null) foreach (var n in c.skillNodes) if (n.classId == cls) list.Add(n);
            list.Sort((a, b) => a.branch != b.branch ? a.branch.CompareTo(b.branch) : a.row.CompareTo(b.row));
            return list;
        }

        // ───────────── points ─────────────
        public static int Rank(ClassProgress cp, string nodeId)
        {
            if (cp == null || cp.tree == null) return 0;
            var nr = cp.tree.Find(x => x.nodeId == nodeId);
            return nr != null ? nr.rank : 0;
        }
        public static int PointsEarned(GameContent c, ClassProgress cp)
        {
            return Math.Max(0, (Math.Max(1, cp.level) - 1) * Math.Max(0, c.tuning.skillPointsPerLevel));
        }
        public static int PointsSpent(GameContent c, ClassProgress cp)
        {
            int spent = 0;
            if (cp.tree != null)
                foreach (var nr in cp.tree)
                {
                    var n = c.Node(nr.nodeId);
                    spent += nr.rank * (n != null ? Math.Max(0, n.cost) : 1);
                }
            return spent;
        }
        public static int PointsFree(GameContent c, ClassProgress cp) { return PointsEarned(c, cp) - PointsSpent(c, cp); }

        /// Why a node can't take another rank right now (null = it can).
        public static string BlockReason(GameContent c, ClassProgress cp, SkillNodeData n)
        {
            if (n == null) return "Unknown skill";
            if (n.classId != cp.classId) return "Another class's skill";
            int rank = Rank(cp, n.id);
            if (rank >= n.maxRank) return "Maxed";
            if (cp.level < n.minLevel) return "Needs level " + n.minLevel;
            if (!string.IsNullOrEmpty(n.requires) && Rank(cp, n.requires) < 1)
            {
                var req = c.Node(n.requires);
                return "Needs " + (req != null ? req.name : n.requires) + " first";
            }
            if (PointsFree(c, cp) < Math.Max(1, n.cost)) return "No skill points left";
            return null;
        }

        public static bool Buy(GameContent c, Profile p, ClassId cls, string nodeId)
        {
            var cp = p.Progress(cls);
            var n = c.Node(nodeId);
            if (BlockReason(c, cp, n) != null) return false;
            if (cp.tree == null) cp.tree = new List<NodeRank>();
            var nr = cp.tree.Find(x => x.nodeId == nodeId);
            if (nr == null) { nr = new NodeRank { nodeId = nodeId }; cp.tree.Add(nr); }
            nr.rank++;
            return true;
        }

        /// Refund every point (free at camp). Moves you no longer own drop back to the starter kit.
        public static void Respec(GameContent c, Profile p, ClassId cls)
        {
            var cp = p.Progress(cls);
            if (cp.tree != null) cp.tree.Clear();
            foreach (var other in p.classes) EnsureLoadout(c, p, other.classId);   // borrowed moves may go too
        }

        /// Removes ranks that are no longer allowed (content changed, level dropped). Returns true if anything changed.
        public static bool Sanitize(GameContent c, ClassProgress cp)
        {
            if (cp.tree == null) { cp.tree = new List<NodeRank>(); return true; }
            bool changed = false;
            for (int i = cp.tree.Count - 1; i >= 0; i--)
            {
                var nr = cp.tree[i]; var n = c.Node(nr.nodeId);
                if (n == null || n.classId != cp.classId || nr.rank <= 0 || cp.level < n.minLevel) { cp.tree.RemoveAt(i); changed = true; continue; }
                if (nr.rank > n.maxRank) { nr.rank = n.maxRank; changed = true; }
            }
            // drop nodes whose prerequisite is gone (repeat until stable)
            bool again = true;
            while (again)
            {
                again = false;
                for (int i = cp.tree.Count - 1; i >= 0; i--)
                {
                    var n = c.Node(cp.tree[i].nodeId);
                    if (!string.IsNullOrEmpty(n.requires) && Rank(cp, n.requires) < 1) { cp.tree.RemoveAt(i); changed = again = true; }
                }
            }
            // over budget (e.g. points per level lowered): drop the deepest nodes first
            while (PointsFree(c, cp) < 0 && cp.tree.Count > 0)
            {
                int worst = 0;
                for (int i = 1; i < cp.tree.Count; i++)
                    if (c.Node(cp.tree[i].nodeId).row > c.Node(cp.tree[worst].nodeId).row) worst = i;
                cp.tree[worst].rank--;
                if (cp.tree[worst].rank <= 0) cp.tree.RemoveAt(worst);
                changed = true;
            }
            return changed;
        }

        // ───────────── moves & loadout ─────────────
        public static bool IsStarter(GameContent c, MoveData m, ClassId cls)
        {
            if (m == null) return false;
            if (m.anyClass) return true;
            var cd = c.Class(cls);
            return cd != null && m.owner == cls && (m.id == cd.starterAttack || m.id == cd.starterSkill || m.id == cd.starterDash);
        }

        /// Does this class own the move (starter kit or unlocked in its own tree)?
        public static bool Owns(GameContent c, Profile p, ClassId cls, MoveData m)
        {
            if (m == null) return false;
            if (IsStarter(c, m, cls)) return true;
            if (m.owner != cls) return false;
            var cp = p.Progress(cls);
            if (c.skillNodes != null)
                foreach (var n in c.skillNodes)
                    if (n.classId == cls && n.effect == NodeEffect.UnlockMove && n.moveId == m.id && Rank(cp, n.id) > 0) return true;
            return false;
        }

        /// Can another class lend this skill to <paramref name="cls"/>'s borrow slot?
        public static bool CanBorrow(GameContent c, Profile p, ClassId cls, MoveData m)
        {
            if (m == null || m.slot != MoveSlot.Skill || m.anyClass || m.owner == cls) return false;
            return p.Progress(m.owner).level >= c.tuning.borrowLevel && Owns(c, p, m.owner, m);
        }

        public static bool SlotOpen(GameContent c, ClassProgress cp, LoadSlot slot)
        {
            return slot != LoadSlot.Skill2 || cp.level >= c.tuning.secondSkillLevel;
        }

        static MoveSlot KindOf(LoadSlot s) { return s == LoadSlot.Attack ? MoveSlot.Attack : s == LoadSlot.Dash ? MoveSlot.Dash : MoveSlot.Skill; }

        /// Why a move can't go in a slot (null = it can).
        public static string EquipBlock(GameContent c, Profile p, ClassId cls, LoadSlot slot, MoveData m)
        {
            var cp = p.Progress(cls);
            if (m == null) return "Unknown move";
            if (!SlotOpen(c, cp, slot)) return "This slot opens at level " + c.tuning.secondSkillLevel;
            if (m.slot != KindOf(slot)) return "Goes in the " + m.slot.ToString().ToLower() + " slot";
            bool own = Owns(c, p, cls, m);
            if (!own)
            {
                if (slot == LoadSlot.Skill2 && CanBorrow(c, p, cls, m)) { }
                else if (m.owner != cls && !m.anyClass)
                    return slot == LoadSlot.Skill2 ? "Borrow it once the " + m.owner + " reaches level " + c.tuning.borrowLevel + " and has unlocked it"
                                                   : "Only the U slot can hold another class's skill";
                else return "Not unlocked yet";
            }
            // the K skill into U only works as a swap with a skill K can hold (not empty, not borrowed)
            if (slot == LoadSlot.Skill2 && cp.loadSkill1 == m.id && !Owns(c, p, cls, c.Move(cp.loadSkill2))) return "Already in your K slot";
            return null;   // otherwise the same skill in the other skill slot is fine: Equip swaps them
        }

        /// Options for a slot, in tree order: owned first, then borrowable (Skill2), then still locked.
        public static List<MoveData> SlotChoices(GameContent c, Profile p, ClassId cls, LoadSlot slot)
        {
            var list = new List<MoveData>();
            var kind = KindOf(slot);
            if (c.moves == null) return list;
            foreach (var m in c.moves) if (m.slot == kind && (m.anyClass || m.owner == cls)) list.Add(m);
            if (slot == LoadSlot.Skill2)
                foreach (var m in c.moves) if (m.slot == kind && m.owner != cls && !m.anyClass && CanBorrow(c, p, cls, m)) list.Add(m);
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
            // Skill 2: may be empty; must be open, valid and not the same as Skill 1
            string s2 = cp.loadSkill2;
            if (!string.IsNullOrEmpty(s2))
            {
                var m = c.Move(s2);
                if (!SlotOpen(c, cp, LoadSlot.Skill2) || m == null || m.slot != MoveSlot.Skill || s2 == cp.loadSkill1
                    || !(Owns(c, p, cls, m) || CanBorrow(c, p, cls, m)))
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

        /// The four equipped moves (Skill2 null when empty or locked).
        public static MoveData[] Loadout(GameContent c, Profile p, ClassId cls)
        {
            EnsureLoadout(c, p, cls);
            var cp = p.Progress(cls);
            return new[]
            {
                c.Move(cp.loadAttack), c.Move(cp.loadSkill1),
                SlotOpen(c, cp, LoadSlot.Skill2) ? c.Move(cp.loadSkill2) : null,
                c.Move(cp.loadDash)
            };
        }

        // ───────────── bonuses ─────────────
        /// Stat bonuses from tree nodes, indexed by BuffStat (same layout as Meals.TotalBonus).
        public static float[] StatBonus(GameContent c, Profile p, ClassId cls)
        {
            var sum = new float[Enum.GetValues(typeof(BuffStat)).Length];
            if (p == null || c.skillNodes == null) return sum;
            var cp = p.Progress(cls);
            if (cp.tree == null) return sum;
            foreach (var nr in cp.tree)
            {
                var n = c.Node(nr.nodeId);
                if (n == null || n.effect != NodeEffect.Stat || nr.rank <= 0) continue;
                sum[(int)n.stat] += n.valuePerRank * nr.rank;
            }
            return sum;
        }

        public static CombatMods Mods(GameContent c, Profile p, ClassId cls)
        {
            var m = new CombatMods();
            if (p == null) return m;
            var cp = p.Progress(cls);
            // Workshop boots and worn trinkets add to the skill tree's bonuses
            float atk = Gear.Perk(c, p, TrinketPerk.AttackPower), skl = Gear.Perk(c, p, TrinketPerk.SkillPower);
            float scd = Gear.Perk(c, p, TrinketPerk.SkillCooldown), dcd = Gear.Perk(c, p, TrinketPerk.DashCooldown);
            float spd = Gear.BootsSpeed(c, p, cls) + Gear.Perk(c, p, TrinketPerk.MoveSpeed);
            int heal = (int)Math.Round(Gear.Perk(c, p, TrinketPerk.HealOnKill));
            if (cp.tree != null && c.skillNodes != null)
            foreach (var nr in cp.tree)
            {
                var n = c.Node(nr.nodeId);
                if (n == null || nr.rank <= 0) continue;
                float v = n.valuePerRank * nr.rank;
                switch (n.effect)
                {
                    case NodeEffect.AttackPower: atk += v; break;
                    case NodeEffect.SkillPower: skl += v; break;
                    case NodeEffect.SkillCooldown: scd += v; break;
                    case NodeEffect.DashCooldown: dcd += v; break;
                    case NodeEffect.MoveSpeed: spd += v; break;
                    case NodeEffect.HealOnKill: heal += (int)Math.Round(v); break;
                }
            }
            m.attackPower = 1f + atk; m.skillPower = 1f + skl;
            m.skillCooldown = Math.Max(0.4f, 1f - scd); m.dashCooldown = Math.Max(0.4f, 1f - dcd);
            m.moveSpeed = 1f + spd; m.healOnKill = heal;
            return m;
        }

        /// Total XP needed to go from level 1 to <paramref name="level"/>.
        public static int TotalXpTo(TuningData t, int level)
        {
            int total = 0;
            for (int l = 1; l < level; l++) total += Economy.XpToNext(t, l);
            return total;
        }
    }
}
