// Hearth & Hollow — ticket 62: "Hearth & Hollow/Build Audio Library" maps the game's sound ids to the Ninja Adventure
// pack's clips and writes Assets/HearthHollow/Resources/HHAudio/AudioLibrary.asset. Re-run any time.
#if UNITY_EDITOR
using System.Collections.Generic;
using HearthHollow.Data;
using UnityEditor;
using UnityEngine;

namespace HearthHollow.EditorTools
{
    public static class AudioLibraryBuilder
    {
        const string Path = "Assets/HearthHollow/Resources/HHAudio/AudioLibrary.asset";
        const string NA = "Assets/ThirdParty/Ninja Adventure - Asset Pack/Audio/";
        const string S = NA + "Sounds/", M = NA + "Musics/", J = NA + "Jingles/";

        // id, volume, clips…
        static readonly object[][] Sounds =
        {
            new object[] { "swing", 0.45f, S + "Whoosh & Slash/Whoosh.wav", S + "Whoosh & Slash/Whoosh2.wav", S + "Whoosh & Slash/Slash.wav" },
            new object[] { "hit", 0.6f, S + "Hit & Impact/Hit1.wav", S + "Hit & Impact/Hit2.wav", S + "Hit & Impact/Hit3.wav", S + "Hit & Impact/Hit5.wav" },
            new object[] { "crit", 0.7f, S + "Hit & Impact/Impact.wav", S + "Hit & Impact/Impact2.wav" },
            new object[] { "kill", 0.6f, S + "Hit & Impact/Impact3.wav", S + "Hit & Impact/Impact4.wav" },
            new object[] { "boss_kill", 0.85f, S + "Elemental/Explosion2.wav" },
            new object[] { "hurt", 0.7f, S + "Hit & Impact/Hit6.wav", S + "Hit & Impact/Hit7.wav" },
            new object[] { "pickup", 0.5f, S + "Bonus/Bonus.wav", S + "Bonus/Bonus2.wav" },
            new object[] { "coin", 0.45f, S + "Bonus/Coin.wav", S + "Bonus/Coin2.wav", S + "Bonus/Coin3.wav" },
            new object[] { "dig", 0.6f, S + "Jump & Bounce/Bounce3.wav", S + "Hit & Impact/Hit8.wav" },
            new object[] { "chest", 0.6f, S + "Bonus/Gold1.wav" },
            new object[] { "click", 0.35f, S + "Menu/Accept.wav", S + "Menu/Accept2.wav" },
            new object[] { "back", 0.35f, S + "Menu/Cancel.wav" },
            new object[] { "sizzle", 0.4f, S + "Elemental/Fire.wav", S + "Elemental/Fire2.wav" },
            new object[] { "cook_fine", 0.6f, S + "Bonus/PowerUp1.wav" },
            new object[] { "cook_prime", 0.75f, J + "Success1.wav" },
            new object[] { "cook_plain", 0.5f, S + "Bonus/Bonus3.wav" },
            new object[] { "feast", 0.6f, S + "Magic & Skill/Heal.wav" },
            new object[] { "boss", 0.8f, S + "Alert/Alert2.wav" },
            new object[] { "room_clear", 0.55f, S + "Magic & Skill/Magic2.wav" },
            new object[] { "fire", 0.45f, S + "Elemental/Fireball.wav" },
            new object[] { "frost", 0.45f, S + "Magic & Skill/Magic4.wav" },
            new object[] { "nature", 0.45f, S + "Elemental/Grass.wav", S + "Elemental/Grass2.wav" },
            new object[] { "water", 0.45f, S + "Elemental/Water1.wav", S + "Elemental/Water2.wav" },
            new object[] { "level_up", 0.7f, J + "LevelUp1.wav" },
            new object[] { "run_cleared", 0.7f, J + "Success2.wav" },
            new object[] { "died", 0.7f, J + "GameOver.wav" },
        };
        static readonly object[][] Music =
        {
            new object[] { "town", 0.45f, M + "33 - Calm Village.ogg" },
            new object[] { "meadow", 0.4f, M + "11 - Clearing.ogg" },
            new object[] { "chili", 0.4f, M + "23 - Road.ogg" },
            new object[] { "frosthollow", 0.4f, M + "13 - Mystical.ogg" },
            new object[] { "fall", 0.4f, M + "14 - Curse.ogg" },
            new object[] { "dungeon", 0.4f, M + "21 - Dungeon.ogg" },
            new object[] { "boss", 0.45f, M + "17 - Fight.ogg" },
            new object[] { "wind", 0.35f, S + "Ambient/Wind.wav" },
            new object[] { "rain", 0.35f, S + "Ambient/Rain.wav" },
        };

        [MenuItem("Hearth & Hollow/Build Audio Library")]
        public static string Build()
        {
            System.IO.Directory.CreateDirectory("Assets/HearthHollow/Resources/HHAudio");
            var lib = AssetDatabase.LoadAssetAtPath<AudioLibrary>(Path);
            bool made = lib == null;
            if (made) lib = ScriptableObject.CreateInstance<AudioLibrary>();
            int missing = 0;
            lib.sounds = Fill(Sounds, ref missing);
            lib.music = Fill(Music, ref missing);
            foreach (var m in lib.music) m.pitchJitter = 0f;
            if (made) AssetDatabase.CreateAsset(lib, Path);
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            return "Audio library: " + lib.sounds.Count + " sounds, " + lib.music.Count + " loops, " + missing + " clips missing";
        }

        static List<AudioLibrary.Sound> Fill(object[][] rows, ref int missing)
        {
            var list = new List<AudioLibrary.Sound>();
            foreach (var row in rows)
            {
                var s = new AudioLibrary.Sound { id = (string)row[0], volume = (float)row[1] };
                for (int i = 2; i < row.Length; i++)
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>((string)row[i]);
                    if (clip != null) s.clips.Add(clip); else { missing++; Debug.LogWarning("[Audio] missing " + row[i]); }
                }
                list.Add(s);
            }
            return list;
        }
    }
}
#endif
