using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Models.DTOs.Dashboard;
using ExhibitionManagementSystem.DeskTop.Helpers;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;
using ExhibitionManagementSystem.DeskTop.Services.Session;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Dashboard
{
    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly IExhibitionService _exhibitionService;
        private readonly IDashboardService _dashboardService;
        private readonly DispatcherTimer _clockTimer;

        // ━━━━━━━━━━━━━━ Stat Cards ━━━━━━━━━━━━━━
        [ObservableProperty] private int _activeExhibitions;
        [ObservableProperty] private int _occupiedBooths;
        [ObservableProperty] private int _totalExhibitors;
        [ObservableProperty] private int _totalVisitors;
        [ObservableProperty] private bool _canViewFinancials;
        [ObservableProperty] private string _currencyCode = string.Empty;

        // ━━━━━━━━━━━━━━ Clock ━━━━━━━━━━━━━━
        [ObservableProperty] private string _currentTime = string.Empty;
        [ObservableProperty] private string _currentDate = string.Empty;
        [ObservableProperty] private string _greeting = string.Empty;

        // ━━━━━━━━━━━━━━ Charts ━━━━━━━━━━━━━━
        public ObservableCollection<RevenueChartPointDto> MonthlyStats { get; } = [];
        public ObservableCollection<ExhibitionTypeChartItem> TypeDistribution { get; } = [];

        // ━━━━━━━━━━━━━━ Table ━━━━━━━━━━━━━━
        public ObservableCollection<ExhibitionSummaryDto> UpcomingExhibitions { get; } = [];

        public DashboardViewModel(
            IExhibitionService exhibitionService,
            IDashboardService dashboardService,
            INavigationService navigationService,
            INotificationService notificationService,
            SessionService session) : base(navigationService, notificationService, session)
        {
            _exhibitionService = exhibitionService;
            _dashboardService = dashboardService;

            // ساعة تُحدَّث كل 30 ثانية
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _clockTimer.Tick += (_, _) => UpdateClock();
            _clockTimer.Start();
            UpdateClock();
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            await ExecuteSafeAsync(async () =>
            {
                var tenantId = Session.TenantId;

                var stats = await _dashboardService.GetStatsAsync();
                if (!stats.IsSuccess || stats.Data == null)
                    throw new InvalidOperationException(stats.ErrorMessage ?? "تعذر تحميل المؤشرات");
                ActiveExhibitions = stats.Data.ActiveExhibitions;
                TotalExhibitors = stats.Data.TotalExhibitors;
                TotalVisitors = stats.Data.TotalVisitors;
                OccupiedBooths = stats.Data.OccupiedBooths;
                CurrencyCode = stats.Data.CurrencyCode;
                CanViewFinancials = stats.Data.CanViewFinancials;

                // 4. Upcoming Exhibitions
                var upcomingResult = await _exhibitionService.GetUpcomingAsync(tenantId, count: 10);
                if (upcomingResult.IsSuccess && upcomingResult.Data is not null)
                {
                    UpcomingExhibitions.Clear();
                    foreach (var item in upcomingResult.Data)
                        UpcomingExhibitions.Add(item);
                }

                MonthlyStats.Clear();
                if (CanViewFinancials)
                {
                    var chart = await _dashboardService.GetRevenueChartAsync("6m");
                    if (!chart.IsSuccess || chart.Data == null)
                        throw new InvalidOperationException(chart.ErrorMessage ?? "تعذر تحميل الإيرادات");
                    foreach (var point in chart.Data) MonthlyStats.Add(point);
                }

                // 6. Type Distribution Chart Data
                var exhibitionsList = new List<ExhibitionSummaryDto>();
                for (var page = 1; ; page++)
                {
                    var exhResult = await _exhibitionService.GetByTenantAsync(tenantId, page, 200);
                    if (!exhResult.IsSuccess || exhResult.Data == null)
                        throw new InvalidOperationException(exhResult.ErrorMessage ?? "تعذر تحميل المعارض");
                    exhibitionsList.AddRange(exhResult.Data.Items);
                    if (page >= exhResult.Data.TotalPages) break;
                }

                var typeCounts = exhibitionsList
                    .GroupBy(e => e.Type)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToList();

                TypeDistribution.Clear();
                if (typeCounts.Count > 0)
                {
                    double total = typeCounts.Sum(tc => tc.Count);
                    foreach (var tc in typeCounts)
                    {
                        TypeDistribution.Add(new ExhibitionTypeChartItem
                        {
                            Type = ExhibitionTypeHelper.GetDisplayName(tc.Type.ToString()),
                            Count = tc.Count,
                            Percentage = (tc.Count / total) * 100
                        });
                    }
                }

                Greeting = $"مرحباً، {Session.FullName}";

            }, "خطأ في تحميل بيانات لوحة التحكم");
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;
            CurrentTime = now.ToString("HH:mm");
            CurrentDate = now.ToString("dddd، d MMMM yyyy", new System.Globalization.CultureInfo("ar-SA"));
        }

        public override async Task OnNavigatedToAsync()
        {
            await LoadDataAsync();
        }
    }
}
