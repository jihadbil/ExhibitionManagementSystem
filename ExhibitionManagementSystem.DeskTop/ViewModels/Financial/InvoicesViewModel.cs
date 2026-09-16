using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.DeskTop.Helpers;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Financial;

public partial class InvoicesViewModel : ViewModelBase
{
    private readonly IFinancialService _financialService;
    private readonly IReservationService _reservationService;

    // ━━━━━━━━━━━━━━ Collections ━━━━━━━━━━━━━━

    public ObservableCollection<InvoiceDto> Invoices { get; } = [];
    public ObservableCollection<string> StatusFilters { get; } = new() { "All", "Pending", "Paid", "Overdue", "Cancelled" };

    // ━━━━━━━━━━━━━━ Properties ━━━━━━━━━━━━━━

    [ObservableProperty]
    private string _statusFilter = "All";

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _paidCount;

    [ObservableProperty]
    private int _pendingCount;

    [ObservableProperty]
    private int _overdueCount;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages;

    private const int PageSize = 15;

    // ━━━━━━━━━━━━━━ Constructor ━━━━━━━━━━━━━━

    public InvoicesViewModel(
        IFinancialService financialService,
        IReservationService reservationService,
        INavigationService navigationService,
        INotificationService notificationService,
        SessionService session) : base(navigationService, notificationService, session)
    {
        _financialService = financialService;
        _reservationService = reservationService;
        Title = "إدارة الفواتير";
    }

    // ━━━━━━━━━━━━━━ Methods & Commands ━━━━━━━━━━━━━━

    [RelayCommand]
    private async Task LoadInvoicesAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            var result = await _financialService.GetInvoicesByTenantAsync(Session.TenantId, CurrentPage, PageSize);
            if (result.IsSuccess && result.Data is not null)
            {
                var rawData = result.Data.Items;

                // Apply status filter locally
                var filtered = StatusFilter == "All"
                    ? rawData
                    : rawData.Where(i => i.Status.Equals(StatusFilter, StringComparison.OrdinalIgnoreCase)).ToList();

                Invoices.Clear();
                foreach (var invoice in filtered)
                {
                    Invoices.Add(invoice);
                }

                TotalCount = result.Data.TotalCount;
                TotalPages = result.Data.TotalPages;

                // Calculate summary counts
                PaidCount = rawData.Count(i => i.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase));
                PendingCount = rawData.Count(i => i.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase));

                // Load overdue counts
                var overdueResult = await _financialService.GetOverdueInvoicesAsync(Session.TenantId);
                if (overdueResult.IsSuccess && overdueResult.Data is not null)
                {
                    OverdueCount = overdueResult.Data.Count;
                }
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل تحميل الفواتير");
            }
        }, "خطأ في تحميل الفواتير");
    }

    [RelayCommand]
    private async Task LoadOverdueAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            var result = await _financialService.GetOverdueInvoicesAsync(Session.TenantId);
            if (result.IsSuccess && result.Data is not null)
            {
                Invoices.Clear();
                foreach (var invoice in result.Data)
                {
                    Invoices.Add(invoice);
                }

                TotalCount = result.Data.Count;
                TotalPages = 1;
                CurrentPage = 1;
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل تحميل الفواتير المتأخرة");
            }
        }, "خطأ في تحميل الفواتير المتأخرة");
    }

    [RelayCommand]
    private async Task GenerateInvoiceAsync(int reservationId)
    {
        await ExecuteSafeAsync(async () =>
        {
            var result = await _financialService.GenerateInvoiceForReservationAsync(Session.TenantId, reservationId);
            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess("تم إنشاء الفاتورة بنجاح ✓");
                await LoadInvoicesAsync();
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل إنشاء الفاتورة");
            }
        }, "خطأ أثناء إنشاء الفاتورة");
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadInvoicesAsync();
        }
    }

    [RelayCommand]
    private async Task PrevPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadInvoicesAsync();
        }
    }

    // ━━━━━━━━━━━━━━ Property Change Handlers ━━━━━━━━━━━━━━

    async partial void OnStatusFilterChanged(string value)
    {
        CurrentPage = 1;
        await LoadInvoicesAsync();
    }

    // ━━━━━━━━━━━━━━ Lifecycle ━━━━━━━━━━━━━━

    public override async Task OnNavigatedToAsync()
    {
        await LoadInvoicesAsync();
    }
}
