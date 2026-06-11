using System.Windows;
using System.Windows.Controls;
using ExhibitionManagementSystem.DeskTop.ViewModels.Settings;

namespace ExhibitionManagementSystem.DeskTop.Views.Settings;

public partial class SettingsPage : UserControl
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;

        Loaded += async (s, e) => await ViewModel.OnNavigatedToAsync();
    }

    private async void ChangePassword_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CurrentPassword = CurrentPasswordBox.Password;
        ViewModel.NewPassword = NewPasswordBox.Password;
        ViewModel.ConfirmPassword = ConfirmPasswordBox.Password;

        await ViewModel.ChangePasswordCommand.ExecuteAsync(null);

        // Clear boxes on successful change (if values are reset in VM)
        if (string.IsNullOrEmpty(ViewModel.NewPassword))
        {
            CurrentPasswordBox.Password = string.Empty;
            NewPasswordBox.Password = string.Empty;
            ConfirmPasswordBox.Password = string.Empty;
        }
    }
}
