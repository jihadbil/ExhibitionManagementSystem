using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ExhibitionManagementSystem.DeskTop.Controls.Dialogs;
using ExhibitionManagementSystem.DeskTop.Controls.Forms;
using ExhibitionManagementSystem.DeskTop.ViewModels.Sponsorship;
using ExhibitionManagementSystem.Models.DTOs.Sponsorship;

namespace ExhibitionManagementSystem.DeskTop.Views.Sponsorship;

public partial class SponsorshipPage : UserControl
{
    private readonly SponsorshipViewModel _viewModel;

    public SponsorshipPage(SponsorshipViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.OnNavigatedToAsync();
    }

    private void AddSponsor_Click(object sender, RoutedEventArgs e)
    {
        var formVm = App.Services.GetRequiredService<SponsorFormViewModel>();
        formVm.LoadForCreate();

        var formControl = new SponsorFormControl { DataContext = formVm };
        var dialog = new FormDialog(formControl, "إضافة راعي جديد");

        formVm.Saved += () =>
        {
            dialog.DialogResult = true;
            dialog.Close();
            _ = _viewModel.LoadAllDataAsync();
        };

        dialog.ShowDialog();
    }

    private void EditSponsor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SponsorDto sponsor })
        {
            var formVm = App.Services.GetRequiredService<SponsorFormViewModel>();
            formVm.LoadForEdit(sponsor);

            var formControl = new SponsorFormControl { DataContext = formVm };
            var dialog = new FormDialog(formControl, "تعديل بيانات الراعي");

            formVm.Saved += () =>
            {
                dialog.DialogResult = true;
                dialog.Close();
                _ = _viewModel.LoadAllDataAsync();
            };

            dialog.ShowDialog();
        }
    }

    private void AddPackage_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedExhibitionId == 0)
        {
            MessageBox.Show("يرجى اختيار المعرض أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var formVm = App.Services.GetRequiredService<SponsorshipPackageFormViewModel>();
        formVm.LoadForCreate(_viewModel.SelectedExhibitionId);

        var formControl = new SponsorshipPackageFormControl { DataContext = formVm };
        var dialog = new FormDialog(formControl, "إضافة باقة رعاية جديدة");

        formVm.Saved += () =>
        {
            dialog.DialogResult = true;
            dialog.Close();
            _ = _viewModel.LoadAllDataAsync();
        };

        dialog.ShowDialog();
    }

    private void EditPackage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SponsorshipPackageDto pkg })
        {
            var formVm = App.Services.GetRequiredService<SponsorshipPackageFormViewModel>();
            formVm.LoadForEdit(pkg);

            var formControl = new SponsorshipPackageFormControl { DataContext = formVm };
            var dialog = new FormDialog(formControl, "تعديل باقة الرعاية");

            formVm.Saved += () =>
            {
                dialog.DialogResult = true;
                dialog.Close();
                _ = _viewModel.LoadAllDataAsync();
            };

            dialog.ShowDialog();
        }
    }

    private void AddSpace_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedExhibitionId == 0)
        {
            MessageBox.Show("يرجى اختيار المعرض أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var formVm = App.Services.GetRequiredService<AdvertisingSpaceFormViewModel>();
        formVm.LoadForCreate(_viewModel.SelectedExhibitionId);

        var formControl = new AdvertisingSpaceFormControl { DataContext = formVm };
        var dialog = new FormDialog(formControl, "إضافة مساحة إعلانية جديدة");

        formVm.Saved += () =>
        {
            dialog.DialogResult = true;
            dialog.Close();
            _ = _viewModel.LoadAllDataAsync();
        };

        dialog.ShowDialog();
    }

    private void EditSpace_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: AdvertisingSpaceDto space })
        {
            var formVm = App.Services.GetRequiredService<AdvertisingSpaceFormViewModel>();
            formVm.LoadForEdit(space);

            var formControl = new AdvertisingSpaceFormControl { DataContext = formVm };
            var dialog = new FormDialog(formControl, "تعديل المساحة الإعلانية");

            formVm.Saved += () =>
            {
                dialog.DialogResult = true;
                dialog.Close();
                _ = _viewModel.LoadAllDataAsync();
            };

            dialog.ShowDialog();
        }
    }

    private async void AddContract_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedExhibitionId == 0)
        {
            MessageBox.Show("يرجى اختيار المعرض أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var formVm = App.Services.GetRequiredService<SponsorshipContractFormViewModel>();
        await formVm.LoadAsync(_viewModel.SelectedExhibitionId);

        var formControl = new SponsorshipContractFormControl { DataContext = formVm };
        var dialog = new FormDialog(formControl, "توثيق عقد رعاية جديد");

        formVm.Saved += () =>
        {
            dialog.DialogResult = true;
            dialog.Close();
            _ = _viewModel.LoadAllDataAsync();
        };

        dialog.ShowDialog();
    }
}
