// Hearth & Hollow — the Lamp-maker in town (ticket 17): the slice's one building you choose to build, on the
// north-east fenced plot. Talk at the plot's gate to build or upgrade it (coins + Meadow timber + Ridge ember coal).
// Each stage draws a bigger building with more lamps; its effects live in Core/LampMaker.cs.
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Dungeon;
using UnityEngine;

namespace HearthHollow.Town
{
    public partial class TownController
    {
        const string LampPlotId = "plot_ne";
        static readonly Vector2 LampSite = new Vector2(42.5f, 32.6f);   // inside the NE plot, behind its fence

        /// The NE plot becomes the Lamp-maker's spot (built or not).
        void SetupLampMakerSpot()
        {
            var spot = Spots.Find(s => s.id == LampPlotId);
            if (spot == null) return;
            var p = Session.Profile;
            var cur = LampMaker.Current(Session.Content, p);
            spot.action = TownAction.LampMaker;
            spot.emoji = "🏮";
            spot.name = cur != null ? cur.name : "Empty plot";
            spot.verb = cur == null ? "Build the Lamp-maker" : LampMaker.Next(Session.Content, p) != null ? "Upgrade the Lamp-maker" : "Visit the Lamp-maker";
        }

        void TalkToLampMaker()
        {
            var C = Session.Content; var p = Session.Profile;
            var cur = LampMaker.Current(C, p); var next = LampMaker.Next(C, p);
            var pages = new List<string>();
            if (cur == null)
                pages.Add("An empty plot behind a fence. Old Wren says a lamp-maker used to work here, before the fog. Build her a place and she'll come back: a better lantern means seeing further down there, and a bigger, quicker Flare.");
            else
                pages.Add("🏮 " + cur.name + ": " + LampMaker.EffectText(cur) + ".\n\n" + cur.description);
            string action = null; System.Action onAction = null;
            if (next != null)
            {
                string why;
                bool can = LampMaker.CanBuild(C, p, out why);
                pages.Add((cur == null ? "Build the " : "Next: the ") + next.name + " — " + LampMaker.EffectText(next) + ".\n\nCosts " + LampMaker.CostText(C, next) + "."
                          + (can ? "" : "\n\n" + why + "." + Have(next)));
                if (can)
                {
                    action = "🔨 Build (" + LampMaker.CostText(C, next) + ")";
                    onAction = () => { string r; if (Session.BuildLampMaker(out r)) { if (_ui != null) _ui.Toast("🏮 The " + LampMaker.Current(C, p).name + " is built!"); } else if (_ui != null) _ui.Toast(r); };
                }
            }
            else pages.Add("The lamp on top of the workshop shines all the way to the gate. There's nothing left to build here.");
            _ui.OpenDialogue("Lamp-maker", "🏮", pages, action, onAction);
        }

        string Have(LampStage s)
        {
            var parts = new List<string>();
            if (s.materials != null)
                foreach (var m in s.materials)
                {
                    var ing = Session.Content.Ingredient(m.ingredientId);
                    parts.Add((ing != null ? ing.emoji + " " + ing.name : m.ingredientId) + " " + Economy.PantryCount(Session.Profile, m.ingredientId) + "/" + m.count);
                }
            return parts.Count > 0 ? "\nYou have: " + string.Join(" · ", parts.ToArray()) + " · 🪙 " + Session.Profile.coins + "/" + s.price : "";
        }

        /// The building itself, bigger and brighter each stage.
        void BuildLampMakerArt()
        {
            int stage = LampMaker.Stage(Session.Profile);
            if (stage <= 0) return;
            var root = Node("Lamp-maker", LampSite, _root.transform);
            int o = O(LampSite.y);
            Color warm = new Color(1f, 0.82f, 0.45f), glass = new Color(1f, 0.92f, 0.65f);
            float w = stage >= 2 ? 3.6f : 2.4f, h = stage >= 2 ? 2.2f : 1.3f;
            // walls and roof
            Box(root, new Vector2(0, h * 0.5f), new Vector2(w, h), stage >= 2 ? Stone : WoodLight, o);
            Box(root, new Vector2(0, h + 0.25f), new Vector2(w + 0.4f, 0.5f), Clay, o + 1);
            Box(root, new Vector2(0, 0.5f), new Vector2(0.6f, 1f), Wood, o + 2);   // door
            // a counter of lanterns out front
            Box(root, new Vector2(0, -0.35f), new Vector2(w * 0.8f, 0.3f), Wood, o + 3);
            int lamps = 2 + stage;
            for (int i = 0; i < lamps; i++)
            {
                float x = -w * 0.35f + i * (w * 0.7f / Mathf.Max(1, lamps - 1));
                Dot(root, new Vector2(x, -0.1f), 0.22f, glass, o + 4);
            }
            Glow(root, new Vector2(0, 0.2f), warm, 2.5f + stage, 0.7f + 0.15f * stage);
            if (stage >= 2)   // the glass kiln
            {
                Box(root, new Vector2(w * 0.5f + 0.5f, 0.6f), new Vector2(0.8f, 1.2f), StoneDark, o + 1);
                var kiln = Node("Kiln fire", LampSite + new Vector2(w * 0.5f + 0.5f, 0.4f), root);
                kiln.gameObject.AddComponent<TownAmbient>().Setup(AmbientKind.Forge, Glow(kiln, Vector2.zero, new Color(1f, 0.55f, 0.2f), 2f, 0.9f));
            }
            if (stage >= 3)   // the lighthouse lamp on top
            {
                Box(root, new Vector2(0, h + 1.2f), new Vector2(0.7f, 1.5f), Stone, o + 2);
                Dot(root, new Vector2(0, h + 2.1f), 0.55f, glass, o + 3);
                var top = Node("Lighthouse lamp", LampSite + new Vector2(0, h + 2.1f), root);
                top.gameObject.AddComponent<TownAmbient>().Setup(AmbientKind.Flicker, Glow(top, Vector2.zero, warm, 7f, 1.2f));
            }
        }
    }
}
