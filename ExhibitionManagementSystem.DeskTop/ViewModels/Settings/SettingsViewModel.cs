using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.DeskTop.Helpers;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using ExhibitionManagementSystem.Models.DTOs.Auth;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Settings;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IAuthService _authService;

    // ━━━━━━━━━━━━━━ Profile Properties ━━━━━━━━━━━━━━
    [ObservableProperty] private string _fullName = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _phoneNumber = string.Empty;
    [ObservableProperty] private string _tenantName = string.Empty;
    [ObservableProperty] private string _userRole = "Admin";

    // ━━━━━━━━━━━━━━ Security Properties ━━━━━━━━━━━━━━
    [ObservableProperty] private string _currentPassword = string.Empty;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;

    // ━━━━━━━━━━━━━━ Constructor ━━━━━━━━━━━━━━
    public SettingsViewModel(
        IAuthService authService,
        INavigationService navigationService,
        INotificationService notificationService,
        SessionService session) : base(navigationService, notificationService, session)
    {
        _authService = authService;
        Title = "الإعدادات العامة";
    }

    // ━━━━━━━━━━━━━━ Methods ━━━━━━━━━━━━━━
    public override async Task OnNavigatedToAsync()
    {
        await LoadProfileAsync();
    }

    [RelayCommand]
    private async Task LoadProfileAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            var result = await _authService.GetProfileAsync(Session.UserId);
            if (result.IsSuccess && result.Data is not null)
            {
                var profile = result.Data;
                FullName = profile.FullName;
                Email = profile.Email;
                PhoneNumber = profile.PhoneNumber ?? string.Empty;
                TenantName = profile.TenantName;
                UserRole = profile.Roles.FirstOrDefault() ?? "Admin";
            }
            else
            {
                // Fallback to Session if Service profile load encounters empty DB
                FullName = Session.FullName;
                Email = Session.Email;
                TenantName = Session.TenantName;
                UserRole = Session.Roles.FirstOrDefault() ?? "Admin";
            }
        }, "خطأ في تحميل ملف المستخدم");
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(FullName))
        {
            NotificationService.ShowError("الرجاء إدخال الاسم الكامل");
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            var dto = new UpdateProfileDto
            {
                FullName = FullName,
                PhoneNumber = PhoneNumber
            };

            var result = await _authService.UpdateProfileAsync(Session.UserId, dto);
            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess("تم حفظ تعديلات الملف الشخصي بنجاح ✓");
                await LoadProfileAsync();
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل حفظ الملف الشخصي");
            }
        }, "خطأ أثناء حفظ الملف الشخصي");
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            NotificationService.ShowError("الرجاء إدخال كلمة المرور الحالية");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
        {
            NotificationService.ShowError("كلمة المرور الجديدة يجب ألا تقل عن 6 رموز");
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            NotificationService.ShowError("كلمة المرور الجديدة لا تطابق تأكيد كلمة المرور");
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            var dto = new ChangePasswordDto
            {
                CurrentPassword = CurrentPassword,
                NewPassword = NewPassword,
                ConfirmNewPassword = ConfirmPassword
            };

            var result = await _authService.ChangePasswordAsync(Session.UserId, dto);
            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess("تم تغيير كلمة المرور بنجاح ✓");
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل تغيير كلمة المرور. يرجى التحقق من كلمة المرور الحالية");
            }
        }, "خطأ أثناء تغيير كلمة المرور");
    }
}
