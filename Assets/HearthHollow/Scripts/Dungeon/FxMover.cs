// Hearth & Hollow — an empty point that flies: something for an effect to follow (a thrown crescent slash rides one).
// Tick is called every frame with where it is, so whoever threw it can hit what it passes.
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public class FxMover : MonoBehaviour
    {
        public Vector2 Velocity;
        public float Drag;              // slows it down (per second)
        public float FlyTime = 0.5f;    // seconds it moves for…
        public float Linger = 1f;       // …then it stays put this long (so its effect can fade where it stopped)
        public System.Action<Vector2> Tick;   // each frame while it flies
        public System.Action<Vector2> Landed; // once, when it stops
        float _t; bool _landed;

        public static FxMover Throw(Vector2 from, Vector2 velocity, float flyTime)
        {
            var go = new GameObject("fx_mover");
            go.transform.position = from;
            var m = go.AddComponent<FxMover>();
            m.Velocity = velocity; m.FlyTime = flyTime;
            return m;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _t += dt;
            if (_t <= FlyTime)
            {
                transform.position += (Vector3)(Velocity * dt);
                Velocity *= Mathf.Max(0f, 1f - Drag * dt);
                if (Tick != null) Tick(transform.position);
            }
            else if (!_landed)
            {
                _landed = true;
                if (Landed != null) Landed(transform.position);
            }
            if (_t > FlyTime + Linger) Destroy(gameObject);
        }
    }
}
