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
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Badges;

public partial class CheckInKioskViewModel : ViewModelBase
{
    private readonly IBadgeService _badgeService;
    private readonly ITicketService _ticketService;
    private readonly IExhibitionService _exhibitionService;
    private readonly BadgePrintService _printService;

    // Collections
    public ObservableCollection<ExhibitionSummaryDto> Exhibitions { get; } = [];
    public ObservableCollection<BadgePrintPayloadDto> RecentCheckIns { get; } = [];

    // Kiosk State
    [ObservableProperty] private int _selectedExhibitionId;
    [ObservableProperty] private string _selectedExhibitionName = "اختر المعرض...";
    [ObservableProperty] private string _scanInput = string.Empty;
    [ObservableProperty] private bool _autoPrintOnScan = true;
    [ObservableProperty] private bool _silentPrint = false;
    [ObservableProperty] private int _todayCheckInCount = 0;
    [ObservableProperty] private string _gateName = "البوابة الرئيسية (Gate A)";

    // Current Scanned Attendee Flash
    [ObservableProperty] private BadgePrintPayloadDto? _currentBadge;
    [ObservableProperty] private bool _hasScannedBadge = false;
    [ObservableProperty] private string _statusMessage = "جاهز لمسح رمز QR أو إدخال رقم التذكرة...";
    [ObservableProperty] private bool _isSuccessStatus = true;

    public CheckInKioskViewModel(
        IBadgeService badgeService,
        ITicketService ticketService,
        IExhibitionService exhibitionService,
        BadgePrintService printService,
        INavigationService navigationService,
        INotificationService notificationService,
        SessionService session)
        : base(navigationService, notificationService, session)
    {
        _badgeService = badgeService;
        _ticketService = ticketService;
        _exhibitionService = exhibitionService;
        _printService = printService;
        Title = "كشك الاستقبال وطباعة الشارات الفورية";
    }

    public override async Task OnNavigatedToAsync()
    {
        await LoadExhibitionsAsync();
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
                    SelectedExhibitionName = Exhibitions.First().Name;
                }
            }
        });
    }

    partial void OnSelectedExhibitionIdChanged(int value)
    {
        var ex = Exhibitions.FirstOrDefault(e => e.ExhibitionID == value);
        if (ex != null)
        {
            SelectedExhibitionName = ex.Name;
        }
    }

    /// <summary>
    /// معالجة مسح الباركود أو الـ QR Code عند الضغط على Enter أو الإدخال من الماسح.
    /// </summary>
    [RelayCommand]
    public async Task ProcessScanAsync()
    {
        if (string.IsNullOrWhiteSpace(ScanInput)) return;

        var scannedCode = ScanInput.Trim();
        ScanInput = string.Empty;

        await ExecuteSafeAsync(async () =>
        {
            StatusMessage = "جاري التحقق وسحب بيانات المشارك...";
            IsSuccessStatus = true;

            var payloadResult = await _badgeService.GenerateBadgePayloadByQRCodeAsync(Session.TenantId, scannedCode);
            if (!payloadResult.IsSuccess || payloadResult.Data == null)
            {
                StatusMessage = payloadResult.ErrorMessage ?? "رمز غير صالح أو لم يتم العثور على المشارك";
                IsSuccessStatus = false;
                HasScannedBadge = false;
                NotificationService.ShowWarning(StatusMessage, "تعذر التعرف");
                return;
            }

            var badge = payloadResult.Data;
            CurrentBadge = badge;
            HasScannedBadge = true;
            TodayCheckInCount++;
            StatusMessage = $"تم تسجيل دخول: {badge.FullName} ({badge.ParticipantTypeTitle})";
            IsSuccessStatus = true;

            // Add to recent check-ins list
            RecentCheckIns.Insert(0, badge);
            if (RecentCheckIns.Count > 20)
            {
                RecentCheckIns.RemoveAt(RecentCheckIns.Count - 1);
            }

            // Register Gate Scan in Ticket System in background
            _ = _ticketService.ScanTicketAsync(Session.TenantId, badge.QRCode, "In", GateName, Session.UserId);

            // Auto-print if enabled
            if (AutoPrintOnScan)
            {
                _printService.PrintBadge(badge, showDialog: !SilentPrint);
            }
        });
    }

    [RelayCommand]
    public void PrintCurrentBadge()
    {
        if (CurrentBadge != null)
        {
            bool success = _printService.PrintBadge(CurrentBadge, showDialog: !SilentPrint);
            if (success)
            {
                NotificationService.ShowSuccess($"تمت طباعة شارة {CurrentBadge.FullName}", "نجاح الطباعة");
            }
        }
    }

    [RelayCommand]
    public void ClearCurrentBadge()
    {
        CurrentBadge = null;
        HasScannedBadge = false;
        StatusMessage = "جاهز لمسح رمز QR جديد...";
        IsSuccessStatus = true;
    }
}
