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

namespace ExhibitionManagementSystem.Desktop.ViewModels.Booths
{
    public partial class BoothDesignerViewModel : BaseViewModel
    {
        private readonly IBoothService _boothService;
        private readonly IHallService _hallService;
        private readonly IVenueService _venueService;
        private readonly IPricingService _pricingService;
        private readonly ISessionService _sessionService;

        [ObservableProperty]
        private string _boothId = string.Empty;

        [ObservableProperty]
        private double _length;

        [ObservableProperty]
        private double _width;

        [ObservableProperty]
        private string _category = "Standard";

        [ObservableProperty]
        private string _status = "متاح";

        [ObservableProperty]
        private decimal _price;

        [ObservableProperty]
        private ObservableCollection<BoothDto> _booths = new();

        [ObservableProperty]
        private BoothDto? _selectedBooth;

        [ObservableProperty]
        private double _canvasScale = 1.0;

        public string SelectedBoothInfo => SelectedBooth != null
            ? $"{SelectedBooth.BoothNumber} — {SelectedBooth.HallName}"
            : "لا يوجد جناح محدد";

        public BoothDesignerViewModel(
            INavigationService navigationService,
            INotificationService notificationService,
            IBoothService boothService,
            IHallService hallService,
            IVenueService venueService,
            IPricingService pricingService,
            ISessionService sessionService)
            : base(navigationService, notificationService)
        {
            _boothService = boothService;
            _hallService = hallService;
            _venueService = venueService;
            _pricingService = pricingService;
            _sessionService = sessionService;
            Title = "مصمم الأجنحة";
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
                                        Booths.Add(booth);
                                    }
                                }
                            }
                        }
                    }
                }

                if (Booths.Count > 0)
                {
                    SelectedBooth = Booths[0];
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"فشل تحميل مصمم الأجنحة: {ex.Message}";
                NotificationService.ShowError(ErrorMessage);
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnSelectedBoothChanged(BoothDto? value)
        {
            if (value != null)
            {
                BoothId = value.BoothNumber;
                Length = (double)(value.Height ?? 0) / 10.0;
                Width = (double)(value.Width ?? 0) / 10.0;
                Category = value.HallName == "القاعة أ" ? "VIP" : (value.HallName == "القاعة ب" ? "Premium" : "Standard");
                Status = value.Status;
                
                // Real price calculation from IPricingService or default
                UpdateBoothPriceAsync(value.CurrentAreaSqM);
                
                OnPropertyChanged(nameof(SelectedBoothInfo));
            }
        }

        private async void UpdateBoothPriceAsync(decimal area)
        {
            var tenantId = _sessionService.TenantId;
            var priceResult = await _pricingService.CalculateBoothPriceAsync(
                tenantId, 
                null, 
                global::ExhibitionManagementSystem.Models.Enums.BoothType.Equipped, 
                global::ExhibitionManagementSystem.Models.Enums.ExhibitorCategory.Local, 
                area
            );
            Price = priceResult.IsSuccess ? priceResult.Data : area * 500;
        }

        [RelayCommand]
        private async Task SaveBoothAsync()
        {
            if (SelectedBooth == null) return;

            var tenantId = _sessionService.TenantId;
            var updateDto = new BoothUpdateDto
            {
                BoothNumber = BoothId,
                Status = Status,
                PosX = SelectedBooth.PosX,
                PosY = SelectedBooth.PosY,
                Width = (decimal)(Width * 10.0),
                Height = (decimal)(Length * 10.0),
                RotationAngle = SelectedBooth.RotationAngle,
                ShapeType = SelectedBooth.ShapeType,
                ShapePolygonJSON = SelectedBooth.ShapePolygonJSON
            };

            var result = await ExecuteServiceAsync(() => _boothService.UpdateAsync(tenantId, SelectedBooth.BoothID, updateDto), "فشل حفظ بيانات الجناح");
            if (result != null)
            {
                SelectedBooth.BoothNumber = result.BoothNumber;
                SelectedBooth.Width = result.Width;
                SelectedBooth.Height = result.Height;
                SelectedBooth.Status = result.Status;
                SelectedBooth.CurrentAreaSqM = result.CurrentAreaSqM;

                UpdateBoothPriceAsync(result.CurrentAreaSqM);

                NotificationService.ShowSuccess($"تم حفظ بيانات الجناح {BoothId} بنجاح.");
                OnPropertyChanged(nameof(SelectedBoothInfo));
            }
        }

        [RelayCommand]
        private void AutoArrange()
        {
            double startX = 60;
            double startY = 60;
            double padding = 24;
            int columns = 4;

            for (int i = 0; i < Booths.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;

                Booths[i].PosX = (decimal)(startX + col * (100 + padding));
                Booths[i].PosY = (decimal)(startY + row * (80 + padding));
            }
            NotificationService.ShowInfo("تمت إعادة ترتيب الأجنحة تلقائياً في شبكة منظمة.");
        }

        [RelayCommand]
        private void ZoomIn()
        {
            CanvasScale = Math.Min(3.0, CanvasScale + 0.25);
        }

        [RelayCommand]
        private void ZoomOut()
        {
            CanvasScale = Math.Max(0.25, CanvasScale - 0.25);
        }

        [RelayCommand]
        private void ResetZoom()
        {
            CanvasScale = 1.0;
        }

        [RelayCommand]
        private async Task SaveLayoutAsync()
        {
            var tenantId = _sessionService.TenantId;
            var successCount = 0;
            foreach (var booth in Booths)
            {
                var updateDto = new BoothUpdateDto
                {
                    BoothNumber = booth.BoothNumber,
                    Status = booth.Status,
                    PosX = booth.PosX,
                    PosY = booth.PosY,
                    Width = booth.Width,
                    Height = booth.Height,
                    RotationAngle = booth.RotationAngle,
                    ShapeType = booth.ShapeType,
                    ShapePolygonJSON = booth.ShapePolygonJSON
                };
                var result = await _boothService.UpdateAsync(tenantId, booth.BoothID, updateDto);
                if (result.IsSuccess)
                {
                    successCount++;
                }
            }
            NotificationService.ShowSuccess($"تم حفظ المخطط لـ {successCount} جناح.");
        }

        [RelayCommand]
        private async Task MergeBoothsAsync()
        {
            var tenantId = _sessionService.TenantId;
            var userId = _sessionService.UserId;
            if (Booths.Count < 2)
            {
                NotificationService.ShowWarning("يجب أن يكون هناك جناحان على الأقل للدمج.");
                return;
            }

            var mergeDto = new BoothMergeCreateDto
            {
                HallID = Booths[0].HallID,
                ExhibitionID = 1,
                MergedBoothLabel = $"{Booths[0].BoothNumber}-{Booths[1].BoothNumber}-M",
                BoothIDs = new List<int> { Booths[0].BoothID, Booths[1].BoothID }
            };

            var result = await ExecuteServiceAsync(() => _boothService.MergeBoothsAsync(tenantId, userId, mergeDto), "فشل دمج الأجنحة");
            if (result != null)
            {
                NotificationService.ShowSuccess("تم دمج الأجنحة بنجاح!");
                await InitializeAsync();
            }
        }

        [RelayCommand]
        private async Task UnmergeBoothsAsync(int mergeId)
        {
            var tenantId = _sessionService.TenantId;
            var success = await ExecuteServiceAsync(() => _boothService.UnmergeBoothsAsync(tenantId, mergeId), "فشل إلغاء دمج الأجنحة");
            if (success)
            {
                NotificationService.ShowSuccess("تم إلغاء دمج الأجنحة بنجاح!");
                await InitializeAsync();
            }
        }
    }
}

