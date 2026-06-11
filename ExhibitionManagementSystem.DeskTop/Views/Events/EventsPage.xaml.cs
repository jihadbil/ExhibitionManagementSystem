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
}
