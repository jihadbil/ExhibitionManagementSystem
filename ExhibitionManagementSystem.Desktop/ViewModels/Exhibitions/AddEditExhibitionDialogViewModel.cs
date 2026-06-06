using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.Models.DTOs.Venue;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.Desktop.ViewModels.Exhibitions
{
    public partial class AddEditExhibitionDialogViewModel : ObservableObject
    {
        private readonly IVenueService _venueService;

        public event Action<bool>? RequestClose;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _type = "عام";

        [ObservableProperty]
        private string _edition = "الأولى";

        [ObservableProperty]
        private DateTime _startDate = DateTime.Now.AddDays(30);

        [ObservableProperty]
        private DateTime _endDate = DateTime.Now.AddDays(35);

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private int _expectedVisitors = 1000;

        [ObservableProperty]
        private decimal _entryFee = 0;

        [ObservableProperty]
        private string _entryCurrency = "SAR";

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private string _title = "إضافة معرض جديد";

        [ObservableProperty]
        private ObservableCollection<VenueDto> _venues = new();

        [ObservableProperty]
        private VenueDto? _selectedVenue;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _isLoading;

        public AddEditExhibitionDialogViewModel(IVenueService venueService)
        {
            _venueService = venueService;
        }

        public async Task LoadVenuesAsync(int tenantId, int? selectedVenueId = null)
        {
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                var result = await _venueService.GetByTenantAsync(tenantId);
                if (result.IsSuccess && result.Data != null)
                {
                    Venues.Clear();
                    foreach (var venue in result.Data)
                    {
                        Venues.Add(venue);
                    }

                    if (selectedVenueId.HasValue)
                    {
                        SelectedVenue = Venues.FirstOrDefault(v => v.VenueID == selectedVenueId.Value);
                    }
                    else if (Venues.Any())
                    {
                        SelectedVenue = Venues.First();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"فشل تحميل الصالات: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void LoadExistingData(ExhibitionDto exhibition)
        {
            IsEditMode = true;
            Title = "تعديل المعرض";
            Name = exhibition.Name;
            Type = exhibition.Type;
            Edition = exhibition.Edition;
            StartDate = exhibition.StartDate;
            EndDate = exhibition.EndDate;
            Description = exhibition.Description;
            ExpectedVisitors = exhibition.ExpectedVisitors ?? 0;
            EntryFee = exhibition.EntryFee ?? 0;
            EntryCurrency = exhibition.EntryCurrency;
        }

        [RelayCommand]
        private void Confirm()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "اسم المعرض مطلوب.";
                return;
            }

            if (SelectedVenue == null)
            {
                ErrorMessage = "يجب اختيار صالة عرض.";
                return;
            }

            if (StartDate >= EndDate)
            {
                ErrorMessage = "تاريخ النهاية يجب أن يكون بعد تاريخ البداية.";
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
