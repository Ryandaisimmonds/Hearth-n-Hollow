// Hearth & Hollow — the Effects Lab (Scenes/EffectsLab.unity, GameBootstrap.effectsLab): a sandbox for working on
// fights, moves and effects before they go into the game. A throwaway profile with everything unlocked (it never
// loads or writes the save), straight into a dungeon, and a menu (F1) to swap area, floor, class, moves, gear and
// meals, spawn monsters and training dummies, slow time down and read a damage meter.
//
// Keys: F1 menu · F2 dummy at the cursor · F3 spawn the picked monster at the cursor · F4 kill all · F5 restart ·
//       - / = slower / faster time. No F keys? R opens a ring of the same actions round the mouse (time holds still
//       while it's open; click one, or click anywhere else / press R again to close).
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Dungeon;
using HearthHollow.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace HearthHollow.Lab
{
    public class EffectsLab : MonoBehaviour
    {
        GameSession S;
        GameUI _ui;
        DungeonController DC { get { return DungeonController.Instance; } }

        // what the next run sets out with
        string _area = Ids.Area.Meadow;
        int _floor = 1;
        readonly List<CookedMeal> _meals = new List<CookedMeal>();
        readonly Dictionary<string, int> _levels = new Dictionary<string, int>();   // skill levels, put back on every new run
        // the menu's own picks
        int _monster, _mealRecipe, _mealGrade = Cooking.Grades - 1, _mealElement;
        bool _elite, _boss, _autoDummy = true, _autoHeal = true, _noCooldowns;
        static readonly float[] Speeds = { 0.1f, 0.25f, 0.5f, 0.75f, 1f };
        int _speed = Speeds.Length - 1;

        VisualElement _panel, _body, _skillsBody, _setupView, _skillsView;
        Button _setupTab, _skillsTab;
        int _castLevel;            // 0 = each skill's own level, else 1…3
        bool _castAtNearest = true;
        Label _status, _meter;
        float _runEndAt = -1f;

        // ── damage meter ──
        struct HitRec { public float t; public int dmg; }
        readonly Queue<HitRec> _recent = new Queue<HitRec>();
        int _total, _hits, _last, _biggest;
        const float MeterWindow = 5f;

        public void Init(GameSession session, GameUI ui)
        {
            S = session; _ui = ui;
            UnlockEverything();
            S.DevNoDeath = true;
            EnemyController.AnyHit += OnAnyHit;
            BuildPanel();
            Restart();
        }

        void OnDestroy()
        {
            EnemyController.AnyHit -= OnAnyHit;
            GameFeel.LabScale = 1f; GameFeel.LabPaused = false;
            if (S != null) S.DevNoDeath = false;
        }

        // ───────────────────────── the throwaway profile ─────────────────────────
        /// Moves still being made: they live here (not in the content) until they're ready to port.
        public const string VerdantCrescent = "lab_verdant_crescent";
        public static void LabMoves(GameContent c)
        {
            if (c.Move(VerdantCrescent) == null)
                c.moves.Add(new MoveData
                {
                    id = VerdantCrescent, name = "Verdant Crescent", emoji = "🌿", anyClass = true,
                    description = "A huge slash all the way round you. White-hot at the blade, cooling to green.",
                    slot = MoveSlot.Skill, kind = MoveKind.VerdantCrescent, element = Element.Nature,
                    power = 1.8f, cooldown = 6f, radius = 2.4f, range = 1f, duration = 0.5f, value = 1.15f,   // value = turns
                    lightColour = "9CFF7A", lightIntensity = 0.9f, lightRadius = 4f,
                });
        }

        void UnlockEverything()
        {
            var C = S.Content; var P = S.Profile;
            LabMoves(C);
            foreach (var r in C.routes)
            {
                if (!P.clearedRouteIds.Contains(r.id)) P.clearedRouteIds.Add(r.id);
                int n = DungeonRules.Floors(C, r.id);
                for (int f = 1; f <= n; f++) if (DungeonRules.IsBossFloor(C, r.id, f)) Progression.RecordBoss(P, r.id, f);   // every floor shortcut
            }
            if (P.learnedMoves == null) P.learnedMoves = new List<string>();
            foreach (var m in C.moves) if (!P.learnedMoves.Contains(m.id)) P.learnedMoves.Add(m.id);
            S.DevGiveKeyItems();
            S.Story.DevSetArcStage(int.MaxValue);
            S.DevMaxEverything();   // every class, gear track, trinket and the kitchen
            foreach (var m in P.packed) _meals.Add(m);   // start with the dev tray (the best dish of each station, Prime)
            foreach (var cd in C.classes) Progression.Equip(C, P, cd.id, LoadSlot.Skill2, VerdantCrescent);   // the newest lab move, ready on Q
            if (C.Route(_area) == null && C.routes.Count > 0) _area = C.routes[0].id;
        }

        // ───────────────────────── runs ─────────────────────────
        /// Back to camp (whatever's going on) and straight out again with the lab's area, floor and meals.
        void Restart()
        {
            if (S.Phase == Phase.Dungeon || S.Phase == Phase.Gather || S.Phase == Phase.Rest) S.FleeHome();
            if (S.Phase == Phase.RunEnd) S.ReturnToHearth();
            if (S.Phase != Phase.Hearth) return;
            // a calm, plain day: no storm, no Rich / Lucky / Dangerous area rolls
            if (S.Hearth != null)
            {
                S.Hearth.campEvent = CampEvent.None;
                foreach (var o in S.Hearth.routeOffers) o.mod = RouteMod.None;
            }
            var P = S.Profile;
            P.packed.Clear();
            foreach (var m in _meals) P.packed.Add(new CookedMeal { recipeId = m.recipeId, grade = m.grade, accelId = m.accelId });
            Cooking.SyncTray(P);
            // a class-locked area (only a Ranger can enter…): go in as that class
            var route = S.Content.Route(_area);
            if (route != null && Routes.ClassBlock(S.Content, route, P.selectedClass) != null && S.ClassAvailable(route.requiredClass))
            {
                S.SelectClass(route.requiredClass);
                _ui.Juice.Toast("Lab: " + route.name + " is " + route.requiredClass + "-only — switched class", null, 3f);
                BuildBody();
            }
            if (!S.StartRun(_area, 1)) { _ui.Juice.Toast("Lab: couldn't set out for " + _area, null, 3f); return; }
            while (S.Run != null && S.Run.floor < _floor && S.DungeonDescend()) { }   // any floor, not just the shortcut ones
            _runEndAt = -1f;
            foreach (var kv in _levels) S.SetMoveLevel(kv.Key, kv.Value);
            if (_autoDummy && DC != null && DC.CurrentRoom != null && DC.Player != null)
                SpawnDummy(DC.CurrentRoom.NearestWalkable(DC.Player.Feet + new Vector2(2.6f, 0.4f)));
            Refresh();
        }

        // ───────────────────────── spawning ─────────────────────────
        Vector2 SpawnPoint() { return SpawnPoint(null); }

        /// Where to put a spawn: the given spot, else the cursor (when it's over the room), else near the player.
        Vector2 SpawnPoint(Vector2? spot)
        {
            var room = DC.CurrentRoom;
            if (spot.HasValue) return room.NearestWalkable(spot.Value);
            Vector2 at;
            if (DungeonInput.Cursor(out at) && !DungeonInput.PointerOverUI()) return room.NearestWalkable(at);
            Vector2 off = Random.insideUnitCircle.normalized * 2.8f;
            return room.NearestWalkable(DC.Player.Feet + off);
        }

        bool Ready { get { return DC != null && DC.CurrentRoom != null && DC.Player != null && S.Phase == Phase.Dungeon; } }

        void SpawnMonster(Vector2 at)
        {
            if (!Ready || S.Content.monsters.Count == 0) return;
            var md = S.Content.monsters[Mathf.Clamp(_monster, 0, S.Content.monsters.Count - 1)];
            var room = DC.CurrentRoom;
            var e = DC.SpawnEnemy(room, md.id, _elite && !_boss, _boss, at);
            if (e != null) room.Adopt(e);
        }

        void SpawnDummy(Vector2 at)
        {
            if (DC == null || DC.CurrentRoom == null) return;
            var stats = new EnemyStats { monsterId = "training_dummy", maxHp = 9999, damage = 0f, difficulty = 1 };
            var e = DC.SpawnEnemy(DC.CurrentRoom, stats.monsterId, false, false, at, stats);
            if (e != null) e.MakeDummy();   // not adopted: it never holds a room's doors shut
        }

        void KillAll(bool dummiesToo)
        {
            if (!Ready) return;
            foreach (var e in FindObjectsByType<EnemyController>())
            {
                if (e == null || e.Dead) continue;
                if (e.Dummy) { if (dummiesToo) Destroy(e.gameObject); continue; }
                if (e.Spawning) { Destroy(e.gameObject); continue; }
                e.TakeHit(999999f, true, Vector2.up);
            }
        }

        // ───────────────────────── every frame ─────────────────────────
        void Update()
        {
            var k = Keyboard.current;
            if (k != null && k.rKey.wasPressedThisFrame) { if (RingOpen) CloseRing(); else OpenRing(); }
            if (k != null && !RingOpen)
            {
                if (k.f1Key.wasPressedThisFrame) _panel.style.display = _panel.style.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;
                if (k.f2Key.wasPressedThisFrame && Ready) SpawnDummy(SpawnPoint());
                if (k.f3Key.wasPressedThisFrame && Ready) SpawnMonster(SpawnPoint());
                if (k.f4Key.wasPressedThisFrame) KillAll(false);
                if (k.f5Key.wasPressedThisFrame) Restart();
                if (k.minusKey.wasPressedThisFrame || k.numpadMinusKey.wasPressedThisFrame) SetSpeed(_speed - 1);
                if (k.equalsKey.wasPressedThisFrame || k.numpadPlusKey.wasPressedThisFrame) SetSpeed(_speed + 1);
            }

            if (Ready)
            {
                var run = S.Run; var st = S.Stats;
                if (_autoHeal && run != null && st != null && run.hp < st.maxHp) run.hp = st.maxHp;
                if (_noCooldowns) DC.Player.SetCooldowns(new float[4]);
            }
            // died (cheats off) or left by a ladder home: back out after a moment
            if (S.Phase == Phase.RunEnd)
            {
                if (_runEndAt < 0f) _runEndAt = Time.unscaledTime;
                else if (Time.unscaledTime - _runEndAt > 2.5f) Restart();
            }
            else if (S.Phase == Phase.Hearth) Restart();   // never sit in camp
        }

        void SetSpeed(int i)
        {
            _speed = Mathf.Clamp(i, 0, Speeds.Length - 1);
            GameFeel.LabScale = Speeds[_speed];
            Refresh();
        }

        void OnAnyHit(EnemyController e, int dmg, bool crit)
        {
            float now = Time.time;
            _recent.Enqueue(new HitRec { t = now, dmg = dmg });
            _total += dmg; _hits++; _last = dmg; if (dmg > _biggest) _biggest = dmg;
        }

        string MeterText()
        {
            float now = Time.time;
            while (_recent.Count > 0 && now - _recent.Peek().t > MeterWindow) _recent.Dequeue();
            int sum = 0; float first = now;
            foreach (var h in _recent) { sum += h.dmg; if (h.t < first) first = h.t; }
            float span = Mathf.Max(1f, Mathf.Min(MeterWindow, now - first));
            return "Last " + _last + "   Biggest " + _biggest + "   DPS " + (sum / span).ToString("0.0") +
                   "\nTotal " + _total + " in " + _hits + " hits";
        }

        void Refresh()
        {
            if (_status == null) return;
            var run = S.Run;
            var r = S.Content.Route(_area);
            string room = DC != null && DC.CurrentRoom != null ? DC.CurrentRoom.Node.kind.ToString() : "-";
            int alive = 0;
            foreach (var e in FindObjectsByType<EnemyController>()) if (e != null && !e.Dead && !e.Dummy) alive++;
            _status.text = (r != null ? r.emoji + " " + r.name : _area) + "  ·  floor " + (run != null ? run.floor : _floor) + "  ·  " + room +
                           "\n" + S.Profile.selectedClass + "  ·  monsters " + alive + "  ·  time ×" + Speeds[_speed] +
                           (run != null ? "  ·  HP " + run.hp + "/" + (S.Stats != null ? S.Stats.maxHp : 0) : "");
        }

        // ───────────────────────── the menu ─────────────────────────
        void BuildPanel()
        {
            _panel = new VisualElement { name = "effects-lab" };
            _panel.AddToClassList("panel");   // DungeonInput.PointerOverUI: clicks here never swing
            var ps = _panel.style;
            ps.position = Position.Absolute; ps.right = 8; ps.top = 8; ps.bottom = 8; ps.width = 380;
            ps.backgroundColor = new Color(0.08f, 0.06f, 0.05f, 0.9f);
            ps.borderTopLeftRadius = ps.borderTopRightRadius = ps.borderBottomLeftRadius = ps.borderBottomRightRadius = 8;
            ps.paddingLeft = ps.paddingRight = 10; ps.paddingTop = ps.paddingBottom = 8;
            ps.borderLeftWidth = ps.borderRightWidth = ps.borderTopWidth = ps.borderBottomWidth = 1;
            ps.borderLeftColor = ps.borderRightColor = ps.borderTopColor = ps.borderBottomColor = new Color(1f, 0.75f, 0.4f, 0.35f);
            _panel.pickingMode = PickingMode.Position;

            var title = Text("🔥 Effects Lab", 17, new Color(1f, 0.85f, 0.55f));
            _panel.Add(title);
            _panel.Add(Text("R quick ring · F1 menu · F2 dummy · F3 spawn · F4 kill all · F5 restart · − / = time", 11, new Color(1f, 1f, 1f, 0.55f)));
            _status = Text("", 12, Color.white); _status.style.marginTop = 4;
            _panel.Add(_status);
            _meter = Text("", 12, new Color(1f, 0.9f, 0.5f)); _meter.style.marginTop = 2;
            _panel.Add(_meter);

            // two tabs: Setup (area, class, gear, meals, spawning…) and Skills (every move, one click to use it)
            var tabs = Row(_setupTab = Btn("⚙️ Setup", () => ShowTab(false)), _skillsTab = Btn("✨ Skills", () => ShowTab(true)));
            tabs.style.marginTop = 8;
            _panel.Add(tabs);
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1; scroll.style.marginTop = 4;
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _panel.Add(scroll);
            _setupView = scroll;
            _body = scroll.contentContainer;
            BuildBody();
            var skills = new ScrollView(ScrollViewMode.Vertical);
            skills.style.flexGrow = 1; skills.style.marginTop = 4;
            skills.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _panel.Add(skills);
            _skillsView = skills;
            _skillsBody = skills.contentContainer;
            BuildSkills();
            ShowTab(false);
            _ui.Root.Add(_panel);

            _panel.schedule.Execute(() => { Refresh(); _meter.text = MeterText(); }).Every(250);
        }

        void BuildBody()
        {
            _body.Clear();
            var C = S.Content; var P = S.Profile;

            // ── where ──
            Section("Area");
            var routes = new List<RouteData>(C.routes);
            _body.Add(Drop("Area", routes.ConvertAll(r => r.emoji + " " + r.name + (r.id == GameSession.FallRoute ? " (the fall)" : "")), routes.FindIndex(r => r.id == _area), i =>
            {
                _area = routes[i].id; _floor = 1; BuildBody(); Restart();
            }));
            var floors = Floors();
            if (!floors.Contains(_floor)) _floor = 1;
            _body.Add(Drop("Floor", floors.ConvertAll(f => "Floor " + f + (DungeonRules.IsBossFloor(C, _area, f) ? " (boss)" : "")), floors.IndexOf(_floor), i => { _floor = floors[i]; Restart(); }));
            _body.Add(Row(Btn("Restart run (F5)", Restart), Btn("Next floor", NextFloor)));

            // ── who ──
            Section("Class & moves");
            var classes = C.classes.FindAll(c => S.ClassAvailable(c.id));
            _body.Add(Drop("Class", classes.ConvertAll(c => c.emoji + " " + c.name), classes.FindIndex(c => c.id == P.selectedClass), i =>
            {
                Home(); S.SelectClass(classes[i].id); BuildBody(); Restart();
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
                _body.Add(Drop(SlotName(s), names, at, i =>
                {
                    Home();
                    string id = s == LoadSlot.Skill2 ? (i == 0 ? null : choices[i - 1].id) : choices[i].id;
                    if (!Progression.Equip(C, P, P.selectedClass, s, id)) _ui.Juice.Toast(Progression.EquipBlock(C, P, P.selectedClass, s, C.Move(id)) ?? "Can't equip that", null, 3f);
                    BuildBody(); Restart();
                }));
            }

            // ── skill levels (this run; the lab keeps them across restarts) ──
            Section("Skill levels");
            var shown = new List<string>();
            foreach (var mv in S.Loadout(P.selectedClass))
            {
                if (mv == null || mv.slot == MoveSlot.Dash || shown.Contains(mv.id)) continue;
                shown.Add(mv.id);
                string id = mv.id;
                _body.Add(Drop(mv.emoji + " " + mv.name, new List<string> { "Level 1", "Level 2", "Level 3" }, (_levels.ContainsKey(id) ? _levels[id] : S.MoveLevel(id)) - 1, i => SetLevel(id, i + 1)));
            }
            _body.Add(Row(Btn("All Lv 1", () => SetAllLevels(1)), Btn("All Lv 2", () => SetAllLevels(2)), Btn("All Lv 3", () => SetAllLevels(3))));

            // ── gear ──
            Section("Gear & trinkets");
            foreach (var g in Gear.Shop)
            {
                var slot = g;
                var tiers = Gear.Tiers(C, slot);
                if (tiers == null || tiers.Count == 0) continue;
                _body.Add(Drop(Gear.SlotName(slot), tiers.ConvertAll(t => t.name + " · " + Gear.ValueText(slot, t)), Gear.TierIndex(P, slot, P.selectedClass), i =>
                {
                    if (slot == GearSlot.Boots) P.bootsTier = i; else if (slot == GearSlot.Lantern) P.lanternTier = i; else if (slot == GearSlot.Bag) P.bagTier = i;
                    Restart();
                }));
            }
            var trinkets = new List<TrinketData>(C.trinkets);
            var tNames = trinkets.ConvertAll(t => t.emoji + " " + t.name); tNames.Insert(0, "(none)");
            int slots = Gear.TrinketSlots(C, P);
            for (int n = 0; n < slots; n++)
            {
                int k = n;
                string cur = k < P.equippedTrinkets.Count ? P.equippedTrinkets[k] : null;
                _body.Add(Drop("Trinket " + (k + 1), tNames, cur == null ? 0 : trinkets.FindIndex(t => t.id == cur) + 1, i =>
                {
                    var list = new List<string>(P.equippedTrinkets);
                    while (list.Count <= k) list.Add(null);
                    list[k] = i == 0 ? null : trinkets[i - 1].id;
                    P.equippedTrinkets.Clear();
                    foreach (var id in list) if (!string.IsNullOrEmpty(id) && !P.equippedTrinkets.Contains(id)) P.equippedTrinkets.Add(id);
                    BuildBody(); Restart();
                }));
            }

            // ── meals ──
            Section("Meals (eaten when the run starts)");
            var recipes = C.recipes;
            var accels = Cooking.Accelerators(C);
            var aNames = accels.ConvertAll(a => a.emoji + " " + a.name + ElementNote(a)); aNames.Insert(0, "(no element)");
            if (recipes.Count > 0) _body.Add(Drop("Dish", recipes.ConvertAll(r => r.emoji + " " + r.name), Mathf.Clamp(_mealRecipe, 0, recipes.Count - 1), i => _mealRecipe = i));
            _body.Add(Drop("Quality", new List<string>(Cooking.GradeNames), _mealGrade, i => _mealGrade = i));
            _body.Add(Drop("Element", aNames, _mealElement, i => _mealElement = i));
            _body.Add(Row(Btn("Add to tray", () =>
            {
                if (recipes.Count == 0) return;
                if (_meals.Count >= C.tuning.traySize) { _ui.Juice.Toast("The tray holds " + C.tuning.traySize, null, 2f); return; }
                _meals.Add(new CookedMeal { recipeId = recipes[Mathf.Clamp(_mealRecipe, 0, recipes.Count - 1)].id, grade = _mealGrade, accelId = _mealElement > 0 ? accels[_mealElement - 1].id : null });
                BuildBody(); Restart();
            }), Btn("Clear tray", () => { _meals.Clear(); BuildBody(); Restart(); })));
            if (_meals.Count == 0) _body.Add(Text("  (no meals)", 12, new Color(1, 1, 1, 0.5f)));
            foreach (var m in _meals)
            {
                var r = C.Recipe(m.recipeId); var a = string.IsNullOrEmpty(m.accelId) ? null : C.Ingredient(m.accelId);
                _body.Add(Text("  " + Cooking.GradeGem(m.grade) + " " + (r != null ? r.emoji + " " + r.name : m.recipeId) + (a != null ? "  + " + a.emoji + " " + a.name : ""), 12, Color.white));
            }

            // ── monsters ──
            Section("Spawn");
            var mons = C.monsters;
            if (mons.Count > 0) _body.Add(Drop("Monster", mons.ConvertAll(m => m.emoji + " " + m.name), Mathf.Clamp(_monster, 0, mons.Count - 1), i => _monster = i));
            _body.Add(Row(Tog("Elite", _elite, v => _elite = v), Tog("Boss", _boss, v => _boss = v)));
            _body.Add(Row(Btn("Spawn (F3)", () => { if (Ready) SpawnMonster(SpawnPoint()); }), Btn("Spawn 5", () => { if (Ready) for (int i = 0; i < 5; i++) SpawnMonster(SpawnPoint()); })));
            _body.Add(Row(Btn("Floor boss", () =>
            {
                if (!Ready) return;
                string id = DungeonRules.BossMonster(C, S.Run);
                var room = DC.CurrentRoom;
                var e = DC.SpawnEnemy(room, id, false, true, SpawnPoint());
                if (e != null) { room.Adopt(e); DC.BossEntrance(room, e); }
            }), Btn("Dummy (F2)", () => { if (Ready) SpawnDummy(SpawnPoint()); })));
            _body.Add(Row(Btn("Kill all (F4)", () => KillAll(false)), Btn("Remove dummies", () =>
            {
                foreach (var e in FindObjectsByType<EnemyController>()) if (e != null && e.Dummy) Destroy(e.gameObject);
            })));
            _body.Add(Tog("A dummy by the door each run", _autoDummy, v => _autoDummy = v));

            // ── cheats ──
            Section("Cheats & time");
            _body.Add(Tog("Can't die", S.DevNoDeath, v => S.DevNoDeath = v));
            _body.Add(Tog("Heal to full all the time", _autoHeal, v => _autoHeal = v));
            _body.Add(Tog("No skill cooldowns", _noCooldowns, v => _noCooldowns = v));
            _body.Add(Drop("Time", new List<float>(Speeds).ConvertAll(f => "×" + f), _speed, SetSpeed));
            _body.Add(Row(Btn("Reset meter", () => { _recent.Clear(); _total = _hits = _last = _biggest = 0; }), Btn("Heal", () => { if (S.Run != null && S.Stats != null) S.Run.hp = S.Stats.maxHp; })));
        }

        // ───────────────────────── the quick ring (R): the F-key actions round the mouse ─────────────────────────
        VisualElement _ring;
        Vector2? _ringWorld;   // the room spot under the mouse when the ring opened (dummy / spawn go there)
        bool RingOpen { get { return _ring != null; } }

        void OpenRing()
        {
            var m = Mouse.current;
            if (m != null) OpenRingAt(m.position.ReadValue());
        }

        /// The ring centred on a screen point (pixels, origin bottom-left).
        void OpenRingAt(Vector2 screen)
        {
            var panel = _ui.Root.panel;
            if (panel == null) return;
            Vector2 c = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screen.x, Screen.height - screen.y));
            var cam = Camera.main;
            _ringWorld = cam != null && !DungeonInput.PointerOverUI() ? (Vector2?)(Vector2)cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z)) : null;

            // a see-through cover over the whole screen: a click on it closes the ring (and never swings)
            _ring = new VisualElement { name = "lab-ring" };
            _ring.AddToClassList("panel");
            var rs = _ring.style;
            rs.position = Position.Absolute; rs.left = 0; rs.top = 0; rs.right = 0; rs.bottom = 0;
            rs.backgroundColor = new Color(0f, 0f, 0f, 0.25f);
            rs.backgroundImage = new StyleBackground(StyleKeyword.None);   // .panel draws a framed box: not here
            rs.marginLeft = rs.marginRight = rs.marginTop = rs.marginBottom = 0;
            rs.paddingLeft = rs.paddingRight = rs.paddingTop = rs.paddingBottom = 0;
            _ring.RegisterCallback<PointerDownEvent>(e => { if (e.target == _ring) { CloseRing(); e.StopPropagation(); } });

            var items = new List<KeyValuePair<string, System.Action>>
            {
                new KeyValuePair<string, System.Action>("☰\nMenu  F1", () => _panel.style.display = _panel.style.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None),
                new KeyValuePair<string, System.Action>("🎯\nDummy  F2", () => { if (Ready) SpawnDummy(SpawnPoint(_ringWorld)); }),
                new KeyValuePair<string, System.Action>("👾\nSpawn  F3", () => { if (Ready) SpawnMonster(SpawnPoint(_ringWorld)); }),
                new KeyValuePair<string, System.Action>("💀\nKill all  F4", () => KillAll(false)),
                new KeyValuePair<string, System.Action>("🔄\nRestart  F5", Restart),
                new KeyValuePair<string, System.Action>("🐢\nSlower  −", () => SetSpeed(_speed - 1)),
                new KeyValuePair<string, System.Action>("🐇\nFaster  =", () => SetSpeed(_speed + 1)),
                new KeyValuePair<string, System.Action>("⬆️\nSkill level", CycleLevels),
            };
            const float Radius = 112f, Size = 86f;
            // near a screen edge, slide the ring in so every button shows (the spawn spot stays where the mouse was)
            float reach = Radius + Size / 2f + 16f;
            var full = _ui.Root.layout;
            if (full.width > reach * 2f) c.x = Mathf.Clamp(c.x, reach, full.width - reach);
            if (full.height > reach * 2f) c.y = Mathf.Clamp(c.y, reach, full.height - reach);
            for (int i = 0; i < items.Count; i++)
            {
                var act = items[i].Value;
                float a = Mathf.PI / 2f - i * Mathf.PI * 2f / items.Count;   // clockwise from the top
                var b = new Button(() => { CloseRing(); act(); _ui.Root.Focus(); }) { text = items[i].Key };
                b.focusable = false;
                var bs = b.style;
                bs.position = Position.Absolute; bs.width = Size; bs.height = Size;
                bs.left = c.x + Mathf.Cos(a) * Radius - Size / 2f; bs.top = c.y - Mathf.Sin(a) * Radius - Size / 2f;
                bs.borderTopLeftRadius = bs.borderTopRightRadius = bs.borderBottomLeftRadius = bs.borderBottomRightRadius = Size / 2f;
                bs.backgroundColor = new Color(0.16f, 0.11f, 0.08f, 0.94f);
                bs.borderLeftWidth = bs.borderRightWidth = bs.borderTopWidth = bs.borderBottomWidth = 2;
                bs.borderLeftColor = bs.borderRightColor = bs.borderTopColor = bs.borderBottomColor = new Color(1f, 0.75f, 0.4f, 0.7f);
                bs.color = new Color(1f, 0.93f, 0.8f); bs.fontSize = 12; bs.whiteSpace = WhiteSpace.Normal;
                bs.unityTextAlign = TextAnchor.MiddleCenter;
                bs.paddingLeft = bs.paddingRight = bs.paddingTop = bs.paddingBottom = 0;
                bs.marginLeft = bs.marginRight = bs.marginTop = bs.marginBottom = 0;
                _ring.Add(b);
            }
            var hub = Text("R", 13, new Color(1f, 0.85f, 0.55f));
            var hs = hub.style;
            hs.position = Position.Absolute; hs.width = 30; hs.height = 30; hs.left = c.x - 15f; hs.top = c.y - 15f;
            hs.unityTextAlign = TextAnchor.MiddleCenter; hs.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
            hs.borderTopLeftRadius = hs.borderTopRightRadius = hs.borderBottomLeftRadius = hs.borderBottomRightRadius = 15;
            _ring.Add(hub);

            _ui.Root.Add(_ring);
            GameFeel.LabPaused = true;
        }

        void CloseRing()
        {
            if (_ring != null) _ring.RemoveFromHierarchy();
            _ring = null;
            GameFeel.LabPaused = false;
        }

        void ShowTab(bool skills)
        {
            _setupView.style.display = skills ? DisplayStyle.None : DisplayStyle.Flex;
            _skillsView.style.display = skills ? DisplayStyle.Flex : DisplayStyle.None;
            var on = new Color(0.62f, 0.42f, 0.2f); var off = new Color(0.25f, 0.18f, 0.12f);
            _setupTab.style.backgroundColor = skills ? off : on;
            _skillsTab.style.backgroundColor = skills ? on : off;
        }

        // ───────────────────────── the Skills tab: every move in the game, one click to use it ─────────────────────────
        void BuildSkills()
        {
            var C = S.Content;
            var b = _skillsBody;
            b.Clear();
            var tip = Text("Click a move to use it now (any class's, any slot). It doesn't change your loadout or cooldowns.", 11, new Color(1, 1, 1, 0.6f));
            b.Add(tip);
            var lvl = Drop("Level", new List<string> { "Each skill's own", "Level 1", "Level 2", "Level 3" }, _castLevel, i => _castLevel = i);
            b.Add(lvl);
            var aim = Tog("Aim at the nearest monster", _castAtNearest, v => _castAtNearest = v);
            b.Add(aim);

            // grouped: the lab's own moves first, then each class, then the moves any class can use
            var lab = C.moves.FindAll(m => m.id.StartsWith("lab_"));
            if (lab.Count > 0) SkillGroup(b, "🧪 New in the lab", lab);
            foreach (var cd in C.classes)
            {
                var own = C.moves.FindAll(m => !m.anyClass && m.owner == cd.id && !m.id.StartsWith("lab_"));
                if (own.Count > 0) SkillGroup(b, cd.emoji + " " + cd.name, own);
            }
            var any = C.moves.FindAll(m => m.anyClass && !m.id.StartsWith("lab_"));
            if (any.Count > 0) SkillGroup(b, "Any class", any);
        }

        void SkillGroup(VisualElement parent, string title, List<MoveData> moves)
        {
            var l = Text(title, 13, new Color(1f, 0.78f, 0.45f));
            l.style.marginTop = 10; l.style.marginBottom = 2; l.style.unityFontStyleAndWeight = FontStyle.Bold;
            parent.Add(l);
            moves.Sort((x, y) => x.slot != y.slot ? x.slot.CompareTo(y.slot) : string.CompareOrdinal(x.name, y.name));
            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row; grid.style.flexWrap = Wrap.Wrap;
            foreach (var m in moves)
            {
                var mv = m;
                var btn = Btn(mv.emoji + " " + mv.name + "  · " + SlotTag(mv.slot), () => Cast(mv));
                btn.style.flexGrow = 0; btn.style.width = Length.Percent(48); btn.style.height = 28;
                btn.style.marginLeft = btn.style.marginRight = Length.Percent(1); btn.style.marginTop = btn.style.marginBottom = 2;
                btn.style.unityTextAlign = TextAnchor.MiddleLeft; btn.style.paddingLeft = 6;
                btn.style.whiteSpace = WhiteSpace.NoWrap; btn.style.overflow = Overflow.Hidden; btn.style.textOverflow = TextOverflow.Ellipsis;
                btn.tooltip = mv.description;
                grid.Add(btn);
            }
            parent.Add(grid);
        }

        static string SlotTag(MoveSlot s)
        {
            switch (s) { case MoveSlot.Attack: return "Attack"; case MoveSlot.Dash: return "Dash"; case MoveSlot.Ultimate: return "Ult"; default: return "Skill"; }
        }

        void Cast(MoveData m)
        {
            if (!Ready) return;
            if (_castLevel > 0) S.SetMoveLevel(m.id, _castLevel);
            Vector2 aim = Vector2.zero;
            if (_castAtNearest)
            {
                float best = float.MaxValue; Vector2 from = DC.Player.Body;
                foreach (var e in FindObjectsByType<EnemyController>())
                {
                    if (e == null || e.Dead) continue;
                    float d = (e.Body - from).sqrMagnitude;
                    if (d < best) { best = d; aim = e.Body - from; }
                }
            }
            if (!DC.Player.LabCast(m, aim)) _ui.Juice.Toast("Lab: " + m.name + " did nothing here", null, 1.6f);
        }

        void SetLevel(string moveId, int level)
        {
            _levels[moveId] = level;
            S.SetMoveLevel(moveId, level);
            _ui.Juice.Toast("Lab: " + (S.Content.Move(moveId) != null ? S.Content.Move(moveId).name : moveId) + " → level " + level, null, 1.6f);
        }

        void SetAllLevels(int level)
        {
            foreach (var mv in S.Loadout(S.Profile.selectedClass))
                if (mv != null && mv.slot != MoveSlot.Dash) { _levels[mv.id] = level; S.SetMoveLevel(mv.id, level); }
            _ui.Juice.Toast("Lab: every skill → level " + level, null, 1.6f);
            BuildBody();
        }

        /// The quick ring's level button: every skill up one level (3 wraps back to 1).
        void CycleLevels()
        {
            int now = 1;
            foreach (var mv in S.Loadout(S.Profile.selectedClass)) if (mv != null && mv.slot != MoveSlot.Dash) now = Mathf.Max(now, S.MoveLevel(mv.id));
            SetAllLevels(now >= GameSession.MaxMoveLevel ? 1 : now + 1);
        }

        void NextFloor()
        {
            var floors = Floors();
            int i = floors.IndexOf(_floor);
            _floor = floors[(i + 1) % floors.Count];
            BuildBody(); Restart();
        }

        List<int> Floors()
        {
            var list = new List<int>();
            int n = _area == GameSession.FallRoute ? 1 : DungeonRules.Floors(S.Content, _area);
            for (int f = 1; f <= n; f++) list.Add(f);
            return list;
        }

        /// Loadout and class changes are camp-only in the game: step home first (Restart sets out again).
        void Home()
        {
            if (S.Phase == Phase.Dungeon || S.Phase == Phase.Gather || S.Phase == Phase.Rest) S.FleeHome();
            if (S.Phase == Phase.RunEnd) S.ReturnToHearth();
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

        // ── little UI builders (inline styles: the lab doesn't touch the game's style sheets) ──
        void Section(string name)
        {
            var l = Text(name, 13, new Color(1f, 0.78f, 0.45f));
            l.style.marginTop = 10; l.style.marginBottom = 2; l.style.unityFontStyleAndWeight = FontStyle.Bold;
            _body.Add(l);
        }

        static Label Text(string t, int size, Color c)
        {
            var l = new Label(t);
            l.style.fontSize = size; l.style.color = c; l.style.whiteSpace = WhiteSpace.Normal;
            l.pickingMode = PickingMode.Ignore;
            return l;
        }

        DropdownField Drop(string label, List<string> choices, int index, System.Action<int> changed)
        {
            var d = new DropdownField(label, choices, Mathf.Clamp(index, 0, Mathf.Max(0, choices.Count - 1)));
            d.focusable = false;   // keys stay with the game (Space is dash, not "open this list")
            d.style.fontSize = 12; d.style.marginTop = 1; d.style.marginBottom = 1;
            d.labelElement.style.minWidth = 78; d.labelElement.style.color = new Color(1, 1, 1, 0.75f);
            var box = d.Q(className: DropdownField.inputUssClassName);   // the game's sheet draws these light grey: make them read on the dark panel
            if (box != null) { box.style.backgroundColor = new Color(0.2f, 0.15f, 0.11f); box.style.color = new Color(1f, 0.93f, 0.8f); box.style.borderTopColor = box.style.borderBottomColor = box.style.borderLeftColor = box.style.borderRightColor = new Color(1f, 0.75f, 0.4f, 0.3f); }
            var txt = d.Q(className: DropdownField.textUssClassName);
            if (txt != null) txt.style.color = new Color(1f, 0.93f, 0.8f);
            d.RegisterValueChangedCallback(e => { int i = d.index; if (i >= 0) changed(i); _ui.Root.Focus(); });
            return d;
        }

        Button Btn(string text, System.Action click)
        {
            var b = new Button(() => { click(); _ui.Root.Focus(); }) { text = text };
            b.focusable = false;
            b.style.flexGrow = 1; b.style.fontSize = 12; b.style.height = 24;
            b.style.backgroundColor = new Color(0.42f, 0.27f, 0.14f); b.style.color = new Color(1f, 0.93f, 0.8f);
            return b;
        }

        Toggle Tog(string text, bool on, System.Action<bool> changed)
        {
            var t = new Toggle(text) { value = on };
            t.focusable = false;
            t.style.flexGrow = 1; t.style.fontSize = 12;
            t.labelElement.style.color = new Color(1, 1, 1, 0.8f);
            t.RegisterValueChangedCallback(e => { changed(e.newValue); _ui.Root.Focus(); });
            return t;
        }

        static VisualElement Row(params VisualElement[] items)
        {
            var r = new VisualElement();
            r.style.flexDirection = FlexDirection.Row; r.style.marginTop = 2;
            foreach (var i in items) r.Add(i);
            return r;
        }
    }
}
