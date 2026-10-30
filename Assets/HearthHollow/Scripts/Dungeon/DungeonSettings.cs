// Hearth & Hollow — everything the real-time dungeon needs: themes, the shared room pool,
// marker tiles and how each monster moves and fights. Built by "Hearth & Hollow > Build Dungeon Kit".
// Damage / HP / rewards come from the content asset's tuning (difficulty-scaled), not from here.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace HearthHollow.Dungeon
{
    public enum EnemyArchetype
    {
        Chaser,   // walks at you and swings when close
        Charger,  // winds up, then rushes in a straight line
        Flyer,    // weaves around, darts in to sting
        Caster,   // keeps its distance and throws orbs; may heal allies
        Brute     // big and slow: breath fan + charge (bosses / drakes)
    }

    [Serializable] public class EnemyProfile
    {
        public string monsterId;
        public EnemyArchetype archetype = EnemyArchetype.Chaser;
        [Tooltip("Tiles per second.")] public float moveSpeed = 2.2f;
        [Tooltip("Starts an attack inside this distance.")] public float attackRange = 1.0f;
        [Tooltip("Seconds of telegraph before a hit lands.")] public float windup = 0.4f;
        [Tooltip("Multiplier on the tuning's monster attack cooldown.")] public float cooldownMult = 1f;
        [Tooltip("Projectile speed for casters / breath.")] public float projectileSpeed = 6f;
        [Tooltip("Hits per attack (wolves bite twice).")] public int hits = 1;
        [Tooltip("Heals the weakest ally now and then (Bog Shaman).")] public bool healsAllies;
        [Tooltip("Visual size multiplier.")] public float scale = 1f;
    }

    [CreateAssetMenu(menuName = "Hearth & Hollow/Dungeon Settings", fileName = "DungeonSettings")]
    public class DungeonSettings : ScriptableObject
    {
        [Header("Themes (one per route)")]
        public List<DungeonTheme> themes = new List<DungeonTheme>();
        [Tooltip("Used for any route without its own theme.")]
        public DungeonTheme fallbackTheme;

        [Header("Shared room pool (used by every theme)")]
        public List<RoomTemplate> rooms = new List<RoomTemplate>();

        [Header("Marker tiles painted in room prefabs")]
        public TileBase markerFloor;
        public TileBase markerWall;
        public TileBase markerWallFace;
        public TileBase markerHazard;

        [Header("Monsters")]
        public List<EnemyProfile> enemies = new List<EnemyProfile>();

        [Header("Feel")]
        [Tooltip("Player walk speed (tiles per second).")] public float playerSpeed = 5.2f;
        public float dashSpeed = 15f;
        public float dashSeconds = 0.16f;
        [Tooltip("Character art: source pixels per world unit (tiles are 32 px).")] public float characterPixelsPerUnit = 32f;
        [Tooltip("Camera half-height in tiles.")] public float cameraSize = 8.4f;

        public DungeonTheme ThemeFor(string routeId)
        {
            foreach (var t in themes) if (t != null && t.routeId == routeId) return t;
            return fallbackTheme != null ? fallbackTheme : (themes.Count > 0 ? themes[0] : null);
        }

        public EnemyProfile ProfileFor(string monsterId)
        {
            foreach (var e in enemies) if (e != null && e.monsterId == monsterId) return e;
            return new EnemyProfile { monsterId = monsterId };
        }
    }
}
