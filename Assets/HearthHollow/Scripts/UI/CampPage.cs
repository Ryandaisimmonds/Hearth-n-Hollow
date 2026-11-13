// Hearth & Hollow — what the camp's tabs and its right-hand column share. HearthScreen hosts them.
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public abstract class CampPage
    {
        protected readonly GameUI Ui;
        protected GameSession S { get { return Ui.Session; } }
        protected GameContent C { get { return Ui.Session.Content; } }

        protected CampPage(GameUI ui) { Ui = ui; }

        /// Rebuild from session state (the host calls it while this page is showing).
        public abstract void Refresh();

        protected static readonly Station[] Stations = { Station.Pot, Station.BBQ, Station.Sandwich };
        protected static readonly string[] StationLabels = { "🍲 Pot", "🍖 BBQ", "🥪 Sandwich" };

        protected static void SetIgnore(VisualElement e)
        {
            e.pickingMode = PickingMode.Ignore;
            foreach (var c in e.Children()) SetIgnore(c);
        }

        protected static string GearPrefix(GearSlot s) { return s == GearSlot.Boots ? "boots_" : s == GearSlot.Bag ? "bag_" : s == GearSlot.Lantern ? "lantern_" : s == GearSlot.Weapon ? "weapon_" : s == GearSlot.Body ? "armour_" : "head_"; }
        protected static string GearEmoji(GearSlot s) { return s == GearSlot.Boots ? "👢" : s == GearSlot.Bag ? "🎒" : s == GearSlot.Lantern ? "🏮" : s == GearSlot.Weapon ? "🗡️" : s == GearSlot.Body ? "🛡️" : "🪖"; }

        protected static VisualElement GearPic(string prefix, GearTier tier, string emoji)
        {
            Sprite s = tier != null ? ArtLibrary.GetGear(prefix + tier.tier) : null;
            if (s == null && prefix == "bag_") s = ArtLibrary.GetGear("backpack");   // one bag picture for every size
            return Art.Framed(Art.Pic(s, emoji, "ico", "shop-row__icon"), "item-frame--sm");
        }
    }
}
