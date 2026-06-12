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
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.Models.DTOs.Visitor;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Tickets;

public partial class TicketsViewModel : ViewModelBase
{
    private readonly ITicketService _ticketService;
    private readonly IVisitorService _visitorService;
    private readonly IExhibitionService _exhibitionService;

    // ━━━━━━━━━━━━━━ Collections ━━━━━━━━━━━━━━
    public ObservableCollection<VisitorDto> Visitors { get; } = [];
    public ObservableCollection<TicketDto> Tickets { get; } = [];
    public ObservableCollection<ExhibitionSummaryDto> Exhibitions { get; } = [];

    // ━━━━━━━━━━━━━━ Selection / Search Properties ━━━━━━━━━━━━━━
    [ObservableProperty] private string _visitorSearch = string.Empty;
    [ObservableProperty] private int _visitorsCurrentPage = 1;
    [ObservableProperty] private int _visitorsTotalPages;
    [ObservableProperty] private int _visitorsTotalCount;

    [ObservableProperty] private int _selectedExhibitionId;

    // ━━━━━━━━━━━━━━ Form Fields: Register Visitor ━━━━━━━━━━━━━━
    [ObservableProperty] private string _newVisitorFullName = string.Empty;
    [ObservableProperty] private string _newVisitorPhone = string.Empty;
    [ObservableProperty] private string _newVisitorEmail = string.Empty;
    [ObservableProperty] private string _newVisitorNationality = "سعودي";
    [ObservableProperty] private string _newVisitorType = "Regular"; // Regular | VIP | Student | Delegate

    public ObservableCollection<string> VisitorTypes { get; } = new()
    {
        "Regular", "VIP", "Student", "Delegate"
    };

    // ━━━━━━━━━━━━━━ Form Fields: Issue Ticket ━━━━━━━━━━━━━━
    [ObservableProperty] private int _selectedVisitorId;
    [ObservableProperty] private string _selectedTicketType = "Regular"; // Regular | VIP | Student | EarlyBird
    [ObservableProperty] private decimal _newTicketPrice = 0;
    [ObservableProperty] private string _newTicketCurrencyCode = "LYD";
    [ObservableProperty] private DateTime _newTicketValidDate = DateTime.Today.AddDays(7);

    public ObservableCollection<string> TicketTypes { get; } = new()
    {
        "Regular", "VIP", "Student", "EarlyBird"
    };

    // ━━━━━━━━━━━━━━ Scanning ━━━━━━━━━━━━━━
    [ObservableProperty] private string _scannedQrCode = string.Empty;

    // ━━━━━━━━━━━━━━ Constructor ━━━━━━━━━━━━━━
    public TicketsViewModel(
        ITicketService ticketService,
        IVisitorService visitorService,
        IExhibitionService exhibitionService,
        INavigationService navigationService,
        INotificationService notificationService,
        SessionService session) : base(navigationService, notificationService, session)
    {
        _ticketService = ticketService;
        _visitorService = visitorService;
        _exhibitionService = exhibitionService;
        Title = "التذاكر والزوار";
    }

    // ━━━━━━━━━━━━━━ Methods ━━━━━━━━━━━━━━
    public override async Task OnNavigatedToAsync()
    {
        await LoadVisitorsAsync();
        await LoadExhibitionsAsync();
    }

    // ━━ Visitors Tab ━━

    [RelayCommand]
    private async Task LoadVisitorsAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            if (!string.IsNullOrWhiteSpace(VisitorSearch))
            {
                await SearchVisitorsAsync();
                return;
            }

