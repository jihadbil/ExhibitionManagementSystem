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
using ExhibitionManagementSystem.Models.DTOs.Reservation;
using ExhibitionManagementSystem.Models.DTOs.Booth;
using ExhibitionManagementSystem.Models.DTOs.Exhibitor;
using ExhibitionManagementSystem.Models.DTOs.Service;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Reservations;

public class SelectedServiceItem
{
    public int ServiceID { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal TotalPrice => Quantity * UnitPrice;
}

public partial class ReservationFormViewModel : ViewModelBase
{
    private readonly IReservationService _reservationService;
    private readonly IBoothService _boothService;
    private readonly IExhibitorService _exhibitorService;
    private readonly IServiceManagementService _serviceManagementService;
    private readonly IExhibitionService _exhibitionService;
    private readonly IHallService _hallService;

    // ━━━━━━━━━━━━━━ Collections ━━━━━━━━━━━━━━

    public ObservableCollection<ExhibitionSummaryDto> Exhibitions { get; } = [];
    public ObservableCollection<BoothSummaryDto> AvailableBooths { get; } = [];
    public ObservableCollection<ExhibitorSummaryDto> SearchedExhibitors { get; } = [];
    public ObservableCollection<ServiceDto> AvailableServices { get; } = [];
    public ObservableCollection<SelectedServiceItem> SelectedServices { get; } = [];

    // ━━━━━━━━━━━━━━ Properties ━━━━━━━━━━━━━━

    [ObservableProperty]
    private int _selectedExhibitionId;

    [ObservableProperty]
    private int _selectedBoothId;

    [ObservableProperty]
    private int _selectedExhibitorId;

    [ObservableProperty]
    private string _exhibitorSearchTerm = string.Empty;

    [ObservableProperty]
    private int _selectedServiceId;

    [ObservableProperty]
    private int _serviceQuantity = 1;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string _boothTypeSelected = "Standard";

    [ObservableProperty]
    private decimal _requestedAreaSqM;

    [ObservableProperty]
    private string _exhibitorCategory = "Local";

    [ObservableProperty]
    private string _currencyCode = "USD";

    public Action? CloseAction { get; set; }

    // ━━━━━━━━━━━━━━ Constructor ━━━━━━━━━━━━━━

    public ReservationFormViewModel(
        IReservationService reservationService,
        IBoothService boothService,
        IExhibitorService exhibitorService,
        IServiceManagementService serviceManagementService,
        IExhibitionService exhibitionService,
        IHallService hallService,
        INavigationService navigationService,
        INotificationService notificationService,
        SessionService session) : base(navigationService, notificationService, session)
    {
        _reservationService = reservationService;
        _boothService = boothService;
        _exhibitorService = exhibitorService;
        _serviceManagementService = serviceManagementService;
        _exhibitionService = exhibitionService;
        _hallService = hallService;
        Title = "حجز جناح جديد";
    }

    // ━━━━━━━━━━━━━━ Methods & Commands ━━━━━━━━━━━━━━

