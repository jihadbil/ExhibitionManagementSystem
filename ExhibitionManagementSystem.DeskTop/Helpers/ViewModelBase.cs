using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;
using ExhibitionManagementSystem.DeskTop.Services.Session;

namespace ExhibitionManagementSystem.DeskTop.Helpers;

/// <summary>
/// الـ Base class لجميع ViewModels في التطبيق.
/// يوفر: IsBusy, Navigation, Notifications, Session
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    protected readonly INavigationService NavigationService;
    protected readonly INotificationService NotificationService;
    protected readonly SessionService Session;

    private bool _isBusy;
    /// <summary>
    /// يُستخدم لإظهار/إخفاء مؤشر التحميل في الـ View
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    private string _title = string.Empty;
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    protected ViewModelBase(
        INavigationService navigationService,
        INotificationService notificationService,
        SessionService session)
    {
        NavigationService = navigationService;
        NotificationService = notificationService;
        Session = session;
    }

    /// <summary>
    /// تنفيذ عملية async مع معالجة الأخطاء تلقائياً وإدارة IsBusy
    /// </summary>
    /// <param name="action">العملية المطلوب تنفيذها</param>
    /// <param name="errorTitle">عنوان رسالة الخطأ (اختياري)</param>
    protected async Task ExecuteSafeAsync(Func<Task> action, string errorTitle = "حدث خطأ")
    {
        try
        {
            IsBusy = true;
            await action();
        }
        catch (Exception ex)
        {
            NotificationService.ShowError(ex.Message, errorTitle);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// يُستدعى عند التنقل إلى هذه الصفحة — override في كل ViewModel
    /// </summary>
    public virtual Task OnNavigatedToAsync() => Task.CompletedTask;

    /// <summary>
    /// يُستدعى عند مغادرة هذه الصفحة
    /// </summary>
    public virtual Task OnNavigatedFromAsync() => Task.CompletedTask;
}
