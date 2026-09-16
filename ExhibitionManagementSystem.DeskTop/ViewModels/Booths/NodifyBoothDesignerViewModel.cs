using System;
using System.Collections.Generic;
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
using ExhibitionManagementSystem.Models.DTOs.Booth;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.Models.DTOs.Hall;
using ExhibitionManagementSystem.Models.DTOs.Exhibitor;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.DeskTop.Views.Booths;
using System.Windows.Controls;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Booths;

public partial class NodifyBoothDesignerViewModel : ViewModelBase
{
    private readonly IBoothService _boothService;
    private readonly IHallService _hallService;
    private readonly IExhibitionService _exhibitionService;
    private readonly IExhibitorService _exhibitorService;

    // ━━━━━━━━━━━━━━ Collections ━━━━━━━━━━━━━━
    public ObservableCollection<BoothNodeViewModel> BoothNodes { get; } = [];
    public ObservableCollection<HallDto> AvailableHalls { get; } = [];
    public ObservableCollection<ExhibitionSummaryDto> Exhibitions { get; } = [];
    public ObservableCollection<ExhibitorSummaryDto> UnassignedExhibitors { get; } = [];

    // ━━━━━━━━━━━━━━ Selection ━━━━━━━━━━━━━━
    [ObservableProperty] private int _selectedExhibitionId;
    [ObservableProperty] private int _selectedHallId;
    [ObservableProperty] private BoothNodeViewModel? _selectedNode;
    [ObservableProperty] private bool _isMultiSelected;
    [ObservableProperty] private ExhibitorSummaryDto? _selectedExhibitor;

    // ━━━━━━━━━━━━━━ Hall Boundaries ━━━━━━━━━━━━━━
    [ObservableProperty] private double _hallWidth = 1000;
    [ObservableProperty] private double _hallHeight = 800;
    [ObservableProperty] private double _hallWidthMeters = 50;
    [ObservableProperty] private double _hallHeightMeters = 40;
    [ObservableProperty] private string _hallName = string.Empty;

    // ━━━━━━━━━━━━━━ Viewport (Zoom & Pan) ━━━━━━━━━━━━━━
    [ObservableProperty] private double _viewportZoom = 1.0;
    [ObservableProperty] private Point _viewportLocation = new(0, 0);

    // ━━━━━━━━━━━━━━ Properties Panel Binding ━━━━━━━━━━━━━━
    [ObservableProperty] private string _editBoothNumber = string.Empty;
    [ObservableProperty] private double _editWidth;
    [ObservableProperty] private double _editHeight;
    [ObservableProperty] private string _editStatus = string.Empty;
    [ObservableProperty] private double _editRotation;
    [ObservableProperty] private string _editShapeType = "Rectangle";
    [ObservableProperty] private bool _editIsVIP;

    // ━━━━━━━━━━━━━━ Background Image & Coordinates ━━━━━━━━━━━━━━
    [ObservableProperty] private string _backgroundImagePath = string.Empty;
    [ObservableProperty] private string _cursorCoordinateString = "0.0م , 0.0م";

    // ━━━━━━━━━━━━━━ Occupancy Statistics Card ━━━━━━━━━━━━━━
    [ObservableProperty] private int _totalBoothsCount;
    [ObservableProperty] private double _totalReservedArea;
    [ObservableProperty] private double _totalAvailableArea;
    [ObservableProperty] private double _occupancyPercentage;

    public ObservableCollection<string> Statuses { get; } = new()
    {
        "Available", "Reserved", "PendingReview"
    };

    public ObservableCollection<string> Shapes { get; } = new()
    {
        "Rectangle", "LShape", "Triangle", "Trapezoid"
    };

    private const double MeterToPixel = 20.0;

    // ━━━━━━━━━━━━━━ Constructor ━━━━━━━━━━━━━━
    public NodifyBoothDesignerViewModel(
        IBoothService boothService,
        IHallService hallService,
        IExhibitionService exhibitionService,
        IExhibitorService exhibitorService,
        INavigationService navigationService,
        INotificationService notificationService,
        SessionService session) : base(navigationService, notificationService, session)
    {
        _boothService = boothService;
        _hallService = hallService;
        _exhibitionService = exhibitionService;
        _exhibitorService = exhibitorService;
        Title = "مصمم الأجنحة المطور (Nodify)";
    }

