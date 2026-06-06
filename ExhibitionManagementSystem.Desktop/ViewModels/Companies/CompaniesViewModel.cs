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
using ExhibitionManagementSystem.Models.DTOs.Exhibitor;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.Desktop.Services.Auth;
using ExhibitionManagementSystem.Desktop.Services.Dialog;
using Microsoft.Extensions.DependencyInjection;

namespace ExhibitionManagementSystem.Desktop.ViewModels.Companies
{
    public partial class CompaniesViewModel : BaseViewModel
    {
        private readonly IExhibitorService _exhibitorService;
        private readonly ISessionService _sessionService;
        private readonly IDialogService _dialogService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<ExhibitorDto> _companies = new();

        [ObservableProperty]
        private ObservableCollection<ExhibitorDto> _filteredCompanies = new();

        public CompaniesViewModel(
            INavigationService navigationService,
            INotificationService notificationService,
            IExhibitorService exhibitorService,
            ISessionService sessionService,
            IDialogService dialogService,
            IServiceProvider serviceProvider)
            : base(navigationService, notificationService)
        {
            _exhibitorService = exhibitorService;
            _sessionService = sessionService;
            _dialogService = dialogService;
            _serviceProvider = serviceProvider;
            Title = "الشركات العارضة";
        }

        public override async Task InitializeAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                var tenantId = _sessionService.TenantId;
                var listResult = await _exhibitorService.GetByTenantAsync(tenantId, CurrentPage, PageSize);
                if (listResult.IsSuccess && listResult.Data != null)
                {
                    TotalCount = listResult.Data.TotalCount;
                    Companies.Clear();
                    foreach (var summary in listResult.Data.Items)
                    {
                        var detailResult = await _exhibitorService.GetByIdAsync(tenantId, summary.ExhibitorID);
                        if (detailResult.IsSuccess && detailResult.Data != null)
                        {
                            Companies.Add(detailResult.Data);
                        }
                        else
                        {
                            Companies.Add(new ExhibitorDto
                            {
                                ExhibitorID = summary.ExhibitorID,
                                CompanyName = summary.CompanyName,
                                Sector = summary.Sector,
                                Nationality = summary.Nationality,
                                ExhibitorCategory = summary.ExhibitorCategory,
                                IsActive = summary.IsActive
                            });
                        }
                    }
                }

                FilterCompanies();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"فشل تحميل الشركات: {ex.Message}";
                NotificationService.ShowError(ErrorMessage);
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnSearchTextChanged(string value) => FilterCompanies();

        private void FilterCompanies()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredCompanies = new ObservableCollection<ExhibitorDto>(Companies);
            }
            else
            {
                var query = SearchText.Trim();
                var filtered = Companies.Where(c =>
                    c.CompanyName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    (c.ContactPerson != null && c.ContactPerson.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                    c.Sector.Contains(query, StringComparison.OrdinalIgnoreCase)
                );
                FilteredCompanies = new ObservableCollection<ExhibitorDto>(filtered);
            }
        }

        [RelayCommand]
        private async Task AddCompanyAsync()
        {
            var tenantId = _sessionService.TenantId;
            var dialogVm = _serviceProvider.GetRequiredService<AddEditCompanyDialogViewModel>();

            var result = _dialogService.ShowDialog(dialogVm);
            if (result == true)
            {
                var createDto = new ExhibitorCreateDto
                {
                    TenantID = tenantId,
                    CompanyName = dialogVm.CompanyName,
                    ContactPerson = dialogVm.ContactPerson,
                    Phone = dialogVm.Phone,
                    Email = dialogVm.Email,
                    Sector = dialogVm.Sector,
                    Nationality = dialogVm.Nationality,
                    ExhibitorCategory = dialogVm.ExhibitorCategory,
                    LogoURL = dialogVm.LogoURL,
                    CompanyProfile = dialogVm.CompanyProfile
                };

                var createResult = await ExecuteServiceAsync(() => _exhibitorService.CreateAsync(tenantId, createDto), "فشل إضافة الشركة");
                if (createResult != null)
                {
                    NotificationService.ShowSuccess("تم إضافة الشركة العارضة بنجاح!");
                    await InitializeAsync();
                }
            }
        }

        [RelayCommand]
        private async Task EditCompanyAsync(object? item)
        {
            if (item is not ExhibitorDto exhibitor) return;

            var tenantId = _sessionService.TenantId;
            var dialogVm = _serviceProvider.GetRequiredService<AddEditCompanyDialogViewModel>();
            
            dialogVm.LoadExistingData(exhibitor);

            var result = _dialogService.ShowDialog(dialogVm);
            if (result == true)
            {
                var updateDto = new ExhibitorUpdateDto
                {
                    CompanyName = dialogVm.CompanyName,
                    ContactPerson = dialogVm.ContactPerson,
                    Phone = dialogVm.Phone,
                    Email = dialogVm.Email,
                    Sector = dialogVm.Sector,
                    Nationality = dialogVm.Nationality,
                    ExhibitorCategory = dialogVm.ExhibitorCategory,
                    LogoURL = dialogVm.LogoURL,
                    CompanyProfile = dialogVm.CompanyProfile,
                    IsActive = dialogVm.IsActive
                };

                var updateResult = await ExecuteServiceAsync(() => _exhibitorService.UpdateAsync(tenantId, exhibitor.ExhibitorID, updateDto), "فشل تعديل الشركة");
                if (updateResult != null)
                {
                    NotificationService.ShowSuccess("تم تعديل بيانات الشركة بنجاح!");
                    await InitializeAsync();
                }
            }
        }

        [RelayCommand]
        private async Task DeleteCompanyAsync(object? item)
        {
            if (item is not ExhibitorDto exhibitor) return;

            var confirm = _dialogService.ShowConfirm("تأكيد الحذف", $"هل أنت متأكد من رغبتك في حذف الشركة العارضة {exhibitor.CompanyName}؟");
            if (confirm)
            {
                var tenantId = _sessionService.TenantId;
                var success = await ExecuteServiceAsync(() => _exhibitorService.DeleteAsync(tenantId, exhibitor.ExhibitorID), "فشل حذف الشركة");
                if (success)
                {
                    Companies.Remove(exhibitor);
                    FilterCompanies();
                    NotificationService.ShowSuccess($"تم حذف الشركة {exhibitor.CompanyName} بنجاح.");
                }
            }
        }

        [RelayCommand]
        private async Task ViewReservationsAsync(object? item)
        {
            if (item is not ExhibitorDto exhibitor) return;
            var tenantId = _sessionService.TenantId;
            var resResult = await _exhibitorService.GetReservationsAsync(tenantId, exhibitor.ExhibitorID);
            if (resResult.IsSuccess && resResult.Data != null)
            {
                var count = resResult.Data.Count;
                NotificationService.ShowInfo($"للشركة {exhibitor.CompanyName} عدد {count} حجز مسجل.");
            }
        }
    }
}
