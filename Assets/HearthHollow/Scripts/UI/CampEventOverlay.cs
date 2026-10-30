// Hearth & Hollow — today's camp event as a pop-up. Opens by itself when you get back to camp from an
// expedition (if something is happening), and again from the event chip at the top right of the Hearth.
// The travelling merchant's wares are bought here.
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class CampEventOverlay : UIScreen
    {
        readonly VisualElement _box, _iconHolder, _wares;
        readonly Label _title, _text, _coins, _waresTitle;

        public CampEventOverlay(GameUI ui) : base(ui, "overlay", "camp-event")
        {
            _box = UIKit.E("overlay__box", "camp-event__box");
            Root.Add(_box);
            // clicking the dim backdrop closes it; clicks inside the box don't
            Root.RegisterCallback<PointerDownEvent>(e => { if (e.target == Root) Ui.CloseCampEvent(); });

            var close = UIKit.B("", () => Ui.CloseCampEvent(), "settings__close");
            close.tooltip = "Close (Esc)";
            _box.Add(close);

            _title = UIKit.L("", "overlay__title");
            _box.Add(_title);
            _iconHolder = UIKit.E("camp-event__icon-holder");
            _box.Add(_iconHolder);
            _text = UIKit.L("", "camp-event__text");
            _box.Add(_text);

            _waresTitle = UIKit.L("Wares — today only", "subhead", "subhead--merchant", "camp-event__wares-title");
            _box.Add(_waresTitle);
            _wares = UIKit.E("merchant-grid", "camp-event__wares");
            _box.Add(_wares);

            var foot = UIKit.E("camp-event__foot");
            var coinChip = Art.Chip("coin", "🪙", out _coins, "chip--coins");
            foot.Add(coinChip);
            foot.Add(UIKit.B("Back to camp", () => Ui.CloseCampEvent(), "btn--primary"));
            _box.Add(foot);
        }

        public override void Refresh()
        {
            var h = S.Hearth;
            CampEvent ev = h != null ? h.campEvent : CampEvent.None;
            _box.EnableInClassList("camp-event--merchant", ev == CampEvent.Merchant);
            _box.EnableInClassList("camp-event--storm", ev == CampEvent.Storm);
            _box.EnableInClassList("camp-event--armourer", ev == CampEvent.Armourer);
            _box.EnableInClassList("camp-event--forager", ev == CampEvent.Forager);
            _box.EnableInClassList("camp-event--harvest", ev == CampEvent.HarvestMoon);
            _title.text = Title(ev);
            Art.Set(_iconHolder, Art.Icon(Art.EventIconId(ev), Emoji(ev), "ico", "camp-event__icon"));
            _text.text = Text(C, h);
            _coins.text = S.Profile.coins + " coins";

            _wares.Clear();
            bool merchant = ev == CampEvent.Merchant && h.merchant != null && h.merchant.Count > 0;
            UIKit.Show(_waresTitle, merchant);
            UIKit.Show(_wares, merchant);
            UIKit.Show(_coins.parent, merchant);
            if (!merchant) return;
            var p = S.Profile;
            for (int i = 0; i < h.merchant.Count; i++)
            {
                MerchantOffer o = h.merchant[i];
                var w = UIKit.E("card", "merchant-card");
                w.EnableInClassList("merchant-card--sold", o.sold);
                w.Add(Art.Framed(Art.Pic(ArtLibrary.GetIngredient(o.ingredientId), UIKit.IngEmoji(C, o.ingredientId), "merchant-card__emoji"), "item-frame--sm"));
                w.Add(UIKit.L(UIKit.IngName(C, o.ingredientId) + " ×" + o.count, "merchant-card__name"));
                int idx = i;
                System.Action buyIt = () => { string r; S.BuyMerchant(idx, out r); };
                var b = o.sold ? UIKit.B("Sold", buyIt, "btn--small", "btn--buy") : Art.PriceButton(o.price.ToString(), buyIt, "btn--small", "btn--buy");
                b.SetEnabled(!o.sold && p.coins >= o.price);
                w.Add(b);
                _wares.Add(w);
            }
            // sometimes a trinket too
            var t = Gear.Trinket(C, h.merchantTrinket);
            if (t != null)
            {
                bool sold = h.merchantTrinketSold || Gear.Owns(p, t.id);
                var w = UIKit.E("card", "merchant-card", "merchant-card--trinket");
                w.EnableInClassList("merchant-card--sold", sold);
                w.Add(UIKit.L(t.emoji, "merchant-card__trinket-emoji"));
                var txt = UIKit.E("merchant-card__trinket-text");
                txt.Add(UIKit.L("Trinket · " + t.name, "merchant-card__name"));
                txt.Add(UIKit.L(Gear.EffectText(C, t), "merchant-card__trinket-fx"));
                w.Add(txt);
                System.Action buyT = () => { string r; S.BuyMerchantTrinket(out r); };
                var b = sold ? UIKit.B("Sold", buyT, "btn--small", "btn--buy") : Art.PriceButton(h.merchantTrinketPrice.ToString(), buyT, "btn--small", "btn--buy");
                b.SetEnabled(!sold && p.coins >= h.merchantTrinketPrice);
                w.Add(b);
                _wares.Add(w);
            }
        }

        public override bool HandleKey(KeyCode key)
        {
            if (key == KeyCode.Escape || key == KeyCode.Return || key == KeyCode.KeypadEnter || key == KeyCode.Space)
                Ui.CloseCampEvent();
            return true;   // modal
        }

        // ───────── shared text (also used by the Hearth's event chip) ─────────
        public static string Title(CampEvent ev)
        {
            switch (ev)
            {
                case CampEvent.Merchant: return "Travelling Merchant";
                case CampEvent.Storm: return "Storm Night";
                case CampEvent.Armourer: return "Travelling Armourer";
                case CampEvent.Forager: return "A Forager's Gift";
                case CampEvent.HarvestMoon: return "Harvest Moon";
            }
            return "A Quiet Evening";
        }

        /// Short name for the header chip.
        public static string ChipText(CampEvent ev)
        {
            switch (ev)
            {
                case CampEvent.Merchant: return "Merchant";
                case CampEvent.Storm: return "Storm";
                case CampEvent.Armourer: return "Armourer";
                case CampEvent.Forager: return "Forager";
                case CampEvent.HarvestMoon: return "Harvest moon";
            }
            return "Quiet night";
        }

        public static string Emoji(CampEvent ev)
        {
            switch (ev)
            {
                case CampEvent.Merchant: return "🧳";
                case CampEvent.Storm: return "⛈️";
                case CampEvent.Armourer: return "⚒️";
                case CampEvent.Forager: return "🧺";
                case CampEvent.HarvestMoon: return "🌕";
            }
            return "🌙";
        }

        public static string Text(GameContent c, HearthState h)
        {
            CampEvent ev = h != null ? h.campEvent : CampEvent.None;
            switch (ev)
            {
                case CampEvent.Merchant:
                    return "A travelling merchant has set up by the fire with rare ingredients. They move on when you next head out.";
                case CampEvent.Storm:
                    return "A storm rolls through the hollow — every route is Dangerous today (harder, but richer).";
                case CampEvent.Armourer:
                    return "A travelling armourer is visiting — weapons and armour in the Workshop are " + UIKit.Pct(c.tuning.armourerDiscount) + " cheaper today.";
                case CampEvent.Forager:
                    return "A passing forager left a gift in your pantry: " + GiftText(c, h) + ".";
                case CampEvent.HarvestMoon:
                    return "Harvest moon — every successful gather yields +" + c.tuning.harvestMoonGatherBonus + " extra on today's run.";
            }
            return "A quiet evening at camp. The fire crackles.";
        }

        static string GiftText(GameContent c, HearthState h)
        {
            if (h == null || h.gift == null || h.gift.Count == 0) return "a few bits and bobs";
            var parts = new List<string>();
            for (int i = 0; i < h.gift.Count; i++)
                parts.Add(UIKit.IngEmoji(c, h.gift[i].ingredientId) + " " + UIKit.IngName(c, h.gift[i].ingredientId) + " ×" + h.gift[i].count);
            return string.Join(", ", parts.ToArray());
        }
    }
}
