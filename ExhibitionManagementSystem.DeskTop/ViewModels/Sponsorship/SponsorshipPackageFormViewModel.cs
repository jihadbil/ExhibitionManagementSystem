using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.DeskTop.Helpers;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using ExhibitionManagementSystem.Models.DTOs.Sponsorship;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Sponsorship;

public partial class SponsorshipPackageFormViewModel : ViewModelBase
{
    private readonly ISponsorshipService _sponsorshipService;

    [ObservableProperty] private int _packageId;
    [ObservableProperty] private int _exhibitionId;
    [ObservableProperty] private string _packageName = string.Empty;
    [ObservableProperty] private SponsorshipLevel _level = SponsorshipLevel.Gold;
    [ObservableProperty] private decimal _price;
    [ObservableProperty] private string _currencyCode = "LYD";
    [ObservableProperty] private int _maxSponsorsCount = 1;
    [ObservableProperty] private string? _entitlementsDescription;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private bool _isEditMode;

    public ObservableCollection<SponsorshipLevel> Levels { get; } = new(Enum.GetValues<SponsorshipLevel>());
    public ObservableCollection<string> Currencies { get; } = ["LYD", "USD", "EUR", "SAR", "AED"];

    public event Action? Saved;

    public SponsorshipPackageFormViewModel(
        ISponsorshipService sponsorshipService,
        INavigationService navigationService,
        INotificationService notificationService,
        SessionService session)
        : base(navigationService, notificationService, session)
    {
        _sponsorshipService = sponsorshipService;
        Title = "باقة الرعاية";
    }

    public void LoadForCreate(int exhibitionId)
    {
        IsEditMode = false;
        PackageId = 0;
        ExhibitionId = exhibitionId;
        PackageName = string.Empty;
        Level = SponsorshipLevel.Gold;
        Price = 10000;
        CurrencyCode = "LYD";
        MaxSponsorsCount = 1;
        EntitlementsDescription = "- جناح بمساحة 36 م²\n- لافتة معلقة في المدخل الرئيسي\n- 5 بطاقات VIP\n- شعار رسمي على كافة المطبوعات";
        IsActive = true;
        Title = "إضافة باقة رعاية جديدة";
    }

    public void LoadForEdit(SponsorshipPackageDto package)
    {
        IsEditMode = true;
        PackageId = package.PackageID;
        ExhibitionId = package.ExhibitionID;
        PackageName = package.PackageName;
        Level = package.Level;
        Price = package.Price;
        CurrencyCode = package.CurrencyCode;
        MaxSponsorsCount = package.MaxSponsorsCount;
        EntitlementsDescription = package.EntitlementsDescription;
        IsActive = package.IsActive;
        Title = "تعديل باقة الرعاية";
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(PackageName))
        {
            NotificationService.ShowWarning("اسم باقة الرعاية مطلوب", "تنبيه");
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            if (IsEditMode)
            {
                var dto = new SponsorshipPackageUpdateDto
                {
                    ExhibitionID = ExhibitionId,
                    PackageName = PackageName,
                    Level = Level,
                    Price = Price,
                    CurrencyCode = CurrencyCode,
                    MaxSponsorsCount = MaxSponsorsCount,
                    EntitlementsDescription = EntitlementsDescription,
                    IsActive = IsActive
                };

                var result = await _sponsorshipService.UpdatePackageAsync(Session.TenantId, PackageId, dto);
                if (result.IsSuccess)
                {
                    NotificationService.ShowSuccess("تم تحديث باقة الرعاية بنجاح", "تم الحفظ");
                    Saved?.Invoke();
                }
                else
                {
                    NotificationService.ShowError(result.ErrorMessage ?? "فشل حفظ الباقة", "خطأ");
                }
            }
            else
            {
                var dto = new SponsorshipPackageCreateDto
                {
                    ExhibitionID = ExhibitionId,
                    PackageName = PackageName,
                    Level = Level,
                    Price = Price,
                    CurrencyCode = CurrencyCode,
                    MaxSponsorsCount = MaxSponsorsCount,
                    EntitlementsDescription = EntitlementsDescription,
                    IsActive = IsActive
                };

                var result = await _sponsorshipService.CreatePackageAsync(Session.TenantId, dto);
                if (result.IsSuccess)
                {
                    NotificationService.ShowSuccess("تمت إضافة باقة الرعاية بنجاح", "تم الحفظ");
                    Saved?.Invoke();
                }
                else
                {
                    NotificationService.ShowError(result.ErrorMessage ?? "فشل حفظ الباقة", "خطأ");
                }
            }
        });
    }
}
