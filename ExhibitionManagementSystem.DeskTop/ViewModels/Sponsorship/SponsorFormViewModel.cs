using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.DeskTop.Helpers;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using ExhibitionManagementSystem.Models.DTOs.Sponsorship;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Sponsorship;

public partial class SponsorFormViewModel : ViewModelBase
{
    private readonly ISponsorshipService _sponsorshipService;

    [ObservableProperty] private int _sponsorId;
    [ObservableProperty] private string _companyName = string.Empty;
    [ObservableProperty] private string? _contactPerson;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _websiteUrl;
    [ObservableProperty] private string? _logoUrl;
    [ObservableProperty] private string? _companyProfile;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private bool _isEditMode;

    public event Action? Saved;

    public SponsorFormViewModel(
        ISponsorshipService sponsorshipService,
        INavigationService navigationService,
        INotificationService notificationService,
        SessionService session)
        : base(navigationService, notificationService, session)
    {
        _sponsorshipService = sponsorshipService;
        Title = "بيانات الراعي";
    }

    public void LoadForCreate()
    {
        IsEditMode = false;
        SponsorId = 0;
        CompanyName = string.Empty;
        ContactPerson = string.Empty;
        Phone = string.Empty;
        Email = string.Empty;
        WebsiteUrl = string.Empty;
        LogoUrl = string.Empty;
        CompanyProfile = string.Empty;
        IsActive = true;
        Title = "إضافة راعي جديد";
    }

    public void LoadForEdit(SponsorDto sponsor)
    {
        IsEditMode = true;
        SponsorId = sponsor.SponsorID;
        CompanyName = sponsor.CompanyName;
        ContactPerson = sponsor.ContactPerson;
        Phone = sponsor.Phone;
        Email = sponsor.Email;
        WebsiteUrl = sponsor.WebsiteUrl;
        LogoUrl = sponsor.LogoUrl;
        CompanyProfile = sponsor.CompanyProfile;
        IsActive = sponsor.IsActive;
        Title = "تعديل بيانات الراعي";
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(CompanyName))
        {
            NotificationService.ShowWarning("اسم الشركة الراعية مطلوب", "تنبيه");
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            if (IsEditMode)
            {
                var dto = new SponsorUpdateDto
                {
                    CompanyName = CompanyName,
                    ContactPerson = ContactPerson,
                    Phone = Phone,
                    Email = Email,
                    WebsiteUrl = WebsiteUrl,
                    LogoUrl = LogoUrl,
                    CompanyProfile = CompanyProfile,
                    IsActive = IsActive
                };

                var result = await _sponsorshipService.UpdateSponsorAsync(Session.TenantId, SponsorId, dto);
                if (result.IsSuccess)
                {
                    NotificationService.ShowSuccess("تم تحديث بيانات الراعي بنجاح", "تم الحفظ");
                    Saved?.Invoke();
                }
                else
                {
                    NotificationService.ShowError(result.ErrorMessage ?? "فشل حفظ البيانات", "خطأ");
                }
            }
            else
            {
                var dto = new SponsorCreateDto
                {
                    CompanyName = CompanyName,
                    ContactPerson = ContactPerson,
                    Phone = Phone,
                    Email = Email,
                    WebsiteUrl = WebsiteUrl,
                    LogoUrl = LogoUrl,
                    CompanyProfile = CompanyProfile,
                    IsActive = IsActive
                };

                var result = await _sponsorshipService.CreateSponsorAsync(Session.TenantId, dto);
                if (result.IsSuccess)
                {
                    NotificationService.ShowSuccess("تمت إضافة الراعي بنجاح", "تم الحفظ");
                    Saved?.Invoke();
                }
                else
                {
                    NotificationService.ShowError(result.ErrorMessage ?? "فشل حفظ البيانات", "خطأ");
                }
            }
        });
    }
}
