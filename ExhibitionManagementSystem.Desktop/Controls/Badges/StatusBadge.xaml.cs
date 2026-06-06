using System.Windows;
using System.Windows.Controls;

namespace ExhibitionManagementSystem.Desktop.Controls.Badges
{
    public partial class StatusBadge : UserControl
    {
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register("Status", typeof(string), typeof(StatusBadge), new PropertyMetadata(string.Empty, OnStatusChanged));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(StatusBadge), new PropertyMetadata(string.Empty));

        public string Status
        {
            get => (string)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public StatusBadge()
        {
            InitializeComponent();
        }

        private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StatusBadge badge)
            {
                if (string.IsNullOrEmpty(badge.Text) || badge.Text == (string)e.OldValue)
                {
                    badge.Text = TranslateStatus((string)e.NewValue);
                }
            }
        }

        private static string TranslateStatus(string status)
        {
            switch (status)
            {
                case "Active": return "نشط";
                case "Available": return "متاح";
                case "Reserved": return "محجوز";
                case "UnderReview": return "قيد المراجعة";
                case "Soon": return "قريباً";
                case "Ended": return "منتهي";
                case "Cancelled": return "ملغى";
                default: return status;
            }
        }
    }
}
