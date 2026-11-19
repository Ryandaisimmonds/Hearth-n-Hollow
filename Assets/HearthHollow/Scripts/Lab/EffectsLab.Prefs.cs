// Hearth & Hollow — the Effects Lab's settings, kept between sessions (PlayerPrefs "hh_lab", JSON). Nothing here
// touches the save: the lab's profile is thrown away every time. Setup tab → "Reset lab settings" clears it.
using System;
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;

namespace HearthHollow.Lab
{
    [Serializable] public class LabLoadout { public string cls; public List<string> ids = new List<string>(); }   // by LoadSlot

    [Serializable] public class LabPrefs
    {
        // ── setup ──
        public bool arena = true;
        public string area = Ids.Area.Meadow;
        public int floor = 1;
        public string cls = "";
        public List<LabLoadout> loadouts = new List<LabLoadout>();
        public int boots = -1, lantern = -1, bag = -1;
        public bool trinketsSet; public List<string> trinkets = new List<string>();
        public bool mealsSet; public List<CookedMeal> meals = new List<CookedMeal>();
        public List<MoveLevel> levels = new List<MoveLevel>();       // each skill's level in the run
        public bool bossSwirl = true;
        // ── play ──
        public List<string> favourites = new List<string>();
        public string filter = "lab";
        public List<MoveLevel> lastLevels = new List<MoveLevel>();   // the level each move was last used at (0 = its own)
        public float speed = 1f, zoom = 1f;
        public bool autoOn; public string autoMove = ""; public float autoInterval = 1.5f;
        public bool aimNearest = true;
        public string packMonster = ""; public int packCount; public bool freeze; public int dummies = 1;
        public int light;                                            // 0 the area's own, 1 bright, 2 dark
        public bool cantDie = true, autoHeal = true, noCooldowns;

        const string Key = "hh_lab";

        public static LabPrefs Load()
        {
            var json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) return new LabPrefs();
            try { return JsonUtility.FromJson<LabPrefs>(json) ?? new LabPrefs(); }
            catch { return new LabPrefs(); }
        }

        public void Save() { PlayerPrefs.SetString(Key, JsonUtility.ToJson(this)); PlayerPrefs.Save(); }
        public static void Clear() { PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); }

        public int Level(List<MoveLevel> list, string id) { var x = list.Find(l => l.moveId == id); return x != null ? x.level : 0; }
        public void SetLevel(List<MoveLevel> list, string id, int level)
        {
            var x = list.Find(l => l.moveId == id);
            if (x == null) list.Add(x = new MoveLevel { moveId = id });
            x.level = level;
        }
    }
}
