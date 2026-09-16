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
using ExhibitionManagementSystem.Models.DTOs.Service;
using ExhibitionManagementSystem.Models.DTOs.Pricing;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.Models.DTOs.Currency;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.ServiceMgmt
{
    public partial class ServicesViewModel : ViewModelBase
    {
        private readonly IServiceManagementService _serviceService;
        private readonly IPricingService _pricingService;
        private readonly IExhibitionService _exhibitionService;
        private readonly ITenantService _tenantService;
        private readonly ICurrencyService _currencyService;

        // ━━━━━━━━━━━━━━ Collections ━━━━━━━━━━━━━━
        public ObservableCollection<ServiceDto> Services { get; } = [];
        public ObservableCollection<BoothPriceRuleDto> BoothPriceRules { get; } = [];
        public ObservableCollection<PricingPackageDto> Packages { get; } = [];
        public ObservableCollection<ExhibitionSummaryDto> Exhibitions { get; } = [];
        public ObservableCollection<ExhibitionSummaryDto> ExhibitionsWithAll { get; } = [];
        public ObservableCollection<CurrencyDto> Currencies { get; } = [];

        public ObservableCollection<string> ServiceUnits { get; } = new() { "وحدة", "يوم", "متر مربع", "ساعة" };
        public ObservableCollection<string> BoothTypes { get; } = new() { "الكل", "Standard", "Corner", "Premium", "VIP", "Custom" };
        public ObservableCollection<string> ExhibitorCategories { get; } = new() { "الكل", "Local", "International", "Government" };

        // ━━━━━━━━━━━━━━ Tab 1 — Services properties ━━━━━━━━━━━━━━
        [ObservableProperty]
        private string _newServiceName = string.Empty;

        [ObservableProperty]
        private string _newServiceDescription = string.Empty;

        [ObservableProperty]
        private decimal _newServiceBasePrice;

        [ObservableProperty]
        private string _newServiceUnit = "وحدة";

        // ━━━━━━━━━━━━━━ Tab 2 — Booth Pricing properties ━━━━━━━━━━━━━━
        [ObservableProperty]
        private string _newRuleName = string.Empty;

        [ObservableProperty]
        private int _newRuleExhibitionId = 0; // 0 = General (All)

        [ObservableProperty]
        private int? _selectedExhibitionIdForPricing = 0; // 0 = General rules

        [ObservableProperty]
        private string _newRuleBoothType = "الكل";

        [ObservableProperty]
        private string _newRuleCategory = "الكل";

        [ObservableProperty]
        private decimal _newRulePricePerSqM;

        [ObservableProperty]
        private bool _newRuleIsActive = true;

        [ObservableProperty]
        private string _newRuleCurrencyCode = "LYD";

        [ObservableProperty]
        private decimal? _newRuleMinArea;

        [ObservableProperty]
        private decimal? _newRuleMaxArea;

        [ObservableProperty]
        private DateTime _newRuleValidFrom = DateTime.Today;

        [ObservableProperty]
        private DateTime? _newRuleValidTo;

        [ObservableProperty]
        private string _newRuleNotes = string.Empty;

        // ━━━━━━━━━━━━━━ Tab 3 — Packages properties ━━━━━━━━━━━━━━
        [ObservableProperty]
        private string _newPackageName = string.Empty;

        [ObservableProperty]
        private string _newPackageDescription = string.Empty;

        [ObservableProperty]
        private decimal _newPackagePrice;

        // ━━━━━━━━━━━━━━ Constructor ━━━━━━━━━━━━━━
        public ServicesViewModel(
            IServiceManagementService serviceService,
            IPricingService pricingService,
            IExhibitionService exhibitionService,
            ITenantService tenantService,
            ICurrencyService currencyService,
            INavigationService navigationService,
            INotificationService notificationService,
            SessionService session) : base(navigationService, notificationService, session)
        {
            _serviceService = serviceService;
            _pricingService = pricingService;
            _exhibitionService = exhibitionService;
            _tenantService = tenantService;
            _currencyService = currencyService;
            Title = "الخدمات والتسعير";
        }

        // ━━━━━━━━━━━━━━ Methods ━━━━━━━━━━━━━━
        public override async Task OnNavigatedToAsync()
        {
            await LoadServicesAsync();
            await LoadExhibitionsAsync();
            await LoadPriceRulesAsync();
            await LoadPackagesAsync();
            await LoadCurrenciesAsync();
            await UpdateDefaultCurrencyAsync();
        }

        [RelayCommand]
        private async Task LoadServicesAsync()
        {
            await ExecuteSafeAsync(async () =>
            {
                var result = await _serviceService.GetByTenantAsync(Session.TenantId);
                if (result.IsSuccess && result.Data is not null)
                {
                    Services.Clear();
                    foreach (var service in result.Data)
                    {
                        Services.Add(service);
                    }
                }
            }, "خطأ في تحميل الخدمات");
        }

        public Action? CloseServiceAction { get; set; }
        public Action? ClosePriceRuleAction { get; set; }
        public Action? ClosePackageAction { get; set; }

        [RelayCommand]
        private async Task SaveServiceAsync()
        {
            if (string.IsNullOrWhiteSpace(NewServiceName))
            {
                NotificationService.ShowError("الرجاء إدخال اسم الخدمة");
                return;
            }

            await ExecuteSafeAsync(async () =>
            {
                var dto = new ServiceCreateDto
                {
                    TenantID = Session.TenantId,
                    ServiceName = NewServiceName,
                    Description = NewServiceDescription,
                    DefaultPrice = NewServiceBasePrice,
                    Unit = NewServiceUnit
                };

                var result = await _serviceService.CreateAsync(Session.TenantId, dto);
                if (result.IsSuccess)
                {
                    NotificationService.ShowSuccess("تم إضافة الخدمة بنجاح ✓");
                    NewServiceName = string.Empty;
                    NewServiceDescription = string.Empty;
                    NewServiceBasePrice = 0;
                    NewServiceUnit = "وحدة";
                    await LoadServicesAsync();
                    CloseServiceAction?.Invoke();
                }
                else
                {
                    NotificationService.ShowError(result.ErrorMessage ?? "فشل إضافة الخدمة");
                }
            }, "خطأ أثناء إضافة الخدمة");
        }

        [RelayCommand]
        private async Task DeactivateServiceAsync(int serviceId)
        {
            var r = System.Windows.MessageBox.Show("هل أنت متأكد من رغبتك في تعطيل هذه الخدمة؟", "تأكيد التعطيل",
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning,
                System.Windows.MessageBoxResult.No, System.Windows.MessageBoxOptions.RtlReading | System.Windows.MessageBoxOptions.RightAlign);

            if (r != System.Windows.MessageBoxResult.Yes) return;

            await ExecuteSafeAsync(async () =>
            {
                var result = await _serviceService.DeactivateAsync(Session.TenantId, serviceId);
                if (result.IsSuccess)
                {
                    NotificationService.ShowSuccess("تم تعطيل الخدمة بنجاح ✓");
                    await LoadServicesAsync();
                }
                else
                {
                    NotificationService.ShowError(result.ErrorMessage ?? "فشل تعطيل الخدمة");
                }
            }, "خطأ أثناء تعطيل الخدمة");
        }

        [RelayCommand]
        private async Task LoadExhibitionsAsync()
        {
            await ExecuteSafeAsync(async () =>
            {
                var result = await _exhibitionService.GetByTenantAsync(Session.TenantId, 1, 100);
                if (result.IsSuccess && result.Data is not null)
                {
                    Exhibitions.Clear();
                    ExhibitionsWithAll.Clear();

                    ExhibitionsWithAll.Add(new ExhibitionSummaryDto { ExhibitionID = 0, Name = "عامة (الكل)" });

                    foreach (var exh in result.Data.Items)
                    {
                        Exhibitions.Add(exh);
                        ExhibitionsWithAll.Add(exh);
                    }
                }
            }, "خطأ في تحميل المعارض");
        }

        [RelayCommand]
        private async Task LoadPriceRulesAsync()
        {
            await ExecuteSafeAsync(async () =>
            {
                int? filterExhId = SelectedExhibitionIdForPricing == 0 ? null : SelectedExhibitionIdForPricing;
                var result = await _pricingService.GetBoothPriceRulesAsync(Session.TenantId, filterExhId);
                if (result.IsSuccess && result.Data is not null)
                {
                    BoothPriceRules.Clear();
                    foreach (var rule in result.Data)
                    {
                        BoothPriceRules.Add(rule);
                    }
                }
            }, "خطأ في تحميل قواعد التسعير");
        }

        [RelayCommand]
        private async Task SavePriceRuleAsync()
        {
            if (string.IsNullOrWhiteSpace(NewRuleName))
            {
                NotificationService.ShowError("الرجاء إدخال اسم قاعدة التسعير");
                return;
            }

            if (NewRulePricePerSqM <= 0)
            {
                NotificationService.ShowError("الرجاء إدخال سعر صحيح للمتر المربع");
                return;
            }

            if (string.IsNullOrWhiteSpace(NewRuleCurrencyCode))
            {
                NotificationService.ShowError("الرجاء إدخال رمز العملة");
                return;
            }

            await ExecuteSafeAsync(async () =>
            {
                string? boothType = NewRuleBoothType == "الكل" ? null : NewRuleBoothType;
                string? category = NewRuleCategory == "الكل" ? null : NewRuleCategory;
                int? targetExhId = NewRuleExhibitionId == 0 ? null : NewRuleExhibitionId;

                var dto = new BoothPriceRuleCreateDto
                {
                    TenantID = Session.TenantId,
                    ExhibitionID = targetExhId,
                    RuleName = NewRuleName,
                    BoothType = boothType,
                    ExhibitorCategory = category,
                    PricePerSqM = NewRulePricePerSqM,
                    CurrencyCode = NewRuleCurrencyCode,
                    MinAreaSqM = NewRuleMinArea,
                    MaxAreaSqM = NewRuleMaxArea,
                    ValidFrom = NewRuleValidFrom,
                    ValidTo = NewRuleValidTo,
                    Notes = string.IsNullOrWhiteSpace(NewRuleNotes) ? "قاعدة مضافة عبر سطح المكتب" : NewRuleNotes
                };

                var result = await _pricingService.CreateBoothPriceRuleAsync(Session.TenantId, dto);
                if (result.IsSuccess)
                {
                    NotificationService.ShowSuccess("تم إضافة قاعدة التسعير بنجاح ✓");
                    NewRuleName = string.Empty;
                    NewRulePricePerSqM = 0;
                    NewRuleMinArea = null;
                    NewRuleMaxArea = null;
                    NewRuleNotes = string.Empty;
                    NewRuleExhibitionId = 0;
                    await LoadPriceRulesAsync();
                    ClosePriceRuleAction?.Invoke();
                }
                else
                {
                    NotificationService.ShowError(result.ErrorMessage ?? "فشل إضافة قاعدة التسعير");
                }
            }, "خطأ أثناء إضافة قاعدة التسعير");
        }

        [RelayCommand]
        private async Task DeletePriceRuleAsync(int ruleId)
        {
            var r = System.Windows.MessageBox.Show("هل أنت متأكد من رغبتك في حذف قاعدة التسعير هذه؟", "تأكيد الحذف",
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning,
                System.Windows.MessageBoxResult.No, System.Windows.MessageBoxOptions.RtlReading | System.Windows.MessageBoxOptions.RightAlign);

            if (r != System.Windows.MessageBoxResult.Yes) return;

            await ExecuteSafeAsync(async () =>
            {
                var result = await _pricingService.DeleteBoothPriceRuleAsync(Session.TenantId, ruleId);
                if (result.IsSuccess)
                {
                    NotificationService.ShowSuccess("تم حذف قاعدة التسعير بنجاح ✓");
                    await LoadPriceRulesAsync();
                }
                else
                {
                    NotificationService.ShowError(result.ErrorMessage ?? "فشل حذف قاعدة التسعير");
                }
            }, "خطأ أثناء حذف قاعدة التسعير");
        }

        [RelayCommand]
        private async Task LoadPackagesAsync()
        {
            await ExecuteSafeAsync(async () =>
            {
                var result = await _pricingService.GetPackagesAsync(Session.TenantId);
                if (result.IsSuccess && result.Data is not null)
                {
                    Packages.Clear();
                    foreach (var package in result.Data)
                    {
                        Packages.Add(package);
                    }
                }
            }, "خطأ في تحميل الباقات");
        }

        [RelayCommand]
        private async Task SavePackageAsync()
        {
            if (string.IsNullOrWhiteSpace(NewPackageName))
            {
                NotificationService.ShowError("الرجاء إدخال اسم الباقة");
                return;
            }

            await ExecuteSafeAsync(async () =>
            {
                var dto = new PricingPackageCreateDto
                {
                    TenantID = Session.TenantId,
                    PackageName = NewPackageName,
                    Description = NewPackageDescription,
                    TotalPrice = NewPackagePrice,
                    CurrencyCode = NewRuleCurrencyCode,
                    ValidFrom = DateTime.UtcNow,
                    ServiceIDs = new List<int>()
                };

                var result = await _pricingService.CreatePackageAsync(Session.TenantId, dto);
                if (result.IsSuccess)
                {
                    NotificationService.ShowSuccess("تم إضافة الباقة بنجاح ✓");
                    NewPackageName = string.Empty;
                    NewPackageDescription = string.Empty;
                    NewPackagePrice = 0;
                    await LoadPackagesAsync();
                    ClosePackageAction?.Invoke();
                }
                else
                {
                    NotificationService.ShowError(result.ErrorMessage ?? "فشل إضافة الباقة");
                }
            }, "خطأ أثناء إضافة الباقة");
        }

        async partial void OnSelectedExhibitionIdForPricingChanged(int? value)
        {
            await LoadPriceRulesAsync();
            await UpdateDefaultCurrencyAsync();
        }

        [RelayCommand]
        private async Task LoadCurrenciesAsync()
        {
            await ExecuteSafeAsync(async () =>
            {
                var result = await _currencyService.GetAllAsync();
                Currencies.Clear();
                if (result.IsSuccess && result.Data is not null)
                {
                    foreach (var currency in result.Data.Where(c => c.IsActive))
                    {
                        Currencies.Add(currency);
                    }
                }

                // Fallback standard currencies if empty
                if (!Currencies.Any())
                {
                    Currencies.Add(new CurrencyDto { CurrencyCode = "LYD", CurrencyName = "دينار ليبي (LYD)", Symbol = "ل.د", IsActive = true });
                    Currencies.Add(new CurrencyDto { CurrencyCode = "USD", CurrencyName = "دولار أمريكي (USD)", Symbol = "$", IsActive = true });
                    Currencies.Add(new CurrencyDto { CurrencyCode = "EUR", CurrencyName = "يورو (EUR)", Symbol = "€", IsActive = true });
                }
            }, "خطأ في تحميل العملات");
        }

        private async Task UpdateDefaultCurrencyAsync()
        {
            await ExecuteSafeAsync(async () =>
            {
                if (SelectedExhibitionIdForPricing.HasValue && SelectedExhibitionIdForPricing.Value > 0)
                {
                    var exhResult = await _exhibitionService.GetByIdAsync(Session.TenantId, SelectedExhibitionIdForPricing.Value);
                    if (exhResult.IsSuccess && exhResult.Data != null)
                    {
                        NewRuleCurrencyCode = string.IsNullOrWhiteSpace(exhResult.Data.EntryCurrency) ? "USD" : exhResult.Data.EntryCurrency;
                        return;
                    }
                }

                var tenantResult = await _tenantService.GetByIdAsync(Session.TenantId);
                if (tenantResult.IsSuccess && tenantResult.Data != null)
                {
                    NewRuleCurrencyCode = string.IsNullOrWhiteSpace(tenantResult.Data.BaseCurrency) ? "LYD" : tenantResult.Data.BaseCurrency;
                }
            }, "خطأ في جلب العملة الافتراضية");
        }
    }
}
