using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.DeskTop.Helpers;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Events;

public partial class EventsViewModel : ViewModelBase
{
    private readonly IExhibitionService _exhibitionService;

    // ━━━━━━━━━━━━━━ Collections ━━━━━━━━━━━━━━
    public ObservableCollection<ExhibitionScheduleDto> Events { get; } = [];
    public ObservableCollection<ExhibitionSummaryDto> Exhibitions { get; } = [];

    // ━━━━━━━━━━━━━━ Selection ━━━━━━━━━━━━━━
    [ObservableProperty]
    private int _selectedExhibitionId;

    // ━━━━━━━━━━━━━━ Form Fields for New Event ━━━━━━━━━━━━━━
    [ObservableProperty] private string _newEventName = string.Empty;
    [ObservableProperty] private string _newSpeakerName = string.Empty;
    [ObservableProperty] private DateTime _newStartDate = DateTime.Today;
    [ObservableProperty] private TimeSpan _newStartTime = new(10, 0, 0); // 10:00 AM
    [ObservableProperty] private DateTime _newEndDate = DateTime.Today;
    [ObservableProperty] private TimeSpan _newEndTime = new(11, 0, 0); // 11:00 AM
    [ObservableProperty] private int? _newMaxAttendees = 50;
    [ObservableProperty] private string _newDescription = string.Empty;
    [ObservableProperty] private bool _newIsPublic = true;
    [ObservableProperty] private string _newEventType = "Seminar"; // Seminar | Workshop | Keynote | Panel

    public ObservableCollection<string> EventTypes { get; } = new()
    {
        "Seminar", "Workshop", "Keynote", "Panel"
    };

    // ━━━━━━━━━━━━━━ Constructor ━━━━━━━━━━━━━━
    public EventsViewModel(
        IExhibitionService exhibitionService,
        INavigationService navigationService,
        INotificationService notificationService,
        SessionService session) : base(navigationService, notificationService, session)
    {
        _exhibitionService = exhibitionService;
        Title = "إدارة الفعاليات والمؤتمرات";
    }

    // ━━━━━━━━━━━━━━ Methods ━━━━━━━━━━━━━━
    public override async Task OnNavigatedToAsync()
    {
        await LoadExhibitionsAsync();
    }

    [RelayCommand]
    private async Task LoadExhibitionsAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            var result = await _exhibitionService.GetByTenantAsync(Session.TenantId, 1, 100);
            if (result.IsSuccess && result.Data is not null)
            {
                Exhibitions.Clear();
                foreach (var ex in result.Data.Items)
                {
                    Exhibitions.Add(ex);
                }

                if (Exhibitions.Count > 0 && SelectedExhibitionId == 0)
                {
                    SelectedExhibitionId = Exhibitions[0].ExhibitionID;
                }
            }
        }, "خطأ في تحميل المعارض");
    }

    async partial void OnSelectedExhibitionIdChanged(int value)
    {
        if (value > 0)
        {
            await LoadSchedulesAsync();
        }
        else
        {
            Events.Clear();
        }
    }

    [RelayCommand]
    private async Task LoadSchedulesAsync()
    {
        if (SelectedExhibitionId == 0) return;

        await ExecuteSafeAsync(async () =>
        {
            var result = await _exhibitionService.GetSchedulesAsync(Session.TenantId, SelectedExhibitionId);
            if (result.IsSuccess && result.Data is not null)
            {
                Events.Clear();
                foreach (var ev in result.Data)
                {
                    Events.Add(ev);
                }
            }
        }, "خطأ في تحميل جدول الفعاليات");
    }

    [RelayCommand]
    private async Task AddScheduleAsync()
    {
        if (SelectedExhibitionId == 0)
        {
            NotificationService.ShowError("الرجاء اختيار المعرض أولاً");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewEventName))
        {
            NotificationService.ShowError("الرجاء إدخال اسم الفعالية");
            return;
        }

        DateTime startDateTime = NewStartDate.Date.Add(NewStartTime);
        DateTime endDateTime = NewEndDate.Date.Add(NewEndTime);

        if (startDateTime > endDateTime)
        {
            NotificationService.ShowError("تاريخ البدء لا يمكن أن يكون بعد تاريخ الانتهاء");
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            var dto = new ExhibitionScheduleCreateDto
            {
                ExhibitionID = SelectedExhibitionId,
                EventName = NewEventName,
                SpeakerName = NewSpeakerName,
                StartDateTime = startDateTime,
                EndDateTime = endDateTime,
                MaxAttendees = NewMaxAttendees,
                Description = NewDescription,
                IsPublic = NewIsPublic,
                EventType = NewEventType
            };

            var result = await _exhibitionService.AddScheduleAsync(Session.TenantId, dto);
            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess("تمت إضافة الفعالية بنجاح ✓");
                NewEventName = string.Empty;
                NewSpeakerName = string.Empty;
                NewDescription = string.Empty;
                await LoadSchedulesAsync();
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل إضافة الفعالية");
            }
        }, "خطأ أثناء إضافة الفعالية");
    }

    [RelayCommand]
    private async Task DeleteScheduleAsync(int scheduleId)
    {
        var confirmResult = System.Windows.MessageBox.Show(
            "هل أنت متأكد من رغبتك في حذف هذه الفعالية؟",
            "تأكيد الحذف",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (confirmResult != System.Windows.MessageBoxResult.Yes) return;

        await ExecuteSafeAsync(async () =>
        {
            var result = await _exhibitionService.RemoveScheduleAsync(Session.TenantId, scheduleId);
            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess("تم حذف الفعالية بنجاح ✓");
                await LoadSchedulesAsync();
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل حذف الفعالية");
            }
        }, "خطأ أثناء حذف الفعالية");
    }
}
