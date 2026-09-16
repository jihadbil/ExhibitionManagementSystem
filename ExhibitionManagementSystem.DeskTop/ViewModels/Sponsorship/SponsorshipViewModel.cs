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
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.Models.DTOs.Sponsorship;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Sponsorship;

public partial class SponsorshipViewModel : ViewModelBase
{
    private readonly ISponsorshipService _sponsorshipService;
    private readonly IExhibitionService _exhibitionService;

    // Collections
    public ObservableCollection<ExhibitionSummaryDto> Exhibitions { get; } = [];
    public ObservableCollection<SponsorDto> Sponsors { get; } = [];
    public ObservableCollection<SponsorshipPackageDto> Packages { get; } = [];
    public ObservableCollection<AdvertisingSpaceDto> AdvertisingSpaces { get; } = [];
    public ObservableCollection<SponsorshipContractDto> Contracts { get; } = [];

    // Filter properties
    [ObservableProperty] private int _selectedExhibitionId;
    [ObservableProperty] private string _selectedExhibitionName = "اختر المعرض...";
    [ObservableProperty] private string _searchSponsorText = string.Empty;
    [ObservableProperty] private string _searchSpaceText = string.Empty;

    // Statistics Cards
    [ObservableProperty] private int _totalSponsorsCount;
    [ObservableProperty] private decimal _totalSponsorshipRevenue;
    [ObservableProperty] private int _totalSpacesCount;
    [ObservableProperty] private int _availableSpacesCount;
    [ObservableProperty] private int _occupiedSpacesCount;
    [ObservableProperty] private string _currencyCode = "LYD";

    public SponsorshipViewModel(
        ISponsorshipService sponsorshipService,
        IExhibitionService exhibitionService,
        INavigationService navigationService,
        INotificationService notificationService,
        SessionService session)
        : base(navigationService, notificationService, session)
    {
        _sponsorshipService = sponsorshipService;
        _exhibitionService = exhibitionService;
        Title = "إدارة الرعاة والمساحات الإعلانية";
    }

    public override async Task OnNavigatedToAsync()
    {
        await LoadExhibitionsAsync();
        await LoadAllDataAsync();
    }

    [RelayCommand]
    public async Task LoadExhibitionsAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            var result = await _exhibitionService.GetActiveAsync(Session.TenantId);
            if (result.IsSuccess && result.Data != null)
            {
                Exhibitions.Clear();
                foreach (var ex in result.Data)
                {
                    Exhibitions.Add(ex);
                }

                if (Exhibitions.Any() && SelectedExhibitionId == 0)
                {
                    SelectedExhibitionId = Exhibitions.First().ExhibitionID;
                    SelectedExhibitionName = Exhibitions.First().Name;
                }
            }
        });
    }

    partial void OnSelectedExhibitionIdChanged(int value)
    {
        var ex = Exhibitions.FirstOrDefault(e => e.ExhibitionID == value);
        if (ex != null)
        {
            SelectedExhibitionName = ex.Name;
        }
        _ = LoadAllDataAsync();
    }

    [RelayCommand]
    public async Task LoadAllDataAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            // 1. Load Sponsors
            var sponsorsResult = await _sponsorshipService.GetSponsorsAsync(Session.TenantId);
            if (sponsorsResult.IsSuccess && sponsorsResult.Data != null)
            {
                Sponsors.Clear();
                foreach (var s in sponsorsResult.Data)
                {
                    Sponsors.Add(s);
                }
                TotalSponsorsCount = Sponsors.Count;
            }

            if (SelectedExhibitionId > 0)
            {
                // 2. Load Packages
                var pkgResult = await _sponsorshipService.GetPackagesByExhibitionAsync(Session.TenantId, SelectedExhibitionId);
                if (pkgResult.IsSuccess && pkgResult.Data != null)
                {
                    Packages.Clear();
                    foreach (var p in pkgResult.Data)
                    {
                        Packages.Add(p);
                    }
                }

                // 3. Load Advertising Spaces
                var spacesResult = await _sponsorshipService.GetSpacesByExhibitionAsync(Session.TenantId, SelectedExhibitionId);
                if (spacesResult.IsSuccess && spacesResult.Data != null)
                {
                    AdvertisingSpaces.Clear();
                    foreach (var sp in spacesResult.Data)
                    {
                        AdvertisingSpaces.Add(sp);
                    }
                    TotalSpacesCount = AdvertisingSpaces.Count;
                    AvailableSpacesCount = AdvertisingSpaces.Count(a => a.IsAvailable);
                    OccupiedSpacesCount = TotalSpacesCount - AvailableSpacesCount;
                }

                // 4. Load Contracts
                var contractsResult = await _sponsorshipService.GetContractsByExhibitionAsync(Session.TenantId, SelectedExhibitionId);
                if (contractsResult.IsSuccess && contractsResult.Data != null)
                {
                    Contracts.Clear();
                    foreach (var c in contractsResult.Data)
                    {
                        Contracts.Add(c);
                    }
                    TotalSponsorshipRevenue = Contracts.Where(c => c.Status != SponsorshipStatus.Cancelled).Sum(c => c.TotalAmount);
                }
            }
        });
    }

    [RelayCommand]
    public async Task DeleteSponsorAsync(int sponsorId)
    {
        await ExecuteSafeAsync(async () =>
        {
            var result = await _sponsorshipService.DeleteSponsorAsync(Session.TenantId, sponsorId, Session.UserId);
            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess("تم حذف الراعي بنجاح", "تم الحذف");
                await LoadAllDataAsync();
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل حذف الراعي", "خطأ");
            }
        });
    }

    [RelayCommand]
    public async Task DeletePackageAsync(int packageId)
    {
        await ExecuteSafeAsync(async () =>
        {
            var result = await _sponsorshipService.DeletePackageAsync(Session.TenantId, packageId, Session.UserId);
            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess("تم حذف باقة الرعاية بنجاح", "تم الحذف");
                await LoadAllDataAsync();
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل حذف الباقة", "خطأ");
            }
        });
    }

    [RelayCommand]
    public async Task DeleteSpaceAsync(int spaceId)
    {
        await ExecuteSafeAsync(async () =>
        {
            var result = await _sponsorshipService.DeleteSpaceAsync(Session.TenantId, spaceId, Session.UserId);
            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess("تم حذف المساحة الإعلانية بنجاح", "تم الحذف");
                await LoadAllDataAsync();
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل حذف المساحة", "خطأ");
            }
        });
    }

    [RelayCommand]
    public async Task UpdateContractStatusAsync((int ContractId, string Status) param)
    {
        await ExecuteSafeAsync(async () =>
        {
            var result = await _sponsorshipService.UpdateContractStatusAsync(Session.TenantId, param.ContractId, param.Status);
            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess("تم تحديث حالة العقد بنجاح", "تم التحديث");
                await LoadAllDataAsync();
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل تحديث العقد", "خطأ");
            }
        });
    }
}
