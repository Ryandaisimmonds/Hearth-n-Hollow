// Hearth & Hollow — the HD-2D camera look (light & look plan, step 1).
// One global post-processing Volume made in code: bloom, vignette, colour grading (white balance + colour adjustments),
// a touch of chromatic aberration on big hits, and the tilt-shift edge blur (a Full Screen Pass on Renderer2D that
// reads the _HHTilt* globals set here). Each dungeon theme has its own grade (DungeonTheme.grade*); the town has
// day / rain / night grades; the camp screen uses Camp. Grades blend over about a second.
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Dungeon
{
    public class GameLook : MonoBehaviour
    {
        public struct Grade
        {
            public float temperature, tint, saturation, contrast, exposure, vignette, bloom, tilt;
            public Color filter;
        }

        public static GameLook Instance { get; private set; }

        // the tilt-shift's shape (strength comes from the grade)
        public static float TiltBand = 0.36f, TiltFeather = 0.14f, TiltRadius = 0.0025f;   // toned down 31 Oct (Ryan): only the very edges, gently

        Volume _vol;
        Bloom _bloom; Vignette _vig; ColorAdjustments _ca; WhiteBalance _wb; ChromaticAberration _chroma;
        Grade _target, _cur;
        float _hurt, _punch;
        static readonly int TiltId = Shader.PropertyToID("_HHTiltStrength"), BandId = Shader.PropertyToID("_HHTiltBand"),
                            FeatherId = Shader.PropertyToID("_HHTiltFeather"), RadiusId = Shader.PropertyToID("_HHTiltRadius");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("Game Look (HD-2D)");
            DontDestroyOnLoad(go);
            go.AddComponent<GameLook>();
        }

        /// The camp screen / anywhere without its own grade.
        public static Grade Camp { get { return new Grade { temperature = 6f, saturation = 6f, contrast = 6f, vignette = 0.2f, bloom = 0.6f, tilt = 0f, filter = Color.white }; } }

        void Awake()
        {
            Instance = this;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "HD-2D look (runtime)";
            _bloom = profile.Add<Bloom>(true);
            _bloom.threshold.Override(0.8f); _bloom.scatter.Override(0.72f); _bloom.intensity.Override(0.6f); _bloom.highQualityFiltering.Override(true);
            _vig = profile.Add<Vignette>(true);
            _vig.smoothness.Override(0.5f); _vig.rounded.Override(false); _vig.color.Override(Color.black); _vig.intensity.Override(0.2f);
            _ca = profile.Add<ColorAdjustments>(true);
            _ca.postExposure.Override(0f); _ca.contrast.Override(0f); _ca.saturation.Override(0f); _ca.colorFilter.Override(Color.white);
            _wb = profile.Add<WhiteBalance>(true);
            _wb.temperature.Override(0f); _wb.tint.Override(0f);
            _chroma = profile.Add<ChromaticAberration>(true);
            _chroma.intensity.Override(0f);
            _vol = gameObject.AddComponent<Volume>();
            _vol.isGlobal = true; _vol.priority = 10f; _vol.sharedProfile = profile;
            _target = _cur = Camp;
            Push();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Shader.SetGlobalFloat(TiltId, 0f);
        }

        // ───────── what to look like ─────────
        static string _owner;   // "dungeon" / "town" / null (camp)

        /// Back to the camp look — only if <paramref name="owner"/> is still the one in charge.
        public static void Release(string owner)
        {
            if (_owner != owner) return;
            _owner = null;
            Apply(Camp);
        }

        public static void Apply(Grade g, bool snap = false)
        {
            if (Instance == null) return;
            Instance._target = g;
            if (snap) Instance._cur = g;
        }

        public static void ForTheme(DungeonTheme t)
        {
            _owner = "dungeon";
            if (t == null) { Apply(Camp); return; }
            Apply(new Grade
            {
                temperature = t.gradeTemperature, tint = t.gradeTint, saturation = t.gradeSaturation, contrast = t.gradeContrast,
                exposure = t.gradeExposure, filter = t.gradeFilter, vignette = t.gradeVignette, bloom = t.gradeBloom, tilt = 1f,
            }, true);
        }

        public static void ForTown(bool night, bool rain)
        {
            _owner = "town";
            var g = new Grade { temperature = 10f, tint = 0f, saturation = 10f, contrast = 8f, exposure = 0f, vignette = 0.24f, bloom = 0.7f, tilt = 1f, filter = Color.white };
            if (rain) { g.temperature = -14f; g.saturation = -18f; g.contrast = 12f; g.vignette = 0.32f; g.filter = new Color(0.92f, 0.95f, 1f); }
            if (night) { g.temperature = -22f; g.tint = 4f; g.saturation = -6f; g.contrast = 14f; g.vignette = 0.36f; g.bloom = 1.1f; g.filter = new Color(0.85f, 0.9f, 1f); }
            Apply(g, true);
        }

        /// You took a hit: a red pulse at the screen's edges.
        public static void Hurt() { if (Instance != null) Instance._hurt = 1f; }
        /// A big moment (boss down, heavy crit): a burst of bloom and a little lens fringing.
        public static void Punch(float k) { if (Instance != null) Instance._punch = Mathf.Max(Instance._punch, Mathf.Clamp01(k)); }

        // ───────── every frame ─────────
        void Update()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                var acd = cam.GetUniversalAdditionalCameraData();
                if (acd != null && !acd.renderPostProcessing) acd.renderPostProcessing = true;
            }
            float dt = Time.unscaledDeltaTime, k = 1f - Mathf.Exp(-dt * 3f);
            _cur.temperature = Mathf.Lerp(_cur.temperature, _target.temperature, k);
            _cur.tint = Mathf.Lerp(_cur.tint, _target.tint, k);
            _cur.saturation = Mathf.Lerp(_cur.saturation, _target.saturation, k);
            _cur.contrast = Mathf.Lerp(_cur.contrast, _target.contrast, k);
            _cur.exposure = Mathf.Lerp(_cur.exposure, _target.exposure, k);
            _cur.vignette = Mathf.Lerp(_cur.vignette, _target.vignette, k);
            _cur.bloom = Mathf.Lerp(_cur.bloom, _target.bloom, k);
            _cur.tilt = Mathf.Lerp(_cur.tilt, _target.tilt, k);
            _cur.filter = Color.Lerp(_cur.filter, _target.filter, k);
            _hurt = Mathf.MoveTowards(_hurt, 0f, dt * 2.2f);
            _punch = Mathf.MoveTowards(_punch, 0f, dt * 2.5f);
            Push();
        }

        void Push()
        {
            _wb.temperature.Override(_cur.temperature); _wb.tint.Override(_cur.tint);
            _ca.saturation.Override(_cur.saturation); _ca.contrast.Override(_cur.contrast);
            _ca.postExposure.Override(_cur.exposure); _ca.colorFilter.Override(_cur.filter);
            _bloom.intensity.Override(_cur.bloom + _punch * 1.2f);
            _vig.intensity.Override(Mathf.Clamp01(_cur.vignette + _hurt * 0.22f));
            _vig.color.Override(Color.Lerp(Color.black, new Color(0.6f, 0.03f, 0.02f), _hurt));
            _chroma.intensity.Override(Mathf.Clamp01(_punch * 0.45f + _hurt * 0.25f));
            Shader.SetGlobalFloat(TiltId, _cur.tilt);
            Shader.SetGlobalFloat(BandId, TiltBand); Shader.SetGlobalFloat(FeatherId, TiltFeather); Shader.SetGlobalFloat(RadiusId, TiltRadius);
        }
    }
}
