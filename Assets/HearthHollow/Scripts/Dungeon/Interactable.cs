// Hearth & Hollow — things you use with E in a room: forage spots, campfires, ladders and exits.
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Dungeon
{
    public enum InteractKind { Forage, Campfire, Ladder, Exit }   // (the opening arc's Pit was cut in Design v2, ticket 18)

    public class Interactable : MonoBehaviour
    {
        public InteractKind Kind { get; private set; }
        public bool Used { get; private set; }
        /// Ladders / exits stay hidden until their room's event is cleared.
        public bool Available { get; private set; }
        public RoomInstance Room { get; private set; }

        SpriteRenderer _sr;
        float _t;
        Vector3 _base;
        float _scale = 1f;
        Light2D _light;
        float _lightBase;

        public void Setup(RoomInstance room, InteractKind kind, Sprite sprite, bool available)
        {
            Room = room; Kind = kind;
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = sprite != null ? sprite : DungeonFx.Circle;
            if (sprite == null) _sr.color = kind == InteractKind.Forage ? new Color(0.4f, 1f, 0.4f) : kind == InteractKind.Campfire ? new Color(1f, 0.6f, 0.2f) : new Color(0.6f, 0.75f, 1f);
            _base = transform.localPosition;
            _t = Random.value * 6f;
            // props read better a bit larger than one tile
            _scale = kind == InteractKind.Exit ? 2f : kind == InteractKind.Campfire ? 1.8f : kind == InteractKind.Ladder ? 1.6f : 1.4f;
            transform.localScale = new Vector3(_scale, _scale, 1f);
            // campfires and exits glow
            if (kind == InteractKind.Campfire || kind == InteractKind.Exit)
            {
                var lgo = new GameObject("Glow");
                lgo.transform.SetParent(transform, false);
                lgo.transform.localPosition = new Vector3(0, 0.3f, 0);
                _light = lgo.AddComponent<Light2D>();
                _light.lightType = Light2D.LightType.Point;
                _light.color = kind == InteractKind.Campfire ? new Color(1f, 0.62f, 0.3f) : new Color(0.55f, 0.9f, 1f);
                _lightBase = kind == InteractKind.Campfire ? 1.4f : 1.0f;
                _light.intensity = _lightBase;
                _light.pointLightOuterRadius = kind == InteractKind.Campfire ? 5f : 3.5f;
                _light.pointLightInnerRadius = 0.4f;
                DungeonController.ApplyToAllLayers(_light);
                DungeonController.CastShadows(_light, 0.75f, 0.45f);   // with ShadowSafeLight: never dark if it sits in a wall
            }
            SetAvailable(available);
        }

        public void MarkUsed() { Used = true; }

        public void SetAvailable(bool on)
        {
            Available = on;
            if (_sr != null) _sr.enabled = on;
            if (_light != null) _light.enabled = on;
            if (on) DungeonFx.RingAt(transform.position + new Vector3(0, 0.4f), new Color(1f, 1f, 0.8f, 0.8f), 1f, 0.5f);
        }

        public string Prompt(DungeonController dc)
        {
            switch (Kind)
            {
                case InteractKind.Forage: return "Forage";
                case InteractKind.Campfire: return "Rest by the fire";
                case InteractKind.Ladder: return dc.Session.DungeonHomeChoice ? "Floor " + (dc.Session.Run.floor + 1) + " below — go deeper or head home?" : "Climb down to floor " + (dc.Session.Run.floor + 1);
                default: return dc.Session.DungeonOnLastFloor ? "Leave — route cleared!" : dc.Session.DungeonPortalFloor ? "✨ Step through the portal — home with your haul" : "Head home with your haul";
            }
        }

        /// Returns true if something happened.
        public bool Use(DungeonController dc)
        {
            if (Used || !Available) return false;
            switch (Kind)
            {
                case InteractKind.Forage:
                    if (!dc.Session.DungeonStartGather()) return false;
                    Used = true;
                    if (_sr != null) _sr.color = new Color(0.55f, 0.55f, 0.55f, 0.6f);
                    return true;
                case InteractKind.Campfire:
                    if (!dc.Session.DungeonRest()) return false;
                    Used = true;
                    if (_sr != null) _sr.color = new Color(0.7f, 0.6f, 0.55f);
                    _lightBase *= 0.45f;   // embers
                    return true;
                case InteractKind.Ladder:
                    dc.AskLadder(this);   // go deeper or head home (contentVersion 14); descending marks it used
                    return true;
                default:
                    Used = true;
                    dc.Session.DungeonExit();             // ends the run
                    return true;
            }
        }

        void Update()
        {
            if (_sr == null || !Available) return;
            _t += Time.deltaTime;
            if (_light != null) _light.intensity = _lightBase * (1f + Mathf.Sin(_t * 11f) * 0.07f + Mathf.Sin(_t * 23f) * 0.05f);
            if (Kind == InteractKind.Campfire && !Used)
            {
                float s = _scale * (1f + Mathf.Sin(_t * 9f) * 0.05f + Mathf.Sin(_t * 15f) * 0.03f);
                transform.localScale = new Vector3(s, s, 1f);
            }
            else if (Kind == InteractKind.Exit || Kind == InteractKind.Ladder || (Kind == InteractKind.Forage && !Used))
            {
                transform.localPosition = _base + new Vector3(0, Mathf.Sin(_t * 2.5f) * (Kind == InteractKind.Exit ? 0.08f : 0.03f), 0);
                if (Kind == InteractKind.Exit) transform.localRotation = Quaternion.Euler(0, 0, _t * -40f);
            }
        }

        void LateUpdate() { if (_sr != null) _sr.sortingOrder = DungeonController.SortOrder(transform.position.y) - 1; }
    }
}
