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
using ExhibitionManagementSystem.Models.DTOs.Sponsorship;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Sponsorship;

public partial class SponsorshipContractFormViewModel : ViewModelBase
{
    private readonly ISponsorshipService _sponsorshipService;

    [ObservableProperty] private int _exhibitionId;
    [ObservableProperty] private int _selectedSponsorId;
    [ObservableProperty] private int? _selectedPackageId;
    [ObservableProperty] private string _contractNumber = string.Empty;
    [ObservableProperty] private decimal _totalAmount;
    [ObservableProperty] private string _currencyCode = "LYD";
    [ObservableProperty] private SponsorshipStatus _status = SponsorshipStatus.Confirmed;
    [ObservableProperty] private DateTime _paymentDueDate = DateTime.Today.AddDays(30);
    [ObservableProperty] private string? _specialTerms;
    [ObservableProperty] private string? _notes;

    public ObservableCollection<SponsorDto> Sponsors { get; } = [];
    public ObservableCollection<SponsorshipPackageDto> Packages { get; } = [];
    public ObservableCollection<AdvertisingSpaceDto> AvailableSpaces { get; } = [];
    public ObservableCollection<int> SelectedSpaceIds { get; } = [];

    public event Action? Saved;

    public SponsorshipContractFormViewModel(
        ISponsorshipService sponsorshipService,
        INavigationService navigationService,
        INotificationService notificationService,
        SessionService session)
        : base(navigationService, notificationService, session)
    {
        _sponsorshipService = sponsorshipService;
        Title = "إنشاء عقد رعاية جديد";
    }

    public async Task LoadAsync(int exhibitionId)
    {
        ExhibitionId = exhibitionId;
        ContractNumber = $"SPN-{exhibitionId}-{DateTime.UtcNow:MMddHHmm}";
        Status = SponsorshipStatus.Confirmed;
        PaymentDueDate = DateTime.Today.AddDays(30);
        SelectedSpaceIds.Clear();

        await ExecuteSafeAsync(async () =>
        {
            // Load Sponsors
            var sponsorsRes = await _sponsorshipService.GetSponsorsAsync(Session.TenantId);
            if (sponsorsRes.IsSuccess && sponsorsRes.Data != null)
            {
                Sponsors.Clear();
                foreach (var s in sponsorsRes.Data)
                {
                    Sponsors.Add(s);
                }
                if (Sponsors.Any()) SelectedSponsorId = Sponsors.First().SponsorID;
            }

            // Load Packages
            var pkgRes = await _sponsorshipService.GetPackagesByExhibitionAsync(Session.TenantId, exhibitionId);
            if (pkgRes.IsSuccess && pkgRes.Data != null)
            {
                Packages.Clear();
                foreach (var p in pkgRes.Data)
                {
                    Packages.Add(p);
                }
                if (Packages.Any())
                {
                    SelectedPackageId = Packages.First().PackageID;
                    TotalAmount = Packages.First().Price;
                    CurrencyCode = Packages.First().CurrencyCode;
                }
            }

            // Load Available Spaces
            var spacesRes = await _sponsorshipService.GetSpacesByExhibitionAsync(Session.TenantId, exhibitionId, availableOnly: true);
            if (spacesRes.IsSuccess && spacesRes.Data != null)
            {
                AvailableSpaces.Clear();
                foreach (var sp in spacesRes.Data)
                {
                    AvailableSpaces.Add(sp);
                }
            }
        });
    }

    partial void OnSelectedPackageIdChanged(int? value)
    {
        if (value.HasValue)
        {
            var pkg = Packages.FirstOrDefault(p => p.PackageID == value.Value);
            if (pkg != null)
            {
                TotalAmount = pkg.Price;
                CurrencyCode = pkg.CurrencyCode;
            }
        }
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (SelectedSponsorId == 0)
        {
            NotificationService.ShowWarning("يرجى اختيار الراعي", "تنبيه");
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            var dto = new SponsorshipContractCreateDto
            {
                ExhibitionID = ExhibitionId,
                SponsorID = SelectedSponsorId,
                PackageID = SelectedPackageId,
                ContractNumber = ContractNumber,
                TotalAmount = TotalAmount,
                CurrencyCode = CurrencyCode,
                Status = Status,
                PaymentDueDate = PaymentDueDate,
                SpecialTerms = SpecialTerms,
                Notes = Notes,
                SpaceIDsToAssign = SelectedSpaceIds.ToList()
            };

            var result = await _sponsorshipService.CreateContractAsync(Session.TenantId, Session.UserId, dto);
            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess("تم توثيق وإنشاء عقد الرعاية بنجاح", "تم الحفظ");
                Saved?.Invoke();
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل حفظ عقد الرعاية", "خطأ");
            }
        });
    }
}
