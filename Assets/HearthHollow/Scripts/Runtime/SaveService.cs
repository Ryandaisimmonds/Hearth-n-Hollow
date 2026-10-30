// Hearth & Hollow — profile persistence via JsonUtility.
using System;
using System.IO;
using HearthHollow.Core;
using UnityEngine;

namespace HearthHollow.Runtime
{
    public static class SaveService
    {
        public const string FileName = "hearth_hollow_save.json";

        public static string SavePath
        {
            get { return Path.Combine(Application.persistentDataPath, FileName); }
        }

        public static bool HasSave()
        {
            return File.Exists(SavePath);
        }

        public static bool Save(Profile profile)
        {
            if (profile == null) return false;
            try
            {
                string json = JsonUtility.ToJson(profile, true);
                string tmp = SavePath + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(SavePath)) File.Delete(SavePath);
                File.Move(tmp, SavePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Hearth & Hollow] Save failed: " + e.Message);
                return false;
            }
        }

        /// Returns null if there is no save or it can't be read.
        public static Profile Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return null;
                string json = File.ReadAllText(SavePath);
                if (string.IsNullOrWhiteSpace(json)) return null;
                Profile p = JsonUtility.FromJson<Profile>(json);
                if (p == null || p.classes == null) return null;
                return p;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Hearth & Hollow] Save could not be loaded (" + e.Message + "); starting fresh.");
                return null;
            }
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
                string tmp = SavePath + ".tmp";
                if (File.Exists(tmp)) File.Delete(tmp);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Hearth & Hollow] Delete save failed: " + e.Message);
            }
        }
    }
}
