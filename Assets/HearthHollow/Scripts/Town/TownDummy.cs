// Hearth & Hollow — a straw training dummy in the training yard. Hit it (your swing, or the sword-master's) and it rocks
// on its post and springs back, shedding a little straw.
using HearthHollow.Dungeon;
using UnityEngine;

namespace HearthHollow.Town
{
    public class TownDummy : MonoBehaviour
    {
        Transform _pivot;     // rocks round its foot
        float _ang, _vel;
        public void Setup(Transform pivot) { _pivot = pivot; }

        public void Hit(Vector2 dir, bool hard)
        {
            _vel += (dir.x >= 0f ? -1f : 1f) * (hard ? 340f : 220f);
            DungeonFx.Puff((Vector2)transform.position + new Vector2(0, 0.9f), new Color(0.92f, 0.82f, 0.45f, 0.75f), hard ? 7 : 4, 0.35f);   // straw
            DungeonFx.Sparks((Vector2)transform.position + new Vector2(0, 0.9f), Vector2.up, new Color(0.95f, 0.85f, 0.5f), hard ? 5 : 3);
        }

        void Update()
        {
            if (_pivot == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            _vel += (-90f * _ang - 7f * _vel) * dt;   // a damped spring back to upright
            _ang = Mathf.Clamp(_ang + _vel * dt, -40f, 40f);
            _pivot.localRotation = Quaternion.Euler(0, 0, _ang);
        }
    }
}
