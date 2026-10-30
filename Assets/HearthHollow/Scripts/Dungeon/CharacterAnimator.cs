// Hearth & Hollow — plays a pack character's Idle / Move / Attack rows in four directions.
// Lives on a child "Body" object; the parent's position is the character's feet.
using HearthHollow.Data;
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public enum AnimState { Idle = 0, Move = 1, Attack = 2 }
    public enum Facing { Down = 0, Left = 1, Right = 2, Up = 3 }

    [RequireComponent(typeof(SpriteRenderer))]
    public class CharacterAnimator : MonoBehaviour
    {
        public ArtLibrary.Anim anim;
        public float fps = 8f;

        public AnimState State { get; private set; }
        public Facing Dir = Facing.Down;
        public SpriteRenderer Renderer { get; private set; }
        /// Height of the visible figure in world units (from the anim's crop).
        public float FigureHeight { get; private set; }

        float _t;
        float _attackLen;   // seconds the current attack anim should take
        float _attackT;

        public static Facing FacingFrom(Vector2 v, Facing fallback)
        {
            if (v.sqrMagnitude < 0.0001f) return fallback;
            if (Mathf.Abs(v.x) > Mathf.Abs(v.y)) return v.x < 0 ? Facing.Left : Facing.Right;
            return v.y < 0 ? Facing.Down : Facing.Up;
        }

        public static Vector2 Vector(Facing f)
        {
            switch (f) { case Facing.Left: return Vector2.left; case Facing.Right: return Vector2.right; case Facing.Up: return Vector2.up; default: return Vector2.down; }
        }

        /// <param name="pixelsPerUnit">Source pixels per world unit (32 = a tile per 32 art pixels).</param>
        public void Setup(ArtLibrary.Anim a, float pixelsPerUnit, float sizeMult)
        {
            anim = a;
            Renderer = GetComponent<SpriteRenderer>();
            Sprite first = a != null ? a.Frame(0, 0, 0) : null;
            float spritePpu = first != null ? first.pixelsPerUnit : 100f;
            float scale = spritePpu / Mathf.Max(1f, pixelsPerUnit) * sizeMult;
            transform.localScale = new Vector3(scale, scale, 1f);

            // put the figure's feet (bottom of the crop) on the parent's origin, centred horizontally
            if (a != null && first != null && a.crop.width > 0)
            {
                Vector2 pivot = first.pivot;                         // pixels within the frame
                float cx = a.crop.x + a.crop.width / 2f, footY = a.crop.y + 2f, heightPx = a.crop.height;
                // The crop also covers attack frames (a Ranger's arrow, a slime's splash), which reach below the
                // feet and lifted the figure off the ground. Stand it on its idle frames' lowest pixel instead.
                float lo, hi;
                if (IdleBounds(a, first.rect.height, out lo, out hi)) { footY = lo + 1f; heightPx = hi - lo; }
                transform.localPosition = new Vector3(-(cx - pivot.x) / spritePpu * scale, -(footY - pivot.y) / spritePpu * scale, 0f);
                FigureHeight = heightPx / spritePpu * scale;
            }
            else
            {
                FigureHeight = 0.8f;
                if (a == null)   // no art for this character: a plain blob so it's still visible
                {
                    Renderer.sprite = DungeonFx.Circle;
                    transform.localScale = new Vector3(sizeMult * 1.6f, sizeMult * 1.6f, 1f);
                    transform.localPosition = new Vector3(0, 0.4f * sizeMult, 0);
                }
            }
            Apply();
        }

        /// Lowest and highest visible pixel (cell pixels from the bottom) over every idle frame, read from the
        /// sprites' tight meshes. False when the sprites use full-rect meshes, which don't follow the pixels.
        static bool IdleBounds(ArtLibrary.Anim a, float cellH, out float lo, out float hi)
        {
            lo = float.MaxValue; hi = float.MinValue;
            int cols = Mathf.Max(1, a.Cols(0));
            for (int d = 0; d < 4; d++)
                for (int c = 0; c < cols; c++)
                {
                    Sprite sp = a.Frame(0, d, c);
                    if (sp == null) continue;
                    foreach (Vector2 v in sp.vertices)
                    {
                        float y = v.y * sp.pixelsPerUnit + sp.pivot.y;
                        if (y < lo) lo = y; if (y > hi) hi = y;
                    }
                }
            return hi > lo && hi - lo > 4f && hi - lo < cellH * 0.95f;
        }

        public void SetMoving(bool moving)
        {
            if (State == AnimState.Attack) return;
            AnimState s = moving ? AnimState.Move : AnimState.Idle;
            if (s != State) { State = s; _t = 0f; }
        }

        /// Play the attack row once over <paramref name="seconds"/>.
        public void PlayAttack(float seconds)
        {
            State = AnimState.Attack; _attackLen = Mathf.Max(0.05f, seconds); _attackT = 0f; _t = 0f;
            Apply();
        }

        public bool Attacking { get { return State == AnimState.Attack; } }

        void Update()
        {
            if (anim == null || Renderer == null) return;
            if (State == AnimState.Attack)
            {
                _attackT += Time.deltaTime;
                if (_attackT >= _attackLen) { State = AnimState.Idle; _t = 0f; }
            }
            else _t += Time.deltaTime;
            Apply();
        }

        void Apply()
        {
            if (anim == null || Renderer == null) return;
            int s = (int)State;
            int cols = Mathf.Max(1, anim.Cols(s));
            int frame;
            if (State == AnimState.Attack) frame = Mathf.Min(cols - 1, (int)(_attackT / _attackLen * cols));
            else frame = (int)(_t * (State == AnimState.Move ? fps * 1.25f : fps * 0.8f));
            Sprite sp = anim.Frame(s, (int)Dir, frame);
            if (sp != null) Renderer.sprite = sp;
        }
    }
}
