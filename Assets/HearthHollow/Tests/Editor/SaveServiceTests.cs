// Hearth & Hollow — edit-mode tests: saving and loading. Every test saves into a temporary folder,
// never the real save in persistentDataPath.
using System.IO;
using HearthHollow.Core;
using HearthHollow.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace HearthHollow.Tests
{
    public class SaveServiceTests
    {
        string _dir;
        GameContent _content;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "HearthHollowTests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            SaveService.DirectoryOverride = _dir;
            _content = DefaultContent.Create();
        }

        [TearDown]
        public void TearDown()
        {
            SaveService.DirectoryOverride = null;
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); } catch (IOException) { }
        }

        Profile NewProfile(int coins)
        {
            var p = Economy.NewProfile(_content);
            p.coins = coins;
            return p;
        }

        [Test]
        public void SavePath_UsesTestFolder_NotTheRealSave()
        {
            StringAssert.StartsWith(_dir, SaveService.SavePath);
            Assert.AreNotEqual(Path.Combine(Application.persistentDataPath, SaveService.FileName), SaveService.SavePath);
        }

        [Test]
        public void SaveThenLoad_RoundTripsTheProfile()
        {
            var p = NewProfile(123);
            p.day = 7;
            Assert.IsTrue(SaveService.Save(p));
            var loaded = SaveService.Load();
            Assert.IsNotNull(loaded);
            Assert.AreEqual(123, loaded.coins);
            Assert.AreEqual(7, loaded.day);
            Assert.AreEqual(p.classes.Count, loaded.classes.Count);
        }

        [Test]
        public void SecondSave_KeepsThePreviousSaveAsBak()
        {
            SaveService.Save(NewProfile(10));
            SaveService.Save(NewProfile(20));
            string bak = SaveService.SavePath + ".bak";
            Assert.IsTrue(File.Exists(bak), "no .bak after the second save");
            var old = JsonUtility.FromJson<Profile>(File.ReadAllText(bak));
            Assert.AreEqual(10, old.coins);
            Assert.AreEqual(20, SaveService.Load().coins);
            Assert.IsFalse(File.Exists(SaveService.SavePath + ".tmp"), "a .tmp file was left behind");
        }

        [Test]
        public void CorruptSave_FallsBackToBak()
        {
            SaveService.Save(NewProfile(10));
            SaveService.Save(NewProfile(20));
            File.WriteAllText(SaveService.SavePath, "{ this is not json");
            var p = SaveService.Load();
            Assert.IsNotNull(p, "a corrupt save with a good .bak should still load");
            Assert.AreEqual(10, p.coins);
        }

        [Test]
        public void EmptySave_FallsBackToBak()
        {
            SaveService.Save(NewProfile(10));
            SaveService.Save(NewProfile(20));
            File.WriteAllText(SaveService.SavePath, "");
            Assert.AreEqual(10, SaveService.Load().coins);
        }

        [Test]
        public void NoSave_LoadsNull()
        {
            Assert.IsFalse(SaveService.HasSave());
            Assert.IsNull(SaveService.Load());
        }

        [Test]
        public void Delete_RemovesSaveAndBackup()
        {
            SaveService.Save(NewProfile(10));
            SaveService.Save(NewProfile(20));
            SaveService.Delete();
            Assert.IsFalse(File.Exists(SaveService.SavePath));
            Assert.IsFalse(File.Exists(SaveService.SavePath + ".bak"));
            Assert.IsNull(SaveService.Load());
        }

        [Test]
        public void SavedProfile_StillWorksInANewSession()
        {
            var s1 = new GameSession(_content, NewProfile(55), 1);
            SaveService.Save(s1.Profile);
            var s2 = new GameSession(DefaultContent.Create(), SaveService.Load(), 1);
            Assert.AreEqual(55, s2.Profile.coins);
            Assert.AreEqual(Phase.Hearth, s2.Phase);
        }
    }
}
