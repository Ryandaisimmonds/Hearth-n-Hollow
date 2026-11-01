// Hearth & Hollow — townsfolk at work in Hearth's Hollow: the smith hammering the anvil, the carpenter at the bench,
// the cook stirring the pot, the sword-master sparring with a dummy. Each works on a loop with its own little effects,
// and says a line (a toast) when you walk up — not too often.
using HearthHollow.Dungeon;
using UnityEngine;

namespace HearthHollow.Town
{
    public enum WorkKind { Hammer, Carve, Stir, Spar }

    public class TownWorker : MonoBehaviour
    {
        TownController _tc;
        CharacterAnimator _anim;
        WorkKind _kind;
        Vector2 _work;          // where the work happens (anvil top, bench, pot, dummy)
        Facing _facing;
        string _who; string[] _lines;
        float _next, _barkAt = -99f; int _line, _beat;
        System.Random _rng;

        public void Setup(TownController tc, CharacterAnimator anim, WorkKind kind, Vector2 work, Facing facing, string who, string[] lines, int seed)
        {
            _tc = tc; _anim = anim; _kind = kind; _work = work; _facing = facing; _who = who; _lines = lines;
            _rng = new System.Random(seed);
            _next = Time.time + (float)_rng.NextDouble();
            if (_anim != null) _anim.Dir = _facing;
        }

        void Update()
        {
            if (_tc == null || _anim == null) return;
            _anim.Dir = _facing;
            if (Time.time >= _next)
            {
                _beat++;
                // a rhythm: a few strokes, then a breather
                bool rest = _beat % 5 == 0;
                _next = Time.time + (rest ? 1.6f + (float)_rng.NextDouble() : _kind == WorkKind.Stir ? 0.9f : 0.55f + (float)_rng.NextDouble() * 0.15f);
                if (!rest) Work();
            }
            // say something when you walk up (at most every 40 s each)
            var pl = _tc.Player;
            if (pl != null && _tc.Walking && _lines != null && _lines.Length > 0 && Time.time - _barkAt > 40f
                && Vector2.Distance(pl.transform.position, transform.position) < 2.2f)
            {
                _barkAt = Time.time;
                _tc.Say(_who, _lines[_line++ % _lines.Length]);
            }
        }

        void Work()
        {
            _anim.PlayAttack(0.3f);
            switch (_kind)
            {
                case WorkKind.Hammer:   // clang: sparks off the anvil and a flash of forge-light
                    DungeonFx.Sparks(_work, Vector2.up, new Color(1f, 0.75f, 0.3f), 7);
                    DungeonFx.Embers(_work, new Color(1f, 0.55f, 0.2f), 3, 0.15f);
                    DungeonFx.Flash(_work, new Color(1f, 0.6f, 0.25f), 2.2f, 1.1f, 0.18f);
                    break;
                case WorkKind.Carve:    // sawdust and wood chips
                    DungeonFx.Puff(_work, new Color(0.9f, 0.78f, 0.55f, 0.6f), 4, 0.25f);
                    DungeonFx.Sparks(_work, Vector2.up, new Color(0.85f, 0.65f, 0.4f), 4);
                    break;
                case WorkKind.Stir:     // a swirl of steam and a bubble
                    for (int i = 0; i < 2; i++) TownAmbient.Steam(_work + new Vector2((float)(_rng.NextDouble() - 0.5) * 0.3f, 0.1f), 1.3f);
                    DungeonFx.Embers(_work + new Vector2(0, -0.35f), new Color(1f, 0.6f, 0.25f), 2, 0.2f);
                    break;
                case WorkKind.Spar:     // whack the nearest training dummy
                    foreach (var d in Object.FindObjectsByType<TownDummy>(FindObjectsSortMode.None))
                        if (Vector2.Distance(d.transform.position, _work) < 0.8f) d.Hit(((Vector2)d.transform.position - (Vector2)transform.position).normalized, false);
                    break;
            }
        }

        void LateUpdate()
        {
            if (_anim != null && _anim.Renderer != null) _anim.Renderer.sortingOrder = DungeonController.SortOrder(transform.position.y);
        }
    }
}
