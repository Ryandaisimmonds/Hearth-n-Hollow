// Hearth & Hollow — small UI Toolkit helpers shared by every screen.
//
// EMOJI NOTE: all placeholder art is emoji. Unity's default runtime font has no emoji glyphs, so
// for them to render you need an emoji-capable font asset (e.g. a TextCore FontAsset made from
// Noto Color Emoji / Segoe UI Emoji) assigned as a fallback in the PanelSettings' Text Settings.
// The UI never depends on emoji to be usable: every emoji is paired with a text label.
using System;
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public static class UIKit
    {
        public static float Now { get { return Time.realtimeSinceStartup; } }

        public static VisualElement E(params string[] classes)
        {
            var e = new VisualElement();
            for (int i = 0; i < classes.Length; i++) e.AddToClassList(classes[i]);
            return e;
        }

        public static Label L(string text, params string[] classes)
        {
            var l = new Label(text);
            for (int i = 0; i < classes.Length; i++) l.AddToClassList(classes[i]);
            return l;
        }

        /// Button that never takes keyboard focus (so the root keeps receiving KeyDownEvents).
        public static Button B(string text, Action onClick, params string[] classes)
        {
            var b = new Button(onClick) { text = text };
            b.focusable = false;
            b.AddToClassList("btn");
            for (int i = 0; i < classes.Length; i++) b.AddToClassList(classes[i]);
            return b;
        }

        /// Button whose face is several stacked labels (labels ignore picking so the button gets the click).
        public static Button BigButton(Action onClick, string cls, out Label title, out Label sub)
        {
            var b = B("", onClick, "btn-big", cls);
            title = L("", "btn-big__title"); title.pickingMode = PickingMode.Ignore;
            sub = L("", "btn-big__sub"); sub.pickingMode = PickingMode.Ignore;
            b.Add(title); b.Add(sub);
            return b;
        }

        public static void Show(VisualElement e, bool on)
        {
            e.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static bool IsInsideButton(IEventHandler target, VisualElement stopAt)
        {
            var ve = target as VisualElement;
            while (ve != null && ve != stopAt)
            {
                if (ve is Button) return true;
                ve = ve.parent;
            }
            return false;
        }

        // ───────── formatting ─────────
        public static string Pct(float f) { return Mathf.RoundToInt(f * 100f) + "%"; }

        public static string Num(float f)
        {
            return Mathf.Approximately(f, Mathf.Round(f)) ? Mathf.RoundToInt(f).ToString() : f.ToString("0.##");
        }

        public static string BuffText(StatMod m, float mult = 1f)
        {
            float v = m.value * mult;
            switch (m.stat)
            {
                case BuffStat.DamagePct: return "+" + Pct(v) + " dmg";
                case BuffStat.MaxHp: return "+" + Num(v) + " max HP";
                case BuffStat.Heal: return "heal " + Num(v);
                case BuffStat.Armour: return "+" + Num(v) + " armour";
                case BuffStat.CritPct: return "+" + Pct(v) + " crit";
                case BuffStat.GatherZonePct: return "+" + Pct(v) + " gather zone";
                case BuffStat.CoinPct: return "+" + Pct(v) + " coins";
                case BuffStat.XpPct: return "+" + Pct(v) + " XP";
                case BuffStat.MoveSpeedPct: return "+" + Pct(v) + " speed";
                case BuffStat.HealOnKill: return "+" + Num(Mathf.Round(v * 10f) / 10f) + " HP per kill";
            }
            return m.stat + " " + Num(v);
        }

        /// Tints a meal card / tray slot / route-board row in its element-slot element's colour (USS el-tint--*).
        public static void ElementTint(VisualElement e, Element el)
        {
            if (e == null || el == Element.None) return;
            e.AddToClassList("el-tint");
            e.AddToClassList("el-tint--" + el.ToString().ToLowerInvariant());
        }

        /// What a meal's element slot does, in words: "🔥 Fire infused" / "❄️ Frost guard".
        public static string SlotLine(GameContent c, string accelId)
        {
            Element inf = Cooking.InfusionOf(c, accelId), g = Cooking.GuardOf(c, accelId);
            if (inf != Element.None) return Elements.Emoji(inf) + " " + Elements.Name(inf) + " infused";
            if (g != Element.None) return Elements.Emoji(g) + " " + Elements.Name(g) + " guard";
            return "";
        }

        /// One word per meal stat, for "gets more from …" lines.
        public static string StatWord(BuffStat s)
        {
            switch (s)
            {
                case BuffStat.DamagePct: return "damage";
                case BuffStat.MaxHp: return "health";
                case BuffStat.Armour: return "armour";
                case BuffStat.CritPct: return "crit";
                case BuffStat.GatherZonePct: return "gathering";
                case BuffStat.CoinPct: return "coins";
                case BuffStat.MoveSpeedPct: return "speed";
                case BuffStat.HealOnKill: return "heal on kill";
            }
            return s.ToString();
        }

        /// How a class takes its food, in words (no maths): "Gets more from health, armour · less from speed".
        public static string LensText(ClassData cls)
        {
            if (cls == null) return "";
            var strong = Meals.StrongStats(cls); var weak = Meals.WeakStats(cls);
            if (strong.Count == 0 && weak.Count == 0)
                return cls.mealBuffMult > 1.001f ? "Gets a bit more from every meal" : "";
            var parts = new List<string>();
            if (strong.Count > 0) parts.Add("Gets more from " + string.Join(", ", strong.ConvertAll(StatWord).ToArray()));
            if (weak.Count > 0) parts.Add("less from " + string.Join(", ", weak.ConvertAll(StatWord).ToArray()));
            return string.Join(" · ", parts.ToArray());
        }

        public static string BuffSummary(List<StatMod> buffs, float mult = 1f)
        {
            if (buffs == null || buffs.Count == 0) return "no buffs";
            var parts = new List<string>();
            for (int i = 0; i < buffs.Count; i++) parts.Add(BuffText(buffs[i], mult));
            return string.Join(" · ", parts.ToArray());
        }

        public static int PantryCount(Profile p, string ingredientId)
        {
            int n = 0;
            for (int i = 0; i < p.pantry.Count; i++)
                if (p.pantry[i].ingredientId == ingredientId) n += p.pantry[i].count;
            return n;
        }

        public static string IngEmoji(GameContent c, string id)
        {
            var ing = c.Ingredient(id);
            return ing != null && !string.IsNullOrEmpty(ing.emoji) ? ing.emoji : "❔";
        }

        public static string IngName(GameContent c, string id)
        {
            var ing = c.Ingredient(id);
            return ing != null ? ing.name : id;
        }

        public static string RouteName(GameContent c, string id)
        {
            var r = c.Route(id);
            return r != null ? r.emoji + " " + r.name : id;
        }

        public static string ModLabel(RouteMod mod)
        {
            switch (mod)
            {
                case RouteMod.Double: return "✖️2 Double XP";
                case RouteMod.Rich: return "💰 Rich";
                case RouteMod.Dangerous: return "☠️ Dangerous";
                case RouteMod.Lucky: return "🌟 Lucky";
            }
            return "";
        }

        public static string ModHint(RouteMod mod)
        {
            switch (mod)
            {
                case RouteMod.Double: return "kill XP ×2";
                case RouteMod.Rich: return "kill coins ×1.5";
                case RouteMod.Dangerous: return "+1 difficulty, more elites, rewards ×1.5";
                case RouteMod.Lucky: return "double drops & gathers";
            }
            return "";
        }

        public static string BehaviourHint(MonsterBehaviour b)
        {
            switch (b)
            {
                case MonsterBehaviour.DoubleHit: return "Attacks twice each turn";
                case MonsterBehaviour.Heal: return "Heals itself when hurt";
                case MonsterBehaviour.WindUp: return "Winds up crushing blows — Guard them!";
            }
            return "A plain old brawler";
        }

        public static string OutcomeTitle(RunOutcome o)
        {
            switch (o)
            {
                case RunOutcome.Cleared: return "🌀 Cleared!";
                case RunOutcome.Fled: return "🏃 Fled home";
                case RunOutcome.Died: return "💀 Limped home";
                case RunOutcome.Returned: return "🎒 Home with your haul";
            }
            return "Back home";
        }
    }

    /// Simple horizontal bar (track + fill + centred text). Use SetLow(true) to join the low-HP pulse.
    public class Bar : VisualElement
    {
        readonly VisualElement _fill;
        readonly Label _text;

        public Bar(string variant)
        {
            AddToClassList("bar");
            if (!string.IsNullOrEmpty(variant)) AddToClassList(variant);
            _fill = UIKit.E("bar__fill");
            _text = UIKit.L("", "bar__text");
            _text.pickingMode = PickingMode.Ignore;
            Add(_fill);
            Add(_text);
        }

        public void Set(float cur, float max, string text)
        {
            float p = max > 0 ? Mathf.Clamp01(cur / max) : 0f;
            _fill.style.width = Length.Percent(p * 100f);
            _text.text = text;
        }

        public void SetLow(bool low) { EnableInClassList("bar--low", low); }
    }

    /// Two-click confirm button ("Flee home" → "Really flee?").
    public class ConfirmButton
    {
        public readonly Button Button;
        readonly string _idle, _confirm;
        readonly Action _onConfirm;
        bool _armed;
        IVisualElementScheduledItem _revert;
        Label _label;   // set by SetIcon: text moves into a child label so it sits beside the icon

        public ConfirmButton(string idle, string confirm, Action onConfirm, params string[] classes)
        {
            _idle = idle; _confirm = confirm; _onConfirm = onConfirm;
            Button = UIKit.B(idle, Click, classes);
        }

        /// Puts an icon at the left of the button face (the text moves into a child label).
        public void SetIcon(VisualElement icon)
        {
            if (_label == null)
            {
                _label = UIKit.L(Button.text, "btn__label");
                _label.pickingMode = PickingMode.Ignore;
                Button.text = "";
                Button.AddToClassList("btn--icon");
                Button.Add(_label);
            }
            if (icon != null) { icon.pickingMode = PickingMode.Ignore; Button.Insert(0, icon); }
        }

        void SetText(string t)
        {
            if (_label != null) _label.text = t;
            else Button.text = t;
        }

        void Click()
        {
            if (!_armed)
            {
                _armed = true;
                SetText(_confirm);
                Button.AddToClassList("btn--armed");
                if (_revert != null) _revert.Pause();
                _revert = Button.schedule.Execute(Reset).StartingIn(3000);
                return;
            }
            Reset();
            _onConfirm();
        }

        public void Reset()
        {
            _armed = false;
            SetText(_idle);
            Button.RemoveFromClassList("btn--armed");
        }
    }
}