    public INotificationService Notifications => NotificationService;

    // ━━━━━━━━━━━━━━ Navigation Methods ━━━━━━━━━━━━━━
    public override async Task OnNavigatedToAsync()
    {
        await LoadExhibitionsAsync();
        await LoadExhibitorsAsync();
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
        BoothNodes.Clear();
        SelectedNode = null;

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
        SelectedNode = null;
        if (value > 0)
        {
            var selectedHall = AvailableHalls.FirstOrDefault(h => h.HallID == value);
            if (selectedHall is not null)
            {
                HallWidthMeters = (double)(selectedHall.FloorPlanWidth ?? 50);
                HallHeightMeters = (double)(selectedHall.FloorPlanHeight ?? 40);
                HallWidth = HallWidthMeters * MeterToPixel;
                HallHeight = HallHeightMeters * MeterToPixel;
                HallName = selectedHall.HallName;

                // Load background image path from JSON
                BackgroundImagePath = string.Empty;
                if (!string.IsNullOrWhiteSpace(selectedHall.FloorPlanJSON))
                {
                    try
                    {
                        var data = System.Text.Json.JsonSerializer.Deserialize<HallFloorPlanData>(selectedHall.FloorPlanJSON);
                        BackgroundImagePath = data?.BackgroundImagePath ?? string.Empty;
                    }
                    catch
                    {
                        // Fallback in case raw path was saved directly
                        BackgroundImagePath = selectedHall.FloorPlanJSON;
                    }
                }
            }
            await LoadBoothNodesAsync();
        }
        else
        {
            BoothNodes.Clear();
            HallWidth = 1000;
            HallHeight = 800;
            HallWidthMeters = 50;
            HallHeightMeters = 40;
            HallName = string.Empty;
            BackgroundImagePath = string.Empty;
        }
    }

    [RelayCommand]
    private async Task LoadBoothNodesAsync()
    {
        if (SelectedHallId == 0) return;

        await ExecuteSafeAsync(async () =>
        {
            var result = await _boothService.GetByHallAsync(Session.TenantId, SelectedHallId);
            if (result.IsSuccess && result.Data is not null)
            {
                BoothNodes.Clear();
                foreach (var b in result.Data)
                {
                    bool isVIP = false;
                    if (!string.IsNullOrEmpty(b.ShapePolygonJSON))
                    {
                        try
                        {
                            var meta = System.Text.Json.JsonSerializer.Deserialize<BoothMetadata>(b.ShapePolygonJSON);
                            isVIP = meta?.IsVIP ?? false;
                        }
                        catch {}
                    }

                    var node = new BoothNodeViewModel
                    {
                        BoothID = b.BoothID,
                        BoothNumber = b.BoothNumber,
                        Status = b.Status,
                        Location = new Point(
                            (double)((b.PosX ?? 10) * (decimal)MeterToPixel),
                            (double)((b.PosY ?? 10) * (decimal)MeterToPixel)
                        ),
                        Width = (double)((b.Width ?? 4) * (decimal)MeterToPixel),
                        Height = (double)((b.Height ?? 3) * (decimal)MeterToPixel),
                        RotationAngle = (double)(b.RotationAngle ?? 0),
                        ShapeType = b.ShapeType ?? "Rectangle",
                        ShapePolygonJSON = b.ShapePolygonJSON ?? string.Empty,
                        IsSelected = false,
                        IsVIP = isVIP
                    };

                    BoothNodes.Add(node);
                }
                CheckForCollisions();
            }
        }, "خطأ في تحميل الأجنحة للمصمم");
    }

    async partial void OnSelectedNodeChanged(BoothNodeViewModel? value)
    {
        if (value is not null)
        {
            value.IsSelected = true;
            EditBoothNumber = value.BoothNumber;
            EditWidth = value.Width / MeterToPixel;
            EditHeight = value.Height / MeterToPixel;
            EditStatus = value.Status;
            EditRotation = value.RotationAngle;
            EditShapeType = value.ShapeType;
            EditIsVIP = value.IsVIP;
        }
        else
        {
            EditBoothNumber = string.Empty;
            EditWidth = 0;
            EditHeight = 0;
            EditStatus = string.Empty;
            EditRotation = 0;
            EditShapeType = "Rectangle";
            EditIsVIP = false;
        }
    }

