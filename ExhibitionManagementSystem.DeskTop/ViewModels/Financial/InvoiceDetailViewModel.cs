using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.DeskTop.Helpers;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Financial;

public partial class InvoiceDetailViewModel : ViewModelBase
{
    private readonly IFinancialService _financialService;
    private readonly IInvoiceItemService _invoiceItemService;

    // ━━━━━━━━━━━━━━ Collections ━━━━━━━━━━━━━━

    public ObservableCollection<InvoiceItemDto> InvoiceItems { get; } = [];
    public ObservableCollection<PaymentDto> Payments { get; } = [];
    public ObservableCollection<string> PaymentMethods { get; } = new() { "Cash", "BankTransfer", "Check" };

    // ━━━━━━━━━━━━━━ Properties ━━━━━━━━━━━━━━

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PaidAmount))]
    [NotifyPropertyChangedFor(nameof(RemainingAmount))]
    private InvoiceDto? _invoice;

    [ObservableProperty]
    private decimal _newPaymentAmount;

    [ObservableProperty]
    private string _newPaymentMethod = "Cash";

    [ObservableProperty]
    private string _newPaymentNotes = string.Empty;

    [ObservableProperty]
    private string _newPaymentReference = string.Empty;

    public Action? CloseAction { get; set; }

    // Calculated properties
    public decimal PaidAmount => Payments.Where(p => p.Status.Equals("Success", StringComparison.OrdinalIgnoreCase) || p.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase)).Sum(p => p.Amount);
    public decimal RemainingAmount => (Invoice?.TotalAmount ?? 0) - PaidAmount;

    // ━━━━━━━━━━━━━━ Constructor ━━━━━━━━━━━━━━

    public InvoiceDetailViewModel(
        IFinancialService financialService,
        IInvoiceItemService invoiceItemService,
        INavigationService navigationService,
        INotificationService notificationService,
        SessionService session) : base(navigationService, notificationService, session)
    {
        _financialService = financialService;
        _invoiceItemService = invoiceItemService;
        Title = "تفاصيل الفاتورة";
    }

    // ━━━━━━━━━━━━━━ Methods & Commands ━━━━━━━━━━━━━━

    public async Task InitializeAsync(int invoiceId)
    {
        await ExecuteSafeAsync(async () =>
        {
            var result = await _financialService.GetInvoiceByIdAsync(Session.TenantId, invoiceId);
            if (result.IsSuccess && result.Data is not null)
            {
                Invoice = result.Data;

                InvoiceItems.Clear();
                if (Invoice.Items != null)
                {
                    foreach (var item in Invoice.Items)
                    {
                        InvoiceItems.Add(item);
                    }
                }

                var paymentsResult = await _financialService.GetPaymentsByInvoiceAsync(Session.TenantId, invoiceId);
                if (paymentsResult.IsSuccess && paymentsResult.Data is not null)
                {
                    Payments.Clear();
                    foreach (var payment in paymentsResult.Data)
                    {
                        Payments.Add(payment);
                    }
                }

                // Trigger UI updates for calculated properties
                OnPropertyChanged(nameof(PaidAmount));
                OnPropertyChanged(nameof(RemainingAmount));
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل تحميل تفاصيل الفاتورة");
            }
        }, "خطأ في تحميل تفاصيل الفاتورة");
    }

    [RelayCommand]
    private async Task RecordPaymentAsync()
    {
        if (Invoice == null) return;

        if (NewPaymentAmount <= 0)
        {
            NotificationService.ShowError("الرجاء إدخال مبلغ صحيح");
            return;
        }

        if (NewPaymentAmount > RemainingAmount)
        {
            NotificationService.ShowError("مبلغ الدفعة أكبر من المبلغ المتبقي المستحق");
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            var dto = new PaymentCreateDto
            {
                InvoiceID = Invoice.InvoiceID,
                Amount = NewPaymentAmount,
                Method = NewPaymentMethod,
                ReferenceNo = NewPaymentReference,
                Notes = NewPaymentNotes,
                CurrencyCode = Invoice.CurrencyCode
            };

            var result = await _financialService.RecordPaymentAsync(Session.TenantId, Session.UserId, dto);
            if (result.IsSuccess)
            {
                NotificationService.ShowSuccess("تم تسجيل الدفعة بنجاح ✓");

                // Reset inputs
                NewPaymentAmount = 0;
                NewPaymentNotes = string.Empty;
                NewPaymentReference = string.Empty;

                // Reload data
                await InitializeAsync(Invoice.InvoiceID);
            }
            else
            {
                NotificationService.ShowError(result.ErrorMessage ?? "فشل تسجيل الدفعة");
            }
        }, "خطأ أثناء تسجيل الدفعة");
    }

    [RelayCommand]
    private void Close()
    {
        CloseAction?.Invoke();
    }
}
