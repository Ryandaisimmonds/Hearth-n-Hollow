// Hearth & Hollow — HUD over the real-time dungeon: HP, special + dash, floor, status, boss bar, loot chips,
// minimap, the "E" prompt, floating damage numbers and a Tab bag panel (pauses the dungeon).
using HearthHollow.Core;
using HearthHollow.Data;
using HearthHollow.Dungeon;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class DungeonScreen : UIScreen
    {
        // top-left
        readonly SpriteAnimView _portrait;
        readonly Label _name;
        readonly Bar _hp;
        readonly VisualElement _skill, _skillFill, _skill2, _skill2Fill, _dash, _dashFill;
        readonly VisualElement _skillIcon, _skill2Icon, _dashIcon;
        readonly Label _guard;
        readonly Label _skillKey, _skill2Key;
        string _iconKey;
        // top-centre
        readonly VisualElement _routeIcon;
        readonly Label _route, _floor, _status;
        readonly VisualElement _bossBox;
        readonly Label _bossName;
        readonly Bar _bossBar;
        // top-right
        readonly Label _coins, _xp, _kills, _bagChip;
        // bottom
        readonly Label _prompt;
        readonly VisualElement _map, _numbers, _hurt;
        // bag
        readonly VisualElement _bagPanel, _bagGrid;
        readonly Label _bagTitle;

        int _mapVersion = -1;
        bool _bagShown;
        IVisualElementScheduledItem _tick;

        public DungeonScreen(GameUI ui) : base(ui, "dungeon")
        {
            Root.pickingMode = PickingMode.Ignore;

            _hurt = UIKit.E("dungeon__hurt"); _hurt.pickingMode = PickingMode.Ignore;
            Root.Add(_hurt);
            _numbers = UIKit.E("dungeon__numbers"); _numbers.pickingMode = PickingMode.Ignore;
            Root.Add(_numbers);

            var top = UIKit.E("dungeon__top"); top.pickingMode = PickingMode.Ignore;
            Root.Add(top);

            // ── player card ──
            var card = UIKit.E("panel", "dhud__card");
            var row = UIKit.E("dhud__row");
            _portrait = new SpriteAnimView(null, "🧑‍🍳", "dhud__portrait");
            row.Add(Art.Framed(_portrait, "dhud__frame"));
            var info = UIKit.E("dhud__info");
            _name = UIKit.L("", "dhud__name");
            info.Add(_name);
            _hp = new Bar("bar--hp"); _hp.AddToClassList("dhud__hp");
            info.Add(_hp);
            var abil = UIKit.E("dhud__abilities");
            _skill = UIKit.E("dhud__ability", "dhud__ability--skill");
            _skillIcon = UIKit.E("dhud__icon-slot"); _skill.Add(_skillIcon);
            _skillFill = UIKit.E("dhud__cool"); _skill.Add(_skillFill);
            _skillKey = UIKit.L("K", "dhud__key"); _skill.Add(_skillKey);
            _skill2 = UIKit.E("dhud__ability", "dhud__ability--skill");
            _skill2Icon = UIKit.E("dhud__icon-slot"); _skill2.Add(_skill2Icon);
            _skill2Fill = UIKit.E("dhud__cool"); _skill2.Add(_skill2Fill);
            _skill2Key = UIKit.L("U", "dhud__key"); _skill2.Add(_skill2Key);
            _dash = UIKit.E("dhud__ability", "dhud__ability--dash");
            _dashIcon = UIKit.E("dhud__icon-slot"); _dash.Add(_dashIcon);
            _dashFill = UIKit.E("dhud__cool"); _dash.Add(_dashFill);
            _dash.Add(UIKit.L("Space", "dhud__key"));
            _guard = UIKit.L("", "badge", "dhud__guard");
            abil.Add(_skill); abil.Add(_skill2); abil.Add(_dash); abil.Add(_guard);
            info.Add(abil);
            row.Add(info);
            card.Add(row);
            top.Add(card);

            // ── centre: where you are ──
            var mid = UIKit.E("dhud__mid"); mid.pickingMode = PickingMode.Ignore;
            var where = UIKit.E("panel", "dhud__where");
            var wrow = UIKit.E("dhud__row");
            _routeIcon = UIKit.E("dhud__route-icon");
            wrow.Add(_routeIcon);
            _route = UIKit.L("", "dhud__route");
            wrow.Add(_route);
            _floor = UIKit.L("", "badge", "dhud__floor");
            wrow.Add(_floor);
            where.Add(wrow);
            _status = UIKit.L("", "dhud__status");
            where.Add(_status);
            mid.Add(where);
            _bossBox = UIKit.E("dhud__boss");
            _bossName = UIKit.L("", "dhud__boss-name");
            _bossBar = new Bar("bar--monster"); _bossBar.AddToClassList("dhud__boss-bar");
            _bossBox.Add(_bossName); _bossBox.Add(_bossBar);
            mid.Add(_bossBox);
            top.Add(mid);

            // ── right: the haul ──
            var chips = UIKit.E("dhud__chips");
            chips.Add(Art.Chip("coin", "🪙", out _coins, "chip--coins"));
            chips.Add(Art.Chip("xp", "✨", out _xp, "chip--xp"));
            chips.Add(Art.Chip("skull", "💀", out _kills));
            var bagChip = Art.Chip("bag", "🎒", out _bagChip, "dhud__bag-chip");
            bagChip.RegisterCallback<ClickEvent>(e => ToggleBag());
            bagChip.pickingMode = PickingMode.Position;
            chips.Add(bagChip);
            top.Add(chips);

            // ── bottom ──
            _prompt = UIKit.L("", "dhud__prompt");
            _prompt.pickingMode = PickingMode.Ignore;
            Root.Add(_prompt);
            var help = UIKit.L("WASD move · J attack · K / U skills · Space dash · E use · Tab bag · Esc menu", "dhud__help");
            help.pickingMode = PickingMode.Ignore;
            Root.Add(help);
            var mapBox = UIKit.E("panel", "dhud__mapbox");
            mapBox.pickingMode = PickingMode.Ignore;
            _map = UIKit.E("dhud__map");
            mapBox.Add(_map);
            Root.Add(mapBox);

            // ── bag (paused) ──
            _bagPanel = UIKit.E("overlay", "dhud__bagwrap");
            var box = UIKit.E("overlay__box", "dhud__bag");
            _bagTitle = UIKit.L("Bag", "overlay__title");
            box.Add(_bagTitle);
            box.Add(UIKit.L("Paused · right-click a stack to drop it", "muted", "small"));
            _bagGrid = UIKit.E("bag-grid", "dhud__bag-grid");
            box.Add(_bagGrid);
            box.Add(UIKit.B("Back to the dungeon  (Tab)", ToggleBag, "btn--primary"));
            _bagPanel.Add(box);
            UIKit.Show(_bagPanel, false);
            Root.Add(_bagPanel);
        }

        void ToggleBag() { DungeonHud.BagOpen = !DungeonHud.BagOpen; Tick(); }

        protected override void OnShow()
        {
            DungeonHud.ShowNumber = ShowNumber;
            DungeonHud.PlayerHurt = FlashHurt;
            _mapVersion = -1;
            if (_tick == null) _tick = Root.schedule.Execute(Tick).Every(50);
            else _tick.Resume();
        }

        protected override void OnHide()
        {
            if (_tick != null) _tick.Pause();
            if (DungeonHud.ShowNumber == ShowNumber) DungeonHud.ShowNumber = null;
            if (DungeonHud.PlayerHurt == FlashHurt) DungeonHud.PlayerHurt = null;
            _numbers.Clear();
            UIKit.Show(_bagPanel, false); _bagShown = false;
        }

        // ───────────────────────── session-driven bits ─────────────────────────
        public override void Refresh()
        {
            var run = S.Run;
            if (run == null) return;
            var cls = C.Class(run.classId);
            var cp = S.Profile.Progress(run.classId);
            int max = S.Stats != null ? S.Stats.maxHp : run.hp;
            _portrait.SetAnim(Art.ClassAnim(run.classId), cls != null ? cls.emoji : "🧑‍🍳");
            _name.text = (cls != null ? cls.name : run.classId.ToString()) + " · Lv " + cp.level;
            _hp.Set(run.hp, max, run.hp + " / " + max);
            _hp.SetLow(max > 0 && run.hp < max * 0.3f);

            _iconKey = null;   // re-read the loadout icons on the next tick

            var route = C.Route(run.routeId);
            Art.Set(_routeIcon, Art.Pic(ArtLibrary.GetRoute(run.routeId), route != null ? route.emoji : "", "ico", "dhud__route-pic"));
            _route.text = (route != null ? route.name : run.routeId) + (run.mod != RouteMod.None ? "  · " + Art.ModName(run.mod) : "");
            _floor.text = "Floor " + run.floor + " / " + run.floorsTotal;

            _coins.text = "+" + run.coinsEarned;
            _xp.text = "+" + run.killXp + " XP";
            _kills.text = run.kills.ToString();
            int used = 0; foreach (var b in run.bag) if (b != null && !string.IsNullOrEmpty(b.ingredientId) && b.count > 0) used++;
            _bagChip.text = "Bag " + used + "/" + run.bag.Count;
            if (_bagShown) RefreshBag();
        }

        // ───────────────────────── per-frame bits (from DungeonHud) ─────────────────────────
        void Tick()
        {
            var m1 = DungeonHud.Skill1Move; var m2 = DungeonHud.Skill2Move; var md = DungeonHud.DashMove;
            string key = (m1 != null ? m1.id : "-") + "|" + (m2 != null ? m2.id : "-") + "|" + (md != null ? md.id : "-");
            if (key != _iconKey)
            {
                _iconKey = key;
                SetMoveIcon(_skill, _skillIcon, m1);
                SetMoveIcon(_skill2, _skill2Icon, m2);
                SetMoveIcon(_dash, _dashIcon, md);
                bool open2 = S.Run != null && S.Profile.Progress(S.Run.classId).level >= C.tuning.secondSkillLevel;
                _skill2Key.text = m2 != null ? "U" : open2 ? "U —" : "Lv " + C.tuning.secondSkillLevel;
                if (m2 == null) _skill2.tooltip = open2 ? "Empty — pick a second skill in the Skills page at camp" : "Opens at level " + C.tuning.secondSkillLevel;
            }
            Cool(_skill, _skillFill, m1 != null, DungeonHud.Skill01);
            Cool(_skill2, _skill2Fill, m2 != null, DungeonHud.Skill2_01);
            _dashFill.style.height = Length.Percent((1f - DungeonHud.Dash01) * 100f);
            _guard.text = DungeonHud.BuffText ?? "";
            UIKit.Show(_guard, !string.IsNullOrEmpty(DungeonHud.BuffText));

            _status.text = DungeonHud.Status ?? "";
            _status.EnableInClassList("dhud__status--locked", DungeonHud.Locked);
            bool boss = DungeonHud.BossHp01 >= 0f;
            UIKit.Show(_bossBox, boss);
            if (boss) { _bossName.text = "👑 " + DungeonHud.BossName; _bossBar.Set(DungeonHud.BossHp01, 1f, ""); }

            string prompt = DungeonHud.Prompt;
            _prompt.text = string.IsNullOrEmpty(prompt) ? "" : "E   " + prompt;
            UIKit.Show(_prompt, !string.IsNullOrEmpty(prompt));

            if (DungeonHud.MapVersion != _mapVersion) { _mapVersion = DungeonHud.MapVersion; DrawMap(); }

            if (DungeonHud.BagOpen != _bagShown)
            {
                _bagShown = DungeonHud.BagOpen;
                UIKit.Show(_bagPanel, _bagShown);
                _bagPanel.pickingMode = _bagShown ? PickingMode.Position : PickingMode.Ignore;
                if (_bagShown) RefreshBag();
            }
        }

        static void Cool(VisualElement box, VisualElement fill, bool has, float ready01)
        {
            box.EnableInClassList("dhud__ability--locked", !has);
            fill.style.height = Length.Percent(has ? (1f - ready01) * 100f : 100f);
            box.EnableInClassList("dhud__ability--ready", has && ready01 >= 1f);
        }

        static void SetMoveIcon(VisualElement box, VisualElement slot, MoveData m)
        {
            slot.Clear();
            if (m != null) slot.Add(UIKit.L(m.emoji, "dhud__ability-emoji"));
            box.tooltip = m != null ? m.name + " — " + m.description : null;
        }

        void DrawMap()
        {
            _map.Clear();
            var rooms = DungeonHud.Map;
            if (rooms.Count == 0) return;
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach (var r in rooms) { minX = Mathf.Min(minX, r.cell.x); maxX = Mathf.Max(maxX, r.cell.x); minY = Mathf.Min(minY, r.cell.y); maxY = Mathf.Max(maxY, r.cell.y); }
            const float cw = 26f, ch = 18f, gap = 6f;
            _map.style.width = (maxX - minX + 1) * (cw + gap);
            _map.style.height = (maxY - minY + 1) * (ch + gap);
            foreach (var r in rooms)
            {
                var cell = UIKit.E("dmap__room");
                cell.style.left = (r.cell.x - minX) * (cw + gap);
                cell.style.top = (maxY - r.cell.y) * (ch + gap);
                cell.style.width = cw; cell.style.height = ch;
                cell.EnableInClassList("dmap__room--visited", r.visited);
                cell.EnableInClassList("dmap__room--current", r.current);
                cell.EnableInClassList("dmap__room--unknown", !r.visited);
                string mark = "";
                if (r.visited)
                {
                    switch (r.kind)
                    {
                        case RoomKind.Ladder: mark = "🪜"; break;
                        case RoomKind.Exit: mark = "🚪"; break;
                        case RoomKind.Boss: mark = "👑"; break;
                        case RoomKind.Gather: mark = "🌿"; break;
                        case RoomKind.Rest: mark = "🔥"; break;
                        case RoomKind.Start: mark = "⛺"; break;
                    }
                }
                if (mark.Length > 0) cell.Add(UIKit.L(mark, "dmap__mark"));
                if (r.visited)
                {
                    if (r.n) cell.Add(Link("dmap__link--n"));
                    if (r.e) cell.Add(Link("dmap__link--e"));
                }
                _map.Add(cell);
            }
        }

        static VisualElement Link(string cls) { var l = UIKit.E("dmap__link", cls); l.pickingMode = PickingMode.Ignore; return l; }

        void RefreshBag()
        {
            var run = S.Run;
            if (run == null) return;
            _bagGrid.Clear();
            int used = 0;
            for (int i = 0; i < run.bag.Count; i++)
            {
                var b = run.bag[i];
                bool filled = b != null && !string.IsNullOrEmpty(b.ingredientId) && b.count > 0;
                if (filled) used++;
                var slot = UIKit.E("slot", "bag-slot");
                slot.AddToClassList(filled ? "slot--filled" : "slot--empty");
                if (filled)
                {
                    slot.Add(Art.Pic(ArtLibrary.GetIngredient(b.ingredientId), UIKit.IngEmoji(C, b.ingredientId), "slot__emoji"));
                    slot.Add(UIKit.L(b.count.ToString(), "slot__count"));
                    slot.tooltip = UIKit.IngName(C, b.ingredientId) + " ×" + b.count;
                    int idx = i;
                    slot.RegisterCallback<PointerDownEvent>(e => { if (e.button == 1) S.BagDiscard(idx); });
                }
                foreach (var c in slot.Children()) c.pickingMode = PickingMode.Ignore;
                _bagGrid.Add(slot);
            }
            _bagTitle.text = "Bag  " + used + "/" + run.bag.Count;
        }

        // ───────────────────────── juice ─────────────────────────
        void ShowNumber(Vector3 world, string text, string cls)
        {
            var cam = Camera.main;
            if (cam == null || Root.panel == null) return;
            Vector2 p = RuntimePanelUtils.CameraTransformWorldToPanel(Root.panel, world, cam);
            var l = UIKit.L(text, "dnum");
            if (!string.IsNullOrEmpty(cls)) l.AddToClassList(cls);
            l.pickingMode = PickingMode.Ignore;
            l.style.left = p.x - 40f; l.style.top = p.y - 14f;
            _numbers.Add(l);
            float start = UIKit.Now;
            float drift = Random.Range(-12f, 12f);
            l.schedule.Execute(() =>
            {
                float t = (UIKit.Now - start) / 0.75f;
                l.style.translate = new Translate(drift * t, -36f * t, 0);
                l.style.opacity = 1f - Mathf.Clamp01((t - 0.5f) * 2f);
                if (t >= 1f) l.RemoveFromHierarchy();
            }).Every(16).Until(() => l.parent == null);
        }

        void FlashHurt()
        {
            _hurt.AddToClassList("dungeon__hurt--on");
            _hurt.schedule.Execute(() => _hurt.RemoveFromClassList("dungeon__hurt--on")).StartingIn(120);
        }

        public override void OnGameEvent(GameEvent e)
        {
            if (e.type == GameEventType.Coins && e.amount > 0 && DungeonController.Instance != null && DungeonController.Instance.Player != null)
                ShowNumber(DungeonController.Instance.Player.Body + Vector2.up * 1.1f, "+" + Mathf.RoundToInt(e.amount) + " 🪙", "dnum--coins");
        }

        public override bool HandleKey(KeyCode key) { return false; }   // gameplay keys are read by the dungeon
    }
}
