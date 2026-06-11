using System;
using System.Windows;
using System.Windows.Controls;
using ExhibitionManagementSystem.DeskTop.Helpers;

namespace ExhibitionManagementSystem.DeskTop.Controls.Cards;

public partial class ExhibitionCardControl : UserControl
{
    public static readonly DependencyProperty ExhibitionIdProperty =
        DependencyProperty.Register(nameof(ExhibitionId), typeof(int), typeof(ExhibitionCardControl));

    public static readonly DependencyProperty ExhibitionNameProperty =
        DependencyProperty.Register(nameof(ExhibitionName), typeof(string), typeof(ExhibitionCardControl));

    public static readonly DependencyProperty TypeProperty =
        DependencyProperty.Register(nameof(Type), typeof(string), typeof(ExhibitionCardControl),
            new PropertyMetadata(string.Empty, OnTypeChanged));

    public static readonly DependencyProperty StartDateProperty =
        DependencyProperty.Register(nameof(StartDate), typeof(DateTime), typeof(ExhibitionCardControl));

    public static readonly DependencyProperty EndDateProperty =
        DependencyProperty.Register(nameof(EndDate), typeof(DateTime), typeof(ExhibitionCardControl));

    public static readonly DependencyProperty VenueNameProperty =
        DependencyProperty.Register(nameof(VenueName), typeof(string), typeof(ExhibitionCardControl));

    public static readonly DependencyProperty StatusProperty =
        DependencyProperty.Register(nameof(Status), typeof(string), typeof(ExhibitionCardControl));

    public int ExhibitionId { get => (int)GetValue(ExhibitionIdProperty); set => SetValue(ExhibitionIdProperty, value); }
    public string ExhibitionName { get => (string)GetValue(ExhibitionNameProperty); set => SetValue(ExhibitionNameProperty, value); }
    public string Type { get => (string)GetValue(TypeProperty); set => SetValue(TypeProperty, value); }
    public DateTime StartDate { get => (DateTime)GetValue(StartDateProperty); set => SetValue(StartDateProperty, value); }
    public DateTime EndDate { get => (DateTime)GetValue(EndDateProperty); set => SetValue(EndDateProperty, value); }
    public string VenueName { get => (string)GetValue(VenueNameProperty); set => SetValue(VenueNameProperty, value); }
    public string Status { get => (string)GetValue(StatusProperty); set => SetValue(StatusProperty, value); }

    public event EventHandler<int>? EditRequested;
    public event EventHandler<int>? DeleteRequested;

    public ExhibitionCardControl()
    {
        InitializeComponent();
    }

    private static void OnTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ExhibitionCardControl card)
        {
            string type = e.NewValue?.ToString() ?? string.Empty;
            card.EmojiText.Text = ExhibitionTypeHelper.GetEmoji(type);
            card.TypeLabel.Text = ExhibitionTypeHelper.GetDisplayName(type);
        }
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        EditRequested?.Invoke(this, ExhibitionId);
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        DeleteRequested?.Invoke(this, ExhibitionId);
    }
}
