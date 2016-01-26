// --------------------------------------------------
// 3DS Theme Editor - CursorSetViewModel.cs
// --------------------------------------------------

using System.Windows.Media;

using ThemeEditor.Common.Themes.ColorSets;
using ThemeEditor.WPF.Localization;

namespace ThemeEditor.WPF.Themes.ColorSets
{
    public sealed class CursorSetViewModel : ViewModelBase
    {
        [Order(3)]
        [DisplayName("Theme_Sets_Cursor_Glow", typeof(ThemeResources))]
        [Description("Theme_Sets_Cursor_Glow_Desc", typeof(ThemeResources))]
        public Color Glow
        {
            get { return Model.Glow.ToMediaColor(); }
            set
            {
                var oldValue = Model.Glow;
                var newValue = value.ToColorRgb888();
                if (oldValue == newValue)
                    return;
                Model.Glow = newValue;
                RaiseViewModelChanged(nameof(Glow), oldValue, value);
            }
        }

        [Order(1)]
        [DisplayName("Theme_Sets_Cursor_Main", typeof(ThemeResources))]
        [Description("Theme_Sets_Cursor_Main_Desc", typeof(ThemeResources))]
        public Color Main
        {
            get { return Model.Main.ToMediaColor(); }
            set
            {
                var oldValue = Model.Main;
                var newValue = value.ToColorRgb888();
                if (oldValue == newValue)
                    return;
                Model.Main = newValue;
                RaiseViewModelChanged(nameof(Main), oldValue, value);
            }
        }

        private new CursorSet Model => (CursorSet) base.Model;

        [Order(0)]
        [DisplayName("Theme_Sets_Cursor_Shading", typeof(ThemeResources))]
        [Description("Theme_Sets_Cursor_Shading_Desc", typeof(ThemeResources))]
        public Color Shading
        {
            get { return Model.Shading.ToMediaColor(); }
            set
            {
                var oldValue = Model.Shading;
                var newValue = value.ToColorRgb888();
                if (oldValue == newValue)
                    return;
                Model.Shading = newValue;
                RaiseViewModelChanged(nameof(Shading), oldValue, value);
            }
        }

        public CursorSetViewModel(CursorSet model) : base(model) {}
    }
}