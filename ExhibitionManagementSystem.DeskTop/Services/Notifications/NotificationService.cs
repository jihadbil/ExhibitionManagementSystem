using System;

namespace ExhibitionManagementSystem.DeskTop.Services.Notifications;

public class NotificationService : INotificationService
{
    public event EventHandler<NotificationEventArgs>? NotificationRequested;

    public void ShowSuccess(string message, string title = "نجاح")
        => Notify(message, title, NotificationType.Success);

    public void ShowError(string message, string title = "خطأ")
        => Notify(message, title, NotificationType.Error);

    public void ShowWarning(string message, string title = "تحذير")
        => Notify(message, title, NotificationType.Warning);

    public void ShowInfo(string message, string title = "معلومة")
        => Notify(message, title, NotificationType.Info);

    private void Notify(string message, string title, NotificationType type)
    {
        NotificationRequested?.Invoke(this, new NotificationEventArgs
        {
            Message = message,
            Title = title,
            Type = type
        });
    }
}
