using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ExhibitionManagementSystem.DeskTop.ViewModels.Financial;
using ExhibitionManagementSystem.DeskTop.Controls.Forms;
using ExhibitionManagementSystem.DeskTop.Controls.Dialogs;

namespace ExhibitionManagementSystem.DeskTop.Views.Financial;

public partial class InvoicesPage : UserControl
{
    public InvoicesViewModel ViewModel { get; }

    public InvoicesPage(InvoicesViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;

        Loaded += async (s, e) => await ViewModel.OnNavigatedToAsync();
    }

    private async void ViewDetails_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int invoiceId)
        {
            var detailVm = App.Services.GetRequiredService<InvoiceDetailViewModel>();
            await detailVm.InitializeAsync(invoiceId);

            var formCtrl = new InvoiceDetailControl { DataContext = detailVm };
            var dialog = new FormDialog(formCtrl, "تفاصيل الفاتورة")
            {
                Owner = Window.GetWindow(this)
            };

            detailVm.CloseAction = () => dialog.Close();
            dialog.ShowDialog();

            await ViewModel.LoadInvoicesCommand.ExecuteAsync(null);
        }
    }
}
