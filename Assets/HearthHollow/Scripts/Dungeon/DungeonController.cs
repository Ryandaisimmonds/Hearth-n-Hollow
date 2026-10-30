// Hearth & Hollow — runs the real-time dungeon. Watches GameSession: when a dungeon run is active it builds
// the current floor (generator → room prefabs → theme), spawns the player, moves the camera room to room and
// pauses everything while a menu / minigame / the bag is open. Tears the floor down when the run ends.
using System.Collections.Generic;
using System.Reflection;
using HearthHollow.Core;
using HearthHollow.Data;
using HearthHollow.UI;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Dungeon
{
    public class DungeonController : MonoBehaviour
    {
        public static DungeonController Instance { get; private set; }

        public GameSession Session { get; private set; }
        public DungeonSettings Settings { get; private set; }
        public PlayerController Player { get; private set; }
        public GameObject FloorRoot { get { return _root; } }
        public DungeonTheme Theme { get { return _theme; } }
        public RoomInstance CurrentRoom { get { return _current; } }
        /// False while paused (menus, minigames, bag) or when there is no floor.
        public bool GameplayActive { get; private set; }

        GameUI _ui;
        FloorPlan _plan;
        DungeonTheme _theme;
        GameObject _root;
        string _builtKey;
        readonly Dictionary<RoomNode, RoomInstance> _rooms = new Dictionary<RoomNode, RoomInstance>();
        RoomInstance _current;

        Camera _cam;
        bool _camSaved; Vector3 _camHome; float _camHomeSize; Color _camHomeBg;
        Vector3 _camTarget;
        float _shakeT, _shakeAmp;
        float _doorLockUntil;
        Light2D _playerLight;

        public static int SortOrder(float y) { return 10000 - Mathf.RoundToInt(y * 10f); }

        public void Init(GameSession session, GameUI ui, DungeonSettings settings)
        {
            Instance = this;
            Session = session; _ui = ui; Settings = settings;
            _cam = Camera.main;
            Session.Changed += OnChanged;
            OnChanged();
        }

        void OnDestroy()
        {
            if (Session != null) Session.Changed -= OnChanged;
            if (_root != null) Teardown();
            Time.timeScale = 1f;
            if (Instance == this) Instance = null;
        }

        // ───────────────────────── session → world ─────────────────────────
        void OnChanged()
        {
            var run = Session.Run;
            Phase p = Session.Phase;
            bool want = Settings != null && run != null && run.dungeon && (p == Phase.Dungeon || p == Phase.Gather || p == Phase.Rest);
            if (!want) { if (_root != null) Teardown(); return; }
            string key = run.dungeonSeed + "/" + run.floor;
            if (_root == null || key != _builtKey) BuildFloor(run, key);
        }

        float[] _carryCd; int _carrySeed;
        float _hazardT, _stillFor;

        void BuildFloor(RunState run, string key)
        {
            // going down a ladder keeps skill cooldowns ticking (no free reset by changing floor)
            if (Player != null) { _carryCd = Player.Cooldowns; _carrySeed = run.dungeonSeed; }
            else if (_carrySeed != run.dungeonSeed) _carryCd = null;
            if (_root != null) DestroyFloor();
            DungeonHud.Reset();
            DungeonHud.Active = true;
            _builtKey = key;

            var C = Session.Content;
            var T = C.tuning;
            var route = C.Route(run.routeId);
            int seed = unchecked(run.dungeonSeed * 31 + run.floor * 7919);
            var rng = new System.Random(seed);
            int count = rng.Next(T.roomsPerFloorMin, Mathf.Max(T.roomsPerFloorMin, T.roomsPerFloorMax) + 1) + (run.floor - 1);
            _plan = DungeonGenerator.Generate(seed, count, Session.DungeonOnLastFloor, run.floor, route != null ? route.gatherChance : 0.2f);
            _theme = Settings.ThemeFor(run.routeId);

            _root = new GameObject("Dungeon · " + (route != null ? route.name : run.routeId) + " · floor " + run.floor);
            MakeLights();

            _rooms.Clear();
            foreach (var node in _plan.rooms)
            {
                var tpl = PickTemplate(node.kind, rng);
                if (tpl == null) { Debug.LogError("[Hearth & Hollow] No room prefabs — run Hearth & Hollow > Build Dungeon Kit."); continue; }
                var go = Instantiate(tpl.gameObject, RoomOrigin(node.cell), Quaternion.identity, _root.transform);
                go.name = "Room " + node.index + " · " + node.kind + " (" + tpl.name + ")";
                var ri = go.AddComponent<RoomInstance>();
                ri.Setup(this, node, _theme, rng.Next());
                _rooms[node] = ri;
            }

            RoomInstance start;
            if (!_rooms.TryGetValue(_plan.start, out start))
            {
                // no usable room prefabs: don't sit paused on an empty floor — send the player home with their bag
                Debug.LogError("[Hearth & Hollow] Couldn't build the dungeon floor (no room prefabs for '" + run.routeId + "'). Check Dungeon Settings → Rooms, or run Hearth & Hollow > Build Dungeon Kit.");
                Teardown();
                Session.FleeHome();
                return;
            }

            // player
            var pgo = new GameObject("Player");
            pgo.transform.SetParent(_root.transform, false);
            Player = pgo.AddComponent<PlayerController>();
            Player.Setup(this, run.classId, ArtLibrary.GetClass(run.classId.ToString()));
            Player.Teleport(start.PlayerStart);
            if (_carryCd != null && _carrySeed == run.dungeonSeed) Player.SetCooldowns(_carryCd);
            _carryCd = null;
            if (_theme != null && (_theme.playerLight > 0f || Session.DungeonHazard == Hazard.Darkness)) MakePlayerLight();

            // camera
            if (_cam == null) _cam = Camera.main;
            if (_cam != null)
            {
                if (!_camSaved) { _camHome = _cam.transform.position; _camHomeSize = _cam.orthographicSize; _camHomeBg = _cam.backgroundColor; _camSaved = true; }
                _cam.orthographicSize = Settings.cameraSize;
                if (_theme != null) _cam.backgroundColor = _theme.background;
            }
            EnterRoom(start, true);
        }

        RoomTemplate PickTemplate(RoomKind kind, System.Random rng)
        {
            var pool = new List<RoomTemplate>();
            if (_theme != null) pool.AddRange(_theme.rooms);
            if (_theme == null || _theme.rooms.Count == 0 || _theme.alsoUseSharedRooms) pool.AddRange(Settings.rooms);
            var pick = Weighted(pool, kind, rng);
            if (pick == null && kind != RoomKind.Combat)
            {
                Debug.LogWarning("[Hearth & Hollow] No room prefab is marked usable as " + kind + " for theme '" + (_theme != null ? _theme.name : "none") + "' — using a Combat room instead. Tick " + kind + " on a RoomTemplate (and give it the matching SpawnPoint).");
                pick = Weighted(pool, RoomKind.Combat, rng);
            }
            if (pick == null) foreach (var r in pool) if (r != null) { pick = r; break; }
            return pick;
        }

        static RoomTemplate Weighted(List<RoomTemplate> pool, RoomKind kind, System.Random rng)
        {
            int total = 0;
            foreach (var r in pool) if (r != null && r.weight > 0 && r.usableAs.Contains(kind)) total += r.weight;
            if (total <= 0) return null;
            int roll = rng.Next(total);
            foreach (var r in pool)
            {
                if (r == null || r.weight <= 0 || !r.usableAs.Contains(kind)) continue;
                roll -= r.weight;
                if (roll < 0) return r;
            }
            return null;
        }

        static Vector3 RoomOrigin(Vector2Int cell) { return new Vector3(cell.x * (RoomTemplate.Width + 9), cell.y * (RoomTemplate.Height + 9), 0); }

        void MakeLights()
        {
            if (_theme == null) return;
            var lgo = new GameObject("Global Light");
            lgo.transform.SetParent(_root.transform, false);
            var light = lgo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.color = _theme.lightColor;
            light.intensity = _theme.lightIntensity;
            // Darkness hazard: the world goes nearly black; glowcap dishes bring the light back
            if (Session.DungeonHazard == Hazard.Darkness)
                light.intensity *= Mathf.Lerp(Session.Content.tuning.darkLight, 1f, Session.DungeonLight);
            ApplyToAllLayers(light);
        }

        void MakePlayerLight()
        {
            var lgo = new GameObject("Player Light");
            lgo.transform.SetParent(Player.transform, false);
            lgo.transform.localPosition = new Vector3(0, 0.5f, 0);
            _playerLight = lgo.AddComponent<Light2D>();
            _playerLight.lightType = Light2D.LightType.Point;
            _playerLight.color = new Color(1f, 0.85f, 0.6f);
            _playerLight.intensity = _theme.playerLight;
            _playerLight.pointLightOuterRadius = 6f;
            _playerLight.pointLightInnerRadius = 1f;
            if (Session.DungeonHazard == Hazard.Darkness)
            {
                // you only see as far as your light reaches; glowcap dishes widen it
                var t = Session.Content.tuning;
                _playerLight.intensity = Mathf.Max(_theme.playerLight, 1.15f);
                _playerLight.pointLightOuterRadius = Mathf.Lerp(t.darkRadiusMin, t.darkRadiusMax, Session.DungeonLight);
                _playerLight.pointLightInnerRadius = _playerLight.pointLightOuterRadius * 0.35f;
            }
            ApplyToAllLayers(_playerLight);
        }

        public static void ApplyToAllLayers(Light2D light)
        {
            var f = typeof(Light2D).GetField("m_ApplyToSortingLayers", BindingFlags.NonPublic | BindingFlags.Instance);
            if (f == null) return;
            var layers = SortingLayer.layers;
            var ids = new int[layers.Length];
            for (int i = 0; i < layers.Length; i++) ids[i] = layers[i].id;
            f.SetValue(light, ids);
        }

        void DestroyFloor()
        {
            if (_root != null) Destroy(_root);
            _root = null; Player = null; _current = null; _playerLight = null;
            _rooms.Clear();
        }

        void Teardown()
        {
            DestroyFloor();
            _builtKey = null;
            GameplayActive = false;
            Time.timeScale = 1f;
            Session.DungeonGuardMult = 1f;
            DungeonHud.Reset();
            if (_cam != null && _camSaved)
            {
                _cam.transform.position = _camHome; _cam.orthographicSize = _camHomeSize; _cam.backgroundColor = _camHomeBg;
                _camSaved = false;
            }
        }

        // ───────────────────────── rooms ─────────────────────────
        void EnterRoom(RoomInstance room, bool snap)
        {
            _current = room;
            _camTarget = (Vector3)(room.Origin + RoomTemplate.Centre) + new Vector3(0, 0.35f, 0);
            _camTarget.z = _cam != null ? _cam.transform.position.z : -10f;
            if (snap && _cam != null) _cam.transform.position = _camTarget;
            room.OnEnter();
            RefreshMap();
        }

        public void GoThrough(RoomInstance room, Side side)
        {
            if (room != _current || !GameplayActive || Time.unscaledTime < _doorLockUntil || _plan == null) return;
            var n = _plan.Neighbour(room.Node, side);
            RoomInstance next;
            if (n == null || !_rooms.TryGetValue(n, out next)) return;
            _doorLockUntil = Time.unscaledTime + 0.3f;
            foreach (var p in _root.GetComponentsInChildren<Projectile>()) Destroy(p.gameObject);
            Player.Teleport(next.Origin + RoomTemplate.EntryPoint(RoomTemplate.Opposite(side)));
            EnterRoom(next, false);
        }

        public void OnRoomCleared(RoomInstance room)
        {
            Session.DungeonRoomCleared();
            DungeonFx.RingAt(Player != null ? (Vector3)Player.Body : (Vector3)(room.Origin + RoomTemplate.Centre), new Color(1f, 0.95f, 0.7f, 0.7f), 3f, 0.6f);
            RefreshMap();
        }

        public EnemyController SpawnEnemy(RoomInstance room, string monsterId, bool elite, bool boss, Vector2 at)
        {
            if (string.IsNullOrEmpty(monsterId)) return null;
            var stats = Session.DungeonEnemy(monsterId, elite, boss);
            var profile = Settings.ProfileFor(monsterId);
            var go = new GameObject((boss ? "BOSS " : elite ? "Elite " : "") + monsterId);
            go.transform.SetParent(room.transform, false);
            go.transform.position = at;
            var e = go.AddComponent<EnemyController>();
            e.Setup(this, room, stats, profile, ArtLibrary.GetMonster(monsterId), (_theme != null ? _theme.enemySpeedMult : 1f) * Session.MutatorEnemySpeed);
            float scale = Session.MutatorEnemyScale;   // Giants
            if (scale != 1f) go.transform.localScale = new Vector3(scale, scale, 1f);
            DungeonFx.RingAt(at + new Vector2(0, 0.3f), boss ? new Color(1f, 0.4f, 0.2f, 0.9f) : new Color(1f, 0.5f, 0.45f, 0.7f), boss ? 2f : 0.9f, 0.6f);
            return e;
        }

        // ───────────────────────── interaction ─────────────────────────
        Interactable Nearest(PlayerController p, float range)
        {
            if (_current == null || p == null) return null;
            Interactable best = null; float bd = range;
            foreach (var it in _current.Props)
            {
                if (it == null || it.Used || !it.Available) continue;
                float d = Vector2.Distance(p.Feet, it.transform.position);
                if (d < bd) { bd = d; best = it; }
            }
            return best;
        }

        public void TryInteract(PlayerController p)
        {
            if (!GameplayActive) return;
            var it = Nearest(p, 1.4f);
            if (it != null) it.Use(this);
        }

        public void Shake(float seconds, float amp) { _shakeT = Mathf.Max(_shakeT, seconds); _shakeAmp = Mathf.Max(_shakeAmp, amp); }

        // ───────────────────────── frame ─────────────────────────
        void Update()
        {
            bool hasFloor = _root != null && Player != null;
            bool settings = _ui != null && _ui.SettingsOpen;
            if (hasFloor && Session.Phase == Phase.Dungeon && !settings && DungeonInput.Bag) DungeonHud.BagOpen = !DungeonHud.BagOpen;
            if (!hasFloor || Session.Phase != Phase.Dungeon) DungeonHud.BagOpen = false;
            GameplayActive = hasFloor && Session.Phase == Phase.Dungeon && !settings && !DungeonHud.BagOpen;
            if (_root != null) Time.timeScale = GameplayActive ? 1f : 0f;
            if (!hasFloor) return;

            // dungeon hazard (poison / cold / heat): a bite every few seconds, softened by counter meals
            if (GameplayActive && Session.DungeonHazard != Hazard.None)
            {
                _stillFor = Player != null && !Player.Moving ? _stillFor + Time.deltaTime : 0f;
                _hazardT += Time.deltaTime;
                if (_hazardT >= Mathf.Max(0.25f, Session.Content.tuning.hazardTickSeconds))
                {
                    _hazardT = 0f;
                    int lost = Session.DungeonHazardTick(_stillFor >= 0.5f);   // Heat: standing still burns twice as fast
                    if (lost > 0 && Player != null) DungeonHud.Number(Player.transform.position + Vector3.up * 1.3f, "-" + lost + " " + Routes.HazardEmoji(Session.DungeonHazard), "dnum--hazard");
                }
            }

            // prompt + status for the HUD
            var near = GameplayActive ? Nearest(Player, 1.4f) : null;
            DungeonHud.Prompt = near != null ? near.Prompt(this) : null;
            DungeonHud.Locked = _current != null && _current.EventRunning;
            DungeonHud.Status = StatusText();
            if (_current != null && _current.Boss != null && !_current.Boss.Dead)
            {
                DungeonHud.BossHp01 = _current.Boss.Hp / (float)Mathf.Max(1, _current.Boss.Stats.maxHp);
                DungeonHud.BossName = _current.Boss.DisplayName;
            }
            else DungeonHud.BossHp01 = -1f;
        }

        string StatusText()
        {
            if (_current == null) return null;
            if (_current.EventRunning)
            {
                int left = 0; foreach (var e in _current.Enemies) if (e != null && !e.Dead) left++;
                return _current.Node.kind == RoomKind.Boss ? "Defeat the guardian!" : left > 0 ? "Clear the room!  " + left + " left" : "Clear the room!";
            }
            switch (_current.Node.kind)
            {
                case RoomKind.Ladder: return "A ladder leads deeper";
                case RoomKind.Exit: return "A way home";
                case RoomKind.Boss: return "The way out is open";
                case RoomKind.Gather: return "A quiet grove — forage here";
                case RoomKind.Rest: return "A campfire crackles";
            }
            return null;
        }

        void LateUpdate()
        {
            if (_cam == null || _root == null) return;
            float dt = Time.unscaledDeltaTime;
            Vector3 pos = Vector3.Lerp(_cam.transform.position, _camTarget, 1f - Mathf.Exp(-dt * 14f));
            pos.z = _camTarget.z;
            if (_shakeT > 0f && GameplayActive)
            {
                _shakeT -= dt;
                pos += (Vector3)(Random.insideUnitCircle * _shakeAmp);
                if (_shakeT <= 0f) _shakeAmp = 0f;
            }
            _cam.transform.position = pos;
        }

        void RefreshMap()
        {
            DungeonHud.Map.Clear();
            if (_plan == null) return;
            var known = new HashSet<RoomNode>();
            foreach (var kv in _rooms)
            {
                if (!kv.Value.Visited) continue;
                known.Add(kv.Key);
                foreach (Side s in System.Enum.GetValues(typeof(Side))) { var n = _plan.Neighbour(kv.Key, s); if (n != null) known.Add(n); }
            }
            foreach (var node in known)
            {
                RoomInstance ri; _rooms.TryGetValue(node, out ri);
                DungeonHud.Map.Add(new DungeonHud.MapRoom
                {
                    cell = node.cell, kind = node.kind,
                    visited = ri != null && ri.Visited, current = ri == _current, cleared = ri != null && ri.Cleared, known = true,
                    n = node.Has(Side.North), s = node.Has(Side.South), e = node.Has(Side.East), w = node.Has(Side.West)
                });
            }
            DungeonHud.MapVersion++;
        }
    }
}
