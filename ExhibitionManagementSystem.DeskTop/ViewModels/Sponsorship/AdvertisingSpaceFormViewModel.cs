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

public partial class AdvertisingSpaceFormViewModel : ViewModelBase
{
    private readonly ISponsorshipService _sponsorshipService;

    [ObservableProperty] private int _spaceId;
    [ObservableProperty] private int _exhibitionId;
    [ObservableProperty] private int? _venueId;
    [ObservableProperty] private int? _hallId;
    [ObservableProperty] private string _spaceCode = string.Empty;
    [ObservableProperty] private string _spaceName = string.Empty;
    [ObservableProperty] private AdvertisingSpaceType _spaceType = AdvertisingSpaceType.DigitalScreen;
    [ObservableProperty] private string? _dimensionsDescription = "1920x1080px";
    [ObservableProperty] private decimal _basePrice = 2500;
    [ObservableProperty] private string _currencyCode = "LYD";
    [ObservableProperty] private bool _isAvailable = true;
    [ObservableProperty] private string? _locationNotes;
    [ObservableProperty] private string? _photoUrl;
    [ObservableProperty] private bool _isEditMode;

    public ObservableCollection<AdvertisingSpaceType> SpaceTypes { get; } = new(Enum.GetValues<AdvertisingSpaceType>());
    public ObservableCollection<string> Currencies { get; } = ["LYD", "USD", "EUR", "SAR", "AED"];

    public event Action? Saved;

    public AdvertisingSpaceFormViewModel(
        ISponsorshipService sponsorshipService,
        INavigationService navigationService,
        INotificationService notificationService,
        SessionService session)
        : base(navigationService, notificationService, session)
    {
        _sponsorshipService = sponsorshipService;
        Title = "المساحة الإعلانية";
    }

    public void LoadForCreate(int exhibitionId)
    {
        IsEditMode = false;
        SpaceId = 0;
        ExhibitionId = exhibitionId;
        SpaceCode = $"AD-{DateTime.UtcNow:mmss}";
        SpaceName = "شاشة المدخل الرئيسي";
        SpaceType = AdvertisingSpaceType.DigitalScreen;
        DimensionsDescription = "1920x1080px (60Hz)";
        BasePrice = 3000;
        CurrencyCode = "LYD";
        IsAvailable = true;
        LocationNotes = "بجانب مكتب الاستقبال وبوابة التذاكر الرئيسية";
        Title = "إضافة مساحة إعلانية جديدة";
    }

    public void LoadForEdit(AdvertisingSpaceDto space)
    {
        IsEditMode = true;
        SpaceId = space.SpaceID;
        ExhibitionId = space.ExhibitionID;
        VenueId = space.VenueID;
        HallId = space.HallID;
        SpaceCode = space.SpaceCode;
        SpaceName = space.SpaceName;
        SpaceType = space.SpaceType;
        DimensionsDescription = space.DimensionsDescription;
        BasePrice = space.BasePrice;
        CurrencyCode = space.CurrencyCode;
        IsAvailable = space.IsAvailable;
        LocationNotes = space.LocationNotes;
        PhotoUrl = space.PhotoUrl;
        Title = "تعديل المساحة الإعلانية";
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(SpaceCode) || string.IsNullOrWhiteSpace(SpaceName))
        {
            NotificationService.ShowWarning("رمز المساحة واسمها مطلوبان", "تنبيه");
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            if (IsEditMode)
            {
                var dto = new AdvertisingSpaceUpdateDto
                {
                    ExhibitionID = ExhibitionId,
                    VenueID = VenueId,
                    HallID = HallId,
                    SpaceCode = SpaceCode,
                    SpaceName = SpaceName,
                    SpaceType = SpaceType,
                    DimensionsDescription = DimensionsDescription,
                    BasePrice = BasePrice,
                    CurrencyCode = CurrencyCode,
                    IsAvailable = IsAvailable,
                    LocationNotes = LocationNotes,
                    PhotoUrl = PhotoUrl
                };

                var result = await _sponsorshipService.UpdateSpaceAsync(Session.TenantId, SpaceId, dto);
                if (result.IsSuccess)
                {
                    NotificationService.ShowSuccess("تم تحديث المساحة الإعلانية بنجاح", "تم الحفظ");
                    Saved?.Invoke();
                }
                else
                {
                    NotificationService.ShowError(result.ErrorMessage ?? "فشل حفظ المساحة", "خطأ");
                }
            }
            else
            {
                var dto = new AdvertisingSpaceCreateDto
                {
                    ExhibitionID = ExhibitionId,
                    VenueID = VenueId,
                    HallID = HallId,
                    SpaceCode = SpaceCode,
                    SpaceName = SpaceName,
                    SpaceType = SpaceType,
                    DimensionsDescription = DimensionsDescription,
                    BasePrice = BasePrice,
                    CurrencyCode = CurrencyCode,
                    IsAvailable = IsAvailable,
                    LocationNotes = LocationNotes,
                    PhotoUrl = PhotoUrl
                };

                var result = await _sponsorshipService.CreateSpaceAsync(Session.TenantId, dto);
                if (result.IsSuccess)
                {
                    NotificationService.ShowSuccess("تمت إضافة المساحة الإعلانية بنجاح", "تم الحفظ");
                    Saved?.Invoke();
                }
                else
                {
                    NotificationService.ShowError(result.ErrorMessage ?? "فشل حفظ المساحة", "خطأ");
                }
            }
        });
    }
}
