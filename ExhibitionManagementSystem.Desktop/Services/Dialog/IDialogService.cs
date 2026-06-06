using System.Threading.Tasks;

namespace ExhibitionManagementSystem.Desktop.Services.Dialog
{
    public interface IDialogService
    {
        bool? ShowDialog<TViewModel>(TViewModel viewModel) where TViewModel : class;
        bool ShowConfirm(string title, string message);
    }
}
