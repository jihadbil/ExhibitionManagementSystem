using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.DeskTop.Helpers;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;
using ExhibitionManagementSystem.DeskTop.Services.Session;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Exhibitions;

public partial class ExhibitionsViewModel : ViewModelBase
{
    private readonly IExhibitionService _exhibitionService;

    // ━━━━━━━━━━━━━━ Properties ━━━━━━━━━━━━━━

    public ObservableCollection<ExhibitionSummaryDto> Exhibitions { get; } = [];

    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private int _totalCount;
    private const int PageSize = 9; // 3x3 grid

    // ━━━━━━━━━━━━━━ Constructor ━━━━━━━━━━━━━━

    public ExhibitionsViewModel(
        IExhibitionService exhibitionService,
        INavigationService navigationService,
        INotificationService notificationService,
        SessionService session) : base(navigationService, notificationService, session)
    {
        _exhibitionService = exhibitionService;
    }

    // ━━━━━━━━━━━━━━ Commands ━━━━━━━━━━━━━━

    [RelayCommand]
    private async Task LoadExhibitionsAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            var result = await _exhibitionService.GetByTenantAsync(
                Session.TenantId, CurrentPage, PageSize);

            if (result.IsSuccess && result.Data is not null)
            {
                Exhibitions.Clear();
                foreach (var item in result.Data.Items)
                {
                    // Filter locally if search query is provided
                    if (string.IsNullOrWhiteSpace(SearchQuery) || 
                        item.Name.Contains(SearchQuery, System.StringComparison.OrdinalIgnoreCase))
                    {
                        Exhibitions.Add(item);
                    }
                }

                TotalPages = result.Data.TotalPages;
                TotalCount = result.Data.TotalCount;
            }
        }, "خطأ في تحميل المعارض");
    }

    [RelayCommand]
    private async Task DeleteExhibitionAsync(int exhibitionId)
    {
        await ExecuteSafeAsync(async () =>
        {
            var result = await _exhibitionService.DeleteAsync(Session.TenantId, exhibitionId);
            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess("تم حذف المعرض بنجاح ✓");
                await LoadExhibitionsAsync();
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل الحذف");
            }
        }, "خطأ أثناء الحذف");
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadExhibitionsAsync();
        }
    }

    [RelayCommand]
    private async Task PrevPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadExhibitionsAsync();
        }
    }

    public override async Task OnNavigatedToAsync()
    {
        await LoadExhibitionsAsync();
    }
}
