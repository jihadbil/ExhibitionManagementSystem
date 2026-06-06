using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.Desktop.Services.Navigation;
using ExhibitionManagementSystem.Desktop.Services.Notifications;
using ExhibitionManagementSystem.Desktop.ViewModels.Base;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.Desktop.Services.Auth;
using ExhibitionManagementSystem.Desktop.Services.Dialog;
using Microsoft.Extensions.DependencyInjection;

namespace ExhibitionManagementSystem.Desktop.ViewModels.Exhibitions
{
    public partial class ExhibitionsViewModel : BaseViewModel
    {
        private readonly IExhibitionService _exhibitionService;
        private readonly ISessionService _sessionService;
        private readonly IVenueService _venueService;
        private readonly IDialogService _dialogService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<ExhibitionDto> _exhibitions = new();

        [ObservableProperty]
        private ObservableCollection<ExhibitionDto> _filteredExhibitions = new();

        public ExhibitionsViewModel(
            INavigationService navigationService,
            INotificationService notificationService,
            IExhibitionService exhibitionService,
            ISessionService sessionService,
            IVenueService venueService,
            IDialogService dialogService,
            IServiceProvider serviceProvider)
            : base(navigationService, notificationService)
        {
            _exhibitionService = exhibitionService;
            _sessionService = sessionService;
            _venueService = venueService;
            _dialogService = dialogService;
            _serviceProvider = serviceProvider;
            Title = "المعارض";
        }

        public override async Task InitializeAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                var tenantId = _sessionService.TenantId;
                var listResult = await _exhibitionService.GetByTenantAsync(tenantId, CurrentPage, PageSize);
                if (listResult.IsSuccess && listResult.Data != null)
                {
                    TotalCount = listResult.Data.TotalCount;
                    Exhibitions.Clear();
                    foreach (var summary in listResult.Data.Items)
                    {
                        var detailResult = await _exhibitionService.GetByIdAsync(tenantId, summary.ExhibitionID);
                        if (detailResult.IsSuccess && detailResult.Data != null)
                        {
                            Exhibitions.Add(detailResult.Data);
                        }
                        else
                        {
                            // Fallback
                            Exhibitions.Add(new ExhibitionDto
                            {
                                ExhibitionID = summary.ExhibitionID,
                                Name = summary.Name,
                                Type = summary.Type,
                                StartDate = summary.StartDate,
                                EndDate = summary.EndDate,
                                Status = summary.Status,
                                VenueName = summary.VenueName
                            });
                        }
                    }
                }
                FilterExhibitions();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"فشل تحميل المعارض: {ex.Message}";
                NotificationService.ShowError(ErrorMessage);
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterExhibitions();
        }

        private void FilterExhibitions()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredExhibitions = new ObservableCollection<ExhibitionDto>(Exhibitions);
            }
            else
            {
                var query = SearchText.Trim();
                var filtered = Exhibitions.Where(e =>
                    e.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    (e.VenueName != null && e.VenueName.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                    (e.Type != null && e.Type.Contains(query, StringComparison.OrdinalIgnoreCase))
                );
                FilteredExhibitions = new ObservableCollection<ExhibitionDto>(filtered);
            }
        }

        [RelayCommand]
        private async Task AddExhibitionAsync()
        {
            var tenantId = _sessionService.TenantId;
            var dialogVm = _serviceProvider.GetRequiredService<AddEditExhibitionDialogViewModel>();
            
            await dialogVm.LoadVenuesAsync(tenantId);

            var result = _dialogService.ShowDialog(dialogVm);
            if (result == true)
            {
                var createDto = new ExhibitionCreateDto
                {
                    TenantID = tenantId,
                    VenueID = dialogVm.SelectedVenue?.VenueID ?? 0,
                    Name = dialogVm.Name,
                    Type = dialogVm.Type,
                    Edition = dialogVm.Edition,
                    StartDate = dialogVm.StartDate,
                    EndDate = dialogVm.EndDate,
                    Description = dialogVm.Description,
                    ExpectedVisitors = dialogVm.ExpectedVisitors,
                    EntryFee = dialogVm.EntryFee,
                    EntryCurrency = dialogVm.EntryCurrency
                };

                var createResult = await ExecuteServiceAsync(() => _exhibitionService.CreateAsync(tenantId, createDto), "فشل إضافة المعرض");
                if (createResult != null)
                {
                    NotificationService.ShowSuccess("تم إضافة المعرض بنجاح!");
                    await InitializeAsync();
                }
            }
        }

        [RelayCommand]
        private async Task EditExhibitionAsync(object? item)
        {
            if (item is not ExhibitionDto exhibition) return;

            var tenantId = _sessionService.TenantId;
            
            // Load full detail
            var detailResult = await _exhibitionService.GetByIdAsync(tenantId, exhibition.ExhibitionID);
            var detail = detailResult.IsSuccess && detailResult.Data != null ? detailResult.Data : exhibition;

            var dialogVm = _serviceProvider.GetRequiredService<AddEditExhibitionDialogViewModel>();
            await dialogVm.LoadVenuesAsync(tenantId, detail.VenueID);
            dialogVm.LoadExistingData(detail);

            var result = _dialogService.ShowDialog(dialogVm);
            if (result == true)
            {
                var updateDto = new ExhibitionUpdateDto
                {
                    Name = dialogVm.Name,
                    Type = dialogVm.Type,
                    Edition = dialogVm.Edition,
                    StartDate = dialogVm.StartDate,
                    EndDate = dialogVm.EndDate,
                    Status = detail.Status,
                    Description = dialogVm.Description,
                    ExpectedVisitors = dialogVm.ExpectedVisitors,
                    EntryFee = dialogVm.EntryFee,
                    EntryCurrency = dialogVm.EntryCurrency
                };

                var updateResult = await ExecuteServiceAsync(() => _exhibitionService.UpdateAsync(tenantId, exhibition.ExhibitionID, updateDto), "فشل تعديل المعرض");
                if (updateResult != null)
                {
                    NotificationService.ShowSuccess("تم تعديل المعرض بنجاح!");
                    await InitializeAsync();
                }
            }
        }

        [RelayCommand]
        private async Task DeleteExhibitionAsync(object? item)
        {
            if (item is not ExhibitionDto exhibition) return;

            var tenantId = _sessionService.TenantId;
            var success = await ExecuteServiceAsync(() => _exhibitionService.DeleteAsync(tenantId, exhibition.ExhibitionID), "فشل حذف المعرض");
            if (success)
            {
                Exhibitions.Remove(exhibition);
                FilterExhibitions();
                NotificationService.ShowSuccess("تم حذف المعرض بنجاح!");
            }
        }
    }
}
