// Hearth & Hollow — the opening fall's extras in the dungeon: rooms that get darker and foggier the further in you go,
// the last room's fog that bites until you fall, and the first two rooms' how-to-play lines.
using HearthHollow.Core;
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public sealed class FallGuide
    {
        readonly DungeonController _dc;
        float _fogTarget;   // the fall's fog veil
        int _tutSeed = -1; bool _tMoved, _tAttacked, _tSkill, _tFlare;   // what the tutorial has seen you do

        public FallGuide(DungeonController dc) { _dc = dc; }

        /// You walked into a room of the fall: how dark it is (× the room light). Sets the fog too.
        public float EnterRoom(RoomInstance room)
        {
            int d = room.Node.depth;
            float gloom = Mathf.Clamp01((d - 2) / 3f);
            _fogTarget = room.Node.fog ? _fogTarget : gloom * 0.4f;
            return d == 2 ? 0.8f : 1f - 0.5f * gloom;
        }

        /// Once a frame: the tutorial line, and the fog easing towards its target.
        public void Tick()
        {
            UpdateTutorial();
            if (!_dc.Session.InFall) _fogTarget = 0f;
            _dc.Hud.Fog01 = Mathf.MoveTowards(_dc.Hud.Fog01, _fogTarget, Time.unscaledDeltaTime * (_fogTarget >= 1f ? 0.3f : 0.6f));
        }

        public System.Collections.IEnumerator FogRoom()
        {
            _dc.Session.Log("🌫️ The fog pours in…");
            if (_dc.UI != null) _dc.UI.Juice.Toast("🌫️ The fog pours in…", "toast--warn", 3f);
            _fogTarget = 1f;
            yield return new WaitForSeconds(1.2f);
            while (_dc.Session.InDungeon && _dc.Session.InFall && _dc.Session.Run.hp > 0)
            {
                _dc.Session.Story.FallFogBite();
                if (_dc.Player != null && _dc.Session.InDungeon) DungeonHud.Number(_dc.Player.transform.position + Vector3.up * 1.3f, "🌫️", "dnum--hazard");
                yield return new WaitForSeconds(0.5f);
            }
        }

        /// The fall's first two rooms teach you to play, one line at a time.
        void UpdateTutorial()
        {
            _dc.Hud.InFall = _dc.Session.InFall;
            if (!_dc.Session.InFall || _dc.CurrentRoom == null) { _dc.Hud.Tutorial = null; return; }
            if (_tutSeed != _dc.Session.Run.dungeonSeed) { _tutSeed = _dc.Session.Run.dungeonSeed; _tMoved = _tAttacked = _tSkill = _tFlare = false; }
            if (_dc.GameplayActive)
            {
                if (DungeonInput.Move.sqrMagnitude > 0.1f) _tMoved = true;
                if (DungeonInput.Attack) _tAttacked = true;
                if (DungeonInput.Special) _tSkill = true;
                if (DungeonInput.Flare) _tFlare = true;
            }
            bool mouse = DungeonInput.MouseAim;
            int d = _dc.CurrentRoom.Node.depth;
            string line = null;
            if (d <= 1 && !_tMoved) line = "🕹️ WASD to move";
            else if (d <= 1 && !_tAttacked) line = mouse ? "🗡️ Click to attack" : "🗡️ J to attack";
            else if (d == 2 && !_tSkill) line = mouse ? "✨ Right-click for your skill" : "✨ K for your skill";
            else if (d == 2 && !_tFlare) line = "🔆 F for Flare: light up the dark";
            _dc.Hud.Tutorial = line;
        }
    }
}
