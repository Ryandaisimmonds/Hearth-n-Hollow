// Hearth & Hollow — the play-mode smoke test: the real scene, start to first room, with a throwaway save.
// A brand-new game opens in the fall; the fog drops you, you wake in town and Wren has her say; you walk up to every
// station and open it, cook a meal, show Wren, pack the tray, set out for Meadow Hollow and clear the first fight room.
// Any error in the console fails it (the test framework does that), and so does any step that doesn't finish in time.
// Your real save is never touched: saves go to a temp folder, and the test checks the real file didn't change.
// It also leaves screenshots of the key screens in Temp/ClaudeShots/smoke_*.png, for before/after checks of UI work.
using System;
using System.Collections;
using System.IO;
using HearthHollow.Core;
using HearthHollow.Dungeon;
using HearthHollow.Runtime;
using HearthHollow.Town;
using HearthHollow.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HearthHollow.PlayTests
{
    public class SmokeTest
    {
        const string ScenePath = "Assets/HearthHollow/Scenes/HearthHollow.unity";
        string _dir, _realSave;
        DateTime _realStamp;
        GameBootstrap _boot;

        GameSession S { get { return _boot.Session; } }
        GameUI UI { get { return _boot.UI; } }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Assume.That(PlayerPrefs.GetInt(GameBootstrap.SlicePref, 1) == 1, "the vertical slice is switched off in the dev panel");
            _realSave = Path.Combine(Application.persistentDataPath, SaveService.FileName);
            _realStamp = File.Exists(_realSave) ? File.GetLastWriteTimeUtc(_realSave) : DateTime.MinValue;
            _dir = Path.Combine(Path.GetTempPath(), "hh-smoke-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            SaveService.DirectoryOverride = _dir;   // before the scene loads: GameBootstrap reads the save in Start
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync(ScenePath);
#endif
            yield return Until("the game starts", () => (_boot = UnityEngine.Object.FindObjectOfType<GameBootstrap>()) != null && _boot.Session != null && _boot.UI != null, 10f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // the game goes first, while saves still go to the temp folder (leaving play mode would save it otherwise)
            foreach (var b in UnityEngine.Object.FindObjectsOfType<GameBootstrap>()) UnityEngine.Object.Destroy(b.gameObject);
            yield return null;
            SaveService.DirectoryOverride = null;
            try { Directory.Delete(_dir, true); } catch (Exception) { }
            DateTime now = File.Exists(_realSave) ? File.GetLastWriteTimeUtc(_realSave) : DateTime.MinValue;
            Assert.AreEqual(_realStamp, now, "the real save was touched");
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator NewGame_ToTheFirstClearedRoom()
        {
            // ── the fall ──
            yield return Until("a new game opens in the fall", () => S.InFall && S.InDungeon && DungeonController.Instance != null, 10f);
            yield return Seconds(1.5f);   // let the first room come up
            while (S.InDungeon) { S.Story.FallFogBite(); yield return Seconds(0.1f); }
            yield return Until("the fall fades to town", () => S.Phase == Phase.Hearth && TownController.Instance != null, 15f);
            Assert.IsTrue(S.Story.TalkDue || S.Wren == WrenStep.CookMeal, "Wren has something to say after the fall");
            yield return Until("Wren comes over", () => S.Wren == WrenStep.CookMeal, 10f);
            UI.CloseDialogue();
            Step("fell, woke in town, Wren had her say");
            yield return Seconds(2.5f);   // the wake-up fade clears
            yield return Shot("1_town");

            // ── walk the town and open every station ──
            var town = TownController.Instance;
            string opened = "";
            foreach (var spot in town.Spots.ToArray())
            {
                if (spot.action == TownAction.Gate || spot.action == TownAction.Wren || spot.action == TownAction.BackToCamp) continue;
                town.Player.Teleport(spot.pos + Vector2.down * 0.8f);
                yield return Until("walk up to " + spot.name, () => town.Near != null, 3f);
                var used = town.Near;
                town.Use(used);
                opened += used.name + ", ";
                yield return Seconds(0.3f);
                if (used.action == TownAction.Cooking) yield return Shot("2_kitchen");
                if (used.action == TownAction.Larder) yield return Shot("3_larder");
                if (used.action == TownAction.SpiceRack) yield return Shot("4_spice");
                CloseEverything();
                yield return null;
            }

            Step("used " + opened.TrimEnd(' ', ','));

            // ── cook, show Wren, pack ──
            var recipe = S.Content.recipes.Find(r => !r.always && r.ingredients.Count > 0 && Kitchen.Unlocked(S.Content, S.Profile, r) && CanCook(r));
            Assert.IsNotNull(recipe, "something to cook after the fall");
            Assert.IsNotNull(S.Cook(recipe.id, new CookChoice { grade = 1 }), "cook " + recipe.name);
            Assert.AreEqual(WrenStep.ShowMeal, S.Wren);
            S.Story.Talk();
            Assert.AreEqual(WrenStep.FirstBoss, S.Wren);
            var bread = S.Content.recipes.Find(r => r.always);
            for (int i = 0; i < 6 && S.TrayBlock != null; i++)
                if (!(S.Profile.mealPantry.Count > 0 && S.PackMeal(0)) && (bread == null || !S.PackAlwaysMeal(bread.id))) break;
            Assert.IsNull(S.TrayBlock, "the tray is packed");
            Step("cooked " + recipe.name + ", showed Wren, packed the tray");

            // ── set out from the gate ──
            var gate = town.Spots.Find(x => x.action == TownAction.Gate);
            town.Player.Teleport(gate.pos + Vector2.down * 0.8f);
            yield return Until("walk up to the gate", () => town.Near == gate, 3f);
            town.Use(gate);
            yield return Seconds(0.3f);
            yield return Shot("5_routes");
            Assert.IsTrue(S.StartRun(Ids.Area.Meadow), "set out for Meadow Hollow");
            CloseEverything();

            // ── clear the first fight room ──
            var dc = DungeonController.Instance;
            yield return Until("the dungeon is up", () => dc.CurrentRoom != null, 10f);
            yield return Seconds(1f);
            RoomInstance fight = null;
            for (int hop = 0; hop < 8 && fight == null; hop++)
            {
                var here = dc.CurrentRoom;
                if (here.HasEvent && !here.Cleared) { fight = here; break; }
                foreach (Side side in Enum.GetValues(typeof(Side)))
                    if (here.Node.Has(side)) { dc.GoThrough(here, side); break; }
                yield return Seconds(0.6f);
            }
            Assert.IsNotNull(fight, "found a fight room");
            yield return Seconds(0.8f);
            yield return Shot("6_fight");
            yield return Until("the monsters arrive", () => AnyReady(fight), 10f);
            float until = Time.unscaledTime + 30f;
            while (!fight.Cleared)
            {
                Assert.Less(Time.unscaledTime, until, "the room didn't clear in time");
                foreach (var e in new System.Collections.Generic.List<EnemyController>(fight.Enemies))
                    if (e != null && !e.Dead && !e.Spawning) e.TakeHit(9999f, false, Vector2.zero);
                yield return Seconds(0.2f);
            }
            Assert.Greater(S.Run.hp, 0, "still standing");
            yield return Seconds(0.6f);
            yield return Shot("7_cleared");

            // ── effects come from pools: after one warm-up, a boss dying in a full room makes no new objects ──
            Vector3 at = dc.Player.Body;
            yield return BossDeaths(at);
            yield return Seconds(5.5f);   // everything fades and goes back (floor embers last up to 5 s)
            int made = DungeonFx.Created;
            yield return BossDeaths(at);
            yield return Seconds(0.15f);
            yield return Shot("8_bursts");
            Assert.AreEqual(made, DungeonFx.Created, "effects made new objects after warm-up");
            yield return Seconds(3f);
            Step("a second round of boss deaths made no new effect objects (" + made + " pooled)");
            Step("set out for Meadow Hollow and cleared a " + fight.Node.kind + " room");
        }

        /// A boss dies in every element at once, with hits, rings and sparks all round: the worst case for effects.
        static IEnumerator BossDeaths(Vector3 at)
        {
            foreach (Element e in Enum.GetValues(typeof(Element)))
            {
                DungeonFx.DeathBurst(at, e, new Color(0.6f, 0.4f, 0.3f), true, false);
                DungeonFx.Sparks(at, Vector2.right, Color.white, 14);
                DungeonFx.RingAt(at, Color.white, 1.6f, 0.35f);
                DungeonFx.SlashAt(at, Vector2.left, Color.white, 1f);
                DungeonFx.FloorEmbers(at, new Color(1f, 0.5f, 0.2f));
                yield return null;
            }
        }

        static bool AnyReady(RoomInstance room)
        {
            foreach (var e in room.Enemies) if (e != null && !e.Dead && !e.Spawning) return true;
            return room.Cleared;
        }

        bool CanCook(RecipeData r) { string why; return Cooking.CanCook(S.Content, S.Profile, r, new CookChoice(), out why); }

        void CloseEverything()
        {
            UI.CloseDialogue(); UI.CloseCook(); UI.CloseSeason(); UI.ClosePantry(); UI.CloseFishing();
            UI.CloseCampEvent(); UI.CloseRoutes(); UI.CloseTownMenu(); UI.CloseSettings();
        }

        static IEnumerator Shot(string name)
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "ClaudeShots"));
            Directory.CreateDirectory(dir);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, "smoke_" + name + ".png"), 1);
            yield return null; yield return null;
        }

        static void Step(string what) { Debug.Log("[Smoke] " + what); }

        static IEnumerator Seconds(float s) { float t = Time.unscaledTime + s; while (Time.unscaledTime < t) yield return null; }

        static IEnumerator Until(string what, Func<bool> done, float timeout)
        {
            float t = Time.unscaledTime + timeout;
            while (!done())
            {
                if (Time.unscaledTime > t) Assert.Fail("Timed out: " + what);
                yield return null;
            }
        }
    }
}
