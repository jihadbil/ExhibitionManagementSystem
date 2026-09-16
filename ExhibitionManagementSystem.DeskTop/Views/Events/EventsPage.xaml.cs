using System.Windows.Controls;
using ExhibitionManagementSystem.DeskTop.ViewModels.Events;

namespace ExhibitionManagementSystem.DeskTop.Views.Events;

public partial class EventsPage : UserControl
{
    public EventsViewModel ViewModel { get; }

    public EventsPage(EventsViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;

        Loaded += async (s, e) => await ViewModel.OnNavigatedToAsync();
    }

    private void AddEvent_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var formControl = new Controls.Forms.EventFormControl { DataContext = ViewModel };
        var dialog = new Controls.Dialogs.FormDialog(formControl, "إضافة فعالية جديدة")
        {
            Owner = System.Windows.Window.GetWindow(this)
        };
        ViewModel.CloseAction = () => dialog.Close();
        dialog.ShowDialog();
    }
}
