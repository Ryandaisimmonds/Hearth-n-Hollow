// Hearth & Hollow — title / class select. Shown on first launch and via "Change class" at camp.
// Click anywhere on a class card to pick it, then confirm with "Play as {Class}" at the bottom.
using HearthHollow.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class ClassSelectScreen : UIScreen
    {
        readonly VisualElement _cards;
        readonly Button _play, _back;
        ClassId _pending;

        public ClassSelectScreen(GameUI ui) : base(ui, "class-select")
        {
            var title = UIKit.E("title-block");
            title.Add(UIKit.L("🔥 Hearth & Hollow", "title-block__name"));
            title.Add(UIKit.L("A lantern-lit picnic at the edge of somewhere you shouldn't be.", "title-block__tagline"));
            Root.Add(title);

            Root.Add(UIKit.L("Choose your cook", "section-title", "class-select__prompt"));
            _cards = UIKit.E("class-cards");
            Root.Add(_cards);

            var footer = UIKit.E("class-select__footer");
            _play = UIKit.B("", Confirm, "btn--primary", "btn--huge", "class-select__play");
            _back = UIKit.B("Back to camp", () => Ui.CloseClassSelect(), "btn--ghost");
            footer.Add(_play);
            footer.Add(_back);
            Root.Add(footer);
        }

        protected override void OnShow() { _pending = S.Profile.selectedClass; }

        void Pick(ClassId id) { _pending = id; Refresh(); }

        void Confirm()
        {
            if (!S.ClassJoined(_pending)) return;
            S.SelectClass(_pending);
            Ui.CloseClassSelect();
        }

        public override void Refresh()
        {
            _cards.Clear();
            var p = S.Profile;
            var t = C.tuning;
            for (int i = 0; i < C.classes.Count; i++)
            {
                ClassData cd = C.classes[i];
                if (!Slice.HasClass(cd.id)) continue;   // the vertical slice only has the Cook
                ClassProgress cp = p.Progress(cd.id);
                bool picked = _pending == cd.id;
                bool joined = S.ClassJoined(cd.id);

                var card = UIKit.E("card", "class-card", "clickable");
                card.EnableInClassList("class-card--selected", picked);
                if (picked) card.Add(UIKit.L("✓", "class-card__tick"));
                var portrait = UIKit.E("class-card__portrait");
                portrait.Add(new SpriteAnimView(Art.ClassAnim(cd.id), cd.emoji, "class-card__emoji", "class-card__anim"));
                card.Add(portrait);
                card.Add(UIKit.L(cd.name, "class-card__name"));
                card.Add(UIKit.L(cd.role, "badge", "class-card__role"));
                card.Add(UIKit.L(BonusText(cd), "class-card__bonus"));
                if (cd.favouriteBonus > 0f) card.Add(UIKit.L("⭐ Favourite: " + StationName(cd.favouriteStation) + " meals +" + UIKit.Pct(cd.favouriteBonus), "class-card__bonus"));
                if (!string.IsNullOrEmpty(cd.description)) card.Add(UIKit.L(cd.description, "muted", "class-card__desc"));
                if (!joined)   // not in your camp yet: say where they join
                {
                    card.Add(Art.IconText("lock", "🔒", S.ClassJoinHint(cd.id), "small", "class-card__locked"));
                    portrait.style.opacity = 0.35f;
                    card.style.opacity = 0.75f;
                }

                var skill = UIKit.E("class-card__skill");
                var skillHead = UIKit.E("class-card__skill-head");
                // the class's real starting skill (the dungeon kit), not the old turn-based one
                var sm = C.moves.Find(x => x.id == cd.starterSkill);
                skillHead.Add(Art.Framed(Art.Icon(sm != null ? "move_" + sm.id : Art.SkillIconId(cd.skill), sm != null ? sm.emoji : (cd.skill != null ? cd.skill.emoji : "✨"), "ico", "ico--lg"), "item-frame--sm"));
                var skillText = UIKit.E("class-card__skill-text");
                skillText.Add(UIKit.L(sm != null ? sm.name : (cd.skill != null ? cd.skill.name : "Skill"), "class-card__skill-name"));
                string sdesc = sm != null ? sm.description : (cd.skill != null ? cd.skill.description : null);
                if (!string.IsNullOrEmpty(sdesc)) skillText.Add(UIKit.L(sdesc, "muted", "small"));
                skillHead.Add(skillText);
                skill.Add(skillHead);
                card.Add(skill);


                // The whole card is the select button (labels/bars ignore picking so clicks land on the card).
                foreach (var child in card.Children()) SetIgnore(child);
                ClassId id = cd.id;
                card.RegisterCallback<ClickEvent>(e => Pick(id));
                _cards.Add(card);
            }

            var pend = C.Class(_pending);
            bool ok = S.ClassJoined(_pending);
            _play.text = ok ? "Play as " + (pend != null ? pend.name : _pending.ToString()) + "  (Enter)" : "🔒 " + S.ClassJoinHint(_pending);
            _play.SetEnabled(ok);
        }

        static void SetIgnore(VisualElement e)
        {
            e.pickingMode = PickingMode.Ignore;
            foreach (var c in e.Children()) SetIgnore(c);
        }

        static string StationName(Station s)
        {
            return s == Station.Pot ? "🍲 Pot" : s == Station.BBQ ? "🍖 BBQ" : "🥪 Sandwich";
        }

        static string BonusText(ClassData cd)
        {
            if (cd.damageMult > 1.001f) return "+" + UIKit.Pct(cd.damageMult - 1f) + " damage";
            if (cd.gatherZoneMult > 1.001f) return "Golden gather zone ×" + UIKit.Num(cd.gatherZoneMult);
            if (cd.mealBuffMult > 1.001f) return "Meal buffs ×" + UIKit.Num(cd.mealBuffMult);
            return cd.baseMaxHp + " base HP";
        }

        public override bool HandleKey(KeyCode key)
        {
            int idx = -1;
            if (key == KeyCode.Alpha1) idx = 0;
            else if (key == KeyCode.Alpha2) idx = 1;
            else if (key == KeyCode.Alpha3) idx = 2;
            else if (key == KeyCode.Alpha4) idx = 3;
            else if (key == KeyCode.Alpha5) idx = 4;
            if (idx >= 0 && idx < C.classes.Count) { Pick(C.classes[idx].id); return true; }
            if (key == KeyCode.LeftArrow || key == KeyCode.RightArrow)
            {
                int cur = C.classes.FindIndex(x => x.id == _pending);
                int n = C.classes.Count;
                cur = (cur + (key == KeyCode.RightArrow ? 1 : n - 1)) % n;
                Pick(C.classes[cur].id);
                return true;
            }
            if (key == KeyCode.Return || key == KeyCode.KeypadEnter) { Confirm(); return true; }
            if (key == KeyCode.Escape) { Ui.CloseClassSelect(); return true; }
            return false;
        }
    }
}
