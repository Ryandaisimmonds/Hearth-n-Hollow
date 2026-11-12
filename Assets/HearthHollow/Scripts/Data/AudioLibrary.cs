// Hearth & Hollow — ticket 62: every sound and music track by id (built from the Ninja Adventure pack by
// "Hearth & Hollow/Build Audio Library"; lives in Resources so builds include it). Swap clips in the Inspector freely.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HearthHollow.Data
{
    [CreateAssetMenu(menuName = "Hearth & Hollow/Audio Library", fileName = "AudioLibrary")]
    public class AudioLibrary : ScriptableObject
    {
        [Serializable] public class Sound
        {
            public string id;
            [Tooltip("One is picked at random each time.")] public List<AudioClip> clips = new List<AudioClip>();
            [Range(0f, 1f)] public float volume = 0.8f;
            [Tooltip("Random pitch ± this (0.05 = ±5%) so repeats don't sound identical.")] public float pitchJitter = 0.05f;
        }

        public List<Sound> sounds = new List<Sound>();
        [Tooltip("Music and ambience loops by id (town, meadow, chili, frosthollow, fall, boss, wind, rain…).")]
        public List<Sound> music = new List<Sound>();

        public Sound Get(string id) { return sounds.Find(s => s != null && s.id == id); }
        public Sound Track(string id) { return music.Find(s => s != null && s.id == id); }
    }
}
