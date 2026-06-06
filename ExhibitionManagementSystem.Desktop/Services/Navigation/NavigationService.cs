using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ExhibitionManagementSystem.Desktop.ViewModels.Base;
using ExhibitionManagementSystem.Desktop.ViewModels.Auth;
using ExhibitionManagementSystem.Desktop.ViewModels.Dashboard;
using ExhibitionManagementSystem.Desktop.ViewModels.Exhibitions;
using ExhibitionManagementSystem.Desktop.ViewModels.Booths;
using ExhibitionManagementSystem.Desktop.ViewModels.Analytics;
using ExhibitionManagementSystem.Desktop.ViewModels.Tickets;
using ExhibitionManagementSystem.Desktop.ViewModels.Companies;
using ExhibitionManagementSystem.Desktop.ViewModels.Events;
using ExhibitionManagementSystem.Desktop.ViewModels.Settings;
using ExhibitionManagementSystem.Desktop.Views.Auth;
using ExhibitionManagementSystem.Desktop.Views.Dashboard;
using ExhibitionManagementSystem.Desktop.Views.Exhibitions;
using ExhibitionManagementSystem.Desktop.Views.Booths;
using ExhibitionManagementSystem.Desktop.Views.Analytics;
using ExhibitionManagementSystem.Desktop.Views.Tickets;
using ExhibitionManagementSystem.Desktop.Views.Companies;
using ExhibitionManagementSystem.Desktop.Views.Events;
using ExhibitionManagementSystem.Desktop.Views.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace ExhibitionManagementSystem.Desktop.Services.Navigation
{
    public class NavigationService : INavigationService
    {
        private Frame? _frame;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, Type> _viewModelToViewMap;
        private BaseViewModel? _currentViewModel;

        public BaseViewModel? CurrentViewModel => _currentViewModel;

        public bool CanGoBack => _frame?.CanGoBack ?? false;

        public event EventHandler? CurrentViewModelChanged;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _viewModelToViewMap = new Dictionary<Type, Type>
            {
                { typeof(LoginViewModel), typeof(LoginView) },
                { typeof(DashboardViewModel), typeof(DashboardView) },
                { typeof(ExhibitionsViewModel), typeof(ExhibitionsView) },
                { typeof(ExhibitionDetailViewModel), typeof(ExhibitionDetailView) },
                { typeof(BoothsViewModel), typeof(BoothsView) },
                { typeof(BoothDesignerViewModel), typeof(BoothDesignerView) },
                { typeof(AnalyticsViewModel), typeof(AnalyticsView) },
                { typeof(TicketsViewModel), typeof(TicketsView) },
                { typeof(CompaniesViewModel), typeof(CompaniesView) },
                { typeof(EventsViewModel), typeof(EventsView) },
                { typeof(SettingsViewModel), typeof(SettingsView) }
            };
        }

        public void Initialize(Frame frame)
        {
            _frame = frame;
        }

        public void NavigateTo<TViewModel>() where TViewModel : BaseViewModel
        {
            NavigateToInternal(typeof(TViewModel), null);
        }

        public void NavigateTo<TViewModel>(object parameter) where TViewModel : BaseViewModel
        {
            NavigateToInternal(typeof(TViewModel), parameter);
        }

        private void NavigateToInternal(Type viewModelType, object? parameter)
        {
            if (_frame == null)
                throw new InvalidOperationException("NavigationService must be initialized with a Frame first.");

            if (!_viewModelToViewMap.TryGetValue(viewModelType, out var viewType))
                throw new ArgumentException($"No view registered for ViewModel type: {viewModelType.Name}");

            var view = (FrameworkElement)_serviceProvider.GetRequiredService(viewType);
            var viewModel = (BaseViewModel)_serviceProvider.GetRequiredService(viewModelType);

            view.DataContext = viewModel;
            _currentViewModel = viewModel;
            CurrentViewModelChanged?.Invoke(this, EventArgs.Empty);

            _frame.Navigate(view);
            
            // تطبيق انيميشن التلاشي Page Fade-In
            var fadeInAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromSeconds(0.3)),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };
            view.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
            
            if (parameter != null)
            {
                _ = viewModel.InitializeAsync(parameter);
            }
            else
            {
                _ = viewModel.InitializeAsync();
            }
        }

        public void GoBack()
        {
            if (CanGoBack)
            {
                _frame?.GoBack();
            }
        }
    }
}
