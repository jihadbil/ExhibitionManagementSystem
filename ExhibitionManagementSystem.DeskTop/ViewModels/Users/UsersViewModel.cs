using System;
using System.Collections.Generic;
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
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Users
{
    public partial class UsersViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;

        // ━━━━━━━━━━━━━━ Collections ━━━━━━━━━━━━━━
        public ObservableCollection<UserManagementDto> Users { get; } = [];

        // ━━━━━━━━━━━━━━ Observable Properties ━━━━━━━━━━━━━━
        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages;

        [ObservableProperty]
        private int _totalCount;

        private const int PageSize = 15;

        // ━━━━━━━━━━━━━━ Constructor ━━━━━━━━━━━━━━
        public UsersViewModel(
            IAuthService authService,
            INavigationService navigationService,
            INotificationService notificationService,
            SessionService session) : base(navigationService, notificationService, session)
        {
            _authService = authService;
            Title = "إدارة المستخدمين والأدوار";
        }

        // ━━━━━━━━━━━━━━ Methods ━━━━━━━━━━━━━━
        public override async Task OnNavigatedToAsync()
        {
            await LoadUsersAsync();
        }

        [RelayCommand]
        private async Task LoadUsersAsync()
        {
            await ExecuteSafeAsync(async () =>
            {
                var result = await _authService.GetUsersAsync(Session.TenantId, CurrentPage, PageSize);
                if (result.IsSuccess && result.Data is not null)
                {
                    var items = result.Data.Items ?? new List<UserManagementDto>();

                    if (!string.IsNullOrWhiteSpace(SearchQuery))
                    {
                        items = items.Where(u => u.FullName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) || 
                                                 u.Email.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    Users.Clear();
                    foreach (var user in items)
                    {
                        Users.Add(user);
                    }

                    TotalCount = result.Data.TotalCount;
                    TotalPages = result.Data.TotalPages;
                }
            }, "خطأ في تحميل المستخدمين");
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            CurrentPage = 1;
            await LoadUsersAsync();
        }

        [RelayCommand]
        private async Task ToggleStatusAsync((string userId, bool currentIsActive) param)
        {
            await ExecuteSafeAsync(async () =>
            {
                var result = await _authService.UpdateUserStatusAsync(param.userId, !param.currentIsActive);
                if (result.IsSuccess)
                {
                    NotificationService.ShowSuccess("تم تغيير حالة المستخدم بنجاح ✓");
                    await LoadUsersAsync();
                }
                else
                {
                    NotificationService.ShowError(result.ErrorMessage ?? "فشل تعديل حالة المستخدم");
                }
            }, "خطأ في تعديل حالة المستخدم");
        }

        [RelayCommand]
        private async Task DeleteUserAsync(string userId)
        {
            await ExecuteSafeAsync(async () =>
            {
                var result = await _authService.DeleteUserAsync(userId);
                if (result.IsSuccess)
                {
                    NotificationService.ShowSuccess("تم حذف المستخدم بنجاح ✓");
                    await LoadUsersAsync();
                }
                else
                {
                    NotificationService.ShowError(result.ErrorMessage ?? "فشل حذف المستخدم");
                }
            }, "خطأ في حذف المستخدم");
        }

        [RelayCommand]
        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadUsersAsync();
            }
        }

        [RelayCommand]
        private async Task PrevPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadUsersAsync();
            }
        }
    }
}
