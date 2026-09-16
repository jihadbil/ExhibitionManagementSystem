using System.Windows.Controls;
using ExhibitionManagementSystem.DeskTop.ViewModels.Badges;

namespace ExhibitionManagementSystem.DeskTop.Views.Badges;

public partial class BadgeDesignerPage : UserControl
{
    public BadgeDesignerPage(BadgeDesignerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.OnNavigatedToAsync();
    }
}
