// Hearth & Hollow — end-of-run summary (already settled by the session: LastSummary).
using HearthHollow.Core;
using HearthHollow.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class RunSummaryScreen : UIScreen
    {
        readonly Label _title, _route;
        readonly VisualElement _body, _headline;
        bool _hasHeadline;

        public RunSummaryScreen(GameUI ui) : base(ui, "summary")
        {
            var box = UIKit.E("panel", "summary__box");
            Root.Add(box);
            _headline = UIKit.E("summary__headline");
            _headline.pickingMode = PickingMode.Ignore;
            UIKit.Show(_headline, false);
            _title = UIKit.L("", "summary__title");
            _route = UIKit.L("", "muted");
            box.Add(_headline);
            box.Add(_title);
            box.Add(_route);
            _body = UIKit.E("summary__body");
            box.Add(_body);
            box.Add(Art.IconButton("event_none", "🔥", "Back to the hearth  (Enter)", () => S.ReturnToHearth(), "btn--primary", "btn--huge"));
        }

        protected override void OnShow()
        {
            Refresh();
            Juice.Pop(_hasHeadline ? _headline : (VisualElement)_title, 1.25f, 0.08f, 0.3f);
        }

        public override void Refresh()
        {
            var s = S.LastSummary;
            _body.Clear();
            if (s == null) { _title.text = "Back home"; _route.text = ""; UIKit.Show(_headline, false); _hasHeadline = false; return; }

            _title.text = UIKit.OutcomeTitle(s.outcome);
            // painted headline (COMPLETE / DEFEAT) when available; the outcome text stays as the subtitle
            Sprite head = s.outcome == RunOutcome.Cleared || s.outcome == RunOutcome.Returned ? ArtLibrary.GetIcon("text_complete")
                        : s.outcome == RunOutcome.Died ? ArtLibrary.GetIcon("text_defeat") : null;
            _headline.style.backgroundImage = head != null ? new StyleBackground(head) : new StyleBackground(StyleKeyword.None);
            _hasHeadline = head != null;
            UIKit.Show(_headline, _hasHeadline);
            _title.EnableInClassList("summary__title--sub", head != null);
            _title.EnableInClassList("summary__title--cleared", s.outcome == RunOutcome.Cleared);
            _title.EnableInClassList("summary__title--died", s.outcome == RunOutcome.Died);
            var cls = C.Class(s.classId);
            _route.text = UIKit.RouteName(C, s.routeId) + "  ·  " + (cls != null ? cls.name : s.classId.ToString())
                        + (s.floorsTotal > 0 ? "  ·  floor " + s.floorReached + " of " + s.floorsTotal : "");

            var stats = UIKit.E("stat-row");
            stats.Add(Big("trophy", "🚪", s.roomsCleared.ToString(), "rooms"));
            stats.Add(Big("skull", "💀", s.kills.ToString(), "kills"));
            stats.Add(Big("coin", "🪙", "+" + s.totalCoins, "coins"));
            stats.Add(Big("xp", "✨", "+" + s.xpGained, "XP"));
            _body.Add(stats);

            // story: key item found + the chapter's closing text (first clear only), then what's next
            if (!string.IsNullOrEmpty(s.keyItemFound) || !string.IsNullOrEmpty(s.storyText))
            {
                var story = UIKit.E("summary__section", "summary__story");
                if (!string.IsNullOrEmpty(s.keyItemFound))
                {
                    var k = Routes.KeyItem(C, s.keyItemFound);
                    story.Add(UIKit.L("Key item found: " + Routes.KeyItemLabel(C, s.keyItemFound), "subhead", "summary__key"));
                    if (k != null) story.Add(UIKit.L(k.description, "small", "muted"));
                }
                if (!string.IsNullOrEmpty(s.storyText)) story.Add(UIKit.L(s.storyText, "summary__story-text"));
                story.Add(UIKit.L("📖 Next: " + Routes.Objective(C, S.Profile), "small", "summary__next"));
                _body.Add(story);
            }

            // trinkets found (first-clear reward, rare drops)
            if (s.trinketsFound != null && s.trinketsFound.Count > 0)
            {
                var tr = UIKit.E("summary__section", "summary__trinkets");
                foreach (var id in s.trinketsFound)
                {
                    var t = Gear.Trinket(C, id);
                    if (t == null) continue;
                    tr.Add(UIKit.L("💍 Trinket found: " + t.emoji + " " + t.name, "subhead", "summary__key"));
                    tr.Add(UIKit.L(Gear.EffectText(C, t) + (Gear.Worn(S.Profile, id) ? " — wearing it now" : " — wear it in the Workshop"), "small", "muted"));
                }
                _body.Add(tr);
            }

            var coins = UIKit.E("summary__section");
            coins.Add(UIKit.L("Coins", "subhead"));
            coins.Add(Line("From kills", "🪙 " + s.coinsFromKills));
            coins.Add(Line(s.outcome == RunOutcome.Returned ? "Return bonus (floor " + s.floorReached + "/" + s.floorsTotal + ")" : "Clear bonus",
                           s.clearCoins > 0 ? "🪙 " + s.clearCoins : "— (no clear)"));
            coins.Add(Line("Total banked", "🪙 " + s.totalCoins, "summary__line--total"));
            _body.Add(coins);

            var xp = UIKit.E("summary__section");
            xp.Add(UIKit.L("Experience", "subhead"));
            xp.Add(Line("XP gained", "✨ " + s.xpGained + (s.outcome == RunOutcome.Died ? " (halved — limped home)" : "")));
            if (s.levelAfter > s.levelBefore)
            {
                Sprite lvl = ArtLibrary.GetIcon("text_levelup");
                if (lvl != null) xp.Add(Art.Pic(lvl, "", "summary__levelup-image"));
                xp.Add(Line("Level", "⭐ " + s.levelBefore + " → " + s.levelAfter + "  Level up!", "summary__line--levelup"));
            }
            else
                xp.Add(Line("Level", s.levelAfter.ToString()));
            var cp = S.Profile.Progress(s.classId);
            int next = Economy.XpToNext(C.tuning, cp.level);
            var bar = new Bar("bar--xp");
            bool maxed = cp.level >= C.tuning.levelCap;
            bar.Set(maxed ? 1 : cp.xp, maxed ? 1 : next, maxed ? "MAX LEVEL" : cp.xp + " / " + next + " XP");
            xp.Add(bar);
            _body.Add(xp);

            var goods = UIKit.E("summary__section");
            goods.Add(UIKit.L("Goods brought home", "subhead"));
            var row = UIKit.E("pantry-grid");
            if (s.goods == null || s.goods.Count == 0) row.Add(UIKit.L("Nothing this time.", "muted"));
            else
                for (int i = 0; i < s.goods.Count; i++)
                {
                    var g = s.goods[i];
                    var item = UIKit.E("pantry-item");
                    item.Add(Art.Framed(Art.Pic(ArtLibrary.GetIngredient(g.ingredientId), UIKit.IngEmoji(C, g.ingredientId), "pantry-item__emoji"), "item-frame--sm"));
                    item.Add(UIKit.L(UIKit.IngName(C, g.ingredientId), "pantry-item__name"));
                    item.Add(UIKit.L("×" + g.count, "pantry-item__count"));
                    row.Add(item);
                }
            goods.Add(row);
            _body.Add(goods);

            if (s.unlockedRouteIds != null && s.unlockedRouteIds.Count > 0)
            {
                var un = UIKit.E("summary__section", "summary__unlocks");
                un.Add(Art.IconText("map", "🗺️", "New routes unlocked!", "subhead"));
                for (int i = 0; i < s.unlockedRouteIds.Count; i++)
                    un.Add(UIKit.L("🔓 " + UIKit.RouteName(C, s.unlockedRouteIds[i]), "summary__unlock"));
                _body.Add(un);
            }
        }

        static VisualElement Big(string iconId, string emoji, string value, string label)
        {
            var s = UIKit.E("stat", "stat--big");
            s.Add(Art.IconText(iconId, emoji, value, "stat__value"));
            s.Add(UIKit.L(label, "stat__label"));
            return s;
        }

        static VisualElement Line(string label, string value, string cls = null)
        {
            var row = UIKit.E("summary__line");
            if (cls != null) row.AddToClassList(cls);
            row.Add(UIKit.L(label, "summary__line-label"));
            row.Add(UIKit.L(value, "summary__line-value"));
            return row;
        }

        public override bool HandleKey(KeyCode key)
        {
            if (key == KeyCode.Return || key == KeyCode.KeypadEnter || key == KeyCode.Space) { S.ReturnToHearth(); return true; }
            return false;
        }
    }
}
