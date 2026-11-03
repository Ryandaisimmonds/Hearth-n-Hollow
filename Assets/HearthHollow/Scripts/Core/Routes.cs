// Hearth & Hollow — route gates, the story's progress and dungeon hazards. Pure C#.
// A route can be ENTERED when: its previous route is cleared, you own its key item (if any), and — for class-only
// dungeons — you're playing that class. Once cleared a route stays open (old saves keep their progress).
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public static class Routes
    {
        public static KeyItemData KeyItem(GameContent c, string id)
        {
            return string.IsNullOrEmpty(id) || c.keyItems == null ? null : c.keyItems.Find(k => k.id == id);
        }

        public static string KeyItemLabel(GameContent c, string id)
        {
            var k = KeyItem(c, id);
            return k != null ? k.emoji + " " + k.name : id;
        }

        public static bool HasKey(Profile p, string id)
        {
            return string.IsNullOrEmpty(id) || (p.keyItems != null && p.keyItems.Contains(id));
        }

        /// The route that gives this key item when cleared.
        public static RouteData KeySource(GameContent c, string keyId)
        {
            return string.IsNullOrEmpty(keyId) ? null : c.routes.Find(r => r.rewardKeyItem == keyId);
        }

        public static bool Cleared(Profile p, string routeId) { return p.clearedRouteIds.Contains(routeId); }

        public static bool HasFlag(Profile p, string flag) { return string.IsNullOrEmpty(flag) || (p != null && p.storyFlags != null && p.storyFlags.Contains(flag)); }

        /// Set a story flag (true the first time).
        public static bool SetFlag(Profile p, string flag)
        {
            if (p == null || string.IsNullOrEmpty(flag)) return false;
            if (p.storyFlags == null) p.storyFlags = new List<string>();
            if (p.storyFlags.Contains(flag)) return false;
            p.storyFlags.Add(flag); return true;
        }

        /// The previous route is cleared (so the route shows on the board, even if a key item still blocks it).
        public static bool Discovered(GameContent c, Profile p, RouteData r)
        {
            if (r == null) return false;
            if (Cleared(p, r.id)) return true;
            if (!HasFlag(p, r.requiredFlag)) return false;   // e.g. Chili Ridge: Old Wren's directions first (ticket 14)
            return string.IsNullOrEmpty(r.unlockedByRouteId) || Cleared(p, r.unlockedByRouteId);
        }

        /// Open for any class: discovered and its key item owned (or already cleared once).
        public static bool IsUnlocked(GameContent c, Profile p, string routeId)
        {
            RouteData r = c.Route(routeId);
            if (r == null) return false;
            if (Cleared(p, r.id)) return true;
            return Discovered(c, p, r) && HasKey(p, r.requiredKeyItem);
        }

        /// Why the route can't be entered yet (null = it can, class aside).
        public static string LockReason(GameContent c, Profile p, RouteData r)
        {
            if (r == null) return "Unknown route";
            if (IsUnlocked(c, p, r.id)) return null;
            if (!HasFlag(p, r.requiredFlag)) return !string.IsNullOrEmpty(r.lockHint) ? r.lockHint : "Not found yet";
            if (!Discovered(c, p, r)) return "Clear " + RouteName(c, r.unlockedByRouteId) + " first";
            var src = KeySource(c, r.requiredKeyItem);
            return "Needs the " + KeyItemLabel(c, r.requiredKeyItem) + (src != null ? " — found in " + src.emoji + " " + src.name : "");
        }

        /// "Only a Warden can enter" when this class can't go in (null = fine).
        public static string ClassBlock(GameContent c, RouteData r, ClassId cls)
        {
            if (r == null || !r.classLocked || r.requiredClass == cls) return null;
            var cd = c.Class(r.requiredClass);
            return "Only a " + (cd != null ? cd.name : r.requiredClass.ToString()) + " can enter";
        }

        /// Full check for setting out (null = go).
        public static string EnterBlock(GameContent c, Profile p, string routeId, ClassId cls)
        {
            var r = c.Route(routeId);
            return LockReason(c, p, r) ?? ClassBlock(c, r, cls);
        }

        public static string RouteName(GameContent c, string id)
        {
            var r = c.Route(id);
            return r != null ? r.name : id;
        }

        // ───────────── the story ─────────────
        public static List<RouteData> Chapters(GameContent c)
        {
            var list = c.routes.FindAll(r => r.chapter > 0);
            list.Sort((a, b) => a.chapter.CompareTo(b.chapter));
            return list;
        }

        /// The first story chapter not cleared yet (null = the story is done).
        public static RouteData CurrentChapter(GameContent c, Profile p)
        {
            foreach (var r in Chapters(c)) if (!Cleared(p, r.id)) return r;
            return null;
        }

        /// One line telling the player what to do next for the story.
        public static string Objective(GameContent c, Profile p)
        {
            var r = CurrentChapter(c, p);
            if (r == null) return "The Everflame burns again. Every route is yours to wander.";
            string head = "Chapter " + r.chapter + ": " + r.emoji + " " + r.name;
            if (IsUnlocked(c, p, r.id)) return head + " — set out when you're ready.";
            if (!HasKey(p, r.requiredKeyItem))
            {
                var src = KeySource(c, r.requiredKeyItem);
                string where = src == null ? "" : " Look in " + src.emoji + " " + src.name +
                    (IsUnlocked(c, p, src.id) ? "" : " (" + LockReason(c, p, src) + ")") +
                    (src.classLocked ? " — only a " + src.requiredClass + " can enter" : "") + ".";
                return head + " needs the " + KeyItemLabel(c, r.requiredKeyItem) + "." + where;
            }
            return head + " — " + LockReason(c, p, r);
        }

        // ───────────── recipe discovery ─────────────
        /// An ingredient is known once it's in the pantry or grows on a route the player has found.
        public static bool IngredientKnown(GameContent c, Profile p, string ingId)
        {
            if (Economy.PantryCount(p, ingId) > 0) return true;
            foreach (var r in c.routes)
                if (r.gatherIngredientIds != null && r.gatherIngredientIds.Contains(ingId) && Discovered(c, p, r)) return true;
            return false;
        }

        /// Camp shows a recipe once all its ingredients are known (keeps the cook page tidy early on).
        public static bool RecipeKnown(GameContent c, Profile p, RecipeData rec)
        {
            if (rec == null) return false;
            if (p.tray != null && p.tray.Contains(rec.id)) return true;
            foreach (var ic in rec.ingredients) if (!IngredientKnown(c, p, ic.ingredientId)) return false;
            return true;
        }

        // ───────────── hazards ─────────────
        /// How many counter meals are eaten against a hazard: a meal that counters it, or a meal with a guarding accelerator, counts 1.
        public static float CounterMeals(GameContent c, List<EatenMeal> eaten, Hazard h)
        {
            if (h == Hazard.None || eaten == null) return 0;
            float n = 0;
            foreach (var m in eaten)
            {
                var r = c.Recipe(m.recipeId);
                if (r != null && r.counters == h) n++;
                var a = string.IsNullOrEmpty(m.accelId) ? null : c.Ingredient(m.accelId);
                if (a != null && a.accelGuard == h) n += 1f;
            }
            return n;
        }

        /// 0 = full hazard, 1 = blocked. Each counter meal eaten cuts it by hazardMitigationPerMeal.
        public static float Mitigation(GameContent c, List<EatenMeal> eaten, Hazard h)
        {
            return Math.Min(1f, CounterMeals(c, eaten, h) * Math.Max(0f, c.tuning.hazardMitigationPerMeal));
        }

        /// Same, plus any worn ward trinket (each counts as perkValue counter meals).
        public static float Mitigation(GameContent c, Profile p, List<EatenMeal> eaten, Hazard h)
        {
            return Math.Min(1f, (CounterMeals(c, eaten, h) + Gear.Ward(c, p, h)) * Math.Max(0f, c.tuning.hazardMitigationPerMeal));
        }

        public static string HazardName(Hazard h) { return h == Hazard.Poison ? "Poison" : h == Hazard.Cold ? "Cold" : h == Hazard.Heat ? "Heat" : h == Hazard.Darkness ? "Darkness" : ""; }
        public static string HazardEmoji(Hazard h) { return h == Hazard.Poison ? "☠️" : h == Hazard.Cold ? "🥶" : h == Hazard.Heat ? "🥵" : h == Hazard.Darkness ? "🌑" : ""; }

        public static bool HazardActive(RouteData r) { return r != null && r.hazard != Hazard.None && r.hazard != Hazard.Darkness && r.hazardPct > 0f; }

        /// What helps: "nettle dishes (antidote)".
        public static string CounterHint(Hazard h)
        {
            return h == Hazard.Poison ? "a meal with nettle in its accelerator slot (antidote)" : h == Hazard.Cold ? "a meal with ginger in its accelerator slot (warming)" :
                   h == Hazard.Heat ? "a meal with mint in its accelerator slot (cooling)" :
                   h == Hazard.Darkness ? "a meal with glowcap in its accelerator slot (light)" : "";
        }

        public static string HazardEffect(Hazard h)
        {
            return h == Hazard.Cold ? "drains HP and slows you" : h == Hazard.Heat ? "drains HP, twice as fast while you stand still" :
                   h == Hazard.Darkness ? "shrinks what you can see to a few steps" : "drains HP";
        }

        // ───────────── mutators (fun dungeon rules) ─────────────
        public static bool Has(RouteData r, Mutator m) { return r != null && r.mutators != null && r.mutators.Contains(m); }

        public static string MutatorName(Mutator m)
        {
            switch (m)
            {
                case Mutator.Swift: return "Swift"; case Mutator.Frantic: return "Frantic"; case Mutator.BigBangs: return "Big Bangs";
                case Mutator.GlassHearts: return "Glass Hearts"; case Mutator.Giants: return "Giants"; case Mutator.Swarm: return "Swarm";
                default: return "";
            }
        }

        public static string MutatorEmoji(Mutator m)
        {
            switch (m)
            {
                case Mutator.Swift: return "💨"; case Mutator.Frantic: return "⚡"; case Mutator.BigBangs: return "💥";
                case Mutator.GlassHearts: return "💔"; case Mutator.Giants: return "🦣"; case Mutator.Swarm: return "🐜";
                default: return "";
            }
        }

        public static string MutatorText(TuningData t, Mutator m)
        {
            switch (m)
            {
                case Mutator.Swift: return "Everyone moves ×" + t.swiftSpeed.ToString("0.#") + " as fast, you included.";
                case Mutator.Frantic: return "Everyone moves ×" + t.franticSpeed.ToString("0.#") + " as fast, you included.";
                case Mutator.BigBangs: return "Area hits (swings, cleaves, slams, zones) deal ×" + t.bigBangDamage.ToString("0.#") + " damage and reach further.";
                case Mutator.GlassHearts: return "Everyone hits ×" + t.glassMult.ToString("0.#") + " as hard: you and the monsters.";
                case Mutator.Giants: return "Monsters are huge: ×" + t.giantHp.ToString("0.##") + " HP, but slower.";
                case Mutator.Swarm: return "×" + t.swarmCount.ToString("0.#") + " the monsters, each one weaker.";
                default: return "";
            }
        }


        public static List<RecipeData> CounterRecipes(GameContent c, Hazard h)
        {
            return c.recipes.FindAll(r => h != Hazard.None && r.counters == h);
        }

        /// Accelerators that guard against a hazard (put one in any meal's accelerator slot).
        public static List<IngredientData> CounterAccelerators(GameContent c, Hazard h)
        {
            return c.ingredients.FindAll(i => h != Hazard.None && i.accelGuard == h);
        }
    }
}