    public async Task InitializeAsync()
    {
        SelectedServices.Clear();
        Notes = string.Empty;
        ExhibitorSearchTerm = string.Empty;
        SearchedExhibitors.Clear();
        AvailableBooths.Clear();
        SelectedExhibitionId = 0;
        SelectedBoothId = 0;
        SelectedExhibitorId = 0;

        await LoadExhibitionsAsync();
        await LoadServicesAsync();
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
                foreach (var exhibition in result.Data.Items)
                {
                    Exhibitions.Add(exhibition);
                }
            }
        }, "خطأ في تحميل المعارض");
    }

    [RelayCommand]
    private async Task LoadAvailableBoothsAsync()
    {
        if (SelectedExhibitionId == 0) return;

        await ExecuteSafeAsync(async () =>
        {
            // 1. Get exhibition details to find VenueID
            var exhResult = await _exhibitionService.GetByIdAsync(Session.TenantId, SelectedExhibitionId);
            if (!exhResult.IsSuccess || exhResult.Data is null)
            {
                NotificationService.ShowError(exhResult.ErrorMessage ?? "فشل تحميل تفاصيل المعرض");
                return;
            }

            var venueId = exhResult.Data.VenueID;
            CurrencyCode = string.IsNullOrWhiteSpace(exhResult.Data.EntryCurrency) ? "USD" : exhResult.Data.EntryCurrency;

            // 2. Get halls for this venue
            var hallsResult = await _hallService.GetByVenueAsync(Session.TenantId, venueId);
            if (!hallsResult.IsSuccess || hallsResult.Data is null)
            {
                NotificationService.ShowError(hallsResult.ErrorMessage ?? "فشل تحميل القاعات");
                return;
            }

            AvailableBooths.Clear();
            // 3. Get available booths for each hall
            foreach (var hall in hallsResult.Data)
            {
                var boothsResult = await _boothService.GetAvailableAsync(Session.TenantId, hall.HallID, SelectedExhibitionId);
                if (boothsResult.IsSuccess && boothsResult.Data is not null)
                {
                    foreach (var booth in boothsResult.Data)
                    {
                        AvailableBooths.Add(booth);
                    }
                }
            }
        }, "خطأ في تحميل الأجنحة المتاحة");
    }

    [RelayCommand]
    private async Task SearchExhibitorAsync()
    {
        if (string.IsNullOrWhiteSpace(ExhibitorSearchTerm)) return;

        await ExecuteSafeAsync(async () =>
        {
            var result = await _exhibitorService.SearchAsync(Session.TenantId, ExhibitorSearchTerm);
            if (result.IsSuccess && result.Data is not null)
            {
                SearchedExhibitors.Clear();
                foreach (var exhibitor in result.Data)
                {
                    SearchedExhibitors.Add(exhibitor);
                }
            }
        }, "خطأ أثناء البحث عن شركة عارضة");
    }

    [RelayCommand]
    private async Task LoadServicesAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            var result = await _serviceManagementService.GetByTenantAsync(Session.TenantId);
            if (result.IsSuccess && result.Data is not null)
            {
                AvailableServices.Clear();
                foreach (var s in result.Data.Where(x => x.IsActive))
                {
                    AvailableServices.Add(s);
                }
            }
        }, "خطأ في تحميل الخدمات");
    }

    [RelayCommand]
    private void AddService()
    {
        if (SelectedServiceId == 0 || ServiceQuantity < 1) return;

        var service = AvailableServices.FirstOrDefault(s => s.ServiceID == SelectedServiceId);
        if (service == null) return;

        var existing = SelectedServices.FirstOrDefault(s => s.ServiceID == SelectedServiceId);
        if (existing != null)
        {
            existing.Quantity += ServiceQuantity;
            // Refresh in list by removing and re-adding
            SelectedServices.Remove(existing);
            SelectedServices.Add(existing);
        }
        else
        {
            SelectedServices.Add(new SelectedServiceItem
            {
                ServiceID = service.ServiceID,
                ServiceName = service.ServiceName,
                Quantity = ServiceQuantity,
                UnitPrice = service.DefaultPrice ?? 0,
                CurrencyCode = CurrencyCode
            });
        }

        SelectedServiceId = 0;
        ServiceQuantity = 1;
    }

    [RelayCommand]
    private void RemoveService(SelectedServiceItem item)
    {
        if (item != null)
        {
            SelectedServices.Remove(item);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedExhibitionId == 0)
        {
            NotificationService.ShowError("الرجاء اختيار المعرض");
            return;
        }

        if (SelectedBoothId == 0)
        {
            NotificationService.ShowError("الرجاء اختيار الجناح");
            return;
        }

        if (SelectedExhibitorId == 0)
        {
            NotificationService.ShowError("الرجاء اختيار الشركة العارضة");
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            var dto = new BoothReservationCreateDto
            {
                ExhibitionID = SelectedExhibitionId,
                BoothID = SelectedBoothId,
                ExhibitorID = SelectedExhibitorId,
                BoothTypeSelected = BoothTypeSelected,
                RequestedAreaSqM = RequestedAreaSqM,
                ExhibitorCategory = ExhibitorCategory,
                CurrencyCode = CurrencyCode,
                LogisticNotes = Notes
            };

            var result = await _reservationService.CreateAsync(Session.TenantId, Session.UserId, dto);
            if (result.IsSuccess && result.Data is not null)
            {
                var newReservationId = result.Data.ReservationID;

                // Add services if any
                foreach (var item in SelectedServices)
                {
                    var sDto = new ReservationServiceCreateDto
                    {
                        ServiceID = item.ServiceID,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        CurrencyCode = item.CurrencyCode
                    };
                    await _reservationService.AddServiceToReservationAsync(Session.TenantId, newReservationId, sDto);
                }

                NotificationService.ShowSuccess("تم إنشاء الحجز بنجاح ✓");
                CloseAction?.Invoke();
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل إنشاء الحجز");
            }
        }, "خطأ أثناء حفظ الحجز");
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseAction?.Invoke();
    }

    // ━━━━━━━━━━━━━━ Property Change Handlers ━━━━━━━━━━━━━━

    async partial void OnSelectedExhibitionIdChanged(int value)
    {
        AvailableBooths.Clear();
        SelectedBoothId = 0;
        if (value > 0)
        {
            await LoadAvailableBoothsAsync();
        }
    }

    partial void OnSelectedBoothIdChanged(int value)
    {
        if (value > 0)
        {
            var booth = AvailableBooths.FirstOrDefault(b => b.BoothID == value);
            if (booth != null)
            {
                RequestedAreaSqM = booth.CurrentAreaSqM;
            }
        }
        else
        {
            RequestedAreaSqM = 0;
        }
    }

    partial void OnSelectedExhibitorIdChanged(int value)
    {
        if (value > 0)
        {
            var exhibitor = SearchedExhibitors.FirstOrDefault(e => e.ExhibitorID == value);
            if (exhibitor != null)
            {
                ExhibitorCategory = string.IsNullOrWhiteSpace(exhibitor.ExhibitorCategory) ? "Local" : exhibitor.ExhibitorCategory;
            }
        }
    }
}
