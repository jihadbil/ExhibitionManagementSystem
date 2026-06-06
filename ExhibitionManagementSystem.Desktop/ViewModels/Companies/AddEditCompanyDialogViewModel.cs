using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.Models.DTOs.Exhibitor;

namespace ExhibitionManagementSystem.Desktop.ViewModels.Companies
{
    public partial class AddEditCompanyDialogViewModel : ObservableObject
    {
        public event Action<bool>? RequestClose;

        [ObservableProperty]
        private string _companyName = string.Empty;

        [ObservableProperty]
        private string _contactPerson = string.Empty;

        [ObservableProperty]
        private string _phone = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _sector = "عام";

        [ObservableProperty]
        private string _nationality = "سعودي";

        [ObservableProperty]
        private string _exhibitorCategory = "Local";

        [ObservableProperty]
        private string _logoURL = string.Empty;

        [ObservableProperty]
        private string _companyProfile = string.Empty;

        [ObservableProperty]
        private bool _isActive = true;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private string _title = "إضافة شركة عارضة جديدة";

        [ObservableProperty]
        private string? _errorMessage;

        public ObservableCollection<string> ExhibitorCategories { get; } = new() { "Local", "International", "Government" };

        public AddEditCompanyDialogViewModel()
        {
        }

        public void LoadExistingData(ExhibitorDto exhibitor)
        {
            IsEditMode = true;
            Title = "تعديل بيانات الشركة";
            CompanyName = exhibitor.CompanyName;
            ContactPerson = exhibitor.ContactPerson ?? string.Empty;
            Phone = exhibitor.Phone ?? string.Empty;
            Email = exhibitor.Email ?? string.Empty;
            Sector = exhibitor.Sector ?? string.Empty;
            Nationality = exhibitor.Nationality ?? string.Empty;
            ExhibitorCategory = exhibitor.ExhibitorCategory ?? "Local";
            LogoURL = exhibitor.LogoURL ?? string.Empty;
            CompanyProfile = exhibitor.CompanyProfile ?? string.Empty;
            IsActive = exhibitor.IsActive;
        }

        [RelayCommand]
        private void Confirm()
        {
            if (string.IsNullOrWhiteSpace(CompanyName))
            {
                ErrorMessage = "اسم الشركة مطلوب.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "البريد الإلكتروني مطلوب.";
                return;
            }

            if (string.IsNullOrWhiteSpace(ExhibitorCategory))
            {
                ErrorMessage = "يجب اختيار فئة العارض.";
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
