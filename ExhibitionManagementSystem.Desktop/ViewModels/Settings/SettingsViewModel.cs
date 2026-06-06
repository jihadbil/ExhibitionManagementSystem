using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.Desktop.Services.Navigation;
using ExhibitionManagementSystem.Desktop.Services.Notifications;
using ExhibitionManagementSystem.Desktop.Services.Theme;
using ExhibitionManagementSystem.Desktop.ViewModels.Base;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.Desktop.Services.Auth;
using ExhibitionManagementSystem.Models.DTOs.Auth;

namespace ExhibitionManagementSystem.Desktop.ViewModels.Settings
{
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly IThemeService _themeService;
        private readonly IAuthService _authService;
        private readonly ISessionService _sessionService;

        [ObservableProperty]
        private string _organizationName = "ExpoManager Demo";

        [ObservableProperty]
        private string _userName = "Admin";

        [ObservableProperty]
        private string _email = "admin@expo.com";

        [ObservableProperty]
        private string _currentPassword = string.Empty;

        [ObservableProperty]
        private string _newPassword = string.Empty;

        [ObservableProperty]
        private bool _isDarkMode;

        [ObservableProperty]
        private string _selectedLanguage = "عربي";

        [ObservableProperty]
        private string _selectedCurrency = "SAR";

        [ObservableProperty]
        private string _selectedTimezone = "Asia/Riyadh";

        public List<string> Languages { get; } = new() { "عربي", "English" };
        public List<string> Currencies { get; } = new() { "SAR", "USD", "EUR" };

        public SettingsViewModel(
            INavigationService navigationService, 
            INotificationService notificationService,
            IThemeService themeService,
            IAuthService authService,
            ISessionService sessionService)
            : base(navigationService, notificationService)
        {
            _themeService = themeService;
            _authService = authService;
            _sessionService = sessionService;
            Title = "الإعدادات";
            
            IsDarkMode = _themeService.CurrentTheme == AppTheme.Dark;
        }

        public override async Task InitializeAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                var userId = _sessionService.UserId;
                if (!string.IsNullOrEmpty(userId))
                {
                    var result = await _authService.GetProfileAsync(userId);
                    if (result.IsSuccess && result.Data != null)
                    {
                        UserName = result.Data.FullName;
                        Email = result.Data.Email;
                        OrganizationName = result.Data.TenantName;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"فشل تحميل الإعدادات: {ex.Message}";
                NotificationService.ShowError(ErrorMessage);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SaveAccountSettingsAsync()
        {
            var userId = _sessionService.UserId;
            if (string.IsNullOrEmpty(userId)) return;

            var dto = new UpdateProfileDto
            {
                FullName = UserName,
                PhoneNumber = "+966 50 111 2222"
            };

            var result = await ExecuteServiceAsync(() => _authService.UpdateProfileAsync(userId, dto), "فشل تعديل الملف الشخصي");
            if (result != null)
            {
                UserName = result.FullName;
                NotificationService.ShowSuccess("تم حفظ التغييرات بنجاح!");
            }
        }

        [RelayCommand]
        private async Task ChangePasswordAsync()
        {
            var userId = _sessionService.UserId;
            if (string.IsNullOrEmpty(userId)) return;

            if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword))
            {
                NotificationService.ShowWarning("الرجاء إدخال كلمة المرور الحالية والجديدة.");
                return;
            }

            var dto = new ChangePasswordDto
            {
                CurrentPassword = CurrentPassword,
                NewPassword = NewPassword,
                ConfirmNewPassword = NewPassword
            };

            var success = await ExecuteServiceAsync(() => _authService.ChangePasswordAsync(userId, dto), "فشل تغيير كلمة المرور");
            if (success)
            {
                NotificationService.ShowSuccess("تم تغيير كلمة المرور بنجاح!");
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
            }
        }

        [RelayCommand]
        private void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
            var targetTheme = IsDarkMode ? AppTheme.Dark : AppTheme.Light;
            _themeService.SwitchTheme(targetTheme);
            
            string themeName = IsDarkMode ? "الداكن" : "الفاتح";
            NotificationService.ShowSuccess($"تم تبديل المظهر للوضع {themeName}!");
        }
    }
}
