// Ticket 62: the music follows where you are; every sound id the game plays is in the library.
using HearthHollow.Core;
using HearthHollow.Data;
using HearthHollow.Runtime;
using NUnit.Framework;
using UnityEditor;

namespace HearthHollow.Tests
{
    public class GameAudioTests
    {
        [Test]
        public void TheLibrary_HasEverySoundTheGamePlays()
        {
            var lib = AssetDatabase.LoadAssetAtPath<AudioLibrary>("Assets/HearthHollow/Resources/HHAudio/AudioLibrary.asset");
            Assert.IsNotNull(lib, "run Hearth & Hollow > Build Audio Library");
            foreach (var id in new[] { "swing", "hit", "crit", "kill", "boss_kill", "hurt", "pickup", "coin", "dig", "chest", "click", "sizzle", "cook_prime", "cook_fine", "cook_plain", "feast", "boss", "room_clear", "fire", "frost", "nature", "water", "level_up", "run_cleared", "died" })
            {
                var s = lib.Get(id);
                Assert.IsNotNull(s, id); Assert.Greater(s.clips.Count, 0, id);
            }
            foreach (var id in new[] { "town", "meadow", "chili", "frosthollow", "fall", "boss", "dungeon", "wind", "rain" })
            {
                var t = lib.Track(id);
                Assert.IsNotNull(t, id); Assert.Greater(t.clips.Count, 0, id);
            }
        }

        [Test]
        public void TheMusic_FollowsWhereYouAre()
        {
            Assert.AreEqual("town", GameAudio.TrackFor(null, false));
            var s = new GameSession(DefaultContent.Create(), null, 1);
            Assert.AreEqual("town", GameAudio.TrackFor(s, false));
        }
    }
}
