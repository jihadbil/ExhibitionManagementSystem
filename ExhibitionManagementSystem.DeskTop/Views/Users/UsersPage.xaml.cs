using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ExhibitionManagementSystem.DeskTop.ViewModels.Users;
using ExhibitionManagementSystem.Models.DTOs.Auth;

namespace ExhibitionManagementSystem.DeskTop.Views.Users
{
    public partial class UsersPage : UserControl
    {
        public UsersViewModel ViewModel { get; }

        public UsersPage(UsersViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel;

            Loaded += async (s, e) => await ViewModel.OnNavigatedToAsync();
        }

        private async void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var formVm = App.Services.GetRequiredService<UserFormViewModel>();
            await formVm.InitializeAsync("");
            var ctrl = new Controls.Forms.UserFormControl { DataContext = formVm };
            var dialog = new Controls.Dialogs.FormDialog(ctrl, "إنشاء مستخدم جديد") 
            { 
                Owner = Window.GetWindow(this) 
            };
            formVm.CloseAction = () => dialog.Close();
            dialog.ShowDialog();
            await ViewModel.LoadUsersCommand.ExecuteAsync(null);
        }

        private async void ToggleStatus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is UserManagementDto user)
            {
                await ViewModel.ToggleStatusCommand.ExecuteAsync((user.UserId, user.IsActive));
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string userId)
            {
                var r = MessageBox.Show("هل أنت متأكد من حذف هذا المستخدم؟", "تأكيد الحذف",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning,
                    MessageBoxResult.No, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                if (r == MessageBoxResult.Yes)
                {
                    await ViewModel.DeleteUserCommand.ExecuteAsync(userId);
                }
            }
        }
    }
}
