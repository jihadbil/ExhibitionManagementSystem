using System;

namespace ExhibitionManagementSystem.DeskTop.Services.Notifications;

public class NotificationEventArgs : EventArgs
{
    public string Message { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
}

public enum NotificationType { Success, Error, Warning, Info }
