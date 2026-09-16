using System.Windows;
using System.Windows.Controls;
using ExhibitionManagementSystem.DeskTop.ViewModels.ServiceMgmt;

namespace ExhibitionManagementSystem.DeskTop.Views.Services
{
    public partial class ServicesPage : UserControl
    {
        public ServicesViewModel ViewModel { get; }

        public ServicesPage(ServicesViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel;

            Loaded += async (s, e) => await ViewModel.OnNavigatedToAsync();
        }

        private void ClearPricingExhibition_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectedExhibitionIdForPricing = 0;
        }

        private void AddService_Click(object sender, RoutedEventArgs e)
        {
            var formControl = new Controls.Forms.ServiceFormControl { DataContext = ViewModel };
            var dialog = new Controls.Dialogs.FormDialog(formControl, "إضافة خدمة جديدة")
            {
                Owner = Window.GetWindow(this)
            };
            ViewModel.CloseServiceAction = () => dialog.Close();
            dialog.ShowDialog();
        }

        private void AddPriceRule_Click(object sender, RoutedEventArgs e)
        {
            var formControl = new Controls.Forms.PriceRuleFormControl { DataContext = ViewModel };
            var dialog = new Controls.Dialogs.FormDialog(formControl, "إضافة قاعدة تسعير جديدة")
            {
                Owner = Window.GetWindow(this)
            };
            ViewModel.ClosePriceRuleAction = () => dialog.Close();
            dialog.ShowDialog();
        }

        private void AddPackage_Click(object sender, RoutedEventArgs e)
        {
            var formControl = new Controls.Forms.PackageFormControl { DataContext = ViewModel };
            var dialog = new Controls.Dialogs.FormDialog(formControl, "إضافة باقة جديدة")
            {
                Owner = Window.GetWindow(this)
            };
            ViewModel.ClosePackageAction = () => dialog.Close();
            dialog.ShowDialog();
        }
    }
}
