using System.Windows.Controls;
using ExhibitionManagementSystem.DeskTop.ViewModels.Tenants;

namespace ExhibitionManagementSystem.DeskTop.Views.Tenants
{
    public partial class TenantsPage : UserControl
    {
        public TenantsViewModel ViewModel { get; }

        public TenantsPage(TenantsViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel;

            Loaded += async (s, e) => await ViewModel.OnNavigatedToAsync();
        }
    }
}
