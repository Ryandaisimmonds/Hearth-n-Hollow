// Hearth & Hollow — the HUD while walking around Hearth's Hollow (town preview): the town's name, day and coins,
// your class + key stats, a name label over every spot, and the "E — Cook" prompt for whatever you're next to.
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Town;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class TownHud : UIScreen
    {
        public static TownHud Instance { get; private set; }

        readonly Label _day, _coins, _class, _stats, _prompt, _weather;
        readonly VisualElement _labels;
        readonly Label _objective;
        readonly Dictionary<string, Label> _spotLabels = new Dictionary<string, Label>();
        string _promptText;

        public string Prompt
        {
            get { return _promptText; }
            set { _promptText = value; _prompt.text = string.IsNullOrEmpty(value) ? "" : "E  ·  " + value; UIKit.Show(_prompt, !string.IsNullOrEmpty(value)); }
        }

        public TownHud(GameUI ui) : base(ui, "town-hud")
        {
            Instance = this;
            Root.pickingMode = PickingMode.Ignore;
            _labels = UIKit.E("town-hud__labels");
            _labels.pickingMode = PickingMode.Ignore;
            Root.Add(_labels);

            var top = UIKit.E("panel", "town-hud__top");
            top.Add(UIKit.L("🏘️ Hearth's Hollow", "town-hud__title"));
            top.Add(UIKit.L("preview", "town-hud__tag"));
            _day = UIKit.L("", "town-hud__chip");
            _coins = UIKit.L("", "town-hud__chip");
            _weather = UIKit.L("", "town-hud__weather");
            top.Add(_day); top.Add(_weather); top.Add(_coins);
            Root.Add(top);

            var who = UIKit.E("panel", "town-hud__who");
            _class = UIKit.L("", "town-hud__class");
            _stats = UIKit.L("", "town-hud__stats");
            who.Add(_class); who.Add(_stats);
            Root.Add(who);

            _objective = UIKit.L("", "panel", "town-hud__objective");   // ticket 35: what Old Wren wants you to do next
            _objective.pickingMode = PickingMode.Ignore;
            Root.Add(_objective);

            _prompt = UIKit.L("", "town-hud__prompt");
            Root.Add(_prompt);
            UIKit.Show(_prompt, false);

            var help = UIKit.L("WASD / stick: walk  ·  E / (Y): use  ·  Esc: settings", "town-hud__help");
            Root.Add(help);
        }

        public override void Refresh()
        {
            var p = S.Profile;
            _day.text = "☀️ Day " + p.day;
            _coins.text = "🪙 " + p.coins;
            var ev = S.Hearth != null ? S.Hearth.campEvent : CampEvent.None;
            _weather.text = ev == CampEvent.Storm ? "🌧️ Stormy" : ev == CampEvent.HarvestMoon ? "🌕 Harvest moon" : ev == CampEvent.Merchant ? "🛒 Market day" : ev == CampEvent.Armourer ? "🛡️ Armourer in town" : ev == CampEvent.Forager ? "🧺 Forager's basket" : "☀️ Calm";
            var cls = C.Class(p.selectedClass);
            var cp = p.Progress(p.selectedClass);
            _class.text = cls != null ? cls.emoji + " " + cls.name : p.selectedClass.ToString();
            _objective.text = "📌 " + S.WrenObjective;
            UIKit.Show(_objective, Slice.On);
            var st = S.Stats;
            _stats.text = st == null ? "" : "❤️ " + st.maxHp + "   🗡️ " + UIKit.Num(st.damage) + "   🛡️ " + UIKit.Pct(st.armourPct) + "   💥 " + UIKit.Pct(st.critChance);
        }

        /// Name labels over each spot (the near one highlighted). Called every frame by the TownController.
        public void PlaceLabels(Camera cam, List<TownSpot> spots, TownSpot near, Vector3 player)
        {
            if (!Visible || spots == null || Root.panel == null) return;
            foreach (var s in spots)
            {
                Label l;
                if (!_spotLabels.TryGetValue(s.id, out l))
                {
                    l = UIKit.L(s.emoji + " " + s.name, "town-label");
                    l.pickingMode = PickingMode.Ignore;
                    _labels.Add(l);
                    _spotLabels[s.id] = l;
                }
                string want = s.emoji + " " + s.name;
                if (l.text != want) l.text = want;   // a spot can be renamed (e.g. the Lamp-maker once built)
                float top = s.action == TownAction.Wren ? 1.6f : Mathf.Max(0.8f, s.size.y) + (s.action == TownAction.Hearth ? 3.2f : 0.4f);   // above the tallest (end-of-story) fire
                Vector2 pp = RuntimePanelUtils.CameraTransformWorldToPanel(Root.panel, (Vector3)s.pos + new Vector3(0, top, 0), cam);
                l.style.left = pp.x; l.style.top = pp.y;
                l.EnableInClassList("town-label--near", s == near);
                // far-away labels fade so the screen stays calm
                float d = Vector2.Distance(s.pos, player);
                l.style.opacity = s == near ? 1f : Mathf.Clamp01(1.4f - d / 10f);
            }
        }

        public override bool HandleKey(KeyCode key) { return false; }
    }
}
