// Hearth & Hollow — the Effects Lab: things to hit (dummies and a pack kept alive at set spots), using moves (by click,
// key or auto-cast), and the damage meter.
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Dungeon;
using UnityEngine;

namespace HearthHollow.Lab
{
    public partial class EffectsLab
    {
        // ───────────────────────── where things stand ─────────────────────────
        /// The middle of the arena (or, on a real floor, a little in front of where you came in).
        Vector2 Anchor
        {
            get
            {
                var room = DC != null ? DC.CurrentRoom : null;
                if (room == null) return Vector2.zero;
                return _p.arena ? room.Origin + RoomTemplate.Centre : room.PlayerStart + new Vector2(3.5f, 0f);
            }
        }

        static readonly Vector2[] DummySpots = { new Vector2(0.5f, 0f), new Vector2(0.5f, 1.9f), new Vector2(0.5f, -1.9f) };
        static readonly Vector2[] PackSpots = { new Vector2(3.5f, 0f), new Vector2(3.5f, 2f), new Vector2(3.5f, -2f), new Vector2(5.5f, 1f), new Vector2(5.5f, -1f), new Vector2(6.5f, 0f) };

        Vector2 Spot(Vector2 off) { return DC.CurrentRoom.NearestWalkable(Anchor + off); }

        // ───────────────────────── keeping targets ─────────────────────────
        readonly List<EnemyController> _dummies = new List<EnemyController>();
        readonly EnemyController[] _packAt = new EnemyController[6];
        readonly bool[] _packHad = new bool[6];
        readonly Dictionary<int, float> _packDue = new Dictionary<int, float>();   // spot → when it's refilled

        /// A new floor: nothing placed yet.
        void ResetTargets()
        {
            _dummies.Clear(); _packDue.Clear();
            for (int i = 0; i < _packAt.Length; i++) { _packAt[i] = null; _packHad[i] = false; }
        }

