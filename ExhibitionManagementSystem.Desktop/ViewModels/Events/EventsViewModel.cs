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
using ExhibitionManagementSystem.Models.DTOs.Service;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.Desktop.Services.Auth;
using ExhibitionManagementSystem.Desktop.Services.Dialog;
using Microsoft.Extensions.DependencyInjection;

namespace ExhibitionManagementSystem.Desktop.ViewModels.Events
{
    public partial class EventsViewModel : BaseViewModel
    {
        private readonly IServiceManagementService _serviceManagementService;
        private readonly ISessionService _sessionService;
        private readonly IDialogService _dialogService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<ServiceDto> _events = new();

        [ObservableProperty]
        private ObservableCollection<ServiceDto> _filteredEvents = new();

        public EventsViewModel(
            INavigationService navigationService,
            INotificationService notificationService,
            IServiceManagementService serviceManagementService,
            ISessionService sessionService,
            IDialogService dialogService,
            IServiceProvider serviceProvider)
            : base(navigationService, notificationService)
        {
            _serviceManagementService = serviceManagementService;
            _sessionService = sessionService;
            _dialogService = dialogService;
            _serviceProvider = serviceProvider;
            Title = "خدمات وفعاليات المعارض";
        }

        public override async Task InitializeAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                var tenantId = _sessionService.TenantId;
                var result = await _serviceManagementService.GetByTenantAsync(tenantId);

                Events.Clear();
                if (result.IsSuccess && result.Data != null)
                {
                    foreach (var item in result.Data)
                    {
                        Events.Add(item);
                    }
                }

                FilterEvents();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"فشل تحميل الفعاليات والخدمات: {ex.Message}";
                NotificationService.ShowError(ErrorMessage);
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnSearchTextChanged(string value) => FilterEvents();

        private void FilterEvents()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredEvents = new ObservableCollection<ServiceDto>(Events);
            }
            else
            {
                var query = SearchText.Trim();
                var filtered = Events.Where(s =>
                    (s.ServiceName != null && s.ServiceName.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                    (s.Category != null && s.Category.Contains(query, StringComparison.OrdinalIgnoreCase))
                );
                FilteredEvents = new ObservableCollection<ServiceDto>(filtered);
            }
        }

        [RelayCommand]
        private async Task AddEventAsync()
        {
            var tenantId = _sessionService.TenantId;
            var dialogVm = _serviceProvider.GetRequiredService<AddEditEventDialogViewModel>();

            var result = _dialogService.ShowDialog(dialogVm);
            if (result == true)
            {
                var createDto = new ServiceCreateDto
                {
                    TenantID = tenantId,
                    ServiceName = dialogVm.ServiceName,
                    Category = dialogVm.Category,
                    Unit = dialogVm.Unit,
                    DefaultPrice = dialogVm.DefaultPrice,
                    IsMandatory = dialogVm.IsMandatory,
                    Description = dialogVm.Description
                };

                var createResult = await ExecuteServiceAsync(() => _serviceManagementService.CreateAsync(tenantId, createDto), "فشل إضافة الخدمة");
                if (createResult != null)
                {
                    Events.Add(createResult);
                    FilterEvents();
                    NotificationService.ShowSuccess("تم إضافة الخدمة الجديدة بنجاح!");
                }
            }
        }

        [RelayCommand]
        private async Task EditEventAsync(object? item)
        {
            if (item is not ServiceDto service) return;
            
            var tenantId = _sessionService.TenantId;
            var dialogVm = _serviceProvider.GetRequiredService<AddEditEventDialogViewModel>();
            
            dialogVm.LoadExistingData(service);

            var result = _dialogService.ShowDialog(dialogVm);
            if (result == true)
            {
                var editDto = new ServiceCreateDto
                {
                    TenantID = tenantId,
                    ServiceName = dialogVm.ServiceName,
                    Category = dialogVm.Category,
                    Unit = dialogVm.Unit,
                    DefaultPrice = dialogVm.DefaultPrice,
                    IsMandatory = dialogVm.IsMandatory,
                    Description = dialogVm.Description
                };

                var updateResult = await ExecuteServiceAsync(() => _serviceManagementService.UpdateAsync(tenantId, service.ServiceID, editDto), "فشل تعديل الخدمة");
                if (updateResult != null)
                {
                    var index = Events.IndexOf(service);
                    if (index >= 0)
                    {
                        Events[index] = updateResult;
                    }
                    FilterEvents();
                    NotificationService.ShowSuccess($"تم تعديل الخدمة {updateResult.ServiceName} بنجاح.");
                }
            }
        }

        [RelayCommand]
        private async Task DeleteEventAsync(object? item)
        {
            if (item is not ServiceDto service) return;

            var confirm = _dialogService.ShowConfirm("تأكيد إلغاء التنشيط", $"هل أنت متأكد من رغبتك في إلغاء تنشيط وحذف الخدمة {service.ServiceName}؟");
            if (confirm)
            {
                var tenantId = _sessionService.TenantId;
                var success = await ExecuteServiceAsync(() => _serviceManagementService.DeactivateAsync(tenantId, service.ServiceID), "فشل إلغاء تنشيط الخدمة");
                if (success)
                {
                    Events.Remove(service);
                    FilterEvents();
                    NotificationService.ShowSuccess($"تم إلغاء تنشيط وحذف الخدمة {service.ServiceName} بنجاح.");
                }
            }
        }
    }
}
