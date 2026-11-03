// Hearth & Hollow — the choice at a ladder (contentVersion 14): go deeper, or head home with your haul.
// Shows what's below (better grades, tougher monsters, the boss on the last floor) and what you'd risk: a fall keeps
// only a quarter of the bag, heading home keeps all of it. The dungeon is paused while this is open.
using System;
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Dungeon;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class LadderChoiceOverlay : UIScreen
    {
        readonly Label _title, _below, _odds, _bag, _risk, _home;
        readonly Button _deeper, _goHome, _stay;
        Action _onDeeper, _onHome;

        public LadderChoiceOverlay(GameUI ui) : base(ui, "overlay", "ladder")
        {
            var box = UIKit.E("overlay__box", "ladder__box");
            Root.Add(box);
            _title = UIKit.L("", "overlay__title");
            box.Add(_title);
            _below = UIKit.L("", "ladder__line");
            _odds = UIKit.L("", "ladder__line", "ladder__odds");
            _bag = UIKit.L("", "ladder__line", "ladder__bag");
            _risk = UIKit.L("", "ladder__line", "ladder__risk");
            _home = UIKit.L("", "ladder__line", "muted");
            box.Add(_below); box.Add(_odds); box.Add(_bag); box.Add(_risk); box.Add(_home);
            var row = UIKit.E("ladder__buttons");
            _deeper = UIKit.B("", () => Choose(true), "btn--primary", "ladder__btn");
            _goHome = UIKit.B("🏠 Head home with your haul", () => Choose(false), "btn--secondary", "ladder__btn");
            _stay = UIKit.B("Not yet", () => Ui.CloseLadderChoice(), "btn--ghost", "ladder__btn");
            row.Add(_deeper); row.Add(_goHome); row.Add(_stay);
            box.Add(row);
            box.Add(UIKit.L("Enter / (A): go deeper  ·  H / (X): head home  ·  Esc / (B): not yet", "small", "muted"));
        }

        public void Set(Action deeper, Action home) { _onDeeper = deeper; _onHome = home; }

        void Choose(bool deeper)
        {
            var a = deeper ? _onDeeper : _onHome;
            Ui.CloseLadderChoice();
            if (a != null) a();
        }

        public override void Refresh()
        {
            var run = S.Run;
            if (run == null) return;
            int next = run.floor + 1;
            bool last = next >= run.floorsTotal;
            _title.text = "🪜 Floor " + next + " of " + run.floorsTotal + " lies below";
            _below.text = last ? "👑 The boss waits at the bottom — beat it to clear the route (and its big loot)." : "Tougher monsters, richer pickings.";
            _odds.text = "Deeper floors hold rarer ingredients: the ones tier-2 dishes need.";
            // what's in the bag
            int ings = 0, deep = 0, mats = 0;
            foreach (var b in run.bag)
            {
                if (b == null || string.IsNullOrEmpty(b.ingredientId) || b.count <= 0) continue;
                var ing = C.Ingredient(b.ingredientId);
                if (Kitchen.IsMaterial(C, b.ingredientId)) mats += b.count;
                else if (ing != null && ing.deep) deep += b.count;
                else ings += b.count;
            }
            _bag.text = "🎒 Your haul: " + ings + " ingredients" + (deep > 0 ? " · ✨ " + deep + " rare" : "") + " · 🪵 " + mats + " materials";
            _risk.text = "💀 If you fall, you keep only " + UIKit.Pct(C.tuning.deathKeepFraction) + " of it. ❤️ " + run.hp + " / " + (S.Stats != null ? S.Stats.maxHp : run.hp) + " HP left.";
            _home.text = "Heading home now keeps everything, plus part of the clear bonus.";
            _deeper.text = last ? "👑 Go down to the boss" : "⬇️ Go deeper";
        }

        public override bool HandleKey(KeyCode key)
        {
            if (key == KeyCode.Escape) Ui.CloseLadderChoice();
            else if (key == KeyCode.Return || key == KeyCode.KeypadEnter) Choose(true);   // (not E: that's the key that opened this)
            else if (key == KeyCode.H) Choose(false);
            return true;
        }
    }
}
