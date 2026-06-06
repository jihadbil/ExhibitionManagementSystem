using System;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using ExhibitionManagementSystem.Desktop.Services.Navigation;
using ExhibitionManagementSystem.Desktop.ViewModels.Auth;
using ExhibitionManagementSystem.Desktop.ViewModels.Dashboard;
using ExhibitionManagementSystem.Desktop.ViewModels.Exhibitions;
using ExhibitionManagementSystem.Desktop.ViewModels.Booths;
using ExhibitionManagementSystem.Desktop.ViewModels.Analytics;
using ExhibitionManagementSystem.Desktop.ViewModels.Tickets;
using ExhibitionManagementSystem.Desktop.ViewModels.Companies;
using ExhibitionManagementSystem.Desktop.ViewModels.Events;
using ExhibitionManagementSystem.Desktop.ViewModels.Settings;

namespace ExhibitionManagementSystem.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly INavigationService _navigationService;

        public INavigationService NavigationService => _navigationService;
        public ICommand NavigateCommand { get; }

        public MainWindow(INavigationService navigationService)
        {
            InitializeComponent();
            _navigationService = navigationService;
            
            // تهيئة خدمة التنقل بالإطار الرئيسي
            _navigationService.Initialize(MainFrame);

            // تسجيل حاوية الإشعارات لـ NotificationService
            Services.Notifications.NotificationService.RegisterContainer(NotificationContainer);

            NavigateCommand = new RelayCommand<string>(ExecuteNavigation);
            Sidebar.NavigateCommand = NavigateCommand;
            Sidebar.SetNavigationService(_navigationService);

            _navigationService.CurrentViewModelChanged += NavigationService_CurrentViewModelChanged;
            UpdateShellVisibility();
            
            DataContext = this;
        }

        private void NavigationService_CurrentViewModelChanged(object? sender, EventArgs e)
        {
            UpdateShellVisibility();
        }

        private void UpdateShellVisibility()
        {
            if (_navigationService.CurrentViewModel is LoginViewModel)
            {
                TopBar.Visibility = Visibility.Collapsed;
                Sidebar.Visibility = Visibility.Collapsed;
            }
            else
            {
                TopBar.Visibility = Visibility.Visible;
                Sidebar.Visibility = Visibility.Visible;
            }
        }

        private void ExecuteNavigation(string? destination)
        {
            if (string.IsNullOrEmpty(destination)) return;

            switch (destination)
            {
                case "Dashboard":
                    _navigationService.NavigateTo<DashboardViewModel>();
                    break;
                case "Exhibitions":
                    _navigationService.NavigateTo<ExhibitionsViewModel>();
                    break;
                case "Booths":
                    _navigationService.NavigateTo<BoothsViewModel>();
                    break;
                case "Companies":
                    _navigationService.NavigateTo<CompaniesViewModel>();
                    break;
                case "Events":
                    _navigationService.NavigateTo<EventsViewModel>();
                    break;
                case "Tickets":
                    _navigationService.NavigateTo<TicketsViewModel>();
                    break;
                case "Analytics":
                    _navigationService.NavigateTo<AnalyticsViewModel>();
                    break;
                case "Settings":
                    _navigationService.NavigateTo<SettingsViewModel>();
                    break;
                case "LogOut":
                    _navigationService.NavigateTo<LoginViewModel>();
                    break;
            }
        }
    }
}