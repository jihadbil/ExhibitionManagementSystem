using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.Models.DTOs.Auth;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;
using ExhibitionManagementSystem.DeskTop.Views.Shell;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Auth;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly SessionService _session;
    private readonly INotificationService _notificationService;

    // ━━━━━━━━━━━━━━ Properties ━━━━━━━━━━━━━━

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _email = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _password = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError = false;

    // ━━━━━━━━━━━━━━ Constructor ━━━━━━━━━━━━━━

    public LoginViewModel(
        IAuthService authService,
        SessionService session,
        INotificationService notificationService)
    {
        _authService = authService;
        _session = session;
        _notificationService = notificationService;
    }

    // ━━━━━━━━━━━━━━ Commands ━━━━━━━━━━━━━━

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _authService.LoginAsync(new LoginRequestDto
            {
                Email = Email,
                Password = Password,
                RememberMe = false  // لا Remember Me حسب المتطلبات
            });

            if (result.IsSuccess && result.Data is not null)
            {
                // تخزين بيانات الجلسة
                _session.SetSession(result.Data);

                // فتح MainShell + إغلاق LoginWindow
                var shellWindow = App.Services.GetRequiredService<MainShellWindow>();
                shellWindow.Show();

                // إغلاق نافذة Login الحالية
                App.Current.Windows[0]?.Close(); 
            }
            else
            {
                HasError = true;
                ErrorMessage = result.ErrorMessage ?? "بيانات الدخول غير صحيحة";
            }
        }
        catch (System.Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanLogin() =>
        !string.IsNullOrWhiteSpace(Email) &&
        !string.IsNullOrWhiteSpace(Password) &&
        !IsLoading;

    [RelayCommand]
    private void ForgotPassword()
    {
        _notificationService.ShowInfo("ميزة استعادة كلمة المرور قيد التطوير");
    }
}
