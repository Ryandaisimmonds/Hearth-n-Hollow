// Hearth & Hollow — the camp's 🔨 Workshop tab (the town's workshop): kitchen upgrades — each station's level (more
// dishes, a better-meal chance) and the meal pantry's shelves, paid in coins and building materials.
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class KitchenTab : CampPage
    {
        readonly VisualElement _kitchen;

        public KitchenTab(GameUI ui, VisualElement page) : base(ui)
        {
            // ── Workshop tab: kitchen upgrades — each station's level (more dishes, better-meal chance) and the meal pantry's shelves ──
            var kitchenBox = UIKit.E("kitchen");
            var kTitle = UIKit.L("🔨 Kitchen", "cook-col__title", "kitchen__title");
            kTitle.tooltip = "Upgrade with coins and building materials that monsters drop in the dungeons.";
            kitchenBox.Add(kTitle);
            _kitchen = UIKit.E("kitchen__row");
            kitchenBox.Add(_kitchen);
            page.Add(kitchenBox);   // the Workshop tab
        }

        public override void Refresh() { RefreshKitchen(); }

        /// Coin + material cost chips, each marked short when you don't have enough.
        VisualElement CostRow(KitchenTier t)
        {
            var row = UIKit.E("kitchen__costs");
            var p = S.Profile;
            var coin = UIKit.L("🪙 " + t.price, "cost", "kitchen__cost");
            coin.EnableInClassList("cost--short", p.coins < t.price);
            row.Add(coin);
            if (t.materials != null)
                foreach (var m in t.materials)
                {
                    int have = Economy.PantryCount(p, m.ingredientId);
                    var l = UIKit.L(UIKit.IngEmoji(C, m.ingredientId) + " " + have + "/" + m.count, "cost", "kitchen__cost");
                    l.EnableInClassList("cost--short", have < m.count);
                    l.tooltip = UIKit.IngName(C, m.ingredientId) + ": you have " + have + ", need " + m.count + Where(m.ingredientId);
                    row.Add(l);
                }
            return row;
        }

        /// " — dropped in Meadow Trail, Berry Hunt…" for a material.
        string Where(string materialId)
        {
            var names = new List<string>();
            foreach (var r in C.routes) if (r.materialIds != null && r.materialIds.Contains(materialId)) names.Add(r.name);
            return names.Count > 0 ? "\nDropped by monsters in: " + string.Join(", ", names.ToArray()) : "";
        }

        void RefreshKitchen()
        {
            _kitchen.Clear();
            var p = S.Profile;
            int max = Kitchen.MaxLevel(C);
            for (int i = 0; i < Stations.Length; i++)
            {
                Station st = Stations[i];
                int lv = Kitchen.Level(C, p, st);
                var card = UIKit.E("kitchen-card", "kitchen-card--" + st.ToString().ToLowerInvariant());
                card.Add(UIKit.L(StationLabels[i] + "  ·  Lv " + lv + "/" + max, "kitchen-card__name"));
                card.Add(UIKit.L("Better-meal chance " + UIKit.Pct(Kitchen.Jackpot(C, p, st)), "kitchen-card__now"));
                var next = Kitchen.NextStation(C, p, st);
                if (next == null) card.Add(UIKit.L("Fully upgraded", "small", "muted"));
                else
                {
                    var dishes = Kitchen.DishesAt(C, st, lv + 1);
                    var names = new List<string>();
                    foreach (var d in dishes) names.Add(d.emoji + " " + d.name);
                    card.Add(UIKit.L("Lv " + (lv + 1) + ": " + (names.Count > 0 ? string.Join(", ", names.ToArray()) : "no new dishes") + " · " + UIKit.Pct(Kitchen.Jackpot(C, p, st) + C.tuning.stationJackpotPerLevel), "kitchen-card__next"));
                    card.Add(CostRow(next));
                    string why;
                    bool can = Kitchen.CanAfford(C, p, next, out why);
                    Station s2 = st;
                    var b = UIKit.B("Upgrade", () => { string r; S.UpgradeStation(s2, out r); }, "btn--small", can ? "btn--primary" : "btn--ghost", "kitchen-card__btn");
                    b.SetEnabled(can);
                    b.tooltip = can ? "Upgrade the " + st + " to level " + (lv + 1) : why;
                    card.Add(b);
                }
                _kitchen.Add(card);
            }
            // meal-pantry shelves
            var pc = UIKit.E("kitchen-card", "kitchen-card--pantry");
            pc.Add(UIKit.L("🍽️ Pantry shelves  ·  " + Kitchen.PantrySlots(C, p), "kitchen-card__name"));
            pc.Add(UIKit.L("Cooked meals you can keep", "kitchen-card__now"));
            var np = Kitchen.NextPantry(C, p);
            if (np == null) pc.Add(UIKit.L("Fully upgraded", "small", "muted"));
            else
            {
                pc.Add(UIKit.L("Next: " + np.name + " — " + (int)np.value + " shelves", "kitchen-card__next"));
                pc.Add(CostRow(np));
                string why;
                bool can = Kitchen.CanAfford(C, p, np, out why);
                var b = UIKit.B("Upgrade", () => { string r; S.UpgradePantry(out r); }, "btn--small", can ? "btn--primary" : "btn--ghost", "kitchen-card__btn");
                b.SetEnabled(can);
                b.tooltip = can ? "More shelves for cooked meals" : why;
                pc.Add(b);
            }
            _kitchen.Add(pc);
        }
    }
}
