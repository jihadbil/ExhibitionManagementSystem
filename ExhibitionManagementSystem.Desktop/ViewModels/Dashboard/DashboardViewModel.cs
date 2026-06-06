using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.Desktop.Services.Navigation;
using ExhibitionManagementSystem.Desktop.Services.Notifications;
using ExhibitionManagementSystem.Desktop.ViewModels.Base;
using ExhibitionManagementSystem.Desktop.ViewModels.Exhibitions;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.Desktop.Services.Auth;


namespace ExhibitionManagementSystem.Desktop.ViewModels.Dashboard
{
    public partial class DashboardViewModel : BaseViewModel
    {
        [ObservableProperty]
        private int _activeExhibitions;

        [ObservableProperty]
        private int _reservedBooths;

        [ObservableProperty]
        private int _companiesCount;

        [ObservableProperty]
        private int _registeredVisitors;

        [ObservableProperty]
        private string _activeTrend = "+3 هذا الشهر";

        // Chart properties
        public ISeries[] MonthlyVisitorsSeries { get; set; } = Array.Empty<ISeries>();
        public ISeries[] ExhibitionTypeSeries { get; set; } = Array.Empty<ISeries>();

        // Fallbacks for the current view bindings (before chart refactoring)
        public ObservableCollection<ISeries> BarSeries { get; set; } = new();
        public ObservableCollection<ISeries> DonutSeries { get; set; } = new();

        public Axis[] XAxes { get; set; } = Array.Empty<Axis>();
        public Axis[] YAxes { get; set; } = Array.Empty<Axis>();

        private readonly IExhibitionService _exhibitionService;
        private readonly IVisitorService _visitorService;
        private readonly IExhibitorService _exhibitorService;
        private readonly IReservationService _reservationService;
        private readonly ISessionService _sessionService;

        [ObservableProperty]
        private ObservableCollection<ExhibitionSummaryDto> _upcomingExhibitions = new();

        public DashboardViewModel(
            INavigationService navigationService,
            INotificationService notificationService,
            IExhibitionService exhibitionService,
            IVisitorService visitorService,
            IExhibitorService exhibitorService,
            IReservationService reservationService,
            ISessionService sessionService)
            : base(navigationService, notificationService)
        {
            _exhibitionService = exhibitionService;
            _visitorService = visitorService;
            _exhibitorService = exhibitorService;
            _reservationService = reservationService;
            _sessionService = sessionService;
            Title = "لوحة التحكم";
        }

        public override async Task InitializeAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                var tenantId = _sessionService.TenantId;

                // 1. Active Exhibitions Count & Reserved Booths Count
                var activeResult = await _exhibitionService.GetActiveAsync(tenantId);
                var activeExpos = activeResult.IsSuccess ? activeResult.Data : null;
                ActiveExhibitions = activeExpos?.Count ?? 0;

                var reservedBoothsCount = 0;
                if (activeExpos != null)
                {
                    foreach (var expo in activeExpos)
                    {
                        var resResult = await _reservationService.GetByExhibitionAsync(tenantId, expo.ExhibitionID, 1, 1);
                        if (resResult.IsSuccess && resResult.Data != null)
                        {
                            reservedBoothsCount += resResult.Data.TotalCount;
                        }
                    }
                }
                ReservedBooths = reservedBoothsCount;

                // 2. Companies (Exhibitors) Count
                var exhibitorsResult = await _exhibitorService.GetByTenantAsync(tenantId, 1, 1);
                CompaniesCount = exhibitorsResult.IsSuccess && exhibitorsResult.Data != null 
                    ? exhibitorsResult.Data.TotalCount 
                    : 0;

                // 3. Registered Visitors Count
                var visitorsResult = await _visitorService.GetByTenantAsync(tenantId, 1, 1);
                RegisteredVisitors = visitorsResult.IsSuccess && visitorsResult.Data != null 
                    ? visitorsResult.Data.TotalCount 
                    : 0;

                // 4. Upcoming Exhibitions
                var upcomingResult = await _exhibitionService.GetUpcomingAsync(tenantId, 5);
                UpcomingExhibitions.Clear();
                if (upcomingResult.IsSuccess && upcomingResult.Data != null)
                {
                    foreach (var expo in upcomingResult.Data)
                    {
                        UpcomingExhibitions.Add(expo);
                    }
                }

                // تهيئة المخطط الشريطي للزوار الشهريين (تقريبية)
                var columnSeries = new ColumnSeries<int>
                {
                    Name = "الزوار الشهريون",
                    Values = new[] { 1200, 1800, 2400, 2100, 2900, 3200, 2800, 3500, 3100, 2600, 3800, 4200 },
                    Fill = new LinearGradientPaint(
                        new SKColor(0x63, 0x66, 0xF1),
                        new SKColor(0x8B, 0x5C, 0xF6)),
                    Rx = 8,
                    Ry = 8
                };
                MonthlyVisitorsSeries = new ISeries[] { columnSeries };

                // تهيئة المخطط الدائري لتوزيع المعارض (تقريبية)
                ExhibitionTypeSeries = new ISeries[]
                {
                    new PieSeries<double> { Name = "تقنية", Values = new double[] { 42.0 }, InnerRadius = 45, Fill = new SolidColorPaint(SKColor.Parse("#6366F1")) },
                    new PieSeries<double> { Name = "طبية", Values = new double[] { 33.0 }, InnerRadius = 45, Fill = new SolidColorPaint(SKColor.Parse("#10B981")) },
                    new PieSeries<double> { Name = "تعليمية", Values = new double[] { 25.0 }, InnerRadius = 45, Fill = new SolidColorPaint(SKColor.Parse("#F59E0B")) }
                };

                // تغذية مجموعات التوافق القديم
                BarSeries.Clear();
                BarSeries.Add(columnSeries);

                DonutSeries.Clear();
                foreach (var series in ExhibitionTypeSeries)
                {
                    DonutSeries.Add(series);
                }

                XAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = new[] { "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر" },
                        LabelsPaint = new SolidColorPaint(SKColor.Parse("#64748B")),
                        SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#E2E8F0")) { StrokeThickness = 0 }
                    }
                };

                YAxes = new Axis[]
                {
                    new Axis
                    {
                        LabelsPaint = new SolidColorPaint(SKColor.Parse("#64748B")),
                        SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#E2E8F0")) { StrokeThickness = 1 }
                    }
                };
            }
            catch (Exception ex)
            {
                ErrorMessage = $"فشل تحميل بيانات لوحة التحكم: {ex.Message}";
                NotificationService.ShowError(ErrorMessage);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void NavigateToExhibitions()
        {
            NavigationService.NavigateTo<ExhibitionsViewModel>();
        }

        [RelayCommand]
        private async Task RefreshData()
        {
            await InitializeAsync();
            NotificationService.ShowSuccess("تم تحديث البيانات بنجاح!");
        }
    }
}

