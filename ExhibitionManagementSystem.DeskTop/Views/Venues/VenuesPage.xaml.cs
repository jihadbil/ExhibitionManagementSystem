using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ExhibitionManagementSystem.DeskTop.ViewModels.Venues;
using ExhibitionManagementSystem.Models.DTOs.Venue;
using ExhibitionManagementSystem.Models.DTOs.Hall;

namespace ExhibitionManagementSystem.DeskTop.Views.Venues;

public partial class VenuesPage : UserControl
{
    public VenuesViewModel ViewModel { get; }

    public VenuesPage(VenuesViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;

        Loaded += async (s, e) => await ViewModel.OnNavigatedToAsync();
    }

    private async void AddVenue_Click(object sender, RoutedEventArgs e)
    {
        var formViewModel = App.Services.GetRequiredService<VenueFormViewModel>();
        await formViewModel.InitializeAsync(0);

        var formControl = new Controls.Forms.VenueFormControl { DataContext = formViewModel };
        var dialog = new Controls.Dialogs.FormDialog(formControl, "إضافة موقع جديد")
        {
            Owner = Window.GetWindow(this)
        };
        formViewModel.CloseAction = () => dialog.Close();

        dialog.ShowDialog();
        await ViewModel.LoadVenuesCommand.ExecuteAsync(null);
    }

    private async void EditVenue_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int venueId)
        {
            var formViewModel = App.Services.GetRequiredService<VenueFormViewModel>();
            await formViewModel.InitializeAsync(venueId);

            var formControl = new Controls.Forms.VenueFormControl { DataContext = formViewModel };
            var dialog = new Controls.Dialogs.FormDialog(formControl, "تعديل بيانات الموقع")
            {
                Owner = Window.GetWindow(this)
            };
            formViewModel.CloseAction = () => dialog.Close();

            dialog.ShowDialog();
            await ViewModel.LoadVenuesCommand.ExecuteAsync(null);
        }
    }

    private async void DeleteVenue_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is VenueDto venue)
        {
            var result = MessageBox.Show(
                Window.GetWindow(this),
                $"هل أنت متأكد من رغبتك في حذف موقع \"{venue.Name}\"؟ سيؤدي ذلك لحذف جميع القاعات والأجنحة التابعة له.",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No,
                MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

            if (result == MessageBoxResult.Yes)
            {
                await ViewModel.DeleteVenueCommand.ExecuteAsync(venue.VenueID);
            }
        }
    }

    private async void AddHall_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedVenue is null) return;

        var formViewModel = App.Services.GetRequiredService<HallFormViewModel>();
        await formViewModel.InitializeAsync(ViewModel.SelectedVenue.VenueID, 0);

        var formControl = new Controls.Forms.HallFormControl { DataContext = formViewModel };
        var dialog = new Controls.Dialogs.FormDialog(formControl, "إضافة قاعة جديدة")
        {
            Owner = Window.GetWindow(this)
        };
        formViewModel.CloseAction = () => dialog.Close();

        dialog.ShowDialog();
        // Reload venues to update halls count and reload current venue's halls
        await ViewModel.LoadVenuesCommand.ExecuteAsync(null);
    }

    private async void EditHall_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedVenue is null) return;

        if (sender is Button button && button.Tag is int hallId)
        {
            var formViewModel = App.Services.GetRequiredService<HallFormViewModel>();
            await formViewModel.InitializeAsync(ViewModel.SelectedVenue.VenueID, hallId);

            var formControl = new Controls.Forms.HallFormControl { DataContext = formViewModel };
            var dialog = new Controls.Dialogs.FormDialog(formControl, "تعديل بيانات القاعة")
            {
                Owner = Window.GetWindow(this)
            };
            formViewModel.CloseAction = () => dialog.Close();

            dialog.ShowDialog();
            await ViewModel.LoadVenuesCommand.ExecuteAsync(null);
        }
    }

    private async void DeleteHall_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is HallDto hall)
        {
            var result = MessageBox.Show(
                Window.GetWindow(this),
                $"هل أنت متأكد من رغبتك في حذف القاعة \"{hall.HallName}\"؟",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No,
                MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

            if (result == MessageBoxResult.Yes)
            {
                await ViewModel.DeleteHallCommand.ExecuteAsync(hall.HallID);
            }
        }
    }
}
