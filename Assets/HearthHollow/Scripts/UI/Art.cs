// Hearth & Hollow — art helpers for the UI. Everything here reads sprites through ArtLibrary (which may
// be null / have empty slots) and always falls back to the emoji the UI used before the art pass.
//   Art.Pic(sprite, "🍓", "ico")      → sprite as a scale-to-fit background, or an emoji Label
//   new SpriteAnimView(anim, "🐗")    → looping pixel-art idle, PlayAttack() once, emoji fallback
using System;
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public static class Art
    {
        /// Element (class "pic") showing `sprite` scaled to fit, or — when the sprite is null — a Label with
        /// the emoji (classes "pic" + "pic--emoji"). Extra classes are always added. Ignores picking.
        public static VisualElement Pic(Sprite sprite, string fallbackEmoji, params string[] classes)
        {
            VisualElement e;
            if (sprite != null)
            {
                e = new VisualElement();
                e.style.backgroundImage = new StyleBackground(sprite);
                e.AddToClassList("pic--sprite");
            }
            else
            {
                e = new Label(fallbackEmoji ?? "");
                e.AddToClassList("pic--emoji");
            }
            e.AddToClassList("pic");
            for (int i = 0; i < classes.Length; i++) if (!string.IsNullOrEmpty(classes[i])) e.AddToClassList(classes[i]);
            e.pickingMode = PickingMode.Ignore;
            return e;
        }

        /// Pic of a UI icon by id (see ArtLibrary icon ids).
        public static VisualElement Icon(string iconId, string fallbackEmoji, params string[] classes)
        {
            return Pic(ArtLibrary.GetIcon(iconId), fallbackEmoji, classes);
        }

        /// Static pic of an anim's first idle frame (cropped). Cheaper than a SpriteAnimView for lists.
        public static VisualElement AnimFrame(ArtLibrary.Anim a, string fallbackEmoji, params string[] classes)
        {
            Sprite s = null;
            if (a != null && a.idle != null && a.idle.Count > 0 && a.idle[0] != null) s = ArtLibrary.Cropped(a, a.idle[0]);
            var e = Pic(s, fallbackEmoji, classes);
            if (s != null) e.AddToClassList("pic--pixel");
            return e;
        }

        /// Wraps `inner` in an item frame (round octagon frame from the pack). `variant` e.g. "item-frame--green".
        public static VisualElement Framed(VisualElement inner, params string[] classes)
        {
            var f = UIKit.E("item-frame");
            for (int i = 0; i < classes.Length; i++) if (!string.IsNullOrEmpty(classes[i])) f.AddToClassList(classes[i]);
            f.pickingMode = PickingMode.Ignore;
            f.Add(inner);
            return f;
        }

        /// Icon + text row (class "icon-text"). The label has class "icon-text__label".
        public static VisualElement IconText(string iconId, string fallbackEmoji, string text, out Label label, params string[] classes)
        {
            var row = UIKit.E("icon-text");
            for (int i = 0; i < classes.Length; i++) if (!string.IsNullOrEmpty(classes[i])) row.AddToClassList(classes[i]);
            row.Add(Icon(iconId, fallbackEmoji, "ico", "icon-text__icon"));
            label = UIKit.L(text, "icon-text__label");
            row.Add(label);
            return row;
        }

        public static VisualElement IconText(string iconId, string fallbackEmoji, string text, params string[] classes)
        {
            Label l;
            return IconText(iconId, fallbackEmoji, text, out l, classes);
        }

        /// Resource-bar chip with an icon on the left.
        public static VisualElement Chip(string iconId, string fallbackEmoji, out Label text, params string[] classes)
        {
            var c = UIKit.E("chip");
            for (int i = 0; i < classes.Length; i++) if (!string.IsNullOrEmpty(classes[i])) c.AddToClassList(classes[i]);
            if (iconId != null) c.Add(Icon(iconId, fallbackEmoji, "ico", "chip__icon"));
            text = UIKit.L("", "chip__text");
            c.Add(text);
            return c;
        }

        /// Button whose face is an icon + a label (the Button's own text stays empty).
        public static Button IconButton(string iconId, string fallbackEmoji, string text, Action onClick, params string[] classes)
        {
            var b = UIKit.B("", onClick, classes);
            b.AddToClassList("btn--icon");
            b.Add(Icon(iconId, fallbackEmoji, "ico", "btn__icon"));
            var l = UIKit.L(text, "btn__label");
            l.pickingMode = PickingMode.Ignore;
            b.Add(l);
            return b;
        }

        /// Coin price button ("🪙 120").
        public static Button PriceButton(string text, Action onClick, params string[] classes)
        {
            return IconButton("coin", "🪙", text, onClick, classes);
        }

        public static string StatIconId(BuffStat s)
        {
            switch (s)
            {
                case BuffStat.MaxHp: case BuffStat.Heal: return "hp";
                case BuffStat.DamagePct: return "damage";
                case BuffStat.Armour: return "armour";
                case BuffStat.CritPct: return "crit";
                case BuffStat.GatherZonePct: return "gather";
                case BuffStat.CoinPct: return "coinpct";
                case BuffStat.XpPct: return "xp";
                case BuffStat.MoveSpeedPct: return "speed";
                case BuffStat.HealOnKill: return "heal";
                case BuffStat.Regen: return "regen";
            }
            return null;
        }

        public static string StatEmoji(BuffStat s)
        {
            switch (s)
            {
                case BuffStat.MaxHp: case BuffStat.Heal: return "❤️";
                case BuffStat.DamagePct: return "🗡️";
                case BuffStat.Armour: return "🛡️";
                case BuffStat.CritPct: return "💥";
                case BuffStat.GatherZonePct: return "🧺";
                case BuffStat.CoinPct: return "🪙";
                case BuffStat.XpPct: return "✨";
                case BuffStat.MoveSpeedPct: return "👟";
                case BuffStat.HealOnKill: return "💚";
                case BuffStat.Regen: return "💗";
            }
            return "•";
        }

        public static string ModIconId(RouteMod mod)
        {
            switch (mod)
            {
                case RouteMod.Double: return "xp";
                case RouteMod.Rich: return "coin";
                case RouteMod.Dangerous: return "skull";
                case RouteMod.Lucky: return "gather";
            }
            return null;
        }

        public static string ModEmoji(RouteMod mod)
        {
            switch (mod)
            {
                case RouteMod.Double: return "✖️";
                case RouteMod.Rich: return "💰";
                case RouteMod.Dangerous: return "☠️";
                case RouteMod.Lucky: return "🌟";
            }
            return "";
        }

        public static string ModName(RouteMod mod)
        {
            switch (mod)
            {
                case RouteMod.Double: return "×2 Double XP";
                case RouteMod.Rich: return "Rich";
                case RouteMod.Dangerous: return "Dangerous";
                case RouteMod.Lucky: return "Lucky";
            }
            return "";
        }

        /// Route-mod badge: icon + name, coloured by the existing badge--{mod} classes.
        public static VisualElement ModBadge(RouteMod mod)
        {
            var b = IconText(ModIconId(mod), ModEmoji(mod), ModName(mod), "badge", "badge--mod", "badge--" + mod.ToString().ToLowerInvariant());
            b.tooltip = UIKit.ModHint(mod);
            return b;
        }

        public static string EventIconId(CampEvent ev)
        {
            switch (ev)
            {
                case CampEvent.Merchant: return "event_merchant";
                case CampEvent.Storm: return "event_storm";
                case CampEvent.Armourer: return "event_armourer";
                case CampEvent.Forager: return "event_forager";
                case CampEvent.HarvestMoon: return "event_harvest";
            }
            return "event_none";
        }

        public static string SkillIconId(SkillData skill)
        {
            return skill != null ? "skill_" + skill.kind.ToString().ToLowerInvariant() : null;
        }

        public static ArtLibrary.Anim ClassAnim(ClassId id) { return ArtLibrary.GetClass(id.ToString()); }

        /// Replaces everything inside `holder` with `content`.
        public static void Set(VisualElement holder, VisualElement content)
        {
            holder.Clear();
            if (content != null) holder.Add(content);
        }
    }

    /// Animated pixel-art character from an ArtLibrary.Anim (cropped frames, scale-to-fit, point-filtered).
    /// Loops idle at anim.fps; PlayAttack() plays the attack row once then returns to idle.
    /// The timer pauses while the element is off-panel so rebuilt lists never leak running timers.
    public class SpriteAnimView : VisualElement
    {
        ArtLibrary.Anim _anim;
        readonly Label _fallback;
        IVisualElementScheduledItem _timer;
        long _intervalMs;
        bool _attached, _attacking, _initialised;
        int _frame;

        public SpriteAnimView() : this(null, "") { }

        public SpriteAnimView(ArtLibrary.Anim anim, string fallbackEmoji, params string[] classes)
        {
            AddToClassList("pic");
            AddToClassList("anim");
            for (int i = 0; i < classes.Length; i++) if (!string.IsNullOrEmpty(classes[i])) AddToClassList(classes[i]);
            pickingMode = PickingMode.Ignore;

            _fallback = new Label();
            _fallback.AddToClassList("pic--emoji");
            _fallback.AddToClassList("anim__emoji");
            _fallback.pickingMode = PickingMode.Ignore;
            Add(_fallback);

            RegisterCallback<AttachToPanelEvent>(e => { _attached = true; UpdateTimer(); });
            RegisterCallback<DetachFromPanelEvent>(e => { _attached = false; UpdateTimer(); });
            SetAnim(anim, fallbackEmoji);
        }

        public ArtLibrary.Anim Anim { get { return _anim; } }
        bool HasArt { get { return _anim != null && _anim.idle != null && _anim.idle.Count > 0; } }

        /// Swap the character. Calling again with the same anim keeps the current frame (no restart).
        public void SetAnim(ArtLibrary.Anim a, string fallbackEmoji)
        {
            _fallback.text = fallbackEmoji ?? "";
            bool same = _initialised && a == _anim;
            _initialised = true;
            _anim = a;
            if (!same)
            {
                _attacking = false;
                _frame = 0;
                if (HasArt) ShowFrame(_anim.idle[0]);
                else style.backgroundImage = new StyleBackground(StyleKeyword.None);
            }
            bool has = HasArt;
            UIKit.Show(_fallback, !has);
            EnableInClassList("anim--sprite", has);
            EnableInClassList("anim--emoji", !has);
            EnableInClassList("pic--emoji", !has);
            UpdateTimer();
        }

        /// One pass of the attack frames, then back to idle. With no art, a little scale pop instead.
        public void PlayAttack()
        {
            if (!HasArt || _anim.attack == null || _anim.attack.Count == 0)
            {
                Juice.Pop(this, 1.15f, 0.05f, 0.15f);
                return;
            }
            _attacking = true;
            _frame = 0;
            ShowFrame(_anim.attack[0]);
            UpdateTimer();
        }

        void UpdateTimer()
        {
            bool run = _attached && HasArt;
            if (!run)
            {
                if (_timer != null) _timer.Pause();
                return;
            }
            float fps = _anim.fps > 0.1f ? _anim.fps : 6f;
            long ms = Math.Max(16L, (long)(1000f / fps));
            if (_timer == null || ms != _intervalMs)
            {
                if (_timer != null) _timer.Pause();
                _intervalMs = ms;
                _timer = schedule.Execute(Tick).Every(ms);
            }
            else _timer.Resume();
        }

        void Tick()
        {
            if (!HasArt) return;
            List<Sprite> list = _attacking ? _anim.attack : _anim.idle;
            _frame++;
            if (list == null || _frame >= list.Count)
            {
                if (_attacking) _attacking = false;
                list = _anim.idle;
                _frame = 0;
            }
            ShowFrame(list[_frame]);
        }

        void ShowFrame(Sprite frame)
        {
            if (frame == null) return;
            style.backgroundImage = new StyleBackground(ArtLibrary.Cropped(_anim, frame));
        }
    }
}
