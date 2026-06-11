using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ExhibitionManagementSystem.DeskTop.Controls.Badges;

public partial class CategoryBadgeControl : UserControl
{
    public static readonly DependencyProperty CategoryProperty =
        DependencyProperty.Register(nameof(Category), typeof(object), typeof(CategoryBadgeControl),
            new PropertyMetadata(null, OnCategoryChanged));

    public object Category
    {
        get => GetValue(CategoryProperty);
        set => SetValue(CategoryProperty, value);
    }

    public CategoryBadgeControl()
    {
        InitializeComponent();
    }

    private static void OnCategoryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (CategoryBadgeControl)d;
        ctrl.UpdateAppearance(e.NewValue?.ToString() ?? string.Empty);
    }

    private void UpdateAppearance(string category)
    {
        var map = new Dictionary<string, (string Text, string BgKey, string FgKey)>
        {
            ["VIP"] = ("VIP", "BgSecondaryBrush", "SecondaryBrush"),
            ["Premium"] = ("مميز", "InfoLightBrush", "InfoTextBrush"),
            ["Corner"] = ("زاوية", "WarningLightBrush", "WarningTextBrush"),
            ["Standard"] = ("قياسي", "BgSecondaryBrush", "TextMutedBrush"),
        };

        if (map.TryGetValue(category, out var info))
        {
            BadgeText.Text = info.Text;
            if (Application.Current != null)
            {
                BadgeBorder.Background = (Brush)Application.Current.Resources[info.BgKey];
                BadgeText.Foreground = (Brush)Application.Current.Resources[info.FgKey];
            }
        }
        else
        {
            BadgeText.Text = category;
            if (Application.Current != null)
            {
                BadgeBorder.Background = (Brush)Application.Current.Resources["BgSecondaryBrush"];
                BadgeText.Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"];
            }
        }
    }
}