    [RelayCommand]
    private async Task SaveSelectedBoothAsync()
    {
        if (SelectedNode is null) return;

        SelectedNode.BoothNumber = EditBoothNumber;
        SelectedNode.Width = EditWidth * MeterToPixel;
        SelectedNode.Height = EditHeight * MeterToPixel;
        SelectedNode.Status = EditStatus;
        SelectedNode.RotationAngle = EditRotation;
        SelectedNode.ShapeType = EditShapeType;
        SelectedNode.IsVIP = EditIsVIP;

        await SaveBoothPositionAsync(SelectedNode);
    }

    public async Task SaveBoothPositionAsync(BoothNodeViewModel item)
    {
        // Serialize metadata (such as IsVIP) to ShapePolygonJSON
        var metadata = new BoothMetadata { IsVIP = item.IsVIP };
        item.ShapePolygonJSON = System.Text.Json.JsonSerializer.Serialize(metadata);

        await ExecuteSafeAsync(async () =>
        {
            var dto = new BoothUpdateDto
            {
                BoothNumber = item.BoothNumber,
                Status = item.Status,
                PosX = (decimal)(item.Location.X / MeterToPixel),
                PosY = (decimal)(item.Location.Y / MeterToPixel),
                Width = (decimal)(item.Width / MeterToPixel),
                Height = (decimal)(item.Height / MeterToPixel),
                RotationAngle = (decimal)item.RotationAngle,
                ShapeType = item.ShapeType,
                ShapePolygonJSON = item.ShapePolygonJSON
            };

            var result = await _boothService.UpdateAsync(Session.TenantId, item.BoothID, dto);
            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess($"تم حفظ الجناح {item.BoothNumber} بنجاح ✓");
                CheckForCollisions();
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل حفظ موضع الجناح");
            }
        }, "خطأ أثناء حفظ موضع الجناح");
    }

    public void CheckForCollisions()
    {
        foreach (var b in BoothNodes)
        {
            b.HasCollision = false;
        }

        for (int i = 0; i < BoothNodes.Count; i++)
        {
            var b1 = BoothNodes[i];
            for (int j = i + 1; j < BoothNodes.Count; j++)
            {
                var b2 = BoothNodes[j];
                if (b1.Location.X < b2.Location.X + b2.Width &&
                    b1.Location.X + b1.Width > b2.Location.X &&
                    b1.Location.Y < b2.Location.Y + b2.Height &&
                    b1.Location.Y + b1.Height > b2.Location.Y)
                {
                    b1.HasCollision = true;
                    b2.HasCollision = true;
                }
            }
        }

        UpdateStatistics();
    }

    [RelayCommand]
    private async Task AddNewBoothAsync()
    {
        if (SelectedHallId == 0) return;

        // Auto-generate booth number
        int nextNum = 1;
        if (BoothNodes.Count > 0)
        {
            var numbers = BoothNodes.Select(b => {
                var digits = new string(b.BoothNumber.Where(char.IsDigit).ToArray());
                return int.TryParse(digits, out int val) ? val : 0;
            }).ToList();
            if (numbers.Count > 0)
            {
                nextNum = numbers.Max() + 1;
            }
        }
        string newBoothNumber = $"B-{nextNum:00}";

        var dto = new BoothCreateDto
        {
            HallID = SelectedHallId,
            BoothNumber = newBoothNumber,
            Width = 4.0m,
            Height = 3.0m,
            OriginalAreaSqM = 12.0m,
            PosX = 5.0m,
            PosY = 5.0m,
            ShapeType = "Rectangle",
            RotationAngle = 0
        };

        await ExecuteSafeAsync(async () =>
        {
            var result = await _boothService.CreateAsync(Session.TenantId, dto);
            if (result.IsSuccess && result.Data is not null)
            {
                var newBooth = result.Data;
                var newItem = new BoothNodeViewModel
                {
                    BoothID = newBooth.BoothID,
                    BoothNumber = newBooth.BoothNumber,
                    Status = newBooth.Status,
                    Location = new Point(
                        (double)((newBooth.PosX ?? 5) * (decimal)MeterToPixel),
                        (double)((newBooth.PosY ?? 5) * (decimal)MeterToPixel)
                    ),
                    Width = (double)((newBooth.Width ?? 4) * (decimal)MeterToPixel),
                    Height = (double)((newBooth.Height ?? 3) * (decimal)MeterToPixel),
                    RotationAngle = (double)(newBooth.RotationAngle ?? 0),
                    ShapeType = newBooth.ShapeType ?? "Rectangle",
                    IsSelected = false
                };
                BoothNodes.Add(newItem);
                SelectedNode = newItem;
                CheckForCollisions();
                NotificationService.ShowSuccess($"تم إنشاء الجناح الجديد {newBoothNumber} بنجاح ✓");
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل إنشاء جناح جديد");
            }
        }, "خطأ أثناء إنشاء جناح جديد");
    }

    [RelayCommand]
    private async Task DeleteSelectedBoothAsync()
    {
        if (SelectedNode is null) return;

        var confirm = MessageBox.Show(
            $"هل أنت متأكد من رغبتك في حذف الجناح {SelectedNode.BoothNumber}؟",
            "تأكيد الحذف",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        await ExecuteSafeAsync(async () =>
        {
            var result = await _boothService.DeleteAsync(Session.TenantId, SelectedNode.BoothID);
            if (result.IsSuccess)
            {
                var removedNumber = SelectedNode.BoothNumber;
                BoothNodes.Remove(SelectedNode);
                SelectedNode = null;
                CheckForCollisions();
                NotificationService.ShowSuccess($"تم حذف الجناح {removedNumber} بنجاح ✓");
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل حذف الجناح");
            }
        }, "خطأ أثناء حذف الجناح");
    }

    [RelayCommand]
    private void AutoArrange()
    {
        if (BoothNodes.Count == 0) return;

        const double startX = 30;
        const double startY = 30;
        const double gapX = 20;
        const double gapY = 20;
        const int columnsCount = 6;

        for (int i = 0; i < BoothNodes.Count; i++)
        {
            int col = i % columnsCount;
            int row = i / columnsCount;

            var item = BoothNodes[i];
            item.Location = new Point(
                startX + col * (item.Width + gapX),
                startY + row * (item.Height + gapY)
            );
        }

        NotificationService.ShowInfo("تم ترتيب الأجنحة تلقائياً. يرجى حفظ التغييرات لكل جناح أو تحريكه ليتم الحفظ.");
        CheckForCollisions();
    }

    [RelayCommand]
    private async Task UpdateHallDimensionsAsync()
    {
        if (SelectedHallId == 0) return;

        var selectedHall = AvailableHalls.FirstOrDefault(h => h.HallID == SelectedHallId);
        if (selectedHall is null) return;

        var dto = new HallUpdateDto
        {
            HallName = selectedHall.HallName,
            AreaSqM = (decimal?)(HallWidthMeters * HallHeightMeters),
            MaxBooths = selectedHall.MaxBooths,
            IsActive = selectedHall.IsActive,
            FloorPlanJSON = selectedHall.FloorPlanJSON,
            FloorPlanWidth = (decimal)HallWidthMeters,
            FloorPlanHeight = (decimal)HallHeightMeters
        };

        await ExecuteSafeAsync(async () =>
        {
            var result = await _hallService.UpdateAsync(Session.TenantId, SelectedHallId, dto);
            if (result.IsSuccess && result.Data is not null)
            {
                selectedHall.FloorPlanWidth = (decimal)HallWidthMeters;
                selectedHall.FloorPlanHeight = (decimal)HallHeightMeters;
                selectedHall.AreaSqM = dto.AreaSqM;

                HallWidth = HallWidthMeters * MeterToPixel;
                HallHeight = HallHeightMeters * MeterToPixel;

                NotificationService.ShowSuccess("تم تحديث أبعاد الصالة بنجاح ✓");
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل تحديث أبعاد الصالة");
            }
        }, "خطأ أثناء تحديث أبعاد الصالة");
    }

    [RelayCommand]
    private void ZoomIn()
    {
        ViewportZoom = Math.Min(ViewportZoom + 0.1, 3.0);
    }

    [RelayCommand]
    private void ZoomOut()
    {
        ViewportZoom = Math.Max(ViewportZoom - 0.1, 0.5);
    }

    [RelayCommand]
    private void ResetZoom()
    {
        ViewportZoom = 1.0;
        ViewportLocation = new Point(0, 0);
    }

    [RelayCommand]
    private void GoBack()
    {
        NavigationService.NavigateTo<BoothsPage>();
    }

    partial void OnEditBoothNumberChanged(string value)
    {
        if (SelectedNode != null && SelectedNode.BoothNumber != value)
        {
            SelectedNode.BoothNumber = value;
        }
    }

    partial void OnEditWidthChanged(double value)
    {
        if (SelectedNode != null)
        {
            double newWidth = value * MeterToPixel;
            if (Math.Abs(SelectedNode.Width - newWidth) > 0.01)
            {
                SelectedNode.Width = newWidth;
                CheckForCollisions();
            }
        }
    }

    partial void OnEditHeightChanged(double value)
    {
        if (SelectedNode != null)
        {
            double newHeight = value * MeterToPixel;
            if (Math.Abs(SelectedNode.Height - newHeight) > 0.01)
            {
                SelectedNode.Height = newHeight;
                CheckForCollisions();
            }
        }
    }

    partial void OnEditStatusChanged(string value)
    {
        if (SelectedNode != null && SelectedNode.Status != value)
        {
            SelectedNode.Status = value;
        }
    }

    partial void OnEditShapeTypeChanged(string value)
    {
        if (SelectedNode != null && SelectedNode.ShapeType != value)
        {
            SelectedNode.ShapeType = value;
        }
    }

    partial void OnEditRotationChanged(double value)
    {
        if (SelectedNode != null && Math.Abs(SelectedNode.RotationAngle - value) > 0.01)
        {
            SelectedNode.RotationAngle = value;
        }
    }

    // ━━━━━━━━━━━━━━ Background Image Overlay Commands ━━━━━━━━━━━━━━

    [RelayCommand]
    private async Task UploadBackgroundImageAsync()
    {
        if (SelectedHallId == 0) return;

        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.svg|All Files|*.*",
            Title = "اختر صورة مخطط الخلفية"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            string selectedFile = openFileDialog.FileName;
            BackgroundImagePath = selectedFile;

            // Save to database
            var selectedHall = AvailableHalls.FirstOrDefault(h => h.HallID == SelectedHallId);
            if (selectedHall is not null)
            {
                var floorData = new HallFloorPlanData { BackgroundImagePath = selectedFile };
                string json = System.Text.Json.JsonSerializer.Serialize(floorData);
                selectedHall.FloorPlanJSON = json;

                var dto = new HallUpdateDto
                {
                    HallName = selectedHall.HallName,
                    AreaSqM = selectedHall.AreaSqM,
                    MaxBooths = selectedHall.MaxBooths,
                    IsActive = selectedHall.IsActive,
                    FloorPlanJSON = json,
                    FloorPlanWidth = selectedHall.FloorPlanWidth,
                    FloorPlanHeight = selectedHall.FloorPlanHeight
                };

                await ExecuteSafeAsync(async () =>
                {
                    var result = await _hallService.UpdateAsync(Session.TenantId, SelectedHallId, dto);
                    if (result.IsSuccess)
                    {
                        NotificationService.ShowSuccess("تم رفع مخطط الخلفية وحفظه بنجاح ✓");
                    }
                }, "خطأ في حفظ مخطط الخلفية");
            }
        }
    }

    [RelayCommand]
    private async Task RemoveBackgroundImageAsync()
    {
        if (SelectedHallId == 0) return;

        BackgroundImagePath = string.Empty;

        var selectedHall = AvailableHalls.FirstOrDefault(h => h.HallID == SelectedHallId);
        if (selectedHall is not null)
        {
            selectedHall.FloorPlanJSON = string.Empty;

            var dto = new HallUpdateDto
            {
                HallName = selectedHall.HallName,
                AreaSqM = selectedHall.AreaSqM,
                MaxBooths = selectedHall.MaxBooths,
                IsActive = selectedHall.IsActive,
                FloorPlanJSON = string.Empty,
                FloorPlanWidth = selectedHall.FloorPlanWidth,
                FloorPlanHeight = selectedHall.FloorPlanHeight
            };

            await ExecuteSafeAsync(async () =>
            {
                await _hallService.UpdateAsync(Session.TenantId, SelectedHallId, dto);
                NotificationService.ShowSuccess("تم إزالة مخطط الخلفية ✓");
            }, "خطأ في تعديل مخطط الخلفية");
        }
    }

    // ━━━━━━━━━━━━━━ Bulk Selection & Alignment Commands ━━━━━━━━━━━━━━

    [RelayCommand]
    private void AlignSelectedLeft()
    {
        var selected = BoothNodes.Where(n => n.IsSelected).ToList();
        if (selected.Count < 2) return;

        double minX = selected.Min(n => n.Location.X);
        foreach (var node in selected)
        {
            node.Location = new Point(minX, node.Location.Y);
            _ = SaveBoothPositionAsync(node);
        }
        CheckForCollisions();
        NotificationService.ShowSuccess("تمت محاذاة الأجنحة لليسار ✓");
    }

    [RelayCommand]
    private void AlignSelectedTop()
    {
        var selected = BoothNodes.Where(n => n.IsSelected).ToList();
        if (selected.Count < 2) return;

        double minY = selected.Min(n => n.Location.Y);
        foreach (var node in selected)
        {
            node.Location = new Point(node.Location.X, minY);
            _ = SaveBoothPositionAsync(node);
        }
        CheckForCollisions();
        NotificationService.ShowSuccess("تمت محاذاة الأجنحة للأعلى ✓");
    }

    [RelayCommand]
    private async Task DeleteSelectedNodesAsync()
    {
        var selected = BoothNodes.Where(n => n.IsSelected).ToList();
        if (selected.Count == 0) return;

        var confirm = MessageBox.Show(
            $"هل أنت متأكد من حذف {selected.Count} أجنحة محددة؟",
            "تأكيد الحذف الجماعي",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        await ExecuteSafeAsync(async () =>
        {
            foreach (var node in selected)
            {
                var result = await _boothService.DeleteAsync(Session.TenantId, node.BoothID);
                if (result.IsSuccess)
                {
                    BoothNodes.Remove(node);
                }
            }
            SelectedNode = null;
            UpdateStatistics();
            NotificationService.ShowSuccess("تم حذف الأجنحة المحددة بنجاح ✓");
        }, "خطأ أثناء الحذف الجماعي");
    }

    [RelayCommand]
    private async Task BulkChangeStatusAsync(string newStatus)
    {
        if (string.IsNullOrEmpty(newStatus)) return;
        var selected = BoothNodes.Where(n => n.IsSelected).ToList();
        if (selected.Count == 0) return;

        await ExecuteSafeAsync(async () =>
        {
            foreach (var node in selected)
            {
                node.Status = newStatus;
                
                var dto = new BoothUpdateDto
                {
                    BoothNumber = node.BoothNumber,
                    Status = node.Status,
                    PosX = (decimal)(node.Location.X / MeterToPixel),
                    PosY = (decimal)(node.Location.Y / MeterToPixel),
                    Width = (decimal)(node.Width / MeterToPixel),
                    Height = (decimal)(node.Height / MeterToPixel),
                    RotationAngle = (decimal)node.RotationAngle,
                    ShapeType = node.ShapeType,
                    ShapePolygonJSON = node.ShapePolygonJSON
                };
                await _boothService.UpdateAsync(Session.TenantId, node.BoothID, dto);
            }
            UpdateStatistics();
            NotificationService.ShowSuccess($"تم تغيير حالة الأجنحة المحددة إلى: {newStatus} ✓");
        }, "خطأ في تعديل الحالة الجماعي");
    }

    // ━━━━━━━━━━━━━━ Statistics Calculation ━━━━━━━━━━━━━━

    public void UpdateStatistics()
    {
        TotalBoothsCount = BoothNodes.Count;
        double reserved = 0;
        double available = 0;
        foreach (var node in BoothNodes)
        {
            double area = (node.Width / MeterToPixel) * (node.Height / MeterToPixel);
            if (node.Status == "Reserved" || node.Status == "محجوز")
                reserved += area;
            else
                available += area;
        }
        TotalReservedArea = Math.Round(reserved, 1);
        TotalAvailableArea = Math.Round(available, 1);
        double total = reserved + available;
        OccupancyPercentage = total > 0 ? Math.Round((reserved / total) * 100, 1) : 0;
    }
}

