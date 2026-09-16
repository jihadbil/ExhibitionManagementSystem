using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.DeskTop.Helpers;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.Models.DTOs.Reservation;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Reservations;

public partial class ReservationsViewModel : ViewModelBase
{
    private readonly IReservationService _reservationService;
    private readonly IExhibitionService _exhibitionService;

    // ━━━━━━━━━━━━━━ Collections ━━━━━━━━━━━━━━

    public ObservableCollection<ExhibitionSummaryDto> Exhibitions { get; } = [];
    public ObservableCollection<BoothReservationSummaryDto> Reservations { get; } = [];
    public ObservableCollection<string> StatusFilters { get; } = new() { "All", "PendingReview", "Approved", "Cancelled" };

    // ━━━━━━━━━━━━━━ Properties ━━━━━━━━━━━━━━

    [ObservableProperty]
    private int _selectedExhibitionId;

    [ObservableProperty]
    private string _statusFilter = "All";

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _pendingCount;

    [ObservableProperty]
    private int _approvedCount;

    [ObservableProperty]
    private int _unpaidCount;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages;

    private const int PageSize = 15;

    // ━━━━━━━━━━━━━━ Constructor ━━━━━━━━━━━━━━

    public ReservationsViewModel(
        IReservationService reservationService,
        IExhibitionService exhibitionService,
        INavigationService navigationService,
        INotificationService notificationService,
        SessionService session) : base(navigationService, notificationService, session)
    {
        _reservationService = reservationService;
        _exhibitionService = exhibitionService;
        Title = "إدارة الحجوزات";
    }

    // ━━━━━━━━━━━━━━ Methods & Commands ━━━━━━━━━━━━━━

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

                if (Exhibitions.Any() && SelectedExhibitionId == 0)
                {
                    SelectedExhibitionId = Exhibitions[0].ExhibitionID;
                }
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل تحميل المعارض");
            }
        }, "خطأ في تحميل المعارض");
    }

    [RelayCommand]
    private async Task LoadReservationsAsync()
    {
        if (SelectedExhibitionId == 0) return;

        await ExecuteSafeAsync(async () =>
        {
            var result = await _reservationService.GetByExhibitionAsync(Session.TenantId, SelectedExhibitionId, CurrentPage, PageSize);
            if (result.IsSuccess && result.Data is not null)
            {
                var rawData = result.Data.Items;

                // Apply local status filter
                var filtered = StatusFilter == "All"
                    ? rawData
                    : rawData.Where(r => r.Status == StatusFilter).ToList();

                Reservations.Clear();
                foreach (var res in filtered)
                {
                    Reservations.Add(res);
                }

                TotalCount = result.Data.TotalCount;
                TotalPages = result.Data.TotalPages;

                PendingCount = rawData.Count(r => r.Status == "PendingReview");
                ApprovedCount = rawData.Count(r => r.Status == "Approved");

                // Get unpaid count
                var unpaidResult = await _reservationService.GetUnpaidAsync(Session.TenantId, SelectedExhibitionId);
                if (unpaidResult.IsSuccess && unpaidResult.Data is not null)
                {
                    UnpaidCount = unpaidResult.Data.Count;
                }
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل تحميل الحجوزات");
            }
        }, "خطأ في تحميل الحجوزات");
    }

    [RelayCommand]
    private async Task ApproveAsync(int reservationId)
    {
        await ExecuteSafeAsync(async () =>
        {
            var result = await _reservationService.ApproveAsync(Session.TenantId, reservationId);
            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess("تمت الموافقة على الحجز بنجاح ✓");
                await LoadReservationsAsync();
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل الموافقة على الحجز");
            }
        }, "خطأ في الموافقة على الحجز");
    }

    [RelayCommand]
    private async Task CancelAsync(int reservationId)
    {
        var confirm = MessageBox.Show(
            "هل أنت متأكد من إلغاء هذا الحجز؟",
            "تأكيد الإلغاء",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No,
            MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

        if (confirm != MessageBoxResult.Yes) return;

        await ExecuteSafeAsync(async () =>
        {
            var result = await _reservationService.CancelAsync(Session.TenantId, reservationId);
            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess("تم إلغاء الحجز بنجاح ✓");
                await LoadReservationsAsync();
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل إلغاء الحجز");
            }
        }, "خطأ في إلغاء الحجز");
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadReservationsAsync();
        }
    }

    [RelayCommand]
    private async Task PrevPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadReservationsAsync();
        }
    }

    // ━━━━━━━━━━━━━━ Property Change Handlers ━━━━━━━━━━━━━━

    async partial void OnSelectedExhibitionIdChanged(int value)
    {
        Reservations.Clear();
        CurrentPage = 1;
        if (value > 0)
        {
            await LoadReservationsAsync();
        }
    }

    async partial void OnStatusFilterChanged(string value)
    {
        CurrentPage = 1;
        if (SelectedExhibitionId > 0)
        {
            await LoadReservationsAsync();
        }
    }

    // ━━━━━━━━━━━━━━ Lifecycle ━━━━━━━━━━━━━━

    public override async Task OnNavigatedToAsync()
    {
        await LoadExhibitionsAsync();
    }
}
