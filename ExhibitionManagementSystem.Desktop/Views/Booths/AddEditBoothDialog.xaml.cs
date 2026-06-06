using System.Windows;
using ExhibitionManagementSystem.Desktop.ViewModels.Booths;

namespace ExhibitionManagementSystem.Desktop.Views.Booths
{
    public partial class AddEditBoothDialog : Window
    {
        public AddEditBoothDialog()
        {
            InitializeComponent();
            DataContextChanged += (s, e) =>
            {
                if (DataContext is AddEditBoothDialogViewModel vm)
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
