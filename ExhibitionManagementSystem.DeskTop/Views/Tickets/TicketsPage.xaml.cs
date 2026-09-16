using System.Windows.Controls;
using ExhibitionManagementSystem.DeskTop.ViewModels.Tickets;

namespace ExhibitionManagementSystem.DeskTop.Views.Tickets;

public partial class TicketsPage : UserControl
{
    public TicketsViewModel ViewModel { get; }

    public TicketsPage(TicketsViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;

        Loaded += async (s, e) => await ViewModel.OnNavigatedToAsync();
    }

    private void RegisterVisitor_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var formControl = new Controls.Forms.VisitorFormControl { DataContext = ViewModel };
        var dialog = new Controls.Dialogs.FormDialog(formControl, "تسجيل زائر جديد")
        {
            Owner = System.Windows.Window.GetWindow(this)
        };
        ViewModel.CloseVisitorAction = () => dialog.Close();
        dialog.ShowDialog();
    }

    private void IssueTicket_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var formControl = new Controls.Forms.IssueTicketFormControl { DataContext = ViewModel };
        var dialog = new Controls.Dialogs.FormDialog(formControl, "إصدار تذكرة جديدة")
        {
            Owner = System.Windows.Window.GetWindow(this)
        };
        ViewModel.CloseIssueTicketAction = () => dialog.Close();
        dialog.ShowDialog();
    }

    private void ScanTicket_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var formControl = new Controls.Forms.ScanTicketFormControl { DataContext = ViewModel };
        var dialog = new Controls.Dialogs.FormDialog(formControl, "تحقق ومسح تذكرة")
        {
            Owner = System.Windows.Window.GetWindow(this)
        };
        ViewModel.CloseScanTicketAction = () => dialog.Close();
        dialog.ShowDialog();
    }
}
