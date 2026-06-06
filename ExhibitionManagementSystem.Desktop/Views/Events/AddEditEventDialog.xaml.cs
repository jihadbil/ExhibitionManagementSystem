using System.Windows;
using ExhibitionManagementSystem.Desktop.ViewModels.Events;

namespace ExhibitionManagementSystem.Desktop.Views.Events
{
    public partial class AddEditEventDialog : Window
    {
        public AddEditEventDialog()
        {
            InitializeComponent();
            DataContextChanged += (s, e) =>
            {
                if (DataContext is AddEditEventDialogViewModel vm)
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
