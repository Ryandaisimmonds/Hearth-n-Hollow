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

        /// Tests only: when set, saves go to this folder instead of persistentDataPath (so the real save is never touched).
        public static string DirectoryOverride;

        public static string SavePath
        {
            get { return Path.Combine(string.IsNullOrEmpty(DirectoryOverride) ? Application.persistentDataPath : DirectoryOverride, FileName); }
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
                string tmp = SavePath + ".tmp", bak = SavePath + ".bak";
                File.WriteAllText(tmp, json);
                if (!File.Exists(SavePath)) { File.Move(tmp, SavePath); return true; }
                try { File.Replace(tmp, SavePath, bak, true); }   // one step: the new save goes in, the old one becomes the .bak
                catch (Exception)
                {
                    // no atomic replace on this file system: keep a copy first, so a crash mid-way can't lose everything
                    File.Copy(SavePath, bak, true);
                    File.Delete(SavePath);
                    File.Move(tmp, SavePath);
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Hearth & Hollow] Save failed: " + e.Message);
                return false;
            }
        }

        /// Returns null if there is no save or it can't be read (falls back to the previous save, the .bak, first).
        public static Profile Load()
        {
            Profile p = LoadFrom(SavePath);
            if (p != null) return p;
            p = LoadFrom(SavePath + ".bak");
            if (p != null) Debug.LogWarning("[Hearth & Hollow] The save couldn't be read, so the previous save (.bak) was loaded.");
            return p;
        }

        static Profile LoadFrom(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return null;
                Profile p = JsonUtility.FromJson<Profile>(json);
                if (p == null || p.classes == null) return null;
                return p;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Hearth & Hollow] " + Path.GetFileName(path) + " could not be loaded (" + e.Message + ").");
                return null;
            }
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
                string tmp = SavePath + ".tmp", bak = SavePath + ".bak";
                if (File.Exists(tmp)) File.Delete(tmp);
                if (File.Exists(bak)) File.Delete(bak);   // a full reset wipes the backup too
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Hearth & Hollow] Delete save failed: " + e.Message);
            }
        }
    }
}
