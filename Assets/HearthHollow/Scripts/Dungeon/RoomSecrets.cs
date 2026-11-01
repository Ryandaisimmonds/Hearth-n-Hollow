// Hearth & Hollow — secrets in a room (light plan step 7). Light finds things:
//   • Hidden forage: invisible in the dark, a faint glint in your own light, fully there once a torch, brazier or Flare
//     falls on it. Walk over it once it's been seen to pick it up (better grade odds than a normal pickup).
//   • Cracked walls: a crack in the side or back wall glows once lit. Two hits break it open; a chest waits inside.
//   • Brazier puzzles: some calm rooms keep their braziers cold (marked with a blue rune). Light them all to win a chest.
//     Deeper floors: a puzzle brazier burns out after a while, so they must all be lit together.
//   • Flare shows every secret in its reach for a few seconds (DungeonSettings.secretRevealSeconds).
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace HearthHollow.Dungeon
{
    public class RoomSecrets : MonoBehaviour
    {
        public RoomInstance Room { get; private set; }
        public bool Puzzle { get; private set; }
        public bool PuzzleDone { get; private set; }
        public readonly List<HiddenForage> Forage = new List<HiddenForage>();
        public readonly List<CrackedWall> Walls = new List<CrackedWall>();
        readonly List<Brazier> _puzzle = new List<Brazier>();
        DungeonTheme _theme;
        System.Random _rng;
        bool _hinted;
        float _revealUntil = -1f, _revealR; Vector2 _revealAt;

        static readonly Color Gold = new Color(1f, 0.78f, 0.38f);

        public void Setup(RoomInstance room, DungeonTheme theme, int seed, int floor)
        {
            Room = room; _theme = theme;
            _rng = new System.Random(seed * 17 + 3);
            var set = room.Controller.Settings;
            var kind = room.Node.kind;
            if (kind == RoomKind.Boss || kind == RoomKind.Start) return;

            if (_rng.NextDouble() < set.secretForageChance) MakeForage();
            if (_rng.NextDouble() < set.secretWallChance) MakeWall();
            if ((kind == RoomKind.Gather || kind == RoomKind.Rest) && room.Lights != null && _rng.NextDouble() < set.puzzleChance)
                MakePuzzle(floor);
        }

        // ───────── building ─────────
        void MakeForage()
        {
            Vector2 at = Room.RandomFloorPoint(Room.Origin + RoomTemplate.Centre, 3f);
            var go = new GameObject("Hidden forage");
            go.transform.SetParent(transform, false);
            go.transform.position = at;
            var f = go.AddComponent<HiddenForage>();
            f.Setup(this, _theme != null ? _theme.forage : null);
            Forage.Add(f);
        }

        void MakeWall()
        {
            var walls = Room.Template.walls;
            if (walls == null) return;
            var hazard = _theme != null ? _theme.hazard : null;
            var cands = new List<Vector3Int>();
            int W = RoomTemplate.Width, H = RoomTemplate.Height;
            // side walls (x = 0 / W-1) and the back wall's face (y = H-2); never the front wall, never near a door
            for (int y = 2; y <= H - 4; y++)
            {
                Cand(cands, walls, hazard, new Vector3Int(0, y, 0), new Vector3Int(1, y, 0));
                Cand(cands, walls, hazard, new Vector3Int(W - 1, y, 0), new Vector3Int(W - 2, y, 0));
            }
            for (int x = 2; x <= W - 3; x++) Cand(cands, walls, hazard, new Vector3Int(x, H - 2, 0), new Vector3Int(x, H - 3, 0));
            if (cands.Count == 0) return;
            var c = cands[_rng.Next(cands.Count)];
            var go = new GameObject("Cracked wall");
            go.transform.SetParent(transform, false);
            go.transform.position = Room.Origin + new Vector2(c.x + 0.5f, c.y + 0.5f);
            var w = go.AddComponent<CrackedWall>();
            w.Setup(this, c, _rng.Next());
            Walls.Add(w);
        }

        void Cand(List<Vector3Int> list, Tilemap walls, TileBase hazard, Vector3Int wall, Vector3Int inside)
        {
            var t = walls.GetTile(wall);
            if (t == null || t == hazard) return;
            if (!Room.IsWalkable(inside) || Room.NearDoor(inside)) return;
            list.Add(wall);
        }

        void MakePuzzle(int floor)
        {
            var set = Room.Controller.Settings;
            var lights = Room.Lights;
            for (int i = 0; lights.Braziers.Count < 3 && i < 3; i++) lights.AddBrazier(3.5f);
            if (lights.Braziers.Count < 3 && _rng.NextDouble() < 0.5) lights.AddBrazier(2.5f);
            if (lights.Braziers.Count < 2) return;
            var burn = set.puzzleBurnByFloor;
            float seconds = burn != null && burn.Length > 0 ? burn[Mathf.Clamp(floor - 1, 0, burn.Length - 1)] : 0f;
            foreach (var b in lights.Braziers) { if (b == null) continue; b.MakePuzzle(seconds); _puzzle.Add(b); }
            Puzzle = true;
        }

        // ───────── reveal ─────────
        /// Flare: everything secret within <paramref name="radius"/> of <paramref name="at"/> shows for <paramref name="seconds"/>.
        public void Reveal(Vector2 at, float radius, float seconds)
        {
            _revealAt = at; _revealR = radius; _revealUntil = Time.time + seconds;
            foreach (var f in Forage) if (f != null && Vector2.Distance(f.transform.position, at) <= radius) DungeonFx.RingAt(f.transform.position, Gold, 0.9f, 0.6f);
            foreach (var w in Walls) if (w != null && !w.Broken && Vector2.Distance(w.transform.position, at) <= radius) DungeonFx.RingAt(w.transform.position, Gold, 1f, 0.6f);
        }

        /// 0 = hidden … 1 = plain to see. Torch / brazier / Flare light shows it fully; your own light gives a faint glint close up.
        public float Reveal01(Vector2 p)
        {
            if (Time.time < _revealUntil && Vector2.Distance(p, _revealAt) <= _revealR) return 1f;
            Vector2 src; float s;
            if (DungeonLight.StrongestSource(p, 0.45f, out src, out s)) return Mathf.Clamp01(0.55f + s * 0.5f);
            var pl = Room.Controller.Player;
            if (pl == null) return 0f;
            float reach = Room.Controller.Settings.secretGlintReach;
            float d = Vector2.Distance(pl.Body, p);
            return d < reach ? 0.28f * (1f - d / reach) : 0f;
        }

        // ───────── hits ─────────
        /// A player swing / skill / arrow reached <paramref name="p"/>: knocks on any cracked wall in reach.
        public void Hit(Vector2 p, float radius)
        {
            foreach (var w in Walls) if (w != null && !w.Broken && Vector2.Distance(w.transform.position, p) <= radius + 0.75f) w.Hit();
        }

        public TileBase FloorTile { get { var s = Room.Controller.Settings; return _theme != null && _theme.floor != null ? _theme.floor : s.markerFloor; } }
        public TileBase WallTile { get { var s = Room.Controller.Settings; return _theme != null && _theme.wall != null ? _theme.wall : s.markerWall; } }

        public void SpawnChest(Vector2 at, int tier)
        {
            var go = new GameObject("Secret chest");
            go.transform.SetParent(transform, false);
            go.transform.position = at;
            go.AddComponent<SecretChest>().Setup(this, tier);
        }

        void Update()
        {
            var dc = Room != null ? Room.Controller : null;
            if (dc == null || !dc.GameplayActive || dc.CurrentRoom != Room) return;
            if (!Puzzle || PuzzleDone) return;
            if (!_hinted)
            {
                _hinted = true;
                DungeonHud.Number(Room.Origin + RoomTemplate.Centre + new Vector2(0, 1.5f), "Light every brazier…", "dnum--info");
            }
            foreach (var b in _puzzle) if (b == null || !b.Lit) return;
            // solved
            PuzzleDone = true;
            foreach (var b in _puzzle) if (b != null) b.Keep();
            Vector2 c = Room.NearestWalkable(Room.Origin + RoomTemplate.Centre);
            DungeonFx.Flash(c, Gold, 8f, 2f, 1.2f);
            DungeonFx.RingAt(c, Gold, 3f, 0.7f);
            DungeonFx.Embers(c, Gold, 30, 1f);
            GameLook.Punch(0.4f);
            DungeonHud.Number(c + new Vector2(0, 1f), "Puzzle solved!", "dnum--info");
            SpawnChest(c, 2);
        }
    }

    /// Forage that only light can find.
    public class HiddenForage : MonoBehaviour
    {
        RoomSecrets _s; SpriteRenderer _sr, _glint; bool _found, _taken; float _seed;

        public void Setup(RoomSecrets s, Sprite sprite)
        {
            _s = s; _seed = Random.value * 10f;
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = sprite != null ? sprite : DungeonFx.Circle;
            _sr.color = new Color(1f, 1f, 1f, 0f);
            transform.localScale = new Vector3(1.3f, 1.3f, 1f);
            var g = new GameObject("Glint"); g.transform.SetParent(transform, false); g.transform.localPosition = new Vector3(0.12f, 0.35f, 0);
            g.transform.localScale = new Vector3(2.5f, 2.5f, 1f);
            _glint = g.AddComponent<SpriteRenderer>(); _glint.sprite = DungeonFx.Pixel; _glint.color = new Color(1f, 0.9f, 0.6f, 0f);
            if (DungeonFx.GlowMat != null) _glint.sharedMaterial = DungeonFx.GlowMat;
        }

        void Update()
        {
            var dc = _s != null ? _s.Room.Controller : null;
            if (_taken || dc == null || dc.CurrentRoom != _s.Room) return;
            float r = _found ? 1f : _s.Reveal01(transform.position);
            if (!_found && r >= 0.9f)
            {
                _found = true;
                DungeonFx.RingAt(transform.position + new Vector3(0, 0.3f), new Color(1f, 0.9f, 0.6f, 0.9f), 0.9f, 0.5f);
            }
            float tw = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f + _seed);
            _sr.color = new Color(1f, 1f, 1f, _found ? 1f : r * 0.9f);
            _glint.color = new Color(1f, 0.9f, 0.6f, Mathf.Max(r * 0.8f, 0f) * tw);
            _glint.transform.localScale = Vector3.one * (1.8f + tw * 1.4f);
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0);
            _sr.sortingOrder = DungeonController.SortOrder(transform.position.y) - 1; _glint.sortingOrder = _sr.sortingOrder + 1;

            if (_found && dc.GameplayActive && dc.Player != null && Vector2.Distance(dc.Player.Feet, transform.position) < 0.8f)
            {
                _taken = true;
                dc.Session.DungeonHiddenForage();
                DungeonFx.Puff(transform.position + new Vector3(0, 0.3f), new Color(0.6f, 1f, 0.6f), 8, 0.5f);
                DungeonFx.Embers(transform.position, new Color(1f, 0.9f, 0.6f), 10, 0.3f);
                DungeonHud.Number((Vector2)transform.position + new Vector2(0, 0.9f), "Hidden forage!", "dnum--info");
                Destroy(gameObject);
            }
        }
    }

    /// A crack in the wall: glows once lit, breaks after two hits and opens a little alcove with a chest.
    public class CrackedWall : MonoBehaviour
    {
        RoomSecrets _s; Vector3Int _cell; int _hp = 2; bool _found;
        readonly List<SpriteRenderer> _cracks = new List<SpriteRenderer>();
        public bool Broken { get; private set; }

        public void Setup(RoomSecrets s, Vector3Int cell, int seed)
        {
            _s = s; _cell = cell;
            var rng = new System.Random(seed);
            // a zig-zag crack down the wall
            Vector2 p = new Vector2(-0.05f + (float)rng.NextDouble() * 0.1f, 0.4f);
            for (int i = 0; i < 5; i++)
            {
                float ang = -90f + (-35f + (float)rng.NextDouble() * 70f);
                float len = 0.16f + (float)rng.NextDouble() * 0.1f;
                Vector2 d = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
                _cracks.Add(Strip(p + d * len * 0.5f, len, ang, 0.045f));
                if (i == 2) _cracks.Add(Strip(p + new Vector2(0.1f, -0.04f), 0.14f, ang + 55f, 0.03f));   // a side branch
                p += d * len;
            }
        }

        SpriteRenderer Strip(Vector2 local, float len, float ang, float w)
        {
            var g = new GameObject("Crack"); g.transform.SetParent(transform, false);
            g.transform.localPosition = local; g.transform.localRotation = Quaternion.Euler(0, 0, ang);
            var b = DungeonFx.Pixel.bounds.size;
            g.transform.localScale = new Vector3(len / b.x, w / b.y, 1f);
            var sr = g.AddComponent<SpriteRenderer>(); sr.sprite = DungeonFx.Pixel; sr.color = new Color(1f, 0.75f, 0.4f, 0f);
            sr.sortingOrder = 30000;   // over the wall (Default layer draws after Walls)
            if (DungeonFx.GlowMat != null) sr.sharedMaterial = DungeonFx.GlowMat;
            return sr;
        }

        void Update()
        {
            if (Broken || _s == null || _s.Room.Controller.CurrentRoom != _s.Room) return;
            float r = _found ? 1f : _s.Reveal01(transform.position);
            if (!_found && r >= 0.9f) { _found = true; DungeonFx.RingAt(transform.position, new Color(1f, 0.8f, 0.45f, 0.9f), 1f, 0.5f); }
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 3f);
            foreach (var c in _cracks) if (c != null) c.color = new Color(1f, 0.75f, 0.4f, (_found ? 0.9f : r) * pulse);
        }

        public void Hit()
        {
            if (Broken) return;
            _hp--;
            var dc = _s.Room.Controller;
            DungeonFx.Puff(transform.position, new Color(0.65f, 0.6f, 0.55f), 6, 0.5f);
            DungeonFx.Sparks(transform.position, Vector2.down, new Color(1f, 0.75f, 0.4f), 6);
            dc.Shake(0.06f, 0.06f);
            if (!_found) { _found = true; }
            if (_hp > 0) return;
            Break();
        }

        void Break()
        {
            Broken = true;
            var dc = _s.Room.Controller;
            var tpl = _s.Room.Template;
            // seal anything open round the cell first, so the alcove never leads into the void
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var n = new Vector3Int(_cell.x + dx, _cell.y + dy, 0);
                    if ((tpl.floor == null || tpl.floor.GetTile(n) == null) && tpl.walls.GetTile(n) == null) tpl.walls.SetTile(n, _s.WallTile);
                }
            tpl.walls.SetTile(_cell, null);
            if (tpl.floor != null) tpl.floor.SetTile(_cell, _s.FloorTile);
            Vector2 at = _s.Room.Origin + new Vector2(_cell.x + 0.5f, _cell.y + 0.25f);
            DungeonFx.Puff(transform.position, new Color(0.6f, 0.55f, 0.5f), 18, 1f);
            for (int i = 0; i < 10; i++) DungeonFx.Throw(transform.position, Random.insideUnitCircle.normalized, new Color(0.55f, 0.5f, 0.45f), 1, 2f, 5f);
            DungeonFx.Flash(at, new Color(1f, 0.8f, 0.45f), 4f, 1.6f, 0.6f);
            dc.Shake(0.15f, 0.15f);
            GameLook.Punch(0.3f);
            DungeonHud.Number(at + new Vector2(0, 1f), "Secret!", "dnum--info");
            _s.SpawnChest(at, 1);
            foreach (var c in _cracks) if (c != null) Destroy(c.gameObject);
        }
    }

    /// A small chest: walk up to it to open it (GameSession.DungeonSecretChest).
    public class SecretChest : MonoBehaviour
    {
        RoomSecrets _s; int _tier; bool _open; Transform _lid; UnityEngine.Rendering.Universal.Light2D _light; float _t;

        public void Setup(RoomSecrets s, int tier)
        {
            _s = s; _tier = tier;
            int o = DungeonController.SortOrder(transform.position.y);
            Color wood = new Color(0.45f, 0.28f, 0.15f), dark = new Color(0.3f, 0.18f, 0.1f), band = tier >= 2 ? new Color(0.95f, 0.78f, 0.35f) : new Color(0.7f, 0.68f, 0.62f);
            Box(transform, new Vector2(0, 0.17f), new Vector2(0.62f, 0.34f), wood, o);
            Box(transform, new Vector2(0, 0.06f), new Vector2(0.64f, 0.05f), band, o + 1);
            var lid = new GameObject("Lid"); lid.transform.SetParent(transform, false); lid.transform.localPosition = new Vector3(-0.31f, 0.34f, 0); _lid = lid.transform;
            Box(_lid, new Vector2(0.31f, 0.08f), new Vector2(0.66f, 0.16f), dark, o + 2);
            Box(_lid, new Vector2(0.31f, 0.02f), new Vector2(0.66f, 0.04f), band, o + 3);
            Box(_lid, new Vector2(0.31f, -0.02f), new Vector2(0.1f, 0.1f), band, o + 3);   // the clasp
            _light = DungeonFx.Flash((Vector2)transform.position + new Vector2(0, 0.4f), new Color(1f, 0.8f, 0.45f), tier >= 2 ? 2.6f : 2f, 0.9f, 0f, transform);
            DungeonFx.RingAt(transform.position + new Vector3(0, 0.2f), new Color(1f, 0.85f, 0.5f, 0.9f), 1.2f, 0.6f);
        }

        static void Box(Transform parent, Vector2 local, Vector2 size, Color c, int order)
        {
            var g = new GameObject("Part"); g.transform.SetParent(parent, false); g.transform.localPosition = local;
            var b = DungeonFx.Pixel.bounds.size;
            g.transform.localScale = new Vector3(size.x / b.x, size.y / b.y, 1f);
            var sr = g.AddComponent<SpriteRenderer>(); sr.sprite = DungeonFx.Pixel; sr.color = c; sr.sortingOrder = order;
        }

        void Update()
        {
            var dc = _s != null ? _s.Room.Controller : null;
            if (dc == null) return;
            _t += Time.deltaTime;
            if (_open)
            {
                if (_light != null) _light.intensity = Mathf.MoveTowards(_light.intensity, 0.25f, Time.deltaTime * 0.5f);
                return;
            }
            if (_light != null) _light.intensity = 0.9f * (1f + 0.15f * Mathf.Sin(_t * 3f));
            if (!dc.GameplayActive || dc.Player == null || dc.CurrentRoom != _s.Room) return;
            if (Vector2.Distance(dc.Player.Feet, transform.position) > 0.95f) return;
            _open = true;
            _lid.localRotation = Quaternion.Euler(0, 0, 35f);
            _lid.localPosition += new Vector3(0, 0.1f, 0);
            Vector2 p = (Vector2)transform.position + new Vector2(0, 0.4f);
            Color g = new Color(1f, 0.82f, 0.4f);
            DungeonFx.Flash(p, g, 5f, 2f, 0.7f);
            DungeonFx.Sparks(p, Vector2.up, g, 14);
            DungeonFx.Embers(p, g, 20, 0.4f);
            GameFeel.HitStop(0.05f);
            DungeonHud.Number(p + new Vector2(0, 0.7f), _tier >= 2 ? "Treasure!" : "Hidden stash!", "dnum--info");
            dc.Session.DungeonSecretChest(_tier);
        }
    }
}
