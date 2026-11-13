// Hearth & Hollow — the camp's right-hand column: who's going (class, Change class, stats, skills, equipment),
// then Set out. Meals are pre-run buffs, so the stats preview "base → with meals".
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class CampColumn : CampPage
    {
        readonly SpriteAnimView _portrait;
        readonly Label _className, _classSub;
        readonly VisualElement _xpHolder, _stats, _loadout, _equip;
        readonly Button _setOut;
        readonly Label _setOutHint;
        readonly System.Action<CampTab> _showTab;   // the host's tabs (the Edit / Upgrades links)

        public CampColumn(GameUI ui, VisualElement right, System.Action<CampTab> showTab) : base(ui)
        {
            _showTab = showTab;
            var who = UIKit.E("panel", "panel--who");
            var whoRow = UIKit.E("who");
            _portrait = new SpriteAnimView(null, "", "who__portrait");
            whoRow.Add(Art.Framed(_portrait, "who__frame"));
            var whoText = UIKit.E("who__text");
            var nameRow = UIKit.E("who__name-row");
            _className = UIKit.L("", "who__name");
            nameRow.Add(_className);
            var change = UIKit.B("Change class", () => Ui.OpenClassSelect(), "btn--small", "btn--ghost", "who__change");
            nameRow.Add(change);
            whoText.Add(nameRow);
            _classSub = UIKit.L("", "who__sub");
            whoText.Add(_classSub);
            _xpHolder = UIKit.E("who__xp");
            whoText.Add(_xpHolder);
            whoRow.Add(whoText);
            who.Add(whoRow);

            who.Add(SectionHead("📊 Stats", null, null));
            _stats = UIKit.E("stats", "who__stats");
            who.Add(_stats);
            who.Add(SectionHead("⭐ Skills", "Edit", () => _showTab(CampTab.Skills)));
            _loadout = UIKit.E("who__loadout");
            who.Add(_loadout);
            who.Add(SectionHead("⚒️ Equipment", "Upgrades", () => _showTab(CampTab.Upgrades)));
            _equip = UIKit.E("who__equip");
            who.Add(_equip);
            right.Add(who);

            var go = UIKit.E("panel", "panel--go");
            _setOut = UIKit.B("Set out ➜", SetOut, "btn--primary", "btn--setout", "go__btn");
            _setOut.tooltip = "Choose a route on the Route Board (Enter)";
            go.Add(_setOut);
            _setOutHint = UIKit.L("", "setout-hint", "go__hint");
            go.Add(_setOutHint);
            right.Add(go);
        }

        public override void Refresh() { RefreshWho(); RefreshGo(); }

        public void SetOut() { Ui.OpenRoutes(); }

        static VisualElement SectionHead(string title, string linkText, System.Action link)
        {
            var h = UIKit.E("who__section");
            h.Add(UIKit.L(title, "who__section-title"));
            if (link != null) h.Add(UIKit.B(linkText + " ➜", link, "btn--small", "btn--ghost", "who__section-link"));
            return h;
        }

        void RefreshWho()
        {
            var p = S.Profile;
            var cls = C.Class(p.selectedClass);
            var cp = p.Progress(p.selectedClass);
            var t = C.tuning;
            _portrait.SetAnim(Art.ClassAnim(p.selectedClass), cls != null ? cls.emoji : "🧑‍🍳");
            _className.text = cls != null ? cls.name : p.selectedClass.ToString();
            _classSub.text = (cls != null && !string.IsNullOrEmpty(cls.role) ? cls.role : "")
                + (cls != null && UIKit.LensText(cls).Length > 0 ? " · " + UIKit.LensText(cls) : "");
            _xpHolder.Clear();
            RefreshStats();
            RefreshLoadout();
            RefreshEquipment();
        }

        void RefreshStats()
        {
            _stats.Clear();
            var b = S.BaseStats;
            var st = S.Stats;   // includes the packed tray
            var row = UIKit.E("stat-row");
            row.Add(Stat("hp", "❤️", "Max HP", b.maxHp.ToString(), st.maxHp.ToString(), st.maxHp > b.maxHp));
            row.Add(Stat("damage", "🗡️", "Damage", UIKit.Num(b.damage), UIKit.Num(st.damage), st.damage > b.damage + 0.01f));
            row.Add(Stat("armour", "🛡️", "Armour", UIKit.Pct(b.armourPct), UIKit.Pct(st.armourPct), st.armourPct > b.armourPct + 0.001f));
            row.Add(Stat("crit", "💥", "Crit", UIKit.Pct(b.critChance), UIKit.Pct(st.critChance), st.critChance > b.critChance + 0.001f));
            _stats.Add(row);

            var extras = UIKit.E("stats__extras");
            var mods = S.Mods;
            if (mods.moveSpeed > 1.001f) extras.Add(Art.IconText("action_flee", "👢", "speed +" + UIKit.Pct(mods.moveSpeed - 1f), "small"));
            if (st.gatherZoneMult > 1.001f) extras.Add(Art.IconText("gather", "🧺", "gather zone +" + UIKit.Pct(st.gatherZoneMult - 1f), "small"));
            if (st.coinMult > 1.001f) extras.Add(Art.IconText("coinpct", "🪙", "coins +" + UIKit.Pct(st.coinMult - 1f), "small"));
            if (st.xpMult > 1.001f) extras.Add(Art.IconText("xp", "✨", "XP +" + UIKit.Pct(st.xpMult - 1f), "small"));
            extras.Add(UIKit.L("base → with your packed meals", "small", "muted", "who__stats-note"));
            _stats.Add(extras);
        }

        static VisualElement Stat(string iconId, string emoji, string label, string baseValue, string withMeals, bool boosted)
        {
            var s = UIKit.E("stat");
            s.Add(Art.IconText(iconId, emoji, boosted ? baseValue + " → " + withMeals : withMeals, "stat__value"));
            s.EnableInClassList("stat--boosted", boosted);
            s.Add(UIKit.L(label, "stat__label"));
            return s;
        }

        /// The four loadout slots as big cards (click → Skills tab).
        void RefreshLoadout()
        {
            _loadout.Clear();
            var p = S.Profile;
            var load = S.Loadout(p.selectedClass);
            var mods = S.Mods;
            string[] keys = { "J", "K", "L", "Space" };
            string[] slots = { "Attack", "Skill", "Skill", "Dash" };
            for (int i = 0; i < 4; i++)
            {
                var m = i < load.Length ? load[i] : null;
                var card = UIKit.E("who-move", "clickable");
                card.EnableInClassList("who-move--empty", m == null);
                card.Add(UIKit.L(keys[i], "who-move__key"));
                card.Add(UIKit.L(m != null ? m.emoji : "＋", "who-move__emoji"));
                var txt = UIKit.E("who-move__text");
                txt.Add(UIKit.L(m != null ? m.name : "Empty", "who-move__name"));
                string sub = slots[i];
                if (m != null && m.slot != MoveSlot.Attack)
                {
                    float cd = m.slot == MoveSlot.Skill ? m.cooldown * mods.skillCooldown : m.cooldown * mods.dashCooldown;
                    sub += " · " + cd.ToString("0.#") + "s";
                }
                if (m != null && m.owner != p.selectedClass && !m.anyClass) sub += " · from " + m.owner;
                txt.Add(UIKit.L(sub, "who-move__sub"));
                card.Add(txt);
                if (m != null) card.tooltip = m.name + "\n" + m.description;
                card.RegisterCallback<ClickEvent>(e => _showTab(CampTab.Skills));
                foreach (var ch in card.Children()) SetIgnore(ch);
                _loadout.Add(card);
            }
            // the chosen Ultimate (C / I)
            var um = S.UltimateMove(p.selectedClass);
            if (um != null)
            {
                var card = UIKit.E("who-move", "who-move--ult", "clickable");
                card.Add(UIKit.L("C / I", "who-move__key"));
                card.Add(UIKit.L(um.emoji, "who-move__emoji"));
                var txt = UIKit.E("who-move__text");
                txt.Add(UIKit.L(um.name, "who-move__name"));
                txt.Add(UIKit.L("Ultimate · " + um.cooldown.ToString("0.#") + "s", "who-move__sub"));
                card.Add(txt);
                card.tooltip = um.name + "\n" + um.description + "\nEvery hit you land charges it faster.";
                card.RegisterCallback<ClickEvent>(e => _showTab(CampTab.Skills));
                foreach (var ch in card.Children()) SetIgnore(ch);
                _loadout.Add(card);
            }
        }

        /// Current gear (weapon, body, head, boots, bag) and worn trinkets (click → Upgrades tab).
        void RefreshEquipment()
        {
            _equip.Clear();
            var p = S.Profile;
            var grid = UIKit.E("who-gear-grid");
            foreach (var gs in Gear.Shop) grid.Add(GearChip(gs, GearPrefix(gs), GearEmoji(gs)));
            _equip.Add(grid);

            var tr = UIKit.E("who-trinkets");
            int slots = S.TrinketSlots;
            var worn = p.equippedTrinkets ?? new List<string>();
            for (int i = 0; i < slots; i++)
            {
                var t = i < worn.Count ? Gear.Trinket(C, worn[i]) : null;
                var chip = UIKit.E("who-trinket", "clickable");
                chip.EnableInClassList("who-trinket--empty", t == null);
                chip.Add(UIKit.L(t != null ? t.emoji : "💍", "who-trinket__emoji"));
                var txt = UIKit.E("who-trinket__text");
                txt.Add(UIKit.L(t != null ? t.name : "Empty trinket slot", "who-trinket__name"));
                if (t != null) txt.Add(UIKit.L(Gear.EffectText(C, t), "who-trinket__sub"));
                chip.Add(txt);
                chip.RegisterCallback<ClickEvent>(e => _showTab(CampTab.Upgrades));
                foreach (var ch in chip.Children()) SetIgnore(ch);
                tr.Add(chip);
            }
            _equip.Add(tr);
        }

        VisualElement GearChip(GearSlot slot, string prefix, string emoji)
        {
            var p = S.Profile;
            GearTier cur = Gear.Current(C, p, slot, p.selectedClass);
            var chip = UIKit.E("who-gear", "clickable");
            chip.Add(GearPic(prefix, cur, emoji));
            var txt = UIKit.E("who-gear__text");
            txt.Add(UIKit.L(Gear.SlotName(slot), "who-gear__slot"));
            txt.Add(UIKit.L(cur != null ? cur.name : "none", "who-gear__name"));
            if (cur != null) txt.Add(UIKit.L(Gear.ValueText(slot, cur), "who-gear__value"));
            chip.Add(txt);
            var next = Gear.Next(C, p, slot, p.selectedClass);
            chip.tooltip = next != null ? "Next: " + next.name + " — " + Gear.ValueText(slot, next) + " (" + S.GearPrice(next) + " coins in Upgrades)" : "Maxed";
            chip.RegisterCallback<ClickEvent>(e => _showTab(CampTab.Upgrades));
            foreach (var ch in chip.Children()) SetIgnore(ch);
            return chip;
        }

        void RefreshGo()
        {
            int packed = S.Profile.tray.Count;
            _setOutHint.text = packed > 0 ? "🧺 " + packed + "/" + C.tuning.traySize + " meals packed" : "No meals packed — base stats only";
            _setOutHint.EnableInClassList("setout-hint--warn", packed == 0);
        }
    }
}
