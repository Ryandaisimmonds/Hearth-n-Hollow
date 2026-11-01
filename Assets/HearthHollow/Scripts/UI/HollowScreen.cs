// Hearth & Hollow — the Hollow: fog grid, HUD, tray (eat), bag (drag merge/swap, discard), flee.
using HearthHollow.Core;
using HearthHollow.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class HollowScreen : UIScreen
    {
        // HUD
        readonly Label _route, _coins, _xp, _rooms, _kills;
        readonly Label _statDmg, _statArmour, _statCrit, _statCoin;
        readonly VisualElement _statCoinRow, _routeIcon;
        readonly Bar _hp;
        readonly VisualElement _hudHost;   // float-text host for heals etc.
        readonly VisualElement _synergies;
        // grid
        readonly VisualElement _grid;
        VisualElement[] _tiles;
        Label[] _tileLabels;
        SpriteAnimView[] _tileAnims;
        VisualElement[] _tileIcons, _tileBadges;
        string[] _tileIconKey;
        int _gw, _gh;
        // side
        readonly VisualElement _tray;
        readonly VisualElement _bag;
        readonly Label _bagTitle;
        readonly Button _discardBtn;
        readonly SlotDragger _bagDrag;
        readonly ConfirmButton _flee;
        int _selectedBagSlot = -1;

        public HollowScreen(GameUI ui) : base(ui, "hollow")
        {
            // ───── HUD ─────
            var hud = UIKit.E("header", "header--hollow");
            _hudHost = hud;
            var left = UIKit.E("hud__left");
            var routeRow = UIKit.E("hud__route");
            _routeIcon = UIKit.E("hud__route-icon");
            _route = UIKit.L("", "header__title");
            routeRow.Add(_routeIcon);
            routeRow.Add(_route);
            left.Add(routeRow);
            _hp = new Bar("bar--hp");
            _hp.AddToClassList("hud__hp");
            left.Add(_hp);
            hud.Add(left);

            var mid = UIKit.E("hud__mid");
            var statLine = UIKit.E("hud__stats");
            statLine.Add(Art.IconText("damage", "🗡️", "", out _statDmg, "hud__stat"));
            statLine.Add(Art.IconText("armour", "🛡️", "", out _statArmour, "hud__stat"));
            statLine.Add(Art.IconText("crit", "💥", "", out _statCrit, "hud__stat"));
            _statCoinRow = Art.IconText("coinpct", "🪙", "", out _statCoin, "hud__stat");
            statLine.Add(_statCoinRow);
            _synergies = UIKit.E("synergy-row");
            mid.Add(statLine);
            mid.Add(_synergies);
            hud.Add(mid);

            var right = UIKit.E("header__info");
            right.Add(Art.Chip("coin", "🪙", out _coins, "chip--coins"));
            right.Add(Art.Chip("xp", "✨", out _xp, "chip--xp"));
            right.Add(Art.Chip("tile_start", "🚪", out _rooms));
            right.Add(Art.Chip("skull", "💀", out _kills));
            _flee = new ConfirmButton("Flee home", "Really flee? (no clear bonus)", () => S.FleeHome(), "btn--small", "btn--danger", "btn--flee");
            _flee.SetIcon(Art.Icon("action_flee", "🏃", "ico", "btn__icon"));
            right.Add(_flee.Button);
            hud.Add(right);
            Root.Add(hud);

            // ───── body ─────
            var body = UIKit.E("hollow__body");
            var gridWrap = UIKit.E("grid-wrap");
            _grid = UIKit.E("grid");
            gridWrap.Add(_grid);
            var hint = UIKit.E("grid-hint");
            hint.Add(UIKit.L("Click a glowing tile or use arrows / WASD to move.", "muted"));
            hint.Add(Art.IconText("tile_exit", "🌀", "= way out", "muted"));
            gridWrap.Add(hint);
            body.Add(gridWrap);

            var side = UIKit.E("hollow__side");
            var trayPanel = UIKit.E("panel", "panel--tray");
            var trayTitle = UIKit.E("panel__title", "ribbon");
            trayTitle.Add(Art.Icon("cook", "🍽️", "ico", "panel__icon"));
            trayTitle.Add(UIKit.L("Eaten before setting out", "panel__title-text"));
            trayPanel.Add(trayTitle);
            _tray = UIKit.E("meal-list");
            trayPanel.Add(_tray);
            side.Add(trayPanel);

            var bagPanel = UIKit.E("panel", "panel--bag");
            var bagHead = UIKit.E("panel__head");
            var bagRibbon = UIKit.E("panel__title", "ribbon");
            bagRibbon.Add(Art.Icon("bag", "🎒", "ico", "panel__icon"));
            _bagTitle = UIKit.L("", "panel__title-text");
            bagRibbon.Add(_bagTitle);
            _discardBtn = UIKit.B("🗑️ Discard", DiscardSelected, "btn--small", "btn--ghost");
            bagHead.Add(bagRibbon);
            bagHead.Add(_discardBtn);
            bagPanel.Add(bagHead);
            bagPanel.Add(UIKit.L("Drag to merge or swap · right-click to discard", "muted", "small"));
            _bag = UIKit.E("bag-grid");
            bagPanel.Add(_bag);
            side.Add(bagPanel);
            body.Add(side);
            Root.Add(body);

            _bagDrag = new SlotDragger(Ui.DragLayer,
                i => (S.Run != null && i < S.Run.bag.Count && !Empty(S.Run.bag[i])) ? UIKit.IngEmoji(C, S.Run.bag[i].ingredientId) : null,
                (from, to) => { _selectedBagSlot = -1; S.BagMove(from, to); },
                i => { _selectedBagSlot = (_selectedBagSlot == i) ? -1 : i; RefreshBag(); },
                i => { if (S.Run != null && i < S.Run.bag.Count && !Empty(S.Run.bag[i])) { _selectedBagSlot = -1; S.BagDiscard(i); } });
        }

        static bool Empty(BagSlot s) { return s == null || string.IsNullOrEmpty(s.ingredientId) || s.count <= 0; }

        protected override void OnShow()
        {
            _flee.Reset();
            _selectedBagSlot = -1;
            _tiles = null; // force grid rebuild for a new run
        }

        // ───────────────────────── refresh ─────────────────────────
        public override void Refresh()
        {
            var run = S.Run;
            if (run == null) return;
            RefreshHud(run);
            RefreshGrid(run);
            RefreshTray(run);
            RefreshBag();
        }

        void RefreshHud(RunState run)
        {
            var st = S.Stats;
            int max = st != null ? st.maxHp : run.hp;
            var rd = C.Route(run.routeId);
            Art.Set(_routeIcon, Art.Pic(ArtLibrary.GetRoute(run.routeId), rd != null ? rd.emoji : "", "ico", "hud__route-pic"));
            _route.text = (rd != null ? rd.name : run.routeId) + (run.mod != RouteMod.None ? "  · " + Art.ModName(run.mod) : "") + (run.storm ? "  · storm" : "");
            _route.tooltip = run.mod != RouteMod.None ? UIKit.ModHint(run.mod) : null;
            _hp.Set(run.hp, max, run.hp + " / " + max);
            _hp.SetLow(max > 0 && run.hp < max * 0.3f);
            _coins.text = "+" + run.coinsEarned;
            _xp.text = "+" + run.killXp + " XP";
            _rooms.text = run.roomsCleared + " rooms";
            _kills.text = run.kills.ToString();
            if (st != null)
            {
                _statDmg.text = UIKit.Num(st.damage);
                _statArmour.text = UIKit.Pct(st.armourPct);
                _statCrit.text = UIKit.Pct(st.critChance);
                _statCoin.text = "×" + UIKit.Num(st.coinMult);
                UIKit.Show(_statCoinRow, st.coinMult > 1.001f);
            }
            _synergies.Clear();
            for (int i = 0; i < run.activeSynergyIds.Count; i++)
            {
                var syn = C.synergies.Find(x => x.id == run.activeSynergyIds[i]);
                if (syn == null) continue;
                var b = UIKit.L(syn.emoji + " " + syn.name, "badge", "badge--synergy");   // synergies have no pack art
                b.tooltip = syn.description;
                _synergies.Add(b);
            }
        }

        void BuildGrid(RunState run)
        {
            _grid.Clear();
            _gw = run.width; _gh = run.height;
            int n = _gw * _gh;
            _tiles = new VisualElement[n];
            _tileLabels = new Label[n];
            _tileAnims = new SpriteAnimView[n];
            _tileIcons = new VisualElement[n];
            _tileBadges = new VisualElement[n];
            _tileIconKey = new string[n];
            for (int y = 0; y < _gh; y++)
            {
                var row = UIKit.E("grid__row");
                for (int x = 0; x < _gw; x++)
                {
                    var t = UIKit.E("tile");
                    int i = y * _gw + x;
                    // static icon holder (gather / rest / exit / start / resolved marks)
                    var icon = UIKit.E("tile__icon");
                    icon.pickingMode = PickingMode.Ignore;
                    t.Add(icon);
                    // animated character (monster / elite / player) — one per tile, hidden unless used
                    var anim = new SpriteAnimView(null, "", "tile__anim");
                    UIKit.Show(anim, false);
                    t.Add(anim);
                    var l = UIKit.L("", "tile__label");
                    l.pickingMode = PickingMode.Ignore;
                    t.Add(l);
                    var badge = Art.Icon("elite", "👑", "ico", "tile__badge");
                    UIKit.Show(badge, false);
                    t.Add(badge);
                    int tx = x, ty = y;
                    t.RegisterCallback<ClickEvent>(e => TryMove(tx, ty));
                    row.Add(t);
                    _tiles[i] = t;
                    _tileLabels[i] = l;
                    _tileAnims[i] = anim;
                    _tileIcons[i] = icon;
                    _tileBadges[i] = badge;
                }
                _grid.Add(row);
            }
        }

        void RefreshGrid(RunState run)
        {
            if (_tiles == null || _gw != run.width || _gh != run.height) BuildGrid(run);
            var cls = C.Class(run.classId);
            string me = "🧑‍🍳";
            var meAnim = Art.ClassAnim(run.classId);
            bool canAct = S.Phase == Phase.Explore;
            for (int y = 0; y < _gh; y++)
            {
                for (int x = 0; x < _gw; x++)
                {
                    int i = y * _gw + x;
                    Tile tile = run.TileAt(x, y);
                    var el = _tiles[i];
                    var lbl = _tileLabels[i];
                    bool player = run.px == x && run.py == y;
                    bool fog = !tile.revealed;
                    bool canMove = canAct && S.CanMoveTo(x, y);

                    el.EnableInClassList("tile--fog", fog);
                    el.EnableInClassList("tile--revealed", !fog);
                    el.EnableInClassList("tile--resolved", !fog && tile.resolved && tile.type != TileType.Exit && !player);
                    el.EnableInClassList("tile--player", player);
                    el.EnableInClassList("tile--move", canMove);
                    el.EnableInClassList("tile--monster", !fog && !tile.resolved && tile.type == TileType.Monster);
                    el.EnableInClassList("tile--elite", !fog && !tile.resolved && tile.type == TileType.Elite);
                    el.EnableInClassList("tile--gather", !fog && !tile.resolved && tile.type == TileType.Gather);
                    el.EnableInClassList("tile--rest", !fog && !tile.resolved && tile.type == TileType.Rest);
                    el.EnableInClassList("tile--exit", !fog && tile.type == TileType.Exit);

                    var anim = _tileAnims[i];
                    bool elite = !fog && !player && !tile.resolved && tile.type == TileType.Elite;
                    UIKit.Show(_tileBadges[i], elite);

                    if (player)
                    {
                        lbl.text = "";
                        SetTileIcon(i, null, null);
                        anim.SetAnim(meAnim, me);
                        UIKit.Show(anim, true);
                        el.tooltip = cls != null ? cls.name + " (you)" : "You";
                        continue;
                    }
                    if (fog)
                    {
                        UIKit.Show(anim, false);
                        anim.SetAnim(null, "");
                        SetTileIcon(i, null, null);
                        lbl.text = canMove ? "?" : "";
                        el.tooltip = "Fog";
                        continue;
                    }
                    lbl.text = "";
                    bool liveMonster = !tile.resolved && (tile.type == TileType.Monster || tile.type == TileType.Elite);
                    if (liveMonster)
                    {
                        var m = C.Monster(tile.monsterId);
                        anim.SetAnim(ArtLibrary.GetMonster(tile.monsterId), m != null && !string.IsNullOrEmpty(m.emoji) ? m.emoji : "⚔️");
                        UIKit.Show(anim, true);
                        SetTileIcon(i, null, null);
                    }
                    else
                    {
                        UIKit.Show(anim, false);
                        anim.SetAnim(null, "");
                        string emoji;
                        string iconId = TileIconId(tile, out emoji);
                        SetTileIcon(i, iconId, emoji);
                    }
                    TileGlyph(tile, out string tip);
                    el.tooltip = tip;
                }
            }
        }

        /// Static icon for non-monster tiles (resolved ones show a faint mark).
        static string TileIconId(Tile t, out string emoji)
        {
            switch (t.type)
            {
                case TileType.Monster:
                case TileType.Elite: emoji = "✖"; return t.resolved ? "skull" : null;
                case TileType.Gather: emoji = t.resolved ? "·" : "🧺"; return t.resolved ? null : "tile_gather";
                case TileType.Rest: emoji = t.resolved ? "·" : "💤"; return t.resolved ? null : "tile_rest";
                case TileType.Exit: emoji = "🌀"; return "tile_exit";
                case TileType.Start: emoji = "🏕️"; return "tile_start";
            }
            emoji = "";
            return null;
        }

        /// Swaps the tile's static icon only when it changes (tiles are refreshed on every session change).
        void SetTileIcon(int i, string iconId, string emoji)
        {
            string key = (iconId ?? "") + "|" + (emoji ?? "");
            if (_tileIconKey[i] == key) return;
            _tileIconKey[i] = key;
            var holder = _tileIcons[i];
            holder.Clear();
            if (iconId == null && string.IsNullOrEmpty(emoji)) return;
            holder.Add(Art.Pic(iconId != null ? ArtLibrary.GetIcon(iconId) : null, emoji, "tile__pic"));
        }

        string TileGlyph(Tile t, out string tip)
        {
            switch (t.type)
            {
                case TileType.Monster:
                case TileType.Elite:
                {
                    var m = C.Monster(t.monsterId);
                    string name = m != null ? m.name : "Monster";
                    if (t.resolved) { tip = name + " (defeated)"; return "✖"; }
                    bool elite = t.type == TileType.Elite;
                    tip = (elite ? "👑 Elite " : "") + name + " · difficulty " + t.difficulty;
                    string e = m != null && !string.IsNullOrEmpty(m.emoji) ? m.emoji : "⚔️";
                    return elite ? "👑" + e : e;
                }
                case TileType.Gather: tip = t.resolved ? "Picked clean" : "Gathering spot"; return t.resolved ? "·" : "🧺";
                case TileType.Rest: tip = t.resolved ? "Embers" : "Campfire rest"; return t.resolved ? "·" : "💤";
                case TileType.Exit: tip = "Exit portal — step in to clear the route"; return "🌀";
                case TileType.Start: tip = "Where you came in"; return "🏕️";
            }
            tip = "Empty clearing";
            return "";
        }

        void RefreshTray(RunState run)
        {
            // Read-only: the tray was eaten at camp; its buffs last the whole run.
            _tray.Clear();
            var cls = C.Class(run.classId);
            if (run.eaten.Count == 0)
            {
                _tray.Add(UIKit.L("No meals today — base stats only.", "muted", "small"));
                return;
            }
            for (int i = 0; i < run.eaten.Count; i++)
            {
                var m = run.eaten[i];
                var r = C.Recipe(m.recipeId);
                var row = UIKit.E("meal-row");
                row.Add(Art.Framed(Art.Pic(ArtLibrary.GetRecipe(m.recipeId), r != null ? r.emoji : "🍽️", "meal-row__emoji"), "item-frame--sm"));
                var txt = UIKit.E("meal-row__text");
                txt.Add(UIKit.L((r != null ? r.name : m.recipeId) + (m.potency < 0.99f ? "  (×" + UIKit.Num(m.potency) + ")" : ""), "meal-row__name"));
                if (r != null) txt.Add(UIKit.L(UIKit.BuffSummary(Cooking.MealBuffs(C, cls, m)), "meal-row__buffs"));
                row.Add(txt);
                _tray.Add(row);
            }
        }

        void RefreshBag()
        {
            var run = S.Run;
            if (run == null) return;
            _bag.Clear();
            _bagDrag.Clear();
            int used = 0;
            for (int i = 0; i < run.bag.Count; i++) if (!Empty(run.bag[i])) used++;
            _bagTitle.text = "Bag " + used + "/" + run.bag.Count;
            if (_selectedBagSlot >= run.bag.Count || (_selectedBagSlot >= 0 && Empty(run.bag[_selectedBagSlot]))) _selectedBagSlot = -1;

            for (int i = 0; i < run.bag.Count; i++)
            {
                BagSlot b = run.bag[i];
                var slot = UIKit.E("slot", "bag-slot");
                if (!Empty(b))
                {
                    slot.AddToClassList("slot--filled");
                    slot.EnableInClassList("slot--full-stack", b.count >= C.tuning.stackSize);
                    slot.Add(Art.Pic(ArtLibrary.GetIngredient(b.ingredientId), UIKit.IngEmoji(C, b.ingredientId), "slot__emoji"));
                    slot.Add(UIKit.L(b.count.ToString(), "slot__count"));
                    slot.tooltip = UIKit.IngName(C, b.ingredientId) + " ×" + b.count;
                }
                else slot.AddToClassList("slot--empty");
                slot.EnableInClassList("slot--selected", i == _selectedBagSlot);
                foreach (var child in slot.Children()) child.pickingMode = PickingMode.Ignore;
                _bag.Add(slot);
                _bagDrag.Register(slot);
            }
            _discardBtn.SetEnabled(_selectedBagSlot >= 0);
        }

        void DiscardSelected()
        {
            if (_selectedBagSlot < 0) return;
            int i = _selectedBagSlot;
            _selectedBagSlot = -1;
            S.BagDiscard(i);
        }

        void TryMove(int x, int y)
        {
            if (S.Phase != Phase.Explore) return;
            if (!S.CanMoveTo(x, y))
            {
                if (S.Run != null && (S.Run.px != x || S.Run.py != y))
                    Juice.Flash(_tiles[y * _gw + x], "tile--nope", 200);
                return;
            }
            S.MoveTo(x, y);
        }

        // ───────────────────────── juice ─────────────────────────
        public override void OnGameEvent(GameEvent e)
        {
            if (S.Phase == Phase.Fight) return; // fight overlay handles combat numbers
            if (e.type == GameEventType.PlayerHeal && e.amount > 0) Juice.Float(_hudHost, "+" + Mathf.RoundToInt(e.amount), "float-text--heal");
            else if (e.type == GameEventType.Coins && e.amount > 0) Juice.Float(_hudHost, "+" + Mathf.RoundToInt(e.amount) + " 🪙", "float-text--coins");
        }

        public override bool HandleKey(KeyCode key)
        {
            var run = S.Run;
            if (run == null || S.Phase != Phase.Explore) return false;
            int dx = 0, dy = 0;
            if (key == KeyCode.LeftArrow || key == KeyCode.A) dx = -1;
            else if (key == KeyCode.RightArrow || key == KeyCode.D) dx = 1;
            else if (key == KeyCode.UpArrow || key == KeyCode.W) dy = -1;   // row 0 is drawn at the top
            else if (key == KeyCode.DownArrow || key == KeyCode.S) dy = 1;
            else return false;
            int nx = run.px + dx, ny = run.py + dy;
            if (run.InBounds(nx, ny)) TryMove(nx, ny);
            return true;
        }
    }
}
