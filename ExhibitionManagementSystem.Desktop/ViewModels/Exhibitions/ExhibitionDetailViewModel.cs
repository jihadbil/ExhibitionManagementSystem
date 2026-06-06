using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.Desktop.Services.Navigation;
using ExhibitionManagementSystem.Desktop.Services.Notifications;
using ExhibitionManagementSystem.Desktop.ViewModels.Base;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.Desktop.Services.Auth;
using ExhibitionManagementSystem.Models.DTOs.Reservation;

namespace ExhibitionManagementSystem.Desktop.ViewModels.Exhibitions
{
    public class BoothSummary
    {
        public string Id { get; set; } = string.Empty;
        public string Category { get; set; } = "Standard"; // VIP, Premium, Standard
        public string Status { get; set; } = "Available"; // Available, Reserved, UnderReview
        public decimal Price { get; set; }
        public string CompanyName { get; set; } = "غير محجوز";
    }

    public partial class ExhibitionDetailViewModel : BaseViewModel
    {
        private readonly IExhibitionService _exhibitionService;
        private readonly IBoothService _boothService;
        private readonly IHallService _hallService;
        private readonly IReservationService _reservationService;
        private readonly ISessionService _sessionService;

        [ObservableProperty]
        private ExhibitionDto? _exhibition;

        [ObservableProperty]
        private ObservableCollection<BoothSummary> _booths = new();

        [ObservableProperty]
        private ObservableCollection<ExhibitionScheduleDto> _schedules = new();

        public ExhibitionDetailViewModel(
            INavigationService navigationService,
            INotificationService notificationService,
            IExhibitionService exhibitionService,
            IBoothService boothService,
            IHallService hallService,
            IReservationService reservationService,
            ISessionService sessionService)
            : base(navigationService, notificationService)
        {
            _exhibitionService = exhibitionService;
            _boothService = boothService;
            _hallService = hallService;
            _reservationService = reservationService;
            _sessionService = sessionService;
            Title = "تفاصيل المعرض";
        }

        public override async Task InitializeAsync(object parameter)
        {
            IsLoading = true;
            try
            {
                var tenantId = _sessionService.TenantId;

                if (parameter is int exhibitionId)
                {
                    var result = await _exhibitionService.GetByIdAsync(tenantId, exhibitionId);
                    if (result.IsSuccess && result.Data != null)
                    {
                        Exhibition = result.Data;
                        Title = $"تفاصيل - {Exhibition.Name}";
                        await LoadBoothsForExhibitionAsync();
                        await LoadSchedulesAsync();
                    }
                }
                else if (parameter is ExhibitionDto model)
                {
                    Exhibition = model;
                    Title = $"تفاصيل - {model.Name}";
                    await LoadBoothsForExhibitionAsync();
                    await LoadSchedulesAsync();
                }
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"فشل تحميل تفاصيل المعرض: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadSchedulesAsync()
        {
            Schedules.Clear();
            if (Exhibition == null) return;

            var tenantId = _sessionService.TenantId;
            var exhibitionId = Exhibition.ExhibitionID;

            var result = await _exhibitionService.GetSchedulesAsync(tenantId, exhibitionId);
            if (result.IsSuccess && result.Data != null)
            {
                foreach (var schedule in result.Data)
                {
                    Schedules.Add(schedule);
                }
            }
        }


        private async Task LoadBoothsForExhibitionAsync()
        {
            Booths.Clear();
            if (Exhibition == null) return;

            var tenantId = _sessionService.TenantId;
            var exhibitionId = Exhibition.ExhibitionID;
            var venueId = Exhibition.VenueID;

            // 1. Get all halls for the venue
            var hallsResult = await _hallService.GetByVenueAsync(tenantId, venueId);
            if (!hallsResult.IsSuccess || hallsResult.Data == null) return;

            // 2. Get all reservations for the exhibition
            var reservationsResult = await _reservationService.GetByExhibitionAsync(tenantId, exhibitionId, 1, int.MaxValue);
            var reservations = reservationsResult.IsSuccess && reservationsResult.Data != null 
                ? reservationsResult.Data.Items 
                : new List<BoothReservationSummaryDto>();

            // 3. For each hall, get its booths
            foreach (var hall in hallsResult.Data)
            {
                var boothsResult = await _boothService.GetByHallAsync(tenantId, hall.HallID);
                if (boothsResult.IsSuccess && boothsResult.Data != null)
                {
                    foreach (var booth in boothsResult.Data)
                    {
                        // Check if this booth is reserved in the current exhibition
                        var reservation = reservations.FirstOrDefault(r => r.BoothNumber == booth.BoothNumber);
                        
                        var summary = new BoothSummary
                        {
                            Id = booth.BoothNumber,
                            Category = booth.IsMerged ? "VIP" : (booth.CurrentAreaSqM > 20 ? "Premium" : "Standard"),
                            Price = booth.CurrentAreaSqM * 500, // standard price calculation
                            Status = reservation != null ? "Reserved" : "Available",
                            CompanyName = reservation != null ? reservation.ExhibitorName : "غير محجوز"
                        };
                        
                        Booths.Add(summary);
                    }
                }
            }
        }

        [RelayCommand]
        private void GoBack()
        {
            NavigationService.GoBack();
        }

        [RelayCommand]
        private void EditExhibitionDetails()
        {
            NotificationService.ShowInfo("تعديل تفاصيل المعرض (محاكاة)");
        }
    }
}

