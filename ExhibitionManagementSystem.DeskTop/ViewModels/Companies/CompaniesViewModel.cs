using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.DeskTop.Helpers;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using ExhibitionManagementSystem.Models.DTOs.Exhibitor;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Companies;

public partial class CompaniesViewModel : ViewModelBase
{
    private readonly IExhibitorService _exhibitorService;

    // ━━━━━━━━━━━━━━ Collections ━━━━━━━━━━━━━━
    public ObservableCollection<ExhibitorSummaryDto> Companies { get; } = [];

    // ━━━━━━━━━━━━━━ Properties ━━━━━━━━━━━━━━
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private int _totalCount;
    private const int PageSize = 10;

    // ━━━━━━━━━━━━━━ Constructor ━━━━━━━━━━━━━━
    public CompaniesViewModel(
        IExhibitorService exhibitorService,
        INavigationService navigationService,
        INotificationService notificationService,
        SessionService session) : base(navigationService, notificationService, session)
    {
        _exhibitorService = exhibitorService;
        Title = "إدارة الشركات العارضة";
    }

    // ━━━━━━━━━━━━━━ Methods ━━━━━━━━━━━━━━
    public override async Task OnNavigatedToAsync()
    {
        await LoadCompaniesAsync();
    }

    [RelayCommand]
    private async Task LoadCompaniesAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                await SearchAsync();
                return;
            }

            var result = await _exhibitorService.GetByTenantAsync(Session.TenantId, CurrentPage, PageSize);
            if (result.IsSuccess && result.Data is not null)
            {
                Companies.Clear();
                foreach (var ex in result.Data.Items)
                {
                    Companies.Add(ex);
                }
                TotalPages = result.Data.TotalPages;
                TotalCount = result.Data.TotalCount;
            }
        }, "خطأ في تحميل الشركات العارضة");
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            CurrentPage = 1;
            await LoadCompaniesAsync();
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            var result = await _exhibitorService.SearchAsync(Session.TenantId, SearchQuery);
            if (result.IsSuccess && result.Data is not null)
            {
                Companies.Clear();
                foreach (var ex in result.Data)
                {
                    Companies.Add(ex);
                }
                TotalPages = 1;
                TotalCount = result.Data.Count;
                CurrentPage = 1;
            }
        }, "خطأ أثناء البحث عن شركات");
    }

    [RelayCommand]
    private async Task DeleteCompanyAsync(int exhibitorId)
    {
        var confirmResult = System.Windows.MessageBox.Show(
            "هل أنت متأكد من رغبتك في حذف هذه الشركة العارضة؟ سيؤدي ذلك لإلغاء حجوزاتها.",
            "تأكيد الحذف",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (confirmResult != System.Windows.MessageBoxResult.Yes) return;

        await ExecuteSafeAsync(async () =>
        {
            var result = await _exhibitorService.DeleteAsync(Session.TenantId, exhibitorId);
            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess("تم حذف الشركة بنجاح ✓");
                await LoadCompaniesAsync();
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل حذف الشركة");
            }
        }, "خطأ أثناء حذف الشركة");
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages && string.IsNullOrWhiteSpace(SearchQuery))
        {
            CurrentPage++;
            await LoadCompaniesAsync();
        }
    }

    [RelayCommand]
    private async Task PrevPageAsync()
    {
        if (CurrentPage > 1 && string.IsNullOrWhiteSpace(SearchQuery))
        {
            CurrentPage--;
            await LoadCompaniesAsync();
        }
    }
}
