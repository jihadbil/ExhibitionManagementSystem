using System;
using System.Windows;

namespace ExhibitionManagementSystem.Desktop.Services.Notifications
{
    public class NotificationService : INotificationService
    {
        private static ExhibitionManagementSystem.Desktop.Controls.Notifications.NotificationContainer? _container;

        public static void RegisterContainer(ExhibitionManagementSystem.Desktop.Controls.Notifications.NotificationContainer container)
        {
            _container = container;
        }

        public void ShowSuccess(string message, string? description = null) => Show(NotificationType.Success, message, description);
        public void ShowError(string message, string? description = null) => Show(NotificationType.Error, message, description);
        public void ShowWarning(string message, string? description = null) => Show(NotificationType.Warning, message, description);
        public void ShowInfo(string message, string? description = null) => Show(NotificationType.Info, message, description);

        private void Show(NotificationType type, string message, string? description)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_container != null)
                {
                    _container.AddToast(type, message, description);
                }
                else
                {
                    string title = type switch
                    {
                        NotificationType.Success => "نجاح",
                        NotificationType.Error => "خطأ",
                        NotificationType.Warning => "تحذير",
                        _ => "معلومات"
                    };
                    MessageBoxImage img = type switch
                    {
                        NotificationType.Success => MessageBoxImage.Information,
                        NotificationType.Error => MessageBoxImage.Error,
                        NotificationType.Warning => MessageBoxImage.Warning,
                        _ => MessageBoxImage.Information
                    };
                    MessageBox.Show($"{message}\n{description}", title, MessageBoxButton.OK, img);
                }
            });
        }
    }
}
