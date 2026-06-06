using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ExhibitionManagementSystem.Desktop.Services.Notifications;

namespace ExhibitionManagementSystem.Desktop.Controls.Notifications
{
    public partial class NotificationContainer : UserControl
    {
        public NotificationContainer()
        {
            InitializeComponent();
        }

        public void AddToast(NotificationType type, string message, string? description)
        {
            var toast = new ToastNotification(type, message, description);
            toast.Dismissed += (s, e) =>
            {
                ToastsStackPanel.Children.Remove(toast);
            };

            ToastsStackPanel.Children.Add(toast);

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                toast.Dismiss();
            };
            timer.Start();
        }
    }
}
