using System.Windows.Controls;
using ExhibitionManagementSystem.DeskTop.ViewModels.Admin;

namespace ExhibitionManagementSystem.DeskTop.Views.Admin
{
    public partial class AdminPage : UserControl
    {
        public AdminViewModel ViewModel { get; }

        public AdminPage(AdminViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel;

            Loaded += async (s, e) => await ViewModel.OnNavigatedToAsync();
        }
    }
}
