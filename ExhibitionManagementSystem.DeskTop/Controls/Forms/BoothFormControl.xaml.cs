using System.Windows;
using System.Windows.Controls;

namespace ExhibitionManagementSystem.DeskTop.Controls.Forms
{
    public partial class BoothFormControl : UserControl
    {
        public BoothFormControl()
        {
            InitializeComponent();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            window?.Close();
        }
    }
}
