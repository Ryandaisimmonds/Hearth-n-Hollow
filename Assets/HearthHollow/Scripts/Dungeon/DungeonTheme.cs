// Hearth & Hollow — the look and feel of one route's dungeon. One asset per route.
// Rooms are painted with marker tiles; this theme decides what those markers become.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace HearthHollow.Dungeon
{
    [CreateAssetMenu(menuName = "Hearth & Hollow/Dungeon Theme", fileName = "Theme")]
    public class DungeonTheme : ScriptableObject
    {
        [Tooltip("Route id this theme is for (meadow, berry, salt, cavern, chili, ember).")]
        public string routeId;
        public string displayName;

        [Header("Tiles — replace the marker tiles painted in the room prefabs")]
        public TileBase floor;
        [Tooltip("Optional variation sprinkled over the floor.")]
        public TileBase floorAlt;
        [Range(0f, 1f)] public float floorAltChance = 0.18f;
        public TileBase wall;
        public TileBase wallFace;
        public TileBase hazard;
        [Tooltip("Small visual bits scattered on the floor (no collision).")]
        public List<TileBase> decor = new List<TileBase>();
        [Range(0f, 0.3f)] public float decorDensity = 0.05f;

        [Header("Colour & light")]
        public Color background = new Color(0.07f, 0.09f, 0.08f);
        [Tooltip("Tint on every floor / wall tile (white = as painted).")]
        public Color floorTint = Color.white;
        public Color wallTint = Color.white;
        [Tooltip("Global light colour over the whole floor.")]
        public Color lightColor = Color.white;
        [Range(0f, 2f)] public float lightIntensity = 1f;
        [Tooltip("Small light that follows the player (0 = off). Nice for caves.")]
        [Range(0f, 3f)] public float playerLight = 0f;

        [Header("Camera look (HD-2D colour grade — see GameLook)")]
        [Range(-100f, 100f)] public float gradeTemperature;
        [Range(-100f, 100f)] public float gradeTint;
        [Range(-100f, 100f)] public float gradeSaturation;
        [Range(-100f, 100f)] public float gradeContrast;
        [Range(-2f, 2f)] public float gradeExposure;
        public Color gradeFilter = Color.white;
        [Range(0f, 0.6f)] public float gradeVignette = 0.28f;
        [Range(0f, 3f)] public float gradeBloom = 0.8f;

        [Header("Room light (light plan step 4)")]
        [Tooltip("The route's own light sources scattered round each room.")] public RoomGlow ambient = RoomGlow.None;
        public Color ambientColor = new Color(1f, 0.9f, 0.5f);

        [Header("Props")]
        public Sprite doorOpen;
        public Sprite doorClosed;
        public Sprite ladder;
        public Sprite exitPortal;
        public Sprite campfire;
        public Sprite forage;

        [Header("Rooms for this dungeon (empty = the shared pool in Dungeon Settings)")]
        public List<RoomTemplate> rooms = new List<RoomTemplate>();
        [Tooltip("Also use the shared pool when this list has rooms.")]
        public bool alsoUseSharedRooms = true;

        [Header("Feel")]
        [Range(0.5f, 2f)] public float enemySpeedMult = 1f;
    }
}
