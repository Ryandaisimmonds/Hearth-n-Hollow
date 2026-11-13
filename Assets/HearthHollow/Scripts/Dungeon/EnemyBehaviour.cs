// Hearth & Hollow — how each kind of monster fights (its EnemyArchetype): when it decides to strike, the marker a boss
// shows while winding up, what the strike is, how fast and far a charge goes, and how it moves the rest of the time.
// EnemyController keeps the rest: health, statuses, hits, death, light and the state clock these hook into.
using HearthHollow.Core;
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public abstract class EnemyBehaviour
    {
        protected EnemyController E;
        protected MonsterData Data { get { return E.Data; } }

        public static EnemyBehaviour For(EnemyController e)
        {
            EnemyBehaviour b;
            switch (e.Data.archetype)
            {
                case EnemyArchetype.Charger: b = new ChargerBehaviour(); break;
                case EnemyArchetype.Flyer: b = new FlyerBehaviour(); break;
                case EnemyArchetype.Caster: b = new CasterBehaviour(); break;
                case EnemyArchetype.Brute: b = new BruteBehaviour(); break;
                default: b = new ChaserBehaviour(); break;
            }
            b.E = e;
            return b;
        }

        /// Close enough (and off cooldown, checked by the caller) to start winding up.
        public virtual bool WantsToStrike(float dist) { return dist <= Data.attackRange; }
        /// A windup has started (a brute picks breath or charge now, so its marker shows the right one).
        public virtual void OnWindup() { }
        /// A boss's ground marker for the strike it's winding up (null for a fan of lanes, which can't be cancelled).
        public abstract Telegraph Telegraph(Vector2 from, Vector2 dir, float seconds, float width);
        /// The windup is over: strike.
        public abstract void Release(Vector2 dir);
        /// How it moves while it isn't attacking (before light-steering and crowd-avoiding).
        public abstract Vector2 Move(float dist, Vector2 dir, Vector2 to, float speed);
        /// Its speed while striking (a chaser's lunge).
        public virtual Vector2 StrikeVelocity() { return Vector2.zero; }
        /// A charge: its speed and how long it lasts.
        public virtual float ChargeSpeed { get { return 10f; } }
        public virtual float ChargeTime { get { return 0.55f; } }
        /// Its speed mid-charge (`mult` = its own speed boost; a bat's swoop bends it).
        public virtual Vector2 ChargeVelocity(Vector2 lockDir, float mult, float timeLeft) { return lockDir * ChargeSpeed * mult; }
        /// The colour of its shots.
        public virtual Color ShotColour { get { return new Color(0.75f, 0.45f, 1f); } }

        protected Telegraph Lane(Vector2 from, Vector2 dir, float seconds, float width)
        {
            return HearthHollow.Dungeon.Telegraph.Lane(from, dir, E.ChargeReach() + 0.6f, width, seconds);
        }
        protected void Charge(Vector2 dir) { E.StartCharge(dir, ChargeTime); }
    }

    /// Walks at you and bites when close; wolves circle round you while they wait their turn.
    public sealed class ChaserBehaviour : EnemyBehaviour
    {
        public override Telegraph Telegraph(Vector2 from, Vector2 dir, float seconds, float width)
        {
            return HearthHollow.Dungeon.Telegraph.Ring(from, Data.attackRange + 0.35f + 0.3f * E.Profile.scale, seconds);   // a bite: everything in reach
        }

        public override void Release(Vector2 dir) { E.Lunge(dir); }

        public override Vector2 Move(float dist, Vector2 dir, Vector2 to, float speed)
        {
            if (Data.circles && E.Cooldown > 0.15f && dist < 5.5f) return E.CircleVelocity(to, speed);
            return dist > Data.attackRange * 0.8f ? dir * speed : Vector2.zero;
        }

        public override Vector2 StrikeVelocity() { return E.LockDir * 2.5f; }
    }

    /// Winds up, then rushes in a straight line.
    public sealed class ChargerBehaviour : EnemyBehaviour
    {
        public override bool WantsToStrike(float dist) { return dist < 6.5f || dist <= Data.attackRange; }
        public override Telegraph Telegraph(Vector2 from, Vector2 dir, float seconds, float width) { return Lane(from, dir, seconds, width); }
        public override void Release(Vector2 dir) { Charge(dir); }
        public override Vector2 Move(float dist, Vector2 dir, Vector2 to, float speed) { return dir * speed * 0.6f; }
    }

    /// Weaves around, darts in to sting; bats swoop in an arc.
    public sealed class FlyerBehaviour : EnemyBehaviour
    {
        float _swoopSide = 1f;   // which side it swoops round

        public override Telegraph Telegraph(Vector2 from, Vector2 dir, float seconds, float width) { return Lane(from, dir, seconds, width); }

        public override void Release(Vector2 dir)
        {
            Charge(dir);
            _swoopSide = Random.value < 0.5f ? -1f : 1f;
        }

        public override Vector2 Move(float dist, Vector2 dir, Vector2 to, float speed)
        {
            var v = (dir + new Vector2(-dir.y, dir.x) * Mathf.Sin(E.Wobble * 4f) * 0.9f).normalized * speed;
            if (dist < Data.attackRange * 0.7f) v *= 0.3f;
            return v;
        }

        public override float ChargeSpeed { get { return 9f; } }
        public override float ChargeTime { get { return 0.22f; } }

        public override Vector2 ChargeVelocity(Vector2 lockDir, float mult, float timeLeft)
        {
            float cs = ChargeSpeed;
            var v = lockDir * cs * mult;
            if (Data.swoops)
            {   // it bows out and back, ending on the same line
                float k = 1f - Mathf.Clamp01(timeLeft / 0.22f);
                v += new Vector2(-lockDir.y, lockDir.x) * _swoopSide * Mathf.Cos(k * Mathf.PI) * cs * 0.9f;
            }
            return v;
        }
    }

    /// Keeps its distance and throws orbs (and may heal allies: MonsterData.healsAllies).
    public sealed class CasterBehaviour : EnemyBehaviour
    {
        public override bool WantsToStrike(float dist) { return dist < 9f; }
        public override Telegraph Telegraph(Vector2 from, Vector2 dir, float seconds, float width) { return HearthHollow.Dungeon.Telegraph.Lane(from, dir, 8f, 0.4f, seconds); }

        public override void Release(Vector2 dir)
        {
            E.Shoot(dir, 0f);
            E.Strike(0.2f);
        }

        public override Vector2 Move(float dist, Vector2 dir, Vector2 to, float speed)
        {
            if (dist < 3.5f) return -dir * speed;
            if (dist > 6f) return dir * speed;
            return new Vector2(-dir.y, dir.x) * speed * 0.6f * Mathf.Sign(Mathf.Sin(E.Wobble * 0.7f));
        }
    }

    /// Big and slow: a five-shot breath or a charge, taking turns (bosses, drakes).
    public sealed class BruteBehaviour : EnemyBehaviour
    {
        bool _breathNext;

        public override bool WantsToStrike(float dist) { return dist < 7f; }
        public override void OnWindup() { _breathNext = !_breathNext; }

        public override Telegraph Telegraph(Vector2 from, Vector2 dir, float seconds, float width)
        {
            if (!_breathNext) return Lane(from, dir, seconds, width);
            HearthHollow.Dungeon.Telegraph.Fan(from, dir, 6.5f, 14f, 2, seconds);   // the five-shot breath
            return null;
        }

        public override void Release(Vector2 dir)
        {
            if (_breathNext) { for (int i = -2; i <= 2; i++) E.Shoot(dir, i * 14f); E.Strike(0.4f); }
            else Charge(dir);
        }

        public override Vector2 Move(float dist, Vector2 dir, Vector2 to, float speed)
        {
            if (Data.circles && E.Cooldown > 0.15f && dist < 5.5f) return E.CircleVelocity(to, speed);   // walks at you like a chaser
            return dist > Data.attackRange * 0.8f ? dir * speed : Vector2.zero;
        }

        public override float ChargeSpeed { get { return 8f; } }
        public override float ChargeTime { get { return 0.7f; } }
        public override Color ShotColour { get { return new Color(1f, 0.55f, 0.2f); } }
    }
}