            var result = await _visitorService.GetByTenantAsync(Session.TenantId, VisitorsCurrentPage, 10);
            if (result.IsSuccess && result.Data is not null)
            {
                Visitors.Clear();
                foreach (var v in result.Data.Items)
                {
                    Visitors.Add(v);
                }
                VisitorsTotalPages = result.Data.TotalPages;
                VisitorsTotalCount = result.Data.TotalCount;
            }
        }, "خطأ في تحميل قائمة الزوار");
    }

    [RelayCommand]
    private async Task SearchVisitorsAsync()
    {
        if (string.IsNullOrWhiteSpace(VisitorSearch))
        {
            VisitorsCurrentPage = 1;
            await LoadVisitorsAsync();
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            var result = await _visitorService.SearchAsync(Session.TenantId, VisitorSearch);
            if (result.IsSuccess && result.Data is not null)
            {
                Visitors.Clear();
                foreach (var v in result.Data)
                {
                    Visitors.Add(v);
                }
                VisitorsTotalPages = 1;
                VisitorsTotalCount = result.Data.Count;
                VisitorsCurrentPage = 1;
            }
        }, "خطأ أثناء البحث عن الزوار");
    }

    [RelayCommand]
    private async Task RegisterVisitorAsync()
    {
        if (string.IsNullOrWhiteSpace(NewVisitorFullName))
        {
            NotificationService.ShowError("الرجاء إدخال اسم الزائر");
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            var dto = new VisitorCreateDto
            {
                TenantID = Session.TenantId,
                FullName = NewVisitorFullName,
                Phone = NewVisitorPhone,
                Email = NewVisitorEmail,
                Nationality = NewVisitorNationality,
                VisitorType = NewVisitorType
            };

            var result = await _visitorService.RegisterAsync(Session.TenantId, dto);
            if (result.IsSuccess && result.Data is not null)
            {
                NotificationService.ShowSuccess($"تم تسجيل الزائر {result.Data.FullName} بنجاح ✓");
                NewVisitorFullName = string.Empty;
                NewVisitorPhone = string.Empty;
                NewVisitorEmail = string.Empty;
                await LoadVisitorsAsync();
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل تسجيل الزائر");
            }
        }, "خطأ أثناء تسجيل الزائر");
    }

    [RelayCommand]
    private async Task VisitorsNextPageAsync()
    {
        if (VisitorsCurrentPage < VisitorsTotalPages && string.IsNullOrWhiteSpace(VisitorSearch))
        {
            VisitorsCurrentPage++;
            await LoadVisitorsAsync();
        }
    }

    [RelayCommand]
    private async Task VisitorsPrevPageAsync()
    {
        if (VisitorsCurrentPage > 1 && string.IsNullOrWhiteSpace(VisitorSearch))
        {
            VisitorsCurrentPage--;
            await LoadVisitorsAsync();
        }
    }

    // ━━ Tickets Tab ━━

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
        if (value > 0)
        {
            await LoadTicketsAsync();
        }
        else
        {
            Tickets.Clear();
        }
    }

    [RelayCommand]
    private async Task LoadTicketsAsync()
    {
        if (SelectedExhibitionId == 0) return;

        await ExecuteSafeAsync(async () =>
        {
            var result = await _ticketService.GetByExhibitionAsync(Session.TenantId, SelectedExhibitionId);
            if (result.IsSuccess && result.Data is not null)
            {
                Tickets.Clear();
                foreach (var t in result.Data)
                {
                    Tickets.Add(t);
                }
            }
        }, "خطأ في تحميل قائمة التذاكر");
    }

    [RelayCommand]
    private async Task IssueTicketAsync()
    {
        if (SelectedExhibitionId == 0)
        {
            NotificationService.ShowError("الرجاء اختيار المعرض لإصدار التذكرة");
            return;
        }

        if (SelectedVisitorId == 0)
        {
            NotificationService.ShowError("الرجاء اختيار زائر من القائمة أو تسجيله أولاً");
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            var dto = new TicketCreateDto
            {
                VisitorID = SelectedVisitorId,
                ExhibitionID = SelectedExhibitionId,
                TicketType = SelectedTicketType,
                Price = NewTicketPrice,
                CurrencyCode = NewTicketCurrencyCode,
                ValidDate = NewTicketValidDate
            };

            var result = await _ticketService.IssueTicketAsync(Session.TenantId, dto);
            if (result.IsSuccess && result.Data is not null)
            {
                NotificationService.ShowSuccess($"تم إصدار التذكرة بنجاح. رمز الـ QR: {result.Data.QRCode} ✓");
                await LoadTicketsAsync();
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل إصدار التذكرة");
            }
        }, "خطأ أثناء إصدار التذكرة");
    }

    [RelayCommand]
    private async Task ScanTicketAsync()
    {
        if (string.IsNullOrWhiteSpace(ScannedQrCode))
        {
            NotificationService.ShowError("الرجاء إدخال رمز QR المراد مسحه");
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            var result = await _ticketService.ScanTicketAsync(
                Session.TenantId, ScannedQrCode, "Entry", null, Session.UserId);

            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess($"التذكرة صالحة ✓. نوع الدخول: Entry. رمز QR: {ScannedQrCode}");
                ScannedQrCode = string.Empty;
                if (SelectedExhibitionId > 0)
                {
                    await LoadTicketsAsync();
                }
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "رمز QR غير صالح أو تذكرة منتهية/ملغاة");
            }
        }, "خطأ أثناء مسح التذكرة");
    }

    [RelayCommand]
    private async Task CancelTicketAsync(int ticketId)
    {
        var confirmResult = System.Windows.MessageBox.Show(
            "هل أنت متأكد من رغبتك في إلغاء هذه التذكرة؟ سيمنع ذلك الزائر من الدخول.",
            "تأكيد إلغاء التذكرة",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (confirmResult != System.Windows.MessageBoxResult.Yes) return;

        await ExecuteSafeAsync(async () =>
        {
            var result = await _ticketService.CancelTicketAsync(Session.TenantId, ticketId);
            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess("تم إلغاء التذكرة بنجاح ✓");
                await LoadTicketsAsync();
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل إلغاء التذكرة");
            }
        }, "خطأ أثناء إلغاء التذكرة");
    }
}
