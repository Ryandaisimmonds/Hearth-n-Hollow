// Hearth & Hollow — the camp's Skills tab: the current class's skill tree and its move loadout (compact).
// Top: skill points, the loadout — J attack, K skill, U skill (may borrow), Space dash — and the free respec.
// Below: the tree (three branches; click a node to spend a point) beside the moves for the picked slot and
// the details for whatever you hover. Switch class with "Change class" on the right of the camp.
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class SkillsTab
    {
        public readonly VisualElement Root;
        readonly GameUI Ui;
        GameSession S { get { return Ui.Session; } }
        GameContent C { get { return Ui.Session.Content; } }

        readonly Label _points;
        readonly VisualElement _loadout, _choices, _tree, _detail, _levelInfo;
        readonly Label _choicesTitle;
        readonly ConfirmButton _respec;
        ClassId _cls;
        LoadSlot _slot = LoadSlot.Attack;
        string _focusNode, _focusMove;

        static readonly LoadSlot[] Slots = { LoadSlot.Attack, LoadSlot.Skill1, LoadSlot.Skill2, LoadSlot.Dash };

        public SkillsTab(GameUI ui)
        {
            Ui = ui;
            Root = UIKit.E("skills-tab");

            // ───── top: points · loadout · respec ─────
            var top = UIKit.E("skills-tab__top");
            var pts = UIKit.E("skills-tab__points");
            pts.Add(Art.Chip("xp", "✨", out _points, "chip--xp"));
            _respec = new ConfirmButton("↺ Reset points", "Click again to refund every point", () => S.RespecTree(_cls), "btn--small", "btn--secondary", "skills__respec");
            pts.Add(_respec.Button);
            top.Add(pts);
            _loadout = UIKit.E("loadout", "loadout--row");
            top.Add(_loadout);
            Root.Add(top);

            // ───── tree | moves + details ─────
            var body = UIKit.E("skills-tab__body");
            var treeBox = UIKit.E("skills-tab__tree");
            treeBox.Add(UIKit.L("Skill tree — one point per level; each skill needs the one above it", "small", "muted", "skills-tab__hint"));
            _tree = UIKit.E("tree", "tree--compact");
            treeBox.Add(_tree);
            body.Add(treeBox);

            var side = UIKit.E("skills-tab__side");
            _choicesTitle = UIKit.L("", "subhead", "loadout__choices-title");
            side.Add(_choicesTitle);
            _choices = UIKit.E("loadout__choices");
            side.Add(_choices);
            _detail = UIKit.E("skill-detail", "skill-detail--compact");
            side.Add(_detail);
            _levelInfo = UIKit.E("skill-levels");
            side.Add(_levelInfo);
            body.Add(side);
            Root.Add(body);
        }

        /// Called when the tab is opened: back to the selected class, nothing focused.
        public void OnShow()
        {
            _cls = S.Profile.selectedClass;
            _focusNode = null; _focusMove = null;
            _respec.Reset();
        }

        // ───────────────────────── refresh ─────────────────────────
        public void Refresh()
        {
            if (_cls != S.Profile.selectedClass) { _cls = S.Profile.selectedClass; _focusNode = null; _focusMove = null; _respec.Reset(); }
            var cp = S.Profile.Progress(_cls);
            int free = S.SkillPointsFree(_cls);
            _points.text = free + " skill point" + (free == 1 ? "" : "s");
            BuildLoadout(cp);
            BuildChoices(cp);
            BuildTree(cp);
            BuildDetail(cp);
            BuildLevelInfo(cp);
            int spent = Progression.PointsSpent(C, cp);
            _respec.Button.SetEnabled(spent > 0);
        }

        // ───── loadout ─────
        /// Percent with one decimal when it isn't whole (0.005 → "0.5%").
        static string Pct1(float f) { float v = f * 100f; return (Mathf.Abs(v - Mathf.Round(v)) < 0.05f ? Mathf.Round(v).ToString("0") : v.ToString("0.0")) + "%"; }

        static string SlotKey(LoadSlot s) { return s == LoadSlot.Attack ? "J" : s == LoadSlot.Skill1 ? "K" : s == LoadSlot.Skill2 ? "L" : "Space"; }
        static string SlotName(LoadSlot s) { return s == LoadSlot.Attack ? "Attack" : s == LoadSlot.Skill1 ? "Skill" : s == LoadSlot.Skill2 ? "Skill (can borrow)" : "Dash"; }

        void BuildLoadout(ClassProgress cp)
        {
            _loadout.Clear();
            var load = S.Loadout(_cls);
            for (int i = 0; i < Slots.Length; i++)
            {
                LoadSlot slot = Slots[i];
                var m = load[i];
                bool open = Progression.SlotOpen(C, cp, slot);
                var card = UIKit.E("load-slot");
                card.EnableInClassList("load-slot--on", slot == _slot);
                card.EnableInClassList("load-slot--closed", !open);
                var key = UIKit.L(SlotKey(slot), "load-slot__key");
                card.Add(key);
                var icon = UIKit.E("load-slot__icon");
                icon.Add(UIKit.L(m != null ? m.emoji : open ? "＋" : "🔒", "load-slot__emoji"));
                card.Add(icon);
                var txt = UIKit.E("load-slot__text");
                txt.Add(UIKit.L(SlotName(slot), "load-slot__slot"));
                string name = m != null ? m.name + (m.owner != _cls && !m.anyClass ? "  (" + m.owner + ")" : "")
                            : open ? "Empty — pick a skill below" : "Opens at level " + C.tuning.secondSkillLevel;
                txt.Add(UIKit.L(name, "load-slot__name"));
                card.Add(txt);
                card.RegisterCallback<ClickEvent>(e => { _slot = slot; Refresh(); });
                if (m != null) card.RegisterCallback<PointerEnterEvent>(e => FocusMove(m.id));
                foreach (var c in card.Children()) c.pickingMode = PickingMode.Ignore;
                _loadout.Add(card);
            }
        }

        void BuildChoices(ClassProgress cp)
        {
            _choices.Clear();
            _choicesTitle.text = SlotName(_slot) + " moves";
            if (!Progression.SlotOpen(C, cp, _slot))
            {
                _choices.Add(UIKit.L("The second skill slot opens at level " + C.tuning.secondSkillLevel + ".", "muted", "small"));
                return;
            }
            string equipped = Progression.Equipped(cp, _slot);
            if (_slot == LoadSlot.Skill2 && !string.IsNullOrEmpty(equipped))
                _choices.Add(UIKit.B("Empty the L slot", () => S.EquipMove(_cls, LoadSlot.Skill2, null), "btn--small", "btn--ghost", "loadout__clear"));
            foreach (var m in S.SlotChoices(_cls, _slot))
            {
                string block = S.EquipBlock(_cls, _slot, m.id);
                bool on = m.id == equipped;
                var row = UIKit.E("move-row");
                row.EnableInClassList("move-row--on", on);
                row.EnableInClassList("move-row--locked", block != null && !on);
                row.Add(UIKit.L(m.emoji, "move-row__emoji"));
                var txt = UIKit.E("move-row__text");
                txt.Add(UIKit.L(m.name + (m.owner != _cls && !m.anyClass ? "  · borrowed from " + m.owner : ""), "move-row__name"));
                txt.Add(UIKit.L(on ? "Equipped" : block == null ? "Click to equip" : HowToGet(m, block), "move-row__sub"));
                row.Add(txt);
                string id = m.id;
                row.RegisterCallback<ClickEvent>(e => { if (!on && S.EquipBlock(_cls, _slot, id) == null) S.EquipMove(_cls, _slot, id); });
                row.RegisterCallback<PointerEnterEvent>(e => FocusMove(id));
                foreach (var c in row.Children()) c.pickingMode = PickingMode.Ignore;
                _choices.Add(row);
            }
            if (_slot == LoadSlot.Skill2)
                _choices.Add(UIKit.L("Any class at level " + C.tuning.borrowLevel + "+ lends the skills it has learned to this slot.", "muted", "small", "loadout__hint"));
        }

        string HowToGet(MoveData m, string block)
        {
            if (m.owner == _cls && block == "Not unlocked yet")
                foreach (var n in C.skillNodes)
                    if (n.classId == _cls && n.effect == NodeEffect.UnlockMove && n.moveId == m.id)
                        return "Learn it in the tree (level " + n.minLevel + ")";
            return block;
        }

        // ───── tree ─────
        void BuildTree(ClassProgress cp)
        {
            _tree.Clear();
            var cd = C.Class(_cls);
            var nodes = Progression.Tree(C, _cls);
            for (int b = 0; b < 3; b++)
            {
                var col = UIKit.E("tree__branch");
                string bname = cd != null && cd.branchNames != null && b < cd.branchNames.Count ? cd.branchNames[b] : "Branch " + (b + 1);
                col.Add(UIKit.L(bname, "tree__branch-name"));
                bool first = true;
                foreach (var n in nodes)
                {
                    if (n.branch != b) continue;
                    if (!first) col.Add(Link(n, cp));
                    first = false;
                    col.Add(NodeCard(n, cp));
                }
                _tree.Add(col);
            }
        }

        VisualElement Link(SkillNodeData n, ClassProgress cp)
        {
            var l = UIKit.E("tree__link");
            l.EnableInClassList("tree__link--lit", !string.IsNullOrEmpty(n.requires) && Progression.Rank(cp, n.requires) > 0);
            return l;
        }

        VisualElement NodeCard(SkillNodeData n, ClassProgress cp)
        {
            int rank = Progression.Rank(cp, n.id);
            string block = S.NodeBlock(_cls, n.id);
            bool maxed = rank >= n.maxRank;
            bool canBuy = block == null;
            bool levelLocked = cp.level < n.minLevel;
            var card = UIKit.E("node");
            card.EnableInClassList("node--move", n.effect == NodeEffect.UnlockMove);
            card.EnableInClassList("node--owned", rank > 0);
            card.EnableInClassList("node--maxed", maxed);
            card.EnableInClassList("node--buy", canBuy);
            card.EnableInClassList("node--locked", !canBuy && rank == 0);
            card.EnableInClassList("node--focus", n.id == _focusNode);
            card.Add(UIKit.L(levelLocked ? "🔒" : n.emoji, "node__emoji"));
            var txt = UIKit.E("node__text");
            txt.Add(UIKit.L(n.name, "node__name"));
            string sub = levelLocked ? "Level " + n.minLevel : n.maxRank > 1 ? rank + " / " + n.maxRank : rank > 0 ? "Learned" : "Lv " + n.minLevel;
            txt.Add(UIKit.L(sub, "node__sub"));
            card.Add(txt);
            if (canBuy) card.Add(UIKit.L("+", "node__plus"));
            string id = n.id;
            card.RegisterCallback<ClickEvent>(e => { _focusNode = id; _focusMove = null; if (S.NodeBlock(_cls, id) == null) S.BuyNode(_cls, id); else Refresh(); });
            card.RegisterCallback<PointerEnterEvent>(e => { if (_focusNode != id) { _focusNode = id; _focusMove = null; BuildDetail(S.Profile.Progress(_cls)); } });
            foreach (var c in card.Children()) c.pickingMode = PickingMode.Ignore;
            return card;
        }

        // ───── details ─────
        void FocusMove(string id)
        {
            if (_focusMove == id) return;
            _focusMove = id; _focusNode = null;
            BuildDetail(S.Profile.Progress(_cls));
        }

        void BuildDetail(ClassProgress cp)
        {
            _detail.Clear();
            var n = C.Node(_focusNode);
            var m = n != null && n.effect == NodeEffect.UnlockMove ? C.Move(n.moveId) : C.Move(_focusMove);
            if (n == null && m == null)
            {
                _detail.Add(UIKit.L("Hover a skill or a move to see what it does.", "muted"));
                return;
            }
            if (n != null)
            {
                int rank = Progression.Rank(cp, n.id);
                var top = UIKit.E("skill-detail__top");
                top.Add(UIKit.L(n.emoji, "skill-detail__emoji"));
                var names = UIKit.E("skill-detail__names");
                names.Add(UIKit.L(n.name, "skill-detail__name"));
                names.Add(UIKit.L(BranchName(n) + " · level " + n.minLevel + (n.maxRank > 1 ? " · rank " + rank + "/" + n.maxRank : ""), "muted", "small"));
                top.Add(names);
                _detail.Add(top);
                if (m == null) _detail.Add(UIKit.L(n.description, "skill-detail__desc"));
                if (n.maxRank > 1 && n.effect != NodeEffect.UnlockMove)
                    _detail.Add(UIKit.L("Now: " + EffectText(n, rank) + "   →   Next: " + EffectText(n, Mathf.Min(n.maxRank, rank + 1)), "small", "skill-detail__ranks"));
            }
            if (m != null) MoveDetail(m);
            if (n != null)
            {
                string block = S.NodeBlock(_cls, n.id);
                if (block == null)
                    _detail.Add(UIKit.B("Learn  (1 point)", () => S.BuyNode(_cls, n.id), "btn--primary", "skill-detail__buy"));
                else
                    _detail.Add(UIKit.L(block == "Maxed" ? "✔ Fully learned" : "🔒 " + block, "skill-detail__block"));
            }
        }

        void MoveDetail(MoveData m)
        {
            var top = UIKit.E("skill-detail__top");
            top.Add(UIKit.L(m.emoji, "skill-detail__emoji"));
            var names = UIKit.E("skill-detail__names");
            names.Add(UIKit.L(m.name, "skill-detail__name"));
            string slot = m.slot == MoveSlot.Attack ? "Attack (J)" : m.slot == MoveSlot.Dash ? "Dash (Space)" : "Skill (K or U)";
            names.Add(UIKit.L(slot + (m.anyClass ? " · every class" : " · " + m.owner), "muted", "small"));
            top.Add(names);
            _detail.Add(top);
            _detail.Add(UIKit.L(m.description, "skill-detail__desc"));
            var mods = Progression.Mods(C, S.Profile, _cls);
            var facts = UIKit.E("skill-detail__facts");
            if (m.power > 0f)
            {
                float pw = m.power * (m.slot == MoveSlot.Skill ? mods.skillPower : mods.attackPower);
                facts.Add(Fact("Power", "×" + pw.ToString("0.##") + " damage"));
            }
            float cd = m.slot == MoveSlot.Skill ? m.cooldown * mods.skillCooldown : m.slot == MoveSlot.Dash ? m.cooldown * mods.dashCooldown : m.cooldown;
            facts.Add(Fact(m.slot == MoveSlot.Attack ? "Speed" : "Cooldown", m.slot == MoveSlot.Attack ? (1f / Mathf.Max(0.1f, cd)).ToString("0.#") + " per second" : cd.ToString("0.#") + "s"));
            _detail.Add(facts);
        }

        static VisualElement Fact(string label, string value)
        {
            var r = UIKit.E("skill-detail__fact");
            r.Add(UIKit.L(label, "muted", "small"));
            r.Add(UIKit.L(value, "small", "skill-detail__fact-value"));
            return r;
        }

        string BranchName(SkillNodeData n)
        {
            var cd = C.Class(n.classId);
            return cd != null && cd.branchNames != null && n.branch < cd.branchNames.Count ? cd.branchNames[n.branch] : "";
        }

        static string EffectText(SkillNodeData n, int rank)
        {
            float v = n.valuePerRank * rank;
            switch (n.effect)
            {
                case NodeEffect.Stat:
                    switch (n.stat)
                    {
                        case BuffStat.MaxHp: return "+" + Mathf.RoundToInt(v) + " max HP";
                        case BuffStat.Armour: return "+" + Mathf.RoundToInt(v) + " armour";
                        case BuffStat.DamagePct: return "+" + UIKit.Pct(v) + " damage";
                        case BuffStat.CritPct: return "+" + UIKit.Pct(v) + " crit";
                        case BuffStat.GatherZonePct: return "+" + UIKit.Pct(v) + " gather zone";
                        case BuffStat.CoinPct: return "+" + UIKit.Pct(v) + " coins";
                        case BuffStat.XpPct: return "+" + UIKit.Pct(v) + " XP";
                case BuffStat.MoveSpeedPct: return "+" + UIKit.Pct(v) + " speed";
                case BuffStat.HealOnKill: return "+" + Mathf.RoundToInt(v) + " HP per kill";
                    }
                    return "+" + v;
                case NodeEffect.AttackPower: return "attacks +" + UIKit.Pct(v);
                case NodeEffect.SkillPower: return "skills +" + UIKit.Pct(v);
                case NodeEffect.SkillCooldown: return "skill cooldowns −" + UIKit.Pct(v);
                case NodeEffect.DashCooldown: return "dash cooldown −" + UIKit.Pct(v);
                case NodeEffect.MoveSpeed: return "+" + UIKit.Pct(v) + " move speed";
                case NodeEffect.HealOnKill: return "+" + Mathf.RoundToInt(v) + " HP per kill";
            }
            return "";
        }

        void BuildLevelInfo(ClassProgress cp)
        {
            _levelInfo.Clear();
            var t = C.tuning;
            _levelInfo.Add(UIKit.L("Every level", "subhead"));
            int lv = cp.level;
            string lateHp = t.hpPerLevel >= 1f ? "+" + UIKit.Num(t.hpPerLevel) + " after level " + t.earlyLevels
                : t.hpPerLevel > 0f ? "then +1 every " + Mathf.RoundToInt(1f / t.hpPerLevel) + " levels" : "none after level " + t.earlyLevels;
            _levelInfo.Add(UIKit.L("+" + t.skillPointsPerLevel + " skill point, +" + Pct1(t.damagePerLevel) + " damage, +" + t.hpPerLevelEarly + " max HP (" + lateHp + ")", "small"));
            _levelInfo.Add(UIKit.L("Levels are small on purpose — your meals are where the real strength comes from.", "small", "muted"));
            _levelInfo.Add(UIKit.L("From your " + (lv - 1) + " levels: +" + StatCalc.LevelHp(t, lv) + " HP, +" + Pct1((lv - 1) * t.damagePerLevel) + " damage", "small", "muted"));
            var miles = UIKit.E("skill-levels__miles");
            Mile(miles, t.secondSkillLevel, "Second skill slot (L)", lv);
            Mile(miles, t.borrowLevel, "Lends its skills to the other classes", lv);
            Mile(miles, t.levelCap, "Max level", lv);
            _levelInfo.Add(miles);
            if (lv < t.levelCap)
            {
                int next = Economy.XpToNext(t, lv);
                var bar = new Bar("bar--xp");
                bar.Set(cp.xp, next, cp.xp + " / " + next + " XP to level " + (lv + 1));
                _levelInfo.Add(bar);
            }
        }

        static void Mile(VisualElement holder, int level, string text, int current)
        {
            var r = UIKit.E("skill-levels__mile");
            r.EnableInClassList("skill-levels__mile--done", current >= level);
            r.Add(UIKit.L((current >= level ? "✔ " : "") + "Lv " + level, "skill-levels__lv"));
            r.Add(UIKit.L(text, "small"));
            holder.Add(r);
        }
    }
}
