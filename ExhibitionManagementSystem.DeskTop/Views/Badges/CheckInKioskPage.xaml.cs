using System.Windows.Controls;
using System.Windows.Input;
using ExhibitionManagementSystem.DeskTop.ViewModels.Badges;

namespace ExhibitionManagementSystem.DeskTop.Views.Badges;

public partial class CheckInKioskPage : UserControl
{
    private readonly CheckInKioskViewModel _viewModel;

    public CheckInKioskPage(CheckInKioskViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.OnNavigatedToAsync();
    }

    private void ScanTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel.ProcessScanCommand.Execute(null);
        }
    }
}
