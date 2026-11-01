// Hearth & Hollow — small looping effects that make the town's work areas feel alive: the forge's sparks, smoke and
// flickering glow, steam off the cooking pot, a chimney's smoke, a lantern's flicker.
using HearthHollow.Dungeon;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Town
{
    public enum AmbientKind { Forge, Steam, Smoke, Flicker }

    public class TownAmbient : MonoBehaviour
    {
        AmbientKind _kind;
        Light2D _light; float _baseI;
        float _t, _seed;

        public void Setup(AmbientKind kind, Light2D light = null)
        {
            _kind = kind; _light = light; _baseI = light != null ? light.intensity : 0f;
            _seed = Random.value * 10f;
        }

        /// A soft puff of steam drifting up from `p`.
        public static void Steam(Vector2 p, float rise)
        {
            var fx = DungeonFx.Spawn(DungeonFx.Circle, p, new Color(1f, 1f, 1f, 0.28f), 1.4f, 0.22f, 0.7f);
            if (fx != null) fx.velocity = new Vector2(Random.Range(-0.12f, 0.12f), rise * 0.55f);
        }

        static void SmokePuff(Vector2 p)
        {
            var fx = DungeonFx.Spawn(DungeonFx.Circle, p, new Color(0.45f, 0.43f, 0.42f, 0.35f), 2.2f, 0.3f, 1.0f);
            if (fx != null) fx.velocity = new Vector2(Random.Range(0.05f, 0.25f), 0.55f);
        }

        void Update()
        {
            float dt = Time.deltaTime; _t -= dt;
            Vector2 p = transform.position;
            if (_light != null)   // firelight never sits still
            {
                float tt = Time.time + _seed;
                _light.intensity = _baseI * (0.85f + 0.12f * Mathf.Sin(tt * 9.1f) + 0.08f * Mathf.Sin(tt * 23.7f));
            }
            if (_t > 0f) return;
            switch (_kind)
            {
                case AmbientKind.Forge:
                    _t = Random.Range(0.12f, 0.35f);
                    DungeonFx.Embers(p, new Color(1f, Random.Range(0.45f, 0.7f), 0.2f), 1, 0.3f);
                    if (Random.value < 0.25f) SmokePuff(p + new Vector2(0, 0.6f));
                    break;
                case AmbientKind.Steam:
                    _t = Random.Range(0.25f, 0.5f);
                    Steam(p + new Vector2(Random.Range(-0.15f, 0.15f), 0f), 1f);
                    if (Random.value < 0.3f) DungeonFx.Spawn(DungeonFx.Circle, p + new Vector2(Random.Range(-0.12f, 0.12f), -0.05f), new Color(1f, 1f, 1f, 0.6f), 0.25f, 0.08f, 0.02f);   // a bubble pops
                    break;
                case AmbientKind.Smoke:
                    _t = Random.Range(0.5f, 0.9f);
                    SmokePuff(p);
                    break;
                case AmbientKind.Flicker:
                    _t = 1f;
                    break;
            }
        }
    }
}
