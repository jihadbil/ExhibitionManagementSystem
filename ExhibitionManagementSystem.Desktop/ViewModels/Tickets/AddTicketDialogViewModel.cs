using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.Desktop.ViewModels.Tickets
{
    public partial class AddTicketDialogViewModel : ObservableObject
    {
        private readonly IExhibitionService _exhibitionService;

        public event Action<bool>? RequestClose;

        [ObservableProperty]
        private string _fullName = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _phone = string.Empty;

        [ObservableProperty]
        private string _nationality = "سعودي";

        [ObservableProperty]
        private string _visitorType = "عام";

        [ObservableProperty]
        private string _ticketType = "عام";

        [ObservableProperty]
        private decimal _price = 50;

        [ObservableProperty]
        private string _currencyCode = "SAR";

        [ObservableProperty]
        private DateTime _validDate = DateTime.Now.AddDays(5);

        [ObservableProperty]
        private ObservableCollection<ExhibitionSummaryDto> _activeExhibitions = new();

        [ObservableProperty]
        private ExhibitionSummaryDto? _selectedExhibition;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _title = "إصدار تذكرة جديدة";

        public AddTicketDialogViewModel(IExhibitionService exhibitionService)
        {
            _exhibitionService = exhibitionService;
        }

        public async Task LoadActiveExhibitionsAsync(int tenantId)
        {
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                var result = await _exhibitionService.GetActiveAsync(tenantId);
                if (result.IsSuccess && result.Data != null)
                {
                    ActiveExhibitions.Clear();
                    foreach (var expo in result.Data)
                    {
                        ActiveExhibitions.Add(expo);
                    }

                    if (ActiveExhibitions.Any())
                    {
                        SelectedExhibition = ActiveExhibitions.First();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"فشل تحميل المعارض النشطة: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void Confirm()
        {
            if (string.IsNullOrWhiteSpace(FullName))
            {
                ErrorMessage = "اسم الزائر مطلوب.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "البريد الإلكتروني مطلوب.";
                return;
            }

            if (SelectedExhibition == null)
            {
                ErrorMessage = "يجب اختيار معرض.";
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
