// Hearth & Hollow — ticket 48: loot lying on the floor, drawn as a glowing, bobbing pickup. Press E to pick it up.
// The items themselves live in the run (GameSession.FloorLoot); this just shows them and finds the nearest one.
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Data;
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public class FloorLootView : MonoBehaviour
    {
        DungeonController _dc;
        readonly Dictionary<int, Transform> _views = new Dictionary<int, Transform>();
        readonly Dictionary<int, FloorItem> _items = new Dictionary<int, FloorItem>();
        const float Size = 0.62f;
        const float Apart = 0.7f, HopTime = 0.35f;
        // a new drop hops out from where it fell to its own spot, so several drops don't pile on top of each other
        readonly Dictionary<int, Vector2> _hopFrom = new Dictionary<int, Vector2>();
        readonly Dictionary<int, float> _hopAt = new Dictionary<int, float>();

        public void Init(DungeonController dc) { _dc = dc; }

        /// The nearest floor item within reach of the player, or null.
        public FloorItem Nearest(Vector2 feet, float range, out float dist)
        {
            FloorItem best = null; dist = range;
            if (_dc == null || _dc.Session == null) return null;
            foreach (var it in _dc.Session.FloorLoot)
            {
                if (!it.placed) continue;
                float d = Vector2.Distance(feet, new Vector2(it.x, it.y));
                if (d < dist) { dist = d; best = it; }
            }
            return best;
        }

        void Update()
        {
            if (_dc == null || _dc.Session == null) return;
            var S = _dc.Session;
            var root = _dc.FloorRoot != null ? _dc.FloorRoot.transform : null;
            var live = new HashSet<int>();
            DungeonHud.LootTags.Clear();
            if (root != null && S.Run != null && S.Run.dungeon && (S.InDungeon || S.Phase == Phase.Gather || S.Phase == Phase.Rest))
            {
                foreach (var it in S.FloorLoot)
                {
                    if (!it.placed) Place(it);
                    live.Add(it.id);
                    _items[it.id] = it;
                    Transform tr;
                    if (!_views.TryGetValue(it.id, out tr) || tr == null)
                    {
                        if (!_hopAt.ContainsKey(it.id)) SpreadOut(it, S.FloorLoot);   // first sight of a new drop
                        try { tr = Make(it, root); } catch (System.Exception ex) { Debug.LogException(ex); tr = null; }
                        if (tr == null) continue;
                        _views[it.id] = tr;
                    }
                    Animate(it, tr);
                    var ing = S.Content.Ingredient(it.ingredientId);
                    DungeonHud.LootTags.Add(new DungeonHud.LootTag { at = tr.position + new Vector3(0f, 0.95f, 0f), text = (ing == null ? "🎒" : ArtLibrary.GetIngredient(it.ingredientId) != null ? ing.name : ing.emoji) + (it.count > 1 ? " ×" + it.count : "") });   // with art the tag names it
                }
            }
            var dead = new List<int>();
            foreach (var kv in _views) if (!live.Contains(kv.Key)) dead.Add(kv.Key);
            foreach (var id in dead)
            {
                var tr = _views[id];
                if (tr != null) { DungeonFx.RingAt(tr.position, new Color(1f, 0.9f, 0.5f, 0.8f), 0.6f, 0.25f); Destroy(tr.gameObject); }
                _views.Remove(id); _items.Remove(id); _hopAt.Remove(id); _hopFrom.Remove(id);
            }
        }

        /// A drop with no spot yet (X, or loot from a forage / chest): at the player's feet, nudged into the open.
        void Place(FloorItem it)
        {
            var p = _dc.Player;
            Vector2 at = p != null ? p.Feet + Random.insideUnitCircle * 0.5f + new Vector2(0f, -0.35f) : Vector2.zero;
            var room = _dc.CurrentRoom;
            if (room != null) at = room.NearestWalkable(at);
            it.x = at.x; it.y = at.y; it.placed = true;
        }

        /// Move a new drop off any pile: the nth free spot round a little ring (golden-angle steps), on walkable floor.
        void SpreadOut(FloorItem it, List<FloorItem> all)
        {
            var from = new Vector2(it.x, it.y);
            Vector2 at = from;
            var room = _dc.CurrentRoom;
            for (int n = 0; n < 12; n++)
            {
                bool clash = false;
                foreach (var o in all) if (o != it && o.placed && Vector2.Distance(at, new Vector2(o.x, o.y)) < Apart) { clash = true; break; }
                if (!clash) break;
                float ang = (n * 137.5f + it.id * 41f) * Mathf.Deg2Rad;
                at = from + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (Apart * (0.9f + n * 0.12f));
                if (room != null) at = room.NearestWalkable(at);
            }
            it.x = at.x; it.y = at.y;
            _hopFrom[it.id] = from; _hopAt[it.id] = Time.unscaledTime;
        }

        Transform Make(FloorItem it, Transform root)
        {
            var go = new GameObject("Floor loot · " + it.ingredientId);
            if (root != null) go.transform.SetParent(root, false);
            go.transform.position = new Vector3(it.x, it.y, 0f);
            // a soft gold glow underneath
            var glow = new GameObject("Glow"); glow.transform.SetParent(go.transform, false);
            var gsr = glow.AddComponent<SpriteRenderer>(); gsr.sprite = DungeonFx.Circle; gsr.color = new Color(1f, 0.85f, 0.35f, 0.35f);
            var gb = DungeonFx.Circle.bounds.size; glow.transform.localScale = new Vector3(Size * 1.5f / gb.x, Size * 0.7f / gb.y, 1f);
            // the ingredient itself
            var pic = new GameObject("Item"); pic.transform.SetParent(go.transform, false);
            var sr = pic.AddComponent<SpriteRenderer>();
            var spr = ArtLibrary.GetIngredient(it.ingredientId);   // (a Unity "missing" sprite slips past ??, so test it properly)
            if (spr == null) spr = DungeonFx.Pouch;                 // no ingredient art yet: a pouch, with the emoji tag above it
            sr.sprite = spr;
            var b = sr.sprite.bounds.size; float k = Size / Mathf.Max(0.01f, Mathf.Max(b.x, b.y));
            pic.transform.localScale = new Vector3(k, k, 1f);
            DungeonFx.RingAt(go.transform.position, new Color(1f, 0.9f, 0.5f, 0.8f), 0.7f, 0.3f);
            return go.transform;
        }

        void Animate(FloorItem it, Transform tr)
        {
            float t = Time.unscaledTime + it.id * 0.7f;
            float hop = 1f; Vector2 hopFrom;
            float at0;
            if (_hopAt.TryGetValue(it.id, out at0) && _hopFrom.TryGetValue(it.id, out hopFrom)) hop = Mathf.Clamp01((Time.unscaledTime - at0) / HopTime);
            else hopFrom = new Vector2(it.x, it.y);
            Vector2 p = Vector2.Lerp(hopFrom, new Vector2(it.x, it.y), 1f - (1f - hop) * (1f - hop));
            tr.position = new Vector3(p.x, p.y, 0f);
            if (tr.childCount < 2) return;
            var glow = tr.GetChild(0); var pic = tr.GetChild(1);
            float arc = Mathf.Sin(hop * Mathf.PI) * 0.45f;   // a little bounce as it lands
            pic.localPosition = new Vector3(0f, 0.22f + arc + (hop >= 1f ? Mathf.Sin(t * 3f) * 0.06f : 0f), 0f);
            var gsr = glow.GetComponent<SpriteRenderer>();
            if (gsr != null) { var c = gsr.color; c.a = 0.25f + 0.15f * (0.5f + 0.5f * Mathf.Sin(t * 4f)); gsr.color = c; gsr.sortingOrder = DungeonController.SortOrder(it.y) - 2; }
            var sr = pic.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = DungeonController.SortOrder(it.y);
        }
    }
}
