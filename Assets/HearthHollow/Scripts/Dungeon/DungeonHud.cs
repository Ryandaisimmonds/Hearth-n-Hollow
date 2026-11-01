// Hearth & Hollow — what the dungeon tells the HUD (written by the dungeon each frame, read by DungeonScreen).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public static class DungeonHud
    {
        public struct MapRoom
        {
            public Vector2Int cell; public RoomKind kind;
            public bool visited, current, cleared, known;
            public bool n, s, e, w;
        }

        public static bool Active;            // a dungeon floor exists
        public static bool BagOpen;           // HUD's bag panel is open (pauses the dungeon)
        public static float Skill01 = 1f;     // 1 = ready
        public static bool SkillUnlocked;
        public static float Dash01 = 1f;
        public static bool Guarding;
        public static string BuffText;        // e.g. "🛡 Guard  📯 Fury" (null = none)
        public static HearthHollow.Core.MoveData Skill1Move, Skill2Move, DashMove;   // what's in K / U / Space
        public static float Skill2_01 = 1f;
        public static HearthHollow.Core.MoveData AttackMove;   // what's on left click / J
        public static float Attack01 = 1f;
        public static HearthHollow.Core.MoveData UltMove;   // the class's ultimate (C / I)
        public static float Ult01;
        public static float TorchCooldown;    // seconds until you can throw another torch (T / LB)
        public static float Torch01 = 1f;     // 1 = ready
        public static float FlareCooldown;
        public static int BoostPoints, BoostMax = 5, BoostSel;   // Break & Boost (step 8)    // seconds until Flare is ready (F / LT)
        public static string Prompt;          // e.g. "E  Forage" (null = none)
        public static string Status;          // e.g. "Clear the room!"
        public static bool Locked;            // doors shut
        public static float BossHp01 = -1f;   // < 0 = no boss bar
        public static string BossName;
        public static readonly List<MapRoom> Map = new List<MapRoom>();
        public static int MapVersion;

        /// Floating text at a world position (set by the HUD screen). text, extra USS class.
        public static Action<Vector3, string, string> ShowNumber;
        /// Screen flash when the player is hurt.
        public static Action PlayerHurt;

        public static void Number(Vector3 world, string text, string cls)
        {
            if (ShowNumber != null) ShowNumber(world, text, cls);
        }

        public static void Reset()
        {
            Active = false; BagOpen = false; Skill01 = 1f; Dash01 = 1f; Guarding = false;
            BuffText = null; Skill1Move = Skill2Move = DashMove = null; Skill2_01 = 1f;
            Prompt = null; Status = null; Locked = false; BossHp01 = -1f; BossName = null;
            Map.Clear(); MapVersion++;
        }
    }
}
