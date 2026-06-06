using System.Windows;
using ExhibitionManagementSystem.Desktop.ViewModels.Exhibitions;

namespace ExhibitionManagementSystem.Desktop.Views.Exhibitions
{
    public partial class AddEditExhibitionDialog : Window
    {
        public AddEditExhibitionDialog()
        {
            InitializeComponent();
            DataContextChanged += (s, e) =>
            {
                if (DataContext is AddEditExhibitionDialogViewModel vm)
                {
                    vm.RequestClose += (result) =>
                    {
                        try
                        {
                            this.DialogResult = result;
                        }
                        catch
                        {
                            // In case dialog was shown with Show() rather than ShowDialog()
                        }
                        this.Close();
                    };
                }
            };
        }
    }
}
