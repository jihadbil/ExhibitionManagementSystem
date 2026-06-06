using System.Windows;
using System.Windows.Controls;
using ExhibitionManagementSystem.Desktop.ViewModels.Auth;

namespace ExhibitionManagementSystem.Desktop.Views.Auth
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = PassBox.Password;
            }
        }

        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("تم إرسال رابط استعادة كلمة المرور إلى بريدك الإلكتروني.", "استعادة كلمة المرور", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