public class HallFloorPlanData
{
    public string BackgroundImagePath { get; set; } = string.Empty;
}

public class BoothMetadata
{
    public bool IsVIP { get; set; } = false;
}

public partial class NodifyBoothDesignerViewModel
{
    // ━━━━━━━━━━━━━━ Undo / Redo History System ━━━━━━━━━━━━━━
    private readonly Stack<List<BoothSnapshot>> _undoStack = new();
    private readonly Stack<List<BoothSnapshot>> _redoStack = new();

    public void SaveHistoryState()
    {
        _undoStack.Push(TakeSnapshot());
        _redoStack.Clear();
    }

    private List<BoothSnapshot> TakeSnapshot()
    {
        return BoothNodes.Select(b => new BoothSnapshot
        {
            BoothID = b.BoothID,
            X = b.Location.X,
            Y = b.Location.Y,
            Width = b.Width,
            Height = b.Height,
            Status = b.Status,
            BoothNumber = b.BoothNumber,
            RotationAngle = b.RotationAngle,
            ShapeType = b.ShapeType,
            FurnitureJSON = b.IsVIP ? "VIP" : string.Empty
        }).ToList();
    }

    private async Task RestoreSnapshotAsync(List<BoothSnapshot> snapshot)
    {
        foreach (var snap in snapshot)
        {
            var node = BoothNodes.FirstOrDefault(b => b.BoothID == snap.BoothID);
            if (node is not null)
            {
                node.Location = new Point(snap.X, snap.Y);
                node.Width = snap.Width;
                node.Height = snap.Height;
                node.Status = snap.Status;
                node.BoothNumber = snap.BoothNumber;
                node.RotationAngle = snap.RotationAngle;
                node.ShapeType = snap.ShapeType;
                node.IsVIP = snap.FurnitureJSON == "VIP";

                // Save to API
                var dto = new BoothUpdateDto
                {
                    BoothNumber = node.BoothNumber,
                    Status = node.Status,
                    PosX = (decimal)(node.Location.X / MeterToPixel),
                    PosY = (decimal)(node.Location.Y / MeterToPixel),
                    Width = (decimal)(node.Width / MeterToPixel),
                    Height = (decimal)(node.Height / MeterToPixel),
                    RotationAngle = (decimal)node.RotationAngle,
                    ShapeType = node.ShapeType,
                    ShapePolygonJSON = System.Text.Json.JsonSerializer.Serialize(new BoothMetadata { IsVIP = node.IsVIP })
                };
                await _boothService.UpdateAsync(Session.TenantId, node.BoothID, dto);
            }
        }
        CheckForCollisions();
    }

