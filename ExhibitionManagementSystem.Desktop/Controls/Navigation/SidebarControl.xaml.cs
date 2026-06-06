using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ExhibitionManagementSystem.Desktop.Services.Navigation;
using ExhibitionManagementSystem.Desktop.ViewModels.Auth;
using ExhibitionManagementSystem.Desktop.ViewModels.Dashboard;
using ExhibitionManagementSystem.Desktop.ViewModels.Exhibitions;
using ExhibitionManagementSystem.Desktop.ViewModels.Booths;
using ExhibitionManagementSystem.Desktop.ViewModels.Companies;
using ExhibitionManagementSystem.Desktop.ViewModels.Events;
using ExhibitionManagementSystem.Desktop.ViewModels.Tickets;
using ExhibitionManagementSystem.Desktop.ViewModels.Analytics;
using ExhibitionManagementSystem.Desktop.ViewModels.Settings;

namespace ExhibitionManagementSystem.Desktop.Controls.Navigation
{
    public partial class SidebarControl : UserControl
    {
        public static readonly DependencyProperty SelectedMenuItemProperty =
            DependencyProperty.Register(nameof(SelectedMenuItem), typeof(string), typeof(SidebarControl), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty UserNameProperty =
            DependencyProperty.Register(nameof(UserName), typeof(string), typeof(SidebarControl), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty NavigateCommandProperty =
            DependencyProperty.Register(nameof(NavigateCommand), typeof(ICommand), typeof(SidebarControl), new PropertyMetadata(null));

        public string SelectedMenuItem
        {
            get => (string)GetValue(SelectedMenuItemProperty);
            set => SetValue(SelectedMenuItemProperty, value);
        }

        public string UserName
        {
            get => (string)GetValue(UserNameProperty);
            set => SetValue(UserNameProperty, value);
        }

        public ICommand NavigateCommand
        {
            get => (ICommand)GetValue(NavigateCommandProperty);
            set => SetValue(NavigateCommandProperty, value);
        }

        public SidebarControl()
        {
            InitializeComponent();
        }

        public void SetNavigationService(INavigationService navigationService)
        {
            navigationService.CurrentViewModelChanged += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    SelectedMenuItem = navigationService.CurrentViewModel switch
                    {
                        DashboardViewModel => "Dashboard",
                        ExhibitionsViewModel or ExhibitionDetailViewModel => "Exhibitions",
                        BoothsViewModel or BoothDesignerViewModel => "Booths",
                        CompaniesViewModel => "Companies",
                        EventsViewModel => "Events",
                        TicketsViewModel => "Tickets",
                        AnalyticsViewModel => "Analytics",
                        SettingsViewModel => "Settings",
                        _ => string.Empty
                    };
                });
            };
        }
    }
}
