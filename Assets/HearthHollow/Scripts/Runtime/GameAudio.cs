// Hearth & Hollow — ticket 62: the sound. One-shot effects by id (GameAudio.Play("hit")), a music track that follows
// where you are (town, each area, a boss fight) with a crossfade, an ambience loop (Frost Hollow's wind, storm rain),
// and Music / Effects volumes saved in PlayerPrefs (Settings). Clips come from Resources/HHAudio/AudioLibrary.
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Data;
using HearthHollow.Dungeon;
using UnityEngine;

namespace HearthHollow.Runtime
{
    public class GameAudio : MonoBehaviour
    {
        public const string MusicPref = "hh_music_vol", SfxPref = "hh_sfx_vol";
        public static readonly int[] Levels = { 0, 25, 50, 75, 100 };
        public static GameAudio Instance { get; private set; }

        public static float MusicVolume = 0.75f, SfxVolume = 0.75f;
        AudioLibrary _lib;
        GameSession _s;
        readonly List<AudioSource> _sfx = new List<AudioSource>();
        AudioSource _musicA, _musicB, _amb, _loop;
        string _track, _ambId, _loopId;
        float _fade = 1f;
        readonly Dictionary<string, float> _lastPlayed = new Dictionary<string, float>();

        public static void Create(GameSession s)
        {
            if (Instance != null) { Instance._s = s; return; }
            var go = new GameObject("Game Audio");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<GameAudio>();
            Instance._s = s;
        }

        void Awake()
        {
            _lib = Resources.Load<AudioLibrary>("HHAudio/AudioLibrary");
            MusicVolume = PlayerPrefs.GetInt(MusicPref, 50) / 100f;
            SfxVolume = PlayerPrefs.GetInt(SfxPref, 75) / 100f;
            _musicA = Src(true); _musicB = Src(true); _amb = Src(true); _loop = Src(true);
            for (int i = 0; i < 10; i++) _sfx.Add(Src(false));
        }

        AudioSource Src(bool loop)
        {
            var a = gameObject.AddComponent<AudioSource>();
            a.playOnAwake = false; a.loop = loop; a.spatialBlend = 0f;
            return a;
        }

        public static void SetMusic(int percent) { MusicVolume = Mathf.Clamp01(percent / 100f); PlayerPrefs.SetInt(MusicPref, percent); PlayerPrefs.Save(); }
        public static void SetSfx(int percent) { SfxVolume = Mathf.Clamp01(percent / 100f); PlayerPrefs.SetInt(SfxPref, percent); PlayerPrefs.Save(); Play("click"); }

        /// A one-shot effect. The same id within `gap` seconds is skipped (a cleave hitting five things is one thud).
        public static void Play(string id, float volume = 1f, float gap = 0.04f)
        {
            var g = Instance;
            if (g == null || g._lib == null || SfxVolume <= 0f) return;
            var s = g._lib.Get(id);
            if (s == null || s.clips.Count == 0) return;
            float last;
            if (g._lastPlayed.TryGetValue(id, out last) && Time.unscaledTime - last < gap) return;
            g._lastPlayed[id] = Time.unscaledTime;
            AudioSource src = null;
            foreach (var a in g._sfx) if (!a.isPlaying) { src = a; break; }
            if (src == null) src = g._sfx[0];
            src.clip = s.clips[Random.Range(0, s.clips.Count)];
            src.volume = s.volume * volume * SfxVolume;
            src.pitch = 1f + Random.Range(-s.pitchJitter, s.pitchJitter);
            src.Play();
        }

        /// A looping effect that runs until StopLoop (the stove's sizzle).
        public static void Loop(string id)
        {
            var g = Instance;
            if (g == null || g._lib == null) return;
            if (g._loopId == id && g._loop.isPlaying) return;
            var s = g._lib.Get(id);
            if (s == null || s.clips.Count == 0) return;
            g._loopId = id; g._loop.clip = s.clips[0]; g._loop.volume = s.volume * SfxVolume; g._loop.Play();
        }
        public static void StopLoop() { var g = Instance; if (g != null) { g._loop.Stop(); g._loopId = null; } }

        /// Which music track this moment wants.
        public static string TrackFor(GameSession s, bool bossFight)
        {
            if (s == null) return "town";
            if (s.InDungeon && s.Run != null)
            {
                if (bossFight) return "boss";
                return s.Run.routeId == GameSession.FallRoute ? "fall" : s.Run.routeId == "meadow" || s.Run.routeId == "chili" || s.Run.routeId == "frosthollow" ? s.Run.routeId : "dungeon";
            }
            return "town";
        }

        /// …and which ambience loop (null = none).
        public static string AmbienceFor(GameSession s)
        {
            if (s == null) return null;
            if (s.InDungeon && s.Run != null) return AreaAtmosphere.KindFor(s.Content, s.Run.routeId) == AirKind.Frost ? "wind" : null;
            return s.Hearth != null && s.Hearth.campEvent == CampEvent.Storm ? "rain" : null;
        }

        void Update()
        {
            if (_lib == null) return;
            var dc = DungeonController.Instance;
            bool boss = dc != null && dc.CurrentRoom != null && dc.CurrentRoom.Boss != null && !dc.CurrentRoom.Boss.Dead;
            string want = TrackFor(_s, boss);
            if (want != _track)
            {
                var t = _lib.Track(want);
                if (t != null && t.clips.Count > 0)
                {
                    var tmp = _musicA; _musicA = _musicB; _musicB = tmp;   // A plays the new one; B fades the old one out
                    _musicA.clip = t.clips[0]; _musicA.volume = 0f; _musicA.Play();
                    _fade = 0f;
                }
                _track = want;
            }
            var cur = _lib.Track(_track);
            _fade = Mathf.MoveTowards(_fade, 1f, Time.unscaledDeltaTime / 1.5f);
            float mv = (cur != null ? cur.volume : 0.4f) * MusicVolume;
            _musicA.volume = mv * _fade;
            _musicB.volume = Mathf.Min(_musicB.volume, mv * (1f - _fade));
            if (_fade >= 1f && _musicB.isPlaying) _musicB.Stop();

            string amb = AmbienceFor(_s);
            if (amb != _ambId)
            {
                _ambId = amb;
                var a = amb != null ? _lib.Track(amb) : null;
                if (a != null && a.clips.Count > 0) { _amb.clip = a.clips[0]; _amb.Play(); } else _amb.Stop();
            }
            var at = _ambId != null ? _lib.Track(_ambId) : null;
            if (at != null) _amb.volume = at.volume * SfxVolume;
            if (_loopId != null) { var ls = _lib.Get(_loopId); if (ls != null) _loop.volume = ls.volume * SfxVolume; }
        }
    }
}
