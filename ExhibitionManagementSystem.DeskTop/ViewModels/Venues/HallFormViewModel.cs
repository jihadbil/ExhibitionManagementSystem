using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.DeskTop.Helpers;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using ExhibitionManagementSystem.Models.DTOs.Hall;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Venues;

public partial class HallFormViewModel : ViewModelBase
{
    private readonly IHallService _hallService;

    // ━━━━━━━━━━━━━━ Properties ━━━━━━━━━━━━━━

    [ObservableProperty]
    private int _hallId;

    [ObservableProperty]
    private int _venueId;

    [ObservableProperty]
    private string _hallName = string.Empty;

    [ObservableProperty]
    private decimal? _areaSqM;

    [ObservableProperty]
    private int? _maxBooths;

    [ObservableProperty]
    private decimal? _floorPlanWidth;

    [ObservableProperty]
    private decimal? _floorPlanHeight;

    [ObservableProperty]
    private string _floorPlanJSON = string.Empty;

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private bool _isEditMode;

    public Action? CloseAction { get; set; }

    // ━━━━━━━━━━━━━━ Constructor ━━━━━━━━━━━━━━

    public HallFormViewModel(
        IHallService hallService,
        INavigationService navigationService,
        INotificationService notificationService,
        SessionService session) : base(navigationService, notificationService, session)
    {
        _hallService = hallService;
        Title = "إضافة قاعة جديدة";
    }

    // ━━━━━━━━━━━━━━ Methods ━━━━━━━━━━━━━━

    public async Task InitializeAsync(int venueId, int hallId = 0)
    {
        VenueId = venueId;
        HallId = hallId;
        IsEditMode = hallId > 0;
        Title = IsEditMode ? "تعديل بيانات القاعة" : "إضافة قاعة جديدة";

        if (IsEditMode)
        {
            await LoadHallDetailsAsync();
        }
    }

    private async Task LoadHallDetailsAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            var result = await _hallService.GetByIdAsync(Session.TenantId, HallId);
            if (result.IsSuccess && result.Data is not null)
            {
                var data = result.Data;
                VenueId = data.VenueID;
                HallName = data.HallName;
                AreaSqM = data.AreaSqM;
                MaxBooths = data.MaxBooths;
                FloorPlanWidth = data.FloorPlanWidth;
                FloorPlanHeight = data.FloorPlanHeight;
                FloorPlanJSON = data.FloorPlanJSON;
                IsActive = data.IsActive;
            }
        }, "خطأ في تحميل تفاصيل القاعة");
    }

    // ━━━━━━━━━━━━━━ Commands ━━━━━━━━━━━━━━

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(HallName))
        {
            NotificationService.ShowError("الرجاء إدخال اسم القاعة");
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            if (IsEditMode)
            {
                var dto = new HallUpdateDto
                {
                    HallName = HallName,
                    AreaSqM = AreaSqM,
                    MaxBooths = MaxBooths,
                    FloorPlanWidth = FloorPlanWidth,
                    FloorPlanHeight = FloorPlanHeight,
                    FloorPlanJSON = FloorPlanJSON,
                    IsActive = IsActive
                };

                var result = await _hallService.UpdateAsync(Session.TenantId, HallId, dto);
                if (result.IsSuccess)
                {
                    NotificationService.ShowSuccess("تم تحديث القاعة بنجاح ✓");
                    CloseAction?.Invoke();
                }
                else
                {
                    NotificationService.ShowError(result.ErrorMessage ?? "فشل تحديث القاعة");
                }
            }
            else
            {
                var dto = new HallCreateDto
                {
                    VenueID = VenueId,
                    HallName = HallName,
                    AreaSqM = AreaSqM,
                    MaxBooths = MaxBooths,
                    FloorPlanWidth = FloorPlanWidth,
                    FloorPlanHeight = FloorPlanHeight,
                    FloorPlanJSON = FloorPlanJSON
                };

                var result = await _hallService.CreateAsync(Session.TenantId, dto);
                if (result.IsSuccess)
                {
                    NotificationService.ShowSuccess("تم إضافة القاعة بنجاح ✓");
                    CloseAction?.Invoke();
                }
                else
                {
                    NotificationService.ShowError(result.ErrorMessage ?? "فشل إضافة القاعة");
                }
            }
        }, "خطأ أثناء حفظ القاعة");
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseAction?.Invoke();
    }
}
