using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.DeskTop.Helpers;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using ExhibitionManagementSystem.Models.DTOs.Auth;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Users
{
    public partial class UserFormViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;

        // ━━━━━━━━━━━━━━ Collections ━━━━━━━━━━━━━━
        public ObservableCollection<RoleDto> AvailableRoles { get; } = [];

        // ━━━━━━━━━━━━━━ Observable Properties ━━━━━━━━━━━━━━
        [ObservableProperty]
        private string _fullName = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _initialPassword = string.Empty;

        [ObservableProperty]
        private string _selectedRoleName = string.Empty;

        [ObservableProperty]
        private bool _isEditMode = false;

        [ObservableProperty]
        private string _userId = string.Empty;

        public Action? CloseAction { get; set; }

        // ━━━━━━━━━━━━━━ Constructor ━━━━━━━━━━━━━━
        public UserFormViewModel(
            IAuthService authService,
            INavigationService navigationService,
            INotificationService notificationService,
            SessionService session) : base(navigationService, notificationService, session)
        {
            _authService = authService;
            Title = "إنشاء مستخدم جديد";
        }

        // ━━━━━━━━━━━━━━ Methods ━━━━━━━━━━━━━━
        public async Task InitializeAsync(string userId = "")
        {
            UserId = userId;
            IsEditMode = !string.IsNullOrEmpty(userId);
            Title = IsEditMode ? "تعديل مستخدم" : "إنشاء مستخدم جديد";

            FullName = string.Empty;
            Email = string.Empty;
            InitialPassword = string.Empty;
            SelectedRoleName = string.Empty;

            await ExecuteSafeAsync(async () =>
            {
                // Load roles
                var rolesResult = await _authService.GetRolesAsync(Session.TenantId);
                if (rolesResult.IsSuccess && rolesResult.Data is not null)
                {
                    AvailableRoles.Clear();
                    foreach (var role in rolesResult.Data)
                    {
                        AvailableRoles.Add(role);
                    }
                }

                if (IsEditMode)
                {
                    var userResult = await _authService.GetUserByIdAsync(UserId);
                    if (userResult.IsSuccess && userResult.Data is not null)
                    {
                        FullName = userResult.Data.FullName;
                        Email = userResult.Data.Email;
                        SelectedRoleName = userResult.Data.Roles.FirstOrDefault() ?? string.Empty;
                    }
                }
            }, "خطأ في تهيئة النموذج");
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(FullName))
            {
                NotificationService.ShowError("الرجاء إدخال الاسم الكامل");
                return;
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                NotificationService.ShowError("الرجاء إدخال البريد الإلكتروني");
                return;
            }

            if (!IsEditMode && string.IsNullOrWhiteSpace(InitialPassword))
            {
                NotificationService.ShowError("الرجاء إدخال كلمة المرور الأولية");
                return;
            }

            if (!IsEditMode && InitialPassword.Length < 8)
            {
                NotificationService.ShowError("يجب أن تكون كلمة المرور 8 رموز على الأقل");
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedRoleName))
            {
                NotificationService.ShowError("الرجاء اختيار الدور");
                return;
            }

            await ExecuteSafeAsync(async () =>
            {
                if (IsEditMode)
                {
                    // Since IAuthService has no UpdateUserAsync(UserId, dto) for full name or email,
                    // we'll update the role if it has changed.
                    var userResult = await _authService.GetUserByIdAsync(UserId);
                    if (userResult.IsSuccess && userResult.Data is not null)
                    {
                        var oldRole = userResult.Data.Roles.FirstOrDefault();
                        if (oldRole != SelectedRoleName)
                        {
                            if (!string.IsNullOrEmpty(oldRole))
                            {
                                await _authService.RemoveRoleAsync(UserId, oldRole);
                            }
                            await _authService.AssignRoleAsync(new AssignRoleDto { UserId = UserId, RoleName = SelectedRoleName });
                        }
                    }
                    NotificationService.ShowSuccess("تم تحديث دور المستخدم بنجاح ✓");
                    CloseAction?.Invoke();
                }
                else
                {
                    var dto = new UserManagementCreateDto
                    {
                        FullName = FullName,
                        Email = Email,
                        Password = InitialPassword,
                        TenantID = Session.TenantId,
                        IsActive = true,
                        Roles = new System.Collections.Generic.List<string> { SelectedRoleName }
                    };

                    var result = await _authService.CreateUserAsync(Session.TenantId, dto);
                    if (result.IsSuccess && result.Data is not null)
                    {
                        // AssignRoleAsync is called if the CreateUserAsync doesn't assign roles automatically, 
                        // or we assign it anyway to be safe.
                        await _authService.AssignRoleAsync(new AssignRoleDto
                        {
                            UserId = result.Data.UserId,
                            RoleName = SelectedRoleName
                        });

                        NotificationService.ShowSuccess("تم إنشاء المستخدم وتعيين دوره بنجاح ✓");
                        CloseAction?.Invoke();
                    }
                    else
                    {
                        NotificationService.ShowError(result.ErrorMessage ?? "فشل إنشاء المستخدم");
                    }
                }
            }, "خطأ أثناء حفظ المستخدم");
        }

        [RelayCommand]
        private void Cancel()
        {
            CloseAction?.Invoke();
        }
    }
}
