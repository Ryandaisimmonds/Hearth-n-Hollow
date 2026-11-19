// Hearth & Hollow — the Effects Lab's panel: a column down the right of the screen (the game has the rest).
// At the top: what's going on and the damage meter. Then two tabs:
//   Play  — time (pause, one frame, speed), auto-cast, targets, view (light, zoom), cheats, and every move as a row
//           you can click (favourites first, then a filter: the lab's moves, each class, any class).
//   Setup — arena or a real floor, area and floor, class and loadout, skill levels, gear, trinkets, meals.
// Inline styles throughout: the lab doesn't touch the game's style sheets.
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Dungeon;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.Lab
{
    public partial class EffectsLab
    {
        VisualElement _panel, _playView, _setupView, _playBody, _setupBody, _skillsBody;
        Button _playTab, _setupTab, _pauseBtn;
        Label _status, _meter, _castLine, _speedLabel, _autoLabel, _intervalLabel, _zoomLabel;
        Slider _speedSlider;
        Toggle _autoToggle;
        readonly List<Button> _lightBtns = new List<Button>();
        int _mealRecipe, _mealGrade = Cooking.Grades - 1, _mealElement;

        static readonly Color Gold = new Color(1f, 0.78f, 0.45f), Dim = new Color(1f, 1f, 1f, 0.55f), Cream = new Color(1f, 0.93f, 0.8f);
        static readonly Color BtnOn = new Color(0.62f, 0.42f, 0.2f), BtnOff = new Color(0.25f, 0.18f, 0.12f), BtnNormal = new Color(0.42f, 0.27f, 0.14f);

        void BuildPanel()
        {
            _panel = new VisualElement { name = "effects-lab" };
            _panel.AddToClassList("panel");   // DungeonInput.PointerOverUI: clicks here never swing
            var ps = _panel.style;
            ps.position = Position.Absolute; ps.right = 0; ps.top = 0; ps.bottom = 0; ps.width = Length.Percent(PanelShare * 100f);
            ps.marginLeft = ps.marginRight = ps.marginTop = ps.marginBottom = 0;
            ps.backgroundImage = new StyleBackground(StyleKeyword.None);
            ps.backgroundColor = new Color(0.07f, 0.055f, 0.045f, 1f);
            ps.paddingLeft = ps.paddingRight = 12; ps.paddingTop = 10; ps.paddingBottom = 8;
            ps.borderLeftWidth = 2; ps.borderLeftColor = new Color(1f, 0.75f, 0.4f, 0.35f);
            _panel.pickingMode = PickingMode.Position;

            _panel.Add(Text("🔥 Effects Lab", 18, new Color(1f, 0.85f, 0.55f)));
            _status = Text("", 12, Color.white); _status.style.marginTop = 2;
            _panel.Add(_status);

            // the meter: always in view
            var card = new VisualElement();
            var cs = card.style;
            cs.marginTop = 6; cs.paddingLeft = cs.paddingRight = 8; cs.paddingTop = cs.paddingBottom = 6;
            cs.backgroundColor = new Color(0.14f, 0.1f, 0.07f);
            cs.borderTopLeftRadius = cs.borderTopRightRadius = cs.borderBottomLeftRadius = cs.borderBottomRightRadius = 6;
            _castLine = Text("", 13, new Color(1f, 0.9f, 0.55f));
            _meter = Text("", 12, Cream);
            var meterRow = Row(_meter, Btn("Reset", ResetMeter, 60));
            _meter.style.flexGrow = 1; _meter.style.flexShrink = 1;
            card.Add(_castLine); card.Add(meterRow);
            _panel.Add(card);

            var tabs = Row(_playTab = Btn("▶ Play", () => ShowTab(true)), _setupTab = Btn("⚙ Setup", () => ShowTab(false)));
            tabs.style.marginTop = 8;
            _panel.Add(tabs);

            _playView = Scroll(out _playBody);
            _setupView = Scroll(out _setupBody);
            _panel.Add(_playView); _panel.Add(_setupView);
            BuildPlay();
            BuildSetup();
            ShowTab(true);

            _panel.Add(Text("P pause · . frame · T auto · − = speed · R ring · F1 panel", 11, Dim));
            foreach (var child in _panel.Children()) if (child != _playView && child != _setupView) child.style.flexShrink = 0;   // only the lists scroll
            _ui.Root.Add(_panel);
            _panel.schedule.Execute(Refresh).Every(200);
        }

        VisualElement Scroll(out VisualElement body)
        {
            var s = new ScrollView(ScrollViewMode.Vertical);
            s.style.flexGrow = 1; s.style.flexShrink = 1; s.style.marginTop = 4;
            s.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            body = s.contentContainer;
            return s;
        }

        void ShowTab(bool play)
        {
            _playView.style.display = play ? DisplayStyle.Flex : DisplayStyle.None;
            _setupView.style.display = play ? DisplayStyle.None : DisplayStyle.Flex;
            _playTab.style.backgroundColor = play ? BtnOn : BtnOff;
            _setupTab.style.backgroundColor = play ? BtnOff : BtnOn;
        }

        void Refresh()
        {
            if (_status == null) return;
            var run = S.Run;
            var r = S.Content.Route(_p.area);
            int alive = 0;
            foreach (var e in FindObjectsByType<EnemyController>()) if (e != null && !e.Dead && !e.Dummy) alive++;
            _status.text = (r != null ? r.emoji + " " + r.name : _p.area) + " · floor " + (run != null ? run.floor : _p.floor) + (_p.arena ? " · arena" : "") +
                           "  ·  " + S.Profile.selectedClass + "  ·  monsters " + alive +
                           (run != null && S.Stats != null ? "  ·  HP " + run.hp + "/" + S.Stats.maxHp : "");
            _meter.text = MeterText();
            _castLine.text = CastText();
            if (_speedLabel != null) _speedLabel.text = (_paused ? "paused · " : "") + "×" + _p.speed.ToString("0.00");
        }

        // ───────────────────────── Play ─────────────────────────
        void BuildPlay()
        {
            var b = _playBody;
            b.Clear();
            var C = S.Content;

            Section(b, "Time");
            _pauseBtn = Btn(_paused ? "▶ Play (P)" : "⏸ Pause (P)", () => SetPaused(!_paused));
            _speedLabel = Text("", 13, Cream); _speedLabel.style.unityTextAlign = TextAnchor.MiddleRight; _speedLabel.style.width = 90;
            b.Add(Row(_pauseBtn, Btn("⏭ Frame (.)", StepFrame), _speedLabel));
            _speedSlider = Slide(0.05f, 2f, _p.speed, SetSpeed);
            b.Add(_speedSlider);
            b.Add(Row(Btn("×0.1", () => SetSpeed(0.1f)), Btn("×0.25", () => SetSpeed(0.25f)), Btn("×0.5", () => SetSpeed(0.5f)), Btn("×1", () => SetSpeed(1f)), Btn("×2", () => SetSpeed(2f))));

            Section(b, "Auto-cast");
            _autoToggle = Tog("Repeat (T)", _p.autoOn, SetAuto);
            _autoToggle.style.flexGrow = 0; _autoToggle.style.width = 110;
            _autoLabel = Text("", 12, Cream); _autoLabel.style.flexGrow = 1;
            b.Add(Row(_autoToggle, _autoLabel));
            _intervalLabel = Text("", 12, Dim);
            b.Add(_intervalLabel);
            b.Add(Slide(0.25f, 5f, _p.autoInterval, v => { _p.autoInterval = v; RefreshAutoLabel(); Changed(); }));
            b.Add(Tog("Aim at the nearest target", _p.aimNearest, v => { _p.aimNearest = v; Changed(); }));
            RefreshAutoLabel();

            Section(b, "Targets");
            var mons = C.monsters;
            var monNames = mons.ConvertAll(m => m.emoji + " " + m.name); monNames.Insert(0, "(no pack)");
            b.Add(Drop("Pack", monNames, mons.FindIndex(m => m.id == _p.packMonster) + 1, i => { _p.packMonster = i == 0 ? "" : mons[i - 1].id; if (_p.packCount == 0 && i > 0) _p.packCount = 3; BuildPlay(); Changed(); }));
            b.Add(Drop("How many", new List<string> { "0", "1", "2", "3", "4", "5", "6" }, _p.packCount, i => { _p.packCount = i; Changed(); }));
            b.Add(Drop("Dummies", new List<string> { "0", "1", "2", "3" }, _p.dummies, i => { _p.dummies = i; Changed(); }));
            b.Add(Tog("Freeze monsters (they stand still, no attacks)", _p.freeze, v => { _p.freeze = v; EnemyController.LabFrozen = v; Changed(); }));
            b.Add(Row(Btn("💀 Kill all", KillAll), Btn("👑 Floor boss", FloorBoss), Btn("🌪️ Swirl boss", SpawnSwirlBoss)));
            b.Add(Row(Tog("Bosses wear the swirl", _p.bossSwirl, v => { _p.bossSwirl = v; Changed(); }), Btn("Swirl all on/off", ToggleSwirlAll)));

            Section(b, "View");
            _lightBtns.Clear();
            string[] lights = { "Area's light", "Bright", "Dark" };
            var lightRow = new VisualElement(); lightRow.style.flexDirection = FlexDirection.Row;
            for (int i = 0; i < lights.Length; i++)
            {
                int k = i;
                var lb = Btn(lights[i], () => { _p.light = k; ApplyLight(); PaintLights(); Changed(); });
                _lightBtns.Add(lb); lightRow.Add(lb);
            }
            b.Add(lightRow); PaintLights();
            _zoomLabel = Text("", 12, Dim);
            b.Add(_zoomLabel);
            b.Add(Slide(0.4f, 1.4f, _p.zoom, v => { SetZoom(v); _zoomLabel.text = "Zoom ×" + (1f / _p.zoom).ToString("0.00") + "  (left: closer)"; }));
            _zoomLabel.text = "Zoom ×" + (1f / _p.zoom).ToString("0.00") + "  (left: closer)";

            Section(b, "Cheats");
            var cheats = new VisualElement(); cheats.style.flexDirection = FlexDirection.Row; cheats.style.flexWrap = Wrap.Wrap;
            cheats.Add(Tog("Can't die", _p.cantDie, v => { _p.cantDie = v; S.DevNoDeath = v; Changed(); }));
            cheats.Add(Tog("Auto-heal", _p.autoHeal, v => { _p.autoHeal = v; Changed(); }));
            cheats.Add(Tog("No cooldowns", _p.noCooldowns, v => { _p.noCooldowns = v; Changed(); }));
            b.Add(cheats);

            Section(b, "Skills");
            b.Add(Text("Click to use now · ⭐ pin to the top · 🔁 repeat it (auto-cast)", 11, Dim));
            _skillsBody = new VisualElement();
            b.Add(_skillsBody);
            BuildSkillList();
        }

        void PaintLights() { for (int i = 0; i < _lightBtns.Count; i++) _lightBtns[i].style.backgroundColor = i == _p.light ? BtnOn : BtnOff; }

        void RefreshAutoLabel()
        {
            if (_autoLabel == null) return;
            var m = S.Content.Move(_p.autoMove);
            int lv = m != null ? _p.Level(_p.lastLevels, m.id) : 0;
            _autoLabel.text = m == null ? "(pick a move's 🔁 below)" : "🔁 " + m.emoji + " " + m.name + (lv > 0 ? " Lv " + lv : "");
            _intervalLabel.text = "Every " + _p.autoInterval.ToString("0.00") + " s of game time";
        }

        // the move list: favourites, then the chosen filter
        void BuildSkillList()
        {
            var b = _skillsBody;
            if (b == null) return;
            b.Clear();
            var C = S.Content;

            var favs = _p.favourites.ConvertAll(id => C.Move(id)).FindAll(m => m != null);
            if (favs.Count > 0)
            {
                b.Add(Small("⭐ Favourites"));
                foreach (var m in favs) b.Add(MoveRow(m));
            }

            // filter chips
            var chips = new VisualElement(); chips.style.flexDirection = FlexDirection.Row; chips.style.flexWrap = Wrap.Wrap; chips.style.marginTop = 6;
            var filters = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("lab", "🧪 Lab") };
            foreach (var cd in C.classes) filters.Add(new KeyValuePair<string, string>(cd.id.ToString(), cd.emoji + " " + cd.name));
            filters.Add(new KeyValuePair<string, string>("any", "Any class"));
            foreach (var f in filters)
            {
                var key = f.Key;
                var chip = Btn(f.Value, () => { _p.filter = key; Changed(); BuildSkillList(); });
                chip.style.flexGrow = 0; chip.style.height = 22; chip.style.fontSize = 11; chip.style.marginBottom = 3;
                chip.style.paddingLeft = chip.style.paddingRight = 8;
                chip.style.backgroundColor = _p.filter == key ? BtnOn : BtnOff;
                chips.Add(chip);
            }
            b.Add(chips);

            List<MoveData> list;
            ClassId cls;
            if (_p.filter == "lab") list = C.moves.FindAll(m => m.id.StartsWith("lab_"));
            else if (_p.filter == "any") list = C.moves.FindAll(m => m.anyClass && !m.id.StartsWith("lab_"));
            else if (System.Enum.TryParse(_p.filter, out cls)) list = C.moves.FindAll(m => !m.anyClass && m.owner == cls);
            else list = new List<MoveData>();
            list.Sort((x, y) => x.slot != y.slot ? x.slot.CompareTo(y.slot) : string.CompareOrdinal(x.name, y.name));
            foreach (var m in list) b.Add(MoveRow(m));
            if (list.Count == 0) b.Add(Text("(nothing here)", 12, Dim));
        }

        /// One move: ⭐ · name · Lv 1/2/3 (lab moves) or Use · 🔁.
        VisualElement MoveRow(MoveData m)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginTop = 2;
            bool fav = _p.favourites.Contains(m.id);
            var star = Btn(fav ? "⭐" : "☆", () => { if (fav) _p.favourites.Remove(m.id); else _p.favourites.Add(m.id); Changed(); BuildSkillList(); }, 30);
            star.style.backgroundColor = BtnOff;
            row.Add(star);
            var name = Text(m.emoji + " " + m.name + "  · " + SlotTag(m.slot), 12, Color.white);
            name.style.flexGrow = 1; name.style.flexShrink = 1; name.style.marginLeft = 4;
            name.style.whiteSpace = WhiteSpace.NoWrap; name.style.overflow = Overflow.Hidden; name.style.textOverflow = TextOverflow.Ellipsis;
            name.tooltip = m.description; name.pickingMode = PickingMode.Position;
            row.Add(name);
            int last = _p.Level(_p.lastLevels, m.id);
            if (m.id.StartsWith("lab_"))
                for (int lv = 1; lv <= GameSession.MaxMoveLevel; lv++)
                {
                    int level = lv;
                    var lb = Btn("Lv" + lv, () => { Cast(m, level); BuildSkillList(); RefreshAutoLabel(); }, 42);
                    if (last == lv) lb.style.backgroundColor = BtnOn;
                    row.Add(lb);
                }
            else row.Add(Btn("Use", () => Cast(m, 0), 52));
            bool auto = _p.autoOn && _p.autoMove == m.id;
            var loop = Btn("🔁", () => ToggleAutoMove(m), 34);
            loop.style.backgroundColor = auto ? new Color(0.2f, 0.55f, 0.3f) : BtnOff;
            loop.tooltip = "Repeat this move every few seconds (auto-cast)";
            row.Add(loop);
            return row;
        }

        static string SlotTag(MoveSlot s)
        {
            switch (s) { case MoveSlot.Attack: return "Attack"; case MoveSlot.Dash: return "Dash"; case MoveSlot.Ultimate: return "Ult"; default: return "Skill"; }
        }

        // ───────────────────────── Setup ─────────────────────────
        void BuildSetup()
        {
            var b = _setupBody;
            b.Clear();
            var C = S.Content; var P = S.Profile;

            Section(b, "Where");
            var arena = Btn("🏟 Arena (one room)", () => { _p.arena = true; Changed(); BuildSetup(); Restart(); });
            var real = Btn("🗺 Real floor", () => { _p.arena = false; Changed(); BuildSetup(); Restart(); });
            arena.style.backgroundColor = _p.arena ? BtnOn : BtnOff; real.style.backgroundColor = _p.arena ? BtnOff : BtnOn;
            b.Add(Row(arena, real));
            var routes = new List<RouteData>(C.routes);
            b.Add(Drop("Area", routes.ConvertAll(r => r.emoji + " " + r.name + (r.id == GameSession.FallRoute ? " (the fall)" : "")), routes.FindIndex(r => r.id == _p.area), i =>
            {
                _p.area = routes[i].id; _p.floor = 1; Changed(); BuildSetup(); Restart();
            }));
            var floors = Floors();
            if (!floors.Contains(_p.floor)) _p.floor = 1;
            b.Add(Drop("Floor", floors.ConvertAll(f => "Floor " + f + (DungeonRules.IsBossFloor(C, _p.area, f) ? " (boss)" : "")), floors.IndexOf(_p.floor), i => { _p.floor = floors[i]; Changed(); Restart(); }));
            b.Add(Row(Btn("🔄 Restart (F5)", Restart)));

            Section(b, "Class & moves");
            var classes = C.classes.FindAll(c => S.ClassAvailable(c.id));
            b.Add(Drop("Class", classes.ConvertAll(c => c.emoji + " " + c.name), classes.FindIndex(c => c.id == P.selectedClass), i =>
            {
                Home(); S.SelectClass(classes[i].id); RememberLoadout(); BuildSetup(); Restart();
            }));
            foreach (LoadSlot slot in System.Enum.GetValues(typeof(LoadSlot)))
            {
                var s = slot;
                var choices = Progression.SlotChoices(C, P, P.selectedClass, s);
                if (choices.Count == 0) continue;
                string now = Progression.Equipped(P.Progress(P.selectedClass), s);
                var names = choices.ConvertAll(m => m.emoji + " " + m.name);
                int at = choices.FindIndex(m => m.id == now);
                if (s == LoadSlot.Skill2) { names.Insert(0, "(empty)"); at = at < 0 ? 0 : at + 1; }
                b.Add(Drop(SlotName(s), names, at, i =>
                {
                    Home();
                    string id = s == LoadSlot.Skill2 ? (i == 0 ? null : choices[i - 1].id) : choices[i].id;
                    if (!Progression.Equip(C, P, P.selectedClass, s, id)) _ui.Juice.Toast(Progression.EquipBlock(C, P, P.selectedClass, s, C.Move(id)) ?? "Lab: can't equip that", null, 3f);
                    RememberLoadout(); BuildSetup(); Restart();
                }));
            }

            Section(b, "Skill levels (this run)");
            var shown = new List<string>();
            foreach (var mv in S.Loadout(P.selectedClass))
            {
                if (mv == null || mv.slot == MoveSlot.Dash || shown.Contains(mv.id)) continue;
                shown.Add(mv.id);
                string id = mv.id;
                int lv = _p.Level(_p.levels, id);
                b.Add(Drop(mv.emoji + " " + mv.name, new List<string> { "Level 1", "Level 2", "Level 3" }, Mathf.Max(1, lv) - 1, i => SetLevel(id, i + 1)));
            }
            b.Add(Row(Btn("All Lv 1", () => SetAllLevels(1)), Btn("All Lv 2", () => SetAllLevels(2)), Btn("All Lv 3", () => SetAllLevels(3))));

            Section(b, "Gear & trinkets");
            foreach (var g in Gear.Shop)
            {
                var slot = g;
                var tiers = Gear.Tiers(C, slot);
                if (tiers == null || tiers.Count == 0) continue;
                b.Add(Drop(Gear.SlotName(slot), tiers.ConvertAll(t => t.name + " · " + Gear.ValueText(slot, t)), Gear.TierIndex(P, slot, P.selectedClass), i =>
                {
                    if (slot == GearSlot.Boots) P.bootsTier = _p.boots = i; else if (slot == GearSlot.Lantern) P.lanternTier = _p.lantern = i; else if (slot == GearSlot.Bag) P.bagTier = _p.bag = i;
                    Changed(); Restart();
                }));
            }
            var trinkets = new List<TrinketData>(C.trinkets);
            var tNames = trinkets.ConvertAll(t => t.emoji + " " + t.name); tNames.Insert(0, "(none)");
            int slots = Gear.TrinketSlots(C, P);
            for (int n = 0; n < slots; n++)
            {
                int k = n;
                string cur = k < P.equippedTrinkets.Count ? P.equippedTrinkets[k] : null;
                b.Add(Drop("Trinket " + (k + 1), tNames, cur == null ? 0 : trinkets.FindIndex(t => t.id == cur) + 1, i =>
                {
                    var list = new List<string>(P.equippedTrinkets);
                    while (list.Count <= k) list.Add(null);
                    list[k] = i == 0 ? null : trinkets[i - 1].id;
                    P.equippedTrinkets.Clear();
                    foreach (var id in list) if (!string.IsNullOrEmpty(id) && !P.equippedTrinkets.Contains(id)) P.equippedTrinkets.Add(id);
                    _p.trinkets = new List<string>(P.equippedTrinkets); _p.trinketsSet = true;
                    Changed(); BuildSetup(); Restart();
                }));
            }

            Section(b, "Meals (eaten when the run starts)");
            var recipes = C.recipes;
            var accels = Cooking.Accelerators(C);
            var aNames = accels.ConvertAll(a => a.emoji + " " + a.name + ElementNote(a)); aNames.Insert(0, "(no element)");
            if (recipes.Count > 0) b.Add(Drop("Dish", recipes.ConvertAll(r => r.emoji + " " + r.name), Mathf.Clamp(_mealRecipe, 0, recipes.Count - 1), i => _mealRecipe = i));
            b.Add(Drop("Quality", new List<string>(Cooking.GradeNames), _mealGrade, i => _mealGrade = i));
            b.Add(Drop("Element", aNames, _mealElement, i => _mealElement = i));
            b.Add(Row(Btn("Add to tray", () =>
            {
                if (recipes.Count == 0) return;
                if (_p.meals.Count >= C.tuning.traySize) { _ui.Juice.Toast("Lab: the tray holds " + C.tuning.traySize, null, 2f); return; }
                _p.meals.Add(new CookedMeal { recipeId = recipes[Mathf.Clamp(_mealRecipe, 0, recipes.Count - 1)].id, grade = _mealGrade, accelId = _mealElement > 0 ? accels[_mealElement - 1].id : null });
                Changed(); BuildSetup(); Restart();
            }), Btn("Clear tray", () => { _p.meals.Clear(); Changed(); BuildSetup(); Restart(); })));
            if (_p.meals.Count == 0) b.Add(Text("  (no meals)", 12, Dim));
            foreach (var m in _p.meals)
            {
                var r = C.Recipe(m.recipeId); var a = string.IsNullOrEmpty(m.accelId) ? null : C.Ingredient(m.accelId);
                b.Add(Text("  " + Cooking.GradeGem(m.grade) + " " + (r != null ? r.emoji + " " + r.name : m.recipeId) + (a != null ? "  + " + a.emoji + " " + a.name : ""), 12, Color.white));
            }

            Section(b, "Lab");
            b.Add(Text("The lab remembers all of this (and the Play tab) between sessions. It never touches your save.", 11, Dim));
            b.Add(Row(Btn("Reset lab settings", ResetLab)));
        }

        void SetLevel(string moveId, int level)
        {
            _p.SetLevel(_p.levels, moveId, level);
            S.SetMoveLevel(moveId, level);
            Changed();
            _ui.Juice.Toast("Lab: " + (S.Content.Move(moveId) != null ? S.Content.Move(moveId).name : moveId) + " → level " + level, null, 1.6f);
        }

        void SetAllLevels(int level)
        {
            foreach (var mv in S.Loadout(S.Profile.selectedClass))
                if (mv != null && mv.slot != MoveSlot.Dash) { _p.SetLevel(_p.levels, mv.id, level); S.SetMoveLevel(mv.id, level); }
            Changed();
            _ui.Juice.Toast("Lab: every skill → level " + level, null, 1.6f);
            BuildSetup();
        }

        /// Forget the lab's settings and start it again from scratch.
        void ResetLab()
        {
            LabPrefs.Clear();
            _p = new LabPrefs();
            // the lab scene isn't in Build Settings (it never ships), so SceneManager.LoadScene can't find it: reload it by path
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#else
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
#endif
        }

        static string SlotName(LoadSlot s)
        {
            switch (s)
            {
                case LoadSlot.Attack: return "Attack";
                case LoadSlot.Skill1: return "Skill 1";
                case LoadSlot.Skill2: return "Skill 2";
                case LoadSlot.Dash: return "Dash";
                default: return "Ultimate";
            }
        }

        static string ElementNote(IngredientData a)
        {
            if (a.infuse != Element.None) return "  (" + Elements.Name(a.infuse) + " attacks)";
            if (a.guard != Element.None) return "  (guards " + Elements.Name(a.guard) + ")";
            return "";
        }

        // ───────────────────────── little builders ─────────────────────────
        static void Section(VisualElement parent, string name)
        {
            var l = Text(name, 13, Gold);
            l.style.marginTop = 12; l.style.marginBottom = 3; l.style.unityFontStyleAndWeight = FontStyle.Bold;
            l.style.borderBottomWidth = 1; l.style.borderBottomColor = new Color(1f, 0.75f, 0.4f, 0.2f); l.style.paddingBottom = 2;
            parent.Add(l);
        }

        static Label Small(string t) { var l = Text(t, 12, Gold); l.style.marginTop = 4; return l; }

        static Label Text(string t, int size, Color c)
        {
            var l = new Label(t);
            l.style.fontSize = size; l.style.color = c; l.style.whiteSpace = WhiteSpace.Normal;
            l.style.marginLeft = l.style.marginRight = 0; l.style.paddingLeft = l.style.paddingRight = 0;
            l.pickingMode = PickingMode.Ignore;
            return l;
        }

        DropdownField Drop(string label, List<string> choices, int index, System.Action<int> changed)
        {
            var d = new DropdownField(label, choices, Mathf.Clamp(index, 0, Mathf.Max(0, choices.Count - 1)));
            d.focusable = false;   // keys stay with the game (Space is dash, not "open this list")
            d.style.fontSize = 12; d.style.marginTop = 1; d.style.marginBottom = 1; d.style.marginLeft = d.style.marginRight = 0;
            d.style.height = 26; d.style.minHeight = 0; d.labelElement.style.paddingTop = d.labelElement.style.paddingBottom = 0; d.labelElement.style.unityTextAlign = TextAnchor.MiddleLeft;
            d.labelElement.style.minWidth = 84; d.labelElement.style.color = new Color(1, 1, 1, 0.75f);
            var box = d.Q(className: DropdownField.inputUssClassName);   // the game's sheet draws these light grey: make them read on the dark panel
            if (box != null) { box.style.height = 24; box.style.minHeight = 0; box.style.paddingTop = box.style.paddingBottom = 0; box.style.marginTop = box.style.marginBottom = 0; box.style.backgroundColor = new Color(0.2f, 0.15f, 0.11f); box.style.color = Cream; box.style.borderTopColor = box.style.borderBottomColor = box.style.borderLeftColor = box.style.borderRightColor = new Color(1f, 0.75f, 0.4f, 0.3f); }
            var txt = d.Q(className: DropdownField.textUssClassName);
            if (txt != null) txt.style.color = Cream;
            d.RegisterValueChangedCallback(e => { int i = d.index; if (i >= 0) changed(i); _ui.Root.Focus(); });
            return d;
        }

        Button Btn(string text, System.Action click, float width = 0f)
        {
            var b = new Button(() => { click(); _ui.Root.Focus(); }) { text = text };
            b.focusable = false;
            var s = b.style;
            if (width > 0f) { s.width = width; s.flexGrow = 0; } else s.flexGrow = 1;
            s.fontSize = 12; s.height = 26; s.marginLeft = s.marginRight = 2; s.marginTop = s.marginBottom = 1;
            s.backgroundColor = BtnNormal; s.color = Cream;
            s.paddingLeft = s.paddingRight = 4;
            return b;
        }

        Toggle Tog(string text, bool on, System.Action<bool> changed)
        {
            var t = new Toggle(text) { value = on };
            t.focusable = false;
            t.style.flexGrow = 1; t.style.fontSize = 12; t.style.marginRight = 8;
            t.labelElement.style.color = new Color(1, 1, 1, 0.85f); t.labelElement.style.minWidth = 0;
            t.RegisterValueChangedCallback(e => { changed(e.newValue); _ui.Root.Focus(); });
            return t;
        }

        Slider Slide(float lo, float hi, float value, System.Action<float> changed)
        {
            var s = new Slider(lo, hi) { value = Mathf.Clamp(value, lo, hi) };
            s.focusable = false;
            s.style.marginTop = 2; s.style.marginBottom = 2; s.style.marginLeft = s.style.marginRight = 2;
            s.RegisterValueChangedCallback(e => { changed(e.newValue); });
            return s;
        }

        static VisualElement Row(params VisualElement[] items)
        {
            var r = new VisualElement();
            r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.marginTop = 2;
            foreach (var i in items) r.Add(i);
            return r;
        }
    }
}
