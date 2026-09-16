using System.Windows;
using System.Windows.Controls;
using ExhibitionManagementSystem.DeskTop.ViewModels.Booths;
using ExhibitionManagementSystem.Models.DTOs.Booth;

namespace ExhibitionManagementSystem.DeskTop.Views.Booths;

public partial class BoothsPage : UserControl
{
    public BoothsViewModel ViewModel { get; }

    public BoothsPage(BoothsViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;

        Loaded += async (s, e) => await ViewModel.OnNavigatedToAsync();
    }

    private void EditBooth_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is BoothDto booth)
        {
            ViewModel.EditBoothId = booth.BoothID;
            ViewModel.EditBoothNumber = booth.BoothNumber;
            ViewModel.EditAreaSqM = booth.OriginalAreaSqM;
            ViewModel.EditWidth = booth.Width;
            ViewModel.EditHeight = booth.Height;
            ViewModel.EditShapeType = booth.ShapeType ?? "Standard";
            ViewModel.EditStatus = booth.Status ?? "Available";
            ViewModel.EditAssignedPriceRuleId = booth.AssignedPriceRuleID ?? 0;

            var ctrl = new Controls.Forms.BoothFormControl { DataContext = ViewModel };
            var dialog = new Controls.Dialogs.FormDialog(ctrl, "تعديل بيانات الجناح") { Owner = Window.GetWindow(this) };
            ViewModel.EditCloseAction = () => dialog.Close();
            dialog.ShowDialog();
        }
    }

    private void AddBooth_Click(object sender, RoutedEventArgs e)
    {
        var formControl = new Controls.Forms.AddBoothFormControl { DataContext = ViewModel };
        var dialog = new Controls.Dialogs.FormDialog(formControl, "إضافة جناح جديد")
        {
            Owner = Window.GetWindow(this)
        };
        ViewModel.CloseAction = () => dialog.Close();
        dialog.ShowDialog();
    }
}
