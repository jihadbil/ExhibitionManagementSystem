using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;

namespace ExhibitionManagementSystem.Desktop.Controls.Cards
{
    public partial class ExhibitionCardControl : UserControl
    {
        public static readonly DependencyProperty ExhibitionNameProperty =
            DependencyProperty.Register("ExhibitionName", typeof(string), typeof(ExhibitionCardControl), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ExhibitionTypeProperty =
            DependencyProperty.Register("ExhibitionType", typeof(string), typeof(ExhibitionCardControl), new PropertyMetadata("Tech", OnExhibitionTypeChanged));

        public static readonly DependencyProperty ExhibitionTypeIconProperty =
            DependencyProperty.Register("ExhibitionTypeIcon", typeof(PackIconKind), typeof(ExhibitionCardControl), new PropertyMetadata(PackIconKind.Laptop));

        public static readonly DependencyProperty DateRangeProperty =
            DependencyProperty.Register("DateRange", typeof(string), typeof(ExhibitionCardControl), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty LocationProperty =
            DependencyProperty.Register("Location", typeof(string), typeof(ExhibitionCardControl), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty VisitorsCountProperty =
            DependencyProperty.Register("VisitorsCount", typeof(int), typeof(ExhibitionCardControl), new PropertyMetadata(0));

        public static readonly DependencyProperty BoothsCountProperty =
            DependencyProperty.Register("BoothsCount", typeof(int), typeof(ExhibitionCardControl), new PropertyMetadata(0));

        public static readonly DependencyProperty OccupancyPercentProperty =
            DependencyProperty.Register("OccupancyPercent", typeof(double), typeof(ExhibitionCardControl), new PropertyMetadata(0.0));

        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register("Status", typeof(string), typeof(ExhibitionCardControl), new PropertyMetadata("Active"));

        public static readonly DependencyProperty EditCommandProperty =
            DependencyProperty.Register("EditCommand", typeof(ICommand), typeof(ExhibitionCardControl), new PropertyMetadata(null));

        public static readonly DependencyProperty DeleteCommandProperty =
            DependencyProperty.Register("DeleteCommand", typeof(ICommand), typeof(ExhibitionCardControl), new PropertyMetadata(null));

        public string ExhibitionName
        {
            get => (string)GetValue(ExhibitionNameProperty);
            set => SetValue(ExhibitionNameProperty, value);
        }

        public string ExhibitionType
        {
            get => (string)GetValue(ExhibitionTypeProperty);
            set => SetValue(ExhibitionTypeProperty, value);
        }

        public PackIconKind ExhibitionTypeIcon
        {
            get => (PackIconKind)GetValue(ExhibitionTypeIconProperty);
            set => SetValue(ExhibitionTypeIconProperty, value);
        }

        public string DateRange
        {
            get => (string)GetValue(DateRangeProperty);
            set => SetValue(DateRangeProperty, value);
        }

        public string Location
        {
            get => (string)GetValue(LocationProperty);
            set => SetValue(LocationProperty, value);
        }

        public int VisitorsCount
        {
            get => (int)GetValue(VisitorsCountProperty);
            set => SetValue(VisitorsCountProperty, value);
        }

        public int BoothsCount
        {
            get => (int)GetValue(BoothsCountProperty);
            set => SetValue(BoothsCountProperty, value);
        }

        public double OccupancyPercent
        {
            get => (double)GetValue(OccupancyPercentProperty);
            set => SetValue(OccupancyPercentProperty, value);
        }

        public string Status
        {
            get => (string)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        public ICommand EditCommand
        {
            get => (ICommand)GetValue(EditCommandProperty);
            set => SetValue(EditCommandProperty, value);
        }

        public ICommand DeleteCommand
        {
            get => (ICommand)GetValue(DeleteCommandProperty);
            set => SetValue(DeleteCommandProperty, value);
        }

        public ExhibitionCardControl()
        {
            InitializeComponent();
            UpdateIcon(ExhibitionType);
        }

        private static void OnExhibitionTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExhibitionCardControl control)
            {
                control.UpdateIcon((string)e.NewValue);
            }
        }

        private void UpdateIcon(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                ExhibitionTypeIcon = PackIconKind.HelpCircleOutline;
                return;
            }

            switch (type.Trim())
            {
                case "Tech":
                case "تقني":
                    ExhibitionTypeIcon = PackIconKind.Laptop;
                    break;
                case "Medical":
                case "طبي":
                    ExhibitionTypeIcon = PackIconKind.HeartPulse;
                    break;
                case "Industrial":
                case "صناعي":
                    ExhibitionTypeIcon = PackIconKind.Factory;
                    break;
                case "Commercial":
                case "تجاري":
                    ExhibitionTypeIcon = PackIconKind.StoreOutline;
                    break;
                case "Educational":
                case "تعليمي":
                    ExhibitionTypeIcon = PackIconKind.School;
                    break;
                case "Automotive":
                case "سيارات":
                    ExhibitionTypeIcon = PackIconKind.Car;
                    break;
                default:
                    ExhibitionTypeIcon = PackIconKind.HelpCircleOutline;
                    break;
            }
        }
    }
}
