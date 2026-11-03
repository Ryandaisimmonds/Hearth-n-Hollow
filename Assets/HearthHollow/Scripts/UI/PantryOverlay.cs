// Hearth & Hollow — the larder (ingredient store) as a pop-up, opened from the Cooking tab. Shown as "Larder" since
// cooking v9, where "meal pantry" means cooked meals. One entry per ingredient AND grade (Plain / Fine / Prime).
using HearthHollow.Core;
using HearthHollow.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class PantryOverlay : UIScreen
    {
        readonly VisualElement _grid, _mats;
        readonly Label _sub;

        public PantryOverlay(GameUI ui) : base(ui, "overlay", "pantry-pop")
        {
            var box = UIKit.E("overlay__box", "pantry-pop__box");
            Root.Add(box);
            Root.RegisterCallback<PointerDownEvent>(e => { if (e.target == Root) Ui.ClosePantry(); });

            var close = UIKit.B("", () => Ui.ClosePantry(), "settings__close");
            close.tooltip = "Close (Esc)";
            box.Add(close);
            box.Add(UIKit.L("Larder", "overlay__title"));
            _sub = UIKit.L("", "small", "muted", "pantry-pop__sub");
            box.Add(_sub);
            var sv = new ScrollView(ScrollViewMode.Vertical);
            sv.AddToClassList("pantry-pop__scroll");
            sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _grid = UIKit.E("pantry-grid", "pantry-pop__grid");
            sv.Add(_grid);
            box.Add(sv);
            // building materials (kitchen upgrades)
            _mats = UIKit.E("larder-mats");
            box.Add(_mats);
            box.Add(UIKit.L("⚪ Plain · 🟢 Fine · 🔵 Prime · ⚡ accelerator", "small", "muted"));
            box.Add(UIKit.B("Back to cooking", () => Ui.ClosePantry(), "btn--primary"));
        }

        public override void Refresh()
        {
            _grid.Clear();
            _mats.Clear();
            var matRow = UIKit.E("larder-mats__row");
            int mats = 0;
            foreach (var ing in C.ingredients)
            {
                if (!ing.material) continue;
                int n = Economy.PantryCount(S.Profile, ing.id);
                var it = UIKit.E("pantry-item", "pantry-item--material");
                it.Add(Art.Framed(Art.Pic(ArtLibrary.GetIngredient(ing.id), ing.emoji, "pantry-item__emoji"), "item-frame--sm"));
                it.Add(UIKit.L(ing.name, "pantry-item__name"));
                it.Add(UIKit.L("×" + n, "pantry-item__count"));
                it.EnableInClassList("pantry-item--used", n <= 0);
                it.tooltip = ing.name + " — a building material for kitchen upgrades. Monsters drop it in the dungeons.";
                matRow.Add(it);
                mats++;
            }
            if (mats > 0) { _mats.Add(UIKit.L("🔨 Building materials", "subhead", "larder-mats__title")); _mats.Add(matRow); }
            UIKit.Show(_mats, mats > 0);
            var p = S.Profile;
            int kinds = 0, total = 0;
            for (int i = 0; i < p.pantry.Count; i++)
            {
                ItemStack st = p.pantry[i];
                if (st == null || st.count <= 0) continue;
                if (Kitchen.IsMaterial(C, st.ingredientId)) continue;   // shown under Building materials
                var item = UIKit.E("pantry-item", "pantry-item--big");
                item.Add(Art.Framed(Art.Pic(ArtLibrary.GetIngredient(st.ingredientId), UIKit.IngEmoji(C, st.ingredientId), "pantry-item__emoji"), "item-frame--sm"));
                var ing = C.Ingredient(st.ingredientId);
                item.Add(UIKit.L(UIKit.IngName(C, st.ingredientId), "pantry-item__name"));
                item.Add(UIKit.L("×" + st.count, "pantry-item__count"));
                string slot = Cooking.SlotText(ing);   // "🔥 Infuses Fire" / "❄️ Guards against Frost"
                if (!string.IsNullOrEmpty(slot)) item.Add(UIKit.L(slot.Substring(0, slot.IndexOf(' ')), "pantry-item__reserved"));
                if (ing != null && ing.deep) item.AddToClassList("pantry-item--deep");
                item.tooltip = UIKit.IngName(C, st.ingredientId) + (ing != null && ing.deep ? " — rare, found deep in the dungeons" : "")
                             + (!string.IsNullOrEmpty(slot) ? "\nElement slot: " + slot : "");
                _grid.Add(item);
                kinds++; total += st.count;
            }
            if (kinds == 0) _grid.Add(UIKit.L("The pantry is bare. Gather in the Hollow!", "muted"));
            _sub.text = kinds == 0 ? "" : total + " ingredients · " + kinds + " kinds";
        }

        public override bool HandleKey(KeyCode key)
        {
            if (key == KeyCode.Escape || key == KeyCode.Return || key == KeyCode.KeypadEnter) Ui.ClosePantry();
            return true;   // modal
        }
    }
}
