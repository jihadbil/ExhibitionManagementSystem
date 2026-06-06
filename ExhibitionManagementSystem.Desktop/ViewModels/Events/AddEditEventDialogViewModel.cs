using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.Models.DTOs.Service;

namespace ExhibitionManagementSystem.Desktop.ViewModels.Events
{
    public partial class AddEditEventDialogViewModel : ObservableObject
    {
        public event Action<bool>? RequestClose;

        [ObservableProperty]
        private string _serviceName = string.Empty;

        [ObservableProperty]
        private string _category = "لوجستي";

        [ObservableProperty]
        private string _unit = "يوم";

        [ObservableProperty]
        private decimal _defaultPrice = 1000;

        [ObservableProperty]
        private bool _isMandatory;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private string _title = "إضافة خدمة/فعالية جديدة";

        [ObservableProperty]
        private string? _errorMessage;

        public ObservableCollection<string> Categories { get; } = new() { "لوجستي", "تقني", "ترفيهي", "تعليمي", "أخرى" };

        public AddEditEventDialogViewModel()
        {
        }

        public void LoadExistingData(ServiceDto service)
        {
            IsEditMode = true;
            Title = "تعديل الخدمة/الفعالية";
            ServiceName = service.ServiceName ?? string.Empty;
            Category = service.Category ?? "لوجستي";
            Unit = service.Unit ?? "يوم";
            DefaultPrice = service.DefaultPrice ?? 0;
            IsMandatory = service.IsMandatory;
            Description = service.Description ?? string.Empty;
        }

        [RelayCommand]
        private void Confirm()
        {
            if (string.IsNullOrWhiteSpace(ServiceName))
            {
                ErrorMessage = "اسم الخدمة/الفعالية مطلوب.";
                return;
            }

            if (DefaultPrice < 0)
            {
                ErrorMessage = "السعر الافتراضي لا يمكن أن يكون سالباً.";
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
