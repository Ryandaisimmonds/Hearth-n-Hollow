// Hearth & Hollow — the camp's ⚒️ Upgrades tab (the town's smithy): gear tracks and key items | trinkets.
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class GearTab : CampPage
    {
        readonly VisualElement _gear, _trinkets, _keyItems;
        readonly Label _gearTitle;

        public GearTab(GameUI ui, VisualElement page) : base(ui)
        {
            // ── Upgrades: gear + key items | trinkets ──
            var shopRow = UIKit.E("workshop");
            page.Add(shopRow);
            var shopA = UIKit.E("workshop__col");
            var shopB = UIKit.E("workshop__col", "workshop__col--trinkets");
            shopRow.Add(shopA); shopRow.Add(shopB);
            _gearTitle = UIKit.L("", "subhead", "workshop__title");
            shopA.Add(_gearTitle);
            _gear = UIKit.E("workshop__gear");
            shopA.Add(_gear);
            shopA.Add(UIKit.L("🗝️ Key items", "subhead", "workshop__title"));
            _keyItems = UIKit.E("workshop__keys");
            shopA.Add(_keyItems);
            _trinkets = UIKit.E("workshop__trinkets");
            shopB.Add(_trinkets);
        }

        public override void Refresh() { RefreshWorkshop(); }

        void RefreshWorkshop()
        {
            var p = S.Profile;
            var cls = C.Class(p.selectedClass);
            _gearTitle.text = "⚒️ Gear — shared by everyone";
            _gear.Clear();
            foreach (var gs in Gear.Shop) _gear.Add(GearRow(gs, GearPrefix(gs), GearEmoji(gs)));
            if (S.GearRefund > 0)
                _gear.Add(UIKit.L("🪙 Weapons, armour and helmets are gone: power comes from food now. You were refunded " + S.GearRefund + " coins for them.", "small", "workshop__note", "workshop__refund"));
            var note = UIKit.L("Gear is utility, shared by every class: boots for speed (the first pair lets you dash), a lantern to see further, a bag to carry more home." +
                (S.Hearth != null && S.Hearth.campEvent == CampEvent.Armourer ? "  ⚒️ The armourer's in camp: everything is " + UIKit.Pct(C.tuning.armourerDiscount) + " off today." : ""),
                "small", "muted", "workshop__note");
            _gear.Add(note);

            RefreshKeyItems();
            RefreshTrinkets();
        }



        VisualElement GearRow(GearSlot slot, string prefix, string emoji)
        {
            var p = S.Profile;
            GearTier cur = Gear.Current(C, p, slot, p.selectedClass), next = Gear.Next(C, p, slot, p.selectedClass);
            var row = UIKit.E("gear-row");
            row.Add(GearPic(prefix, cur, emoji));
            var txt = UIKit.E("gear-row__text");
            var top = UIKit.E("gear-row__top");
            top.Add(UIKit.L(Gear.SlotName(slot), "gear-row__slot"));
            top.Add(UIKit.L(cur != null ? cur.name : "none", "gear-row__name"));
            if (cur != null) top.Add(UIKit.L(Gear.ValueText(slot, cur), "gear-row__value"));
            txt.Add(top);
            var list = Gear.Tiers(C, slot);
            int idx = Gear.TierIndex(p, slot, p.selectedClass);
            var pips = UIKit.E("pips", "gear-row__pips");
            for (int i = 0; list != null && i < list.Count; i++) pips.Add(UIKit.E("pip", i <= idx ? "pip--on" : "pip--off"));
            txt.Add(pips);
            if (next != null) txt.Add(UIKit.L("Next: " + next.name + " — " + Gear.ValueText(slot, next), "gear-row__next"));
            row.Add(txt);
            if (next == null) { row.Add(UIKit.L("✔ Maxed", "gear-row__max")); return row; }
            int price = S.GearPrice(next);
            bool sale = price < next.price;
            GearSlot gs = slot;
            var b = Art.PriceButton(price + (sale ? "  (was " + next.price + ")" : ""), () => { string r; S.BuyGear(gs, out r); }, "btn--small", "btn--buy");
            b.EnableInClassList("btn--sale", sale);
            b.SetEnabled(p.coins >= price);
            row.Add(b);
            return row;
        }

        void RefreshKeyItems()
        {
            _keyItems.Clear();
            var p = S.Profile;
            if (C.keyItems == null || C.keyItems.Count == 0) { _keyItems.Add(UIKit.L("No key items in this story.", "muted", "small")); return; }
            foreach (var k in C.keyItems)
            {
                bool have = Routes.HasKey(p, k.id);
                var row = UIKit.E("key-row");
                row.EnableInClassList("key-row--have", have);
                row.Add(UIKit.L(have ? k.emoji : "❔", "key-row__emoji"));
                var txt = UIKit.E("key-row__text");
                txt.Add(UIKit.L(have ? k.name : "???", "key-row__name"));
                var src = Routes.KeySource(C, k.id);
                string hint = have ? k.description : src != null && Routes.Discovered(C, p, src) ? "Waits in " + src.emoji + " " + src.name : "Somewhere further along the story";
                txt.Add(UIKit.L(hint, "key-row__sub"));
                row.Add(txt);
                _keyItems.Add(row);
            }
        }

        void RefreshTrinkets()
        {
            _trinkets.Clear();
            var p = S.Profile;
            int slots = S.TrinketSlots;
            var worn = p.equippedTrinkets ?? new List<string>();
            var owned = p.trinkets ?? new List<string>();

            _trinkets.Add(UIKit.L("💍 Trinkets — " + worn.Count + "/" + slots + " worn", "subhead", "workshop__title"));
            var wornRow = UIKit.E("trinket-slots");
            int maxSlots = Features.ThirdTrinketSlot ? Mathf.Max(slots, C.tuning.trinketSlots + 1) : slots;
            for (int i = 0; i < maxSlots; i++)
            {
                if (i < worn.Count)
                {
                    var t = Gear.Trinket(C, worn[i]);
                    string id = worn[i];
                    var card = TrinketCard(t, "trinket--worn");
                    card.tooltip = "Click to take it off";
                    card.AddToClassList("clickable");
                    card.RegisterCallback<ClickEvent>(e => S.UnequipTrinket(id));
                    wornRow.Add(card);
                }
                else if (i < slots)
                {
                    var empty = UIKit.E("trinket", "trinket--empty");
                    empty.Add(UIKit.L("＋", "trinket__emoji"));
                    empty.Add(UIKit.L("Empty slot — click a trinket below to wear it", "trinket__sub"));
                    wornRow.Add(empty);
                }
                else
                {
                    var r = C.Route(C.tuning.trinketSlotRoute);
                    var locked = UIKit.E("trinket", "trinket--empty", "trinket--locked");
                    locked.Add(UIKit.L("🔒", "trinket__emoji"));
                    locked.Add(UIKit.L("Opens when you clear " + (r != null ? r.emoji + " " + r.name : "the story"), "trinket__sub"));
                    wornRow.Add(locked);
                }
            }
            _trinkets.Add(wornRow);

            // owned, not worn
            var bag = new List<string>();
            foreach (var id in owned) if (!worn.Contains(id)) bag.Add(id);
            _trinkets.Add(UIKit.L("Your collection (" + owned.Count + "/" + C.trinkets.FindAll(x => Gear.InSlice(x)).Count + ")", "small", "workshop__sub"));
            var grid = UIKit.E("trinket-grid");
            if (bag.Count == 0) grid.Add(UIKit.L(owned.Count == 0 ? "None yet — buy one below or find them in the dungeons." : "Everything you own is being worn.", "small", "muted"));
            foreach (var id in bag)
            {
                var t = Gear.Trinket(C, id);
                if (t == null) continue;
                string tid = id;
                var card = TrinketCard(t, "trinket--owned");
                card.AddToClassList("clickable");
                card.tooltip = worn.Count < slots ? "Click to wear it" : "Slots full — take one off first";
                card.RegisterCallback<ClickEvent>(e => { string r; S.EquipTrinket(tid, out r); if (r != null) Ui.Juice.Toast(r, "toast--warn"); });
                grid.Add(card);
            }
            _trinkets.Add(grid);

            // for sale in the Workshop
            var stock = Gear.WorkshopStock(C, p);
            var later = Gear.WorkshopLater(C, p);
            _trinkets.Add(UIKit.L("For sale", "small", "workshop__sub"));
            var sale = UIKit.E("trinket-grid");
            if (stock.Count == 0 && later.Count == 0) sale.Add(UIKit.L("Sold out — you've bought every trinket for sale.", "small", "muted"));
            foreach (var t in stock)
            {
                var card = TrinketCard(t, "trinket--sale");
                string tid = t.id;
                var b = Art.PriceButton(t.price.ToString(), () => { string r; S.BuyTrinket(tid, out r); }, "btn--small", "btn--buy", "trinket__buy");
                b.SetEnabled(p.coins >= t.price);
                card.Add(b);
                sale.Add(card);
            }
            foreach (var t in later)
            {
                var card = UIKit.E("trinket", "trinket--later");
                card.Add(UIKit.L("❔", "trinket__emoji"));
                var txt = UIKit.E("trinket__text");
                txt.Add(UIKit.L("Coming in chapter " + t.shopChapter, "trinket__name"));
                txt.Add(UIKit.L(t.price + " coins", "trinket__sub"));
                card.Add(txt);
                sale.Add(card);
            }
            _trinkets.Add(sale);

            // still to find out there
            int hiddenClear = 0, hiddenDrop = 0, hiddenMerchant = 0;
            foreach (var t in C.trinkets)
            {
                if (Gear.Owns(p, t.id) || t.inWorkshop || !Gear.InSlice(t)) continue;   // the slice only counts its own trinkets (ticket 16)
                if (!string.IsNullOrEmpty(t.fromRouteId)) hiddenClear++;
                else if (t.drop) hiddenDrop++;
                else if (t.merchant) hiddenMerchant++;
            }
            if (hiddenClear + hiddenDrop + hiddenMerchant > 0)
            {
                var parts = new List<string>();
                if (hiddenClear > 0) parts.Add(hiddenClear + " from first clears of side paths");
                if (hiddenDrop > 0) parts.Add(hiddenDrop + " from rare boss / elite drops");
                if (hiddenMerchant > 0) parts.Add(hiddenMerchant + " only the travelling merchant sells");
                _trinkets.Add(UIKit.L("Still to find: " + string.Join(" · ", parts.ToArray()) + ".", "small", "muted", "workshop__find"));
            }
        }

        VisualElement TrinketCard(TrinketData t, string cls)
        {
            var card = UIKit.E("trinket", cls);
            if (t == null) return card;
            card.Add(UIKit.L(t.emoji, "trinket__emoji"));
            var txt = UIKit.E("trinket__text");
            txt.Add(UIKit.L(t.name, "trinket__name"));
            txt.Add(UIKit.L(Gear.EffectText(C, t), "trinket__sub"));
            card.Add(txt);
            foreach (var ch in card.Children()) SetIgnore(ch);
            return card;
        }
    }
}
