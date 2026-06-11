using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ExhibitionManagementSystem.DeskTop.Controls.Cards;

public partial class StatCardControl : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(StatCardControl),
            new PropertyMetadata(string.Empty, OnTitleChanged));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(StatCardControl),
            new PropertyMetadata("0", OnValueChanged));

    public static readonly DependencyProperty TrendTextProperty =
        DependencyProperty.Register(nameof(TrendText), typeof(string), typeof(StatCardControl),
            new PropertyMetadata(string.Empty, OnTrendTextChanged));

    public static readonly DependencyProperty TrendIsPositiveProperty =
        DependencyProperty.Register(nameof(TrendIsPositive), typeof(bool), typeof(StatCardControl),
            new PropertyMetadata(true, OnTrendIsPositiveChanged));

    public static readonly DependencyProperty IconBackgroundProperty =
        DependencyProperty.Register(nameof(IconBackground), typeof(Brush), typeof(StatCardControl),
            new PropertyMetadata(null, OnIconBackgroundChanged));

    public static readonly DependencyProperty IconTextProperty =
        DependencyProperty.Register(nameof(IconText), typeof(string), typeof(StatCardControl),
            new PropertyMetadata("📊", OnIconTextChanged));

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Value { get => (string)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public string TrendText { get => (string)GetValue(TrendTextProperty); set => SetValue(TrendTextProperty, value); }
    public bool TrendIsPositive { get => (bool)GetValue(TrendIsPositiveProperty); set => SetValue(TrendIsPositiveProperty, value); }
    public Brush IconBackground { get => (Brush)GetValue(IconBackgroundProperty); set => SetValue(IconBackgroundProperty, value); }
    public string IconText { get => (string)GetValue(IconTextProperty); set => SetValue(IconTextProperty, value); }

    public StatCardControl()
    {
        InitializeComponent();
    }

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatCardControl card) card.TitleLabel.Text = e.NewValue?.ToString();
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatCardControl card) card.ValueLabel.Text = e.NewValue?.ToString();
    }

    private static void OnTrendTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatCardControl card) card.TrendTextLabel.Text = e.NewValue?.ToString();
    }

    private static void OnTrendIsPositiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatCardControl card && e.NewValue is bool isPositive)
        {
            if (isPositive)
            {
                card.TrendIcon.Text = "▲";
                if (Application.Current != null)
                {
                    card.TrendIcon.Foreground = (Brush)Application.Current.Resources["SuccessBrush"];
                    card.TrendTextLabel.Foreground = (Brush)Application.Current.Resources["SuccessBrush"];
                }
            }
            else
            {
                card.TrendIcon.Text = "▼";
                if (Application.Current != null)
                {
                    card.TrendIcon.Foreground = (Brush)Application.Current.Resources["DangerBrush"];
                    card.TrendTextLabel.Foreground = (Brush)Application.Current.Resources["DangerBrush"];
                }
            }
        }
    }

    private static void OnIconBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatCardControl card && e.NewValue is Brush brush)
        {
            card.IconBorder.Background = brush;
        }
    }

    private static void OnIconTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatCardControl card) card.IconLabel.Text = e.NewValue?.ToString();
    }
}
