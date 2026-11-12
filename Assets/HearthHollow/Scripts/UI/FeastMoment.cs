// Hearth & Hollow — ticket 60: the feast at the gate. As you set out, your packed meals pop onto the table one by
// one, their bonuses fly into your stats and the numbers count up; then off you go. A click or any key skips it.
using System;
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class FeastMoment
    {
        public const float PopAt = 0.3f, PopGap = 0.42f, FlyTime = 0.5f, CountTime = 0.7f, Hold = 0.6f;

        public readonly VisualElement Root;
        readonly VisualElement _table;
        readonly Label _title, _atk, _hp, _hint;
        readonly UiParticles _fx = new UiParticles();
        readonly List<VisualElement> _cards = new List<VisualElement>();
        IVisualElementScheduledItem _tick;
        Action _done;
        float _start, _atk0, _atk1; int _hp0, _hp1; int _popped; bool _flown;

        public bool Playing { get; private set; }

        /// Attack and HP with no meals and with the packed ones.
        public static void Stats(GameContent c, Profile p, List<EatenMeal> meals, out float atk0, out float atk1, out int hp0, out int hp1)
        {
            var bare = StatCalc.ComputeWith(c, p, p.selectedClass, new List<EatenMeal>());
            var fed = StatCalc.ComputeWith(c, p, p.selectedClass, meals ?? new List<EatenMeal>());
            atk0 = bare.damage; atk1 = fed.damage; hp0 = bare.maxHp; hp1 = fed.maxHp;
        }

        /// When the whole moment ends (the last count-up plus a beat), for `n` meals.
        public static float Length(int n) { return PopAt + Mathf.Max(0, n - 1) * PopGap + FlyTime + CountTime + Hold; }

        public FeastMoment()
        {
            Root = UIKit.E("feast");
            Root.style.position = Position.Absolute;
            Root.style.left = 0; Root.style.top = 0; Root.style.right = 0; Root.style.bottom = 0;
            Root.RegisterCallback<PointerDownEvent>(e => Skip());
            var col = UIKit.E("feast__col");
            _title = UIKit.L("🍽️ A feast before the road", "feast__title");
            col.Add(_title);
            _table = UIKit.E("feast__table");
            col.Add(_table);
            var stats = UIKit.E("feast__stats");
            _atk = UIKit.L("", "feast__stat"); _hp = UIKit.L("", "feast__stat");
            stats.Add(_atk); stats.Add(_hp);
            col.Add(stats);
            _hint = UIKit.L("click to skip", "small", "feast__hint");
            col.Add(_hint);
            Root.Add(col);
            Root.Add(_fx.Layer);
            UIKit.Show(Root, false);
        }

        public void Play(GameContent c, Profile p, Action done)
        {
            var meals = Cooking.Packed(c, p);
            Stats(c, p, meals, out _atk0, out _atk1, out _hp0, out _hp1);
            _done = done;
            _table.Clear(); _cards.Clear(); _fx.Clear();
            var cls = c.Class(p.selectedClass);
            foreach (var m in meals)
            {
                var r = c.Recipe(m.recipeId);
                var card = UIKit.E("feast__meal");
                card.Add(Art.Framed(Art.Pic(ArtLibrary.GetRecipe(m.recipeId), r != null ? r.emoji : "🍽️", "feast__pic"), "item-frame--lg"));
                card.Add(UIKit.L(Cooking.GradeGem(m.grade) + " " + (r != null ? r.name : m.recipeId), "feast__name"));
                if (r != null) card.Add(UIKit.L(UIKit.MealSummary(Cooking.MealBuffs(c, cls, m)), "feast__buffs"));
                card.style.opacity = 0f;
                _table.Add(card); _cards.Add(card);
            }
            SetStats(0f);
            _popped = 0; _flown = false;
            _start = UIKit.Now;
            Playing = true;
            UIKit.Show(Root, true);
            Root.style.opacity = 1f;
            if (_tick == null) _tick = Root.schedule.Execute(Tick).Every(16); else _tick.Resume();
        }

        void SetStats(float k)
        {
            float a = Mathf.Lerp(_atk0, _atk1, k); int h = Mathf.RoundToInt(Mathf.Lerp(_hp0, _hp1, k));
            _atk.text = "🗡️ " + a.ToString("0.#") + (k >= 1f && _atk1 > _atk0 ? "  <color=#9ee07a>(+" + (_atk1 - _atk0).ToString("0.#") + ")</color>" : "");
            _hp.text = "❤️ " + h + (k >= 1f && _hp1 > _hp0 ? "  <color=#9ee07a>(+" + (_hp1 - _hp0) + ")</color>" : "");
        }

        void Tick()
        {
            if (!Playing) return;
            float t = UIKit.Now - _start;
            int n = _cards.Count;
            // the meals pop onto the table
            while (_popped < n && t >= PopAt + _popped * PopGap)
            {
                var card = _cards[_popped];
                card.style.opacity = 1f;
                Juice.Pop(card, 1.2f, 0.06f, 0.22f);
                HearthHollow.Runtime.GameAudio.Play("coin", 1f, 0f);   // ticket 62
                if (card.worldBound.width > 1f) _fx.Burst(_fx.At(card, new Vector2(0.5f, 0.35f)), new Color(1f, 0.82f, 0.35f), 14, 240f);
                _popped++;
            }
            float flyAt = PopAt + Mathf.Max(0, n - 1) * PopGap + 0.25f;
            // their bonuses fly into your stats
            if (!_flown && t >= flyAt)
            {
                _flown = true;
                HearthHollow.Runtime.GameAudio.Play("feast");
                foreach (var card in _cards)
                {
                    if (!(card.worldBound.width > 1f) || !(_atk.worldBound.width > 1f)) continue;
                    Vector2 from = _fx.At(card, new Vector2(0.5f, 0.85f));
                    _fx.Fly("🗡️", from, _fx.At(_atk, new Vector2(0.3f, 0.5f)), Color.white, 22f, FlyTime);
                    _fx.Fly("❤️", from, _fx.At(_hp, new Vector2(0.3f, 0.5f)), Color.white, 22f, FlyTime);
                }
            }
            float countAt = flyAt + FlyTime * 0.8f;
            float k = Mathf.Clamp01((t - countAt) / CountTime);
            SetStats(n == 0 ? 1f : k);
            if (k >= 1f && t - countAt < CountTime + 0.05f) { Juice.Pop(_atk, 1.15f, 0.05f, 0.2f); Juice.Pop(_hp, 1.15f, 0.05f, 0.2f); }
            if (t >= Length(n)) Finish();
        }

        public void Skip() { if (Playing) Finish(); }

        /// Stop without setting out (the screen closed under it).
        public void Cancel() { if (!Playing) return; _done = null; Finish(); }

        void Finish()
        {
            Playing = false;
            if (_tick != null) _tick.Pause();
            UIKit.Show(Root, false);
            _fx.Clear();
            var d = _done; _done = null;
            if (d != null) d();
        }
    }
}
