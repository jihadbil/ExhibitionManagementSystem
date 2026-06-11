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
using ExhibitionManagementSystem.Models.DTOs.Booth;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.Models.DTOs.Hall;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.DeskTop.Views.Booths;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Booths;

public partial class BoothsViewModel : ViewModelBase
{
    private readonly IBoothService _boothService;
    private readonly IHallService _hallService;
    private readonly IExhibitionService _exhibitionService;

    // ━━━━━━━━━━━━━━ Collections ━━━━━━━━━━━━━━
    public ObservableCollection<BoothDto> Booths { get; } = [];
    public ObservableCollection<HallDto> AvailableHalls { get; } = [];
    public ObservableCollection<ExhibitionSummaryDto> Exhibitions { get; } = [];

    // ━━━━━━━━━━━━━━ Selection properties ━━━━━━━━━━━━━━
    [ObservableProperty]
    private int _selectedExhibitionId;

    [ObservableProperty]
    private int _selectedHallId;

    [ObservableProperty]
    private string _statusFilter = "All"; // All | Available | Reserved | PendingReview

    // ━━━━━━━━━━━━━━ Statistics ━━━━━━━━━━━━━━
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _availableCount;
    [ObservableProperty] private int _reservedCount;
    [ObservableProperty] private int _pendingCount;

    // ━━━━━━━━━━━━━━ Form Fields for New Booth ━━━━━━━━━━━━━━
    [ObservableProperty] private string _newBoothNumber = string.Empty;
    [ObservableProperty] private decimal _newOriginalAreaSqM = 12;
    [ObservableProperty] private decimal? _newWidth = 4;
    [ObservableProperty] private decimal? _newHeight = 3;
    [ObservableProperty] private string _newShapeType = "Standard"; // Standard | Corner | Premium | VIP

    public ObservableCollection<string> ShapeTypes { get; } = new()
    {
        "Standard", "Corner", "Premium", "VIP"
    };

    public ObservableCollection<string> StatusFilters { get; } = new()
    {
        "All", "Available", "Reserved", "PendingReview"
    };

    // ━━━━━━━━━━━━━━ Constructor ━━━━━━━━━━━━━━
    public BoothsViewModel(
        IBoothService boothService,
        IHallService hallService,
        IExhibitionService exhibitionService,
        INavigationService navigationService,
        INotificationService notificationService,
        SessionService session) : base(navigationService, notificationService, session)
    {
        _boothService = boothService;
        _hallService = hallService;
        _exhibitionService = exhibitionService;
        Title = "إدارة الأجنحة";
    }

