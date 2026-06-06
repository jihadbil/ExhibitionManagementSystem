using System.Windows;
using ExhibitionManagementSystem.Desktop.ViewModels.Companies;

namespace ExhibitionManagementSystem.Desktop.Views.Companies
{
    public partial class AddEditCompanyDialog : Window
    {
        public AddEditCompanyDialog()
        {
            InitializeComponent();
            DataContextChanged += (s, e) =>
            {
                if (DataContext is AddEditCompanyDialogViewModel vm)
                {
                    vm.RequestClose += (result) =>
                    {
                        try
                        {
                            this.DialogResult = result;
                        }
                        catch
                        {
                        }
                        this.Close();
                    };
                }
            };
        }
    }
}
