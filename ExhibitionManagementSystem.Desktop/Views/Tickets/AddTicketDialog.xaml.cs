using System.Windows;
using ExhibitionManagementSystem.Desktop.ViewModels.Tickets;

namespace ExhibitionManagementSystem.Desktop.Views.Tickets
{
    public partial class AddTicketDialog : Window
    {
        public AddTicketDialog()
        {
            InitializeComponent();
            DataContextChanged += (s, e) =>
            {
                if (DataContext is AddTicketDialogViewModel vm)
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
