namespace ExhibitionManagementSystem.Desktop.Services.Notifications
{
    public enum NotificationType
    {
        Success,
        Error,
        Warning,
        Info
    }

    public interface INotificationService
    {
        void ShowSuccess(string message, string? description = null);
        void ShowError(string message, string? description = null);
        void ShowWarning(string message, string? description = null);
        void ShowInfo(string message, string? description = null);
    }
}
