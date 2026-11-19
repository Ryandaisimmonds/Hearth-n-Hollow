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
    public partial class DungeonController : MonoBehaviour   // what it tells the HUD: DungeonController.Hud.cs
    {
        public static DungeonController Instance { get; private set; }

        /// What the HUD shows (a fresh one for each floor). DungeonScreen reads it.
        public DungeonHudState Hud { get; private set; } = new DungeonHudState();
        /// Floating text at a world position (DungeonHud.Number); the HUD screen draws it.
        public event System.Action<Vector3, string, string> NumberShown;
        /// The player was hurt (the HUD flashes the screen).
        public event System.Action PlayerHurt;
        public void ShowNumber(Vector3 world, string text, string cls) { if (NumberShown != null) NumberShown(world, text, cls); }
        public void RaisePlayerHurt() { if (PlayerHurt != null) PlayerHurt(); }

        public GameSession Session { get; private set; }
        public DungeonSettings Settings { get; private set; }
        public PlayerController Player { get; private set; }
        public GameObject FloorRoot { get { return _root; } }
        public DungeonTheme Theme { get { return _theme; } }
        public RoomInstance CurrentRoom { get { return _current; } }
        FloorLootView _loot;
        FallGuide _fall;       // the opening fall: its fog and its how-to-play lines
        /// False while paused (menus, minigames, bag) or when there is no floor.
        public bool GameplayActive { get; private set; }

        GameUI _ui;
        public GameUI UI { get { return _ui; } }
        FloorPlan _plan;
        DungeonTheme _theme;
        GameObject _root;
        string _builtKey;
        readonly Dictionary<RoomNode, RoomInstance> _rooms = new Dictionary<RoomNode, RoomInstance>();
        RoomInstance _current;

        readonly DungeonCamera _camera = new DungeonCamera();   // follows the room, shakes, pushes in on a boss
        BossIntro _intro;                                         // a boss's entrance
        float _ambT;   // ambient embers
        RoomLighting _light;   // the room's light and the light you carry
        float _flareCd;
        float _doorLockUntil;

        public static int SortOrder(float y) { return 10000 - Mathf.RoundToInt(y * 10f); }

        /// Effects Lab: every floor is one calm room (DungeonGenerator.Arena).
        public static bool LabArena;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetLab() { LabArena = false; DungeonCamera.LabFollow = null; DungeonCamera.LabZoom = 1f; RoomLighting.LabLight = -1f; }   // domain reload is off

        public void Init(GameSession session, GameUI ui, DungeonSettings settings)
        {
            Instance = this;
            if (GetComponent<AreaAtmosphere>() == null) gameObject.AddComponent<AreaAtmosphere>();   // each area's air
            Session = session; _ui = ui; Settings = settings;
            _loot = gameObject.AddComponent<FloorLootView>(); _loot.Init(this);   // loot on the floor
            _intro = new BossIntro(this, _camera);
            _fall = new FallGuide(this);
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
            Hud = new DungeonHudState { Active = true };
            _regenRate = -1f; _regenAcc = 0f; _calmHealT = 0f;   // re-read meal Regen each floor
            _builtKey = key;

            var C = Session.Content;
            var T = C.tuning;
            var route = C.Route(run.routeId);
            int seed = unchecked(run.dungeonSeed * 31 + run.floor * 7919);
            var rng = new System.Random(seed);
            int count = rng.Next(T.roomsPerFloorMin, Mathf.Max(T.roomsPerFloorMin, T.roomsPerFloorMax) + 1) + DungeonRules.ExtraRooms(run.floor);
            bool fall = run.routeId == GameSession.FallRoute;   // the nameless opening dungeon
            _plan = LabArena ? DungeonGenerator.Arena()   // the Effects Lab's single room
                 : fall ? DungeonGenerator.Gauntlet(seed, T.fallRooms)
                         : DungeonGenerator.Generate(seed, count, Session.DungeonBossFloor && !Session.DungeonNoLadder, Session.DungeonHomeChoice,
                                                     Session.DungeonNoLadder || Session.DungeonEndExit,   // a boss-free area's last floor ends in an exit
                                                     run.floor, route != null ? route.gatherChance : 0.2f);
            _theme = Settings.ThemeFor(fall ? Ids.Area.Heart : run.routeId);   // it borrows the Hollow Heart's purple gloom

            _root = new GameObject("Dungeon · " + (route != null ? route.name : run.routeId) + " · floor " + run.floor);
            _light = new RoomLighting(this);
            _light.MakeRoomLight(_root.transform, _theme);

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
            if (_theme != null) _light.MakePlayerLight(Player, _theme);   // always: the rooms are dark now

            _camera.Setup(Settings.cameraSize, _theme);
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

        void DestroyFloor()
        {
            if (_root != null) Destroy(_root);
            _root = null; Player = null; _current = null; _light = null;
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
            Hud = new DungeonHudState();
            _camera.Restore();
        }

        // ───────────────────────── rooms ─────────────────────────
        void EnterRoom(RoomInstance room, bool snap)
        {
            _current = room;
            _camera.LookAt((Vector3)(room.Origin + RoomTemplate.Centre) + new Vector3(0, 0.35f, 0), snap);
            room.OnEnter();
            RefreshMap();
            float dark = room.Node.kind == RoomKind.Boss && !room.Cleared ? Settings.bossRoomLight : 1f;
            if (Session.InFall) dark *= _fall.EnterRoom(room);   // the place turns on you: darker and foggier room by room
            if (_light != null) _light.RoomMultTarget = dark;
            // nothing to fight here: a brief pause, then the room lights up. A fight: back to the dark.
            bool calm = !room.HasEvent || room.Cleared;
            if (_light != null) _light.Enter(calm, Settings.lightUpDelay);
            if (calm)
            {
                // every brazier in a peaceful room lights up as you walk in (after the same short pause), even when the
                // room light is already up from the room before
                if (room.Lights != null) room.Lights.IgniteWave(Player != null ? Player.Feet : (Vector2)(room.Origin + RoomTemplate.Centre), Settings.lightUpDelay);
            }
        }

        /// A big move floods the room with light for a moment.
        public void FloodLight(float add, float seconds) { if (_light != null) _light.FloodLight(add, seconds); }

        /// Flare: a big burst of warm white light round you for a few seconds (casts shadows, lights nearby braziers).
        /// Later steps hang more on it (stuns shadow creatures, boosts light damage, reveals secrets).
        /// No fight on: the room is calm or cleared (Flare is cheap then).
        bool OutOfFight { get { return _current != null && (!_current.HasEvent || _current.Cleared) && !_current.EventRunning; } }

        // ── meals that heal over time (Regen) ──
        float _calmHealT, _regenAcc, _regenRate = -1f;
        void SlowHealing(float dt)
        {
            var run = Session.Run; var st = Session.Stats;
            if (run == null || st == null || Player == null || run.hp <= 0 || run.hp >= st.maxHp) { _calmHealT = 0f; return; }
            if (_regenRate < 0f) { var m = Session.Mods; _regenRate = m != null ? m.regen : 0f; }
            // meals with Regen: always, fights included
            if (_regenRate > 0f)
            {
                _regenAcc += _regenRate * dt / 10f;
                if (_regenAcc >= 1f) { int h = Session.DungeonHealPlayer((int)_regenAcc); _regenAcc -= (int)_regenAcc; if (h > 0) DungeonHud.Number(Player.Body + Vector2.up * 0.7f, "+" + h, "dnum--heal"); }
            }
            // (there's no out-of-fight trickle: clearing a room heals instead, GameSession.DungeonRoomCleared)
        }

        /// A monster went down: the Ember Locket takes time off Flare's cooldown.
        public void OnEnemyKilled()
        {
            float cut = Gear.Perk(Session.Content, Session.Profile, TrinketPerk.FlareOnKill);
            if (cut > 0f && _flareCd > 0f) _flareCd = Mathf.Max(0f, _flareCd - cut);
        }

        /// Flare's reach, widened by the Lamp-maker.
        public float FlareRadius { get { return Settings.flareRadius * (1f + LampMaker.FlareRadius(Session.Content, Session.Profile)); } }

        void Flare()
        {
            float lampCut = LampMaker.FlareCooldownCut(Session.Content, Session.Profile);   // the Lamp-maker
            _flareCd = OutOfFight ? Settings.flareCalmCooldown : Mathf.Max(Settings.flareMinCooldown - lampCut * 0.5f, Settings.flareCooldown - Settings.mealSteady * _light.MealSteady - lampCut);
            float flareR = FlareRadius;
            var col = new Color(1f, 0.92f, 0.75f);
            var l = DungeonFx.Flash(Player.Body, col, flareR, Settings.flareIntensity, Settings.flareSeconds, Player.transform);
            LightSetup.CastShadows(l, 0.85f, 0.3f);
            DungeonFx.RingAt(Player.Body, col, 3.5f, 0.5f);
            DungeonFx.Embers(Player.Body, new Color(1f, 0.75f, 0.4f), 30, 0.8f);
            for (int i = 0; i < 24; i++) { Vector2 o = Random.insideUnitCircle.normalized; DungeonFx.Throw(Player.Body, o, col, 1, 4f, 9f); }
            GameLook.Punch(0.5f);
            if (_current != null && _current.Lights != null) _current.Lights.IgniteNear(Player.Feet, flareR * 0.5f, !OutOfFight);   // a cheap out-of-fight Flare doesn't solve brazier puzzles
            if (_current != null && _current.Secrets != null) _current.Secrets.Reveal(Player.Body, flareR, Settings.secretRevealSeconds);   // shows secrets (step 7)
            // shadow creatures caught in it are stunned (step 6)
            if (_current != null)
                foreach (var e in _current.Enemies)
                    if (e != null && !e.Dead && e.Shadow && Vector2.Distance(e.Body, Player.Body) <= flareR * Settings.flareStunReach)
                        e.Stun(Settings.flareStunSeconds);
            // a light hit for Break (no damage) on everything close by (step 8)
            if (_current != null)
                foreach (var e in new List<EnemyController>(_current.Enemies))
                    if (e != null && !e.Dead && Vector2.Distance(e.Body, Player.Body) <= flareR * Settings.flareStunReach) e.ElementContact(Element.Light);
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
            int hpBefore = Session.Run != null ? Session.Run.hp : 0;
            Session.DungeonRoomCleared();   // clearing a room heals a share of your max HP
            int healed = Session.Run != null ? Session.Run.hp - hpBefore : 0;
            if (healed > 0 && Player != null) DungeonHud.Number(Player.Body + Vector2.up * 0.9f, "💗 +" + healed, "dnum--heal");
            if (_light != null) _light.Cleared(room == _current);   // cleared: the room lights up too
            if (room.Lights != null) room.Lights.IgniteWave(room.LastKill != Vector2.zero ? room.LastKill : (Vector2)(room.Origin + RoomTemplate.Centre));   // braziers flare on from the last kill
            DungeonFx.RingAt(Player != null ? (Vector3)Player.Body : (Vector3)(room.Origin + RoomTemplate.Centre), new Color(1f, 0.95f, 0.7f, 0.7f), 3f, 0.6f);
            HearthHollow.Runtime.GameAudio.Play("room_clear");
            RefreshMap();
        }

        public EnemyController SpawnEnemy(RoomInstance room, string monsterId, bool elite, bool boss, Vector2 at, EnemyStats given = null)
        {
            if (string.IsNullOrEmpty(monsterId)) return null;
            var stats = given ?? Session.DungeonEnemy(monsterId, elite, boss);   // split slimes bring their own
            var profile = Settings.ProfileFor(monsterId);
            var go = new GameObject((boss ? "BOSS " : elite ? "Elite " : "") + monsterId);
            go.transform.SetParent(room.transform, false);
            go.transform.position = at;
            var e = go.AddComponent<EnemyController>();
            var md = Session.Content.Monster(monsterId);
            string artId = md != null && !string.IsNullOrEmpty(md.artId) ? md.artId : monsterId;   // named bosses borrow a base monster's sprites
            var art = boss ? (ArtLibrary.GetBoss(artId) ?? ArtLibrary.GetMonster(artId)) : ArtLibrary.GetMonster(artId);   // bosses get their own big sprite
            e.Setup(this, room, stats, profile, art, (_theme != null ? _theme.enemySpeedMult : 1f) * Session.MutatorEnemySpeed * Session.Story.FallEnemySpeed);   // the fall: faster each room
            // shadow creatures: more the deeper you go, more in caves; never bosses or elites
            if (!boss && !elite && given == null && Settings.shadowChance != null && Settings.shadowChance.Length > 0)
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
            float ld = 99f; var floorItem = _loot != null ? _loot.Nearest(p.Feet, 1.2f, out ld) : null;
            if (floorItem != null && (it == null || ld <= Vector2.Distance(p.Feet, it.transform.position)))
            {
                if (Session.DungeonPickUp(floorItem)) Hud.BagOpen = true;   // the bag's full: swap something out
                return;
            }
            var dig = NearestDig(p.Feet);   // a buried chest underfoot
            if (dig != null && it == null) { dig.Dig(this); return; }
            if (it != null) it.Use(this);
        }

        /// A ladder: ask whether to go deeper or head home with the haul (the dungeon pauses meanwhile).
        public void AskLadder(Interactable ladder)
        {
            if (Session.DungeonDescend()) ladder.MarkUsed();   // a ladder only goes down — the way home is a portal or a boss's exit
            return;
            _ui.OpenLadderChoice(() => { if (Session.DungeonDescend()) ladder.MarkUsed(); }, () => Session.DungeonExit());
        }

        /// A buried chest in reach in this room, or null.
        HiddenForage NearestDig(Vector2 feet)
        {
            if (_current == null || _current.Secrets == null) return null;
            foreach (var f in _current.Secrets.Forage) if (f != null && f.InReach(feet)) return f;
            return null;
        }

        /// The fall's last room: no monsters. The fog pours in and drains you until you fall.
        public void StartFogRoom(RoomInstance room) { StartCoroutine(_fall.FogRoom()); }

        public void Shake(float seconds, float amp) { _camera.Shake(seconds, amp); }

        // ───────────────────────── frame ─────────────────────────
        void Update()
        {
            bool hasFloor = _root != null && Player != null;
            bool settings = _ui != null && (_ui.SettingsOpen || _ui.LadderChoiceOpen);   // paused while a menu / the ladder choice is open
            if (hasFloor && Session.Phase == Phase.Dungeon && !settings && DungeonInput.Bag) Hud.BagOpen = !Hud.BagOpen;
            if (!hasFloor || Session.Phase != Phase.Dungeon) Hud.BagOpen = false;
            GameplayActive = hasFloor && Session.Phase == Phase.Dungeon && !settings && !Hud.BagOpen;
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

            if (GameplayActive && _light != null) _light.Tick(_current);

            if (_light != null) _light.Flicker();   // your light flickers like a flame; Flare (F / LT): a burst of light round you
            if (GameplayActive)
            {
                if (_flareCd > Settings.flareCalmCooldown && OutOfFight) _flareCd = Settings.flareCalmCooldown;   // the fight's over: Flare is nearly ready again
            if (_flareCd > 0f) _flareCd -= Time.deltaTime;
                if (DungeonInput.Flare && Player != null && _flareCd <= 0f) Flare();
                SlowHealing(Time.deltaTime);
            }
            Hud.FlareCooldown = Mathf.Max(0f, _flareCd);
            _fall.Tick();   // the fall's tutorial lines and its fog

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

            WriteHud();   // the prompt, the room's status and the boss bar (DungeonController.Hud.cs)
        }

        public void BossEntrance(RoomInstance room, EnemyController boss) { _intro.Begin(room, boss); }

        void LateUpdate()
        {
            if (_root == null) return;
            float dt = Time.unscaledDeltaTime;
            _intro.Tick(dt);
            _camera.Tick(dt, _intro.Push, _intro.Boss, GameplayActive);
        }
    }
}