    // ━━━━━━━━━━━━━━ Methods ━━━━━━━━━━━━━━
    public override async Task OnNavigatedToAsync()
    {
        await LoadExhibitionsAsync();
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
                foreach (var ex in result.Data.Items)
                {
                    Exhibitions.Add(ex);
                }

                if (Exhibitions.Count > 0 && SelectedExhibitionId == 0)
                {
                    SelectedExhibitionId = Exhibitions[0].ExhibitionID;
                }
            }
        }, "خطأ في تحميل المعارض");
    }

    async partial void OnSelectedExhibitionIdChanged(int value)
    {
        SelectedHallId = 0;
        AvailableHalls.Clear();
        Booths.Clear();

        if (value > 0)
        {
            await LoadHallsAsync(value);
        }
    }

    private async Task LoadHallsAsync(int exhibitionId)
    {
        await ExecuteSafeAsync(async () =>
        {
            // Load exhibition details to get VenueID
            var exResult = await _exhibitionService.GetByIdAsync(Session.TenantId, exhibitionId);
            if (exResult.IsSuccess && exResult.Data is not null)
            {
                var result = await _hallService.GetByVenueAsync(Session.TenantId, exResult.Data.VenueID);
                if (result.IsSuccess && result.Data is not null)
                {
                    AvailableHalls.Clear();
                    foreach (var hall in result.Data)
                    {
                        AvailableHalls.Add(hall);
                    }

                    if (AvailableHalls.Count > 0)
                    {
                        SelectedHallId = AvailableHalls[0].HallID;
                    }
                }
            }
        }, "خطأ في تحميل الصالات");
    }

    async partial void OnSelectedHallIdChanged(int value)
    {
        if (value > 0)
        {
            await LoadBoothsAsync();
        }
        else
        {
            Booths.Clear();
            ResetStats();
        }
    }

    async partial void OnStatusFilterChanged(string value)
    {
        if (SelectedHallId > 0)
        {
            await LoadBoothsAsync();
        }
    }

    [RelayCommand]
    private async Task LoadBoothsAsync()
    {
        if (SelectedHallId == 0) return;

        await ExecuteSafeAsync(async () =>
        {
            var result = await _boothService.GetByHallAsync(Session.TenantId, SelectedHallId);
            if (result.IsSuccess && result.Data is not null)
            {
                var rawBooths = result.Data;

                // Stats calculation (on all booths in the hall)
                TotalCount = rawBooths.Count;
                AvailableCount = rawBooths.Count(b => b.Status.Equals("Available", StringComparison.OrdinalIgnoreCase) || b.Status.Equals("متاح", StringComparison.OrdinalIgnoreCase));
                ReservedCount = rawBooths.Count(b => b.Status.Equals("Reserved", StringComparison.OrdinalIgnoreCase) || b.Status.Equals("محجوز", StringComparison.OrdinalIgnoreCase));
                PendingCount = rawBooths.Count(b => b.Status.Equals("PendingReview", StringComparison.OrdinalIgnoreCase) || b.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase) || b.Status.Equals("قيد المراجعة", StringComparison.OrdinalIgnoreCase));

                // Apply filter
                var filtered = rawBooths.AsEnumerable();
                if (StatusFilter != "All")
                {
                    string targetStatus = StatusFilter;
                    if (StatusFilter == "PendingReview")
                    {
                        filtered = filtered.Where(b => b.Status.Equals("PendingReview", StringComparison.OrdinalIgnoreCase) || b.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase) || b.Status.Equals("قيد المراجعة", StringComparison.OrdinalIgnoreCase));
                    }
                    else
                    {
                        filtered = filtered.Where(b => b.Status.Equals(targetStatus, StringComparison.OrdinalIgnoreCase));
                    }
                }

                Booths.Clear();
                foreach (var b in filtered)
                {
                    Booths.Add(b);
                }
            }
        }, "خطأ في تحميل الأجنحة");
    }

    [RelayCommand]
    private async Task SaveBoothAsync()
    {
        if (SelectedHallId == 0)
        {
            NotificationService.ShowError("الرجاء اختيار الصالة أولاً");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewBoothNumber))
        {
            NotificationService.ShowError("الرجاء إدخال رقم الجناح");
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            var dto = new BoothCreateDto
            {
                HallID = SelectedHallId,
                BoothNumber = NewBoothNumber,
                OriginalAreaSqM = NewOriginalAreaSqM,
                Width = NewWidth,
                Height = NewHeight,
                ShapeType = NewShapeType,
                PosX = 10, // default start position
                PosY = 10
            };

            var result = await _boothService.CreateAsync(Session.TenantId, dto);
            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess("تم إضافة الجناح بنجاح ✓");
                NewBoothNumber = string.Empty;
                NewOriginalAreaSqM = 12;
                NewWidth = 4;
                NewHeight = 3;
                await LoadBoothsAsync();
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل إضافة الجناح");
            }
        }, "خطأ أثناء إضافة الجناح");
    }

    [RelayCommand]
    private async Task DeleteBoothAsync(int boothId)
    {
        var result = System.Windows.MessageBox.Show(
            "هل أنت متأكد من رغبتك في حذف هذا الجناح؟",
            "تأكيد الحذف",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        await ExecuteSafeAsync(async () =>
        {
            var deleteResult = await _boothService.DeleteAsync(Session.TenantId, boothId);
            if (deleteResult.IsSuccess)
            {
                NotificationService.ShowSuccess("تم حذف الجناح بنجاح ✓");
                await LoadBoothsAsync();
            }
            else
            {
                NotificationService.ShowError(deleteResult.ErrorMessage ?? "فشل حذف الجناح");
            }
        }, "خطأ أثناء حذف الجناح");
    }

    [RelayCommand]
    private void OpenDesigner()
    {
        NavigationService.NavigateTo<BoothDesignerPage>();
    }

    private void ResetStats()
    {
        TotalCount = 0;
        AvailableCount = 0;
        ReservedCount = 0;
        PendingCount = 0;
    }
}
