using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.Models.DTOs.Booth;
using ExhibitionManagementSystem.Models.DTOs.Hall;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.Desktop.ViewModels.Booths
{
    public partial class AddEditBoothDialogViewModel : ObservableObject
    {
        private readonly IVenueService _venueService;
        private readonly IHallService _hallService;

        public event Action<bool>? RequestClose;

        [ObservableProperty]
        private string _boothNumber = string.Empty;

        [ObservableProperty]
        private decimal _originalAreaSqM = 12;

        [ObservableProperty]
        private decimal _posX = 50;

        [ObservableProperty]
        private decimal _posY = 50;

        [ObservableProperty]
        private decimal _width = 100;

        [ObservableProperty]
        private decimal _height = 80;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private string _title = "إضافة جناح جديد";

        [ObservableProperty]
        private ObservableCollection<HallDto> _halls = new();

        [ObservableProperty]
        private HallDto? _selectedHall;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _isLoading;

        public AddEditBoothDialogViewModel(IVenueService venueService, IHallService hallService)
        {
            _venueService = venueService;
            _hallService = hallService;
        }

        public async Task LoadHallsAsync(int tenantId, int? selectedHallId = null)
        {
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                var venuesResult = await _venueService.GetByTenantAsync(tenantId);
                Halls.Clear();
                if (venuesResult.IsSuccess && venuesResult.Data != null)
                {
                    foreach (var venue in venuesResult.Data)
                    {
                        var hallsResult = await _hallService.GetByVenueAsync(tenantId, venue.VenueID);
                        if (hallsResult.IsSuccess && hallsResult.Data != null)
                        {
                            foreach (var hall in hallsResult.Data)
                            {
                                Halls.Add(hall);
                            }
                        }
                    }
                }

                if (selectedHallId.HasValue)
                {
                    SelectedHall = Halls.FirstOrDefault(h => h.HallID == selectedHallId.Value);
                }
                else if (Halls.Any())
                {
                    SelectedHall = Halls.First();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"فشل تحميل القاعات: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void LoadExistingData(BoothDto booth)
        {
            IsEditMode = true;
            Title = "تعديل الجناح";
            BoothNumber = booth.BoothNumber;
            OriginalAreaSqM = booth.OriginalAreaSqM;
            PosX = booth.PosX ?? 0;
            PosY = booth.PosY ?? 0;
            Width = booth.Width ?? 0;
            Height = booth.Height ?? 0;
        }

        [RelayCommand]
        private void Confirm()
        {
            if (string.IsNullOrWhiteSpace(BoothNumber))
            {
                ErrorMessage = "رقم الجناح مطلوب.";
                return;
            }

            if (SelectedHall == null)
            {
                ErrorMessage = "يجب اختيار قاعة.";
                return;
            }

            if (OriginalAreaSqM <= 0)
            {
                ErrorMessage = "المساحة يجب أن تكون أكبر من الصفر.";
                return;
            }

            RequestClose?.Invoke(true);
        }

        [RelayCommand]
        private void Cancel()
        {
            RequestClose?.Invoke(false);
        }
    }
}
