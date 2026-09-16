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
using ExhibitionManagementSystem.Models.DTOs.Pricing;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.DeskTop.Views.Booths;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Booths;

public partial class BoothsViewModel : ViewModelBase
{
    private readonly IBoothService _boothService;
    private readonly IHallService _hallService;
    private readonly IExhibitionService _exhibitionService;
    private readonly IPricingService _pricingService;

    private readonly List<BoothPriceRuleDto> _currentExhibitionRules = new();

    // ━━━━━━━━━━━━━━ Collections ━━━━━━━━━━━━━━
    public ObservableCollection<BoothDto> Booths { get; } = [];
    public ObservableCollection<HallDto> AvailableHalls { get; } = [];
    public ObservableCollection<ExhibitionSummaryDto> Exhibitions { get; } = [];
    public ObservableCollection<BoothPriceRuleDto> PricingRules { get; } = [];

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
    [ObservableProperty] private int? _newAssignedPriceRuleId = 0;

    // ━━━━━━━━━━━━━━ Form Fields for Editing Booth ━━━━━━━━━━━━━━
    [ObservableProperty] private int _editBoothId;
    [ObservableProperty] private string _editBoothNumber = string.Empty;
    [ObservableProperty] private decimal _editAreaSqM = 12;
    [ObservableProperty] private decimal? _editWidth;
    [ObservableProperty] private decimal? _editHeight;
    [ObservableProperty] private string _editShapeType = "Standard";
    [ObservableProperty] private string _editStatus = "Available";
    [ObservableProperty] private int? _editAssignedPriceRuleId = 0;
    public Action? EditCloseAction { get; set; }

    // ━━━━━━━━━━━━━━ Pricing Preview properties ━━━━━━━━━━━━━━
    [ObservableProperty] private string _pricingPreviewText = "الرجاء اختيار معرض أولاً لعرض الأسعار";
    [ObservableProperty] private bool _hasPricingRules;

    [ObservableProperty] private string _editPricingPreviewText = string.Empty;
    [ObservableProperty] private bool _editHasPricingRules;

