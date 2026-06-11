using System;

namespace ExhibitionManagementSystem.DeskTop.Services.Notifications;

public interface INotificationService
{
    void ShowSuccess(string message, string title = "نجاح");
    void ShowError(string message, string title = "خطأ");
    void ShowWarning(string message, string title = "تحذير");
    void ShowInfo(string message, string title = "معلومة");
    event EventHandler<NotificationEventArgs> NotificationRequested;
}
