// Hearth & Hollow — the camp.
// Left: three tabs — 🍳 Cooking (tray across the top, filters, then every meal in one colour-coded 3-column grid;
//       pantry in a pop-up), ⚒️ Upgrades (gear tracks, trinkets, key items), 🔨 Workshop (kitchen upgrades:
//       station levels + meal-pantry shelves) and ⭐ Skills (the current class's tree and loadout).
// Right: who's going (class, Change class, stats, skills, equipment), then Set out.
// Meals are pre-run buffs: the tray is eaten as you set out and its buffs last the whole run, so the stats
// preview "base → with meals". Rebuilt from session state on every Changed (cheap at this size).
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public enum CampTab { Cooking, Upgrades, Workshop, Skills }   // Upgrades = gear/trinkets (was "Workshop"); Workshop = kitchen upgrades

    public class HearthScreen : UIScreen
    {
        // header
        readonly Label _day, _coins;
        readonly Button _backToTown;   // shown when this screen is a building's menu in the town preview
        readonly Label _titleL;               // "The Hearth", or the station's name in town
        readonly VisualElement _tabBarEl, _rightEl;   // hidden in town: each station shows only its own page
        readonly VisualElement _eventChip, _eventIcon;
        readonly Label _eventText;
        // tabs
        readonly Button[] _tabButtons = new Button[4];
        readonly Label _skillsBadge;
        readonly VisualElement[] _tabPages = new VisualElement[4];
        CampTab _tab = CampTab.Cooking;
        // cooking
        readonly VisualElement _recipeGrid, _filters;
        readonly Label _filterCount;
        readonly VisualElement _tray, _trayBuffs;
        readonly Button _pantryBtn;
        readonly VisualElement _mealShelf;      // cooked meals waiting to be packed (cooking v9)
        readonly Label _mealShelfTitle;
        readonly VisualElement _kitchen;         // kitchen upgrades: station levels + pantry shelves (contentVersion 13)
        // cooking filters (kept while the game runs)
        static readonly BuffStat[] FilterStats = { BuffStat.MaxHp, BuffStat.DamagePct, BuffStat.Armour, BuffStat.CritPct, BuffStat.GatherZonePct, BuffStat.CoinPct, BuffStat.MoveSpeedPct };
        static readonly string[] FilterStatNames = { "HP", "Damage", "Armour", "Crit", "Gather", "Coins", "Speed" };
        int _fStation = -1;          // -1 = all stations, else index into Stations
        int _fStat = -1;             // -1 = any bonus, else index into FilterStats
        bool _fCanCook;
        // workshop
        readonly VisualElement _gear, _trinkets, _keyItems;
        readonly Label _gearTitle;
        // skills
        readonly SkillsTab _skills;
        // right column
        readonly SpriteAnimView _portrait;
        readonly Label _className, _classSub;
        readonly VisualElement _xpHolder, _stats, _loadout, _equip;
        readonly Button _setOut;
        readonly Label _setOutHint;
        // drag & drop
        readonly SlotDragger _trayDrag;          // reorder / discard inside the tray
        readonly DragToTargets _recipeDrag;      // cooked meal (meal pantry) → tray slot

        static readonly Station[] Stations = { Station.Pot, Station.BBQ, Station.Sandwich };
        static readonly string[] StationLabels = { "🍲 Pot", "🍖 BBQ", "🥪 Sandwich" };
        static readonly string[] TabNames = { "🍳 Cooking", "⚒️ Upgrades", "🔨 Workshop", "⭐ Skills" };

        float _shownAt;

        protected override void OnShow() { _shownAt = UIKit.Now; }

        public HearthScreen(GameUI ui) : base(ui, "hearth")
        {
            // ───── header ─────
            var header = UIKit.E("header", "header--hearth");
            var brand = UIKit.E("header__brand");
            brand.Add(Art.Icon("event_none", "🔥", "ico", "header__icon"));
            _titleL = UIKit.L("The Hearth", "header__title");
            brand.Add(_titleL);
            header.Add(brand);
            var info = UIKit.E("header__info");
            // Today's camp event: click to reopen its pop-up (the merchant's wares live there).
            _eventChip = UIKit.E("chip", "chip--event", "clickable");
            _eventIcon = UIKit.E("chip__icon-holder");
            _eventChip.Add(_eventIcon);
            _eventText = UIKit.L("", "chip__text");
            _eventChip.Add(_eventText);
            _eventChip.AddManipulator(new Clickable(() => Ui.OpenCampEvent()));
            info.Add(_eventChip);
            info.Add(Art.Chip("day", "☀️", out _day));
            info.Add(Art.Chip("coin", "🪙", out _coins, "chip--coins"));
            _backToTown = UIKit.B("🏘️ Back to town", () => Ui.CloseTownMenu(), "btn--secondary", "btn--small", "header__town");
            _backToTown.tooltip = "Back to walking around Hearth's Hollow (Esc)";
            info.Add(_backToTown);
            header.Add(info);
            Root.Add(header);

            var body = UIKit.E("hearth__body");
            Root.Add(body);

            // ═════ left: tabs ═════
            var left = UIKit.E("hearth__left");
            body.Add(left);
            var tabBar = UIKit.E("camp-tabs");
            for (int i = 0; i < TabNames.Length; i++)
            {
                CampTab t = (CampTab)i;
                var b = UIKit.B(TabNames[i], () => ShowTab(t), "camp-tab");
                b.tooltip = TabNames[i].Substring(TabNames[i].IndexOf(' ') + 1) + "  (" + (i + 1) + ")";
                _tabButtons[i] = b;
                tabBar.Add(b);
            }
            _skillsBadge = UIKit.L("", "camp-tab__badge");
            _skillsBadge.pickingMode = PickingMode.Ignore;
            _tabButtons[3].Add(_skillsBadge);
            left.Add(tabBar);
            _tabBarEl = tabBar;

            var pages = UIKit.E("panel", "camp-page");
            left.Add(pages);
            for (int i = 0; i < TabNames.Length; i++)
            {
                var sv = new ScrollView(ScrollViewMode.Vertical);
                sv.AddToClassList("camp-page__scroll");
                sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                _tabPages[i] = sv;
                pages.Add(sv);
            }

            // ── Cooking: tray across the top (pantry on the right), filters, then one grid of every meal ──
            var trayBox = UIKit.E("tray-bar");
            var trayTitle = UIKit.L("🧺 Tray", "cook-col__title", "cook-col__title--tray", "tray-bar__title");
            trayTitle.tooltip = "Eaten as you set out. Click or drag cooked meals in from the meal pantry · drag to reorder · × or right-click to take one off.";
            trayBox.Add(trayTitle);
            var trayRow = UIKit.E("tray-bar__row");
            _tray = UIKit.E("tray-bar__slots");
            trayRow.Add(_tray);
            _trayBuffs = UIKit.E("tray-sum", "tray-bar__sum");
            trayRow.Add(_trayBuffs);
            _pantryBtn = Art.IconButton("pantry", "🥕", "Larder", () => Ui.OpenPantry(), "btn--secondary", "tray-pantry-btn");
            _pantryBtn.tooltip = "Every ingredient you've gathered or bought, by grade";
            trayRow.Add(_pantryBtn);
            trayBox.Add(trayRow);
            _tabPages[0].Add(trayBox);

            // meal pantry: cooked meals (with their rolled grade) waiting to be packed
            var shelfBox = UIKit.E("meal-shelf");
            _mealShelfTitle = UIKit.L("", "cook-col__title", "meal-shelf__title");
            _mealShelfTitle.tooltip = "Meals you've cooked. Click or drag one onto the tray to pack it · × throws it out.";
            shelfBox.Add(_mealShelfTitle);
            _mealShelf = UIKit.E("meal-shelf__row");
            shelfBox.Add(_mealShelf);
            _tabPages[0].Add(shelfBox);

            // ── Workshop tab: kitchen upgrades — each station's level (more dishes, better-meal chance) and the meal pantry's shelves ──
            var kitchenBox = UIKit.E("kitchen");
            var kTitle = UIKit.L("🔨 Kitchen", "cook-col__title", "kitchen__title");
            kTitle.tooltip = "Upgrade with coins and building materials that monsters drop in the dungeons.";
            kitchenBox.Add(kTitle);
            _kitchen = UIKit.E("kitchen__row");
            kitchenBox.Add(_kitchen);
            _tabPages[2].Add(kitchenBox);   // the Workshop tab

            _filters = UIKit.E("meal-filters");
            _tabPages[0].Add(_filters);
            _filterCount = UIKit.L("", "small", "muted", "meal-filters__count");
            _tabPages[0].Add(_filterCount);
            _recipeGrid = UIKit.E("recipe-grid");   // three unnamed columns: Pot · BBQ · Sandwich
            _tabPages[0].Add(_recipeGrid);

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
            _recipeDrag.onClick = key =>   // click a meal to pack it
            {
                if (key.StartsWith("always:")) S.PackAlwaysMeal(key.Substring(7)); else S.PackMeal(int.Parse(key));
            };

            // ── Upgrades: gear + key items | trinkets ──
            var shopRow = UIKit.E("workshop");
            _tabPages[1].Add(shopRow);
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

            // ── Skills ──
            _skills = new SkillsTab(ui);
            _tabPages[3].Add(_skills.Root);

            // ═════ right: who's going (class, stats, skills, equipment), then set out ═════
            var right = UIKit.E("hearth__right");
            body.Add(right);
            _rightEl = right;

            var who = UIKit.E("panel", "panel--who");
            var whoRow = UIKit.E("who");
            _portrait = new SpriteAnimView(null, "", "who__portrait");
            whoRow.Add(Art.Framed(_portrait, "who__frame"));
            var whoText = UIKit.E("who__text");
            var nameRow = UIKit.E("who__name-row");
            _className = UIKit.L("", "who__name");
            nameRow.Add(_className);
            var change = UIKit.B("Change class", () => Ui.OpenClassSelect(), "btn--small", "btn--ghost", "who__change");
            nameRow.Add(change);
            whoText.Add(nameRow);
            _classSub = UIKit.L("", "who__sub");
            whoText.Add(_classSub);
            _xpHolder = UIKit.E("who__xp");
            whoText.Add(_xpHolder);
            whoRow.Add(whoText);
            who.Add(whoRow);

            who.Add(SectionHead("📊 Stats", null, null));
            _stats = UIKit.E("stats", "who__stats");
            who.Add(_stats);
            who.Add(SectionHead("⭐ Skills", "Edit", () => ShowTab(CampTab.Skills)));
            _loadout = UIKit.E("who__loadout");
            who.Add(_loadout);
            who.Add(SectionHead("⚒️ Equipment", "Upgrades", () => ShowTab(CampTab.Upgrades)));
            _equip = UIKit.E("who__equip");
            who.Add(_equip);
            right.Add(who);

            var go = UIKit.E("panel", "panel--go");
            _setOut = UIKit.B("Set out ➜", SetOut, "btn--primary", "btn--setout", "go__btn");
            _setOut.tooltip = "Choose a route on the Route Board (Enter)";
            go.Add(_setOut);
            _setOutHint = UIKit.L("", "setout-hint", "go__hint");
            go.Add(_setOutHint);
            right.Add(go);

            ApplyTab();
        }

        public void ShowTab(CampTab t) { _tab = t; ApplyTab(); if (t == CampTab.Skills) _skills.OnShow(); Refresh(); }
        public CampTab Tab { get { return _tab; } }

        void ApplyTab()
        {
            for (int i = 0; i < TabNames.Length; i++)
            {
                UIKit.Show(_tabPages[i], i == (int)_tab);
                _tabButtons[i].EnableInClassList("camp-tab--on", i == (int)_tab);
            }
        }

        string RecipeEmoji(string id)
        {
            var r = C.Recipe(id);
            return r != null ? r.emoji : "🍽️";
        }

        static void SetIgnore(VisualElement e)
        {
            e.pickingMode = PickingMode.Ignore;
            foreach (var c in e.Children()) SetIgnore(c);
        }

        /// What the selected class gets out of this meal (its per-stat lens), as a plain-quality helping.
        List<StatMod> ClassBuffs(RecipeData r) { return Cooking.MealBuffs(C, C.Class(S.Profile.selectedClass), new EatenMeal { recipeId = r.id, potency = 1f }); }

        // ───────────────────────── refresh ─────────────────────────
        public override void Refresh()
        {
            var p = S.Profile;
            _day.text = "Day " + p.day;
            _coins.text = p.coins + " coins";
            UIKit.Show(_backToTown, Ui.TownOpen);
            ApplyStationMode(Ui.TownOpen);
            UIKit.Show(_skillsBadge, false);   // no skill points any more (Design v2)

            RefreshEventChip();
            RefreshWho();
            RefreshGo();
            switch (_tab)
            {
                case CampTab.Cooking: RefreshTray(); RefreshFilters(); RefreshRecipes(); break;
                case CampTab.Upgrades: RefreshWorkshop(); break;   // gear, trinkets, key items
                case CampTab.Workshop: RefreshKitchen(); break;    // kitchen upgrades
                case CampTab.Skills: _skills.Refresh(); break;
            }
        }

        void RefreshEventChip()
        {
            var h = S.Hearth;
            CampEvent ev = h != null ? h.campEvent : CampEvent.None;
            _eventChip.EnableInClassList("chip--event-on", ev != CampEvent.None);
            _eventChip.EnableInClassList("chip--event-merchant", ev == CampEvent.Merchant);
            Art.Set(_eventIcon, Art.Icon(Art.EventIconId(ev), CampEventOverlay.Emoji(ev), "ico", "chip__icon"));
            _eventText.text = CampEventOverlay.ChipText(ev);
            _eventChip.tooltip = CampEventOverlay.Title(ev) + " — click to see today's event";
        }

        // ───── cooking ─────
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
                        txt.Add(UIKit.L(UIKit.BuffSummary(Cooking.MealBuffs(C, C.Class(p.selectedClass), em)) + (pot < 0.99f ? " (×" + UIKit.Num(pot) + " repeat)" : ""), "slot__sub"));
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
                at.Add(UIKit.L(UIKit.BuffSummary(Cooking.MealBuffs(C, cls, new EatenMeal { recipeId = ar.id, potency = 1f })), "meal-card__buffs"));
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
                txt.Add(UIKit.L(UIKit.BuffSummary(Cooking.MealBuffs(C, cls, Cooking.AsEaten(m))), "meal-card__buffs"));
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
            _filterCount.text = "Showing " + shown.Count + " of " + total + " meals" + (locked > 0 ? "  ·  🔨 " + locked + " need a station upgrade" : "") + (hidden > 0 ? "  ·  🔒 " + hidden + " more to discover (find their ingredients on new routes)" : "");
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
            mid.Add(UIKit.L(UIKit.BuffSummary(ClassBuffs(r)), "recipe-row__buffs"));
            row.Add(mid);

            var costs = UIKit.E("recipe-row__costs");
            if (!unlocked) costs.Add(UIKit.L(Kitchen.LockText(r), "cost", "cost--short", "recipe-row__cost", "recipe-row__lock"));
            for (int k = 0; k < r.ingredients.Count; k++)
            {
                IngredientCost ic = r.ingredients[k];
                int have = Mathf.Max(0, Meals.Available(C, p, ic.ingredientId));
                var cost = UIKit.L(UIKit.IngEmoji(C, ic.ingredientId) + " " + have + "/" + ic.count, "cost", "recipe-row__cost");
                cost.EnableInClassList("cost--short", have < ic.count);
                costs.Add(cost);
            }
            if (r.ingredients.Count == 0) costs.Add(UIKit.L("Free", "cost", "cost--free", "recipe-row__cost"));
            row.Add(costs);

            string id = r.id;
            row.RegisterCallback<ClickEvent>(e => Ui.OpenCook(id));   // open the stove: pick grades + accelerator, see the odds
            var ings = new List<string>();
            foreach (var ic in r.ingredients) ings.Add(ic.count + "× " + UIKit.IngName(C, ic.ingredientId));
            string tip = (r.description ?? "") + (ings.Count > 0 ? "\nNeeds " + string.Join(", ", ings.ToArray()) : "");
            if (r.counters != Hazard.None) tip += "\n" + Routes.HazardEmoji(r.counters) + " guards against " + Routes.HazardName(r.counters).ToLower();
            if (!can && !string.IsNullOrEmpty(reason)) tip += "\n" + reason;
            row.tooltip = StationLabels[(int)r.station] + "\nClick to cook (choose ingredient grades and an accelerator)\n" + tip;
            foreach (var child in row.Children()) SetIgnore(child);
            return row;
        }

        // ───── workshop ─────
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

        static string GearPrefix(GearSlot s) { return s == GearSlot.Boots ? "boots_" : s == GearSlot.Bag ? "bag_" : s == GearSlot.Lantern ? "lantern_" : s == GearSlot.Weapon ? "weapon_" : s == GearSlot.Body ? "armour_" : "head_"; }
        static string GearEmoji(GearSlot s) { return s == GearSlot.Boots ? "👢" : s == GearSlot.Bag ? "🎒" : s == GearSlot.Lantern ? "🏮" : s == GearSlot.Weapon ? "🗡️" : s == GearSlot.Body ? "🛡️" : "🪖"; }

        static VisualElement GearPic(string prefix, GearTier tier, string emoji)
        {
            Sprite s = tier != null ? ArtLibrary.GetGear(prefix + tier.tier) : null;
            if (s == null && prefix == "bag_") s = ArtLibrary.GetGear("backpack");   // one bag picture for every size
            return Art.Framed(Art.Pic(s, emoji, "ico", "shop-row__icon"), "item-frame--sm");
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
            int maxSlots = Slice.On ? slots : Mathf.Max(slots, C.tuning.trinketSlots + 1);   // the slice has no Cavern Hunt to open a third slot
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

        // ───── right column ─────
        static VisualElement SectionHead(string title, string linkText, System.Action link)
        {
            var h = UIKit.E("who__section");
            h.Add(UIKit.L(title, "who__section-title"));
            if (link != null) h.Add(UIKit.B(linkText + " ➜", link, "btn--small", "btn--ghost", "who__section-link"));
            return h;
        }

        void RefreshWho()
        {
            var p = S.Profile;
            var cls = C.Class(p.selectedClass);
            var cp = p.Progress(p.selectedClass);
            var t = C.tuning;
            _portrait.SetAnim(Art.ClassAnim(p.selectedClass), cls != null ? cls.emoji : "🧑‍🍳");
            _className.text = cls != null ? cls.name : p.selectedClass.ToString();
            _classSub.text = (cls != null && !string.IsNullOrEmpty(cls.role) ? cls.role : "")
                + (cls != null && UIKit.LensText(cls).Length > 0 ? " · " + UIKit.LensText(cls) : "");
            _xpHolder.Clear();
            RefreshStats();
            RefreshLoadout();
            RefreshEquipment();
        }

        void RefreshStats()
        {
            _stats.Clear();
            var b = S.BaseStats;
            var st = S.Stats;   // includes the packed tray
            var row = UIKit.E("stat-row");
            row.Add(Stat("hp", "❤️", "Max HP", b.maxHp.ToString(), st.maxHp.ToString(), st.maxHp > b.maxHp));
            row.Add(Stat("damage", "🗡️", "Damage", UIKit.Num(b.damage), UIKit.Num(st.damage), st.damage > b.damage + 0.01f));
            row.Add(Stat("armour", "🛡️", "Armour", UIKit.Pct(b.armourPct), UIKit.Pct(st.armourPct), st.armourPct > b.armourPct + 0.001f));
            row.Add(Stat("crit", "💥", "Crit", UIKit.Pct(b.critChance), UIKit.Pct(st.critChance), st.critChance > b.critChance + 0.001f));
            _stats.Add(row);

            var extras = UIKit.E("stats__extras");
            var mods = S.Mods;
            if (mods.moveSpeed > 1.001f) extras.Add(Art.IconText("action_flee", "👢", "speed +" + UIKit.Pct(mods.moveSpeed - 1f), "small"));
            if (st.gatherZoneMult > 1.001f) extras.Add(Art.IconText("gather", "🧺", "gather zone +" + UIKit.Pct(st.gatherZoneMult - 1f), "small"));
            if (st.coinMult > 1.001f) extras.Add(Art.IconText("coinpct", "🪙", "coins +" + UIKit.Pct(st.coinMult - 1f), "small"));
            if (st.xpMult > 1.001f) extras.Add(Art.IconText("xp", "✨", "XP +" + UIKit.Pct(st.xpMult - 1f), "small"));
            extras.Add(UIKit.L("base → with your packed meals", "small", "muted", "who__stats-note"));
            _stats.Add(extras);
        }

        static VisualElement Stat(string iconId, string emoji, string label, string baseValue, string withMeals, bool boosted)
        {
            var s = UIKit.E("stat");
            s.Add(Art.IconText(iconId, emoji, boosted ? baseValue + " → " + withMeals : withMeals, "stat__value"));
            s.EnableInClassList("stat--boosted", boosted);
            s.Add(UIKit.L(label, "stat__label"));
            return s;
        }

        /// The four loadout slots as big cards (click → Skills tab).
        void RefreshLoadout()
        {
            _loadout.Clear();
            var p = S.Profile;
            var load = S.Loadout(p.selectedClass);
            var mods = S.Mods;
            string[] keys = { "J", "K", "L", "Space" };
            string[] slots = { "Attack", "Skill", "Skill", "Dash" };
            for (int i = 0; i < 4; i++)
            {
                var m = i < load.Length ? load[i] : null;
                var card = UIKit.E("who-move", "clickable");
                card.EnableInClassList("who-move--empty", m == null);
                card.Add(UIKit.L(keys[i], "who-move__key"));
                card.Add(UIKit.L(m != null ? m.emoji : "＋", "who-move__emoji"));
                var txt = UIKit.E("who-move__text");
                txt.Add(UIKit.L(m != null ? m.name : "Empty", "who-move__name"));
                string sub = slots[i];
                if (m != null && m.slot != MoveSlot.Attack)
                {
                    float cd = m.slot == MoveSlot.Skill ? m.cooldown * mods.skillCooldown : m.cooldown * mods.dashCooldown;
                    sub += " · " + cd.ToString("0.#") + "s";
                }
                if (m != null && m.owner != p.selectedClass && !m.anyClass) sub += " · from " + m.owner;
                txt.Add(UIKit.L(sub, "who-move__sub"));
                card.Add(txt);
                if (m != null) card.tooltip = m.name + "\n" + m.description;
                card.RegisterCallback<ClickEvent>(e => ShowTab(CampTab.Skills));
                foreach (var ch in card.Children()) SetIgnore(ch);
                _loadout.Add(card);
            }
            // the chosen Ultimate (C / I)
            var um = S.UltimateMove(p.selectedClass);
            if (um != null)
            {
                var card = UIKit.E("who-move", "who-move--ult", "clickable");
                card.Add(UIKit.L("C / I", "who-move__key"));
                card.Add(UIKit.L(um.emoji, "who-move__emoji"));
                var txt = UIKit.E("who-move__text");
                txt.Add(UIKit.L(um.name, "who-move__name"));
                txt.Add(UIKit.L("Ultimate · " + um.cooldown.ToString("0.#") + "s", "who-move__sub"));
                card.Add(txt);
                card.tooltip = um.name + "\n" + um.description + "\nEvery hit you land charges it faster.";
                card.RegisterCallback<ClickEvent>(e => ShowTab(CampTab.Skills));
                foreach (var ch in card.Children()) SetIgnore(ch);
                _loadout.Add(card);
            }
        }

        /// Current gear (weapon, body, head, boots, bag) and worn trinkets (click → Upgrades tab).
        void RefreshEquipment()
        {
            _equip.Clear();
            var p = S.Profile;
            var grid = UIKit.E("who-gear-grid");
            foreach (var gs in Gear.Shop) grid.Add(GearChip(gs, GearPrefix(gs), GearEmoji(gs)));
            _equip.Add(grid);

            var tr = UIKit.E("who-trinkets");
            int slots = S.TrinketSlots;
            var worn = p.equippedTrinkets ?? new List<string>();
            for (int i = 0; i < slots; i++)
            {
                var t = i < worn.Count ? Gear.Trinket(C, worn[i]) : null;
                var chip = UIKit.E("who-trinket", "clickable");
                chip.EnableInClassList("who-trinket--empty", t == null);
                chip.Add(UIKit.L(t != null ? t.emoji : "💍", "who-trinket__emoji"));
                var txt = UIKit.E("who-trinket__text");
                txt.Add(UIKit.L(t != null ? t.name : "Empty trinket slot", "who-trinket__name"));
                if (t != null) txt.Add(UIKit.L(Gear.EffectText(C, t), "who-trinket__sub"));
                chip.Add(txt);
                chip.RegisterCallback<ClickEvent>(e => ShowTab(CampTab.Upgrades));
                foreach (var ch in chip.Children()) SetIgnore(ch);
                tr.Add(chip);
            }
            _equip.Add(tr);
        }

        VisualElement GearChip(GearSlot slot, string prefix, string emoji)
        {
            var p = S.Profile;
            GearTier cur = Gear.Current(C, p, slot, p.selectedClass);
            var chip = UIKit.E("who-gear", "clickable");
            chip.Add(GearPic(prefix, cur, emoji));
            var txt = UIKit.E("who-gear__text");
            txt.Add(UIKit.L(Gear.SlotName(slot), "who-gear__slot"));
            txt.Add(UIKit.L(cur != null ? cur.name : "none", "who-gear__name"));
            if (cur != null) txt.Add(UIKit.L(Gear.ValueText(slot, cur), "who-gear__value"));
            chip.Add(txt);
            var next = Gear.Next(C, p, slot, p.selectedClass);
            chip.tooltip = next != null ? "Next: " + next.name + " — " + Gear.ValueText(slot, next) + " (" + S.GearPrice(next) + " coins in Upgrades)" : "Maxed";
            chip.RegisterCallback<ClickEvent>(e => ShowTab(CampTab.Upgrades));
            foreach (var ch in chip.Children()) SetIgnore(ch);
            return chip;
        }

        void RefreshGo()
        {
            int packed = S.Profile.tray.Count;
            _setOutHint.text = packed > 0 ? "🧺 " + packed + "/" + C.tuning.traySize + " meals packed" : "No meals packed — base stats only";
            _setOutHint.EnableInClassList("setout-hint--warn", packed == 0);
        }

        void SetOut() { Ui.OpenRoutes(); }

        /// Town: this screen is one station's window over the town — just that page (no tabs, no class column, no Set out:
        /// runs start at the Route board by the gate). At the camp screen everything is as before.
        void ApplyStationMode(bool on)
        {
            Root.EnableInClassList("hearth--station", on);
            UIKit.Show(_tabBarEl, !on);
            UIKit.Show(_rightEl, !on);
            _titleL.text = on ? StationTitle(_tab) : "The Hearth";
            _backToTown.text = on ? "✕ Close" : "🏘️ Back to town";
            if (on)
            {
                Root.style.backgroundColor = new Color(0.02f, 0.015f, 0.01f, 0.55f);   // the town, dimmed, behind the window
                Root.style.paddingLeft = Length.Percent(8); Root.style.paddingRight = Length.Percent(8);
                Root.style.paddingTop = Length.Percent(4); Root.style.paddingBottom = Length.Percent(4);
            }
            else
            {
                Root.style.backgroundColor = StyleKeyword.Null;
                Root.style.paddingLeft = StyleKeyword.Null; Root.style.paddingRight = StyleKeyword.Null;
                Root.style.paddingTop = StyleKeyword.Null; Root.style.paddingBottom = StyleKeyword.Null;
            }
        }

        static string StationTitle(CampTab t)
        {
            return t == CampTab.Cooking ? "🍳 Cooking area" : t == CampTab.Upgrades ? "⚒️ Smithy" : t == CampTab.Workshop ? "🔨 Workshop" : "⭐ Training yard";
        }

        public override bool HandleKey(KeyCode key)
        {
            if (Ui.TownOpen) return false;   // in town: no tab hotkeys, and no Enter-to-set-out (that's the Route board's job)
            if (key == KeyCode.Return || key == KeyCode.KeypadEnter) { if (UIKit.Now - _shownAt > 0.8f) SetOut(); return true; }
            if (key == KeyCode.Alpha1 || key == KeyCode.Keypad1) { ShowTab(CampTab.Cooking); return true; }
            if (key == KeyCode.Alpha2 || key == KeyCode.Keypad2) { ShowTab(CampTab.Upgrades); return true; }
            if (key == KeyCode.Alpha3 || key == KeyCode.Keypad3) { ShowTab(CampTab.Workshop); return true; }
            if (key == KeyCode.Alpha4 || key == KeyCode.Keypad4) { ShowTab(CampTab.Skills); return true; }
            return false;
        }
    }
}
