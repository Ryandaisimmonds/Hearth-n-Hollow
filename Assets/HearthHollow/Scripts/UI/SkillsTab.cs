// Hearth & Hollow — the camp's Skills tab: the current class's move loadout.
// Design v2: no levels, XP or skill trees. Top: the loadout — J attack, K skill, L skill, Space dash, C Ultimate.
// Below: the class's moves for the picked slot (ticket 10: locked ones show the arc step that unlocks them;
// unlocked ones are learned here) and the details for whatever you hover.
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

        readonly VisualElement _loadout, _choices, _detail;
        readonly Label _choicesTitle;
        ClassId _cls;
        LoadSlot _slot = LoadSlot.Attack;
        string _focusMove;

        static readonly LoadSlot[] Slots = { LoadSlot.Attack, LoadSlot.Skill1, LoadSlot.Skill2, LoadSlot.Dash, LoadSlot.Ultimate };

        public SkillsTab(GameUI ui)
        {
            Ui = ui;
            Root = UIKit.E("skills-tab");

            var top = UIKit.E("skills-tab__top");
            _loadout = UIKit.E("loadout", "loadout--row");
            top.Add(_loadout);
            Root.Add(top);

            var body = UIKit.E("skills-tab__body");
            var side = UIKit.E("skills-tab__side");
            _choicesTitle = UIKit.L("", "subhead", "loadout__choices-title");
            side.Add(_choicesTitle);
            _choices = UIKit.E("loadout__choices");
            side.Add(_choices);
            body.Add(side);
            _detail = UIKit.E("skill-detail", "skill-detail--compact");
            body.Add(_detail);
            Root.Add(body);
        }

        /// Called when the tab is opened: back to the selected class, nothing focused.
        public void OnShow()
        {
            _cls = S.Profile.selectedClass;
            _focusMove = null;
        }

        // ───────────────────────── refresh ─────────────────────────
        public void Refresh()
        {
            if (_cls != S.Profile.selectedClass) { _cls = S.Profile.selectedClass; _focusMove = null; }
            var cp = S.Profile.Progress(_cls);
            BuildLoadout();
            BuildChoices(cp);
            BuildDetail();
        }

        static string SlotKey(LoadSlot s) { return s == LoadSlot.Attack ? "J" : s == LoadSlot.Skill1 ? "K" : s == LoadSlot.Skill2 ? "L" : s == LoadSlot.Ultimate ? "C / I" : "Space"; }
        static string SlotName(LoadSlot s) { return s == LoadSlot.Attack ? "Attack" : s == LoadSlot.Dash ? "Dash" : s == LoadSlot.Ultimate ? "Ultimate" : "Skill"; }

        void BuildLoadout()
        {
            _loadout.Clear();
            var load = S.Loadout(_cls);
            for (int i = 0; i < Slots.Length; i++)
            {
                LoadSlot slot = Slots[i];
                var m = slot == LoadSlot.Ultimate ? S.UltimateMove(_cls) : load[i];
                var card = UIKit.E("load-slot");
                if (slot == LoadSlot.Ultimate) card.AddToClassList("load-slot--ult");
                card.EnableInClassList("load-slot--on", slot == _slot);
                card.Add(UIKit.L(SlotKey(slot), "load-slot__key"));
                var icon = UIKit.E("load-slot__icon");
                icon.Add(UIKit.L(m != null ? m.emoji : "＋", "load-slot__emoji"));
                card.Add(icon);
                var txt = UIKit.E("load-slot__text");
                txt.Add(UIKit.L(SlotName(slot), "load-slot__slot"));
                txt.Add(UIKit.L(m != null ? m.name : slot == LoadSlot.Ultimate ? "None yet — unlocked along the way" : "Empty — pick a skill below", "load-slot__name"));
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
            string equipped = Progression.Equipped(cp, _slot);
            if (_slot == LoadSlot.Skill2 && !string.IsNullOrEmpty(equipped))
                _choices.Add(UIKit.B("Empty the L slot", () => S.EquipMove(_cls, LoadSlot.Skill2, null), "btn--small", "btn--ghost", "loadout__clear"));
            foreach (var m in S.SlotChoices(_cls, _slot))
            {
                string block = S.EquipBlock(_cls, _slot, m.id);
                bool on = m.id == equipped;
                bool ready = S.ReadyToLearn(_cls, m.id);
                bool locked = !ready && !Progression.Owns(C, S.Profile, _cls, m);
                var row = UIKit.E("move-row");
                row.EnableInClassList("move-row--on", on);
                row.EnableInClassList("move-row--locked", block != null && !on && !ready);
                row.EnableInClassList("move-row--ready", ready);
                row.Add(UIKit.L(locked ? "🔒" : m.emoji, "move-row__emoji"));
                var txt = UIKit.E("move-row__text");
                txt.Add(UIKit.L(m.name, "move-row__name"));
                txt.Add(UIKit.L(on ? "Equipped" : ready ? "✨ Unlocked — learn it at " + Progression.LearnAtText(Progression.Unlock(C, m.id)) : block == null ? "Click to equip" : block, "move-row__sub"));
                row.Add(txt);
                string id = m.id;
                if (ready)
                {
                    var learn = UIKit.B("Learn", () => { if (S.LearnMove(_cls, id)) { _focusMove = id; Refresh(); } }, "btn--small", "move-row__learn");
                    learn.SetEnabled(S.CanEditSkills);
                    row.Add(learn);
                }
                else row.RegisterCallback<ClickEvent>(e => { if (!on && S.EquipBlock(_cls, _slot, id) == null) S.EquipMove(_cls, _slot, id); });
                row.RegisterCallback<PointerEnterEvent>(e => FocusMove(id));
                foreach (var c in row.Children()) if (!(c is Button)) c.pickingMode = PickingMode.Ignore;
                _choices.Add(row);
            }
            if (_choices.childCount == 0) _choices.Add(UIKit.L("Nothing for this slot yet.", "muted", "small"));
        }

        // ───── details ─────
        void FocusMove(string id)
        {
            if (_focusMove == id) return;
            _focusMove = id;
            BuildDetail();
        }

        void BuildDetail()
        {
            _detail.Clear();
            var m = C.Move(_focusMove);
            if (m == null) { _detail.Add(UIKit.L("Hover a move to see what it does.", "muted")); return; }
            MoveDetail(m);
        }

        void MoveDetail(MoveData m)
        {
            var top = UIKit.E("skill-detail__top");
            top.Add(UIKit.L(m.emoji, "skill-detail__emoji"));
            var names = UIKit.E("skill-detail__names");
            names.Add(UIKit.L(m.name, "skill-detail__name"));
            string slot = m.slot == MoveSlot.Attack ? "Attack (J)" : m.slot == MoveSlot.Dash ? "Dash (Space)" : m.slot == MoveSlot.Ultimate ? "Ultimate (C / I) · hits charge it" : "Skill (K or L)";
            names.Add(UIKit.L(slot + (m.anyClass ? " · every class" : " · " + m.owner), "muted", "small"));
            top.Add(names);
            _detail.Add(top);
            _detail.Add(UIKit.L(m.description, "skill-detail__desc"));
            var un = Progression.Unlock(C, m.id);
            if (un != null && !Progression.Owns(C, S.Profile, _cls, m))
                _detail.Add(UIKit.L((Progression.RequirementMet(S.Profile, un) ? "✨ Unlocked: learn it at " + Progression.LearnAtText(un) : "🔒 " + Progression.RequirementText(C, un)), "small", "skill-detail__unlock"));
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
    }
}
