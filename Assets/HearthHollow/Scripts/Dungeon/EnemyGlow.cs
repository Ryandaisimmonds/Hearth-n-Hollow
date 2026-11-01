// Hearth & Hollow — keeps every monster readable in the dark (light plan step 2).
//   • out of light: the body (lit sprite) falls to near-black and a coloured rim + two glowing eyes take over;
//   • in light: full colour, the rim and eyes fade out (DungeonLight.At decides how lit it is);
//   • carriers: elites hold a gold lantern, bosses glow with a big pulsing signature light, some types carry their own
//     light (EnemyProfile.carryLight). Lights also reach the walls further (DungeonFx.Flash adds a wall glow).
using HearthHollow.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Dungeon
{
    public class EnemyGlow : MonoBehaviour
    {
        static Material _rimMat;
        static Material RimMat { get { if (_rimMat == null) _rimMat = Resources.Load<Material>("HHFx/Rim Sprite"); return _rimMat; } }
        static readonly int RimColorId = Shader.PropertyToID("_RimColor"), AmountId = Shader.PropertyToID("_Amount");

        EnemyController _e; CharacterAnimator _anim; EnemyProfile _p; DungeonSettings _set;
        SpriteRenderer _rim, _eyeL, _eyeR, _lanternSr;
        MaterialPropertyBlock _mpb;
        Light2D _carry, _lantern, _boss;
        Color _rimCol, _eyeCol;
        float _size, _dark = 1f, _blinkT, _blinkUntil, _seed, _eyeH, _eyeS;
        bool _showEyes = true;
        float _maxDark = 1f, _rimK = 1f;   // townsfolk stay recognisable: only half-dark, softer rim
        bool _boss2, _shadowLook;
        Color _baseCol, _wroteCol; bool _wrote;   // the body's own tint (elite gold, hit flash…) before we darken it

        public void Setup(EnemyController e, CharacterAnimator anim, EnemyProfile p, EnemyStats stats, float size, DungeonSettings set)
        {
            _e = e; _anim = anim; _p = p; _size = size; _set = set; _seed = Random.value * 10f;
            _eyeH = p.eyeHeight; _eyeS = p.eyeSpread;
            _boss2 = stats.boss;
            _rimCol = stats.boss ? new Color(1f, 0.35f, 0.2f) : stats.elite ? new Color(1f, 0.8f, 0.35f) : p.rimColor;
            _eyeCol = stats.boss ? new Color(1f, 0.25f, 0.15f) : stats.elite ? new Color(1f, 0.85f, 0.4f) : p.eyeColor;
            _mpb = new MaterialPropertyBlock();

            MakeRimAndEyes();

            float h = anim != null ? anim.FigureHeight : 0.8f;
            if (p.carryLight.a > 0.01f)
                _carry = DungeonFx.Flash(transform.position + new Vector3(0, h * 0.5f), p.carryLight, p.carryLightRadius * Mathf.Max(1f, size * 0.8f), 1.2f, 0f, transform);
            if (stats.elite && !stats.boss)
            {
                // a little lantern held at the side, with its own warm light
                var lg = new GameObject("Lantern");
                lg.transform.SetParent(transform, false);
                lg.transform.localPosition = new Vector3(0.32f * size, h * 0.42f, 0);
                _lanternSr = lg.AddComponent<SpriteRenderer>();
                _lanternSr.sprite = DungeonFx.Pixel;
                lg.transform.localScale = new Vector3(5f, 6f, 1f);
                _lanternSr.color = set.eliteLantern;
                if (DungeonFx.GlowMat != null) _lanternSr.sharedMaterial = DungeonFx.GlowMat;
                _lantern = DungeonFx.Flash(lg.transform.position, set.eliteLantern, 4.5f, 1.25f, 0f, lg.transform);
            }
            if (stats.boss)
                _boss = DungeonFx.Flash(transform.position + new Vector3(0, h * 0.5f), set.bossLight, 9f, 1.5f, 0f, transform);
            DungeonController.CastShadows(_carry, 0.7f); DungeonController.CastShadows(_lantern, 0.7f); DungeonController.CastShadows(_boss, 0.8f);
        }

        /// Any other character (town critters, townsfolk): the same silhouette + rim, eyes optional, no carried lights.
        public void SetupCharacter(CharacterAnimator anim, Color rim, Color eyes, bool showEyes, float eyeHeight, float eyeSpread, float size, DungeonSettings set)
        {
            _anim = anim; _size = size; _set = set; _seed = Random.value * 10f;
            _rimCol = rim; _eyeCol = eyes; _showEyes = showEyes; _eyeH = eyeHeight; _eyeS = eyeSpread;
            if (!showEyes) { _maxDark = 0.5f; _rimK = 0.6f; }
            _mpb = new MaterialPropertyBlock();
            MakeRimAndEyes();
        }

        void MakeRimAndEyes()
        {
            // rim: a copy of the body's sprite drawn as an outline, on top of it
            if (_anim != null && _anim.Renderer != null && RimMat != null)
            {
                var r = new GameObject("Rim");
                r.transform.SetParent(_anim.transform, false);
                _rim = r.AddComponent<SpriteRenderer>();
                _rim.sharedMaterial = RimMat;
            }
            if (_showEyes) { _eyeL = Eye("Eye L"); _eyeR = Eye("Eye R"); }
            _blinkT = Random.Range(2f, 5f);
        }

        Vector2 BodyPos { get { return (Vector2)transform.position + new Vector2(0, _anim != null ? _anim.FigureHeight * 0.45f : 0.4f); } }

        SpriteRenderer Eye(string name)
        {
            var g = new GameObject(name);
            g.transform.SetParent(transform, false);
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = DungeonFx.Pixel;
            float px = Mathf.Clamp(2.2f * _size, 2f, 4.5f);   // pixels (the Pixel sprite is 1 art pixel)
            g.transform.localScale = new Vector3(px, px * 0.8f, 1f);
            if (DungeonFx.GlowMat != null) sr.sharedMaterial = DungeonFx.GlowMat;
            return sr;
        }

        void LateUpdate()
        {
            if (_anim == null || _anim.Renderer == null || _set == null) return;
            var body = _anim.Renderer;
            float alpha = body.color.a;                       // spawn fade-in
            int order = DungeonController.SortOrder(transform.position.y);

            // how dark is it where I stand? (smoothed so it doesn't flicker)
            float lit = DungeonLight.At(BodyPos);
            bool shadow = _e != null && _e.Shadow;
            if (shadow && !_shadowLook)
            {
                _shadowLook = true;
                _rimCol = new Color(0.55f, 0.3f, 1f); _eyeCol = new Color(0.85f, 0.45f, 1f);
            }
            float wantDark = 1f - Mathf.Clamp01(Mathf.InverseLerp(_set.silhouetteBelow, _set.silhouetteAbove, lit));
            _dark = Mathf.MoveTowards(_dark, wantDark, Time.deltaTime * 3f);

            // the body sinks to a near-black silhouette in the dark (on top of whatever tint the enemy set this frame)
            if (!_wrote || body.color != _wroteCol) _baseCol = body.color;
            float k = Mathf.Lerp(1f, 0.1f, _dark * _maxDark);
            float va = shadow ? Mathf.Lerp(1f, 0.12f, _dark) : 1f;   // a shadow creature all but disappears out of the light
            _wroteCol = new Color(_baseCol.r * k, _baseCol.g * k, _baseCol.b * k, _baseCol.a * va);
            body.color = _wroteCol; _wrote = true;

            if (_rim != null)
            {
                _rim.sprite = body.sprite; _rim.flipX = body.flipX; _rim.flipY = body.flipY;
                _rim.sortingLayerID = body.sortingLayerID; _rim.sortingOrder = order + 1;
                _rim.GetPropertyBlock(_mpb);
                _mpb.SetColor(RimColorId, _rimCol * 1.6f);
                _mpb.SetFloat(AmountId, Mathf.Lerp(0.06f, shadow ? 0.3f : 1f, _dark) * alpha * _rimK);   // just a hint of rim in light
                _rim.SetPropertyBlock(_mpb);
            }

            // eyes: two dots at head height, following the facing; hidden facing away, blinking now and then
            float h = _anim.FigureHeight * _eyeH;
            float spread = _eyeS * Mathf.Max(0.8f, _size);
            Vector2 l, r; bool show = true;
            switch (_anim.Dir)
            {
                case Facing.Up: l = r = Vector2.zero; show = false; break;
                case Facing.Left: l = new Vector2(-spread * 1.2f, h); r = new Vector2(-spread * 0.2f, h); break;
                case Facing.Right: l = new Vector2(spread * 0.2f, h); r = new Vector2(spread * 1.2f, h); break;
                default: l = new Vector2(-spread * 0.5f, h); r = new Vector2(spread * 0.5f, h); break;
            }
            _blinkT -= Time.deltaTime;
            if (_blinkT <= 0f) { _blinkT = Random.Range(2.5f, 6f); _blinkUntil = Time.time + 0.12f; }
            if (Time.time < _blinkUntil) show = false;
            float eyeA = show ? Mathf.Clamp01(_dark * 1.1f) * alpha : 0f;
            SetEye(_eyeL, l, eyeA, order + 2);
            SetEye(_eyeR, r, eyeA, order + 2);

            // carriers flicker / pulse
            float t = Time.time + _seed;
            if (_lantern != null)
            {
                _lantern.intensity = 1.25f * (1f + 0.08f * Mathf.Sin(t * 11f) + 0.05f * Mathf.Sin(t * 23f));
                if (_lanternSr != null)
                {
                    var lp = _lanternSr.transform.localPosition;
                    lp.x = Mathf.Abs(lp.x) * (_anim.Dir == Facing.Left ? -1f : 1f);
                    _lanternSr.transform.localPosition = lp;
                    _lanternSr.sortingOrder = order + (_anim.Dir == Facing.Up ? -1 : 2);
                    var c = _lanternSr.color; c.a = alpha; _lanternSr.color = c;
                }
            }
            if (_boss != null) _boss.intensity = 1.5f * (0.85f + 0.15f * Mathf.Sin(t * 2.2f));
            if (_carry != null) _carry.intensity = 1.2f * (0.9f + 0.1f * Mathf.Sin(t * 5f));
        }

        void SetEye(SpriteRenderer sr, Vector2 local, float a, int order)
        {
            if (sr == null) return;
            sr.transform.localPosition = local;
            var c = _eyeCol; c.a = a; sr.color = c;
            sr.enabled = a > 0.01f;
            sr.sortingOrder = order;
        }
    }
}
