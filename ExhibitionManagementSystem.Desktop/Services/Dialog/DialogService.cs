using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace ExhibitionManagementSystem.Desktop.Services.Dialog
{
    public class DialogService : IDialogService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, Type> _mappings = new();

        public DialogService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            RegisterMappings();
        }

        private void RegisterMappings()
        {
            _mappings[typeof(ViewModels.Exhibitions.AddEditExhibitionDialogViewModel)] = typeof(Views.Exhibitions.AddEditExhibitionDialog);
            _mappings[typeof(ViewModels.Tickets.AddTicketDialogViewModel)] = typeof(Views.Tickets.AddTicketDialog);
            _mappings[typeof(ViewModels.Booths.AddEditBoothDialogViewModel)] = typeof(Views.Booths.AddEditBoothDialog);
            _mappings[typeof(ViewModels.Companies.AddEditCompanyDialogViewModel)] = typeof(Views.Companies.AddEditCompanyDialog);
            _mappings[typeof(ViewModels.Events.AddEditEventDialogViewModel)] = typeof(Views.Events.AddEditEventDialog);
        }

        public bool? ShowDialog<TViewModel>(TViewModel viewModel) where TViewModel : class
        {
            var vmType = viewModel.GetType();
            if (!_mappings.TryGetValue(vmType, out var windowType))
            {
                throw new ArgumentException($"No registered window found for view model type: {vmType.FullName}");
            }

            var window = (Window)_serviceProvider.GetRequiredService(windowType);
            window.DataContext = viewModel;
            
            // Set Owner to MainWindow if it exists
            if (Application.Current.MainWindow != null && Application.Current.MainWindow != window)
            {
                window.Owner = Application.Current.MainWindow;
            }

            return window.ShowDialog();
        }

        public bool ShowConfirm(string title, string message)
        {
            var result = MessageBox.Show(
                message, 
                title, 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question, 
                MessageBoxResult.No, 
                MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            return result == MessageBoxResult.Yes;
        }
    }
}
