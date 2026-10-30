// Hearth & Hollow — the pantry as a pop-up (opened from the Tray column on the Cooking tab).
// Everything you've gathered or bought; "in tray" counts are already set aside for packed meals.
using HearthHollow.Core;
using HearthHollow.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class PantryOverlay : UIScreen
    {
        readonly VisualElement _grid;
        readonly Label _sub;

        public PantryOverlay(GameUI ui) : base(ui, "overlay", "pantry-pop")
        {
            var box = UIKit.E("overlay__box", "pantry-pop__box");
            Root.Add(box);
            Root.RegisterCallback<PointerDownEvent>(e => { if (e.target == Root) Ui.ClosePantry(); });

            var close = UIKit.B("", () => Ui.ClosePantry(), "settings__close");
            close.tooltip = "Close (Esc)";
            box.Add(close);
            box.Add(UIKit.L("Pantry", "overlay__title"));
            _sub = UIKit.L("", "small", "muted", "pantry-pop__sub");
            box.Add(_sub);
            var sv = new ScrollView(ScrollViewMode.Vertical);
            sv.AddToClassList("pantry-pop__scroll");
            sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _grid = UIKit.E("pantry-grid", "pantry-pop__grid");
            sv.Add(_grid);
            box.Add(sv);
            box.Add(UIKit.B("Back to cooking", () => Ui.ClosePantry(), "btn--primary"));
        }

        public override void Refresh()
        {
            _grid.Clear();
            var p = S.Profile;
            int kinds = 0, total = 0;
            for (int i = 0; i < p.pantry.Count; i++)
            {
                ItemStack st = p.pantry[i];
                if (st == null || st.count <= 0) continue;
                var item = UIKit.E("pantry-item", "pantry-item--big");
                item.Add(Art.Framed(Art.Pic(ArtLibrary.GetIngredient(st.ingredientId), UIKit.IngEmoji(C, st.ingredientId), "pantry-item__emoji"), "item-frame--sm"));
                item.Add(UIKit.L(UIKit.IngName(C, st.ingredientId), "pantry-item__name"));
                int free = Mathf.Max(0, Meals.Available(C, p, st.ingredientId));
                int inTray = st.count - free;
                item.Add(UIKit.L("×" + free, "pantry-item__count"));
                if (inTray > 0) item.Add(UIKit.L("+" + inTray + " in tray", "pantry-item__reserved"));
                item.EnableInClassList("pantry-item--used", free <= 0);
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
