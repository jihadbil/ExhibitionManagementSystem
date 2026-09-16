using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.DeskTop.Helpers;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;
using ExhibitionManagementSystem.DeskTop.Services.Printing;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using ExhibitionManagementSystem.Models.DTOs.Badge;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Badges;

public partial class BadgeDesignerViewModel : ViewModelBase
{
    private readonly IBadgeService _badgeService;
    private readonly IExhibitionService _exhibitionService;
    private readonly BadgePrintService _printService;

    // Collections
    public ObservableCollection<BadgeTemplateDto> Templates { get; } = [];
    public ObservableCollection<ExhibitionSummaryDto> Exhibitions { get; } = [];
    public ObservableCollection<ParticipantType> ParticipantTypes { get; } = new(Enum.GetValues<ParticipantType>());
    public ObservableCollection<BadgeOrientation> Orientations { get; } = new(Enum.GetValues<BadgeOrientation>());

    // Selected items
    [ObservableProperty] private int _selectedExhibitionId;
    [ObservableProperty] private BadgeTemplateDto? _selectedTemplate;

    // Live Editor Properties
    [ObservableProperty] private int _editingTemplateId;
    [ObservableProperty] private string _templateName = "قالب افتراضي جديد";
    [ObservableProperty] private ParticipantType _targetParticipantType = ParticipantType.Visitor;
    [ObservableProperty] private int _widthMm = 86;
    [ObservableProperty] private int _heightMm = 120;
    [ObservableProperty] private BadgeOrientation _orientation = BadgeOrientation.Vertical;
    [ObservableProperty] private string _headerBgColor = "#6366F1";
    [ObservableProperty] private string _headerTextColor = "#FFFFFF";
    [ObservableProperty] private string _accentColor = "#4F46E5";
    [ObservableProperty] private string _categoryBadgeText = "زائر / VISITOR";
    [ObservableProperty] private bool _showLogo = true;
    [ObservableProperty] private string? _logoUrl;
    [ObservableProperty] private bool _showQRCode = true;
    [ObservableProperty] private bool _showBarcode = false;
    [ObservableProperty] private bool _showCompanyName = true;
    [ObservableProperty] private bool _showJobTitle = true;
    [ObservableProperty] private bool _showHallName = true;
    [ObservableProperty] private bool _showDates = true;
    [ObservableProperty] private string? _customFooterText = "ExpoManager — دخول معتمد";
    [ObservableProperty] private bool _isDefault = true;

    // Live Preview Sample Attendee
    [ObservableProperty] private string _sampleAttendeeName = "د. طارق المنصوري";
    [ObservableProperty] private string _sampleCompanyName = "الشركة الليبية لتقنية المعلومات";
    [ObservableProperty] private string _sampleJobTitle = "المدير التنفيذي للابتكار";
    [ObservableProperty] private string _sampleExhibitionName = "معرض طرابلس الدولي 2026";
    [ObservableProperty] private string _sampleLocation = "القاعة الرئيسية | جناح A12";
    [ObservableProperty] private string _sampleDates = "15 - 20 أكتوبر 2026";
    [ObservableProperty] private string _sampleQRCode = "EXPO-2026-VIP-9941";

    public BadgeDesignerViewModel(
        IBadgeService badgeService,
        IExhibitionService exhibitionService,
        BadgePrintService printService,
        INavigationService navigationService,
        INotificationService notificationService,
        SessionService session)
        : base(navigationService, notificationService, session)
    {
        _badgeService = badgeService;
        _exhibitionService = exhibitionService;
        _printService = printService;
        Title = "مصمم ومخصص الشارات وبطاقات الدخول";
    }

    public override async Task OnNavigatedToAsync()
    {
        await LoadExhibitionsAsync();
        await LoadTemplatesAsync();
    }

