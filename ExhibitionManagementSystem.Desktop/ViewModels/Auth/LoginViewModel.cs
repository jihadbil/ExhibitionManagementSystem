using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.Desktop.Services.Navigation;
using ExhibitionManagementSystem.Desktop.Services.Notifications;
using ExhibitionManagementSystem.Desktop.ViewModels.Base;
using ExhibitionManagementSystem.Desktop.ViewModels.Dashboard;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.Desktop.Services.Auth;
using ExhibitionManagementSystem.Models.DTOs.Auth;

namespace ExhibitionManagementSystem.Desktop.ViewModels.Auth
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly ISessionService _sessionService;

        [ObservableProperty]
        private string _email = "admin@example.com";

        [ObservableProperty]
        private string _password = "Admin@123";

        public LoginViewModel(
            INavigationService navigationService,
            INotificationService notificationService,
            IAuthService authService,
            ISessionService sessionService)
            : base(navigationService, notificationService)
        {
            _authService = authService;
            _sessionService = sessionService;
            Title = "تسجيل الدخول";
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            IsLoading = true;
            ErrorMessage = null;

            try
            {
                var result = await _authService.LoginAsync(new LoginRequestDto
                {
                    Email = Email,
                    Password = Password
                });

                if (result.IsSuccess && result.Data != null)
                {
                    _sessionService.SetSession(result.Data);
                    NotificationService.ShowSuccess("تم تسجيل الدخول بنجاح!");
                    NavigationService.NavigateTo<DashboardViewModel>();
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "بريد إلكتروني أو كلمة مرور غير صحيحة";
                    NotificationService.ShowError(ErrorMessage);
                }
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"فشل تسجيل الدخول: {ex.Message}";
                NotificationService.ShowError(ErrorMessage);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ForgotPasswordAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                NotificationService.ShowWarning("يرجى إدخال البريد الإلكتروني أولاً");
                return;
            }

            var success = await ExecuteServiceAsync(() => 
                _authService.ForgotPasswordAsync(new ResetPasswordRequestDto { Email = Email }), 
                "فشل إرسال طلب إعادة تعيين كلمة المرور");

            if (success)
            {
                NotificationService.ShowSuccess("إذا كان البريد الإلكتروني مسجلاً، فقد تم إرسال طلب إعادة التعيين.");
            }
        }
    }
}

