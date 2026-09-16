using System.Windows;
using System.Windows.Controls;
using ExhibitionManagementSystem.DeskTop.ViewModels.Users;

namespace ExhibitionManagementSystem.DeskTop.Controls.Forms
{
    public partial class UserFormControl : UserControl
    {
        public UserFormControl()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                if (DataContext is UserFormViewModel vm)
                {
                    PasswordBoxInput.PasswordChanged += (sender, args) =>
                    {
                        vm.InitialPassword = PasswordBoxInput.Password;
                    };
                }
            };
        }
    }
}
