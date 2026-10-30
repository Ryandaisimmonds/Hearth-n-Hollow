// Hearth & Hollow — a doorway between two rooms. Bars drop while a room's event is running.
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public class DoorGate : MonoBehaviour
    {
        public RoomInstance Room { get; private set; }
        public Side Side { get; private set; }
        public bool Open { get; private set; }

        SpriteRenderer _sr;
        BoxCollider2D _solid;
        Sprite _open, _closed;

        public void Setup(RoomInstance room, Side side, Sprite openSprite, Sprite closedSprite)
        {
            Room = room; Side = side; _open = openSprite; _closed = closedSprite;
            Vector2 c = RoomTemplate.DoorCentre(side);
            bool vertical = side == Side.East || side == Side.West;
            // bars sit on the inner wall line; north door bars sit on the face row
            Vector2 barPos = side == Side.North ? new Vector2(c.x, RoomTemplate.Height - 1.5f) : c;
            transform.localPosition = barPos;
            transform.localRotation = Quaternion.Euler(0, 0, vertical ? 90f : 0f);

            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = 5;
            _solid = gameObject.AddComponent<BoxCollider2D>();
            // 3 cells deep, extending outward (local +y is world −x when rotated), so nothing squeezes past
            _solid.size = new Vector2(3f, 3f);
            switch (side)
            {
                case Side.North: _solid.offset = new Vector2(0, 1f); break;
                case Side.South: _solid.offset = new Vector2(0, -1f); break;
                case Side.West: _solid.offset = new Vector2(0, 1f); break;
                default: _solid.offset = new Vector2(0, -1f); break;
            }

            // the walk-through trigger: the outer edge of the opening
            var trig = new GameObject("Trigger");
            trig.transform.SetParent(room.transform, false);
            Vector2 tPos, tSize;
            switch (side)
            {
                case Side.North: tPos = new Vector2(c.x, RoomTemplate.Height - 0.5f); tSize = new Vector2(3f, 1f); break;
                case Side.South: tPos = new Vector2(c.x, 0.3f); tSize = new Vector2(3f, 0.6f); break;
                case Side.West: tPos = new Vector2(0.3f, c.y); tSize = new Vector2(0.6f, 3f); break;
                default: tPos = new Vector2(RoomTemplate.Width - 0.3f, c.y); tSize = new Vector2(0.6f, 3f); break;
            }
            trig.transform.localPosition = tPos;
            var box = trig.AddComponent<BoxCollider2D>();
            box.isTrigger = true; box.size = tSize;
            trig.AddComponent<DoorTrigger>().gate = this;
            SetOpen(true);
        }

        public void SetOpen(bool open)
        {
            Open = open;
            if (_solid != null) _solid.enabled = !open;
            if (_sr != null)
            {
                _sr.sprite = open ? _open : _closed;
                _sr.enabled = _sr.sprite != null;
            }
        }
    }

    public class DoorTrigger : MonoBehaviour
    {
        public DoorGate gate;

        void OnTriggerEnter2D(Collider2D other) { Try(other); }
        void OnTriggerStay2D(Collider2D other) { Try(other); }

        void Try(Collider2D other)
        {
            if (gate == null || !gate.Open || other.isTrigger) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            if (gate.Room != null && gate.Room.Controller != null) gate.Room.Controller.GoThrough(gate.Room, gate.Side);
        }
    }
}
