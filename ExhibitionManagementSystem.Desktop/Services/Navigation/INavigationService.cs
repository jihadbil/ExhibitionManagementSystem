using System;
using System.Windows.Controls;
using ExhibitionManagementSystem.Desktop.ViewModels.Base;

namespace ExhibitionManagementSystem.Desktop.Services.Navigation
{
    public interface INavigationService
    {
        BaseViewModel? CurrentViewModel { get; }
        void Initialize(Frame frame);
        void NavigateTo<TViewModel>() where TViewModel : BaseViewModel;
        void NavigateTo<TViewModel>(object parameter) where TViewModel : BaseViewModel;
        void GoBack();
        bool CanGoBack { get; }
        event EventHandler? CurrentViewModelChanged;
    }
}
