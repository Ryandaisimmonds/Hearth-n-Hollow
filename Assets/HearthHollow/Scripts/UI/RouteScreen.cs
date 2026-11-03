// Hearth & Hollow — the Route Board: its own page between camp and the Hollow.
// Left: the story banner (current chapter, key items) and a scrolling board — "The Ember Path" chapters, then
// side dungeons. Undiscovered routes show as "???" until the route before them is cleared.
// Right: the expedition panel — chosen route, what blocks it (key item / class), its hazard and which packed meals
// help, your cook with packed meals, and the button that enters.
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class RouteScreen : UIScreen
    {
        readonly Label _day, _coins, _objective;
        readonly VisualElement _keys, _board, _expedition;
        readonly ScrollView _scroll;
        readonly Button _go;
        string _selected;
        float _shownAt;

        public RouteScreen(GameUI ui) : base(ui, "routes")
        {
            // ───── header ─────
            var header = UIKit.E("header", "header--hearth");
            var brand = UIKit.E("header__brand");
            brand.Add(Art.Icon("map", "🗺️", "ico", "header__icon"));
            brand.Add(UIKit.L("Route Board", "header__title"));
            header.Add(brand);
            var info = UIKit.E("header__info");
            info.Add(Art.Chip("day", "☀️", out _day));
            info.Add(Art.Chip("coin", "🪙", out _coins, "chip--coins"));
            header.Add(info);
            Root.Add(header);

            var body = UIKit.E("routes__body");

            // ───── the board ─────
            var board = Panel("map", "🗺️", "Choose your route", "Story chapters follow the Ember Path. Side paths hold the key items it needs.", "panel--board");
            var story = UIKit.E("story-banner");
            _objective = UIKit.L("", "story-banner__objective");
            story.Add(_objective);
            _keys = UIKit.E("story-banner__keys");
            story.Add(_keys);
            board.Add(story);
            _scroll = new ScrollView(ScrollViewMode.Vertical);
            _scroll.AddToClassList("route-scroll");
            _board = UIKit.E("route-board-sections");
            _scroll.Add(_board);
            board.Add(_scroll);
            body.Add(board);

            // ───── the expedition panel ─────
            var side = Panel("tile_exit", "🌀", "Expedition", null, "panel--expedition");
            var expScroll = new ScrollView(ScrollViewMode.Vertical);
            expScroll.AddToClassList("expedition__scroll");
            _expedition = UIKit.E("expedition");
            expScroll.Add(_expedition);
            side.Add(expScroll);
            var footer = UIKit.E("expedition__footer");
            _go = UIKit.B("Enter ➜", Go, "btn--primary", "btn--setout", "expedition__go");
            footer.Add(_go);
            footer.Add(UIKit.B("← Back to camp  (Esc)", () => Ui.CloseRoutes(), "btn--small", "btn--ghost"));
            side.Add(footer);
            body.Add(side);
            Root.Add(body);
        }

        static VisualElement Panel(string iconId, string emoji, string title, string subtitle, string cls)
        {
            var p = UIKit.E("panel", cls);
            var head = UIKit.E("panel__title-row");
            var ribbon = UIKit.E("panel__title", "ribbon");
            ribbon.Add(Art.Icon(iconId, emoji, "ico", "panel__icon"));
            ribbon.Add(UIKit.L(title, "panel__title-text"));
            head.Add(ribbon);
            if (!string.IsNullOrEmpty(subtitle)) head.Add(UIKit.L(subtitle, "muted", "small", "panel__subtitle"));
            p.Add(head);
            return p;
        }

        protected override void OnShow() { _shownAt = UIKit.Now; }

        RouteMod ModFor(string routeId)
        {
            var h = S.Hearth;
            if (h == null) return RouteMod.None;
            var offer = h.routeOffers.Find(o => o.routeId == routeId);
            return offer != null ? offer.mod : RouteMod.None;
        }

        bool Discovered(RouteData r) { return Routes.Discovered(C, S.Profile, r); }

        // ───────────────────────── refresh ─────────────────────────
        public override void Refresh()
        {
            var p = S.Profile;
            _day.text = "Day " + p.day;
            _coins.text = p.coins + " coins";

            var sel = _selected != null ? C.Route(_selected) : null;
            if (sel == null || !Discovered(sel)) _selected = null;
            if (_selected == null)
            {
                // default: the current story chapter if it's open, else the deepest open route
                var ch = Routes.CurrentChapter(C, p);
                if (ch != null && S.IsRouteUnlocked(ch.id)) _selected = ch.id;
                else for (int i = 0; i < C.routes.Count; i++) if (S.IsRouteUnlocked(C.routes[i].id) && C.routes[i].chapter > 0) _selected = C.routes[i].id;
            }

            RefreshStory();
            RefreshBoard();
            RefreshExpedition();
        }

        void RefreshStory()
        {
            var p = S.Profile;
            _objective.text = "📖 " + Routes.Objective(C, p);
            _objective.tooltip = (C.storyTitle ?? "") + "\n" + (C.storyPrologue ?? "");
            _keys.Clear();
            if (C.keyItems == null || C.keyItems.Count == 0) return;
            _keys.Add(UIKit.L("Key items", "story-banner__keys-label"));
            foreach (var k in C.keyItems)
            {
                bool have = Routes.HasKey(p, k.id);
                var chip = UIKit.L(have ? k.emoji : "❔", "story-banner__key");
                chip.EnableInClassList("story-banner__key--have", have);
                var src = Routes.KeySource(C, k.id);
                chip.tooltip = have ? k.emoji + " " + k.name + "\n" + k.description
                                    : "Not found yet" + (src != null && Discovered(src) ? " — somewhere in " + src.emoji + " " + src.name : "");
                _keys.Add(chip);
            }
        }

        void RefreshBoard()
        {
            _board.Clear();
            var p = S.Profile;

            _board.Add(SectionHead("📖 " + (string.IsNullOrEmpty(C.storyTitle) ? "The story" : C.storyTitle) + " — the Ember Path"));
            var story = UIKit.E("route-board");
            foreach (var r in Routes.Chapters(C)) story.Add(Discovered(r) ? RouteCard(r) : HiddenCard(r));
            _board.Add(story);

            var sides = C.routes.FindAll(x => x.chapter <= 0);
            if (sides.Count > 0)
            {
                _board.Add(SectionHead("🧭 Side paths"));
                var sideRow = UIKit.E("route-board");
                foreach (var r in sides) sideRow.Add(Discovered(r) ? RouteCard(r) : HiddenCard(r));
                _board.Add(sideRow);
            }
        }

        static VisualElement SectionHead(string text) { return UIKit.L(text, "route-section__title"); }

        /// A route whose previous route isn't cleared yet: just a silhouette and a hint.
        VisualElement HiddenCard(RouteData r)
        {
            var card = UIKit.E("card", "route-card", "route-big", "route-card--locked", "route-big--hidden");
            var top = UIKit.E("route-big__top");
            top.Add(Art.Framed(Art.Icon("lock", "🔒", "route-big__emoji"), "route-big__well"));
            var names = UIKit.E("route-big__names");
            names.Add(UIKit.L(r.chapter > 0 ? "Chapter " + r.chapter + " · ???" : "??? · side path", "route-big__name"));
            names.Add(UIKit.L("Clear " + UIKit.RouteName(C, r.unlockedByRouteId) + " to find it", "route-big__kind"));
            top.Add(names);
            card.Add(top);
            return card;
        }

        VisualElement RouteCard(RouteData r)
        {
            var p = S.Profile;
            bool open = S.IsRouteUnlocked(r.id);
            string classBlock = Routes.ClassBlock(C, r, p.selectedClass);
            RouteMod mod = ModFor(r.id);
            bool cleared = p.clearedRouteIds.Contains(r.id);

            var card = UIKit.E("card", "route-card", "route-big", "clickable");
            card.EnableInClassList("route-card--locked", !open);
            card.EnableInClassList("route-card--selected", r.id == _selected);
            card.EnableInClassList("route-card--hunt", r.kind == RouteKind.Hunt);
            card.EnableInClassList("route-big--story", r.chapter > 0);

            // name row
            var top = UIKit.E("route-big__top");
            top.Add(Art.Framed(Art.Pic(ArtLibrary.GetRoute(r.id), r.emoji, "route-big__emoji"), "route-big__well"));
            var names = UIKit.E("route-big__names");
            names.Add(UIKit.L(r.name, "route-big__name"));
            string kind = r.kind == RouteKind.Hunt ? "Hunt · more monsters, more coin" : "Trail · more gathering spots";
            names.Add(UIKit.L((r.chapter > 0 ? "Chapter " + r.chapter + " · " : "") + kind, "route-big__kind"));
            top.Add(names);
            if (cleared) top.Add(UIKit.L("✓ cleared", "route-big__cleared"));
            card.Add(top);

            // badges: difficulty, class-only, hazard, key reward / requirement
            var badges = UIKit.E("route-big__badges");
            var pips = UIKit.E("pips");
            int diff = Mathf.Clamp(r.baseDifficulty + 1, 1, 8);
            for (int k = 0; k < 8; k++)
            {
                var pip = UIKit.E("pip");
                pip.EnableInClassList("pip--on", k < diff);
                pips.Add(pip);
            }
            pips.tooltip = "Difficulty " + diff + " / 8";
            badges.Add(pips);
            if (r.classLocked)
            {
                var cd = C.Class(r.requiredClass);
                var b = UIKit.L((cd != null ? cd.emoji + " " + cd.name : r.requiredClass.ToString()) + " only", "route-badge", "route-badge--class");
                b.EnableInClassList("route-badge--bad", classBlock != null);
                b.tooltip = "Only a " + (cd != null ? cd.name : r.requiredClass.ToString()) + " can enter. Switch class at camp or on the Skills page.";
                badges.Add(b);
            }
            if (r.hazard != Hazard.None)
            {
                var b = UIKit.L(Routes.HazardEmoji(r.hazard) + " " + Routes.HazardName(r.hazard), "route-badge", "route-badge--hazard");
                b.tooltip = Routes.HazardName(r.hazard) + " " + Routes.HazardEffect(r.hazard) + ". Pack " + Routes.CounterHint(r.hazard) + ": one halves it, two stop it.";
                badges.Add(b);
            }
            if (r.mutators != null)
                foreach (var m in r.mutators)
                {
                    if (m == Mutator.None) continue;
                    var b = UIKit.L(Routes.MutatorEmoji(m) + " " + Routes.MutatorName(m), "route-badge", "route-badge--mutator");
                    b.tooltip = Routes.MutatorText(C.tuning, m);
                    badges.Add(b);
                }
            if (!string.IsNullOrEmpty(r.rewardKeyItem))
            {
                var k = Routes.KeyItem(C, r.rewardKeyItem);
                bool have = Routes.HasKey(p, r.rewardKeyItem);
                var b = UIKit.L((have ? "✓ " : "Holds ") + (k != null ? k.emoji + " " + k.name : r.rewardKeyItem), "route-badge", "route-badge--key");
                b.tooltip = k != null ? k.description : "";
                badges.Add(b);
            }
            card.Add(badges);

            if (!string.IsNullOrEmpty(r.description)) card.Add(UIKit.L(r.description, "route-big__desc"));

            // monsters + forage on one compact row
            var row = UIKit.E("route-big__row", "route-big__contents");
            if (r.monsterIds != null)
                for (int k = 0; k < r.monsterIds.Count; k++)
                {
                    var md = C.Monster(r.monsterIds[k]);
                    var pic = Art.AnimFrame(ArtLibrary.GetMonster(r.monsterIds[k]), md != null && !string.IsNullOrEmpty(md.emoji) ? md.emoji : "👾", "route-big__monster-pic");
                    pic.tooltip = md != null ? md.name : r.monsterIds[k];
                    row.Add(pic);
                }
            if (r.gatherIngredientIds != null)
                for (int k = 0; k < r.gatherIngredientIds.Count; k++)
                {
                    string ing = r.gatherIngredientIds[k];
                    var chip = Art.Framed(Art.Pic(ArtLibrary.GetIngredient(ing), UIKit.IngEmoji(C, ing), "route-big__forage-pic"), "item-frame--sm", "route-big__forage-chip");
                    chip.tooltip = "Forage: " + UIKit.IngName(C, ing);
                    row.Add(chip);
                }
            if (r.materialIds != null)
                for (int k = 0; k < r.materialIds.Count; k++)
                {
                    string mat = r.materialIds[k];
                    var chip = Art.Framed(Art.Pic(ArtLibrary.GetIngredient(mat), UIKit.IngEmoji(C, mat), "route-big__forage-pic"), "item-frame--sm", "route-big__forage-chip", "route-big__material-chip");
                    chip.tooltip = "Building material (monsters drop it): " + UIKit.IngName(C, mat);
                    row.Add(chip);
                }
            card.Add(row);

            // today's modifier / why it's shut
            if (mod != RouteMod.None && open)
            {
                var modRow = UIKit.E("route-big__mod");
                modRow.Add(Art.ModBadge(mod));
                modRow.Add(UIKit.L(UIKit.ModHint(mod), "route-big__mod-hint"));
                card.Add(modRow);
            }
            string why = Routes.LockReason(C, p, r) ?? classBlock;
            if (why != null) card.Add(Art.IconText("lock", "🔒", why, "reason", "route-big__lock"));

            string id = r.id;
            card.RegisterCallback<ClickEvent>(e => { _selected = id; Refresh(); });
            return card;
        }

        void RefreshExpedition()
        {
            _expedition.Clear();
            var p = S.Profile;
            RouteData r = _selected != null ? C.Route(_selected) : null;
            var cls = C.Class(p.selectedClass);
            string lockWhy = r != null ? Routes.LockReason(C, p, r) : null;
            string classWhy = r != null ? Routes.ClassBlock(C, r, p.selectedClass) : null;

            // chosen route
            var routeBox = UIKit.E("expedition__section");
            if (r == null)
            {
                routeBox.Add(UIKit.L("Pick a route on the board.", "muted"));
            }
            else
            {
                var head = UIKit.E("expedition__route");
                head.Add(Art.Framed(Art.Pic(ArtLibrary.GetRoute(r.id), r.emoji, "expedition__route-pic"), "item-frame--lg", "expedition__route-well"));
                var names = UIKit.E("expedition__route-names");
                names.Add(UIKit.L(r.name, "expedition__route-name"));
                names.Add(UIKit.L((r.chapter > 0 ? "Chapter " + r.chapter + " · " : "Side path · ") + (r.kind == RouteKind.Hunt ? "Hunt" : "Trail"), "muted", "small"));
                RouteMod mod = ModFor(r.id);
                if (mod != RouteMod.None) names.Add(Art.ModBadge(mod));
                head.Add(names);
                routeBox.Add(head);
                if (!string.IsNullOrEmpty(r.storyIntro)) routeBox.Add(UIKit.L(r.storyIntro, "expedition__story"));

                if (lockWhy != null) routeBox.Add(Art.IconText("lock", "🔒", lockWhy, "small", "expedition__warn"));
                if (classWhy != null)
                {
                    var cr = UIKit.E("expedition__class-row");
                    cr.Add(Art.IconText("lock", "🔒", classWhy + ".", "small", "expedition__warn"));
                    var need = C.Class(r.requiredClass);
                    ClassId want = r.requiredClass;
                    cr.Add(UIKit.B("Play as " + (need != null ? need.name : want.ToString()), () => S.SelectClass(want), "btn--small", "btn--secondary", "expedition__switch"));
                    routeBox.Add(cr);
                }
                if (!string.IsNullOrEmpty(r.rewardKeyItem) && !Routes.HasKey(p, r.rewardKeyItem))
                {
                    var k = Routes.KeyItem(C, r.rewardKeyItem);
                    routeBox.Add(Art.IconText("trophy", "🗝️", "Clear it to find the " + Routes.KeyItemLabel(C, r.rewardKeyItem) + (k != null ? ": " + k.description : ""), "small", "expedition__note", "expedition__key"));
                }

                RouteMod m = ModFor(r.id);
                if (m != RouteMod.None) routeBox.Add(UIKit.L(UIKit.ModHint(m), "small", "expedition__note"));
                if (S.Hearth != null && S.Hearth.campEvent == CampEvent.Storm)
                    routeBox.Add(Art.IconText("event_storm", "⛈️", "Storm today — every route is Dangerous.", "small", "expedition__warn"));
                int floors = C.tuning.dungeonFloors;
                routeBox.Add(Art.IconText("trophy", "🏆", floors + " floors. Beat the guardian on floor " + floors + ": +" + r.clearReward + " coins, +" + r.clearXp + " XP (leaving early pays a share).", "small", "expedition__note"));
            }
            _expedition.Add(routeBox);

            // hazard + which packed meals help
            if (r != null && r.hazard != Hazard.None)
            {
                var hz = UIKit.E("expedition__section", "expedition__hazard");
                hz.Add(UIKit.L(Routes.HazardEmoji(r.hazard) + " " + Routes.HazardName(r.hazard) + " — " + Routes.HazardEffect(r.hazard) + " the whole way down", "subhead"));
                var eaten = Cooking.Packed(C, p);
                float n = Routes.CounterMeals(C, eaten, r.hazard);
                float mit = Routes.Mitigation(C, S.Profile, eaten, r.hazard);
                string state = mit >= 0.999f ? "Fully protected." :
                               mit > 0f ? "Partly protected: " + UIKit.Pct(mit) + " less. Pack another guard to stop it." :
                               "No guard packed: it'll wear you down (it can't take you below 1 HP, but monsters can).";
                hz.Add(UIKit.L(state, "small", mit >= 0.999f ? "expedition__ok" : "expedition__warn"));
                var names = new List<string>();
                foreach (var rec in Routes.CounterRecipes(C, r.hazard)) names.Add(rec.emoji + " " + rec.name);
                foreach (var ing in Routes.CounterAccelerators(C, r.hazard)) names.Add(ing.emoji + " " + ing.name + " in a meal's accelerator slot");
                hz.Add(UIKit.L("Helps: " + string.Join(", ", names.ToArray()) + " (one halves it, two stop it)", "small", "expedition__note"));
                _expedition.Add(hz);
            }

            // fun dungeon rules
            if (r != null && r.mutators != null && r.mutators.Exists(x => x != Mutator.None))
            {
                var mu = UIKit.E("expedition__section", "expedition__mutators");
                mu.Add(UIKit.L("🎲 Dungeon rules", "subhead"));
                foreach (var m in r.mutators)
                    if (m != Mutator.None) mu.Add(UIKit.L(Routes.MutatorEmoji(m) + " " + Routes.MutatorName(m) + " — " + Routes.MutatorText(C.tuning, m), "small", "expedition__mutator"));
                _expedition.Add(mu);
            }

            // your cook
            var cp = p.Progress(p.selectedClass);
            var st = S.Stats;
            var cookBox = UIKit.E("expedition__section");
            var who = UIKit.E("expedition__who");
            who.Add(Art.Framed(Art.AnimFrame(Art.ClassAnim(p.selectedClass), cls != null ? cls.emoji : "🧑‍🍳", "expedition__who-pic"), "expedition__who-frame"));
            var whoText = UIKit.E("expedition__who-text");
            whoText.Add(UIKit.L(cls != null ? cls.name : p.selectedClass.ToString(), "expedition__who-name"));
            var statRow = UIKit.E("expedition__stats");
            if (st != null)
            {
                statRow.Add(Art.IconText("hp", "❤️", st.maxHp + " HP", "expedition__stat"));
                statRow.Add(Art.IconText("damage", "🗡️", UIKit.Num(st.damage), "expedition__stat"));
                statRow.Add(Art.IconText("armour", "🛡️", UIKit.Pct(st.armourPct), "expedition__stat"));
                statRow.Add(Art.IconText("crit", "💥", UIKit.Pct(st.critChance), "expedition__stat"));
            }
            whoText.Add(statRow);
            whoText.Add(UIKit.L("with meals · bag " + Economy.BagSlots(C, p) + " slots", "muted", "small"));
            who.Add(whoText);
            cookBox.Add(who);
            _expedition.Add(cookBox);

            // meals
            var mealBox = UIKit.E("expedition__section");
            mealBox.Add(UIKit.L("Meals packed  " + p.tray.Count + "/" + C.tuning.traySize, "subhead"));
            if (p.tray.Count == 0)
            {
                mealBox.Add(UIKit.L("No meals packed — you'll head out on base stats.", "small", "expedition__warn"));
            }
            else
            {
                var packed = Cooking.Packed(C, p);
                for (int i = 0; i < p.tray.Count; i++)
                {
                    var rec = C.Recipe(p.tray[i]);
                    var pm = i < packed.Count ? packed[i] : null;
                    var row = UIKit.E("expedition__meal");
                    row.Add(Art.Framed(Art.Pic(ArtLibrary.GetRecipe(p.tray[i]), rec != null ? rec.emoji : "🍽️", "expedition__meal-pic"), "item-frame--sm"));
                    var acc = pm != null && !string.IsNullOrEmpty(pm.accelId) ? C.Ingredient(pm.accelId) : null;
                    string tag = acc != null && acc.accelGuard != Hazard.None ? "  " + Routes.HazardEmoji(acc.accelGuard) + "✓" : "";
                    row.Add(UIKit.L((pm != null ? Cooking.GradeGem(pm.grade) + " " : "") + (rec != null ? rec.name : p.tray[i]) + (acc != null ? " + " + acc.emoji : "") + tag, "expedition__meal-name"));
                    if (rec != null && pm != null) row.Add(UIKit.L(UIKit.BuffSummary(Cooking.MealBuffs(C, cls, pm)) + (Meals.IsFavourite(cls, rec) ? " ⭐" : ""), "expedition__meal-buffs"));
                    mealBox.Add(row);
                }
                var syns = Meals.ActiveSynergies(C, packed);
                for (int i = 0; i < syns.Count; i++)
                    mealBox.Add(UIKit.L(syns[i].emoji + " " + syns[i].name + " — " + syns[i].description, "badge", "badge--synergy", "combo-badge"));
                mealBox.Add(UIKit.L("Meals are eaten as you enter; their buffs last the whole run.", "muted", "small", "expedition__note"));
            }
            _expedition.Add(mealBox);

            bool can = r != null && lockWhy == null && classWhy == null;
            _go.SetEnabled(can);
            _go.text = r == null ? "Pick a route" : can ? "Enter " + r.name + " ➜" : "🔒 " + r.name;
        }

        void Go()
        {
            if (_selected != null && Routes.EnterBlock(C, S.Profile, _selected, S.Profile.selectedClass) == null) S.StartRun(_selected);
        }

        public override bool HandleKey(KeyCode key)
        {
            if (key == KeyCode.Escape) { Ui.CloseRoutes(); return true; }
            if (key == KeyCode.Return || key == KeyCode.KeypadEnter) { if (UIKit.Now - _shownAt > 0.5f) Go(); return true; }
            return false;
        }
    }
}
