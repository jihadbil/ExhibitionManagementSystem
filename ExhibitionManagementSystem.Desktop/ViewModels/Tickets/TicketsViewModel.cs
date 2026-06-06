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
using ExhibitionManagementSystem.Models.DTOs.Visitor;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.Desktop.Services.Auth;
using ExhibitionManagementSystem.Desktop.Services.Dialog;
using Microsoft.Extensions.DependencyInjection;

namespace ExhibitionManagementSystem.Desktop.ViewModels.Tickets
{
    public partial class TicketsViewModel : BaseViewModel
    {
        private readonly ITicketService _ticketService;
        private readonly IVisitorService _visitorService;
        private readonly IExhibitionService _exhibitionService;
        private readonly ISessionService _sessionService;
        private readonly IDialogService _dialogService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _selectedStatus = "الكل";

        [ObservableProperty]
        private ObservableCollection<TicketDto> _tickets = new();

        [ObservableProperty]
        private ObservableCollection<TicketDto> _filteredTickets = new();

        [ObservableProperty]
        private int _totalTickets;

        [ObservableProperty]
        private int _activeTickets;

        [ObservableProperty]
        private int _usedTickets;

        [ObservableProperty]
        private int _cancelledTickets;

        public List<string> Statuses { get; } = new() { "الكل", "Active", "Used", "Cancelled" };

        public TicketsViewModel(
            INavigationService navigationService,
            INotificationService notificationService,
            ITicketService ticketService,
            IVisitorService visitorService,
            IExhibitionService exhibitionService,
            ISessionService sessionService,
            IDialogService dialogService,
            IServiceProvider serviceProvider)
            : base(navigationService, notificationService)
        {
            _ticketService = ticketService;
            _visitorService = visitorService;
            _exhibitionService = exhibitionService;
            _sessionService = sessionService;
            _dialogService = dialogService;
            _serviceProvider = serviceProvider;
            Title = "إدارة التذاكر";
        }

        public override async Task InitializeAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                var tenantId = _sessionService.TenantId;
                var activeResult = await _exhibitionService.GetActiveAsync(tenantId);
                
                Tickets.Clear();
                if (activeResult.IsSuccess && activeResult.Data != null)
                {
                    foreach (var expo in activeResult.Data)
                    {
                        var ticketsResult = await _ticketService.GetByExhibitionAsync(tenantId, expo.ExhibitionID);
                        if (ticketsResult.IsSuccess && ticketsResult.Data != null)
                        {
                            foreach (var ticket in ticketsResult.Data)
                            {
                                Tickets.Add(ticket);
                            }
                        }
                    }
                }

                CalculateStats();
                FilterTickets();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"فشل تحميل التذاكر: {ex.Message}";
                NotificationService.ShowError(ErrorMessage);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CalculateStats()
        {
            TotalTickets = Tickets.Count;
            ActiveTickets = Tickets.Count(t => t.Status == "Active");
            UsedTickets = Tickets.Count(t => t.Status == "Used");
            CancelledTickets = Tickets.Count(t => t.Status == "Cancelled");
        }

        partial void OnSearchTextChanged(string value) => FilterTickets();
        partial void OnSelectedStatusChanged(string value) => FilterTickets();

