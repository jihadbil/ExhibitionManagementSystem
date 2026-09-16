using System.Windows;
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
    [ObservableProperty] private FurnitureItem? _selectedFurniture;

    // ━━━━━━━━━━━━━━ Hall Boundaries ━━━━━━━━━━━━━━
    [ObservableProperty] private double _hallWidth = 1000;
    [ObservableProperty] private double _hallHeight = 800;
    [ObservableProperty] private double _hallWidthMeters = 50;
    [ObservableProperty] private double _hallHeightMeters = 40;
    [ObservableProperty] private string _hallName = string.Empty;

    // ━━━━━━━━━━━━━━ Zoom Factor ━━━━━━━━━━━━━━
    [ObservableProperty] private double _scaleFactor = 1.0;

    // ━━━━━━━━━━━━━━ Grid & Snap Controls ━━━━━━━━━━━━━━
    [ObservableProperty] private bool _isSnapToGridEnabled = true;
    [ObservableProperty] private bool _isGridVisible = true;
    [ObservableProperty] private double _gridSnapSize = 10.0;

    // ━━━━━━━━━━━━━━ Properties Panel Binding ━━━━━━━━━━━━━━
    [ObservableProperty] private string _selectedBoothNumber = string.Empty;
    [ObservableProperty] private double _selectedBoothWidth;
    [ObservableProperty] private double _selectedBoothHeight;
    [ObservableProperty] private string _selectedBoothStatus = string.Empty;
    [ObservableProperty] private double _selectedBoothRotation;
    [ObservableProperty] private string _selectedBoothShape = "Rectangle";

    public ObservableCollection<string> Statuses { get; } = new()
    {
        "Available", "Reserved", "PendingReview"
    };

    public ObservableCollection<string> Shapes { get; } = new()
    {
        "Rectangle", "LShape", "Triangle", "Trapezoid"
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
            var selectedHall = AvailableHalls.FirstOrDefault(h => h.HallID == value);
            if (selectedHall is not null)
            {
                HallWidthMeters = (double)(selectedHall.FloorPlanWidth ?? 50);
                HallHeightMeters = (double)(selectedHall.FloorPlanHeight ?? 40);
                HallWidth = HallWidthMeters * MeterToPixel;
                HallHeight = HallHeightMeters * MeterToPixel;
                HallName = selectedHall.HallName;
            }
            await LoadCanvasBoothsAsync();
        }
        else
        {
            CanvasBooths.Clear();
            HallWidth = 1000;
            HallHeight = 800;
            HallWidthMeters = 50;
            HallHeightMeters = 40;
            HallName = string.Empty;
        }
    }

    private const double MeterToPixel = 20.0;

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
                    var newItem = new BoothCanvasItem
                    {
                        BoothID = b.BoothID,
                        BoothNumber = b.BoothNumber,
                        Status = b.Status,
                        X = (double)((b.PosX ?? 10) * (decimal)MeterToPixel),
                        Y = (double)((b.PosY ?? 10) * (decimal)MeterToPixel),
                        Width = (double)((b.Width ?? 4) * (decimal)MeterToPixel),
                        Height = (double)((b.Height ?? 3) * (decimal)MeterToPixel),
                        RotationAngle = (double)(b.RotationAngle ?? 0),
                        ShapeType = b.ShapeType ?? "Rectangle",
                        IsSelected = false
                    };

                    if (!string.IsNullOrEmpty(b.ShapePolygonJSON))
                    {
                        try
                        {
                            var furnitureList = System.Text.Json.JsonSerializer.Deserialize<List<FurnitureSnapshot>>(b.ShapePolygonJSON);
                            if (furnitureList != null)
                            {
                                foreach (var f in furnitureList)
                                {
                                    newItem.Furniture.Add(new FurnitureItem
                                    {
                                        Type = f.Type,
                                        X = f.X,
                                        Y = f.Y,
                                        Width = f.Width,
                                        Height = f.Height,
                                        RotationAngle = f.RotationAngle
                                    });
                                }
                            }
                        }
                        catch { /* Ignore invalid JSON */ }
                    }

                    CanvasBooths.Add(newItem);
                }
                CheckForCollisions();
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
            SelectedBoothWidth = value.Width / MeterToPixel;
            SelectedBoothHeight = value.Height / MeterToPixel;
            SelectedBoothStatus = value.Status;
            SelectedBoothRotation = value.RotationAngle;
            SelectedBoothShape = value.ShapeType;
        }
        else
        {
            SelectedBoothNumber = string.Empty;
            SelectedBoothWidth = 0;
            SelectedBoothHeight = 0;
            SelectedBoothStatus = string.Empty;
            SelectedBoothRotation = 0;
            SelectedBoothShape = "Rectangle";
        }
    }

    partial void OnSelectedBoothRotationChanged(double value)
    {
        if (SelectedBooth is not null && SelectedBooth.RotationAngle != value)
        {
            SelectedBooth.RotationAngle = value;
            CheckForCollisions();
        }
    }

    partial void OnSelectedBoothShapeChanged(string value)
    {
        if (SelectedBooth is not null && SelectedBooth.ShapeType != value)
        {
            SelectedBooth.ShapeType = value;
            CheckForCollisions();
        }
    }

    [RelayCommand]
    private async Task SaveBoothAsync()
    {
        if (SelectedBooth is null) return;

        SaveHistoryState();

        // Apply temporary property panel values to the item
        SelectedBooth.BoothNumber = SelectedBoothNumber;
        SelectedBooth.Width = SelectedBoothWidth * MeterToPixel;
        SelectedBooth.Height = SelectedBoothHeight * MeterToPixel;
        SelectedBooth.Status = SelectedBoothStatus;
        SelectedBooth.RotationAngle = SelectedBoothRotation;
        SelectedBooth.ShapeType = SelectedBoothShape;

        await SaveBoothPositionAsync(SelectedBooth);
    }

    public async Task SaveBoothPositionAsync(BoothCanvasItem item)
    {
        await ExecuteSafeAsync(async () =>
        {
            // Serialize furniture
            var furnitureSnshots = item.Furniture.Select(f => new FurnitureSnapshot
            {
                Type = f.Type,
                X = f.X,
                Y = f.Y,
                Width = f.Width,
                Height = f.Height,
                RotationAngle = f.RotationAngle
            }).ToList();

            var jsonStr = System.Text.Json.JsonSerializer.Serialize(furnitureSnshots);

            var dto = new BoothUpdateDto
            {
                BoothNumber = item.BoothNumber,
                Status = item.Status,
                PosX = (decimal)(item.X / MeterToPixel),
                PosY = (decimal)(item.Y / MeterToPixel),
                Width = (decimal)(item.Width / MeterToPixel),
                Height = (decimal)(item.Height / MeterToPixel),
                RotationAngle = (decimal)item.RotationAngle,
                ShapeType = item.ShapeType,
                ShapePolygonJSON = jsonStr
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

        SaveHistoryState();

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

    // ━━━━━━━━━━━━━━ Add, Delete and Collision Helpers ━━━━━━━━━━━━━━

    public void CheckForCollisions()
    {
        // Clear collisions first
        foreach (var b in CanvasBooths)
        {
            b.HasCollision = false;
        }

        // Check all pairs
        for (int i = 0; i < CanvasBooths.Count; i++)
        {
            var b1 = CanvasBooths[i];
            for (int j = i + 1; j < CanvasBooths.Count; j++)
            {
                var b2 = CanvasBooths[j];
                // Rectangle intersection check
                if (b1.X < b2.X + b2.Width &&
                    b1.X + b1.Width > b2.X &&
                    b1.Y < b2.Y + b2.Height &&
                    b1.Y + b1.Height > b2.Y)
                {
                    b1.HasCollision = true;
                    b2.HasCollision = true;
                }
            }
        }
    }

    [RelayCommand]
    private async Task AddNewBoothAsync()
    {
        if (SelectedHallId == 0) return;

        // Auto-generate booth number
        int nextNum = 1;
        if (CanvasBooths.Count > 0)
        {
            var numbers = CanvasBooths.Select(b => {
                var digits = new string(b.BoothNumber.Where(char.IsDigit).ToArray());
                return int.TryParse(digits, out int val) ? val : 0;
            }).ToList();
            if (numbers.Count > 0)
            {
                nextNum = numbers.Max() + 1;
            }
        }
        string newBoothNumber = $"B-{nextNum:00}";

        SaveHistoryState();

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
                var newItem = new BoothCanvasItem
                {
                    BoothID = newBooth.BoothID,
                    BoothNumber = newBooth.BoothNumber,
                    Status = newBooth.Status,
                    X = (double)((newBooth.PosX ?? 5) * (decimal)MeterToPixel),
                    Y = (double)((newBooth.PosY ?? 5) * (decimal)MeterToPixel),
                    Width = (double)((newBooth.Width ?? 4) * (decimal)MeterToPixel),
                    Height = (double)((newBooth.Height ?? 3) * (decimal)MeterToPixel),
                    RotationAngle = (double)(newBooth.RotationAngle ?? 0),
                    ShapeType = newBooth.ShapeType ?? "Rectangle",
                    IsSelected = false
                };
                CanvasBooths.Add(newItem);
                SelectedBooth = newItem;
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
        if (SelectedBooth is null) return;

        var confirm = MessageBox.Show(
            $"هل أنت متأكد من رغبتك في حذف الجناح {SelectedBooth.BoothNumber}؟",
            "تأكيد الحذف",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        SaveHistoryState();

        await ExecuteSafeAsync(async () =>
        {
            var result = await _boothService.DeleteAsync(Session.TenantId, SelectedBooth.BoothID);
            if (result.IsSuccess)
            {
                var removedNumber = SelectedBooth.BoothNumber;
                CanvasBooths.Remove(SelectedBooth);
                SelectedBooth = null;
                CheckForCollisions();
                NotificationService.ShowSuccess($"تم حذف الجناح {removedNumber} بنجاح ✓");
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل حذف الجناح");
            }
        }, "خطأ أثناء حذف الجناح");
    }

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
        return CanvasBooths.Select(b => new BoothSnapshot
        {
            BoothID = b.BoothID,
            X = b.X,
            Y = b.Y,
            Width = b.Width,
            Height = b.Height,
            Status = b.Status,
            BoothNumber = b.BoothNumber,
            RotationAngle = b.RotationAngle,
            ShapeType = b.ShapeType,
            FurnitureJSON = System.Text.Json.JsonSerializer.Serialize(b.Furniture.Select(f => new FurnitureSnapshot
            {
                Type = f.Type,
                X = f.X,
                Y = f.Y,
                Width = f.Width,
                Height = f.Height,
                RotationAngle = f.RotationAngle
            }).ToList())
        }).ToList();
    }

    private async Task RestoreSnapshotAsync(List<BoothSnapshot> snapshot)
    {
        foreach (var snap in snapshot)
        {
            var booth = CanvasBooths.FirstOrDefault(b => b.BoothID == snap.BoothID);
            if (booth is not null)
            {
                booth.X = snap.X;
                booth.Y = snap.Y;
                booth.Width = snap.Width;
                booth.Height = snap.Height;
                booth.Status = snap.Status;
                booth.BoothNumber = snap.BoothNumber;
                booth.RotationAngle = snap.RotationAngle;
                booth.ShapeType = snap.ShapeType;

                booth.Furniture.Clear();
                if (!string.IsNullOrEmpty(snap.FurnitureJSON))
                {
                    try
                    {
                        var list = System.Text.Json.JsonSerializer.Deserialize<List<FurnitureSnapshot>>(snap.FurnitureJSON);
                        if (list != null)
                        {
                            foreach (var f in list)
                            {
                                booth.Furniture.Add(new FurnitureItem
                                {
                                    Type = f.Type,
                                    X = f.X,
                                    Y = f.Y,
                                    Width = f.Width,
                                    Height = f.Height,
                                    RotationAngle = f.RotationAngle
                                });
                            }
                        }
                    }
                    catch { }
                }

                await SaveBoothPositionAsync(booth);
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

    [RelayCommand]
    private async Task UpdateHallDimensionsAsync()
    {
        if (SelectedHallId == 0) return;

        var selectedHall = AvailableHalls.FirstOrDefault(h => h.HallID == SelectedHallId);
        if (selectedHall is null) return;

        var dto = new HallUpdateDto
        {
            HallName = selectedHall.HallName,
            AreaSqM = (decimal?)(HallWidthMeters * HallHeightMeters), // approximate area
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
                // Update local model
                selectedHall.FloorPlanWidth = (decimal)HallWidthMeters;
                selectedHall.FloorPlanHeight = (decimal)HallHeightMeters;
                selectedHall.AreaSqM = dto.AreaSqM;

                // Recalculate pixel sizes
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

    // ━━━━━━━━━━━━━━ Furniture Management Commands ━━━━━━━━━━━━━━

    [RelayCommand]
    private void AddFurniture(string type)
    {
        if (SelectedBooth is null) return;

        SaveHistoryState();

        double w = 24;
        double h = 24;
        
        switch (type)
        {
            case "Chair": w = 20; h = 20; break;
            case "Table": w = 32; h = 32; break;
            case "Counter": w = 40; h = 24; break;
            case "Plant": w = 20; h = 20; break;
            case "Sofa": w = 48; h = 24; break;
            case "TV": w = 40; h = 8; break;
        }

        double targetX = (SelectedBooth.Width - w) / 2;
        double targetY = (SelectedBooth.Height - h) / 2;

        var newItem = new FurnitureItem
        {
            Type = type,
            X = Math.Max(5, targetX),
            Y = Math.Max(5, targetY),
            Width = w,
            Height = h,
            RotationAngle = 0
        };

        SelectedBooth.Furniture.Add(newItem);
        SelectedFurniture = newItem;

        _ = SaveBoothPositionAsync(SelectedBooth);
    }

    [RelayCommand]
    private void RemoveFurniture()
    {
        if (SelectedBooth is null || SelectedFurniture is null) return;

        SaveHistoryState();

        SelectedBooth.Furniture.Remove(SelectedFurniture);
        SelectedFurniture = null;

        _ = SaveBoothPositionAsync(SelectedBooth);
    }

    [RelayCommand]
    private void RotateSelectedFurniture(double angleChange)
    {
        if (SelectedFurniture is null || SelectedBooth is null) return;

        SaveHistoryState();

        double newAngle = (SelectedFurniture.RotationAngle + angleChange) % 360;
        if (newAngle < 0) newAngle += 360;
        SelectedFurniture.RotationAngle = newAngle;

        _ = SaveBoothPositionAsync(SelectedBooth);
    }
}

public class BoothSnapshot
{
    public int BoothID { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string Status { get; set; } = string.Empty;
    public string BoothNumber { get; set; } = string.Empty;
    public double RotationAngle { get; set; }
    public string ShapeType { get; set; } = "Rectangle";
    public string FurnitureJSON { get; set; } = string.Empty;
}

public class FurnitureSnapshot
{
    public string Type { get; set; } = "Chair";
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double RotationAngle { get; set; }
}
