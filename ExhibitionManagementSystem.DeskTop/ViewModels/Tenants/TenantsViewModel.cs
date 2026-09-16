using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.DeskTop.Helpers;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using ExhibitionManagementSystem.Models.DTOs.Tenant;
using ExhibitionManagementSystem.Models.DTOs.Admin;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Tenants
{
    public partial class TenantsViewModel : ViewModelBase
    {
        private readonly ITenantService _tenantService;

        // ━━━━━━━━━━━━━━ Form Properties ━━━━━━━━━━━━━━
        [ObservableProperty] private string _companyName = string.Empty;
        [ObservableProperty] private string _subdomain = string.Empty;
        [ObservableProperty] private string _plan = "Basic";
        [ObservableProperty] private string _baseCurrency = "LYD";
        [ObservableProperty] private DateTime? _trialEndsAt;
        [ObservableProperty] private bool _isActive = true;

        public ObservableCollection<string> Currencies { get; } = new()
        {
            "LYD", "USD", "EUR", "SAR"
        };

        // ━━━━━━━━━━━━━━ Constructor ━━━━━━━━━━━━━━
        public TenantsViewModel(
            ITenantService tenantService,
            INavigationService navigationService,
            INotificationService notificationService,
            SessionService session) : base(navigationService, notificationService, session)
        {
            _tenantService = tenantService;
            Title = "ملف الشركة (Company Profile)";
        }

        // ━━━━━━━━━━━━━━ Overrides ━━━━━━━━━━━━━━
        public override async Task OnNavigatedToAsync()
        {
            await LoadCompanyProfileAsync();
        }

        // ━━━━━━━━━━━━━━ Methods / Commands ━━━━━━━━━━━━━━

        [RelayCommand]
        private async Task LoadCompanyProfileAsync()
        {
            await ExecuteSafeAsync(async () =>
            {
                var result = await _tenantService.GetByIdAsync(Session.TenantId);
                if (result.IsSuccess && result.Data is not null)
                {
                    CompanyName = result.Data.CompanyName;
                    Subdomain = result.Data.Subdomain;
                    Plan = result.Data.CurrentPlan ?? "Basic";
                    BaseCurrency = result.Data.BaseCurrency;
                    TrialEndsAt = result.Data.TrialEndsAt;
                    IsActive = result.Data.IsActive;
                }
            }, "خطأ في تحميل ملف الشركة");
        }

        [RelayCommand]
        private async Task SaveTenantAsync()
        {
            if (string.IsNullOrWhiteSpace(CompanyName))
            {
                NotificationService.ShowError("الرجاء إدخال اسم الشركة");
                return;
            }

            await ExecuteSafeAsync(async () =>
            {
                var dto = new TenantUpdateDto
                {
                    CompanyName = CompanyName,
                    Subdomain = Subdomain,
                    Plan = Plan,
                    BaseCurrency = BaseCurrency,
                    IsActive = IsActive,
                    TrialEndsAt = TrialEndsAt
                };

                var result = await _tenantService.UpdateAsync(Session.TenantId, dto);
                if (result.IsSuccess)
                {
                    NotificationService.ShowSuccess("تم تحديث بيانات ملف الشركة بنجاح ✓");
                    await LoadCompanyProfileAsync();
                }
                else
                {
                    NotificationService.ShowError(result.ErrorMessage ?? "فشل تحديث ملف الشركة");
                }
            }, "خطأ أثناء حفظ ملف الشركة");
        }
    }
}