        private void FilterTickets()
        {
            var query = SearchText.Trim();
            var statusFilter = SelectedStatus;

            var filtered = Tickets.Where(t =>
            {
                bool matchesSearch = string.IsNullOrWhiteSpace(query) ||
                                     t.TicketID.ToString().Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                     (t.VisitorName != null && t.VisitorName.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                                     (t.ExhibitionName != null && t.ExhibitionName.Contains(query, StringComparison.OrdinalIgnoreCase));

                bool matchesStatus = statusFilter == "الكل" || t.Status == statusFilter;

                return matchesSearch && matchesStatus;
            });

            FilteredTickets = new ObservableCollection<TicketDto>(filtered);
        }

        [RelayCommand]
        private async Task AddTicketAsync()
        {
            var tenantId = _sessionService.TenantId;
            var dialogVm = _serviceProvider.GetRequiredService<AddTicketDialogViewModel>();

            await dialogVm.LoadActiveExhibitionsAsync(tenantId);

            var result = _dialogService.ShowDialog(dialogVm);
            if (result == true)
            {
                // Find or register visitor
                var visitorId = 0;
                var searchResult = await _visitorService.SearchAsync(tenantId, dialogVm.Email);
                if (searchResult.IsSuccess && searchResult.Data != null)
                {
                    var existingVisitor = searchResult.Data.FirstOrDefault(v => v.Email.Equals(dialogVm.Email, StringComparison.OrdinalIgnoreCase));
                    if (existingVisitor != null)
                    {
                        visitorId = existingVisitor.VisitorID;
                    }
                }

                if (visitorId == 0)
                {
                    var newVisitorResult = await _visitorService.RegisterAsync(tenantId, new VisitorCreateDto
                    {
                        TenantID = tenantId,
                        FullName = dialogVm.FullName,
                        Email = dialogVm.Email,
                        Phone = dialogVm.Phone,
                        Nationality = dialogVm.Nationality,
                        VisitorType = dialogVm.VisitorType
                    });
                    if (newVisitorResult.IsSuccess && newVisitorResult.Data != null)
                    {
                        visitorId = newVisitorResult.Data.VisitorID;
                    }
                }

                if (visitorId == 0)
                {
                    NotificationService.ShowError("لا يمكن إصدار تذكرة بدون زائر صالح.");
                    return;
                }

                var createDto = new TicketCreateDto
                {
                    VisitorID = visitorId,
                    ExhibitionID = dialogVm.SelectedExhibition?.ExhibitionID ?? 0,
                    TicketType = dialogVm.TicketType,
                    Price = dialogVm.Price,
                    CurrencyCode = dialogVm.CurrencyCode,
                    ValidDate = dialogVm.ValidDate
                };

                var issueResult = await ExecuteServiceAsync(() => _ticketService.IssueTicketAsync(tenantId, createDto), "فشل إصدار التذكرة");
                if (issueResult != null)
                {
                    Tickets.Insert(0, issueResult);
                    CalculateStats();
                    FilterTickets();
                    NotificationService.ShowSuccess("تم إصدار التذكرة الجديدة بنجاح!");
                }
            }
        }

        [RelayCommand]
        private async Task ScanQRAsync()
        {
            if (!Tickets.Any())
            {
                NotificationService.ShowWarning("لا توجد تذاكر متوفرة لمسحها.");
                return;
            }

            var tenantId = _sessionService.TenantId;
            var ticket = Tickets.First();
            var qrCode = ticket.QRCode;
            var userId = _sessionService.UserId;

            NotificationService.ShowInfo("جاري مسح رمز الاستجابة السريعة...");

            var result = await ExecuteServiceAsync(() => 
                _ticketService.ScanTicketAsync(tenantId, qrCode, "In", "البوابة الرئيسية", userId), 
                "فشل مسح التذكرة");

            if (result != null)
            {
                NotificationService.ShowSuccess($"تم مسح التذكرة بنجاح! الاسم: {ticket.VisitorName}");
                await InitializeAsync();
            }
        }

        [RelayCommand]
        private void CancelTicket(object? item)
        {
            if (item is TicketDto ticket)
            {
                ticket.Status = "Cancelled";
                CalculateStats();
                FilterTickets();
                NotificationService.ShowSuccess($"تم إلغاء التذكرة {ticket.TicketID} بنجاح.");
            }
        }

        [RelayCommand]
        private async Task ViewScanHistoryAsync(object? item)
        {
            if (item is not TicketDto ticket) return;
            var tenantId = _sessionService.TenantId;
            var result = await _ticketService.GetScanHistoryAsync(tenantId, ticket.TicketID);
            if (result.IsSuccess && result.Data != null)
            {
                NotificationService.ShowInfo($"عدد مرات المسح للتذكرة {ticket.TicketID}: {result.Data.Count}");
            }
        }
    }
}
