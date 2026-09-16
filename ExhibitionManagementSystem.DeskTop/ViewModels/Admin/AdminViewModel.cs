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
using ExhibitionManagementSystem.Models.DTOs.Admin;
using ExhibitionManagementSystem.Models.DTOs.Tenant;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Admin
{
    public partial class AdminViewModel : ViewModelBase
    {
        private readonly IAdminService _adminService;
        private readonly ITenantService _tenantService;

        // ━━━━━━━━━━━━━━ Collections ━━━━━━━━━━━━━━
        public ObservableCollection<AuditLogDto> AuditLogs { get; } = [];
        public ObservableCollection<TenantSubscriptionDto> Subscriptions { get; } = [];

        // ━━━━━━━━━━━━━━ Audit Logs Properties ━━━━━━━━━━━━━━
        [ObservableProperty] private string _tableNameFilter = string.Empty;
        [ObservableProperty] private int _auditCurrentPage = 1;
        [ObservableProperty] private int _auditTotalPages;
        [ObservableProperty] private int _auditTotalCount;
        private const int PageSize = 20;

        // ━━━━━━━━━━━━━━ Constructor ━━━━━━━━━━━━━━
        public AdminViewModel(
            IAdminService adminService,
            ITenantService tenantService,
            INavigationService navigationService,
            INotificationService notificationService,
            SessionService session) : base(navigationService, notificationService, session)
        {
            _adminService = adminService;
            _tenantService = tenantService;
            Title = "سجلات النظام والاشتراكات";
        }

        // ━━━━━━━━━━━━━━ Overrides ━━━━━━━━━━━━━━
        public override async Task OnNavigatedToAsync()
        {
            await LoadAuditLogsAsync();
            await LoadSubscriptionsAsync();
        }

        // ━━━━━━━━━━━━━━ Methods / Commands ━━━━━━━━━━━━━━

        [RelayCommand]
        private async Task LoadAuditLogsAsync()
        {
            await ExecuteSafeAsync(async () =>
            {
                var result = await _adminService.GetAuditLogsAsync(Session.TenantId, AuditCurrentPage, PageSize);
                if (result.IsSuccess && result.Data is not null)
                {
                    AuditLogs.Clear();
                    foreach (var log in result.Data.Items)
                    {
                        if (string.IsNullOrWhiteSpace(TableNameFilter) ||
                            log.TableName.Contains(TableNameFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            AuditLogs.Add(log);
                        }
                    }
                    AuditTotalPages = result.Data.TotalPages;
                    AuditTotalCount = result.Data.TotalCount;
                }
            }, "خطأ في تحميل سجل العمليات");
        }

        [RelayCommand]
        private async Task LoadSubscriptionsAsync()
        {
            await ExecuteSafeAsync(async () =>
            {
                var result = await _adminService.GetSubscriptionHistoryAsync(Session.TenantId);
                if (result.IsSuccess && result.Data is not null)
                {
                    Subscriptions.Clear();
                    foreach (var sub in result.Data)
                    {
                        Subscriptions.Add(sub);
                    }
                }
            }, "خطأ في تحميل الاشتراكات");
        }



        [RelayCommand]
        private async Task AuditNextPageAsync()
        {
            if (AuditCurrentPage < AuditTotalPages)
            {
                AuditCurrentPage++;
                await LoadAuditLogsAsync();
            }
        }

        [RelayCommand]
        private async Task AuditPrevPageAsync()
        {
            if (AuditCurrentPage > 1)
            {
                AuditCurrentPage--;
                await LoadAuditLogsAsync();
            }
        }
    }
}
