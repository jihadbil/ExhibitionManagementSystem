using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.DeskTop.Helpers;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.Models.DTOs.Venue;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Exhibitions;

public partial class ExhibitionFormViewModel : ViewModelBase
{
    private readonly IExhibitionService _exhibitionService;
    private readonly IVenueService _venueService;

    // ━━━━━━━━━━━━━━ Properties ━━━━━━━━━━━━━━

    [ObservableProperty]
    private int _exhibitionId;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _type = "Tech";

    [ObservableProperty]
    private string _edition = string.Empty;

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today.AddDays(3);

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private int? _expectedVisitors;

    [ObservableProperty]
    private decimal? _entryFee;

    [ObservableProperty]
    private string _entryCurrency = "SAR";

    [ObservableProperty]
    private string _status = "Upcoming";

    [ObservableProperty]
    private int _selectedVenueId;

    [ObservableProperty]
    private bool _isEditMode;

    public ObservableCollection<VenueSummaryDto> Venues { get; } = [];
    public ObservableCollection<string> ExhibitionTypes { get; } = new()
    {
        "Tech", "Medical", "Industrial", "Commercial", "Educational", "Automotive"
    };

    public ObservableCollection<string> Statuses { get; } = new()
    {
        "Upcoming", "Active", "Ended", "Pending", "Cancelled"
    };

    public Action? CloseAction { get; set; }

    // ━━━━━━━━━━━━━━ Constructor ━━━━━━━━━━━━━━

    public ExhibitionFormViewModel(
        IExhibitionService exhibitionService,
        IVenueService venueService,
        INavigationService navigationService,
        INotificationService notificationService,
        SessionService session) : base(navigationService, notificationService, session)
    {
        _exhibitionService = exhibitionService;
        _venueService = venueService;
        Title = "إضافة معرض جديد";
    }

    // ━━━━━━━━━━━━━━ Methods ━━━━━━━━━━━━━━

    public async Task InitializeAsync(int exhibitionId = 0)
    {
        ExhibitionId = exhibitionId;
        IsEditMode = exhibitionId > 0;
        Title = IsEditMode ? "تعديل المعرض" : "إضافة معرض جديد";

        await LoadVenuesAsync();

        if (IsEditMode)
        {
            await LoadExhibitionDetailsAsync();
        }
    }

    private async Task LoadVenuesAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            var result = await _venueService.GetSummariesAsync(Session.TenantId);
            if (result.IsSuccess && result.Data is not null)
            {
                Venues.Clear();
                foreach (var venue in result.Data)
                {
                    if (venue.IsActive)
                    {
                        Venues.Add(venue);
                    }
                }

                if (Venues.Count > 0 && SelectedVenueId == 0)
                {
                    SelectedVenueId = Venues[0].VenueID;
                }
            }
        }, "خطأ في تحميل مواقع المعارض");
    }

    private async Task LoadExhibitionDetailsAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            var result = await _exhibitionService.GetByIdAsync(Session.TenantId, ExhibitionId);
            if (result.IsSuccess && result.Data is not null)
            {
                var data = result.Data;
                Name = data.Name;
                Type = data.Type;
                Edition = data.Edition;
                StartDate = data.StartDate;
                EndDate = data.EndDate;
                Description = data.Description;
                ExpectedVisitors = data.ExpectedVisitors;
                EntryFee = data.EntryFee;
                EntryCurrency = data.EntryCurrency;
                Status = data.Status;
                SelectedVenueId = data.VenueID;
            }
        }, "خطأ في تحميل تفاصيل المعرض");
    }

    // ━━━━━━━━━━━━━━ Commands ━━━━━━━━━━━━━━

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            NotificationService.ShowError("الرجاء إدخال اسم المعرض");
            return;
        }

        if (SelectedVenueId == 0)
        {
            NotificationService.ShowError("الرجاء اختيار موقع المعرض");
            return;
        }

        if (StartDate > EndDate)
        {
            NotificationService.ShowError("تاريخ البدء لا يمكن أن يكون بعد تاريخ الانتهاء");
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            if (IsEditMode)
            {
                var dto = new ExhibitionUpdateDto
                {
                    Name = Name,
                    Type = Type,
                    Edition = Edition,
                    StartDate = StartDate,
                    EndDate = EndDate,
                    Description = Description,
                    ExpectedVisitors = ExpectedVisitors,
                    EntryFee = EntryFee,
                    EntryCurrency = EntryCurrency,
                    Status = Status
                };

                var result = await _exhibitionService.UpdateAsync(Session.TenantId, ExhibitionId, dto);
                if (result.IsSuccess)
                {
                    NotificationService.ShowSuccess("تم تحديث المعرض بنجاح ✓");
                    CloseAction?.Invoke();
                }
                else
                {
                    NotificationService.ShowError(result.ErrorMessage ?? "فشل تحديث المعرض");
                }
            }
            else
            {
                var dto = new ExhibitionCreateDto
                {
                    TenantID = Session.TenantId,
                    VenueID = SelectedVenueId,
                    Name = Name,
                    Type = Type,
                    Edition = Edition,
                    StartDate = StartDate,
                    EndDate = EndDate,
                    Description = Description,
                    ExpectedVisitors = ExpectedVisitors,
                    EntryFee = EntryFee,
                    EntryCurrency = EntryCurrency
                };

                var result = await _exhibitionService.CreateAsync(Session.TenantId, dto);
                if (result.IsSuccess)
                {
                    NotificationService.ShowSuccess("تم إضافة المعرض بنجاح ✓");
                    CloseAction?.Invoke();
                }
                else
                {
                    NotificationService.ShowError(result.ErrorMessage ?? "فشل إضافة المعرض");
                }
            }
        }, "خطأ أثناء الحفظ");
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseAction?.Invoke();
    }
}
