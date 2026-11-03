// Hearth & Hollow — economy, progression, camp (Agent C). Pure C#, no UnityEngine.
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public static class Economy
    {
        // ───────── Profile ─────────
        public static Profile NewProfile(GameContent c)
        {
            var p = new Profile();
            p.coins = c.starterCoins;
            p.selectedClass = ClassId.Cook;   // a new game starts with the Cook; the others join through the story
            foreach (ClassId id in Enum.GetValues(typeof(ClassId))) p.Progress(id);
            for (int i = 0; i < c.classes.Count; i++) p.Progress(c.classes[i].id);
            if (c.starterPantry != null)
                for (int i = 0; i < c.starterPantry.Count; i++)
                    AddToPantry(p, c.starterPantry[i].ingredientId, c.starterPantry[i].count);
            p.day = 1;
            return p;
        }

        // ───────── Pantry helpers ─────────
        public static int PantryCount(Profile p, string ingredientId)
        {
            int n = 0;
            for (int i = 0; i < p.pantry.Count; i++) if (p.pantry[i].ingredientId == ingredientId) n += p.pantry[i].count;
            return n;
        }

        /// How many of one ingredient at one grade (0 Plain · 1 Fine · 2 Prime).
        public static int PantryCount(Profile p, string ingredientId, int grade)
        {
            int n = 0;
            for (int i = 0; i < p.pantry.Count; i++) if (p.pantry[i].ingredientId == ingredientId && p.pantry[i].grade == grade) n += p.pantry[i].count;
            return n;
        }

        public static void AddToPantry(Profile p, string ingredientId, int count, int grade = 0)
        {
            if (string.IsNullOrEmpty(ingredientId) || count <= 0) return;
            Routes.See(p, ingredientId);   // ticket 24: having it reveals its recipes
            grade = 0;   // ingredients have no grades any more (Design v2, ticket 08)
            ItemStack s = p.pantry.Find(x => x.ingredientId == ingredientId && x.grade == grade);
            if (s != null) { s.count += count; return; }
            // keep each ingredient's grades together, Plain first
            int at = p.pantry.Count;
            for (int i = 0; i < p.pantry.Count; i++)
                if (p.pantry[i].ingredientId == ingredientId) at = p.pantry[i].grade < grade ? i + 1 : Math.Min(at, i);
            p.pantry.Insert(at, new ItemStack { ingredientId = ingredientId, count = count, grade = grade });
        }

        /// Removes from one grade only.
        public static bool RemoveFromPantry(Profile p, string ingredientId, int count, int grade)
        {
            if (PantryCount(p, ingredientId, grade) < count) return false;
            for (int i = 0; i < p.pantry.Count && count > 0; i++)
            {
                ItemStack s = p.pantry[i];
                if (s.ingredientId != ingredientId || s.grade != grade) continue;
                int n = Math.Min(count, s.count);
                s.count -= n; count -= n;
            }
            p.pantry.RemoveAll(x => x.count <= 0);
            return true;
        }

        /// Removes from any grade, lowest first.
        public static bool RemoveFromPantry(Profile p, string ingredientId, int count)
        {
            if (PantryCount(p, ingredientId) < count) return false;
            for (int g = 0; g < 3 && count > 0; g++)
            for (int i = 0; i < p.pantry.Count && count > 0; i++)
            {
                ItemStack s = p.pantry[i];
                if (s.ingredientId != ingredientId || s.grade != g) continue;
                int n = Math.Min(count, s.count);
                s.count -= n; count -= n;
            }
            p.pantry.RemoveAll(x => x.count <= 0);
            return true;
        }


        // ───────── Gear ─────────
        private static GearTier TierAt(List<GearTier> tiers, int index)
        {
            if (tiers == null || tiers.Count == 0) return null;
            if (index < 0) index = 0;
            if (index >= tiers.Count) index = tiers.Count - 1;
            return tiers[index];
        }

        private static GearTier NextTier(List<GearTier> tiers, int index)
        {
            if (tiers == null) return null;
            int n = Math.Max(0, index) + 1;
            return n < tiers.Count ? tiers[n] : null;
        }

        public static GearTier CurrentWeapon(GameContent c, Profile p, ClassId id)
        { return TierAt(c.weaponTiers, p != null ? p.Progress(id).weaponTier : 0); }
        public static GearTier NextWeapon(GameContent c, Profile p, ClassId id)
        { return NextTier(c.weaponTiers, p != null ? p.Progress(id).weaponTier : 0); }
        public static GearTier CurrentArmour(GameContent c, Profile p, ClassId id)
        { return TierAt(c.armourTiers, p != null ? p.Progress(id).armourTier : 0); }
        public static GearTier NextArmour(GameContent c, Profile p, ClassId id)
        { return NextTier(c.armourTiers, p != null ? p.Progress(id).armourTier : 0); }

        /// What a gear tier costs today (the Armourer camp event knocks armourerDiscount off).
        public static int GearPrice(GameContent c, HearthState h, GearTier tier)
        {
            if (tier == null) return 0;
            if (h != null && h.campEvent == CampEvent.Armourer)
                return Math.Max(1, (int)Math.Round(tier.price * (1f - c.tuning.armourerDiscount), MidpointRounding.AwayFromZero));
            return tier.price;
        }

        public static bool BuyWeapon(GameContent c, Profile p, out string reason) { return BuyWeapon(c, p, null, out reason); }
        public static bool BuyWeapon(GameContent c, Profile p, HearthState h, out string reason)
        {
            reason = "Power comes from food now: the smithy sells boots, lanterns and bags"; return false;   // Design v2 (ticket 16)
#pragma warning disable 0162
            GearTier next = NextWeapon(c, p, p.selectedClass);
            if (next == null) { reason = "Weapon is maxed"; return false; }
            int price = GearPrice(c, h, next);
            if (p.coins < price) { reason = "Not enough coins"; return false; }
            p.coins -= price;
            p.Progress(p.selectedClass).weaponTier++;
            reason = null; return true;
#pragma warning restore 0162
        }

        public static bool BuyArmour(GameContent c, Profile p, out string reason) { return BuyArmour(c, p, null, out reason); }
        public static bool BuyArmour(GameContent c, Profile p, HearthState h, out string reason)
        {
            reason = "Power comes from food now: the smithy sells boots, lanterns and bags"; return false;   // Design v2 (ticket 16)
#pragma warning disable 0162
            GearTier next = NextArmour(c, p, p.selectedClass);
            if (next == null) { reason = "Armour is maxed"; return false; }
            int price = GearPrice(c, h, next);
            if (p.coins < price) { reason = "Not enough coins"; return false; }
            p.coins -= price;
            p.Progress(p.selectedClass).armourTier++;
            reason = null; return true;
#pragma warning restore 0162
        }

        // ───────── Bag (shared by every class; bigger bags = more slots) ─────────
        public static GearTier CurrentBag(GameContent c, Profile p)
        {
            if (c.bagTiers == null || c.bagTiers.Count == 0) return null;
            return c.bagTiers[Math.Max(0, Math.Min(p.bagTier, c.bagTiers.Count - 1))];
        }

        public static GearTier NextBag(GameContent c, Profile p)
        {
            if (c.bagTiers == null || p.bagTier + 1 >= c.bagTiers.Count) return null;
            return c.bagTiers[p.bagTier + 1];
        }

        /// Bag slots for a run (falls back to the old tuning numbers if the content has no bag tiers).
        public static int BagSlots(GameContent c, Profile p)
        {
            var b = CurrentBag(c, p);
            if (b != null) return Math.Max(1, (int)Math.Round(b.value));
            return p.hasBackpack ? c.tuning.bagSlotsBackpack : c.tuning.bagSlots;
        }

        public static bool BuyBag(GameContent c, Profile p, HearthState h, out string reason)
        {
            GearTier next = NextBag(c, p);
            if (next == null) { reason = "Your bag is as big as it gets"; return false; }
            int price = GearPrice(c, h, next);
            if (p.coins < price) { reason = "Not enough coins"; return false; }
            p.coins -= price;
            p.bagTier++;
            reason = null; return true;
        }

        /// Old saves (before bag tiers): a bought backpack becomes the biggest bag, otherwise you start small.
        public static void UpgradeBag(GameContent c, Profile p)
        {
            if (p.saveVersion >= 4) return;
            int max = c.bagTiers != null ? Math.Max(0, c.bagTiers.Count - 1) : 0;
            p.bagTier = p.hasBackpack ? max : 0;
            p.saveVersion = 4;
        }

        // ───────── Routes ─────────
        public static bool IsRouteUnlocked(GameContent c, Profile p, string routeId)
        {
            return Routes.IsUnlocked(c, p, routeId);   // previous route cleared + key item owned (Routes.cs)
        }

        // ───────── Camp ─────────
        public static int MerchantUnitPrice(IngredientData ing) { return 6 + (ing != null ? ing.tier : 0) * 6; }

        public static HearthState RollCamp(GameContent c, Profile p, Rng rng)
        {
            TuningData t = c.tuning;
            var h = new HearthState();
            // One roll split into bands so each chance is an absolute share of camp visits.
            float roll = rng.Value(), edge = 0f;
            h.campEvent = CampEvent.None;
            if (roll < (edge += t.merchantChance)) h.campEvent = CampEvent.Merchant;
            else if (roll < (edge += t.stormChance)) h.campEvent = CampEvent.Storm;
            else if (roll < (edge += t.armourerChance)) h.campEvent = CampEvent.Armourer;
            else if (roll < (edge += t.foragerChance)) h.campEvent = CampEvent.Forager;
            else if (roll < (edge += t.harvestMoonChance)) h.campEvent = CampEvent.HarvestMoon;
            if (Slice.On) h.campEvent = CampEvent.None;   // no camp events in the slice

            if (h.campEvent == CampEvent.Forager)
            {
                // A passing forager leaves ingredients from routes the player can already reach.
                var pool = new List<string>();
                for (int i = 0; i < c.routes.Count; i++)
                    if (IsRouteUnlocked(c, p, c.routes[i].id))
                        for (int k = 0; k < c.routes[i].gatherIngredientIds.Count; k++)
                            if (!pool.Contains(c.routes[i].gatherIngredientIds[k])) pool.Add(c.routes[i].gatherIngredientIds[k]);
                if (pool.Count == 0) for (int i = 0; i < c.ingredients.Count; i++) if (!c.ingredients[i].material) pool.Add(c.ingredients[i].id);
                for (int i = 0; i < t.foragerGiftCount && pool.Count > 0; i++)
                {
                    string id = rng.Pick(pool);
                    ItemStack g = h.gift.Find(x => x.ingredientId == id);
                    if (g == null) h.gift.Add(new ItemStack { ingredientId = id, count = 1 }); else g.count++;
                }
            }

            if (h.campEvent == CampEvent.Merchant && c.ingredients.Count > 0)
            {
                var pool = c.ingredients.FindAll(x => !x.material);   // building materials only come from the dungeons
                int offers = Math.Min(pool.Count, rng.Range(3, 4));
                for (int i = 0; i < offers; i++)
                {
                    int k = rng.Range(0, pool.Count - 1);
                    IngredientData ing = pool[k]; pool.RemoveAt(k);
                    int count = rng.Range(1, 3);
                    h.merchant.Add(new MerchantOffer { ingredientId = ing.id, count = count, price = MerchantUnitPrice(ing) * count });
                }
                Gear.RollMerchant(c, p, h, rng);   // sometimes a trinket too
            }

            RouteMod[] mods = { RouteMod.Rich, RouteMod.Dangerous, RouteMod.Lucky };   // (Double XP retired with XP, ticket 04)
            for (int i = 0; i < c.routes.Count; i++)
            {
                RouteMod mod = RouteMod.None;
                if (rng.Chance(t.routeModChance)) mod = mods[rng.Range(0, mods.Length - 1)];
                if (h.campEvent == CampEvent.Storm) mod = RouteMod.Dangerous;   // storm: every route is Dangerous
                h.routeOffers.Add(new RouteOffer { routeId = c.routes[i].id, mod = mod });
            }
            return h;
        }

        public static bool BuyMerchant(GameContent c, Profile p, HearthState h, int index, out string reason)
        {
            if (h == null || h.campEvent != CampEvent.Merchant || index < 0 || index >= h.merchant.Count)
            { reason = "No such offer"; return false; }
            MerchantOffer o = h.merchant[index];
            if (o.sold) { reason = "Sold out"; return false; }
            if (p.coins < o.price) { reason = "Not enough coins"; return false; }
            p.coins -= o.price;
            o.sold = true;
            AddToPantry(p, o.ingredientId, o.count);
            reason = null; return true;
        }

        // ───────── Run settlement ─────────
        public static RunSummary SettleRun(GameContent c, Profile p, RunState run, PlayerStats stats)
        {
            TuningData t = c.tuning;
            RouteData route = c.Route(run.routeId);
            var s = new RunSummary
            {
                outcome = run.outcome, routeId = run.routeId, classId = run.classId,
                roomsCleared = run.roomsCleared, kills = run.kills,
                floorReached = run.dungeon ? run.floor : 0, floorsTotal = run.dungeon ? run.floorsTotal : 0
            };

            s.coinsFromKills = run.coinsEarned;
            if (run.outcome == RunOutcome.Cleared)
            {
                int clearReward = route != null ? route.clearReward : 0;
                s.clearCoins = clearReward + run.roomsCleared * t.roomCoinBonus;
            }
            else if (run.outcome == RunOutcome.Returned)
            {
                // took an exit before the last floor: a share of the clear bonus, no route unlock
                float share = DungeonRules.ClearShare(run);
                int clearReward = route != null ? route.clearReward : 0;
                s.clearCoins = (int)Math.Round(clearReward * share) + run.roomsCleared * t.roomCoinBonus;
            }

            s.totalCoins = s.coinsFromKills + s.clearCoins;
            p.coins += s.totalCoins;

            if (run.trinketsDropped != null) s.trinketsFound.AddRange(run.trinketsDropped);   // dropped during the run (already kept)

            // Goods: unload bag into pantry
            s.goods = Bag.Contents(run);
            if (run.outcome == RunOutcome.Died)
            {
                // a fall costs most of the bag: keep deathKeepFraction of each stack (rounded down)
                var kept = new List<ItemStack>();
                foreach (var g in s.goods)
                {
                    int keep = (int)Math.Floor(g.count * Math.Max(0f, Math.Min(1f, t.deathKeepFraction)));
                    if (g.count - keep > 0) s.lost.Add(new ItemStack { ingredientId = g.ingredientId, count = g.count - keep, grade = g.grade });
                    if (keep > 0) kept.Add(new ItemStack { ingredientId = g.ingredientId, count = keep, grade = g.grade });
                }
                s.goods = kept;
            }
            for (int i = 0; i < s.goods.Count; i++) AddToPantry(p, s.goods[i].ingredientId, s.goods[i].count, s.goods[i].grade);

            // (Meals were all eaten at set-out, so there is nothing to return to the tray.)

            if (run.outcome == RunOutcome.Cleared && !string.IsNullOrEmpty(run.routeId))
            {
                var lockedBefore = new List<string>();
                for (int i = 0; i < c.routes.Count; i++)
                    if (!IsRouteUnlocked(c, p, c.routes[i].id)) lockedBefore.Add(c.routes[i].id);
                s.firstClear = !p.clearedRouteIds.Contains(run.routeId);
                if (s.firstClear) p.clearedRouteIds.Add(run.routeId);
                // story: a key item for the first clear, and the route's closing text
                if (route != null && !string.IsNullOrEmpty(route.rewardKeyItem) && !Routes.HasKey(p, route.rewardKeyItem))
                {
                    if (p.keyItems == null) p.keyItems = new List<string>();
                    p.keyItems.Add(route.rewardKeyItem);
                    s.keyItemFound = route.rewardKeyItem;
                }
                if (s.firstClear && route != null && !string.IsNullOrEmpty(route.storyOutro)) s.storyText = route.storyOutro;
                // a trinket for the first clear of some routes (side dungeons)
                string trinket = Gear.ClearReward(c, p, run.routeId);
                if (trinket != null && Gear.Give(c, p, trinket)) s.trinketsFound.Add(trinket);
                for (int i = 0; i < lockedBefore.Count; i++)
                    if (IsRouteUnlocked(c, p, lockedBefore[i])) s.unlockedRouteIds.Add(lockedBefore[i]);
                p.runsCleared++;
            }
            p.day++;
            return s;
        }
    }
}
