using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
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

    private async void AddCompany_Click(object sender, RoutedEventArgs e)
    {
        var formVm = App.Services.GetRequiredService<ExhibitorFormViewModel>();
        await formVm.InitializeAsync(0);
        var ctrl = new Controls.Forms.ExhibitorFormControl { DataContext = formVm };
        var dialog = new Controls.Dialogs.FormDialog(ctrl, "إضافة شركة عارضة") { Owner = Window.GetWindow(this) };
        formVm.CloseAction = () => dialog.Close();
        dialog.ShowDialog();
        await ViewModel.LoadCompaniesCommand.ExecuteAsync(null);
    }

    private async void EditCompany_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int id)
        {
            var formVm = App.Services.GetRequiredService<ExhibitorFormViewModel>();
            await formVm.InitializeAsync(id);
            var ctrl = new Controls.Forms.ExhibitorFormControl { DataContext = formVm };
            var dialog = new Controls.Dialogs.FormDialog(ctrl, "تعديل بيانات الشركة") { Owner = Window.GetWindow(this) };
            formVm.CloseAction = () => dialog.Close();
            dialog.ShowDialog();
            await ViewModel.LoadCompaniesCommand.ExecuteAsync(null);
        }
    }
}