    [RelayCommand]
    private async Task UndoAsync()
    {
        if (_undoStack.Count == 0)
        {
            NotificationService.ShowInfo("لا يوجد عمليات للتراجع عنها.");
            return;
        }

        var current = TakeSnapshot();
        _redoStack.Push(current);

        var previous = _undoStack.Pop();
        await RestoreSnapshotAsync(previous);
        NotificationService.ShowInfo("تم التراجع ✓");
    }

    [RelayCommand]
    private async Task RedoAsync()
    {
        if (_redoStack.Count == 0)
        {
            NotificationService.ShowInfo("لا يوجد عمليات لإعادتها.");
            return;
        }

        var current = TakeSnapshot();
        _undoStack.Push(current);

        var next = _redoStack.Pop();
        await RestoreSnapshotAsync(next);
        NotificationService.ShowInfo("تمت الإعادة ✓");
    }

    // ━━━━━━━━━━━━━━ Exhibitor Allocation System ━━━━━━━━━━━━━━

    [RelayCommand]
    private async Task LoadExhibitorsAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            var result = await _exhibitorService.GetByTenantAsync(Session.TenantId, 1, 100);
            if (result.IsSuccess && result.Data is not null)
            {
                UnassignedExhibitors.Clear();
                foreach (var ex in result.Data.Items)
                {
                    UnassignedExhibitors.Add(ex);
                }
            }
        }, "خطأ أثناء تحميل العارضين");
    }

    [RelayCommand]
    private async Task AllocateExhibitorAsync()
    {
        if (SelectedNode is null || SelectedExhibitor is null)
        {
            NotificationService.ShowWarning("الرجاء اختيار جناح واختيار عارض للتخصيص.");
            return;
        }

        SaveHistoryState();

        // Save original booth number
        string cleanNumber = SelectedNode.BoothNumber;
        int parenIndex = cleanNumber.IndexOf(" (");
        if (parenIndex > 0)
        {
            cleanNumber = cleanNumber.Substring(0, parenIndex);
        }

        SelectedNode.BoothNumber = $"{cleanNumber} ({SelectedExhibitor.CompanyName})";
        SelectedNode.Status = "Reserved";

        await SaveBoothPositionAsync(SelectedNode);
        NotificationService.ShowSuccess($"تم تخصيص الجناح لشركة {SelectedExhibitor.CompanyName} بنجاح ✓");
    }

    [RelayCommand]
    private async Task DeallocateExhibitorAsync()
    {
        if (SelectedNode is null) return;

        SaveHistoryState();

        string cleanNumber = SelectedNode.BoothNumber;
        int parenIndex = cleanNumber.IndexOf(" (");
        if (parenIndex > 0)
        {
            cleanNumber = cleanNumber.Substring(0, parenIndex);
        }

        SelectedNode.BoothNumber = cleanNumber;
        SelectedNode.Status = "Available";

        await SaveBoothPositionAsync(SelectedNode);
        NotificationService.ShowSuccess("تم إلغاء تخصيص الجناح بنجاح ✓");
    }

    // ━━━━━━━━━━━━━━ Image Export Command ━━━━━━━━━━━━━━

    [RelayCommand]
    private void ExportAsImage(FrameworkElement element)
    {
        if (element == null)
        {
            NotificationService.ShowError("لم يتم العثور على لوحة المخطط للتصدير.");
            return;
        }

        var saveFileDialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PNG Image|*.png",
            Title = "حفظ مخطط الصالة كصورة",
            FileName = $"{HallName}_FloorPlan.png"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                var prevSelected = SelectedNode;
                SelectedNode = null;

                double width = element.ActualWidth;
                double height = element.ActualHeight;
                if (width <= 0) width = 1024;
                if (height <= 0) height = 768;

                var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)width, (int)height, 96, 96, 
                    System.Windows.Media.PixelFormats.Pbgra32);
                
                rtb.Render(element);

                var pngEncoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                pngEncoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));

                using (var fs = System.IO.File.OpenWrite(saveFileDialog.FileName))
                {
                    pngEncoder.Save(fs);
                }

                SelectedNode = prevSelected;
                NotificationService.ShowSuccess("تم تصدير مخطط الصالة كصورة بنجاح ✓");
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"خطأ أثناء التصدير: {ex.Message}");
            }
        }
    }

    public bool CheckForCollisionsAndReturnTrueIfAny()
    {
        bool hasAny = false;
        for (int i = 0; i < BoothNodes.Count; i++)
        {
            var b1 = BoothNodes[i];
            for (int j = i + 1; j < BoothNodes.Count; j++)
            {
                var b2 = BoothNodes[j];
                if (b1.Location.X < b2.Location.X + b2.Width &&
                    b1.Location.X + b1.Width > b2.Location.X &&
                    b1.Location.Y < b2.Location.Y + b2.Height &&
                    b1.Location.Y + b1.Height > b2.Location.Y)
                {
                    hasAny = true;
                    break;
                }
            }
            if (hasAny) break;
        }
        return hasAny;
    }
}
