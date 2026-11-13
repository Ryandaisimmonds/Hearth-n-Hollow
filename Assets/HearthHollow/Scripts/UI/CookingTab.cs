// Hearth & Hollow — the camp's 🍳 Cooking tab (the town's cooking area): the kitchen bar (spice rack, larder), the
// meal pantry of cooked meals, filters, then every meal in one colour-coded grid (Pot · BBQ · Sandwich).
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class CookingTab : CampPage
    {
        readonly VisualElement _recipeGrid, _filters;
        readonly Label _filterCount;
        readonly VisualElement _tray, _trayBuffs;
        readonly Button _pantryBtn;
        readonly VisualElement _mealShelf;      // cooked meals waiting to be packed (cooking v9)
        readonly Label _mealShelfTitle;
        // cooking filters (kept while the game runs)
        // ticket 52: filter by special (every meal gives some 🗡️ and ❤️)
        static readonly BuffStat[] FilterStats = { BuffStat.MaxHpPct, BuffStat.Regen, BuffStat.Armour, BuffStat.HealOnKill, BuffStat.DamagePct, BuffStat.CritPct, BuffStat.GatherZonePct, BuffStat.CoinPct, BuffStat.MoveSpeedPct };
        static readonly string[] FilterStatNames = { "HP %", "Healing", "Armour", "Kill heal", "Attack %", "Crit", "Gather", "Coins", "Speed" };   // one per FilterStats entry
        int _fStation = -1;          // -1 = all stations, else index into Stations
        int _fStat = -1;             // -1 = any bonus, else index into FilterStats
        bool _fCanCook;
        readonly SlotDragger _trayDrag;          // reorder / discard inside the tray
        readonly DragToTargets _recipeDrag;      // cooked meal (meal pantry) → tray slot

        public CookingTab(GameUI ui, VisualElement page) : base(ui)
        {
            // ── Cooking: tray across the top (pantry on the right), filters, then one grid of every meal ──
            var trayBox = UIKit.E("tray-bar");
            var trayTitle = UIKit.L("🍳 Kitchen", "cook-col__title", "cook-col__title--tray", "tray-bar__title");
            trayTitle.tooltip = "Cook below. Season meals at the spice rack; pack them at the Route board as you set out.";
            trayBox.Add(trayTitle);
            var trayRow = UIKit.E("tray-bar__row");
            _tray = UIKit.E("tray-bar__slots");
            trayRow.Add(_tray);
            _trayBuffs = UIKit.E("tray-sum", "tray-bar__sum");
            trayRow.Add(_trayBuffs);
            UIKit.Show(_tray, false); UIKit.Show(_trayBuffs, false);   // ticket 29: the tray lives on the Route board now
            var spiceBtn = Art.IconButton("spice_rack", "🧂", "Spice rack", () => Ui.OpenSeason(), "btn--secondary", "tray-pantry-btn");   // ticket 30
            spiceBtn.tooltip = "Season a cooked meal with an element ingredient (Fire, Frost…)";
            trayRow.Add(spiceBtn);
            var hint = UIKit.L("Cook meals here · season them at the 🧂 spice rack · pack them on the 🗺️ Route board as you set out", "small", "muted", "tray-bar__hint");
            trayRow.Insert(0, hint);
            _pantryBtn = Art.IconButton("pantry", "🥕", "Larder", () => Ui.OpenPantry(), "btn--secondary", "tray-pantry-btn");
            _pantryBtn.tooltip = "Every ingredient you've gathered or bought, by grade";
            trayRow.Add(_pantryBtn);
            trayBox.Add(trayRow);
            page.Add(trayBox);

            // meal pantry: cooked meals (with their rolled grade) waiting to be packed
            var shelfBox = UIKit.E("meal-shelf");
            _mealShelfTitle = UIKit.L("", "cook-col__title", "meal-shelf__title");
            _mealShelfTitle.tooltip = "Meals you've cooked. Click one to season it at the spice rack · × throws it out · pack them at the Route board.";
            shelfBox.Add(_mealShelfTitle);
            _mealShelf = UIKit.E("meal-shelf__row");
            shelfBox.Add(_mealShelf);
            page.Add(shelfBox);

            _filters = UIKit.E("meal-filters");
            page.Add(_filters);
            _filterCount = UIKit.L("", "small", "muted", "meal-filters__count");
            page.Add(_filterCount);
            _recipeGrid = UIKit.E("recipe-grid");   // three unnamed columns: Pot · BBQ · Sandwich
            page.Add(_recipeGrid);

            _trayDrag = new SlotDragger(Ui.DragLayer,
                i => i < S.Profile.tray.Count ? RecipeEmoji(S.Profile.tray[i]) : null,
                (from, to) => S.MoveTrayMeal(from, Mathf.Min(to, S.Profile.tray.Count - 1)),
                null,
                i => { if (i < S.Profile.tray.Count) S.DiscardTrayMeal(i); });
            // keys: "always:<recipeId>" for an always-available meal (Camp Bread), else the meal pantry index
            _recipeDrag = new DragToTargets(Ui.DragLayer, (key, slot) =>
            {
                if (key.StartsWith("always:")) S.PackAlwaysMealInto(key.Substring(7), slot); else S.PackMealInto(int.Parse(key), slot);
            });
            _recipeDrag.onClick = key =>   // ticket 29: click a cooked meal to season it (packing happens at the Route board)
            {
                if (!key.StartsWith("always:")) Ui.OpenSeason();
            };
        }

        public override void Refresh() { RefreshTray(); RefreshFilters(); RefreshRecipes(); }

        string RecipeEmoji(string id)
        {
            var r = C.Recipe(id);
            return r != null ? r.emoji : "🍽️";
        }

        /// What the selected class gets out of this meal (its per-stat lens), as a plain-quality helping.
        List<StatMod> ClassBuffs(RecipeData r) { return Cooking.MealBuffs(C, C.Class(S.Profile.selectedClass), new EatenMeal { recipeId = r.id, potency = 1f }); }

        void RefreshTray()
        {
            _tray.Clear();
            _trayDrag.Clear();
            _recipeDrag.ClearTargets();
            var p = S.Profile;
            int size = C.tuning.traySize;
            for (int i = 0; i < size; i++)
            {
                var slot = UIKit.E("slot", "tray-slot", "tray-slot--bar");
                if (i < p.tray.Count)
                {
                    var r = C.Recipe(p.tray[i]);
                    var pm = i < p.packed.Count ? p.packed[i] : null;
                    float pot = Meals.TrayPotency(C, p.tray, i);
                    slot.AddToClassList("slot--filled");
                    if (pm != null) UIKit.ElementTint(slot, Cooking.SlotElement(C, pm.accelId));
                    slot.Add(Art.Framed(Art.Pic(ArtLibrary.GetRecipe(p.tray[i]), r != null ? r.emoji : "🍽️", "slot__emoji"), "tray-slot__well"));
                    var txt = UIKit.E("tray-slot__text");
                    var acc = pm != null && !string.IsNullOrEmpty(pm.accelId) ? C.Ingredient(pm.accelId) : null;
                    txt.Add(UIKit.L((pm != null ? Cooking.GradeGem(pm.grade) + " " : "") + (r != null ? r.name : p.tray[i]) + (acc != null ? " + " + acc.emoji : ""), "slot__name"));
                    if (r != null && pm != null)
                    {
                        var em = Cooking.AsEaten(pm); em.potency = pot;
                        txt.Add(UIKit.L(UIKit.MealSummary(Cooking.MealBuffs(C, C.Class(p.selectedClass), em)) + (pot < 0.99f ? " (×" + UIKit.Num(pot) + " repeat)" : ""), "slot__sub"));
                    }
                    slot.Add(txt);
                    int idx = i;
                    var x = UIKit.B("×", () => S.DiscardTrayMeal(idx), "slot__x");
                    x.tooltip = "Take off the tray — back into the meal pantry";
                    slot.Add(x);
                }
                else
                {
                    slot.AddToClassList("slot--empty");
                    slot.Add(Art.Framed(UIKit.L((i + 1).ToString(), "tray-slot__num"), "tray-slot__well", "item-frame--empty"));
                    var txt = UIKit.E("tray-slot__text");
                    txt.Add(UIKit.L("Pack a cooked meal here", "slot__sub", "muted"));
                    slot.Add(txt);
                }
                foreach (var child in slot.Children()) if (!(child is Button)) SetIgnore(child);
                _tray.Add(slot);
                _trayDrag.Register(slot);
                _recipeDrag.AddTarget(slot);
            }

            // What the packed tray adds — one total per stat, then any combos.
            _trayBuffs.Clear();
            var meals = Cooking.Packed(C, p);
            var syns = Meals.ActiveSynergies(C, meals);
            if (meals.Count == 0) { _trayBuffs.Add(UIKit.L("Nothing packed — you'll set out on base stats.", "tray-bonus__head")); }
            else
            {
                float[] total = Meals.TotalBonus(C, C.Class(p.selectedClass), meals);
                var lines = UIKit.E("tray-bonus", "tray-bonus--compact");
                for (int k = 0; k < total.Length; k++)
                {
                    if (Mathf.Abs(total[k]) < 0.001f) continue;
                    BuffStat bs = (BuffStat)k;
                    var line = Art.IconText(Art.StatIconId(bs), Art.StatEmoji(bs), UIKit.BuffText(new StatMod { stat = bs, value = total[k] }), "tray-bonus__line");
                    lines.Add(line);
                }
                lines.tooltip = "What your packed meals add (combos included)";
                _trayBuffs.Add(lines);
                // the element slot: one infusion, any number of guards
                var inf = Cooking.Infusion(C, meals);
                var guards = Cooking.Guards(C, meals);
                if (inf != Element.None || guards.Count > 0)
                {
                    var el = UIKit.E("tray-combos");
                    if (inf != Element.None)
                    {
                        var b = UIKit.L(Elements.Emoji(inf) + " Infused: " + Elements.Name(inf), "badge", "badge--synergy", "combo-badge");
                        b.tooltip = "Your attacks without an element of their own are " + Elements.Name(inf) + " this run.";
                        el.Add(b);
                    }
                    foreach (var g in guards)
                    {
                        var b = UIKit.L(Elements.Emoji(g) + " Guard: " + Elements.Name(g), "badge", "combo-badge");
                        b.tooltip = Elements.Name(g) + " attacks hit you for half this run.";
                        el.Add(b);
                    }
                    _trayBuffs.Add(el);
                }
            }
            if (syns.Count > 0)
            {
                var combos = UIKit.E("tray-combos");
                for (int i = 0; i < syns.Count; i++)
                {
                    var b = UIKit.L(syns[i].emoji + " " + syns[i].name, "badge", "badge--synergy", "combo-badge");
                    b.tooltip = syns[i].name + ": " + syns[i].description;
                    combos.Add(b);
                }
                _trayBuffs.Add(combos);
            }
            RefreshMealShelf();
        }

        /// The meal pantry: one card per cooked meal (grade, accelerator, what it gives this class).
        void RefreshMealShelf()
        {
            var p = S.Profile;
            var cls = C.Class(p.selectedClass);
            _mealShelf.Clear();
            int cap = Kitchen.PantrySlots(C, p);
            _mealShelfTitle.text = "🍽️ Meal pantry  " + p.mealPantry.Count + "/" + cap;
            // always-available meals (Camp Bread): a permanent card first, never runs out, doesn't use a slot
            foreach (var ar in Cooking.AlwaysMeals(C))
            {
                var card = UIKit.E("meal-card", "clickable", "meal-card--always", "meal-card--" + ar.station.ToString().ToLowerInvariant());
                card.Add(Art.Framed(Art.Pic(ArtLibrary.GetRecipe(ar.id), ar.emoji, "meal-card__emoji"), "meal-card__well"));
                var at = UIKit.E("meal-card__text");
                at.Add(UIKit.L(ar.name + "  ∞", "meal-card__name"));
                at.Add(UIKit.L(UIKit.MealSummary(Cooking.MealBuffs(C, cls, new EatenMeal { recipeId = ar.id, potency = 1f })), "meal-card__buffs"));
                at.Add(UIKit.L("Always available · free", "meal-card__accel"));
                card.Add(at);
                card.tooltip = ar.name + " — " + ar.description + "\nAlways available: never runs out and doesn't use a pantry slot. Click or drag onto the tray to pack it.";
                foreach (var child in card.Children()) SetIgnore(child);
                _recipeDrag.AddSource(card, "always:" + ar.id, ar.emoji, () => p.packed.Count < C.tuning.traySize);
                _mealShelf.Add(card);
            }
            for (int i = 0; i < p.mealPantry.Count; i++)
            {
                var m = p.mealPantry[i];
                var r = C.Recipe(m.recipeId);
                var acc = !string.IsNullOrEmpty(m.accelId) ? C.Ingredient(m.accelId) : null;
                var card = UIKit.E("meal-card", "clickable", "meal-card--grade-" + m.grade, "meal-card--" + (r != null ? r.station.ToString().ToLowerInvariant() : "pot"));
                UIKit.ElementTint(card, Cooking.SlotElement(C, m.accelId));   // element meals read at a glance
                card.Add(Art.Framed(Art.Pic(ArtLibrary.GetRecipe(m.recipeId), r != null ? r.emoji : "🍽️", "meal-card__emoji"), "meal-card__well"));
                var txt = UIKit.E("meal-card__text");
                var nameRow = UIKit.E("meal-card__name-row");
                nameRow.Add(UIKit.L(Cooking.GradeGem(m.grade) + " " + (r != null ? r.name : m.recipeId), "meal-card__name"));
                txt.Add(nameRow);
                string slotLine = acc != null ? UIKit.SlotLine(C, m.accelId) : "";
                string accTxt = acc != null ? acc.emoji + " " + acc.name + (slotLine.Length > 0 ? " · " + slotLine : "") : "";
                txt.Add(UIKit.L(UIKit.MealSummary(Cooking.MealBuffs(C, cls, Cooking.AsEaten(m))), "meal-card__buffs"));
                if (acc != null) txt.Add(UIKit.L("+ " + accTxt, "meal-card__accel"));
                card.Add(txt);
                int idx = i;
                var x = UIKit.B("×", () => S.ThrowOutMeal(idx), "slot__x", "meal-card__x");
                x.tooltip = "Throw out";
                card.Add(x);
                card.tooltip = (r != null ? r.name + " — " + r.description + "\n" : "") + Cooking.GradeName(m.grade) + " — stats ×" + UIKit.Num(Cooking.GradeMult(C.tuning, m.grade)) +
                    (acc != null ? "\nElement slot: " + accTxt : "") +
                    "\nClick or drag onto the tray to pack it";
                foreach (var child in card.Children()) if (!(child is Button)) SetIgnore(child);
                _recipeDrag.AddSource(card, i.ToString(), r != null ? r.emoji : "🍽️", () => p.packed.Count < C.tuning.traySize);
                _mealShelf.Add(card);
            }
            for (int i = p.mealPantry.Count; i < cap; i++)
            {
                var empty = UIKit.E("meal-card", "meal-card--empty");
                empty.Add(UIKit.L(i == p.mealPantry.Count ? "Cook a meal below" : "", "muted", "small"));
                _mealShelf.Add(empty);
            }
        }

        // ───── meal filters ─────
        VisualElement Chip(string text, bool on, System.Action click, string tip = null, string extra = null)
        {
            var b = UIKit.B(text, () => { click(); Refresh(); }, "filter-chip");
            b.EnableInClassList("filter-chip--on", on);
            if (!string.IsNullOrEmpty(extra)) b.AddToClassList(extra);
            if (tip != null) b.tooltip = tip;
            return b;
        }

        void RefreshFilters()
        {
            _filters.Clear();
            var cls = C.Class(S.Profile.selectedClass);

            var rowA = UIKit.E("meal-filters__row");
            rowA.Add(UIKit.L("Station", "meal-filters__label"));
            rowA.Add(Chip("All", _fStation < 0, () => _fStation = -1));
            for (int i = 0; i < Stations.Length; i++)
            {
                int k = i;
                rowA.Add(Chip(StationLabels[i], _fStation == i, () => _fStation = _fStation == k ? -1 : k, null,
                    "filter-chip--" + Stations[i].ToString().ToLowerInvariant()));
            }
            rowA.Add(UIKit.E("meal-filters__gap"));
            rowA.Add(UIKit.L("Show", "meal-filters__label"));
            rowA.Add(Chip("✅ Can cook now", _fCanCook, () => _fCanCook = !_fCanCook, "Only meals you have the ingredients for"));
            _filters.Add(rowA);

            var rowB = UIKit.E("meal-filters__row");
            rowB.Add(UIKit.L("Bonus", "meal-filters__label"));
            rowB.Add(Chip("Any", _fStat < 0, () => _fStat = -1));
            for (int i = 0; i < FilterStats.Length; i++)
            {
                int k = i;
                rowB.Add(Chip(Art.StatEmoji(FilterStats[i]) + " " + FilterStatNames[i], _fStat == i, () => _fStat = _fStat == k ? -1 : k));
            }
            bool any = _fStation >= 0 || _fStat >= 0 || _fCanCook;
            if (any)
            {
                rowB.Add(UIKit.E("meal-filters__gap"));
                rowB.Add(Chip("✖ Clear", false, () => { _fStation = -1; _fStat = -1; _fCanCook = false; }, "Show everything again", "filter-chip--clear"));
            }
            _filters.Add(rowB);
        }

        /// How much of a stat a meal gives this class (after its per-stat lens).
        float StatValue(RecipeData r, BuffStat st)
        {
            float v = 0f;
            foreach (var m in ClassBuffs(r)) if (m.stat == st) v += m.value;
            return v;
        }

        void RefreshRecipes()
        {
            var p = S.Profile;
            var cls = C.Class(p.selectedClass);
            _recipeGrid.Clear();
            int hidden = 0, total = 0, locked = 0;
            var shown = new List<RecipeData>();
            foreach (var r in C.recipes)
            {
                if (r.always) continue;   // always-available meals (Camp Bread) live beside the meal pantry, not on the menu
                if (!Routes.RecipeKnown(C, p, r)) { hidden++; continue; }   // its ingredients aren't found yet
                total++;
                if (!Kitchen.Unlocked(C, p, r)) locked++;
                string why;
                bool can = S.CanCook(r.id, out why);
                if (_fStation >= 0 && r.station != Stations[_fStation]) continue;
                if (_fStat >= 0 && StatValue(r, FilterStats[_fStat]) <= 0f) continue;
                if (_fCanCook && !can) continue;
                shown.Add(r);
            }
            // one column per station (Pot · BBQ · Sandwich), in menu order
            for (int st = 0; st < Stations.Length; st++)
            {
                if (_fStation >= 0 && _fStation != st) continue;
                var col = UIKit.E("recipe-col", "recipe-col--" + Stations[st].ToString().ToLowerInvariant());
                col.tooltip = "";
                int n = 0;
                foreach (var r in shown) if (r.station == Stations[st]) { col.Add(RecipeRow(r, p)); n++; }
                if (n == 0) col.Add(UIKit.L("—", "muted", "recipe-col__empty"));
                _recipeGrid.Add(col);
            }
            if (shown.Count == 0)
            {
                _recipeGrid.Clear();
                _recipeGrid.Add(UIKit.L(total == 0 ? "No meals known yet." : "No meals match these filters.", "muted", "recipe-grid__empty"));
            }
            _filterCount.text = "Showing " + shown.Count + " of " + total + " meals" + (locked > 0 ? "  ·  🔨 " + locked + " need a station upgrade" : "") + (hidden > 0 ? "  ·  🔒 " + hidden + " more to discover: find new ingredients in the dungeons" : "");
        }

        /// One list row: picture · name and bonus · ingredient counts stacked on the right.
        VisualElement RecipeRow(RecipeData r, Profile p)
        {
            string reason;
            bool can = S.CanCook(r.id, out reason);
            var row = UIKit.E("recipe-row", "clickable", "recipe-row--" + r.station.ToString().ToLowerInvariant());
            bool unlocked = Kitchen.Unlocked(C, p, r);
            row.EnableInClassList("recipe-row--ready", can);
            row.EnableInClassList("recipe-row--blocked", !can);
            row.EnableInClassList("recipe-row--locked", !unlocked);

            row.Add(Art.Framed(Art.Pic(ArtLibrary.GetRecipe(r.id), r.emoji, "recipe-row__emoji"), "recipe-row__well"));
            var mid = UIKit.E("recipe-row__mid");
            var cls = C.Class(p.selectedClass);
            var nameRow = UIKit.E("recipe-row__name-row");
            nameRow.Add(UIKit.L(r.name + (r.tier >= 2 ? "  ★2" : "") + (r.counters != Hazard.None ? "  " + Routes.HazardEmoji(r.counters) : ""), "recipe-row__name"));
            mid.Add(nameRow);
            mid.Add(UIKit.L(UIKit.MealSummary(ClassBuffs(r)), "recipe-row__buffs"));
            row.Add(mid);

            var costs = UIKit.E("recipe-row__costs");
            if (!unlocked) costs.Add(UIKit.L(Kitchen.LockText(r), "cost", "cost--short", "recipe-row__cost", "recipe-row__lock"));
            for (int k = 0; k < r.ingredients.Count; k++)
            {
                IngredientCost ic = r.ingredients[k];
                int have = Mathf.Max(0, Meals.Available(C, p, ic.ingredientId));
                bool known = Routes.IngredientKnown(C, p, ic.ingredientId);   // ticket 24: not found yet = ❓
                var cost = UIKit.L((known ? UIKit.IngEmoji(C, ic.ingredientId) : "❓") + " " + have + "/" + ic.count, "cost", "recipe-row__cost");
                cost.EnableInClassList("cost--short", have < ic.count);
                costs.Add(cost);
            }
            if (r.ingredients.Count == 0) costs.Add(UIKit.L("Free", "cost", "cost--free", "recipe-row__cost"));
            row.Add(costs);

            string id = r.id;
            row.RegisterCallback<ClickEvent>(e => Ui.OpenCook(id));   // open the stove: pick grades + accelerator, see the odds
            var ings = new List<string>();
            foreach (var ic in r.ingredients) ings.Add(ic.count + "× " + (Routes.IngredientKnown(C, p, ic.ingredientId) ? UIKit.IngName(C, ic.ingredientId) : "? (not found yet)"));
            string tip = (r.description ?? "") + (ings.Count > 0 ? "\nNeeds " + string.Join(", ", ings.ToArray()) : "");
            if (r.counters != Hazard.None) tip += "\n" + Routes.HazardEmoji(r.counters) + " guards against " + Routes.HazardName(r.counters).ToLower();
            if (!can && !string.IsNullOrEmpty(reason)) tip += "\n" + reason;
            row.tooltip = StationLabels[(int)r.station] + "\nClick to cook (choose ingredient grades and an accelerator)\n" + tip;
            foreach (var child in row.Children()) SetIgnore(child);
            return row;
        }
    }
}
