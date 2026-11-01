// Hearth & Hollow — the town's work areas, filled out (1 Nov): props built from simple shapes (the Cainos pack has no
// anvils or workbenches), looping effects (TownAmbient) and a worker at each (TownWorker, Ninja Adventure art).
//   Smithy (42,25)     anvil + smith, weapon rack, coal pile, quench barrel, forge sparks and smoke
//   Workshop (42,15)   workbench + carpenter, timber stack, sawhorse, sawdust, a lantern
//   Cooking (20,24)    prep table with veg, spice rack, sacks, water barrel, steam off the pot + the cook stirring it
//   Training (7,20)    straw dummies (TownDummy: your swing rocks them), a target, a weapon rack + the sword-master sparring
using HearthHollow.Data;
using HearthHollow.Dungeon;
using UnityEngine;

namespace HearthHollow.Town
{
    public partial class TownController
    {
        static int O(float y) { return DungeonController.SortOrder(y); }

        Transform Node(string name, Vector2 world, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(world.x, world.y, 0);
            return go.transform;
        }

        /// Toast a townsperson's line.
        public void Say(string who, string line) { if (_ui != null) _ui.Toast("💬 " + who + ": \"" + line + "\""); }

        TownWorker Worker(Transform parent, string art, string fallbackArt, string who, Vector2 at, Facing face, WorkKind kind, Vector2 work, string[] lines, int seed)
        {
            var go = new GameObject("Worker · " + who);
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            string useArt = ArtLibrary.GetClass(art) != null ? art : fallbackArt;
            var an = Person(go, useArt, Color.white, at);
            var w = go.AddComponent<TownWorker>();
            w.Setup(this, an, kind, work, face, who, lines, seed);
            return w;
        }

