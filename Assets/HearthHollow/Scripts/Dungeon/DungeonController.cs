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
        float _ambT;   // ambient embers
        float _torchCd;   // torch throw cooldown
        // room light: dark while there's something to fight, lights up (after a pause) in calm / cleared rooms
        Light2D _global; float _darkI, _litI, _lightLevel, _lightTarget, _lightUpAt = -1f;
        float _roomMult = 1f, _roomMultTarget = 1f;   // boss rooms are darker until the boss is down
        float _lightBaseI, _mealSteady, _flareCd;
        float _glowAdd, _glowDecay;   // big moves flood the room with light for a moment (class rework)      // your carried light (fed by meals) + Flare
        float _doorLockUntil;
        Light2D _playerLight;
        float _lightBaseR;
        /// The Emberfist's Heat (0..1): her carried light grows from a candle to a bonfire.
        public float HeatGlow;

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
            if (_theme != null) MakePlayerLight();   // always: the rooms are dark now

            // camera
            if (_cam == null) _cam = Camera.main;
            if (_cam != null)
            {
                if (!_camSaved) { _camHome = _cam.transform.position; _camHomeSize = _cam.orthographicSize; _camHomeBg = _cam.backgroundColor; _camSaved = true; }
                _cam.orthographicSize = Settings.cameraSize;
                if (_theme != null) _cam.backgroundColor = _theme.background;
            }
            EnterRoom(start, true);
            GameLook.ForTheme(_theme);   // this route's colour grade + the tilt-shift
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
            light.intensity = _theme.lightIntensity * Settings.roomLight;   // dark rooms: light comes from you, your moves and the fire
            // Darkness hazard: the world goes nearly black; glowcap dishes bring the light back
            if (Session.DungeonHazard == Hazard.Darkness)
                light.intensity *= Mathf.Lerp(Session.Content.tuning.darkLight, 1f, Session.DungeonLight);
            // darker the deeper you go (step 5): floor 1 as set, floor 2 dusky, floor 3 properly dark; caves start darker
            int floorNo = Session.Run != null ? Mathf.Max(1, Session.Run.floor) : 1;
            var fl = Settings.floorLight;
            float depth = fl != null && fl.Length > 0 ? fl[Mathf.Min(floorNo - 1, fl.Length - 1)] : 1f;
            bool cave = _theme.ambient == RoomGlow.Mushrooms || _theme.ambient == RoomGlow.Crystals || _theme.ambient == RoomGlow.LavaCracks;
            light.intensity *= depth * (cave ? Settings.caveLight : 1f);
            ApplyToAllLayers(light);
            _global = light; _darkI = light.intensity;
            _roomMult = _roomMultTarget = 1f;
            _litI = _darkI * Settings.litRoomBoost;
            _lightLevel = 0f; _lightTarget = 0f; _lightUpAt = -1f;
        }

        void MakePlayerLight()
        {
            var lgo = new GameObject("Player Light");
            lgo.transform.SetParent(Player.transform, false);
            lgo.transform.localPosition = new Vector3(0, 0.5f, 0);
            _playerLight = lgo.AddComponent<Light2D>();
            _playerLight.lightType = Light2D.LightType.Point;
            _playerLight.color = Color.Lerp(new Color(1f, 0.76f, 0.48f), PlayerController.ClassColour(Player.ClassId), Settings.carriedLightClassTint);   // firelight, tinted by your class colour
            _playerLight.intensity = Mathf.Max(_theme.playerLight, Settings.carriedLight);
            _playerLight.pointLightOuterRadius = Settings.carriedLightRadius;
            // packed meals feed your light (step 5): Sandwich = wider, BBQ = hotter + redder, Pot = steadier; finer meals count more
            float reach = 0f, heat = 0f, steady = 0f;
            if (Session.Run != null)
                foreach (var m in Session.Run.eaten)
                {
                    var r = m != null ? Session.Content.Recipe(m.recipeId) : null;
                    if (r == null) continue;
                    float g = (m.grade >= 2 ? 1.5f : m.grade == 1 ? 1.25f : 1f) * (m.potency > 0f ? Mathf.Clamp(m.potency, 0.3f, 1.5f) : 1f);
                    if (r.station == Station.Pot) steady += g; else if (r.station == Station.BBQ) heat += g; else reach += g;
                }
            _mealSteady = steady;
            _playerLight.pointLightOuterRadius *= 1f + Settings.mealReach * reach;
            _playerLight.intensity *= 1f + Settings.mealHeat * heat;
            _playerLight.color = Color.Lerp(_playerLight.color, new Color(1f, 0.5f, 0.25f), Mathf.Clamp01(heat * 0.12f));
            if (reach + heat + steady > 0.01f && Player != null)
            {
                var parts = new List<string>();
                if (reach > 0.01f) parts.Add("wider"); if (heat > 0.01f) parts.Add("hotter"); if (steady > 0.01f) parts.Add("steadier");
                DungeonHud.Number(Player.Body + Vector2.up * 1f, "🔥 Your meals: light " + string.Join(" · ", parts.ToArray()), "dnum--info");
            }
            _playerLight.pointLightInnerRadius = 0.3f;
            _playerLight.falloffIntensity = Settings.carriedLightFalloff;   // reaches far, fades gradually
            CastShadows(_playerLight);   // pillars throw shadows away from you
            // walls pick the light up from further away than the ground
            var wgo = new GameObject("Wall Glow");
            wgo.transform.SetParent(lgo.transform, false);
            var wl = wgo.AddComponent<Light2D>();
            wl.lightType = Light2D.LightType.Point;
            wl.color = _playerLight.color;
            wl.intensity = Settings.wallGlow;
            wl.pointLightOuterRadius = Settings.carriedLightRadius * Settings.wallGlowReach;
            wl.pointLightInnerRadius = 0.5f;
            wl.falloffIntensity = Settings.wallGlowFalloff;
            ApplyToLayers(wl, "Walls");
            if (Session.DungeonHazard == Hazard.Darkness)
            {
                // you only see as far as your light reaches; glowcap dishes widen it
                var t = Session.Content.tuning;
                _playerLight.intensity = Mathf.Max(_theme.playerLight, 1.15f);
                _playerLight.pointLightOuterRadius = Mathf.Lerp(t.darkRadiusMin, t.darkRadiusMax, Session.DungeonLight);
                _playerLight.pointLightInnerRadius = _playerLight.pointLightOuterRadius * 0.35f;
            }
            ApplyToAllLayers(_playerLight);
            _lightBaseI = _playerLight.intensity;   // the flicker in Update works round this
            _lightBaseR = _playerLight.pointLightOuterRadius;
        }

        // ───────── light plan step 3: normal maps + shadows ─────────
        /// How high lights sit above the ground for normal mapping (higher = flatter, softer shading).
        public static float LightHeight = 2.4f;

        /// Every light reads the sprites' normal maps (Hearth & Hollow > Generate Normal Maps), so it wraps round shapes.
        public static void UseNormals(Light2D light)
        {
            if (light == null || light.lightType == Light2D.LightType.Global) return;
            var q = typeof(Light2D).GetField("m_NormalMapQuality", BindingFlags.NonPublic | BindingFlags.Instance);
            var d = typeof(Light2D).GetField("m_NormalMapDistance", BindingFlags.NonPublic | BindingFlags.Instance);
            if (q != null) q.SetValue(light, System.Enum.Parse(q.FieldType, "Accurate"));
            if (d != null) d.SetValue(light, LightHeight);
        }

        /// This light is blocked by walls and pillars (their ShadowCaster2D). Use on a few big lights, not every spark.
        public static void CastShadows(Light2D light, float intensity = 0.85f, float softness = 0.35f)
        {
            if (light == null) return;
            light.shadowsEnabled = true;
            light.shadowIntensity = intensity;
            light.shadowSoftness = softness;
        }

        /// Walls and pillars block light: a shadow caster shaped by the walls tilemap's collider. Shadows fall on the
        /// ground and on characters, not on the walls themselves (they keep their wall glow).
        public static void AddWallShadows(UnityEngine.Tilemaps.Tilemap walls)
        {
            if (walls == null) return;
            var col = walls.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>();
            if (col == null || walls.GetComponent<ShadowCaster2D>() != null) return;
            var sc = walls.gameObject.AddComponent<ShadowCaster2D>();
            var bf = BindingFlags.NonPublic | BindingFlags.Instance;
            var src = typeof(ShadowCaster2D).GetField("m_ShadowCastingSource", bf);
            var comp = typeof(ShadowCaster2D).GetField("m_ShadowShape2DComponent", bf);
            if (src != null) src.SetValue(sc, System.Enum.Parse(src.FieldType, "ShapeProvider"));
            if (comp != null) comp.SetValue(sc, col);
            sc.castingOption = ShadowCaster2D.ShadowCastingOptions.CastShadow;
            var layers = typeof(ShadowCaster2D).GetField("m_ApplyToSortingLayers", bf);
            if (layers != null)
            {
                var ids = new List<int>();
                foreach (var n in new[] { "Ground", "Default" }) { int id = SortingLayer.NameToID(n); if (SortingLayer.IsValid(id)) ids.Add(id); }
                layers.SetValue(sc, ids.ToArray());
            }
            sc.enabled = false; sc.enabled = true;   // rebuild with the new source
        }

        /// Lights only the named sorting layers (e.g. "Walls" for a glow that reaches walls but not the floor).
        public static void ApplyToLayers(Light2D light, params string[] names)
        {
            UseNormals(light);
            var f = typeof(Light2D).GetField("m_ApplyToSortingLayers", BindingFlags.NonPublic | BindingFlags.Instance);
            if (f == null) return;
            var ids = new List<int>();
            foreach (var n in names) { int id = SortingLayer.NameToID(n); if (SortingLayer.IsValid(id)) ids.Add(id); }
            f.SetValue(light, ids.ToArray());
        }

        public static void ApplyToAllLayers(Light2D light)
        {
            UseNormals(light);
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
            GameFeel.Reset();
            GameLook.Release("dungeon");
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
            _roomMultTarget = room.Node.kind == RoomKind.Boss && !room.Cleared ? Settings.bossRoomLight : 1f;
            // nothing to fight here: a brief pause, then the room lights up. A fight: back to the dark.
            if (!room.HasEvent || room.Cleared)
            {
                if (_lightTarget < 1f && _lightUpAt < 0f) _lightUpAt = Time.time + Settings.lightUpDelay;
                // every brazier in a peaceful room lights up as you walk in (after the same short pause), even when the
                // room light is already up from the room before
                if (room.Lights != null) room.Lights.IgniteWave(Player != null ? Player.Feet : (Vector2)(room.Origin + RoomTemplate.Centre), Settings.lightUpDelay);
            }
            else { _lightTarget = 0f; _lightUpAt = -1f; }
        }

        /// Flare: a big burst of warm white light round you for a few seconds (casts shadows, lights nearby braziers).
        /// Later steps hang more on it (stuns shadow creatures, boosts light damage, reveals secrets).
        /// No fight on: the room is calm or cleared (Flare is cheap then).
        bool OutOfFight { get { return _current != null && (!_current.HasEvent || _current.Cleared) && !_current.EventRunning; } }

        /// A big move floods the room with light for a moment (class rework).
        public void FloodLight(float add, float seconds) { if (add > _glowAdd) { _glowAdd = add; _glowDecay = add / Mathf.Max(0.1f, seconds); } }

        void Flare()
        {
            _flareCd = OutOfFight ? Settings.flareCalmCooldown : Mathf.Max(Settings.flareMinCooldown, Settings.flareCooldown - Settings.mealSteady * _mealSteady);
            var col = new Color(1f, 0.92f, 0.75f);
            var l = DungeonFx.Flash(Player.Body, col, Settings.flareRadius, Settings.flareIntensity, Settings.flareSeconds, Player.transform);
            CastShadows(l, 0.85f, 0.3f);
            DungeonFx.RingAt(Player.Body, col, 3.5f, 0.5f);
            DungeonFx.Embers(Player.Body, new Color(1f, 0.75f, 0.4f), 30, 0.8f);
            for (int i = 0; i < 24; i++) { Vector2 o = Random.insideUnitCircle.normalized; DungeonFx.Throw(Player.Body, o, col, 1, 4f, 9f); }
            GameLook.Punch(0.5f);
            if (_current != null && _current.Lights != null) _current.Lights.IgniteNear(Player.Feet, Settings.flareRadius * 0.5f, !OutOfFight);   // a cheap out-of-fight Flare doesn't solve brazier puzzles
            if (_current != null && _current.Secrets != null) _current.Secrets.Reveal(Player.Body, Settings.flareRadius, Settings.secretRevealSeconds);   // shows secrets (step 7)
            // shadow creatures caught in it are stunned (step 6)
            if (_current != null)
                foreach (var e in _current.Enemies)
                    if (e != null && !e.Dead && e.Shadow && Vector2.Distance(e.Body, Player.Body) <= Settings.flareRadius * Settings.flareStunReach)
                        e.Stun(Settings.flareStunSeconds);
            // a light hit for Break (no damage) on everything close by (step 8)
            if (_current != null)
                foreach (var e in new List<EnemyController>(_current.Enemies))
                    if (e != null && !e.Dead && Vector2.Distance(e.Body, Player.Body) <= Settings.flareRadius * Settings.flareStunReach) e.ShieldHit(HitType.Light, 1);
        }

        /// Ramps the room light towards its target; fires the light-up when the pause is over.
        void UpdateRoomLight()
        {
            if (_global == null) return;
            if (_lightUpAt >= 0f && Time.time >= _lightUpAt)
            {
                _lightUpAt = -1f; _lightTarget = 1f;
                if (_current != null)
                {
                    // a soft whoomph of warm light from the middle of the room, and embers rising
                    Vector3 c = (Vector3)(_current.Origin + RoomTemplate.Centre);
                    DungeonFx.Flash(c, new Color(1f, 0.8f, 0.5f), 14f, 1.2f, Settings.lightUpSeconds * 1.2f);
                    DungeonFx.Embers(c, new Color(1f, 0.65f, 0.3f), 24, 4f);
                    if (_current.Lights != null) _current.Lights.IgniteWave(c);
                }
            }
            bool up = _lightTarget > _lightLevel;
            _lightLevel = Mathf.MoveTowards(_lightLevel, _lightTarget, Time.deltaTime / Mathf.Max(0.05f, up ? Settings.lightUpSeconds : 0.35f));
            _roomMult = Mathf.MoveTowards(_roomMult, _roomMultTarget, Time.deltaTime * 0.8f);
            _global.intensity = Mathf.Lerp(_darkI, _litI, Mathf.SmoothStep(0f, 1f, _lightLevel)) * _roomMult;
            _glowAdd = Mathf.MoveTowards(_glowAdd, 0f, _glowDecay * Time.deltaTime);
            _global.intensity += _glowAdd;
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
            _roomMultTarget = 1f;
            if (room == _current) _lightUpAt = Time.time + Settings.lightUpDelay;   // cleared: the room lights up too
            if (room.Lights != null) room.Lights.IgniteWave(room.LastKill != Vector2.zero ? room.LastKill : (Vector2)(room.Origin + RoomTemplate.Centre));   // braziers flare on from the last kill
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
            // shadow creatures: more the deeper you go, more in caves; never bosses or elites
            if (!boss && !elite && Settings.shadowChance != null && Settings.shadowChance.Length > 0)
            {
                int fl = Session.Run != null ? Mathf.Max(1, Session.Run.floor) : 1;
                float ch = Settings.shadowChance[Mathf.Min(fl - 1, Settings.shadowChance.Length - 1)];
                if (_theme != null && (_theme.ambient == RoomGlow.Mushrooms || _theme.ambient == RoomGlow.Crystals || _theme.ambient == RoomGlow.LavaCracks)) ch *= 1.5f;
                if (Random.value < ch) e.MakeShadow();
            }
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

        /// A ladder: ask whether to go deeper or head home with the haul (the dungeon pauses meanwhile).
        public void AskLadder(Interactable ladder)
        {
            if (_ui == null) { if (Session.DungeonDescend()) ladder.MarkUsed(); return; }
            _ui.OpenLadderChoice(() => { if (Session.DungeonDescend()) ladder.MarkUsed(); }, () => Session.DungeonExit());
        }

        public void Shake(float seconds, float amp) { _shakeT = Mathf.Max(_shakeT, seconds); _shakeAmp = Mathf.Max(_shakeAmp, amp); }

        // ───────────────────────── frame ─────────────────────────
        void Update()
        {
            bool hasFloor = _root != null && Player != null;
            bool settings = _ui != null && (_ui.SettingsOpen || _ui.LadderChoiceOpen);   // paused while a menu / the ladder choice is open
            if (hasFloor && Session.Phase == Phase.Dungeon && !settings && DungeonInput.Bag) DungeonHud.BagOpen = !DungeonHud.BagOpen;
            if (!hasFloor || Session.Phase != Phase.Dungeon) DungeonHud.BagOpen = false;
            GameplayActive = hasFloor && Session.Phase == Phase.Dungeon && !settings && !DungeonHud.BagOpen;
            if (_root != null) Time.timeScale = GameplayActive ? GameFeel.TimeScale : 0f;   // hit-stop / slow motion
            if (_root != null) DungeonLight.Refresh();
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

            if (GameplayActive) UpdateRoomLight();

            // torches: T / (LB) throws one where you're aiming — unlimited, on a cooldown; each burns out after a while
            if (GameplayActive)
            {
                if (_torchCd > 0f) _torchCd -= Time.deltaTime;
                if (DungeonInput.Torch && Player != null && _torchCd <= 0f)
                {
                    _torchCd = Settings.torchCooldown;
                    ThrownTorch.Throw(this, Player.Body, Player.Aim, Settings.torchRange, Settings.torchSeconds);
                }
            }
            DungeonHud.TorchCooldown = Mathf.Max(0f, _torchCd);

            // your light flickers like a flame (Pot meals steady it); Flare (F / LT): a burst of light round you
            if (_playerLight != null && _lightBaseI > 0f)
            {
                float amp = 0.08f / (1f + _mealSteady);
                float tt = Time.time;
                _playerLight.intensity = _lightBaseI * (1f + amp * (Mathf.Sin(tt * 7.3f) * 0.6f + Mathf.Sin(tt * 17.1f) * 0.4f)) * (1f + HeatGlow * Settings.heatLightBright);
                if (_lightBaseR > 0f) _playerLight.pointLightOuterRadius = _lightBaseR * (1f + HeatGlow * Settings.heatLightReach);
            }
            if (GameplayActive)
            {
                if (_flareCd > Settings.flareCalmCooldown && OutOfFight) _flareCd = Settings.flareCalmCooldown;   // the fight's over: Flare is nearly ready again
            if (_flareCd > 0f) _flareCd -= Time.deltaTime;
                if (DungeonInput.Flare && Player != null && _flareCd <= 0f) Flare();
            }
            DungeonHud.FlareCooldown = Mathf.Max(0f, _flareCd);
            DungeonHud.Torch01 = Settings.torchCooldown > 0f ? 1f - Mathf.Clamp01(_torchCd / Settings.torchCooldown) : 1f;

            // embers drifting through the dark room (atmosphere)
            if (GameplayActive && _current != null)
            {
                _ambT -= Time.deltaTime;
                if (_ambT <= 0f)
                {
                    _ambT = Random.Range(0.08f, 0.2f);
                    var at = _current.Origin + new Vector2(Random.Range(1f, RoomTemplate.Width - 1f), Random.Range(1f, RoomTemplate.Height - 3f));
                    var fx = DungeonFx.Spawn(DungeonFx.Pixel, at, new Color(1f, Random.Range(0.45f, 0.75f), 0.2f, Random.Range(0.35f, 0.8f)), Random.Range(2f, 3.5f), Random.Range(2f, 3.5f), 0.5f, 0f, 20600, true);
                    fx.velocity = new Vector2(Random.Range(-0.25f, 0.25f), Random.Range(0.2f, 0.5f));
                    fx.drag = 0f;
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