        void KeepTargets()
        {
            // dummies: exactly as many as asked for
            _dummies.RemoveAll(d => d == null);
            while (_dummies.Count < _p.dummies && _dummies.Count < DummySpots.Length)
            {
                var d = SpawnDummy(Spot(DummySpots[_dummies.Count]));
                if (d == null) break;
                _dummies.Add(d);
            }
            while (_dummies.Count > _p.dummies) { Destroy(_dummies[_dummies.Count - 1].gameObject); _dummies.RemoveAt(_dummies.Count - 1); }

            // the pack: refilled a moment after each one dies
            var md = string.IsNullOrEmpty(_p.packMonster) ? null : S.Content.Monster(_p.packMonster);
            int want = md != null ? Mathf.Clamp(_p.packCount, 0, PackSpots.Length) : 0;
            for (int i = 0; i < PackSpots.Length; i++)
            {
                var e = _packAt[i];
                bool alive = e != null && !e.Dead;
                if (i >= want) { if (alive) Destroy(e.gameObject); _packAt[i] = null; _packDue.Remove(i); continue; }
                if (alive) continue;
                float due;
                if (!_packDue.TryGetValue(i, out due)) { _packDue[i] = Time.time + (_packHad[i] ? 1f : 0f); continue; }   // the first one straight away
                if (Time.time < due) continue;
                _packDue.Remove(i);
                var room = DC.CurrentRoom;
                var spawned = DC.SpawnEnemy(room, md.id, false, false, Spot(PackSpots[i]));
                if (spawned != null) room.Adopt(spawned);
                _packAt[i] = spawned; _packHad[i] = true;
            }
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

        string PickedMonster { get { return !string.IsNullOrEmpty(_p.packMonster) && S.Content.Monster(_p.packMonster) != null ? _p.packMonster : (S.Content.monsters.Count > 0 ? S.Content.monsters[0].id : null); } }

        void SpawnMonster(Vector2 at, bool boss)
        {
            if (!Ready) return;
            string id = boss ? DungeonRules.BossMonster(S.Content, S.Run) : PickedMonster;
            if (id == null) return;
            var room = DC.CurrentRoom;
            var e = DC.SpawnEnemy(room, id, false, boss, at);
            if (e == null) return;
            room.Adopt(e);
            if (boss && _p.bossSwirl) SwirlAura.On(e);
        }

        /// The floor's boss, with its entrance (and the swirl, if bosses wear it).
        void FloorBoss()
        {
            if (!Ready) return;
            var room = DC.CurrentRoom;
            var e = DC.SpawnEnemy(room, DungeonRules.BossMonster(S.Content, S.Run), false, true, Spot(PackSpots[0]));
            if (e == null) return;
            room.Adopt(e);
            if (_p.bossSwirl) SwirlAura.On(e);
            DC.BossEntrance(room, e);
        }

        /// A boss walks in wrapped in the swirl (whatever the floor's boss is).
        void SpawnSwirlBoss()
        {
            if (!Ready) return;
            var room = DC.CurrentRoom;
            var e = DC.SpawnEnemy(room, DungeonRules.BossMonster(S.Content, S.Run), false, true, Spot(PackSpots[0]));
            if (e == null) return;
            room.Adopt(e);
            SwirlAura.On(e);
        }

        /// The swirling green aura on everything in the room (dummies too), or off again if they all have it.
        void ToggleSwirlAll()
        {
            var all = new List<EnemyController>();
            foreach (var e in FindObjectsByType<EnemyController>()) if (e != null && !e.Dead) all.Add(e);
            bool allOn = all.Count > 0 && all.TrueForAll(e => e.GetComponentInChildren<SwirlAura>() != null);
            foreach (var e in all) { if (allOn) SwirlAura.Off(e); else SwirlAura.On(e); }
            if (all.Count == 0) _ui.Juice.Toast("Lab: nothing here to wrap — spawn a monster or a dummy", null, 2f);
        }

        EnemyController SpawnDummy(Vector2 at)
        {
            if (DC == null || DC.CurrentRoom == null) return null;
            var stats = new EnemyStats { monsterId = "training_dummy", maxHp = 9999, damage = 0f, difficulty = 1 };
            var e = DC.SpawnEnemy(DC.CurrentRoom, stats.monsterId, false, false, at, stats);
            if (e != null) e.MakeDummy();   // not adopted: it never holds a room's doors shut
            return e;
        }

        /// Every monster goes (dummies stay; the pack comes back on its own).
        void KillAll()
        {
            if (!Ready) return;
            foreach (var e in FindObjectsByType<EnemyController>())
            {
                if (e == null || e.Dead || e.Dummy) continue;
                if (e.Spawning) { Destroy(e.gameObject); continue; }
                e.TakeHit(999999f, true, Vector2.up);
            }
        }

        // ───────────────────────── using moves ─────────────────────────
        /// Use a move now (any class's, any slot), at a level (0 = its own), aimed at the nearest target if asked.
        void Cast(MoveData m, int level)
        {
            if (!Ready || m == null) return;
            if (level > 0) S.SetMoveLevel(m.id, level);
            _p.SetLevel(_p.lastLevels, m.id, level);
            Vector2 aim = Vector2.zero;
            if (_p.aimNearest)
            {
                float best = float.MaxValue; Vector2 from = DC.Player.Body;
                foreach (var e in FindObjectsByType<EnemyController>())
                {
                    if (e == null || e.Dead) continue;
                    float d = (e.Body - from).sqrMagnitude;
                    if (d < best) { best = d; aim = e.Body - from; }
                }
            }
            int lv = level > 0 ? level : S.MoveLevel(m.id);
            _castName = m.emoji + " " + m.name + (m.id.StartsWith("lab_") ? " Lv " + lv : "");
            _castHits = 0; _castDmg = 0; _castAt = Time.time;
            if (!DC.Player.LabCast(m, aim)) _ui.Juice.Toast("Lab: " + m.name + " did nothing here", null, 1.6f);
        }

        void SetAuto(bool on)
        {
            _p.autoOn = on && S.Content.Move(_p.autoMove) != null;
            if (on && !_p.autoOn) _ui.Juice.Toast("Lab: pick a move to repeat first (its 🔁)", null, 2f);
            _autoT = _p.autoInterval;   // the first one goes straight away
            if (_autoToggle != null) _autoToggle.SetValueWithoutNotify(_p.autoOn);
            Changed();
        }

        /// The 🔁 on a move: repeat this one (or stop, if it already is).
        void ToggleAutoMove(MoveData m)
        {
            if (_p.autoMove == m.id && _p.autoOn) { SetAuto(false); }
            else { _p.autoMove = m.id; SetAuto(true); }
            RefreshAutoLabel();
            BuildSkillList();
        }

        void AutoCast()
        {
            if (!_p.autoOn) return;
            var m = S.Content.Move(_p.autoMove);
            if (m == null) return;
            _autoT += Time.deltaTime;   // game time: slow motion slows it too, pause stops it
            if (_autoT < _p.autoInterval) return;
            _autoT = 0f;
            Cast(m, _p.Level(_p.lastLevels, m.id));
        }

        // ───────────────────────── the meter ─────────────────────────
        struct HitRec { public float t; public int dmg; }
        readonly Queue<HitRec> _recent = new Queue<HitRec>();
        int _total, _hits, _last, _biggest;
        string _castName; int _castHits, _castDmg; float _castAt = -99f;
        const float MeterWindow = 5f, CastWindow = 4f;

        void OnAnyHit(EnemyController e, int dmg, bool crit)
        {
            float now = Time.time;
            _recent.Enqueue(new HitRec { t = now, dmg = dmg });
            _total += dmg; _hits++; _last = dmg; if (dmg > _biggest) _biggest = dmg;
            if (_castName != null && now - _castAt <= CastWindow) { _castHits++; _castDmg += dmg; }   // counts towards the last move used
        }

        void ResetMeter() { _recent.Clear(); _total = _hits = _last = _biggest = 0; _castName = null; }

        string MeterText()
        {
            float now = Time.time;
            while (_recent.Count > 0 && now - _recent.Peek().t > MeterWindow) _recent.Dequeue();
            int sum = 0; float first = now;
            foreach (var h in _recent) { sum += h.dmg; if (h.t < first) first = h.t; }
            float span = Mathf.Max(1f, Mathf.Min(MeterWindow, now - first));
            return "Last " + _last + "  ·  Biggest " + _biggest + "  ·  DPS " + (sum / span).ToString("0.0") + "  ·  Total " + _total + " (" + _hits + " hits)";
        }

        string CastText()
        {
            if (_castName == null) return "Last cast: —";
            return "Last cast: " + _castName + " — " + _castHits + " hit" + (_castHits == 1 ? "" : "s") + ", " + _castDmg + " dmg";
        }
    }
}
