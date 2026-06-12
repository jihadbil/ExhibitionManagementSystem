using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.DeskTop.Helpers;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using ExhibitionManagementSystem.Models.DTOs.Venue;
using ExhibitionManagementSystem.Models.DTOs.Hall;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Venues;

public partial class VenuesViewModel : ViewModelBase
{
    private readonly IVenueService _venueService;
    private readonly IHallService _hallService;

    // ━━━━━━━━━━━━━━ Properties ━━━━━━━━━━━━━━

    public ObservableCollection<VenueDto> Venues { get; } = [];
    public ObservableCollection<HallDto> Halls { get; } = [];

    [ObservableProperty]
    private VenueDto? _selectedVenue;

    [ObservableProperty]
    private bool _isVenueSelected;

    // ━━━━━━━━━━━━━━ Constructor ━━━━━━━━━━━━━━

    public VenuesViewModel(
        IVenueService venueService,
        IHallService hallService,
        INavigationService navigationService,
        INotificationService notificationService,
        SessionService session) : base(navigationService, notificationService, session)
    {
        _venueService = venueService;
        _hallService = hallService;
        Title = "إدارة المواقع والقاعات";
    }

    // ━━━━━━━━━━━━━━ Handlers ━━━━━━━━━━━━━━

    partial void OnSelectedVenueChanged(VenueDto? value)
    {
        IsVenueSelected = value is not null;
        Halls.Clear();
        if (value is not null)
        {
            _ = LoadHallsAsync(value.VenueID);
        }
    }

    // ━━━━━━━━━━━━━━ Commands ━━━━━━━━━━━━━━

    [RelayCommand]
    public async Task LoadVenuesAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            // Store selected venue ID to try and restore selection after reload
            var previousSelectedId = SelectedVenue?.VenueID ?? 0;

            var result = await _venueService.GetByTenantAsync(Session.TenantId);
            if (result.IsSuccess && result.Data is not null)
            {
                Venues.Clear();
                VenueDto? newSelectedVenue = null;

                foreach (var venue in result.Data)
                {
                    Venues.Add(venue);
                    if (venue.VenueID == previousSelectedId)
                    {
                        newSelectedVenue = venue;
                    }
                }

                // Restore selection or select the first venue if there's any
                if (newSelectedVenue is not null)
                {
                    SelectedVenue = newSelectedVenue;
                }
                else if (Venues.Count > 0)
                {
                    SelectedVenue = Venues[0];
                }
                else
                {
                    SelectedVenue = null;
                }
            }
        }, "خطأ في تحميل المواقع");
    }

    private async Task LoadHallsAsync(int venueId)
    {
        await ExecuteSafeAsync(async () =>
        {
            var result = await _hallService.GetByVenueAsync(Session.TenantId, venueId);
            if (result.IsSuccess && result.Data is not null)
            {
                Halls.Clear();
                foreach (var hall in result.Data)
                {
                    Halls.Add(hall);
                }
            }
        }, "خطأ في تحميل القاعات للموقع المحدد");
    }

    [RelayCommand]
    private async Task DeleteVenueAsync(int venueId)
    {
        await ExecuteSafeAsync(async () =>
        {
            var result = await _venueService.DeleteAsync(Session.TenantId, venueId);
            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess("تم حذف الموقع بنجام ✓");
                await LoadVenuesAsync();
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل حذف الموقع");
            }
        }, "خطأ أثناء حذف الموقع");
    }

    [RelayCommand]
    private async Task DeleteHallAsync(int hallId)
    {
        if (SelectedVenue is null) return;

        await ExecuteSafeAsync(async () =>
        {
            var result = await _hallService.DeleteAsync(Session.TenantId, hallId);
            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess("تم حذف القاعة بنجاح ✓");
                // Reload halls for current venue
                await LoadHallsAsync(SelectedVenue.VenueID);
                // Also update venue list to refresh halls count
                await LoadVenuesAsync();
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل حذف القاعة");
            }
        }, "خطأ أثناء حذف القاعة");
    }

    public override async Task OnNavigatedToAsync()
    {
        await LoadVenuesAsync();
    }
}
