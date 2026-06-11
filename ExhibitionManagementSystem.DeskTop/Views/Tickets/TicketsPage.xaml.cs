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
}
