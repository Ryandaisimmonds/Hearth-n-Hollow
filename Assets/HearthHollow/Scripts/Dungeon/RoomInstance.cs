// Hearth & Hollow — one room of the current floor, made from a RoomTemplate prefab.
// On setup it wears the theme (marker tiles → theme tiles, tints, decor), seals unused doors,
// adds gates on used ones and places its props. Event rooms shut their doors until every enemy is down.
using System.Collections;
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace HearthHollow.Dungeon
{
    public class RoomInstance : MonoBehaviour
    {
        public DungeonController Controller { get; private set; }
        public RoomNode Node { get; private set; }
        public RoomTemplate Template { get; private set; }
        public bool Visited { get; private set; }
        public bool Cleared { get; private set; }
        public bool EventRunning { get; private set; }
        public Vector2 Origin { get { return transform.position; } }
        public IList<EnemyController> Enemies { get { return _enemies; } }
        public IList<Interactable> Props { get { return _props; } }
        public EnemyController Boss { get; private set; }
        public RoomLights Lights { get; private set; }   // braziers, the route's glow, torch pickups (step 4)
        public Vector2 LastKill { get; private set; }
        public RoomSecrets Secrets { get; private set; }   // hidden forage, cracked walls, brazier puzzles (step 7)

        readonly DoorGate[] _doors = new DoorGate[4];
        readonly List<EnemyController> _enemies = new List<EnemyController>();
        readonly List<Interactable> _props = new List<Interactable>();
        readonly List<Interactable> _hidden = new List<Interactable>();   // revealed when cleared
        System.Random _rng;
        DungeonTheme _theme;
        bool _spawning;

        public bool HasEvent
        {
            get { return Node.kind == RoomKind.Combat || Node.kind == RoomKind.Ladder || Node.kind == RoomKind.Exit || Node.kind == RoomKind.Boss; }
        }

        public void Setup(DungeonController dc, RoomNode node, DungeonTheme theme, int seed)
        {
            Controller = dc; Node = node; _theme = theme;
            Template = GetComponent<RoomTemplate>();
            _rng = new System.Random(seed);
            ApplyTheme();
            BuildDoors();
            PlaceProps();
            Lights = gameObject.AddComponent<RoomLights>();
            Lights.Setup(this, theme, seed);
            Secrets = gameObject.AddComponent<RoomSecrets>();
            Secrets.Setup(this, theme, seed, dc.Session.Run != null ? dc.Session.Run.floor : 1);
            Cleared = !HasEvent;
        }

        // ───────────────────────── look ─────────────────────────
        void ApplyTheme()
        {
            var set = Controller.Settings;
            Tilemap[] maps = { Template.floor, Template.walls, Template.decor };
            foreach (var tm in maps)
            {
                if (tm == null) continue;
                Swap(tm, set.markerFloor, _theme != null ? _theme.floor : null);
                Swap(tm, set.markerWall, _theme != null ? _theme.wall : null);
                Swap(tm, set.markerWallFace, _theme != null ? (_theme.wallFace != null ? _theme.wallFace : _theme.wall) : null);
                Swap(tm, set.markerHazard, _theme != null ? _theme.hazard : null);
            }
            if (_theme == null) return;

            var floor = Template.floor; var walls = Template.walls; var decor = Template.decor;
            if (floor != null)
            {
                floor.color = _theme.floorTint;
                var fr = floor.GetComponent<TilemapRenderer>(); if (fr != null) { fr.sortingOrder = 0; SetLayer(fr, "Ground"); }
            }
            if (walls != null)
            {
                walls.color = _theme.wallTint;
                var wr = walls.GetComponent<TilemapRenderer>(); if (wr != null) { wr.sortingOrder = 2; SetLayer(wr, "Walls"); }   // own layer: walls catch light from further away
                DungeonController.AddWallShadows(walls);   // walls and pillars block light
            }
            if (decor != null)
            {
                decor.color = _theme.floorTint;
                var dr = decor.GetComponent<TilemapRenderer>(); if (dr != null) { dr.sortingOrder = 1; SetLayer(dr, "Ground"); }
            }

            if (floor == null) return;
            for (int y = 1; y <= RoomTemplate.Height - 3; y++)
                for (int x = 1; x <= RoomTemplate.Width - 2; x++)
                {
                    var p = new Vector3Int(x, y, 0);
                    if (_theme.floorAlt != null && floor.GetTile(p) == _theme.floor && _rng.NextDouble() < _theme.floorAltChance)
                        floor.SetTile(p, _theme.floorAlt);
                    if (decor != null && _theme.decor.Count > 0 && IsWalkable(p) && !NearDoor(p) && decor.GetTile(p) == null
                        && _rng.NextDouble() < _theme.decorDensity)
                    {
                        var d = _theme.decor[_rng.Next(_theme.decor.Count)];
                        if (d != null) decor.SetTile(p, d);
                    }
                }
        }

        static void SetLayer(Renderer r, string layer)
        {
            int id = SortingLayer.NameToID(layer);
            if (SortingLayer.IsValid(id)) r.sortingLayerID = id;
        }

        static void Swap(Tilemap tm, TileBase marker, TileBase replacement)
        {
            if (tm != null && marker != null && replacement != null && marker != replacement) tm.SwapTile(marker, replacement);
        }

        public bool NearDoor(Vector3Int p)
        {
            int cx = RoomTemplate.Width / 2, cy = (RoomTemplate.Height - 3) / 2 + 1;
            if (Mathf.Abs(p.x - cx) <= 2 && (p.y <= 2 || p.y >= RoomTemplate.Height - 4)) return true;
            if (Mathf.Abs(p.y - cy) <= 2 && (p.x <= 2 || p.x >= RoomTemplate.Width - 3)) return true;
            return false;
        }

        public bool IsWalkable(Vector3Int p)
        {
            if (Template.floor == null || Template.floor.GetTile(p) == null) return false;
            return Template.walls == null || Template.walls.GetTile(p) == null;
        }

        // ───────────────────────── doors ─────────────────────────
        void BuildDoors()
        {
            var set = Controller.Settings;
            TileBase wall = _theme != null && _theme.wall != null ? _theme.wall : set.markerWall;
            TileBase face = _theme != null && _theme.wallFace != null ? _theme.wallFace : (set.markerWallFace != null ? set.markerWallFace : wall);
            foreach (Side side in System.Enum.GetValues(typeof(Side)))
            {
                if (!Node.Has(side))
                {
                    if (Template.walls == null) continue;
                    foreach (var c in RoomTemplate.DoorCells(side))
                        Template.walls.SetTile(c, side == Side.North && c.y == RoomTemplate.Height - 2 ? face : wall);
                    continue;
                }
                var go = new GameObject("Door " + side);
                go.transform.SetParent(transform, false);
                var gate = go.AddComponent<DoorGate>();
                gate.Setup(this, side, _theme != null ? _theme.doorOpen : null, _theme != null ? _theme.doorClosed : null);
                _doors[(int)side] = gate;
            }
        }

        public void SetDoors(bool open)
        {
            foreach (var d in _doors) if (d != null) d.SetOpen(open);
        }

        // ───────────────────────── props ─────────────────────────
        void PlaceProps()
        {
            switch (Node.kind)
            {
                case RoomKind.Gather:
                {
                    var pts = Points(SpawnKind.Forage);
                    int n = Mathf.Clamp(pts.Count, 2, 3);
                    for (int i = 0; i < n; i++)
                        AddProp(InteractKind.Forage, i < pts.Count ? pts[i] : RandomFloorPoint(Origin + RoomTemplate.Centre, 0f), true);
                    break;
                }
                case RoomKind.Rest:
                    AddProp(InteractKind.Campfire, FirstPoint(SpawnKind.Campfire), true);
                    break;
                case RoomKind.Ladder:
                    AddProp(InteractKind.Ladder, FirstPoint(SpawnKind.Ladder), false);
                    break;
                case RoomKind.Exit:
                case RoomKind.Boss:
                    AddProp(InteractKind.Exit, FirstPoint(SpawnKind.Exit), false);
                    break;
                case RoomKind.Combat:
                    if (_rng.NextDouble() < 0.25)   // the odd bonus forage spot after a fight
                    {
                        var pts = Points(SpawnKind.Forage);
                        AddProp(InteractKind.Forage, pts.Count > 0 ? pts[0] : RandomFloorPoint(Origin + RoomTemplate.Centre, 2f), false);
                    }
                    break;
            }
        }

        void AddProp(InteractKind kind, Vector2 world, bool available)
        {
            var go = new GameObject(kind.ToString());
            go.transform.SetParent(transform, false);
            go.transform.position = world;
            var it = go.AddComponent<Interactable>();
            Sprite sp = null;
            if (_theme != null)
                sp = kind == InteractKind.Forage ? _theme.forage : kind == InteractKind.Campfire ? _theme.campfire : kind == InteractKind.Ladder ? _theme.ladder : _theme.exitPortal;
            it.Setup(this, kind, sp, available);
            _props.Add(it);
            if (!available) _hidden.Add(it);
        }

        List<Vector2> Points(SpawnKind kind)
        {
            var list = new List<Vector2>();
            foreach (var sp in Template.SpawnPoints(kind)) list.Add(sp.transform.position);
            Shuffle(list);
            return list;
        }

        Vector2 FirstPoint(SpawnKind kind)
        {
            var pts = Points(kind);
            if (pts.Count > 0) return pts[0];
            if (kind != SpawnKind.Enemy) Debug.LogWarning("[Hearth & Hollow] Room '" + Template.name + "' has no " + kind + " spawn point — placing it on the nearest open floor to the centre. Add a SpawnPoint to the prefab.");
            return NearestWalkable(Origin + RoomTemplate.Centre);
        }

        /// The walkable cell centre closest to <paramref name="world"/> (searches outward ring by ring).
        public Vector2 NearestWalkable(Vector2 world)
        {
            Vector2 local = world - Origin;
            var c = new Vector3Int(Mathf.FloorToInt(local.x), Mathf.FloorToInt(local.y), 0);
            for (int r = 0; r < RoomTemplate.Width; r++)
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                        var p = new Vector3Int(c.x + dx, c.y + dy, 0);
                        if (p.x < 1 || p.y < 1 || p.x > RoomTemplate.Width - 2 || p.y > RoomTemplate.Height - 3) continue;
                        if (IsWalkable(p)) return Origin + new Vector2(p.x + 0.5f, p.y + 0.3f);
                    }
            return world;
        }

        public Vector2 PlayerStart
        {
            get { var pts = Points(SpawnKind.PlayerStart); return pts.Count > 0 ? pts[0] : NearestWalkable(Origin + RoomTemplate.Centre + new Vector2(0, -1f)); }
        }

        /// A random walkable cell centre at least <paramref name="minDist"/> from <paramref name="awayFrom"/>.
        public Vector2 RandomFloorPoint(Vector2 awayFrom, float minDist)
        {
            for (int i = 0; i < 60; i++)
            {
                var p = new Vector3Int(_rng.Next(2, RoomTemplate.Width - 2), _rng.Next(2, RoomTemplate.Height - 3), 0);
                if (!IsWalkable(p) || NearDoor(p)) continue;
                Vector2 w = Origin + new Vector2(p.x + 0.5f, p.y + 0.3f);
                if (Vector2.Distance(w, awayFrom) < minDist && i < 50) continue;
                return w;
            }
            return NearestWalkable(Origin + RoomTemplate.Centre);
        }

        void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--) { int j = _rng.Next(i + 1); T t = list[i]; list[i] = list[j]; list[j] = t; }
        }

        // ───────────────────────── the event ─────────────────────────
        public void OnEnter()
        {
            Visited = true;
            if (!Cleared && HasEvent && !EventRunning) StartCoroutine(RunEvent());
        }

        IEnumerator RunEvent()
        {
            EventRunning = true;
            _spawning = true;
            SetDoors(false);
            yield return new WaitForSeconds(0.35f);

            // safety: never leave the player stuck in a doorway behind the bars
            if (Controller.Player != null)
            {
                Vector2 local = Controller.Player.Feet - Origin;
                Vector2 inside = new Vector2(Mathf.Clamp(local.x, 1.6f, RoomTemplate.Width - 1.6f), Mathf.Clamp(local.y, 1.4f, RoomTemplate.Height - 3.1f));
                if ((inside - local).sqrMagnitude > 0.0001f) Controller.Player.Teleport(Origin + inside);
            }

            var S = Controller.Session;
            var T = S.Content.tuning;
            var run = S.Run;
            if (run == null) yield break;
            var normal = DungeonRules.NormalPool(S.Content, run);
            var elites = DungeonRules.ElitePool(S.Content, run);
            Vector2 playerAt = Controller.Player != null ? Controller.Player.Feet : Origin + RoomTemplate.Centre;

            var spots = Points(SpawnKind.Enemy);
            var eliteSpots = Points(SpawnKind.Elite);
            int count = T.enemiesPerRoomMin + _rng.Next(Mathf.Max(1, T.enemiesPerRoomMax - T.enemiesPerRoomMin + 1)) + Mathf.Max(0, run.floor - 1);
            if (Node.kind == RoomKind.Ladder || Node.kind == RoomKind.Exit) count += 1;
            count = Mathf.RoundToInt(count * S.MutatorEnemyCount);   // Swarm

            if (Node.kind == RoomKind.Boss)
            {
                string bossId = DungeonRules.BossMonster(S.Content, run);
                // the elite spot furthest from where the player came in (never on top of them)
                Vector2 at = Vector2.zero; float best = -1f;
                foreach (var sp in eliteSpots) { float d = Vector2.Distance(sp, playerAt); if (d > best) { best = d; at = sp; } }
                if (best < 4f) at = RandomFloorPoint(playerAt, 5f);
                Boss = Controller.SpawnEnemy(this, bossId, true, true, at);
                if (Boss != null) _enemies.Add(Boss);
                count = 2 + Mathf.Max(0, run.floor - 2);
            }
            else
            {
                bool dangerous = run.storm || run.mod == RouteMod.Dangerous;
                if (elites.Count > 0 && _rng.NextDouble() < T.dungeonEliteChance * (dangerous ? 2f : 1f))
                {
                    Vector2 at = NextSpot(eliteSpots, playerAt, false);
                    if (float.IsNaN(at.x)) at = NextSpot(spots, playerAt);
                    var e = Controller.SpawnEnemy(this, elites[_rng.Next(elites.Count)], true, false, at);
                    if (e != null) { _enemies.Add(e); count--; }
                }
            }

            for (int i = 0; i < count && normal.Count > 0; i++)
            {
                var e = Controller.SpawnEnemy(this, normal[_rng.Next(normal.Count)], false, false, NextSpot(spots, playerAt));
                if (e != null) _enemies.Add(e);
                yield return new WaitForSeconds(0.06f);
            }
            _spawning = false;
            if (_enemies.Count == 0) Clear();
        }

        static readonly Vector2 NoSpot = new Vector2(float.NaN, float.NaN);

        /// Pops spawn points until one is at least 3 tiles from the player; otherwise a random open spot (or NoSpot).
        Vector2 NextSpot(List<Vector2> spots, Vector2 playerAt, bool fallback = true)
        {
            while (spots.Count > 0)
            {
                Vector2 p = spots[spots.Count - 1]; spots.RemoveAt(spots.Count - 1);
                if (Vector2.Distance(p, playerAt) >= 3f) return p;
            }
            if (!fallback) return NoSpot;
            return RandomFloorPoint(playerAt, 4f);
        }

        public void OnEnemyDied(EnemyController e)
        {
            _enemies.Remove(e);
            if (e != null) LastKill = e.Body;
            if (e == Boss) Boss = null;
            if (EventRunning && !_spawning && _enemies.Count == 0) Clear();
        }

        void Clear()
        {
            EventRunning = false;
            Cleared = true;
            SetDoors(true);
            foreach (var p in _hidden) if (p != null) p.SetAvailable(true);
            _hidden.Clear();
            Controller.OnRoomCleared(this);
        }

        /// Bog Shaman: patch up the most wounded ally (not itself) below 75% HP.
        public void HealWeakestAlly(EnemyController healer, float pct)
        {
            EnemyController best = null; float lowest = 0.75f;
            foreach (var e in _enemies)
            {
                if (e == null || e == healer || e.Dead) continue;
                float r = e.Hp / (float)Mathf.Max(1, e.Stats.maxHp);
                if (r < lowest) { lowest = r; best = e; }
            }
            if (best != null) best.Heal(Mathf.Max(1, Mathf.RoundToInt(best.Stats.maxHp * pct)));
        }
    }
}
