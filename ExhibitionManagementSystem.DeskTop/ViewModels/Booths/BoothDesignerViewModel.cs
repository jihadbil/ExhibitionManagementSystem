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

public partial class BoothDesignerViewModel : ViewModelBase
{
    private readonly IBoothService _boothService;
    private readonly IHallService _hallService;
    private readonly IExhibitionService _exhibitionService;

    // ━━━━━━━━━━━━━━ Collections ━━━━━━━━━━━━━━
    public ObservableCollection<BoothCanvasItem> CanvasBooths { get; } = [];
    public ObservableCollection<HallDto> AvailableHalls { get; } = [];
    public ObservableCollection<ExhibitionSummaryDto> Exhibitions { get; } = [];

    // ━━━━━━━━━━━━━━ Selection ━━━━━━━━━━━━━━
    [ObservableProperty] private int _selectedExhibitionId;
    [ObservableProperty] private int _selectedHallId;
    [ObservableProperty] private BoothCanvasItem? _selectedBooth;

    // ━━━━━━━━━━━━━━ Zoom Factor ━━━━━━━━━━━━━━
    [ObservableProperty] private double _scaleFactor = 1.0;

    // ━━━━━━━━━━━━━━ Properties Panel Binding ━━━━━━━━━━━━━━
    [ObservableProperty] private string _selectedBoothNumber = string.Empty;
    [ObservableProperty] private double _selectedBoothWidth;
    [ObservableProperty] private double _selectedBoothHeight;
    [ObservableProperty] private string _selectedBoothStatus = string.Empty;

    public ObservableCollection<string> Statuses { get; } = new()
    {
        "Available", "Reserved", "PendingReview"
    };

    // ━━━━━━━━━━━━━━ Constructor ━━━━━━━━━━━━━━
    public BoothDesignerViewModel(
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
        Title = "مصمم الأجنحة البصري";
    }

    // ━━━━━━━━━━━━━━ Methods ━━━━━━━━━━━━━━
    public override async Task OnNavigatedToAsync()
    {
        await LoadExhibitionsAsync();
    }

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
        CanvasBooths.Clear();
        SelectedBooth = null;

        if (value > 0)
        {
            await LoadHallsAsync(value);
        }
    }

    private async Task LoadHallsAsync(int exhibitionId)
    {
        await ExecuteSafeAsync(async () =>
        {
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
        SelectedBooth = null;
        if (value > 0)
        {
            await LoadCanvasBoothsAsync();
        }
        else
        {
            CanvasBooths.Clear();
        }
    }

    [RelayCommand]
    private async Task LoadCanvasBoothsAsync()
    {
        if (SelectedHallId == 0) return;

        await ExecuteSafeAsync(async () =>
        {
            var result = await _boothService.GetByHallAsync(Session.TenantId, SelectedHallId);
            if (result.IsSuccess && result.Data is not null)
            {
                CanvasBooths.Clear();
                foreach (var b in result.Data)
                {
                    CanvasBooths.Add(new BoothCanvasItem
                    {
                        BoothID = b.BoothID,
                        BoothNumber = b.BoothNumber,
                        Status = b.Status,
                        X = (double)(b.PosX ?? 20),
                        Y = (double)(b.PosY ?? 20),
                        Width = (double)(b.Width ?? 80),
                        Height = (double)(b.Height ?? 60),
                        IsSelected = false
                    });
                }
            }
        }, "خطأ في تحميل الأجنحة للمصمم");
    }

    async partial void OnSelectedBoothChanged(BoothCanvasItem? value)
    {
        // Deselect previous
        foreach (var item in CanvasBooths)
        {
            item.IsSelected = false;
        }

        if (value is not null)
        {
            value.IsSelected = true;
            SelectedBoothNumber = value.BoothNumber;
            SelectedBoothWidth = value.Width;
            SelectedBoothHeight = value.Height;
            SelectedBoothStatus = value.Status;
        }
        else
        {
            SelectedBoothNumber = string.Empty;
            SelectedBoothWidth = 0;
            SelectedBoothHeight = 0;
            SelectedBoothStatus = string.Empty;
        }
    }

    [RelayCommand]
    private async Task SaveBoothAsync()
    {
        if (SelectedBooth is null) return;

        // Apply temporary property panel values to the item
        SelectedBooth.BoothNumber = SelectedBoothNumber;
        SelectedBooth.Width = SelectedBoothWidth;
        SelectedBooth.Height = SelectedBoothHeight;
        SelectedBooth.Status = SelectedBoothStatus;

        await SaveBoothPositionAsync(SelectedBooth);
    }

    public async Task SaveBoothPositionAsync(BoothCanvasItem item)
    {
        await ExecuteSafeAsync(async () =>
        {
            var dto = new BoothUpdateDto
            {
                BoothNumber = item.BoothNumber,
                Status = item.Status,
                PosX = (decimal)item.X,
                PosY = (decimal)item.Y,
                Width = (decimal)item.Width,
                Height = (decimal)item.Height
            };

            var result = await _boothService.UpdateAsync(Session.TenantId, item.BoothID, dto);
            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess($"تم حفظ الجناح {item.BoothNumber} بنجاح ✓");
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل حفظ موضع الجناح");
            }
        }, "خطأ أثناء حفظ موضع الجناح");
    }

    [RelayCommand]
    private void ZoomIn()
    {
        ScaleFactor = Math.Min(ScaleFactor + 0.1, 3.0);
    }

    [RelayCommand]
    private void ZoomOut()
    {
        ScaleFactor = Math.Max(ScaleFactor - 0.1, 0.5);
    }

    [RelayCommand]
    private void AutoArrange()
    {
        if (CanvasBooths.Count == 0) return;

        const double startX = 30;
        const double startY = 30;
        const double gapX = 20;
        const double gapY = 20;
        const int columnsCount = 6;

        for (int i = 0; i < CanvasBooths.Count; i++)
        {
            int col = i % columnsCount;
            int row = i / columnsCount;

            var item = CanvasBooths[i];
            item.X = startX + col * (item.Width + gapX);
            item.Y = startY + row * (item.Height + gapY);
        }

        NotificationService.ShowInfo("تم ترتيب الأجنحة تلقائياً. يرجى حفظ التغييرات لكل جناح عند تحديده.");
    }

    [RelayCommand]
    private void GoBack()
    {
        NavigationService.NavigateTo<BoothsPage>();
    }
}
