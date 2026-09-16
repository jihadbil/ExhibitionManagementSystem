using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.DeskTop.Helpers;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using ExhibitionManagementSystem.Models.DTOs.Exhibitor;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Companies
{
    public partial class ExhibitorFormViewModel : ViewModelBase
    {
        private readonly IExhibitorService _exhibitorService;

        // ━━━━━━━━━━━━━━ Collections ━━━━━━━━━━━━━━
        public ObservableCollection<string> ExhibitorCategories { get; } = new() { "Local", "International", "Government" };

        // ━━━━━━━━━━━━━━ Observable Properties ━━━━━━━━━━━━━━
        [ObservableProperty]
        private int _exhibitorId;

        [ObservableProperty]
        private string _companyName = string.Empty;

        [ObservableProperty]
        private string _sector = string.Empty;

        [ObservableProperty]
        private string _nationality = "ليبي";

        [ObservableProperty]
        private string _exhibitorCategory = "Local";

        [ObservableProperty]
        private string _contactPerson = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _phone = string.Empty;

        [ObservableProperty]
        private string _logoURL = string.Empty;

        [ObservableProperty]
        private string _companyProfile = string.Empty;

        [ObservableProperty]
        private bool _isEditMode;

        public Action? CloseAction { get; set; }

        // ━━━━━━━━━━━━━━ Constructor ━━━━━━━━━━━━━━
        public ExhibitorFormViewModel(
            IExhibitorService exhibitorService,
            INavigationService navigationService,
            INotificationService notificationService,
            SessionService session) : base(navigationService, notificationService, session)
        {
            _exhibitorService = exhibitorService;
            Title = "إضافة شركة عارضة";
        }

        // ━━━━━━━━━━━━━━ Methods ━━━━━━━━━━━━━━
        public async Task InitializeAsync(int exhibitorId = 0)
        {
            ExhibitorId = exhibitorId;
            IsEditMode = exhibitorId > 0;
            Title = IsEditMode ? "تعديل بيانات الشركة" : "إضافة شركة عارضة";

            CompanyName = string.Empty;
            Sector = string.Empty;
            Nationality = "ليبي";
            ExhibitorCategory = "Local";
            ContactPerson = string.Empty;
            Email = string.Empty;
            Phone = string.Empty;
            LogoURL = string.Empty;
            CompanyProfile = string.Empty;

            if (IsEditMode)
            {
                await ExecuteSafeAsync(async () =>
                {
                    var result = await _exhibitorService.GetByIdAsync(Session.TenantId, ExhibitorId);
                    if (result.IsSuccess && result.Data is not null)
                    {
                        CompanyName = result.Data.CompanyName;
                        Sector = result.Data.Sector;
                        Nationality = result.Data.Nationality;
                        ExhibitorCategory = result.Data.ExhibitorCategory;
                        ContactPerson = result.Data.ContactPerson;
                        Email = result.Data.Email;
                        Phone = result.Data.Phone;
                        LogoURL = result.Data.LogoURL;
                        CompanyProfile = result.Data.CompanyProfile;
                    }
                }, "خطأ في تحميل بيانات الشركة");
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(CompanyName))
            {
                NotificationService.ShowError("الرجاء إدخال اسم الشركة");
                return;
            }

            if (string.IsNullOrWhiteSpace(ExhibitorCategory))
            {
                NotificationService.ShowError("الرجاء اختيار فئة العارض");
                return;
            }

            await ExecuteSafeAsync(async () =>
            {
                if (IsEditMode)
                {
                    var dto = new ExhibitorUpdateDto
                    {
                        CompanyName = CompanyName,
                        Sector = Sector,
                        Nationality = Nationality,
                        ExhibitorCategory = ExhibitorCategory,
                        ContactPerson = ContactPerson,
                        Email = Email,
                        Phone = Phone,
                        LogoURL = LogoURL,
                        CompanyProfile = CompanyProfile,
                        IsActive = true
                    };

                    var result = await _exhibitorService.UpdateAsync(Session.TenantId, ExhibitorId, dto);
                    if (result.IsSuccess)
                    {
                        NotificationService.ShowSuccess("تم تحديث بيانات الشركة بنجاح ✓");
                        CloseAction?.Invoke();
                    }
                    else
                    {
                        NotificationService.ShowError(result.ErrorMessage ?? "فشل تحديث بيانات الشركة");
                    }
                }
                else
                {
                    var dto = new ExhibitorCreateDto
                    {
                        TenantID = Session.TenantId,
                        CompanyName = CompanyName,
                        Sector = Sector,
                        Nationality = Nationality,
                        ExhibitorCategory = ExhibitorCategory,
                        ContactPerson = ContactPerson,
                        Email = Email,
                        Phone = Phone,
                        LogoURL = LogoURL,
                        CompanyProfile = CompanyProfile
                    };

                    var result = await _exhibitorService.CreateAsync(Session.TenantId, dto);
                    if (result.IsSuccess)
                    {
                        NotificationService.ShowSuccess("تمت إضافة الشركة بنجاح ✓");
                        CloseAction?.Invoke();
                    }
                    else
                    {
                        NotificationService.ShowError(result.ErrorMessage ?? "فشل إضافة الشركة");
                    }
                }
            }, "خطأ أثناء حفظ بيانات الشركة");
        }

        [RelayCommand]
        private void Cancel()
        {
            CloseAction?.Invoke();
        }
    }
}
