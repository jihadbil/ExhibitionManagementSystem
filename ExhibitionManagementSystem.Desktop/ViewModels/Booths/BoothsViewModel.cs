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
using ExhibitionManagementSystem.Models.DTOs.Booth;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.Desktop.Services.Auth;
using ExhibitionManagementSystem.Desktop.Services.Dialog;
using Microsoft.Extensions.DependencyInjection;

namespace ExhibitionManagementSystem.Desktop.ViewModels.Booths
{
    public partial class BoothsViewModel : BaseViewModel
    {
        private readonly IBoothService _boothService;
        private readonly IHallService _hallService;
        private readonly IVenueService _venueService;
        private readonly ISessionService _sessionService;
        private readonly IDialogService _dialogService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string? _selectedStatus = "الكل";

        [ObservableProperty]
        private ObservableCollection<BoothDto> _booths = new();

        [ObservableProperty]
        private ObservableCollection<BoothDto> _filteredBooths = new();

        [ObservableProperty]
        private int _availableCount;

        [ObservableProperty]
        private int _reservedCount;

        [ObservableProperty]
        private int _underReviewCount;

        public List<string> Statuses { get; } = new() { "الكل", "متاح", "محجوز", "قيد المراجعة" };

        public BoothsViewModel(
            INavigationService navigationService,
            INotificationService notificationService,
            IBoothService boothService,
            IHallService hallService,
            IVenueService venueService,
            ISessionService sessionService,
            IDialogService dialogService,
            IServiceProvider serviceProvider)
            : base(navigationService, notificationService)
        {
            _boothService = boothService;
            _hallService = hallService;
            _venueService = venueService;
            _sessionService = sessionService;
            _dialogService = dialogService;
            _serviceProvider = serviceProvider;
            Title = "إدارة الأجنحة";
        }

        private string MapStatusToArabic(string status)
        {
            return status?.ToLower() switch
            {
                "available" => "متاح",
                "reserved" => "محجوز",
                "underreview" => "قيد المراجعة",
                "متاح" => "متاح",
                "محجوز" => "محجوز",
                "قيد المراجعة" => "قيد المراجعة",
                _ => "متاح"
            };
        }

        private string MapStatusToEnglish(string status)
        {
            return status switch
            {
                "متاح" => "Available",
                "محجوز" => "Reserved",
                "قيد المراجعة" => "UnderReview",
                _ => "Available"
            };
        }

        public override async Task InitializeAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                var tenantId = _sessionService.TenantId;
                var venuesResult = await _venueService.GetByTenantAsync(tenantId);
                
                Booths.Clear();
                if (venuesResult.IsSuccess && venuesResult.Data != null)
                {
                    foreach (var venue in venuesResult.Data)
                    {
                        var hallsResult = await _hallService.GetByVenueAsync(tenantId, venue.VenueID);
                        if (hallsResult.IsSuccess && hallsResult.Data != null)
                        {
                            foreach (var hall in hallsResult.Data)
                            {
                                var boothsResult = await _boothService.GetByHallAsync(tenantId, hall.HallID);
                                if (boothsResult.IsSuccess && boothsResult.Data != null)
                                {
                                    foreach (var booth in boothsResult.Data)
                                    {
                                        booth.Status = MapStatusToArabic(booth.Status);
                                        Booths.Add(booth);
                                    }
                                }
                            }
                        }
                    }
                }

                CalculateStats();
                FilterBooths();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"فشل تحميل الأجنحة: {ex.Message}";
                NotificationService.ShowError(ErrorMessage);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CalculateStats()
        {
            TotalCount = Booths.Count;
            AvailableCount = Booths.Count(b => b.Status == "متاح");
            ReservedCount = Booths.Count(b => b.Status == "محجوز");
            UnderReviewCount = Booths.Count(b => b.Status == "قيد المراجعة");
        }

        partial void OnSearchTextChanged(string value) => FilterBooths();
        partial void OnSelectedStatusChanged(string? value) => FilterBooths();

