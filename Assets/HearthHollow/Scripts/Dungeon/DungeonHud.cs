// Hearth & Hollow — what the dungeon tells the HUD. The dungeon owns one DungeonHudState (DungeonController.Hud),
// writes it every frame, and swaps in a fresh one for each floor; DungeonScreen reads it.
using System.Collections.Generic;
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public sealed class DungeonHudState
    {
        public struct MapRoom
        {
            public Vector2Int cell; public RoomKind kind;
            public bool visited, current, cleared;
            public bool n, s, e, w;
        }
        /// Loot on the floor, as tags the HUD pins above each pickup (emoji + count). Rebuilt every frame.
        public struct LootTag { public Vector3 at; public string text; }

        public const float IntroLength = 2.8f;   // seconds the boss's title card is up

        public bool Active;            // a dungeon floor exists
        public bool BagOpen;           // the HUD's bag panel is open (pauses the dungeon)
        public float Fog01;            // the fall's rising fog (a purple veil over the screen)
        public string Tutorial;        // the fall's how-to-play line (null = none)
        public bool InFall;            // hide the locked-skill hints
        public float Skill01 = 1f;     // 1 = ready
        public bool SkillUnlocked;
        public float Dash01 = 1f;
        public bool Guarding;
        public string BuffText;        // e.g. "🛡 Guard  📯 Fury" (null = none)
        public HearthHollow.Core.MoveData Skill1Move, Skill2Move, DashMove;   // what's in K / U / Space
        public float Skill2_01 = 1f;
        public HearthHollow.Core.MoveData AttackMove;   // what's on left click / J
        public float Attack01 = 1f;
        public HearthHollow.Core.MoveData UltMove;      // the class's ultimate (C / I)
        public float Ult01;
        public float FlareCooldown;
        public string Prompt;          // e.g. "E  Forage" (null = none)
        public string Status;          // e.g. "Clear the room!"
        public bool Locked;            // doors shut
        public float BossHp01 = -1f;   // < 0 = no boss bar
        public string BossName;
        public string IntroName, IntroSub;   // the boss's title card
        public float IntroT = -1f;           // seconds into the card (< 0 = none)
        public readonly List<MapRoom> Map = new List<MapRoom>();
        public int MapVersion;               // goes up whenever Map changes
        public readonly List<LootTag> LootTags = new List<LootTag>();
    }

    public static class DungeonHud
    {
        /// Floating text at a world position (drawn by the HUD screen; nothing happens when there's no dungeon).
        public static void Number(Vector3 world, string text, string cls)
        {
            var dc = DungeonController.Instance;
            if (dc != null) dc.ShowNumber(world, text, cls);
        }
    }
}
