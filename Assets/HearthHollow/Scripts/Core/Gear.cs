// Hearth & Hollow — Workshop gear tracks (weapon, body, head, boots, bag) and trinkets. Pure C#, no UnityEngine.
// Gear is a small step each tier; meals stay the main source of power.
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public static class Gear
    {
        // ───────────── tracks ─────────────
        public static List<GearTier> Tiers(GameContent c, GearSlot slot)
        {
            switch (slot)
            {
                case GearSlot.Weapon: return c.weaponTiers;
                case GearSlot.Body: return c.armourTiers;
                case GearSlot.Head: return c.headTiers;
                case GearSlot.Boots: return c.bootsTiers;
                case GearSlot.Bag: return c.bagTiers;
            }
            return null;
        }

        public static int TierIndex(Profile p, GearSlot slot, ClassId cls)
        {
            if (p == null) return 0;
            if (slot == GearSlot.Bag) return p.bagTier;
            var cp = p.Progress(cls);
            switch (slot)
            {
                case GearSlot.Weapon: return cp.weaponTier;
                case GearSlot.Body: return cp.armourTier;
                case GearSlot.Head: return cp.headTier;
                case GearSlot.Boots: return cp.bootsTier;
            }
            return 0;
        }

        static void Bump(Profile p, GearSlot slot, ClassId cls)
        {
            if (slot == GearSlot.Bag) { p.bagTier++; return; }
            var cp = p.Progress(cls);
            switch (slot)
            {
                case GearSlot.Weapon: cp.weaponTier++; break;
                case GearSlot.Body: cp.armourTier++; break;
                case GearSlot.Head: cp.headTier++; break;
                case GearSlot.Boots: cp.bootsTier++; break;
            }
        }

        public static GearTier Current(GameContent c, Profile p, GearSlot slot, ClassId cls)
        {
            var t = Tiers(c, slot);
            if (t == null || t.Count == 0) return null;
            return t[Math.Max(0, Math.Min(TierIndex(p, slot, cls), t.Count - 1))];
        }

        public static GearTier Next(GameContent c, Profile p, GearSlot slot, ClassId cls)
        {
            var t = Tiers(c, slot);
            if (t == null) return null;
            int n = Math.Max(0, TierIndex(p, slot, cls)) + 1;
            return n < t.Count ? t[n] : null;
        }

        /// Buy the next tier of a track for the selected class (bag: shared). Armourer event discount applies.
        public static bool Buy(GameContent c, Profile p, HearthState h, GearSlot slot, out string reason)
        {
            GearTier next = Next(c, p, slot, p.selectedClass);
            if (next == null) { reason = SlotName(slot) + " is maxed"; return false; }
            int price = Economy.GearPrice(c, h, next);
            if (p.coins < price) { reason = "Not enough coins"; return false; }
            p.coins -= price;
            Bump(p, slot, p.selectedClass);
            reason = null; return true;
        }

        /// Free: the next tier of a track for <paramref name="cls"/> (the opening arc's pit cache). Null if it's maxed.
        public static GearTier Grant(GameContent c, Profile p, GearSlot slot, ClassId cls)
        {
            GearTier next = Next(c, p, slot, cls);
            if (next == null) return null;
            Bump(p, slot, cls);
            return next;
        }

        public static string SlotName(GearSlot s)
        {
            switch (s)
            {
                case GearSlot.Weapon: return "Weapon";
                case GearSlot.Body: return "Body";
                case GearSlot.Head: return "Head";
                case GearSlot.Boots: return "Boots";
                case GearSlot.Bag: return "Bag";
            }
            return s.ToString();
        }

        /// What a tier gives, e.g. "5 dmg", "+2 armour", "+4 max HP", "+7% speed", "12 slots".
        public static string ValueText(GearSlot s, GearTier t)
        {
            if (t == null) return "";
            switch (s)
            {
                case GearSlot.Weapon: return Num(t.value) + " dmg";
                case GearSlot.Body: return "+" + Num(t.value) + " armour";
                case GearSlot.Head: return "+" + Num(t.value) + " max HP";
                case GearSlot.Boots: return "+" + (int)Math.Round(t.value * 100) + "% speed";
                case GearSlot.Bag: return Num(t.value) + " slots";
            }
            return Num(t.value);
        }

        static string Num(float v) { return Math.Abs(v - Math.Round(v)) < 0.01 ? ((int)Math.Round(v)).ToString() : v.ToString("0.#"); }

        public static int HeadHp(GameContent c, Profile p, ClassId cls) { var t = Current(c, p, GearSlot.Head, cls); return t != null ? (int)Math.Round(t.value) : 0; }
        public static float BootsSpeed(GameContent c, Profile p, ClassId cls) { var t = Current(c, p, GearSlot.Boots, cls); return t != null ? t.value : 0f; }

        // ───────────── trinkets ─────────────
        public static TrinketData Trinket(GameContent c, string id)
        {
            if (c.trinkets == null || string.IsNullOrEmpty(id)) return null;
            return c.trinkets.Find(x => x.id == id);
        }

        public static bool Owns(Profile p, string id) { return p != null && p.trinkets != null && p.trinkets.Contains(id); }
        public static bool Worn(Profile p, string id) { return p != null && p.equippedTrinkets != null && p.equippedTrinkets.Contains(id); }

        /// 2 slots, +1 once the tuning's trinketSlotRoute (Cavern Hunt, chapter 4) is cleared.
        public static int TrinketSlots(GameContent c, Profile p)
        {
            int n = Math.Max(0, c.tuning.trinketSlots);
            if (p != null && !string.IsNullOrEmpty(c.tuning.trinketSlotRoute) && p.clearedRouteIds.Contains(c.tuning.trinketSlotRoute)) n++;
            return n;
        }

        static void Ensure(Profile p)
        {
            if (p.trinkets == null) p.trinkets = new List<string>();
            if (p.equippedTrinkets == null) p.equippedTrinkets = new List<string>();
        }

        /// Adds a trinket to the collection (and wears it if a slot is free). False if already owned.
        public static bool Give(GameContent c, Profile p, string id)
        {
            if (Trinket(c, id) == null) return false;
            Ensure(p);
            if (p.trinkets.Contains(id)) return false;
            p.trinkets.Add(id);
            if (p.equippedTrinkets.Count < TrinketSlots(c, p)) p.equippedTrinkets.Add(id);
            return true;
        }

        public static bool Equip(GameContent c, Profile p, string id, out string reason)
        {
            Ensure(p);
            if (!p.trinkets.Contains(id)) { reason = "You don't have that"; return false; }
            if (p.equippedTrinkets.Contains(id)) { reason = null; return true; }
            if (p.equippedTrinkets.Count >= TrinketSlots(c, p)) { reason = "Trinket slots are full — take one off first"; return false; }
            p.equippedTrinkets.Add(id);
            reason = null; return true;
        }

        public static bool Unequip(Profile p, string id)
        {
            Ensure(p);
            return p.equippedTrinkets.Remove(id);
        }

        /// Highest story chapter reached (cleared chapters + 1).
        public static int ChapterReached(GameContent c, Profile p)
        {
            var cur = Routes.CurrentChapter(c, p);
            if (cur != null) return cur.chapter;
            int max = 0; foreach (var r in Routes.Chapters(c)) max = Math.Max(max, r.chapter);
            return max + 1;
        }

        /// Trinkets the Workshop sells right now (story chapter reached, not owned yet).
        public static List<TrinketData> WorkshopStock(GameContent c, Profile p)
        {
            var list = new List<TrinketData>();
            if (c.trinkets == null) return list;
            int ch = ChapterReached(c, p);
            foreach (var t in c.trinkets) if (t.inWorkshop && t.price > 0 && ch >= t.shopChapter && !Owns(p, t.id)) list.Add(t);
            return list;
        }

        /// Trinkets the Workshop will sell later (so the player can see what's coming).
        public static List<TrinketData> WorkshopLater(GameContent c, Profile p)
        {
            var list = new List<TrinketData>();
            if (c.trinkets == null) return list;
            int ch = ChapterReached(c, p);
            foreach (var t in c.trinkets) if (t.inWorkshop && t.price > 0 && ch < t.shopChapter && !Owns(p, t.id)) list.Add(t);
            return list;
        }

        public static bool BuyTrinket(GameContent c, Profile p, string id, out string reason)
        {
            var t = Trinket(c, id);
            if (t == null || !t.inWorkshop || t.price <= 0) { reason = "Not for sale"; return false; }
            if (Owns(p, id)) { reason = "You already have it"; return false; }
            if (ChapterReached(c, p) < t.shopChapter) { reason = "Not in stock yet"; return false; }
            if (p.coins < t.price) { reason = "Not enough coins"; return false; }
            p.coins -= t.price;
            Give(c, p, id);
            reason = null; return true;
        }

        /// Stat mods from worn trinkets (indexed by BuffStat), added like permanent meal buffs.
        public static float[] StatBonus(GameContent c, Profile p)
        {
            var sum = new float[Enum.GetValues(typeof(BuffStat)).Length];
            if (p == null || p.equippedTrinkets == null) return sum;
            foreach (var id in p.equippedTrinkets)
            {
                var t = Trinket(c, id);
                if (t == null || t.buffs == null) continue;
                foreach (var m in t.buffs) sum[(int)m.stat] += m.value;
            }
            return sum;
        }

        /// Sum of a perk over worn trinkets.
        public static float Perk(GameContent c, Profile p, TrinketPerk perk)
        {
            float v = 0f;
            if (p == null || p.equippedTrinkets == null) return v;
            foreach (var id in p.equippedTrinkets) { var t = Trinket(c, id); if (t != null && t.perk == perk) v += t.perkValue; }
            return v;
        }

        /// Counter "meals" a worn ward is worth against a hazard.
        public static float Ward(GameContent c, Profile p, Hazard h)
        {
            float v = 0f;
            if (p == null || p.equippedTrinkets == null || h == Hazard.None) return v;
            foreach (var id in p.equippedTrinkets)
            { var t = Trinket(c, id); if (t != null && t.perk == TrinketPerk.HazardWard && t.wardHazard == h) v += t.perkValue; }
            return v;
        }

        /// One line describing what a trinket does.
        public static string EffectText(GameContent c, TrinketData t)
        {
            if (t == null) return "";
            var parts = new List<string>();
            if (t.buffs != null) foreach (var m in t.buffs) parts.Add(BuffText(m));
            switch (t.perk)
            {
                case TrinketPerk.MoveSpeed: parts.Add("+" + Pct(t.perkValue) + " move speed"); break;
                case TrinketPerk.DashCooldown: parts.Add("−" + Pct(t.perkValue) + " dash cooldown"); break;
                case TrinketPerk.SkillCooldown: parts.Add("−" + Pct(t.perkValue) + " skill cooldowns"); break;
                case TrinketPerk.HealOnKill: parts.Add("heal " + (int)Math.Round(t.perkValue) + " HP per kill"); break;
                case TrinketPerk.AttackPower: parts.Add("+" + Pct(t.perkValue) + " attack power"); break;
                case TrinketPerk.SkillPower: parts.Add("+" + Pct(t.perkValue) + " skill power"); break;
                case TrinketPerk.HazardWard:
                    parts.Add(Routes.HazardEmoji(t.wardHazard) + " wards off " + Routes.HazardName(t.wardHazard).ToLower() + " (like " +
                              (t.perkValue >= 1.99f ? "two counter meals" : "a counter meal") + ")"); break;
            }
            return string.Join(" · ", parts.ToArray());
        }

        static string Pct(float f) { return (int)Math.Round(f * 100) + "%"; }

        static string BuffText(StatMod m)
        {
            switch (m.stat)
            {
                case BuffStat.DamagePct: return "+" + Pct(m.value) + " damage";
                case BuffStat.MaxHp: return "+" + (int)Math.Round(m.value) + " max HP";
                case BuffStat.Armour: return "+" + Num(m.value) + " armour";
                case BuffStat.CritPct: return "+" + Pct(m.value) + " crit";
                case BuffStat.GatherZonePct: return "+" + Pct(m.value) + " gather zone";
                case BuffStat.CoinPct: return "+" + Pct(m.value) + " coins";
                case BuffStat.XpPct: return "+" + Pct(m.value) + " XP";
                case BuffStat.MoveSpeedPct: return "+" + Pct(m.value) + " speed";
                case BuffStat.HealOnKill: return "+" + Num(m.value) + " HP per kill";
            }
            return m.stat.ToString();
        }

        /// Where a trinket comes from, for the Workshop collection.
        public static string SourceText(GameContent c, TrinketData t)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(t.fromRouteId))
            {
                var r = c.Route(t.fromRouteId);
                parts.Add("first clear of " + (r != null ? r.emoji + " " + r.name : t.fromRouteId));
            }
            if (t.inWorkshop) parts.Add("Upgrades shop" + (t.shopChapter > 1 ? " from chapter " + t.shopChapter : ""));
            if (t.merchant) parts.Add("travelling merchant");
            if (t.drop) parts.Add("rare boss / elite drop");
            return parts.Count == 0 ? "" : string.Join(", ", parts.ToArray());
        }

        // ───────────── finding trinkets ─────────────
        /// A boss or elite might drop a trinket you don't own yet (drop pool only).
        public static string RollDrop(GameContent c, Profile p, bool boss, bool elite, Rng rng)
        {
            float chance = boss ? c.tuning.trinketBossDrop : elite ? c.tuning.trinketEliteDrop : 0f;
            if (chance <= 0f || c.trinkets == null || !rng.Chance(chance)) return null;
            var pool = new List<string>();
            foreach (var t in c.trinkets) if (t.drop && !Owns(p, t.id)) pool.Add(t.id);
            return pool.Count == 0 ? null : rng.Pick(pool);
        }

        /// The trinket a route's first clear gives (null = none, or already owned).
        public static string ClearReward(GameContent c, Profile p, string routeId)
        {
            if (c.trinkets == null) return null;
            foreach (var t in c.trinkets) if (t.fromRouteId == routeId && !Owns(p, t.id)) return t.id;
            return null;
        }

        /// The merchant sometimes stocks one trinket you don't own (merchant pool).
        public static void RollMerchant(GameContent c, Profile p, HearthState h, Rng rng)
        {
            if (c.trinkets == null || !rng.Chance(c.tuning.merchantTrinketChance)) return;
            var pool = new List<TrinketData>();
            foreach (var t in c.trinkets) if (t.merchant && t.price > 0 && !Owns(p, t.id)) pool.Add(t);
            if (pool.Count == 0) return;
            var pick = rng.Pick(pool);
            h.merchantTrinket = pick.id;
            h.merchantTrinketPrice = pick.price;
            h.merchantTrinketSold = false;
        }

        public static bool BuyMerchantTrinket(GameContent c, Profile p, HearthState h, out string reason)
        {
            if (h == null || h.campEvent != CampEvent.Merchant || string.IsNullOrEmpty(h.merchantTrinket)) { reason = "No trinket for sale"; return false; }
            if (h.merchantTrinketSold || Owns(p, h.merchantTrinket)) { reason = "Sold out"; return false; }
            if (p.coins < h.merchantTrinketPrice) { reason = "Not enough coins"; return false; }
            p.coins -= h.merchantTrinketPrice;
            h.merchantTrinketSold = true;
            Give(c, p, h.merchantTrinket);
            reason = null; return true;
        }
    }
}