        private void FilterBooths()
        {
            var query = SearchText.Trim();
            var statusFilter = SelectedStatus;

            var filtered = Booths.Where(b =>
            {
                bool matchesSearch = string.IsNullOrWhiteSpace(query) ||
                                     (b.BoothNumber != null && b.BoothNumber.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                                     (b.HallName != null && b.HallName.Contains(query, StringComparison.OrdinalIgnoreCase));

                bool matchesStatus = string.IsNullOrWhiteSpace(statusFilter) ||
                                     statusFilter == "الكل" ||
                                     b.Status == statusFilter;

                return matchesSearch && matchesStatus;
            });

            FilteredBooths = new ObservableCollection<BoothDto>(filtered);
        }

        [RelayCommand]
        private void GoToDesigner()
        {
            NavigationService.NavigateTo<BoothDesignerViewModel>();
        }

        [RelayCommand]
        private async Task AddBoothAsync()
        {
            var tenantId = _sessionService.TenantId;
            var dialogVm = _serviceProvider.GetRequiredService<AddEditBoothDialogViewModel>();

            await dialogVm.LoadHallsAsync(tenantId);

            var result = _dialogService.ShowDialog(dialogVm);
            if (result == true)
            {
                var createDto = new BoothCreateDto
                {
                    HallID = dialogVm.SelectedHall?.HallID ?? 0,
                    BoothNumber = dialogVm.BoothNumber,
                    OriginalAreaSqM = dialogVm.OriginalAreaSqM,
                    PosX = dialogVm.PosX,
                    PosY = dialogVm.PosY,
                    Width = dialogVm.Width,
                    Height = dialogVm.Height
                };

                var createResult = await ExecuteServiceAsync(() => _boothService.CreateAsync(tenantId, createDto), "فشل إضافة الجناح");
                if (createResult != null)
                {
                    createResult.Status = MapStatusToArabic(createResult.Status);
                    Booths.Add(createResult);
                    CalculateStats();
                    FilterBooths();
                    NotificationService.ShowSuccess("تم إضافة الجناح بنجاح!");
                }
            }
        }

        [RelayCommand]
        private async Task EditBoothAsync(object? item)
        {
            if (item is not BoothDto booth) return;

            var tenantId = _sessionService.TenantId;
            var dialogVm = _serviceProvider.GetRequiredService<AddEditBoothDialogViewModel>();
            
            await dialogVm.LoadHallsAsync(tenantId, booth.HallID);
            dialogVm.LoadExistingData(booth);

            var result = _dialogService.ShowDialog(dialogVm);
            if (result == true)
            {
                var updateDto = new BoothUpdateDto
                {
                    BoothNumber = dialogVm.BoothNumber,
                    Status = MapStatusToEnglish(booth.Status),
                    PosX = dialogVm.PosX,
                    PosY = dialogVm.PosY,
                    Width = dialogVm.Width,
                    Height = dialogVm.Height,
                    RotationAngle = booth.RotationAngle,
                    ShapeType = booth.ShapeType,
                    ShapePolygonJSON = booth.ShapePolygonJSON
                };

                var updateResult = await ExecuteServiceAsync(() => _boothService.UpdateAsync(tenantId, booth.BoothID, updateDto), "فشل تعديل الجناح");
                if (updateResult != null)
                {
                    NotificationService.ShowSuccess("تم تعديل الجناح بنجاح!");
                    await InitializeAsync();
                }
            }
        }

        [RelayCommand]
        private void DeleteBooth(object? item)
        {
            if (item is not BoothDto booth) return;

            var confirm = _dialogService.ShowConfirm("تأكيد الحذف", $"هل أنت متأكد من رغبتك في حذف الجناح {booth.BoothNumber}؟");
            if (confirm)
            {
                Booths.Remove(booth);
                CalculateStats();
                FilterBooths();
                NotificationService.ShowSuccess($"تم حذف الجناح {booth.BoothNumber} بنجاح.");
            }
        }
    }
}
