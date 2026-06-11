using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ExhibitionManagementSystem.DeskTop.ViewModels.Exhibitions;

namespace ExhibitionManagementSystem.DeskTop.Views.Exhibitions;

public partial class ExhibitionsPage : UserControl
{
    public ExhibitionsViewModel ViewModel { get; }

    public ExhibitionsPage(ExhibitionsViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;

        Loaded += async (s, e) => await ViewModel.OnNavigatedToAsync();
    }

    private async void AddExhibition_Click(object sender, RoutedEventArgs e)
    {
        var formViewModel = App.Services.GetRequiredService<ExhibitionFormViewModel>();
        await formViewModel.InitializeAsync(0);

        var formControl = new Controls.Forms.ExhibitionFormControl { DataContext = formViewModel };
        var dialog = new Controls.Dialogs.FormDialog(formControl, "إضافة معرض جديد")
        {
            Owner = Window.GetWindow(this)
        };
        formViewModel.CloseAction = () => dialog.Close();

        dialog.ShowDialog();
        await ViewModel.LoadExhibitionsCommand.ExecuteAsync(null);
    }

    private async void Card_EditRequested(object sender, int exhibitionId)
    {
        var formViewModel = App.Services.GetRequiredService<ExhibitionFormViewModel>();
        await formViewModel.InitializeAsync(exhibitionId);

        var formControl = new Controls.Forms.ExhibitionFormControl { DataContext = formViewModel };
        var dialog = new Controls.Dialogs.FormDialog(formControl, "تعديل المعرض")
        {
            Owner = Window.GetWindow(this)
        };
        formViewModel.CloseAction = () => dialog.Close();

        dialog.ShowDialog();
        await ViewModel.LoadExhibitionsCommand.ExecuteAsync(null);
    }

    private async void Card_DeleteRequested(object sender, int exhibitionId)
    {
        var result = MessageBox.Show(
            Window.GetWindow(this),
            "هل أنت متأكد من رغبتك في حذف هذا المعرض؟ سيؤدي ذلك لحذف جميع البيانات المرتبطة به.",
            "تأكيد الحذف",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No,
            MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

        if (result == MessageBoxResult.Yes)
        {
            await ViewModel.DeleteExhibitionCommand.ExecuteAsync(exhibitionId);
        }
    }
}
