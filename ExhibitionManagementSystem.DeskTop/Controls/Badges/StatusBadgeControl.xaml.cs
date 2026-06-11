using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ExhibitionManagementSystem.DeskTop.Controls.Badges;

public partial class StatusBadgeControl : UserControl
{
    public static readonly DependencyProperty StatusProperty =
        DependencyProperty.Register(nameof(Status), typeof(object), typeof(StatusBadgeControl),
            new PropertyMetadata(null, OnStatusChanged));

    public object Status
    {
        get => GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public StatusBadgeControl()
    {
        InitializeComponent();
    }

    private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (StatusBadgeControl)d;
        ctrl.UpdateAppearance(e.NewValue?.ToString() ?? string.Empty);
    }

    private void UpdateAppearance(string status)
    {
        // Map ExhibitionStatus or BoothStatus to Arabic text and theme brushes
        var map = new Dictionary<string, (string Text, string BgKey, string FgKey)>
        {
            ["Open"] = ("نشط", "SuccessLightBrush", "SuccessTextBrush"),
            ["Active"] = ("نشط", "SuccessLightBrush", "SuccessTextBrush"),
            ["Upcoming"] = ("قادم", "InfoLightBrush", "InfoTextBrush"),
            ["Ended"] = ("منتهي", "BgSecondaryBrush", "TextMutedBrush"),
            ["Closed"] = ("منتهي", "BgSecondaryBrush", "TextMutedBrush"),
            ["Pending"] = ("قيد المراجعة", "WarningLightBrush", "WarningTextBrush"),
            ["Cancelled"] = ("ملغي", "DangerLightBrush", "DangerTextBrush"),
            ["Available"] = ("متاح", "SuccessLightBrush", "SuccessTextBrush"),
            ["Reserved"] = ("محجوز", "InfoLightBrush", "InfoTextBrush"),
        };

        if (map.TryGetValue(status, out var info))
        {
            StatusText.Text = info.Text;
            if (Application.Current != null)
            {
                StatusBorder.Background = (Brush)Application.Current.Resources[info.BgKey];
                StatusText.Foreground = (Brush)Application.Current.Resources[info.FgKey];
            }
        }
        else
        {
            StatusText.Text = status;
            if (Application.Current != null)
            {
                StatusBorder.Background = (Brush)Application.Current.Resources["BgSecondaryBrush"];
                StatusText.Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"];
            }
        }
    }
}
