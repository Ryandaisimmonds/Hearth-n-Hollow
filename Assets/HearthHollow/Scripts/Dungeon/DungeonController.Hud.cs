// Hearth & Hollow — DungeonController: what the floor tells the HUD each frame (the prompt, the room's status, the
// boss bar) and the map of the rooms you know.
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public partial class DungeonController
    {
        void WriteHud()
        {
            // prompt + status for the HUD
            var near = GameplayActive ? Nearest(Player, 1.4f) : null;
            float lootD = 99f; var lootNear = GameplayActive && _loot != null ? _loot.Nearest(Player.Feet, 1.2f, out lootD) : null;
            if (lootNear != null && (near == null || lootD <= Vector2.Distance(Player.Feet, near.transform.position)))
            {
                var ing = Session.Content.Ingredient(lootNear.ingredientId);
                Hud.Prompt = "Pick up " + (ing != null ? ing.emoji + " " + ing.name : lootNear.ingredientId) + (lootNear.count > 1 ? " ×" + lootNear.count : "");
            }
            else if (near == null && GameplayActive && NearestDig(Player.Feet) != null) Hud.Prompt = "⛏️ Dig here";
            else Hud.Prompt = near != null ? near.Prompt(this) : null;
            Hud.Locked = _current != null && _current.EventRunning;
            Hud.Status = StatusText();
            if (_current != null && _current.Boss != null && !_current.Boss.Dead)
            {
                Hud.BossHp01 = _current.Boss.Hp / (float)Mathf.Max(1, _current.Boss.Stats.maxHp);
                Hud.BossName = _current.Boss.DisplayName;
            }
            else Hud.BossHp01 = -1f;
        }

        string StatusText()
        {
            if (_current == null) return null;
            if (_current.EventRunning)
            {
                int left = 0; foreach (var e in _current.Enemies) if (e != null && !e.Dead) left++;
                return _current.Node.kind == RoomKind.Boss ? "Defeat the guardian!" : left > 0 ? "Clear the room!  " + left + " left" : "Clear the room!";
            }
            switch (_current.Node.kind)
            {
                case RoomKind.Ladder: return "A ladder leads deeper";
                case RoomKind.Exit: return "A way home";
                case RoomKind.Boss: return "The way out is open";
                case RoomKind.Gather: return "A quiet grove — forage here";
                case RoomKind.Rest: return "A campfire crackles";
            }
            return null;
        }

        void RefreshMap()
        {
            Hud.Map.Clear();
            if (_plan == null) return;
            var known = new HashSet<RoomNode>();
            foreach (var kv in _rooms)
            {
                if (!kv.Value.Visited) continue;
                known.Add(kv.Key);
                foreach (Side s in System.Enum.GetValues(typeof(Side))) { var n = _plan.Neighbour(kv.Key, s); if (n != null) known.Add(n); }
            }
            foreach (var node in known)
            {
                RoomInstance ri; _rooms.TryGetValue(node, out ri);
                Hud.Map.Add(new DungeonHudState.MapRoom
                {
                    cell = node.cell, kind = node.kind,
                    visited = ri != null && ri.Visited, current = ri == _current, cleared = ri != null && ri.Cleared,
                    n = node.Has(Side.North), s = node.Has(Side.South), e = node.Has(Side.East), w = node.Has(Side.West)
                });
            }
            Hud.MapVersion++;
        }
    }
}
