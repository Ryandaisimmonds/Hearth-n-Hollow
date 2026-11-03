// Hearth & Hollow — runs Hearth's Hollow, the walkable town (PREVIEW, milestone 1: grey-box).
// Built alongside the camp screen and only entered from Settings → "Visit Hearth's Hollow (preview)" until it does
// everything the camp does. It uses the same GameSession (the real save), so cooking, upgrades and gear are real.
//
// While the town is open and you're at camp (Phase.Hearth) it builds the town from TownLayout using the dungeon's
// meadow tiles, spawns you at the hammock and follows you with the camera. Walk up to something and press E / (Y):
// each spot opens the existing menu (Cooking tab, Larder, Upgrades, Workshop, Skills, Route Board). It tears down when
// you leave the town or set out, and rebuilds when you come back to camp.
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Data;
using HearthHollow.Dungeon;
using HearthHollow.UI;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace HearthHollow.Town
{
    public partial class TownController : MonoBehaviour   // visible upgrades + decorations: TownVisuals.cs
    {
        public static TownController Instance { get; private set; }
        public GameSession Session { get; private set; }
        public DungeonSettings Settings { get; private set; }
        public TownPlayer Player { get; private set; }
        public List<TownSpot> Spots { get; private set; }
        /// You can walk and interact (town open, at camp, no menu or pop-up in the way).
        public bool Walking { get { return _root != null && _ui != null && _ui.TownWalking; } }
        public TownSpot Near { get; private set; }

        GameUI _ui;
        GameObject _root;
        Camera _cam;
        bool _camSaved; Vector3 _camHome; float _camHomeSize; Color _camHomeBg;
        float _walkSince;
        SpriteRenderer _fire, _fireCore; Light2D _fireLight, _fireWall;   // the Hearth fire: colour + size by chapters cleared
        static Sprite _flame;
        int _announcedDay = -1; CampEvent _announcedEvent;
        public TownWeather.Kind Weather { get; private set; }
        ClassId _builtClass;
        CampEvent _builtEvent; int _builtDay; string _builtLook;

        public void Init(GameSession session, GameUI ui, DungeonSettings settings)
        {
            Instance = this;
            Session = session; _ui = ui; Settings = settings;
            _cam = Camera.main;
            // remember the camp camera once, at start-up (the dungeon also moves the camera; restoring to this keeps them apart)
            if (_cam != null) { _camHome = _cam.transform.position; _camHomeSize = _cam.orthographicSize; _camHomeBg = _cam.backgroundColor; _camSaved = true; }
        }

        void OnDestroy()
        {
            if (_root != null) Teardown();
            if (Instance == this) Instance = null;
        }

        bool Wanted { get { return _ui != null && _ui.TownOpen && Session.Phase == Phase.Hearth && Settings != null; } }

        void Update()
        {
            if (!Wanted) { if (_root != null) Teardown(); return; }
            var evNow = Session.Hearth != null ? Session.Hearth.campEvent : CampEvent.None;
            if (_root == null || _builtClass != Session.Profile.selectedClass || _builtEvent != evNow || _builtDay != Session.Profile.day || _builtLook != LookKey())
            {
                // swapping class in the training yard rebuilds the town: keep standing where you were
                bool keep = _root != null && Player != null;
                Vector2 at = keep ? (Vector2)Player.transform.position : TownLayout.Spawn;
                Build();
                if (keep && Player != null) Player.Teleport(at);
                Announce();
            }
            UpdateFire();

            // a short grace period after a menu closes, so the key that closed it doesn't reopen something
            if (!Walking) { _walkSince = Time.unscaledTime; Near = null; PushHud(); return; }
            Near = Nearest();
            if (Near != null && Time.unscaledTime - _walkSince > 0.2f && DungeonInput.Interact) Use(Near);
            PushHud();
        }

        void LateUpdate()
        {
            if (_root == null || _cam == null || Player == null) return;
            // follow the player, kept inside the town's edges
            float h = _cam.orthographicSize, w = h * _cam.aspect;
            Vector3 p = Player.transform.position + new Vector3(0, 0.5f, 0);
            p.x = Mathf.Clamp(p.x, w, Mathf.Max(w, TownLayout.Width - w));
            p.y = Mathf.Clamp(p.y, h, Mathf.Max(h, TownLayout.Height - h));
            p.z = _cam.transform.position.z;
            _cam.transform.position = Vector3.Lerp(_cam.transform.position, p, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 12f));
            if (TownHud.Instance != null) TownHud.Instance.PlaceLabels(_cam, Spots, Near, Player.transform.position);
        }

        TownSpot Nearest()
        {
            if (Player == null) return null;
            Vector2 me = Player.transform.position;
            TownSpot best = null; float bd = float.MaxValue;
            foreach (var s in Spots)
            {
                Vector2 c = s.pos + new Vector2(0, Mathf.Min(0.5f, s.size.y * 0.3f));
                float d = Vector2.Distance(me, c);
                if (d <= s.reach + Mathf.Max(s.size.x, s.size.y) * 0.4f && d < bd) { bd = d; best = s; }
            }
            return best;
        }

        void PushHud()
        {
            if (TownHud.Instance == null) return;
            TownHud.Instance.Prompt = Near != null ? Near.emoji + " " + Near.name + " — " + Near.verb : null;
        }

        // ───────────────────────── interactions ─────────────────────────
        void Use(TownSpot s)
        {
            switch (s.action)
            {
                case TownAction.Cooking: _ui.OpenTownMenu(CampTab.Cooking); break;
                case TownAction.Upgrades: _ui.OpenTownMenu(CampTab.Upgrades); break;
                case TownAction.Workshop: _ui.OpenTownMenu(CampTab.Workshop); break;
                case TownAction.Training: _ui.OpenTownMenu(CampTab.Skills); break;
                case TownAction.Larder: _ui.OpenPantry(); break;
                case TownAction.Gate: _ui.OpenRoutes(); break;
                case TownAction.BackToCamp: _ui.CloseTown(); break;
                case TownAction.Hearth:
                {
                    int done = 0, total = 0;
                    foreach (var r in Session.Content.routes) if (r.chapter > 0) { total++; if (Session.Profile.clearedRouteIds.Contains(r.id)) done++; }
                    string mood = done >= total ? "blazes blue-white — the Everflame is home" : done * 2 >= total ? "burns bright and golden" : done > 0 ? "is growing stronger" : "burns low";
                    _ui.Toast("🔥 The Hearth " + mood + ". " + done + " of " + total + " chapters of the Ember Path cleared.");
                    break;
                }
                case TownAction.Bed: _ui.Toast("🛏️ Your hammock. You'll wake up here after each expedition."); break;
                case TownAction.Wren: TalkToWren(); break;
                case TownAction.ClassNpc: TalkToClass(s.classId); break;
                case TownAction.Forager:
                {
                    var names = new List<string>();
                    if (Session.Hearth != null) foreach (var g in Session.Hearth.gift) names.Add(g.count + "× " + UIKit.IngEmoji(Session.Content, g.ingredientId) + " " + UIKit.IngName(Session.Content, g.ingredientId));
                    _ui.Toast("🧺 A passing forager left you a basket: " + (names.Count > 0 ? string.Join(", ", names.ToArray()) : "a few things") + ". It's already in the larder.");
                    break;
                }
                case TownAction.Plot: _ui.Toast("🚧 Fenced off. Something new will be built here one day."); break;
                case TownAction.Notice: _ui.OpenCampEvent(); break;
                case TownAction.Merchant:
                    if (Session.Hearth != null && Session.Hearth.campEvent == CampEvent.Merchant) _ui.OpenCampEvent();
                    else _ui.Toast("🛒 The merchant's stall is empty today. A travelling merchant visits now and then.");
                    break;
                case TownAction.Armourer:
                    if (Session.Hearth != null && Session.Hearth.campEvent == CampEvent.Armourer) { _ui.OpenTownMenu(CampTab.Upgrades); _ui.Toast("🛡️ The armourer's in town: gear is " + UIKit.Pct(Session.Content.tuning.armourerDiscount) + " off today."); }
                    else _ui.Toast("🛡️ The armourer's stall is shut. They pass through every so often with a discount.");
                    break;
                case TownAction.Baker: _ui.Toast("🥖 Baker: \"Camp Bread's free at the cooking area, every day. Can't beat that!\""); break;
                case TownAction.Florist: _ui.Toast("🌼 Florist: \"The town's looking brighter since the fire came back to life.\""); break;
                case TownAction.Fishing:
                    if (Session.CanFish) _ui.OpenFishing();
                    else _ui.Toast("🎣 You've had today's catch. The fish will bite again after your next expedition.");
                    break;
            }
        }

        // ───────────────────────── build / teardown ─────────────────────────
        void Build()
        {
            if (_root != null) DestroyWorld();
            _builtClass = Session.Profile.selectedClass;
            _builtEvent = Session.Hearth != null ? Session.Hearth.campEvent : CampEvent.None;
            _builtDay = Session.Profile.day;
            _builtLook = LookKey();
            if (_announcedDay == _builtDay && _builtEvent != _announcedEvent) _announcedDay = -1;   // the day's event changed (dev): announce it again
            _announcedEvent = _builtEvent;
            var theme = Settings.ThemeFor("meadow");
            _root = new GameObject("Town · Hearth's Hollow (preview)");

            // light (sprites are lit in URP 2D)
            var lgo = new GameObject("Global Light");
            lgo.transform.SetParent(_root.transform, false);
            var light = lgo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.color = theme != null ? theme.lightColor : Color.white;
            light.intensity = theme != null ? theme.lightIntensity : 1f;
            DungeonController.ApplyToAllLayers(light);

            BuildTiles(theme);
            Spots = TownLayout.Spots();
            // your other two classes hang around the training yard; talk to one to swap
            int k = 0;
            foreach (var cd in Session.Content.classes)
            {
                if (cd.id == Session.Profile.selectedClass || k >= TownLayout.ClassSpots.Length) continue;
                var at = TownLayout.ClassSpots[k++];
                Spots.Add(new TownSpot { id = "class_" + cd.id, name = cd.name, emoji = cd.emoji, verb = "Talk", action = TownAction.ClassNpc, classId = cd.id, pos = at, reach = 1.4f });
            }
            // visitors on event days
            var ev = Session.Hearth != null ? Session.Hearth.campEvent : CampEvent.None;
            if (ev == CampEvent.Forager)
                Spots.Add(new TownSpot { id = "forager", name = "Forager's basket", emoji = "🧺", verb = "Look", action = TownAction.Forager, pos = TownLayout.ForagerBasket, size = new Vector2(0.9f, 0.6f), color = new Color(0.7f, 0.55f, 0.3f), reach = 1.3f });
            foreach (var s in Spots) BuildSpot(s, theme);
            BuildFences();
            BuildPond();
            BuildDecor();   // lanterns, flowers and bunting by chapters cleared
            if (Art != null) BuildScenery();   // trees round the edge, the well, benches, stones by the pond
            BuildWorkAreas();   // the smithy, workshop, cooking area and training yard come to life (TownWorkAreas.cs)
            if (ev == CampEvent.Merchant) Visitor("Merchant", "Ranger", new Color(0.8f, 0.6f, 1f), TownLayout.MerchantStand);
            if (ev == CampEvent.Armourer) Visitor("Armourer", "Warden", new Color(0.75f, 0.75f, 0.8f), TownLayout.ArmourerStand);
            ApplyWeather(light, theme, ev);
            GameLook.ForTown(Weather == TownWeather.Kind.Night, Weather == TownWeather.Kind.Rain);   // HD-2D grade + tilt-shift

            var pgo = new GameObject("Player");
            pgo.transform.SetParent(_root.transform, false);
            Player = pgo.AddComponent<TownPlayer>();
            Player.Setup(this, Session.Profile.selectedClass);
            Player.Teleport(TownLayout.Spawn);
            if (Art != null && Weather != TownWeather.Kind.Calm) CarriedLight();   // night / storm: your own light, like the dungeon
            SpawnCritters();

            if (_cam == null) _cam = Camera.main;
            if (_cam != null)
            {
                _cam.orthographicSize = Settings.townCameraSize;
                _cam.backgroundColor = Weather == TownWeather.Kind.Night ? new Color(0.03f, 0.04f, 0.09f) : Weather == TownWeather.Kind.Rain ? new Color(0.08f, 0.1f, 0.13f)
                                     : theme != null ? theme.background : new Color(0.1f, 0.14f, 0.1f);
                _cam.transform.position = new Vector3(TownLayout.Spawn.x, TownLayout.Spawn.y, _cam.transform.position.z);
            }
        }

        void BuildTiles(DungeonTheme theme)
        {
            if (Art != null) { BuildTilesArt(Art); return; }   // Cainos art: TownArtBuild.cs
            var grid = new GameObject("Grid").AddComponent<Grid>();
            grid.transform.SetParent(_root.transform, false);
            var ground = NewMap(grid, "Ground", 0);
            var walls = NewMap(grid, "Walls", 2);
            var decor = NewMap(grid, "Decor", 1);
            walls.gameObject.AddComponent<TilemapCollider2D>();
            var rng = new System.Random(1234);
            for (int x = -2; x < TownLayout.Width + 2; x++)
                for (int y = -2; y < TownLayout.Height + 2; y++)
                {
                    var c = new Vector3Int(x, y, 0);
                    if (TownLayout.IsOpen(x, y))
                    {
                        ground.SetTile(c, theme != null && theme.floorAlt != null && rng.NextDouble() < theme.floorAltChance ? theme.floorAlt : theme != null ? theme.floor : null);
                        if (theme != null && theme.decor.Count > 0 && rng.NextDouble() < theme.decorDensity) decor.SetTile(c, theme.decor[rng.Next(theme.decor.Count)]);
                    }
                    else
                    {
                        bool face = TownLayout.IsOpen(x, y - 1) && theme != null && theme.wallFace != null;
                        walls.SetTile(c, face ? theme.wallFace : theme != null ? theme.wall : null);
                    }
                }
            if (theme != null) { ground.color = theme.floorTint; walls.color = theme.wallTint; }
        }

        Tilemap NewMap(Grid grid, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(grid.transform, false);
            var tm = go.AddComponent<Tilemap>();
            var tr = go.AddComponent<TilemapRenderer>();
            tr.sortingOrder = order;
            return tm;
        }

        void BuildSpot(TownSpot s, DungeonTheme theme)
        {
            var go = new GameObject("Spot · " + s.name);
            go.transform.SetParent(_root.transform, false);
            go.transform.position = s.pos;

            if (s.action == TownAction.Wren || s.action == TownAction.ClassNpc)
            {
                // a person: Old Wren (placeholder: the Cook's art, greyed) or one of your other classes
                bool wren = s.action == TownAction.Wren;
                bool wrenArt = wren && ArtLibrary.GetClass("Wren") != null;
                Person(go, wren ? (wrenArt ? "Wren" : "Cook") : s.classId.ToString(), wren && !wrenArt ? new Color(0.8f, 0.78f, 0.9f) : Color.white, s.pos);
                var c = go.AddComponent<CircleCollider2D>(); c.radius = 0.3f; c.offset = new Vector2(0, 0.2f);
                return;
            }

            // placeholder block standing on the spot (bottom-centre at pos), or Cainos props where there's art for it
            if (s.size.x > 0f && s.size.y > 0f && !(Art != null && DressSpot(go, s)))
            {
                var block = new GameObject("Block");
                block.transform.SetParent(go.transform, false);
                block.transform.localPosition = new Vector3(0, s.size.y / 2f, 0);
                block.transform.localScale = new Vector3(s.size.x / Px, s.size.y / Px, 1);
                var sr = block.AddComponent<SpriteRenderer>();
                sr.sprite = DungeonFx.Pixel;
                sr.color = s.color;
                sr.sortingOrder = DungeonController.SortOrder(s.pos.y);
            }
            if (s.size.x > 0f && s.size.y > 0f && s.solid)
            {
                var bc = go.AddComponent<BoxCollider2D>();
                bc.size = new Vector2(s.size.x, Mathf.Min(s.size.y, 0.8f));
                bc.offset = new Vector2(0, Mathf.Min(s.size.y, 0.8f) / 2f);
            }
            // a real prop where the dungeon has one
            Sprite prop = null;
            if (theme != null)
            {
                if (s.action == TownAction.Gate) prop = theme.doorOpen;
                else if (s.action == TownAction.Larder) prop = theme.forage;
            }
            if (s.action == TownAction.Hearth) BuildFire(go, s);
            if (s.action == TownAction.Cooking) BuildKitchenArea(go, s, theme);   // stations grow with their levels
            if (s.action == TownAction.Larder) BuildShelves(s);                   // shelves + crates grow with the pantry
            if (prop != null)
            {
                var pg = new GameObject("Prop");
                pg.transform.SetParent(go.transform, false);
                pg.transform.localPosition = new Vector3(0, Mathf.Max(0.3f, s.size.y * 0.55f), 0);
                float k = s.action == TownAction.Hearth ? 2.2f : 1.2f;
                pg.transform.localScale = new Vector3(k, k, 1);
                var sr = pg.AddComponent<SpriteRenderer>();
                sr.sprite = prop;
                sr.sortingOrder = DungeonController.SortOrder(s.pos.y) + 1;
            }
        }

        void BuildFences()
        {
            if (Art != null) { BuildFencesArt(); return; }
            foreach (var f in TownLayout.Fences)
            {
                var go = new GameObject("Fence");
                go.transform.SetParent(_root.transform, false);
                float x0 = f.x, x1 = f.y + 1, y = f.z;
                go.transform.position = new Vector3((x0 + x1) / 2f, y + 0.5f, 0);
                // posts + rails (placeholder)
                for (float x = x0 + 0.5f; x < x1; x += 1f)
                {
                    var post = new GameObject("Post");
                    post.transform.SetParent(go.transform, false);
                    post.transform.position = new Vector3(x, y + 0.45f, 0);
                    post.transform.localScale = new Vector3(0.18f / Px, 0.9f / Px, 1);
                    var ps = post.AddComponent<SpriteRenderer>(); ps.sprite = DungeonFx.Pixel; ps.color = new Color(0.45f, 0.3f, 0.16f); ps.sortingOrder = DungeonController.SortOrder(y);
                }
                for (int r = 0; r < 2; r++)
                {
                    var rail = new GameObject("Rail");
                    rail.transform.SetParent(go.transform, false);
                    rail.transform.position = new Vector3((x0 + x1) / 2f, y + 0.3f + r * 0.35f, 0);
                    rail.transform.localScale = new Vector3((x1 - x0) / Px, 0.1f / Px, 1);
                    var rs = rail.AddComponent<SpriteRenderer>(); rs.sprite = DungeonFx.Pixel; rs.color = new Color(0.55f, 0.38f, 0.2f); rs.sortingOrder = DungeonController.SortOrder(y) + 1;
                }
                var bc = go.AddComponent<BoxCollider2D>();
                bc.size = new Vector2(x1 - x0, 0.6f);
            }
        }

        CharacterAnimator Person(GameObject go, string classArt, Color tint, Vector2 pos)
        {
            var body = new GameObject("Body");
            body.transform.SetParent(go.transform, false);
            body.AddComponent<SpriteRenderer>();
            var an = body.AddComponent<CharacterAnimator>();
            an.Setup(ArtLibrary.GetClass(classArt), Settings.characterPixelsPerUnit, 1f);
            if (an.Renderer != null) { an.Renderer.color = tint; an.Renderer.sortingOrder = DungeonController.SortOrder(pos.y); }
            if (Art != null) Silhouette(an, false, null, 1f);
            return an;
        }

        /// A visitor standing behind their stall on an event day (placeholder art: a tinted class).
        void Visitor(string name, string classArt, Color tint, Vector2 pos)
        {
            if (ArtLibrary.GetClass(name) != null) { classArt = name; tint = Color.white; }   // own sprite (Ninja Adventure) when there is one
            var go = new GameObject("Visitor · " + name);
            go.transform.SetParent(_root.transform, false);
            go.transform.position = pos;
            Person(go, classArt, tint, pos);
        }

        // ───────────────────────── people ─────────────────────────
        void TalkToWren()
        {
            var C = Session.Content; var p = Session.Profile;
            var pages = new List<string>();
            if (!p.metWren)
            {
                pages.Add("Well now, a new face by my fire! I'm Wren — I've kept this town's Hearth since before you were born. It's guttering, and the fog's creeping in. You'll need to cook well and walk far to set it right.");
                pages.Add("🍳 The cooking area by the fire is where it starts. Pick a recipe, choose your ingredients — the finer they are, the better the meal tends to come out. Cooked meals wait in the meal pantry until you need them.");
                pages.Add("🧺 Before you set out, pack up to three meals on your tray. They're eaten at the gate and their strength lasts the whole trip. Camp Bread's always there if the pantry's bare.");
                pages.Add("🔨 The Workshop makes the stations better — more dishes and a better chance of a fine meal — but it needs timber, stone and the like from the dungeons. The smithy sells gear and trinkets.");
                pages.Add("⭐ Your friends in the training yard can take a turn if you'd rather. The pond's good for a fish a day, and the gate to the south opens the Ember Path. Come and talk to me whenever you're unsure where to go next.");
                p.metWren = true;
            }
            var ch = Routes.CurrentChapter(C, p);
            if (ch == null) pages.Add("The Everflame burns again — look at it go! Every road out there is yours now. Go where you like, and bring me back something tasty.");
            else
            {
                string obj = Routes.Objective(C, p);
                string line = "Here's where the Ember Path stands: " + obj;
                if (!string.IsNullOrEmpty(ch.storyIntro)) line += "\n\n" + ch.storyIntro;
                pages.Add(line);
                if (ch.hazard != Hazard.None)
                    pages.Add(Routes.HazardEmoji(ch.hazard) + " Mind the " + Routes.HazardName(ch.hazard).ToLower() + " down there. Pack " + Routes.CounterHint(ch.hazard) + " — one softens it, two stop it.");
            }
            _ui.OpenDialogue("Old Wren", "👵", pages);
            Session.SaveProfile();   // remember that Wren has met you
        }

        void TalkToClass(ClassId id)
        {
            var C = Session.Content;
            var cd = C.Class(id);
            var cur = C.Class(Session.Profile.selectedClass);
            var cp = Session.Profile.Progress(id);
            string name = cd != null ? cd.name : id.ToString();
            var pages = new List<string>
            {
                (cd != null ? cd.description : "") + "\n\nWant me to take the next trip" + (cur != null ? " while the " + cur.name + " has a rest?" : "?")
            };
            _ui.OpenDialogue(name + (cd != null ? " · " + cd.role : ""), cd != null ? cd.emoji : "🧑", pages, "Swap to the " + name, () =>
            {
                Session.SelectClass(id);
                _ui.Toast((cd != null ? cd.emoji + " " : "") + "You're the " + name + " now.");
            });
        }

        // ───────────────────────── weather & visitors ─────────────────────────
        void ApplyWeather(Light2D light, DungeonTheme theme, CampEvent ev)
        {
            Weather = ev == CampEvent.Storm ? TownWeather.Kind.Rain : ev == CampEvent.HarvestMoon ? TownWeather.Kind.Night : TownWeather.Kind.Calm;
            if (Weather == TownWeather.Kind.Rain) { light.color = new Color(0.7f, 0.76f, 0.92f); light.intensity = 0.5f; }
            if (Weather == TownWeather.Kind.Night) { light.color = new Color(0.5f, 0.58f, 0.95f); light.intensity = 0.28f; }
            var wgo = new GameObject("Weather");
            wgo.transform.SetParent(_root.transform, false);
            wgo.AddComponent<TownWeather>().Setup(Weather, _cam != null ? _cam : Camera.main);
        }

        /// Once a day, say what's going on in town (instead of the camp screen's pop-up).
        void Announce()
        {
            if (_announcedDay == Session.Profile.day || Session.Hearth == null) return;
            _announcedDay = Session.Profile.day;
            switch (Session.Hearth.campEvent)
            {
                case CampEvent.Merchant: _ui.Toast("🛒 A travelling merchant has set up in the market street."); break;
                case CampEvent.Armourer: _ui.Toast("🛡️ An armourer's in town — gear is cheaper at their stall today."); break;
                case CampEvent.Forager: _ui.Toast("🧺 A forager left a basket by the gate."); break;
                case CampEvent.Storm: _ui.Toast("🌧️ A storm rolls over the Hollow. Every route is Dangerous today."); break;
                case CampEvent.HarvestMoon: _ui.Toast("🌕 A harvest moon tonight — every good forage out there yields a little extra."); break;
            }
        }

        // ───────────────────────── the Hearth fire (by story chapter) ─────────────────────────
        /// 0 at the start of the story, 1 when every chapter of the Ember Path is cleared.
        public float StoryProgress
        {
            get
            {
                int done = 0, total = 0;
                foreach (var r in Session.Content.routes) if (r.chapter > 0) { total++; if (Session.Profile.clearedRouteIds.Contains(r.id)) done++; }
                return total > 0 ? done / (float)total : 0f;
            }
        }

        /// Small normal fire → bright gold → blazing blue-white by the end of the story.
        public static Color FireColour(float t)
        {
            Color a = new Color(1f, 0.55f, 0.2f), b = new Color(1f, 0.85f, 0.45f), c = new Color(1f, 0.98f, 0.9f), d = new Color(0.6f, 0.82f, 1f);
            if (t < 0.4f) return Color.Lerp(a, b, t / 0.4f);
            if (t < 0.75f) return Color.Lerp(b, c, (t - 0.4f) / 0.35f);
            return Color.Lerp(c, d, (t - 0.75f) / 0.25f);
        }

        /// A white pixel-art flame (tinted at runtime), so the colour can go all the way to blue.
        static Sprite Flame
        {
            get
            {
                if (_flame != null) return _flame;
                int w = 16, h = 24;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float fy = y / (float)(h - 1);                       // 0 bottom .. 1 tip
                        float half = (fy < 0.35f ? Mathf.Lerp(5.5f, 7.5f, fy / 0.35f) : Mathf.Lerp(7.5f, 0.5f, (fy - 0.35f) / 0.65f));
                        half += (fy > 0.4f ? Mathf.Sin(fy * 17f) * 0.8f : 0f);   // a little lick of flame
                        bool inside = Mathf.Abs(x + 0.5f - w / 2f) <= half;
                        tex.SetPixel(x, y, inside ? Color.white : new Color(1, 1, 1, 0));
                    }
                tex.Apply();
                _flame = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
                return _flame;
            }
        }

        void BuildFire(GameObject parent, TownSpot s)
        {
            var outer = new GameObject("Fire");
            outer.transform.SetParent(parent.transform, false);
            outer.transform.localPosition = new Vector3(0, 0.45f, 0);
            _fire = outer.AddComponent<SpriteRenderer>();
            _fire.sprite = Flame;
            _fire.sortingOrder = DungeonController.SortOrder(s.pos.y) + 1;
            var core = new GameObject("Core");
            core.transform.SetParent(outer.transform, false);
            core.transform.localScale = new Vector3(0.55f, 0.6f, 1);
            _fireCore = core.AddComponent<SpriteRenderer>();
            _fireCore.sprite = Flame;
            _fireCore.sortingOrder = _fire.sortingOrder + 1;
            var lg = new GameObject("Fire Light");
            lg.transform.SetParent(parent.transform, false);
            lg.transform.localPosition = new Vector3(0, 1f, 0);
            _fireLight = lg.AddComponent<Light2D>();
            _fireLight.lightType = Light2D.LightType.Point;
            DungeonController.ApplyToAllLayers(_fireLight);
            if (Art != null) { _fireWall = WallGlow(_fireLight); DungeonController.CastShadows(_fireLight, 0.8f, 0.5f); }
            UpdateFire();
        }

        void UpdateFire()
        {
            if (_fire == null) return;
            float t = StoryProgress;
            float flicker = 1f + 0.05f * Mathf.Sin(Time.unscaledTime * 9f) + 0.03f * Mathf.Sin(Time.unscaledTime * 23f);
            float size = Mathf.Lerp(0.9f, 2.4f, t);
            _fire.transform.localScale = new Vector3(size * (2f - flicker), size * flicker, 1);
            Color col = FireColour(t);
            _fire.color = col;
            if (_fireCore != null) _fireCore.color = Color.Lerp(col, Color.white, 0.65f);
            if (_fireLight != null)
            {
                _fireLight.color = col;
                _fireLight.intensity = Mathf.Lerp(0.6f, 1.2f, t) * flicker;
                _fireLight.pointLightOuterRadius = Mathf.Lerp(2.5f, 5.5f, t);
                _fireLight.pointLightInnerRadius = Mathf.Lerp(0.3f, 1f, t);
                if (_fireWall != null) { _fireWall.color = col; _fireWall.intensity = _fireLight.intensity * 0.6f; _fireWall.pointLightOuterRadius = _fireLight.pointLightOuterRadius * 1.6f; }
            }
        }

        // ───────────────────────── the pond + creatures ─────────────────────────
        void BuildPond()
        {
            var r = TownLayout.Pond;
            var go = new GameObject("Pond");
            go.transform.SetParent(_root.transform, false);
            go.transform.position = new Vector3(r.center.x, r.center.y, 0);
            var bank = new GameObject("Bank");
            bank.transform.SetParent(go.transform, false);
            bank.transform.localScale = new Vector3((r.width + 0.4f) / Px, (r.height + 0.4f) / Px, 1);
            var bs = bank.AddComponent<SpriteRenderer>(); bs.sprite = DungeonFx.Pixel; bs.color = Art != null ? new Color(0.3f, 0.34f, 0.2f) : new Color(0.42f, 0.36f, 0.22f); bs.sortingOrder = 1;
            var water = new GameObject("Water");
            water.transform.SetParent(go.transform, false);
            water.transform.localScale = new Vector3(r.width / Px, r.height / Px, 1);
            var ws = water.AddComponent<SpriteRenderer>(); ws.sprite = DungeonFx.Pixel; ws.color = Art != null ? new Color(0.24f, 0.44f, 0.55f) : new Color(0.22f, 0.5f, 0.75f); ws.sortingOrder = 2;
            if (Art != null)
            {
                // a lighter middle and a few glints, to sit better with the pack's muted colours
                Box(go.transform, Vector2.zero, new Vector2(r.width - 0.7f, r.height - 0.7f), new Color(0.3f, 0.53f, 0.63f), 3);
                var gr = new System.Random(5);
                for (int i = 0; i < 7; i++)
                    Box(go.transform, new Vector2(((float)gr.NextDouble() - 0.5f) * (r.width - 1.2f), ((float)gr.NextDouble() - 0.5f) * (r.height - 1.2f)), new Vector2(0.3f + (float)gr.NextDouble() * 0.3f, 0.05f), new Color(0.75f, 0.88f, 0.92f, 0.6f), 4);
            }
            // lily pads
            var rng = new System.Random(7);
            for (int i = 0; i < 5; i++)
            {
                var pad = new GameObject("Lily pad");
                pad.transform.SetParent(go.transform, false);
                pad.transform.position = new Vector3(r.x + 0.5f + (float)rng.NextDouble() * (r.width - 1f), r.y + 0.5f + (float)rng.NextDouble() * (r.height - 1f), 0);
                pad.transform.localScale = new Vector3(0.45f, 0.45f, 1);
                var ps = pad.AddComponent<SpriteRenderer>(); ps.sprite = DungeonFx.Circle; ps.color = new Color(0.35f, 0.7f, 0.3f); ps.sortingOrder = 5;
            }
            var bc = go.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(r.width, r.height);
        }

        void SpawnCritters()
        {
            // (monster art id, size, tint, home, roam range)
            var list = new List<object[]>
            {
                new object[] { "slime", 0.8f, new Color(0.7f, 1f, 0.7f), new Vector2(24f, 16f), 4f },
                new object[] { "slime", 0.75f, new Color(0.8f, 0.9f, 1f), new Vector2(30f, 14f), 3f },
                new object[] { "boar", 0.7f, new Color(1f, 0.85f, 0.85f), new Vector2(8f, 18f), 3.5f },
                new object[] { "boar", 0.65f, new Color(1f, 0.9f, 0.85f), new Vector2(6f, 24f), 3f },
                new object[] { "bat", 0.75f, Color.white, new Vector2(16f, 6f), 4f },
                new object[] { "bat", 0.7f, Color.white, new Vector2(43f, 20f), 4f },
                new object[] { "slime", 0.65f, new Color(0.5f, 0.85f, 0.45f), new Vector2(7.6f, 4.6f), 1.5f },   // a frog-ish slime by the pond
                new object[] { "slime", 0.8f, new Color(1f, 0.85f, 0.6f), new Vector2(26f, 33f), 3f },
                new object[] { "bat", 0.7f, new Color(1f, 0.95f, 0.7f), new Vector2(20f, 26f), 3f },
                new object[] { "boar", 0.7f, new Color(0.95f, 0.85f, 0.75f), new Vector2(26f, 8f), 2.5f },
            };
            int seed = 11;
            foreach (var c in list)
            {
                var art = ArtLibrary.GetMonster((string)c[0]);
                if (art == null) continue;
                var go = new GameObject("Critter · " + c[0]);
                go.transform.SetParent(_root.transform, false);
                var cr = go.AddComponent<TownCritter>();
                cr.Setup(this, art, (float)c[1], (Color)c[2], (Vector2)c[3], (float)c[4], seed++);
                if (Art != null) Silhouette(go.GetComponentInChildren<CharacterAnimator>(), true, (string)c[0], (float)c[1]);
            }
        }

        /// World size of the 1-pixel placeholder sprite (scale = wanted size / Px).
        static float Px { get { var b = DungeonFx.Pixel.bounds.size.x; return b > 0f ? b : 1f; } }

        void DestroyWorld()
        {
            if (_root != null) Destroy(_root);
            _root = null; Player = null; Near = null; _fire = null; _fireCore = null; _fireLight = null;
        }

        void Teardown()
        {
            DestroyWorld();
            GameLook.Release("town");
            if (TownHud.Instance != null) TownHud.Instance.Prompt = null;
            if (_cam != null && _camSaved && (Session == null || !Session.InDungeon))   // the dungeon owns the camera during a run
            {
                _cam.transform.position = _camHome; _cam.orthographicSize = _camHomeSize; _cam.backgroundColor = _camHomeBg;
            }
        }
    }
}
