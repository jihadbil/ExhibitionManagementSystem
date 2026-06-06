using System.Windows;
using System.Windows.Controls;

namespace ExhibitionManagementSystem.Desktop.Controls.Cards
{
    public partial class BoothCardControl : UserControl
    {
        public static readonly DependencyProperty BoothIdProperty =
            DependencyProperty.Register("BoothId", typeof(string), typeof(BoothCardControl), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty CategoryProperty =
            DependencyProperty.Register("Category", typeof(string), typeof(BoothCardControl), new PropertyMetadata("Standard"));

        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register("Status", typeof(string), typeof(BoothCardControl), new PropertyMetadata("Available"));

        public static readonly DependencyProperty PriceProperty =
            DependencyProperty.Register("Price", typeof(decimal), typeof(BoothCardControl), new PropertyMetadata(0m));

        public string BoothId
        {
            get => (string)GetValue(BoothIdProperty);
            set => SetValue(BoothIdProperty, value);
        }

        public string Category
        {
            get => (string)GetValue(CategoryProperty);
            set => SetValue(CategoryProperty, value);
        }

        public string Status
        {
            get => (string)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        public decimal Price
        {
            get => (decimal)GetValue(PriceProperty);
            set => SetValue(PriceProperty, value);
        }

        public BoothCardControl()
        {
            InitializeComponent();
        }
    }
}
