using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.DeskTop.Helpers;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using ExhibitionManagementSystem.Models.DTOs.Venue;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Venues;

public partial class VenueFormViewModel : ViewModelBase
{
    private readonly IVenueService _venueService;

    // ━━━━━━━━━━━━━━ Properties ━━━━━━━━━━━━━━

    [ObservableProperty]
    private int _venueId;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _address = string.Empty;

    [ObservableProperty]
    private string _city = string.Empty;

    [ObservableProperty]
    private string _country = string.Empty;

    [ObservableProperty]
    private int? _totalCapacity;

    [ObservableProperty]
    private string _mapImageURL = string.Empty;

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private bool _isEditMode;

    public Action? CloseAction { get; set; }

    // ━━━━━━━━━━━━━━ Constructor ━━━━━━━━━━━━━━

    public VenueFormViewModel(
        IVenueService venueService,
        INavigationService navigationService,
        INotificationService notificationService,
        SessionService session) : base(navigationService, notificationService, session)
    {
        _venueService = venueService;
        Title = "إضافة موقع جديد";
    }

    // ━━━━━━━━━━━━━━ Methods ━━━━━━━━━━━━━━

    public async Task InitializeAsync(int venueId = 0)
    {
        VenueId = venueId;
        IsEditMode = venueId > 0;
        Title = IsEditMode ? "تعديل بيانات الموقع" : "إضافة موقع جديد";

        if (IsEditMode)
        {
            await LoadVenueDetailsAsync();
        }
    }

    private async Task LoadVenueDetailsAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            var result = await _venueService.GetByIdAsync(Session.TenantId, VenueId);
            if (result.IsSuccess && result.Data is not null)
            {
                var data = result.Data;
                Name = data.Name;
                Address = data.Address;
                City = data.City;
                Country = data.Country;
                TotalCapacity = data.TotalCapacity;
                MapImageURL = data.MapImageURL;
                IsActive = data.IsActive;
            }
        }, "خطأ في تحميل تفاصيل الموقع");
    }

    // ━━━━━━━━━━━━━━ Commands ━━━━━━━━━━━━━━

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            NotificationService.ShowError("الرجاء إدخال اسم الموقع");
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            if (IsEditMode)
            {
                var dto = new VenueUpdateDto
                {
                    Name = Name,
                    Address = Address,
                    City = City,
                    Country = Country,
                    TotalCapacity = TotalCapacity,
                    MapImageURL = MapImageURL,
                    IsActive = IsActive
                };

                var result = await _venueService.UpdateAsync(Session.TenantId, VenueId, dto);
                if (result.IsSuccess)
                {
                    NotificationService.ShowSuccess("تم تحديث الموقع بنجاح ✓");
                    CloseAction?.Invoke();
                }
                else
                {
                    NotificationService.ShowError(result.ErrorMessage ?? "فشل تحديث الموقع");
                }
            }
            else
            {
                var dto = new VenueCreateDto
                {
                    TenantID = Session.TenantId,
                    Name = Name,
                    Address = Address,
                    City = City,
                    Country = Country,
                    TotalCapacity = TotalCapacity,
                    MapImageURL = MapImageURL
                };

                var result = await _venueService.CreateAsync(Session.TenantId, dto);
                if (result.IsSuccess)
                {
                    NotificationService.ShowSuccess("تم إضافة الموقع بنجاح ✓");
                    CloseAction?.Invoke();
                }
                else
                {
                    NotificationService.ShowError(result.ErrorMessage ?? "فشل إضافة الموقع");
                }
            }
        }, "خطأ أثناء حفظ الموقع");
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseAction?.Invoke();
    }
}
