// Hearth & Hollow — the play-mode smoke tests: the real scene, start to first room, with a throwaway save.
//   Quick (~30 s, run after any change): a brand-new game opens in the fall; the fog drops you, you wake in town and
//     Wren has her say; you open the cooking area, cook a meal, show Wren, pack the tray, set out for Meadow Hollow
//     and clear the first fight room.
//   Full (~2.5 min, run before a commit; category "Full"): the same, plus every town overlay and station, the old
//     camp screen, effect pooling, a boss's entrance, one monster of every kind, and every move in the game.
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

        [UnityTest, Timeout(120000)]
        public IEnumerator Quick_FallTownCookAndFirstRoom() { return Play(false); }

        [UnityTest, Timeout(300000), Category("Full")]
        public IEnumerator Full_EveryScreenMoveAndMonster() { return Play(true); }

        IEnumerator Play(bool full)
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

            // ── every overlay opens and closes; pop-ups stack; Esc closes the top one ──
            if (full)
            {
                yield return Overlays();
                Step("every town overlay opened and closed, stacked pop-ups unwound, Esc closed the top");
            }

            // ── walk the town and open every station ──
            var town = TownController.Instance;
            string opened = "";
            foreach (var spot in town.Spots.ToArray())
            {
                if (spot.action == TownAction.Gate || spot.action == TownAction.Wren || spot.action == TownAction.BackToCamp) continue;
                if (!full && spot.action != TownAction.Cooking) continue;   // quick: just the cooking area
                town.Player.Teleport(spot.pos + Vector2.down * 0.8f);
                yield return Until("walk up to " + spot.name, () => town.Near != null, 3f);
                var used = town.Near;
                town.Use(used);
                opened += used.name + ", ";
                yield return Seconds(0.3f);
                if (used.action == TownAction.Cooking) yield return Shot("2_kitchen");
                if (used.action == TownAction.Larder) yield return Shot("3_larder");
                if (used.action == TownAction.SpiceRack) yield return Shot("4_spice");
                if (used.action == TownAction.Upgrades) yield return Shot("4b_smithy");
                if (used.action == TownAction.Workshop) yield return Shot("4c_workshop");
                if (used.action == TownAction.Training) yield return Shot("4d_training");
                CloseEverything();
                yield return null;
            }

            Step("used " + opened.TrimEnd(' ', ','));

            // the old camp screen (tabs and the who's-going column) still draws
            if (full)
            {
                UI.CloseTown(); UI.ShowCampTab(CampTab.Cooking);
                yield return Seconds(0.3f);
                yield return Shot("4e_camp");
                UI.ShowCampTab(CampTab.Upgrades);
                yield return Seconds(0.3f);
                yield return Shot("4f_camp_gear");
                UI.OpenTown();
                yield return Seconds(0.3f);
            }

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

            // ── the ladder choice opens and closes in the dungeon ──
            yield return Until("the dungeon is up for the ladder", () => DungeonController.Instance != null && DungeonController.Instance.CurrentRoom != null, 10f);
            UI.OpenLadderChoice(() => { }, () => { });
            yield return null;
            Assert.IsTrue(UI.LadderChoiceOpen, "ladder choice opens");
            UI.CloseLadderChoice();
            yield return null;
            Assert.IsFalse(UI.LadderChoiceOpen, "ladder choice closes");

            // ── clear the first fight room ──
            var dc = DungeonController.Instance;
            yield return Until("the dungeon is up", () => dc.CurrentRoom != null, 10f);
            yield return Seconds(1f);
            RoomInstance fight = null;
            var rng = new System.Random(7);
            for (int hop = 0; hop < 30 && fight == null; hop++)   // wander through doors until a room has a fight
            {
                var here = dc.CurrentRoom;
                if (here.HasEvent && !here.Cleared) { fight = here; break; }
                var sides = new System.Collections.Generic.List<Side>();
                foreach (Side side in Enum.GetValues(typeof(Side))) if (here.Node.Has(side)) sides.Add(side);
                if (sides.Count > 0) dc.GoThrough(here, sides[rng.Next(sides.Count)]);
                yield return Seconds(0.5f);
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

            Step("set out for Meadow Hollow and cleared a " + fight.Node.kind + " room");
            if (!full) yield break;

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

            // ── pooled lights come back clean: no leftover scale, floor-ember lights keep coming, shadow safety re-arms ──
            yield return PooledLights(dc);

            // ── a boss makes its entrance: title card, the camera pushes in and comes back ──
            var room = dc.CurrentRoom;
            string bossId = DungeonRules.BossMonster(S.Content, new RunState { routeId = Ids.Area.Meadow, floor = DungeonRules.Floors(S.Content, Ids.Area.Meadow), dungeon = true });
            var cam = Camera.main;
            float size0 = cam.orthographicSize;
            var boss = dc.SpawnEnemy(room, bossId, false, true, (Vector2)dc.Player.Body + Vector2.right * 3f);
            Assert.IsNotNull(boss, "the boss spawns");
            dc.BossEntrance(room, boss);
            yield return Seconds(0.9f);
            Assert.GreaterOrEqual(dc.Hud.IntroT, 0f, "the title card is up");
            Assert.Less(cam.orthographicSize, size0 - 0.01f, "the camera pushes in");
            yield return Shot("9_boss_intro");
            yield return Until("the entrance ends", () => dc.Hud.IntroT < 0f, 6f);
            yield return Seconds(0.3f);
            Assert.AreEqual(size0, cam.orthographicSize, 0.01f, "the camera comes back");
            boss.TakeHit(99999f, false, Vector2.zero);
            yield return Seconds(1f);
            Step("a boss made its entrance: title card, push-in and back");

            // ── every kind of monster moves and lands a hit (the boss, as a boss, too) ──
            var kinds = new System.Collections.Generic.List<string>();
            foreach (EnemyArchetype a in Enum.GetValues(typeof(EnemyArchetype)))
            {
                var md = S.Content.monsters.Find(m => m.archetype == a && m.lightReaction != LightReaction.Shy)
                      ?? S.Content.monsters.Find(m => m.archetype == a);   // light-shy ones hold back in a lit room
                if (md == null) { kinds.Add(a + " (none in the content)"); continue; }
                yield return Fights(dc, md.id, false, a.ToString());
                kinds.Add(a + " (" + md.name + ")");
            }
            yield return Fights(dc, bossId, true, "boss");
            Step("monsters fought: " + string.Join(", ", kinds.ToArray()) + ", and the boss");

            // ── every move in the game plays out (all classes' attacks, skills and ultimates, with a target to hit) ──
            yield return EveryMove(dc);
        }

        IEnumerator Overlays()
        {
            Assert.IsTrue(UI.TownWalking, "walking in town to start with");
            var opens = new (string name, Action open, Action close)[]
            {
                ("larder", UI.OpenPantry, UI.ClosePantry),
                ("stove", () => UI.OpenCook(S.Content.recipes.Find(r => !r.always).id), UI.CloseCook),
                ("spice rack", UI.OpenSeason, UI.CloseSeason),
                ("fishing", UI.OpenFishing, UI.CloseFishing),
                ("dialogue", () => UI.OpenDialogue("Test", "🙂", new System.Collections.Generic.List<string> { "Hello." }), UI.CloseDialogue),
                ("notice board", UI.OpenCampEvent, UI.CloseCampEvent),
                ("settings", UI.OpenSettings, UI.CloseSettings),
                ("kitchen", () => UI.OpenTownMenu(CampTab.Cooking), UI.CloseTownMenu),
                ("route board", UI.OpenRoutes, UI.CloseRoutes),
            };
            foreach (var o in opens)
            {
                o.open(); yield return null;
                Assert.IsFalse(UI.TownWalking, o.name + " opens over the town");
                o.close(); yield return null;
                Assert.IsTrue(UI.TownWalking, o.name + " closes back to walking");
            }
            Assert.IsTrue(UI.TownWorldVisible);
            UI.OpenTownMenu(CampTab.Cooking); yield return null;
            Assert.IsTrue(UI.TownStationOpen && !UI.TownWorldVisible, "a station covers the town");
            UI.CloseTownMenu(); yield return null;

            // pop-ups stack: the newest shows, closing it goes back to the one under it
            UI.OpenPantry(); UI.OpenSeason(); yield return null;
            UI.CloseSeason(); yield return null;
            Assert.IsFalse(UI.TownWalking, "the larder is still open under the spice rack");
            UI.ClosePantry(); yield return null;
            Assert.IsTrue(UI.TownWalking);

            // Esc: closes the top pop-up; from walking it opens settings, and again closes them
            var key = typeof(GameUI).GetMethod("RouteKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Action esc = () => key.Invoke(UI, new object[] { KeyCode.Escape });
            UI.OpenPantry(); yield return null;
            esc(); yield return null;
            Assert.IsTrue(UI.TownWalking, "Esc closes the larder");
            esc(); yield return null;
            Assert.IsTrue(UI.SettingsOpen, "Esc in town opens settings");
            esc(); yield return null;
            Assert.IsFalse(UI.SettingsOpen, "Esc closes settings");
            Assert.IsTrue(UI.TownWalking);
        }

        IEnumerator EveryMove(DungeonController dc)
        {
            var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var pc = dc.Player; var pt = typeof(PlayerController);
            var useSkill = pt.GetMethod("UseSkill", bf); var attackHit = pt.GetMethod("AttackHit", bf); var atkField = pt.GetField("_atk", bf);
            var dashField = pt.GetField("_dash", bf); var startDash = pt.GetMethod("StartDash", bf);
            var room = dc.CurrentRoom;
            int used = 0, attacks = 0, dashes = 0; var skipped = new System.Collections.Generic.List<string>();
            foreach (var m in S.Content.moves)
            {
                pc.Teleport(room.PlayerStart);
                var target = dc.SpawnEnemy(room, Ids.Monster.Slime, false, false, room.NearestWalkable((Vector2)pc.Feet + Vector2.up * 2f));
                yield return Seconds(0.75f);   // it finishes spawning in
                if (m.slot == MoveSlot.Dash) { var was = dashField.GetValue(pc); dashField.SetValue(pc, m); startDash.Invoke(pc, null); dashField.SetValue(pc, was); dashes++; }
                else if (m.slot == MoveSlot.Attack) { var was = atkField.GetValue(pc); atkField.SetValue(pc, m); attackHit.Invoke(pc, null); atkField.SetValue(pc, was); attacks++; }
                else if ((bool)useSkill.Invoke(pc, new object[] { m })) used++;
                else skipped.Add(m.name);
                yield return Seconds(1.2f);   // let its effects and timers play out
                S.DungeonHealPlayer(9999);
                if (target != null && !target.Dead) target.TakeHit(99999f, false, Vector2.zero);
                yield return null;
            }
            yield return Seconds(1f);
            Assert.Greater(used, 0);
            Step(attacks + " attacks, " + dashes + " dashes and " + used + " skills played out" + (skipped.Count > 0 ? " (not used, nothing to do: " + string.Join(", ", skipped.ToArray()) + ")" : ""));
        }

        /// One monster against you: within a few seconds it has to move, wind up an attack and strike (you're kept alive).
        IEnumerator Fights(DungeonController dc, string monsterId, bool boss, string what)
        {
            var room = dc.CurrentRoom;
            dc.Player.Teleport(room.PlayerStart);   // the same open spot each time
            yield return null;
            var e = dc.SpawnEnemy(room, monsterId, false, boss, room.NearestWalkable((Vector2)dc.Player.Feet + Vector2.up * 3.5f));
            Assert.IsNotNull(e, what + " spawns");
            Vector2 start = e.transform.position;
            var stField = typeof(EnemyController).GetField("_st", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float moved = 0f; bool woundUp = false, struck = false, stepped = false;
            float until = Time.unscaledTime + 10f;
            while (Time.unscaledTime < until && !(struck && moved > 0.3f))
            {
                yield return null;
                S.DungeonHealPlayer(9999);
                if (e == null || e.Dead) break;
                if (!woundUp && !stepped && Time.unscaledTime > until - 6f)   // 4 s and nothing: it's stuck behind a rock, so step up to it
                { stepped = true; dc.Player.Teleport(room.NearestWalkable((Vector2)e.transform.position + Vector2.down * 1f)); }
                string st = stField.GetValue(e).ToString();
                if (st == "Windup") woundUp = true;
                if (woundUp && (st == "Striking" || st == "Charging")) struck = true;
                moved = Mathf.Max(moved, Vector2.Distance(start, e.transform.position));
            }
            string why = e == null ? "gone" : monsterId + " state " + stField.GetValue(e) + ", " + Vector2.Distance(e.transform.position, dc.Player.Feet).ToString("0.0") + " m away, moved " + moved.ToString("0.0") + ", playing " + dc.GameplayActive + ", time " + Time.timeScale + ", cd " + e.GetType().GetField("_cd", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(e);
            Assert.IsTrue(woundUp, what + " winds up an attack (" + why + ")");
            Assert.IsTrue(struck, what + " strikes");
            Assert.Greater(moved, 0.3f, what + " moves");
            if (e != null && !e.Dead) e.TakeHit(99999f, false, Vector2.zero);
            yield return Seconds(0.5f);
        }

        IEnumerator PooledLights(DungeonController dc)
        {
            // a light that rode on something scaled must come back at scale 1
            var big = new GameObject("smoke_scaled"); big.transform.localScale = Vector3.one * 3f;
            var rider = DungeonFx.Flash(dc.Player.Body, Color.white, 2f, 1f, 0.2f, big.transform);
            yield return Seconds(0.5f);
            UnityEngine.Object.Destroy(big);
            var again = DungeonFx.Flash(dc.Player.Body, Color.white, 2f, 1f, 0.2f);
            Assert.AreEqual(1f, again.transform.lossyScale.x, 0.001f, "a reused light keeps no leftover scale");
            // shadow safety is re-armed when a reused light casts shadows again in the same frame it's taken
            yield return Seconds(0.5f);
            var a = DungeonFx.Flash(dc.Player.Body, Color.white, 2f, 1f, 0.1f); LightSetup.CastShadows(a);
            yield return Seconds(0.4f);
            var b = DungeonFx.Flash(dc.Player.Body, Color.white, 2f, 1f, 0.3f); LightSetup.CastShadows(b);
            yield return null;
            var safe = b.GetComponent<ShadowSafeLight>();
            Assert.IsTrue(safe != null && safe.enabled, "a reused shadow-casting light has its shadow safety on");
            // floor-ember lights: lights lost with their parent don't use up the cap
            for (int i = 0; i < 14; i++) DungeonFx.FloorEmbers(dc.Player.Body, Color.red, 1);
            yield return Seconds(5.5f);
            int before = UnityEngine.Object.FindObjectsByType<FxLight>(FindObjectsSortMode.None).Length;
            DungeonFx.FloorEmbers(dc.Player.Body, Color.red, 1);
            yield return null;
            int after = UnityEngine.Object.FindObjectsByType<FxLight>(FindObjectsSortMode.None).Length;
            Assert.Greater(after, before, "floor embers still make a light after the first batch burned out");
            yield return Seconds(5.5f);
            Step("pooled lights come back clean: scale 1, shadow safety re-armed, floor-ember lights keep coming");
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
