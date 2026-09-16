using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ExhibitionManagementSystem.DeskTop.ViewModels.Reservations;
using ExhibitionManagementSystem.DeskTop.Controls.Forms;
using ExhibitionManagementSystem.DeskTop.Controls.Dialogs;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;
using ExhibitionManagementSystem.DeskTop.ViewModels.Financial;

namespace ExhibitionManagementSystem.DeskTop.Views.Reservations;

public partial class ReservationsPage : UserControl
{
    public ReservationsViewModel ViewModel { get; }

    public ReservationsPage(ReservationsViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;

        Loaded += async (s, e) => await ViewModel.OnNavigatedToAsync();
    }

    private async void AddReservation_Click(object sender, RoutedEventArgs e)
    {
        var formVm = App.Services.GetRequiredService<ReservationFormViewModel>();
        await formVm.InitializeAsync();

        var formCtrl = new ReservationFormControl { DataContext = formVm };
        var dialog = new FormDialog(formCtrl, "حجز جناح جديد")
        {
            Owner = Window.GetWindow(this)
        };

        formVm.CloseAction = () => dialog.Close();
        dialog.ShowDialog();

        await ViewModel.LoadReservationsCommand.ExecuteAsync(null);
    }

    private async void Approve_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int reservationId)
        {
            await ViewModel.ApproveCommand.ExecuteAsync(reservationId);
        }
    }

    private async void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int reservationId)
        {
            await ViewModel.CancelCommand.ExecuteAsync(reservationId);
        }
    }

    private async void ViewInvoice_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int reservationId)
        {
            var financialService = App.Services.GetRequiredService<IFinancialService>();
            var session = App.Services.GetRequiredService<SessionService>();
            var notificationService = App.Services.GetRequiredService<INotificationService>();

            // 1. Try to get the existing invoice for this reservation
            var result = await financialService.GetInvoiceByReservationAsync(session.TenantId, reservationId);
            if (result.IsSuccess && result.Data != null)
            {
                // Invoice exists, show detail dialog
                var detailVm = App.Services.GetRequiredService<InvoiceDetailViewModel>();
                await detailVm.InitializeAsync(result.Data.InvoiceID);

                var detailCtrl = new InvoiceDetailControl { DataContext = detailVm };
                var dialog = new FormDialog(detailCtrl, "تفاصيل الفاتورة")
                {
                    Owner = Window.GetWindow(this)
                };

                detailVm.CloseAction = () => dialog.Close();
                dialog.ShowDialog();
            }
            else
            {
                // Invoice does not exist, check if reservation is approved (Confirmed)
                var reservationService = App.Services.GetRequiredService<IReservationService>();
                var resResult = await reservationService.GetByIdAsync(session.TenantId, reservationId);
                
                if (resResult.IsSuccess && resResult.Data != null)
                {
                    if (resResult.Data.Status != "Confirmed" && resResult.Data.Status != "Approved")
                    {
                        notificationService.ShowError("لا يمكن توليد فاتورة لحجز غير مؤكد.");
                        return;
                    }

                    // Ask user to generate
                    var confirm = MessageBox.Show(
                        "لا توجد فاتورة لهذا الحجز بعد. هل تريد توليد فاتورة له الآن؟",
                        "توليد فاتورة",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question,
                        MessageBoxResult.Yes,
                        MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

                    if (confirm == MessageBoxResult.Yes)
                    {
                        var genResult = await financialService.GenerateInvoiceForReservationAsync(session.TenantId, reservationId);
                        if (genResult.IsSuccess && genResult.Data != null)
                        {
                            notificationService.ShowSuccess("تم إنشاء الفاتورة بنجاح ✓");
                            
                            // Show the created invoice detail dialog
                            var detailVm = App.Services.GetRequiredService<InvoiceDetailViewModel>();
                            await detailVm.InitializeAsync(genResult.Data.InvoiceID);

                            var detailCtrl = new InvoiceDetailControl { DataContext = detailVm };
                            var dialog = new FormDialog(detailCtrl, "تفاصيل الفاتورة")
                            {
                                Owner = Window.GetWindow(this)
                            };

                            detailVm.CloseAction = () => dialog.Close();
                            dialog.ShowDialog();
                        }
                        else
                        {
                            notificationService.ShowError(genResult.ErrorMessage ?? "فشل إنشاء الفاتورة");
                        }
                    }
                }
                else
                {
                    notificationService.ShowError("فشل تحميل بيانات الحجز.");
                }
            }
        }
    }
}
