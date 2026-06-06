using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ExhibitionManagementSystem.Desktop.Controls.Badges
{
    public partial class CategoryBadge : UserControl
    {
        public static readonly DependencyProperty CategoryProperty =
            DependencyProperty.Register("Category", typeof(string), typeof(CategoryBadge), new PropertyMetadata(string.Empty, OnCategoryChanged));

        public string Category
        {
            get => (string)GetValue(CategoryProperty);
            set => SetValue(CategoryProperty, value);
        }

        public CategoryBadge()
        {
            InitializeComponent();
            UpdateBadgeColors("Standard");
        }

        private static void OnCategoryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CategoryBadge badge)
            {
                badge.UpdateBadgeColors((string)e.NewValue);
            }
        }

        private void UpdateBadgeColors(string category)
        {
            if (string.IsNullOrEmpty(category))
                category = "Standard";

            Brush bgBrush;
            Brush fgBrush;

            if (string.Equals(category, "VIP", StringComparison.OrdinalIgnoreCase))
            {
                bgBrush = Application.Current.TryFindResource("BrushVipBg") as Brush ?? new SolidColorBrush(Color.FromRgb(243, 232, 255));
                fgBrush = Application.Current.TryFindResource("BrushVipText") as Brush ?? new SolidColorBrush(Color.FromRgb(124, 58, 237));
            }
            else if (string.Equals(category, "Premium", StringComparison.OrdinalIgnoreCase) || string.Equals(category, "مميز", StringComparison.OrdinalIgnoreCase))
            {
                bgBrush = Application.Current.TryFindResource("BrushWarningBg") as Brush ?? new SolidColorBrush(Color.FromRgb(254, 243, 199));
                fgBrush = Application.Current.TryFindResource("BrushWarningText") as Brush ?? new SolidColorBrush(Color.FromRgb(217, 119, 6));
            }
            else
            {
                bgBrush = Application.Current.TryFindResource("BrushEndedBg") as Brush ?? new SolidColorBrush(Color.FromRgb(243, 244, 246));
                fgBrush = Application.Current.TryFindResource("BrushEndedText") as Brush ?? new SolidColorBrush(Color.FromRgb(107, 114, 128));
            }

            BadgeBorder.Background = bgBrush;
            BadgeText.Foreground = fgBrush;
        }
    }
}
