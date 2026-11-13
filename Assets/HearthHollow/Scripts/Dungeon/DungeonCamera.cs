// Hearth & Hollow — the dungeon camera: it eases to each room you enter, shakes on big hits, and pushes in on a
// boss as it makes its entrance. It remembers where the camera was and puts it back when the run ends.
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public sealed class DungeonCamera
    {
        Camera _cam;
        bool _saved; Vector3 _home; float _homeSize; Color _homeBg;
        Vector3 _target;
        float _shakeT, _shakeAmp;
        float _pushBaseSize; bool _wasPushing;

        Camera Cam { get { if (_cam == null) _cam = Camera.main; return _cam; } }

        /// A new floor: remember the camera as it was (once per run), then frame the dungeon.
        public void Setup(float size, DungeonTheme theme)
        {
            var cam = Cam;
            if (cam == null) return;
            if (!_saved) { _home = cam.transform.position; _homeSize = cam.orthographicSize; _homeBg = cam.backgroundColor; _saved = true; }
            cam.orthographicSize = size;
            if (theme != null) cam.backgroundColor = theme.background;
        }

        /// The run is over: the camera goes back to where it was.
        public void Restore()
        {
            if (_cam == null || !_saved) return;
            _cam.transform.position = _home; _cam.orthographicSize = _homeSize; _cam.backgroundColor = _homeBg;
            _saved = false;
        }

        /// Ease to (or jump to) a room's centre.
        public void LookAt(Vector3 at, bool snap)
        {
            _target = at;
            _target.z = Cam != null ? Cam.transform.position.z : -10f;
            if (snap && Cam != null) Cam.transform.position = _target;
        }

        public void Shake(float seconds, float amp) { _shakeT = Mathf.Max(_shakeT, seconds); _shakeAmp = Mathf.Max(_shakeAmp, amp); }

        /// A boss's entrance starts: push in from whatever the camera is now.
        public void StartPush() { if (!_wasPushing && Cam != null) _pushBaseSize = Cam.orthographicSize; }

        /// Once a frame (unscaled time): follow, lean towards a boss while `push` > 0, and shake while you're playing.
        public void Tick(float dt, float push, EnemyController boss, bool shaking)
        {
            if (_cam == null) return;
            Vector3 aim = _target;
            if (push > 0f && boss != null && !boss.Dead)
            {
                Vector3 b = boss.Body; b.z = _target.z;
                aim = Vector3.Lerp(_target, b, 0.55f * push);   // lean towards the boss
            }
            if (push > 0f || _wasPushing) _cam.orthographicSize = _pushBaseSize * (1f - 0.22f * push);   // and push in
            _wasPushing = push > 0f;
            Vector3 pos = Vector3.Lerp(_cam.transform.position, aim, 1f - Mathf.Exp(-dt * 14f));
            pos.z = _target.z;
            if (_shakeT > 0f && shaking)
            {
                _shakeT -= dt;
                pos += (Vector3)(Random.insideUnitCircle * _shakeAmp);
                if (_shakeT <= 0f) _shakeAmp = 0f;
            }
            _cam.transform.position = pos;
        }
    }
}