    [RelayCommand]
    public async Task LoadExhibitionsAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            var result = await _exhibitionService.GetActiveAsync(Session.TenantId);
            if (result.IsSuccess && result.Data != null)
            {
                Exhibitions.Clear();
                foreach (var ex in result.Data)
                {
                    Exhibitions.Add(ex);
                }

                if (Exhibitions.Any() && SelectedExhibitionId == 0)
                {
                    SelectedExhibitionId = Exhibitions.First().ExhibitionID;
                    SampleExhibitionName = Exhibitions.First().Name;
                }
            }
        });
    }

    [RelayCommand]
    public async Task LoadTemplatesAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            var result = await _badgeService.GetTemplatesAsync(Session.TenantId, SelectedExhibitionId > 0 ? SelectedExhibitionId : null);
            if (result.IsSuccess && result.Data != null)
            {
                Templates.Clear();
                foreach (var item in result.Data)
                {
                    Templates.Add(item);
                }

                if (Templates.Any())
                {
                    SelectTemplate(Templates.First());
                }
            }
        });
    }

    partial void OnSelectedExhibitionIdChanged(int value)
    {
        var ex = Exhibitions.FirstOrDefault(e => e.ExhibitionID == value);
        if (ex != null)
        {
            SampleExhibitionName = ex.Name;
        }
        _ = LoadTemplatesAsync();
    }

    partial void OnTargetParticipantTypeChanged(ParticipantType value)
    {
        CategoryBadgeText = value switch
        {
            ParticipantType.VIP => "VIP / كبار الشخصيات",
            ParticipantType.Speaker => "متحدث / SPEAKER",
            ParticipantType.Press => "إعلام / PRESS",
            ParticipantType.Organizer => "منظم / ORGANIZER",
            ParticipantType.Sponsor => "راعي / SPONSOR",
            ParticipantType.Exhibitor => "عارض / EXHIBITOR",
            _ => "زائر / VISITOR"
        };

        HeaderBgColor = value switch
        {
            ParticipantType.VIP => "#D97706",
            ParticipantType.Speaker => "#8B5CF6",
            ParticipantType.Press => "#DC2626",
            ParticipantType.Organizer => "#2563EB",
            ParticipantType.Sponsor => "#F59E0B",
            ParticipantType.Exhibitor => "#059669",
            _ => "#6366F1"
        };

        AccentColor = value switch
        {
            ParticipantType.VIP => "#B45309",
            ParticipantType.Speaker => "#7C3AED",
            ParticipantType.Press => "#B91C1C",
            ParticipantType.Organizer => "#1D4ED8",
            ParticipantType.Sponsor => "#D97706",
            ParticipantType.Exhibitor => "#047857",
            _ => "#4F46E5"
        };
    }

    [RelayCommand]
    public void SelectTemplate(BadgeTemplateDto template)
    {
        SelectedTemplate = template;
        EditingTemplateId = template.TemplateID;
        TemplateName = template.TemplateName;
        TargetParticipantType = template.TargetParticipantType;
        WidthMm = template.WidthMm;
        HeightMm = template.HeightMm;
        Orientation = template.Orientation;
        HeaderBgColor = template.HeaderBgColor;
        HeaderTextColor = template.HeaderTextColor;
        AccentColor = template.AccentColor;
        CategoryBadgeText = string.IsNullOrWhiteSpace(template.CategoryBadgeText) ? template.TargetParticipantTypeName : template.CategoryBadgeText;
        ShowLogo = template.ShowLogo;
        LogoUrl = template.LogoUrl;
        ShowQRCode = template.ShowQRCode;
        ShowBarcode = template.ShowBarcode;
        ShowCompanyName = template.ShowCompanyName;
        ShowJobTitle = template.ShowJobTitle;
        ShowHallName = template.ShowHallName;
        ShowDates = template.ShowDates;
        CustomFooterText = template.CustomFooterText;
        IsDefault = template.IsDefault;
    }

    [RelayCommand]
    public void CreateNewTemplate()
    {
        SelectedTemplate = null;
        EditingTemplateId = 0;
        TemplateName = "قالب شارة مخصص جديد";
        TargetParticipantType = ParticipantType.Visitor;
        WidthMm = 86;
        HeightMm = 120;
        Orientation = BadgeOrientation.Vertical;
        HeaderBgColor = "#6366F1";
        HeaderTextColor = "#FFFFFF";
        AccentColor = "#4F46E5";
        CategoryBadgeText = "زائر / VISITOR";
        ShowLogo = true;
        ShowQRCode = true;
        ShowBarcode = false;
        ShowCompanyName = true;
        ShowJobTitle = true;
        ShowHallName = true;
        ShowDates = true;
        CustomFooterText = "ExpoManager — دخول معتمد";
        IsDefault = false;
    }

    [RelayCommand]
    public async Task SaveTemplateAsync()
    {
        if (string.IsNullOrWhiteSpace(TemplateName))
        {
            NotificationService.ShowWarning("يرجى إدخال اسم القالب", "تنبيه");
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            if (EditingTemplateId == 0)
            {
                var createDto = new BadgeTemplateCreateDto
                {
                    ExhibitionID = SelectedExhibitionId > 0 ? SelectedExhibitionId : null,
                    TemplateName = TemplateName,
                    TargetParticipantType = TargetParticipantType,
                    WidthMm = WidthMm,
                    HeightMm = HeightMm,
                    Orientation = Orientation,
                    HeaderBgColor = HeaderBgColor,
                    HeaderTextColor = HeaderTextColor,
                    AccentColor = AccentColor,
                    CategoryBadgeText = CategoryBadgeText,
                    ShowLogo = ShowLogo,
                    LogoUrl = LogoUrl,
                    ShowQRCode = ShowQRCode,
                    ShowBarcode = ShowBarcode,
                    ShowCompanyName = ShowCompanyName,
                    ShowJobTitle = ShowJobTitle,
                    ShowHallName = ShowHallName,
                    ShowDates = ShowDates,
                    CustomFooterText = CustomFooterText,
                    IsDefault = IsDefault
                };

                var result = await _badgeService.CreateTemplateAsync(Session.TenantId, createDto);
                if (result.IsSuccess && result.Data != null)
                {
                    NotificationService.ShowSuccess("تم إنشاء قالب الشارة بنجاح", "تم الحفظ");
                    await LoadTemplatesAsync();
                }
                else
                {
                    NotificationService.ShowError(result.ErrorMessage ?? "فشل حفظ القالب", "خطأ");
                }
            }
            else
            {
                var updateDto = new BadgeTemplateUpdateDto
                {
                    ExhibitionID = SelectedExhibitionId > 0 ? SelectedExhibitionId : null,
                    TemplateName = TemplateName,
                    TargetParticipantType = TargetParticipantType,
                    WidthMm = WidthMm,
                    HeightMm = HeightMm,
                    Orientation = Orientation,
                    HeaderBgColor = HeaderBgColor,
                    HeaderTextColor = HeaderTextColor,
                    AccentColor = AccentColor,
                    CategoryBadgeText = CategoryBadgeText,
                    ShowLogo = ShowLogo,
                    LogoUrl = LogoUrl,
                    ShowQRCode = ShowQRCode,
                    ShowBarcode = ShowBarcode,
                    ShowCompanyName = ShowCompanyName,
                    ShowJobTitle = ShowJobTitle,
                    ShowHallName = ShowHallName,
                    ShowDates = ShowDates,
                    CustomFooterText = CustomFooterText,
                    IsDefault = IsDefault
                };

                var result = await _badgeService.UpdateTemplateAsync(Session.TenantId, EditingTemplateId, updateDto);
                if (result.IsSuccess && result.Data != null)
                {
                    NotificationService.ShowSuccess("تم تحديث قالب الشارة بنجاح", "تم الحفظ");
                    await LoadTemplatesAsync();
                }
                else
                {
                    NotificationService.ShowError(result.ErrorMessage ?? "فشل تحديث القالب", "خطأ");
                }
            }
        });
    }

    [RelayCommand]
    public async Task DeleteTemplateAsync()
    {
        if (EditingTemplateId == 0) return;

        await ExecuteSafeAsync(async () =>
        {
            var result = await _badgeService.DeleteTemplateAsync(Session.TenantId, EditingTemplateId, Session.UserId);
            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess("تم حذف القالب بنجاح", "تم الحذف");
                CreateNewTemplate();
                await LoadTemplatesAsync();
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل حذف القالب", "خطأ");
            }
        });
    }

    [RelayCommand]
    public void TestPrintCurrentDesign()
    {
        var samplePayload = new BadgePrintPayloadDto
        {
            FullName = SampleAttendeeName,
            CompanyName = ShowCompanyName ? SampleCompanyName : null,
            JobTitle = ShowJobTitle ? SampleJobTitle : null,
            ParticipantType = TargetParticipantType,
            ParticipantTypeTitle = CategoryBadgeText,
            QRCode = SampleQRCode,
            ExhibitionName = SampleExhibitionName,
            VenueAndHallName = ShowHallName ? SampleLocation : null,
            DatesText = ShowDates ? SampleDates : null,
            HeaderBgColor = HeaderBgColor,
            HeaderTextColor = HeaderTextColor,
            AccentColor = AccentColor,
            LogoUrl = ShowLogo ? LogoUrl : null,
            FooterText = CustomFooterText,
            WidthMm = WidthMm,
            HeightMm = HeightMm,
            Orientation = Orientation,
            IssuedAt = DateTime.UtcNow
        };

        bool printed = _printService.PrintBadge(samplePayload, showDialog: true);
        if (printed)
        {
            NotificationService.ShowSuccess("تم إرسال بطاقة الاختبار إلى الطابعة بنجاح", "طباعة تجريبية");
        }
    }
}
