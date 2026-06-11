using System.Windows;
using ExhibitionManagementSystem.DeskTop.ViewModels.Auth;

namespace ExhibitionManagementSystem.DeskTop.Views.Auth;

public partial class LoginWindow : Window
{
    public LoginViewModel ViewModel { get; }

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;
    }

    private void PasswordField_PasswordChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.Password = PasswordField.Password;
    }
}