    public ObservableCollection<string> ShapeTypes { get; } = new()
    {
        "Standard", "Corner", "Premium", "VIP", "Custom"
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
        IPricingService pricingService,
        INavigationService navigationService,
        INotificationService notificationService,
        SessionService session) : base(navigationService, notificationService, session)
    {
        _boothService = boothService;
        _hallService = hallService;
        _exhibitionService = exhibitionService;
        _pricingService = pricingService;
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
            await LoadPricingRulesAsync();
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
            AutoGenerateNewBoothNumber();
        }
        else
        {
            Booths.Clear();
            ResetStats();
            NewBoothNumber = string.Empty;
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

                AutoGenerateNewBoothNumber();
            }
        }, "خطأ في تحميل الأجنحة");
    }

    public Action? CloseAction { get; set; }

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

        if (!HasPricingRules)
        {
            NotificationService.ShowError("لا يمكن إنشاء الجناح لعدم وجود قواعد تسعير معرفة لهذا النوع في المعرض الحالي. يرجى إضافة قواعد التسعير أولاً.");
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
                PosY = 10,
                AssignedPriceRuleID = NewAssignedPriceRuleId == 0 ? null : NewAssignedPriceRuleId
            };

            var result = await _boothService.CreateAsync(Session.TenantId, dto);
            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess("تم إضافة الجناح بنجاح ✓");
                NewBoothNumber = string.Empty;
                NewOriginalAreaSqM = 12;
                NewWidth = 4;
                NewHeight = 3;
                NewAssignedPriceRuleId = 0;
                await LoadBoothsAsync();
                CloseAction?.Invoke();
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل إضافة الجناح");
            }
        }, "خطأ أثناء إضافة الجناح");
    }

    [RelayCommand]
    private async Task UpdateBoothAsync()
    {
        if (EditBoothId == 0) return;
        if (string.IsNullOrWhiteSpace(EditBoothNumber))
        {
            NotificationService.ShowError("الرجاء إدخال رقم الجناح");
            return;
        }

        if (!EditHasPricingRules)
        {
            NotificationService.ShowError("لا يمكن تحديث الجناح لعدم وجود قواعد تسعير معرفة لهذا النوع في المعرض الحالي. يرجى إضافة قواعد التسعير أولاً.");
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            var dto = new BoothUpdateDto
            {
                BoothNumber = EditBoothNumber,
                ShapeType = EditShapeType,
                Width = EditWidth,
                Height = EditHeight,
                Status = EditStatus,
                PosX = 10,
                PosY = 10,
                RotationAngle = 0,
                ShapePolygonJSON = string.Empty,
                AssignedPriceRuleID = EditAssignedPriceRuleId == 0 ? null : EditAssignedPriceRuleId
            };

            var result = await _boothService.UpdateAsync(Session.TenantId, EditBoothId, dto);
            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess("تم تحديث الجناح بنجاح ✓");
                EditCloseAction?.Invoke();
                await LoadBoothsAsync();
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل تحديث الجناح");
            }
        }, "خطأ أثناء تحديث الجناح");
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

    [RelayCommand]
    private void OpenNodifyDesigner()
    {
        NavigationService.NavigateTo<NodifyBoothDesignerPage>();
    }

    private void ResetStats()
    {
        TotalCount = 0;
        AvailableCount = 0;
        ReservedCount = 0;
        PendingCount = 0;
    }

    // ━━━━━━━━━━━━━━ Dynamic Handlers & Helpers ━━━━━━━━━━━━━━

    // Auto-calculate areas when Width or Height changes
    partial void OnNewWidthChanged(decimal? value) => RecalculateNewArea();
    partial void OnNewHeightChanged(decimal? value) => RecalculateNewArea();

    private void RecalculateNewArea()
    {
        if (NewWidth.HasValue && NewHeight.HasValue)
        {
            NewOriginalAreaSqM = NewWidth.Value * NewHeight.Value;
        }
    }

    partial void OnEditWidthChanged(decimal? value) => RecalculateEditArea();
    partial void OnEditHeightChanged(decimal? value) => RecalculateEditArea();

    private void RecalculateEditArea()
    {
        if (EditWidth.HasValue && EditHeight.HasValue)
        {
            EditAreaSqM = EditWidth.Value * EditHeight.Value;
        }
    }

    // Trigger pricing preview updates on area or type changes
    partial void OnNewOriginalAreaSqMChanged(decimal value) => UpdateNewPricingPreview();
    partial void OnNewShapeTypeChanged(string value) => UpdateNewPricingPreview();
    partial void OnNewAssignedPriceRuleIdChanged(int? value) => UpdateNewPricingPreview();

    partial void OnEditAreaSqMChanged(decimal value) => UpdateEditPricingPreview();
    partial void OnEditShapeTypeChanged(string value) => UpdateEditPricingPreview();
    partial void OnEditAssignedPriceRuleIdChanged(int? value) => UpdateEditPricingPreview();

    async partial void OnEditBoothIdChanged(int value)
    {
        if (value > 0)
        {
            await LoadPricingRulesAsync();
            UpdateEditPricingPreview();
        }
    }

    private async Task LoadPricingRulesAsync()
    {
        if (SelectedExhibitionId == 0)
        {
            _currentExhibitionRules.Clear();
            PricingRules.Clear();
            UpdateNewPricingPreview();
            UpdateEditPricingPreview();
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            var result = await _pricingService.GetBoothPriceRulesAsync(Session.TenantId, SelectedExhibitionId);
            if (result.IsSuccess && result.Data != null)
            {
                _currentExhibitionRules.Clear();
                PricingRules.Clear();

                PricingRules.Add(new BoothPriceRuleDto 
                { 
                    RuleID = 0, 
                    RuleName = "حساب تلقائي (حسب القواعد المعرفة)" 
                });

                foreach (var rule in result.Data)
                {
                    _currentExhibitionRules.Add(rule);
                    PricingRules.Add(rule);
                }
            }
            else
            {
                _currentExhibitionRules.Clear();
                PricingRules.Clear();
            }
            UpdateNewPricingPreview();
            UpdateEditPricingPreview();
        }, "خطأ في تحميل قواعد التسعير");
    }

    private void UpdateNewPricingPreview()
    {
        if (SelectedExhibitionId == 0)
        {
            PricingPreviewText = "الرجاء اختيار معرض أولاً لعرض الأسعار";
            HasPricingRules = false;
            return;
        }

        if (NewAssignedPriceRuleId.HasValue && NewAssignedPriceRuleId.Value > 0)
        {
            var rule = _currentExhibitionRules.FirstOrDefault(r => r.RuleID == NewAssignedPriceRuleId.Value);
            if (rule != null)
            {
                HasPricingRules = true;
                var pricePerM = rule.PricePerSqM;
                var totalVal = pricePerM * NewOriginalAreaSqM;
                PricingPreviewText = $"[قاعدة يدوية: {rule.RuleName ?? "غير مسمى"}]\nالسعر: {pricePerM:N2} {rule.CurrencyCode} للمتر²\nالإجمالي: {totalVal:N2} {rule.CurrencyCode}";
                return;
            }
        }

        if (!Enum.TryParse<BoothType>(NewShapeType, true, out var currentBoothType))
        {
            currentBoothType = BoothType.Standard;
        }

        var matchingRules = _currentExhibitionRules
            .Where(r => string.IsNullOrEmpty(r.BoothType) || 
                        r.BoothType.Equals(currentBoothType.ToString(), StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!matchingRules.Any())
        {
            PricingPreviewText = "⚠️ لا توجد قواعد تسعير معرفة لهذا النوع من الأجنحة في هذا المعرض!";
            HasPricingRules = false;
            return;
        }

        HasPricingRules = true;
        var previewLines = new List<string>();

        foreach (var category in Enum.GetValues<ExhibitorCategory>())
        {
            var rule = matchingRules
                .Where(r => string.IsNullOrEmpty(r.ExhibitorCategory) || 
                            r.ExhibitorCategory.Equals(category.ToString(), StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => !string.IsNullOrEmpty(r.BoothType))
                .ThenByDescending(r => !string.IsNullOrEmpty(r.ExhibitorCategory))
                .FirstOrDefault();

            if (rule != null)
            {
                var pricePerM = rule.PricePerSqM;
                var totalVal = pricePerM * NewOriginalAreaSqM;
                string catName = category switch
                {
                    ExhibitorCategory.Local => "محلي",
                    ExhibitorCategory.International => "دولي / أجنبي",
                    ExhibitorCategory.Government => "حكومي",
                    _ => category.ToString()
                };
                previewLines.Add($"{catName}: {pricePerM:N2} {rule.CurrencyCode} للمتر² (الإجمالي: {totalVal:N2} {rule.CurrencyCode})");
            }
        }

        if (previewLines.Any())
        {
            PricingPreviewText = string.Join("\n", previewLines);
        }
        else
        {
            PricingPreviewText = "⚠️ لا توجد أسعار مطابقة.";
            HasPricingRules = false;
        }
    }

    private void UpdateEditPricingPreview()
    {
        if (SelectedExhibitionId == 0)
        {
            EditPricingPreviewText = "الرجاء اختيار معرض أولاً لعرض الأسعار";
            EditHasPricingRules = false;
            return;
        }

        if (EditAssignedPriceRuleId.HasValue && EditAssignedPriceRuleId.Value > 0)
        {
            var rule = _currentExhibitionRules.FirstOrDefault(r => r.RuleID == EditAssignedPriceRuleId.Value);
            if (rule != null)
            {
                EditHasPricingRules = true;
                var pricePerM = rule.PricePerSqM;
                var totalVal = pricePerM * EditAreaSqM;
                EditPricingPreviewText = $"[قاعدة يدوية: {rule.RuleName ?? "غير مسمى"}]\nالسعر: {pricePerM:N2} {rule.CurrencyCode} للمتر²\nالإجمالي: {totalVal:N2} {rule.CurrencyCode}";
                return;
            }
        }

        if (!Enum.TryParse<BoothType>(EditShapeType, true, out var currentBoothType))
        {
            currentBoothType = BoothType.Standard;
        }

        var matchingRules = _currentExhibitionRules
            .Where(r => string.IsNullOrEmpty(r.BoothType) || 
                        r.BoothType.Equals(currentBoothType.ToString(), StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!matchingRules.Any())
        {
            EditPricingPreviewText = "⚠️ لا توجد قواعد تسعير معرفة لهذا النوع من الأجنحة في هذا المعرض!";
            EditHasPricingRules = false;
            return;
        }

        EditHasPricingRules = true;
        var previewLines = new List<string>();

        foreach (var category in Enum.GetValues<ExhibitorCategory>())
        {
            var rule = matchingRules
                .Where(r => string.IsNullOrEmpty(r.ExhibitorCategory) || 
                            r.ExhibitorCategory.Equals(category.ToString(), StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => !string.IsNullOrEmpty(r.BoothType))
                .ThenByDescending(r => !string.IsNullOrEmpty(r.ExhibitorCategory))
                .FirstOrDefault();

            if (rule != null)
            {
                var pricePerM = rule.PricePerSqM;
                var totalVal = pricePerM * EditAreaSqM;
                string catName = category switch
                {
                    ExhibitorCategory.Local => "محلي",
                    ExhibitorCategory.International => "دولي / أجنبي",
                    ExhibitorCategory.Government => "حكومي",
                    _ => category.ToString()
                };
                previewLines.Add($"{catName}: {pricePerM:N2} {rule.CurrencyCode} للمتر² (الإجمالي: {totalVal:N2} {rule.CurrencyCode})");
            }
        }

        if (previewLines.Any())
        {
            EditPricingPreviewText = string.Join("\n", previewLines);
        }
        else
        {
            EditPricingPreviewText = "⚠️ لا توجد أسعار مطابقة.";
            EditHasPricingRules = false;
        }
    }

    // Auto-generate sequential booth numbers
    private void AutoGenerateNewBoothNumber()
    {
        if (SelectedHallId == 0) return;
        var hall = AvailableHalls.FirstOrDefault(h => h.HallID == SelectedHallId);
        if (hall == null) return;

        // Get all booths in this hall
        var hallBooths = Booths;

        // Find the next sequence number
        int nextSeq = 1;
        if (hallBooths.Any())
        {
            var numbers = hallBooths.Select(b => b.BoothNumber).ToList();
            var maxNum = hallBooths
                .Select(b => {
                    var digits = new string(b.BoothNumber.Where(char.IsDigit).ToArray());
                    return int.TryParse(digits, out int val) ? val : 0;
                })
                .DefaultIfEmpty(0)
                .Max();
            nextSeq = maxNum + 1;
        }

        // Generate clean prefix based on HallName or just "B-"
        string prefix = "B-";
        if (!string.IsNullOrWhiteSpace(hall.HallName))
        {
            var initials = new string(hall.HallName.Split(' ').Select(s => s.FirstOrDefault()).ToArray()).ToUpper();
            if (!string.IsNullOrEmpty(initials))
            {
                prefix = $"{initials}-";
            }
        }

        NewBoothNumber = $"{prefix}{nextSeq:D2}";
    }
}
