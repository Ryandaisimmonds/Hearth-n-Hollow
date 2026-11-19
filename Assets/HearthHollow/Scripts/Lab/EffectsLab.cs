// Hearth & Hollow — the Effects Lab (Scenes/EffectsLab.unity, GameBootstrap.effectsLab): a sandbox for working on
// fights, moves and effects before they go into the game. A throwaway profile with everything unlocked (it never
// loads or writes the save), straight into a dungeon (one calm arena room by default).
//
// The screen: the game on the left, the lab's panel down the right (EffectsLab.Panel.cs). Two tabs: Play (skills,
// auto-cast, time, targets, view, cheats, the damage meter) and Setup (area, class, loadout, gear, meals). The lab
// remembers its settings between sessions (EffectsLab.Prefs.cs).
//
// Keys: P pause · . one frame · T auto-cast · − / = slower / faster · R quick ring round the mouse ·
//       F1 hide the panel · F2 dummy at the cursor · F3 monster at the cursor · F4 kill all · F5 restart.
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Dungeon;
using HearthHollow.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace HearthHollow.Lab
{
    public partial class EffectsLab : MonoBehaviour
    {
        GameSession S;
        GameUI _ui;
        LabPrefs _p;
        DungeonController DC { get { return DungeonController.Instance; } }
        bool Ready { get { return DC != null && DC.CurrentRoom != null && DC.Player != null && S.Phase == Phase.Dungeon; } }

        const float PanelShare = 0.3f;   // the panel's share of the screen width (the game gets the rest)
        static readonly float[] SpeedSteps = { 0.05f, 0.1f, 0.25f, 0.5f, 0.75f, 1f, 1.5f, 2f };

        bool _paused, _stepping; int _stepFrame;
        float _runEndAt = -1f, _autoT;
        bool _panelShown = true;

        public void Init(GameSession session, GameUI ui)
        {
            S = session; _ui = ui;
            _p = LabPrefs.Load();
            UnlockEverything();
            ApplySetupPrefs();
            S.DevNoDeath = _p.cantDie;
            EnemyController.AnyHit += OnAnyHit;
            DungeonController.LabArena = _p.arena;
            EnemyController.LabFrozen = _p.freeze;
            GameFeel.LabScale = Mathf.Clamp(_p.speed, 0.05f, 2f);
            DungeonCamera.LabZoom = Mathf.Clamp(_p.zoom, 0.4f, 1.4f);
            ApplyLight();
            // toasts: just the lab's own and level-ups (no loot or coin spam)
            Juice.Filter = t => t.StartsWith("Lab") || t.Contains("level") || t.Contains("⬆️");
            BuildPanel();
            TrimHud();
            ShowPanel(true);
            Restart();
        }

        void OnDestroy()
        {
            EnemyController.AnyHit -= OnAnyHit;
            GameFeel.LabScale = 1f; GameFeel.LabPaused = false;
            EnemyController.LabFrozen = false;
            DungeonController.LabArena = false; DungeonCamera.LabFollow = null; DungeonCamera.LabZoom = 1f; RoomLighting.LabLight = -1f;
            Juice.Filter = null;
            var cam = Camera.main; if (cam != null) cam.rect = new Rect(0f, 0f, 1f, 1f);
            if (S != null) S.DevNoDeath = false;
            if (_p != null) _p.Save();
        }

        void Changed() { _p.Save(); }

        // ───────────────────────── the screen split ─────────────────────────
        /// The game on the left, the panel down the right (or the whole screen for the game with the panel hidden).
        void ShowPanel(bool show)
        {
            _panelShown = show;
            float game = show ? 1f - PanelShare : 1f;
            var cam = Camera.main;
            if (cam != null) cam.rect = new Rect(0f, 0f, game, 1f);
            if (_panel != null) _panel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            _ui.Stage.style.right = Length.Percent(show ? PanelShare * 100f : 0f);        // the HUD stays inside the game
            _ui.ToastLayer.style.right = Length.Percent(show ? PanelShare * 100f : 0f);
        }

        /// The HUD keeps the skill bar and health; the area banner, haul chips, map and control hints go.
        void TrimHud()
        {
            foreach (var cls in new[] { "dhud__where", "dhud__chips", "dhud__mapbox", "dhud__help" })
                _ui.Root.Query(className: cls).ForEach(e => e.style.display = DisplayStyle.None);
        }

        // ───────────────────────── the throwaway profile ─────────────────────────
        /// Moves still being made: they live here (not in the content) until they're ready to port.
        public const string VerdantCrescent = "lab_verdant_crescent", CinderCrescent = "lab_cinder_crescent", StormEdge = "lab_storm_edge", RadiantSurge = "lab_radiant_surge";
        public const string TidalCleave = "lab_tidal_cleave", FrostLance = "lab_frost_lance", VoidRift = "lab_void_rift", Sunbreaker = "lab_sunbreaker", PetalFlurry = "lab_petal_flurry",
                            EmberMantle = "lab_ember_mantle", FrostAegis = "lab_frost_aegis", GaleRush = "lab_gale_rush", CrimsonHunger = "lab_crimson_hunger", AstralCrown = "lab_astral_crown";
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
            if (c.Move(CinderCrescent) == null)
                c.moves.Add(new MoveData
                {
                    id = CinderCrescent, name = "Cinder Crescent", emoji = "🔥", anyClass = true,
                    description = "Throw a crescent slash of fire that flies out, cooling from white-hot to deep red. Lv 2: three in a fan, burning the floor. Lv 3: five, and the middle one bursts into a ring of fire.",
                    slot = MoveSlot.Skill, kind = MoveKind.CinderCrescent, element = Element.Fire,
                    power = 1.4f, cooldown = 4f, radius = 1.5f, range = 5f,
                    lightColour = "FF9A3C", lightIntensity = 0.9f, lightRadius = 4f,
                });
            if (c.Move(StormEdge) == null)
                c.moves.Add(new MoveData
                {
                    id = StormEdge, name = "Storm Edge", emoji = "⚡", anyClass = true,
                    description = "Lightning runs into your weapon: you glow white-blue and every hit lands harder and crackles. Lv 2: hits arc to another monster. Lv 3: arcs to two, and pulses of static hit everything round you.",
                    slot = MoveSlot.Skill, kind = MoveKind.StormEdge, element = Element.Light,
                    power = 1f, cooldown = 12f, duration = 6f, value = 0.35f, radius = 1f, range = 1f,
                    lightColour = "BFE6FF", lightIntensity = 1.2f, lightRadius = 4f,
                });
            if (c.Move(RadiantSurge) == null)
                c.moves.Add(new MoveData
                {
                    id = RadiantSurge, name = "Radiant Surge", emoji = "🌟", anyClass = true,
                    description = "Burst into light: your whole body glows, rings of light orbit you, and you hit harder and take less. Lv 2: longer, stronger, three rings. Lv 3: four rings, and when it ends the light bursts out of you.",
                    slot = MoveSlot.Skill, kind = MoveKind.RadiantSurge, element = Element.Light,
                    power = 1f, cooldown = 14f, duration = 6f, radius = 1f, range = 1f,
                    lightColour = "D8ECFF", lightIntensity = 1.2f, lightRadius = 4f,
                });

            // the second batch: five attacks and five buffs
            AddLab(c, TidalCleave, "Tidal Cleave", "🌊", MoveKind.TidalCleave, Element.Water, 1.5f, 5f, 2.1f, 4.5f, 0f,
                   "A great overhead cleave that throws a wave forward, white foam cooling to deep blue. Lv 2: a second wave. Lv 3: the waves end in a whirlpool that drags monsters in.", "4FD8FF");
            AddLab(c, FrostLance, "Frost Lance", "🧊", MoveKind.FrostLance, Element.Frost, 1.7f, 3.5f, 0.5f, 5f, 0f,
                   "Lunge and thrust a long lance of ice-light that pierces everything in line. Lv 2: two more lances fan out. Lv 3: the line freezes into crystals that shatter.", "9FE6FF");
            AddLab(c, VoidRift, "Void Rift", "🌀", MoveKind.VoidRift, Element.None, 1.6f, 6f, 1.5f, 6f, 0f,
                   "Tear a rift open on the monster you aim at: it sucks everything in, then bursts violet. Lv 2: bigger. Lv 3: two more rifts open nearby.", "A64DFF");
            AddLab(c, Sunbreaker, "Sunbreaker", "🔨", MoveKind.Sunbreaker, Element.Light, 1.8f, 7f, 2.2f, 1f, 0f,
                   "Hammer the ground: golden shockwaves roll out and cracks of light split the floor. Lv 2: a second wave. Lv 3: a third, and pillars of sunlight fall on everything hit.", "FFC24D");
            AddLab(c, PetalFlurry, "Petal Flurry", "🌸", MoveKind.PetalFlurry, Element.Nature, 1.2f, 4f, 1.5f, 1f, 0f,
                   "Twin blades in a blur of quick slashes, shedding pink petals. Lv 2: five slashes. Lv 3: seven, and a finishing cross-cut.", "FF7AB8");
            AddLab(c, EmberMantle, "Ember Mantle", "🌋", MoveKind.EmberMantle, Element.Fire, 1f, 12f, 1.7f, 1f, 6f,
                   "Wrap yourself in fire: rings of flame pulse out and burn what's close. Lv 2: wider and hotter. Lv 3: your footsteps leave fire behind.", "FF7A1A");
            AddLab(c, FrostAegis, "Frost Aegis", "❄️", MoveKind.FrostAegis, Element.Frost, 1f, 14f, 1f, 1f, 10f,
                   "Shards of ice circle you, each taking a hit for you. Lv 2: four, and they burst outwards when they break. Lv 3: five, and any left fly at monsters at the end.", "9FE6FF");
            AddLab(c, GaleRush, "Gale Rush", "💨", MoveKind.GaleRush, Element.Nature, 1f, 12f, 1f, 1f, 6f,
                   "The wind takes you: much faster, after-images and wind streaks, a quicker dash. Lv 2: the wind cuts monsters you run past. Lv 3: a gust on cast throws them back.", "7AFFD6");
            AddLab(c, CrimsonHunger, "Crimson Hunger", "🩸", MoveKind.CrimsonHunger, Element.None, 1f, 14f, 1f, 1f, 6f,
                   "A red hunger: hit harder, and every hit heals you a little. Lv 2: more. Lv 3: more again, and kills make it last longer.", "FF2A44");
            AddLab(c, AstralCrown, "Astral Crown", "✨", MoveKind.AstralCrown, Element.Light, 1f, 15f, 1f, 1f, 8f,
                   "A crown of stars circles your head and each fires starlight at the nearest monster. Lv 2: four stars, faster. Lv 3: five, and every bolt bursts.", "FFD06A");
        }

        static void AddLab(GameContent c, string id, string name, string emoji, MoveKind kind, Element el, float power, float cooldown, float radius, float range, float duration, string text, string light)
        {
            if (c.Move(id) != null) return;
            c.moves.Add(new MoveData
            {
                id = id, name = name, emoji = emoji, anyClass = true, description = text,
                slot = MoveSlot.Skill, kind = kind, element = el,
                power = power, cooldown = cooldown, radius = radius, range = range, duration = duration,
                lightColour = light, lightIntensity = 0.9f, lightRadius = 4f,
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
            foreach (var cd in C.classes) Progression.Equip(C, P, cd.id, LoadSlot.Skill2, VerdantCrescent);   // the newest lab move, ready on Q
        }

        /// Put back what was set up last time: area, class, loadouts, gear, trinkets, meals and skill levels.
        void ApplySetupPrefs()
        {
            var C = S.Content; var P = S.Profile;
            if (C.Route(_p.area) == null && C.routes.Count > 0) _p.area = C.routes[0].id;
            ClassId cls;
            if (!string.IsNullOrEmpty(_p.cls) && System.Enum.TryParse(_p.cls, out cls) && S.ClassAvailable(cls)) S.SelectClass(cls);
            foreach (var lo in _p.loadouts)
            {
                ClassId c;
                if (!System.Enum.TryParse(lo.cls, out c)) continue;
                for (int i = 0; i < lo.ids.Count; i++)
                    if (i == (int)LoadSlot.Skill2 || !string.IsNullOrEmpty(lo.ids[i])) Progression.Equip(C, P, c, (LoadSlot)i, lo.ids[i]);
            }
            if (_p.boots >= 0) P.bootsTier = _p.boots;
            if (_p.lantern >= 0) P.lanternTier = _p.lantern;
            if (_p.bag >= 0) P.bagTier = _p.bag;
            if (_p.trinketsSet) { P.equippedTrinkets.Clear(); foreach (var t in _p.trinkets) if (C.trinkets.Exists(x => x.id == t)) P.equippedTrinkets.Add(t); }
            if (!_p.mealsSet) { _p.meals.Clear(); foreach (var m in P.packed) _p.meals.Add(new CookedMeal { recipeId = m.recipeId, grade = m.grade, accelId = m.accelId }); _p.mealsSet = true; }
            _p.meals.RemoveAll(m => C.Recipe(m.recipeId) == null);
        }

        /// Remember the class and its loadout (after any change on the Setup tab).
        void RememberLoadout()
        {
            var P = S.Profile;
            _p.cls = P.selectedClass.ToString();
            var cp = P.Progress(P.selectedClass);
            var lo = _p.loadouts.Find(x => x.cls == _p.cls);
            if (lo == null) _p.loadouts.Add(lo = new LabLoadout { cls = _p.cls });
            lo.ids.Clear();
            foreach (LoadSlot s in System.Enum.GetValues(typeof(LoadSlot))) lo.ids.Add(Progression.Equipped(cp, s));
            Changed();
        }

        // ───────────────────────── runs ─────────────────────────
        /// Back to camp (whatever's going on) and straight out again with the lab's area, floor and meals.
        void Restart()
        {
            Home();
            if (S.Phase != Phase.Hearth) return;
            if (S.Hearth != null)   // a calm, plain day: no storm, no Rich / Lucky / Dangerous area rolls
            {
                S.Hearth.campEvent = CampEvent.None;
                foreach (var o in S.Hearth.routeOffers) o.mod = RouteMod.None;
            }
            var P = S.Profile;
            P.packed.Clear();
            foreach (var m in _p.meals) P.packed.Add(new CookedMeal { recipeId = m.recipeId, grade = m.grade, accelId = m.accelId });
            Cooking.SyncTray(P);
            var route = S.Content.Route(_p.area);   // a class-locked area (only a Ranger can enter…): go in as that class
            if (route != null && Routes.ClassBlock(S.Content, route, P.selectedClass) != null && S.ClassAvailable(route.requiredClass))
            {
                S.SelectClass(route.requiredClass);
                _ui.Juice.Toast("Lab: " + route.name + " is " + route.requiredClass + "-only — switched class", null, 3f);
                RememberLoadout();
                if (_setupBody != null) BuildSetup();
            }
            DungeonController.LabArena = _p.arena;
            if (!S.StartRun(_p.area, 1)) { _ui.Juice.Toast("Lab: couldn't set out for " + _p.area, null, 3f); return; }
            while (S.Run != null && S.Run.floor < _p.floor && S.DungeonDescend()) { }   // any floor, not just the shortcut ones
            _runEndAt = -1f;
            foreach (var l in _p.levels) S.SetMoveLevel(l.moveId, l.level);
            ResetTargets();
            if (DC != null && DC.Player != null)
            {
                DungeonCamera.LabFollow = DC.Player.transform;
                if (_p.arena && DC.CurrentRoom != null) DC.Player.Teleport(DC.CurrentRoom.NearestWalkable(Anchor + new Vector2(-3.5f, 0f)));   // the left of the arena
            }
            if (_skillsBody != null) BuildSkillList();
        }

        /// Loadout and class changes are camp-only in the game: step home first (Restart sets out again).
        void Home()
        {
            if (S.Phase == Phase.Dungeon || S.Phase == Phase.Gather || S.Phase == Phase.Rest) S.FleeHome();
            if (S.Phase == Phase.RunEnd) S.ReturnToHearth();
        }

        List<int> Floors()
        {
            var list = new List<int>();
            int n = _p.area == GameSession.FallRoute ? 1 : DungeonRules.Floors(S.Content, _p.area);
            for (int f = 1; f <= n; f++) list.Add(f);
            return list;
        }

        // ───────────────────────── every frame ─────────────────────────
        void Update()
        {
            Keys();
            if (Ready)
            {
                var run = S.Run; var st = S.Stats;
                if (_p.autoHeal && run != null && st != null && run.hp < st.maxHp) run.hp = st.maxHp;
                if (_p.noCooldowns) DC.Player.SetCooldowns(new float[4]);
                KeepTargets();
                AutoCast();
            }
            // died (cheats off) or left by a ladder home: back out after a moment
            if (S.Phase == Phase.RunEnd)
            {
                if (_runEndAt < 0f) _runEndAt = Time.unscaledTime;
                else if (Time.unscaledTime - _runEndAt > 2.5f) Restart();
            }
            else if (S.Phase == Phase.Hearth) Restart();   // never sit in camp
        }

        void LateUpdate()
        {
            if (_stepping && Time.frameCount > _stepFrame)   // one frame has run: hold still again
            {
                _stepping = false;
                GameFeel.LabPaused = true; Time.timeScale = 0f;
            }
        }

        void Keys()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (k.rKey.wasPressedThisFrame) { if (RingOpen) CloseRing(); else OpenRing(); }
            if (RingOpen) return;
            if (k.pKey.wasPressedThisFrame) SetPaused(!_paused);
            if (k.periodKey.wasPressedThisFrame || k.numpadPeriodKey.wasPressedThisFrame) StepFrame();
            if (k.tKey.wasPressedThisFrame) SetAuto(!_p.autoOn);
            if (k.minusKey.wasPressedThisFrame || k.numpadMinusKey.wasPressedThisFrame) StepSpeed(-1);
            if (k.equalsKey.wasPressedThisFrame || k.numpadPlusKey.wasPressedThisFrame) StepSpeed(1);
            if (k.f1Key.wasPressedThisFrame) ShowPanel(!_panelShown);
            if (k.f2Key.wasPressedThisFrame && Ready) SpawnDummy(SpawnPoint());
            if (k.f3Key.wasPressedThisFrame && Ready) SpawnMonster(SpawnPoint(), false);
            if (k.f4Key.wasPressedThisFrame) KillAll();
            if (k.f5Key.wasPressedThisFrame) Restart();
        }

        // ───────────────────────── time ─────────────────────────
        void SetSpeed(float s)
        {
            _p.speed = Mathf.Clamp(s, 0.05f, 2f);
            GameFeel.LabScale = _p.speed;
            if (_speedSlider != null) _speedSlider.SetValueWithoutNotify(_p.speed);
            Changed();
        }

        void StepSpeed(int dir)
        {
            int at = 0;
            for (int i = 0; i < SpeedSteps.Length; i++) if (SpeedSteps[i] <= _p.speed + 0.001f) at = i;
            if (dir < 0 && SpeedSteps[at] >= _p.speed - 0.001f) at--;
            else if (dir > 0) at++;
            SetSpeed(SpeedSteps[Mathf.Clamp(at, 0, SpeedSteps.Length - 1)]);
        }

        void SetPaused(bool paused)
        {
            _paused = paused; _stepping = false;
            GameFeel.LabPaused = paused || RingOpen;
            if (_pauseBtn != null) _pauseBtn.text = paused ? "▶ Play (P)" : "⏸ Pause (P)";
        }

        /// Paused: run exactly one frame of game time, then hold still again (pauses first if it was running).
        void StepFrame()
        {
            if (!_paused) { SetPaused(true); return; }
            _stepping = true; _stepFrame = Time.frameCount;
            GameFeel.LabPaused = false; Time.timeScale = GameFeel.LabScale;
        }

        // ───────────────────────── view ─────────────────────────
        void ApplyLight() { RoomLighting.LabLight = _p.light == 1 ? 1.1f : _p.light == 2 ? 0.08f : -1f; }

        void SetZoom(float z)
        {
            _p.zoom = Mathf.Clamp(z, 0.4f, 1.4f);
            DungeonCamera.LabZoom = _p.zoom;
            Changed();
        }
    }
}
