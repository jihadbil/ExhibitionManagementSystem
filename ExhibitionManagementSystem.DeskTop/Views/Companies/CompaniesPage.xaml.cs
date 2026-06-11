using System.Windows.Controls;
using ExhibitionManagementSystem.DeskTop.ViewModels.Companies;

namespace ExhibitionManagementSystem.DeskTop.Views.Companies;

public partial class CompaniesPage : UserControl
{
    public CompaniesViewModel ViewModel { get; }

    public CompaniesPage(CompaniesViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;

        Loaded += async (s, e) => await ViewModel.OnNavigatedToAsync();
    }
}
