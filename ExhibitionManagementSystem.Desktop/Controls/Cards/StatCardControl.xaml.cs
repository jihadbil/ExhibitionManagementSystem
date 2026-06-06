using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace ExhibitionManagementSystem.Desktop.Controls.Cards
{
    public partial class StatCardControl : UserControl
    {
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(StatCardControl), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(string), typeof(StatCardControl), new PropertyMetadata("0"));

        public static readonly DependencyProperty IconKindProperty =
            DependencyProperty.Register("IconKind", typeof(PackIconKind), typeof(StatCardControl), new PropertyMetadata(PackIconKind.ViewDashboard));

        public static readonly DependencyProperty IconForegroundProperty =
            DependencyProperty.Register("IconForeground", typeof(Brush), typeof(StatCardControl), new PropertyMetadata(null));

        public static readonly DependencyProperty IconBackgroundProperty =
            DependencyProperty.Register("IconBackground", typeof(Brush), typeof(StatCardControl), new PropertyMetadata(null));

        public static readonly DependencyProperty TrendValueProperty =
            DependencyProperty.Register("TrendValue", typeof(string), typeof(StatCardControl), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty TrendDirectionProperty =
            DependencyProperty.Register("TrendDirection", typeof(string), typeof(StatCardControl), new PropertyMetadata("Up", OnTrendDirectionChanged));

        public static readonly DependencyProperty TrendBrushProperty =
            DependencyProperty.Register("TrendBrush", typeof(Brush), typeof(StatCardControl), new PropertyMetadata(null));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public PackIconKind IconKind
        {
            get => (PackIconKind)GetValue(IconKindProperty);
            set => SetValue(IconKindProperty, value);
        }

        public Brush IconForeground
        {
            get => (Brush)GetValue(IconForegroundProperty);
            set => SetValue(IconForegroundProperty, value);
        }

        public Brush IconBackground
        {
            get => (Brush)GetValue(IconBackgroundProperty);
            set => SetValue(IconBackgroundProperty, value);
        }

        public string TrendValue
        {
            get => (string)GetValue(TrendValueProperty);
            set => SetValue(TrendValueProperty, value);
        }

        public string TrendDirection
        {
            get => (string)GetValue(TrendDirectionProperty);
            set => SetValue(TrendDirectionProperty, value);
        }

        public Brush TrendBrush
        {
            get => (Brush)GetValue(TrendBrushProperty);
            set => SetValue(TrendBrushProperty, value);
        }

        public StatCardControl()
        {
            InitializeComponent();
            
            // Set defaults
            if (IconForeground == null)
            {
                IconForeground = Application.Current.TryFindResource("BrushPrimary") as Brush ?? Brushes.Indigo;
            }
            if (IconBackground == null)
            {
                IconBackground = Application.Current.TryFindResource("BrushBgSecondary") as Brush ?? Brushes.LightGray;
            }
            UpdateTrendBrush(TrendDirection);
        }

        private static void OnTrendDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StatCardControl card)
            {
                card.UpdateTrendBrush((string)e.NewValue);
            }
        }

        private void UpdateTrendBrush(string direction)
        {
            if (string.Equals(direction, "Up", StringComparison.OrdinalIgnoreCase))
            {
                TrendBrush = Application.Current.TryFindResource("BrushSuccessText") as Brush ?? Brushes.Green;
            }
            else
            {
                TrendBrush = Application.Current.TryFindResource("BrushDangerText") as Brush ?? Brushes.Red;
            }
        }
    }
}
