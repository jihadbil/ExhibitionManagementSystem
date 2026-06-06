using System.Windows;
using System.Windows.Controls;
using ExhibitionManagementSystem.Desktop.ViewModels.Settings;

namespace ExhibitionManagementSystem.Desktop.Views.Settings
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm)
            {
                vm.CurrentPassword = CurrentPasswordBox.Password;
                vm.NewPassword = NewPasswordBox.Password;
            }
        }
    }
}