        void BuildWorkAreas()
        {
            var root = new GameObject("Work Areas").transform;
            root.SetParent(_root.transform, false);
            Color fire = new Color(1f, 0.55f, 0.2f), steel = new Color(0.72f, 0.74f, 0.78f), hilt = new Color(0.45f, 0.28f, 0.15f);

            // ═══ Smithy ═══
            {
                var forge = Node("Forge fire", new Vector2(42f, 25.6f), root);
                forge.gameObject.AddComponent<TownAmbient>().Setup(AmbientKind.Forge, Glow(forge, Vector2.zero, fire, 3.2f, 1.1f));
                var chimney = Node("Forge smoke", new Vector2(42.3f, 27.2f), root);
                chimney.gameObject.AddComponent<TownAmbient>().Setup(AmbientKind.Smoke);
                // anvil
                var anvil = Node("Anvil", new Vector2(45.3f, 24.3f), root); int ao = O(24.3f);
                Box(anvil, new Vector2(0, 0.17f), new Vector2(0.34f, 0.34f), StoneDark, ao);
                Box(anvil, new Vector2(0, 0.4f), new Vector2(0.75f, 0.16f), Iron, ao + 1);
                Box(anvil, new Vector2(0.42f, 0.42f), new Vector2(0.2f, 0.08f), Iron, ao + 1);
                Box(anvil, new Vector2(-0.05f, 0.5f), new Vector2(0.35f, 0.05f), new Color(1f, 0.5f, 0.15f), ao + 2);   // a glowing bar on it
                // weapon rack
                var rack = Node("Weapon rack", new Vector2(39.1f, 25.3f), root); int ro = O(25.3f);
                Box(rack, new Vector2(-0.6f, 0.6f), new Vector2(0.1f, 1.2f), Wood, ro); Box(rack, new Vector2(0.6f, 0.6f), new Vector2(0.1f, 1.2f), Wood, ro);
                Box(rack, new Vector2(0, 1.05f), new Vector2(1.35f, 0.09f), Wood, ro + 1); Box(rack, new Vector2(0, 0.35f), new Vector2(1.35f, 0.09f), Wood, ro + 1);
                for (int i = 0; i < 3; i++)
                {
                    float x = -0.35f + i * 0.35f;
                    Box(rack, new Vector2(x, 0.72f), new Vector2(0.07f, 0.85f), steel, ro + 2);
                    Box(rack, new Vector2(x, 0.25f), new Vector2(0.2f, 0.06f), hilt, ro + 3);
                }
                Dot(rack, new Vector2(0.78f, 0.55f), 0.45f, new Color(0.55f, 0.4f, 0.25f), ro + 3); Dot(rack, new Vector2(0.78f, 0.55f), 0.15f, steel, ro + 4);   // a round shield
                // coal pile with embers
                var coal = Node("Coal", new Vector2(39.7f, 23.9f), root); int co = O(23.9f);
                for (int i = 0; i < 7; i++) Dot(coal, new Vector2(-0.35f + (i % 4) * 0.23f, 0.08f + (i / 4) * 0.16f), 0.26f, new Color(0.13f, 0.12f, 0.13f), co);
                for (int i = 0; i < 3; i++) Dot(coal, new Vector2(-0.2f + i * 0.2f, 0.15f), 0.08f, fire, co + 1);
                Glow(coal, new Vector2(0, 0.2f), fire, 1.2f, 0.5f);
                Prop("Barrel 01", new Vector2(46.5f, 25.0f), root, 0.9f, false);   // quench barrel
                Box(Node("Water", new Vector2(46.5f, 25.0f), root), new Vector2(0, 0.62f), new Vector2(0.55f, 0.08f), new Color(0.3f, 0.5f, 0.75f), O(25f) + 3);
                Worker(root, "TownSmith", "Warden", "Smith", new Vector2(44.6f, 24.25f), Facing.Right, WorkKind.Hammer, new Vector2(45.3f, 24.8f),
                    new[] { "Bring me iron scrap and coin and I'll make that gear sing.", "Mind the sparks, they've a mind of their own.", "Good steel remembers every blow." }, 11);
            }

            // ═══ Workshop ═══
            {
                var bench = Node("Workbench", new Vector2(45.4f, 14.5f), root); int bo = O(14.5f);
                Box(bench, new Vector2(-0.55f, 0.25f), new Vector2(0.1f, 0.5f), Wood, bo); Box(bench, new Vector2(0.55f, 0.25f), new Vector2(0.1f, 0.5f), Wood, bo);
                Box(bench, new Vector2(0, 0.52f), new Vector2(1.35f, 0.14f), WoodLight, bo + 1);
                Box(bench, new Vector2(-0.25f, 0.63f), new Vector2(0.55f, 0.07f), new Color(0.85f, 0.7f, 0.5f), bo + 2);    // a plank on it
                Box(bench, new Vector2(0.35f, 0.66f), new Vector2(0.35f, 0.05f), steel, bo + 2, 10f); Box(bench, new Vector2(0.55f, 0.7f), new Vector2(0.12f, 0.1f), hilt, bo + 3, 10f);   // saw
                for (int i = 0; i < 5; i++) Dot(bench, new Vector2(-0.6f + i * 0.3f, 0.02f), 0.14f, new Color(0.9f, 0.8f, 0.55f, 0.8f), bo - 1);   // sawdust
                var lamp = Node("Workshop lantern", new Vector2(46.3f, 15.6f), root);
                Box(lamp, new Vector2(0, 0.5f), new Vector2(0.08f, 1f), Wood, O(15.6f)); Dot(lamp, new Vector2(0, 1.05f), 0.22f, new Color(1f, 0.85f, 0.5f), O(15.6f) + 1);
                lamp.gameObject.AddComponent<TownAmbient>().Setup(AmbientKind.Flicker, Glow(lamp, new Vector2(0, 1.05f), new Color(1f, 0.75f, 0.45f), 2.6f, 0.8f));
                var timber = Node("Timber", new Vector2(39.1f, 15.2f), root); int to = O(15.2f);
                for (int row = 0; row < 3; row++)
                    for (int i = 0; i < 3 - row; i++)
                    {
                        Box(timber, new Vector2(-0.3f + i * 0.3f + row * 0.15f, 0.13f + row * 0.24f), new Vector2(0.95f, 0.22f), i % 2 == 0 ? Wood : WoodLight, to + row);
                        Dot(timber, new Vector2(-0.3f + i * 0.3f + row * 0.15f + 0.47f, 0.13f + row * 0.24f), 0.2f, new Color(0.85f, 0.68f, 0.45f), to + row + 1);   // cut ends
                    }
                var horse = Node("Sawhorse", new Vector2(39.7f, 13.6f), root); int ho = O(13.6f);
                Box(horse, new Vector2(-0.35f, 0.2f), new Vector2(0.08f, 0.45f), Wood, ho, 12f); Box(horse, new Vector2(0.35f, 0.2f), new Vector2(0.08f, 0.45f), Wood, ho, -12f);
                Box(horse, new Vector2(0, 0.42f), new Vector2(1.2f, 0.12f), WoodLight, ho + 1);
                Worker(root, "TownCarpenter", "Ranger", "Carpenter", new Vector2(44.55f, 14.45f), Facing.Right, WorkKind.Carve, new Vector2(45.3f, 15.05f),
                    new[] { "Every shelf in your kitchen came off this bench.", "Measure twice, cut once. Then cut again, because I measured wrong.", "Got timber? I've got ideas." }, 23);
            }

            // ═══ Cooking area ═══
            {
                var steam = Node("Pot steam", new Vector2(19f, 24.95f), root);
                steam.gameObject.AddComponent<TownAmbient>().Setup(AmbientKind.Steam);
                var table = Node("Prep table", new Vector2(22.95f, 23.7f), root); int po = O(23.7f);
                Box(table, new Vector2(-0.5f, 0.22f), new Vector2(0.09f, 0.44f), Wood, po); Box(table, new Vector2(0.5f, 0.22f), new Vector2(0.09f, 0.44f), Wood, po);
                Box(table, new Vector2(0, 0.47f), new Vector2(1.2f, 0.13f), WoodLight, po + 1);
                Box(table, new Vector2(-0.2f, 0.57f), new Vector2(0.45f, 0.07f), new Color(0.85f, 0.72f, 0.5f), po + 2);   // chopping board
                Color[] veg = { new Color(0.4f, 0.75f, 0.3f), new Color(1f, 0.55f, 0.15f), new Color(0.85f, 0.2f, 0.2f), new Color(0.95f, 0.85f, 0.4f) };
                for (int i = 0; i < 4; i++) Dot(table, new Vector2(0.15f + i * 0.13f, 0.62f + (i % 2) * 0.04f), 0.15f, veg[i], po + 3);
                Box(table, new Vector2(-0.15f, 0.63f), new Vector2(0.28f, 0.04f), steel, po + 3, -8f);   // knife
                var spice = Node("Spice rack", new Vector2(17.0f, 24.5f), root); int so = O(24.5f);
                Box(spice, new Vector2(0, 0.7f), new Vector2(1.1f, 1.3f), Wood * 0.8f, so);
                Color[] jars = { new Color(0.9f, 0.3f, 0.2f), new Color(0.95f, 0.75f, 0.2f), new Color(0.4f, 0.7f, 0.35f), new Color(0.6f, 0.4f, 0.25f), new Color(0.95f, 0.95f, 0.9f), new Color(0.7f, 0.35f, 0.6f) };
                for (int r = 0; r < 2; r++) { Box(spice, new Vector2(0, 0.3f + r * 0.55f), new Vector2(1.05f, 0.06f), WoodLight, so + 1); for (int i = 0; i < 3; i++) Dot(spice, new Vector2(-0.3f + i * 0.3f, 0.45f + r * 0.55f), 0.2f, jars[r * 3 + i], so + 2); }
                for (int i = 0; i < 3; i++) Box(spice, new Vector2(-0.35f + i * 0.35f, 1.45f), new Vector2(0.12f, 0.3f), new Color(0.45f, 0.65f, 0.3f), so + 2);   // herbs drying
                var sacks = Node("Sacks", new Vector2(16.9f, 23.1f), root); int sk = O(23.1f);
                Dot(sacks, new Vector2(-0.22f, 0.3f), 0.55f, new Color(0.82f, 0.72f, 0.52f), sk); Dot(sacks, new Vector2(0.28f, 0.25f), 0.48f, new Color(0.78f, 0.67f, 0.48f), sk + 1);
                Box(sacks, new Vector2(-0.22f, 0.55f), new Vector2(0.18f, 0.05f), Wood, sk + 2);
                Prop("Barrel 01", new Vector2(24.1f, 24.3f), root, 0.9f, false);   // water barrel
                Worker(root, "TownCook", "Cook", "Head cook", new Vector2(19f, 23.35f), Facing.Up, WorkKind.Stir, new Vector2(19f, 24.95f),
                    new[] { "Taste this! ...Hm. Needs salt. Everything needs salt.", "A good meal's half the battle down there.", "Fresh ingredients make the best grades, you know." }, 37);
            }

            // ═══ Training yard ═══
            {
                Vector2[] dummies = { new Vector2(3.4f, 19.4f), new Vector2(10.7f, 19.3f) };
                foreach (var dp in dummies)
                {
                    var d = Node("Training dummy", dp, root); int dO = O(dp.y);
                    var pivot = new GameObject("Pivot").transform; pivot.SetParent(d, false);
                    Box(pivot, new Vector2(0, 0.45f), new Vector2(0.1f, 0.9f), Wood, dO);
                    Box(pivot, new Vector2(0, 0.8f), new Vector2(0.75f, 0.1f), Wood, dO + 1);                        // arms
                    Dot(pivot, new Vector2(0, 0.85f), 0.5f, new Color(0.9f, 0.78f, 0.42f), dO + 2);                   // straw body
                    Dot(pivot, new Vector2(0, 1.28f), 0.32f, new Color(0.85f, 0.72f, 0.4f), dO + 2);                  // head
                    Box(pivot, new Vector2(0, 0.85f), new Vector2(0.52f, 0.06f), new Color(0.6f, 0.25f, 0.2f), dO + 3);   // a red sash
                    d.gameObject.AddComponent<TownDummy>().Setup(pivot);
                }
                var target = Node("Archery target", new Vector2(7f, 24.4f), root); int tO = O(24.4f);
                Box(target, new Vector2(-0.3f, 0.35f), new Vector2(0.08f, 0.7f), Wood, tO, 10f); Box(target, new Vector2(0.3f, 0.35f), new Vector2(0.08f, 0.7f), Wood, tO, -10f);
                Dot(target, new Vector2(0, 0.75f), 0.85f, new Color(0.95f, 0.92f, 0.85f), tO + 1); Dot(target, new Vector2(0, 0.75f), 0.6f, new Color(0.8f, 0.2f, 0.2f), tO + 2);
                Dot(target, new Vector2(0, 0.75f), 0.36f, new Color(0.95f, 0.92f, 0.85f), tO + 3); Dot(target, new Vector2(0, 0.75f), 0.15f, new Color(0.8f, 0.2f, 0.2f), tO + 4);
                Box(target, new Vector2(0.12f, 0.82f), new Vector2(0.4f, 0.04f), hilt, tO + 5, 20f);   // an arrow in it
                var wrack = Node("Practice weapons", new Vector2(2.4f, 21.6f), root); int wO = O(21.6f);
                Box(wrack, new Vector2(0, 0.08f), new Vector2(0.9f, 0.12f), Wood, wO);
                for (int i = 0; i < 4; i++) Box(wrack, new Vector2(-0.3f + i * 0.2f, 0.55f), new Vector2(0.07f, 0.95f), i % 2 == 0 ? WoodLight : Wood, wO + 1, -6f + i * 4f);   // wooden swords and staves
                Worker(root, "TownTrainer", "Warden", "Sword-master", new Vector2(9.95f, 19.25f), Facing.Right, WorkKind.Spar, dummies[1],
                    new[] { "Hit the dummy, not me! Have a go at the other one.", "Footwork first. Then fireworks.", "Every class fights differently. Talk to them to swap." }, 41);
            }
        }
    }
}
