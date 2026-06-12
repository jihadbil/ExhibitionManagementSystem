using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using ExhibitionManagementSystem.DeskTop.Views.Dashboard;
using ExhibitionManagementSystem.DeskTop.Views.Exhibitions;
using ExhibitionManagementSystem.DeskTop.Views.Booths;
using ExhibitionManagementSystem.DeskTop.Views.Companies;
using ExhibitionManagementSystem.DeskTop.Views.Events;
using ExhibitionManagementSystem.DeskTop.Views.Tickets;
using ExhibitionManagementSystem.DeskTop.Views.Analytics;
using ExhibitionManagementSystem.DeskTop.Views.Settings;
using ExhibitionManagementSystem.DeskTop.Views.Auth;
using ExhibitionManagementSystem.DeskTop.Views.Venues;

namespace ExhibitionManagementSystem.DeskTop.Controls.Navigation;

public partial class SidebarControl : UserControl
{
    private INavigationService _navigationService = null!;
    private SessionService _sessionService = null!;

    public ObservableCollection<NavItemModel> NavItems { get; } = [];

    public SidebarControl()
    {
        InitializeComponent();
        
        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            return;

        Loaded += SidebarControl_Loaded;
    }

    private void SidebarControl_Loaded(object sender, RoutedEventArgs e)
    {
        _navigationService = App.Services.GetRequiredService<INavigationService>();
        _sessionService = App.Services.GetRequiredService<SessionService>();

        _navigationService.Navigated += OnNavigated;

        // Initialize Nav Items
        var items = new List<NavItemModel>
        {
            new() { Label = "لوحة التحكم",    Icon = "⊞", Route = "Dashboard" },
            new() { Label = "المعارض",         Icon = "🏛", Route = "Exhibitions" },
            new() { Label = "المواقع والقاعات", Icon = "📍", Route = "Venues" },
            new() { Label = "الأجنحة",         Icon = "🏪", Route = "Booths" },
            new() { Label = "الشركات العارضة", Icon = "🏢", Route = "Companies" },
            new() { Label = "الفعاليات",       Icon = "📅", Route = "Events" },
            new() { Label = "التذاكر والزوار", Icon = "🎟", Route = "Tickets" },
            new() { Label = "التحليلات",       Icon = "📊", Route = "Analytics" },
            new() { Label = "الإعدادات",       Icon = "⚙", Route = "Settings" }
        };

        NavItems.Clear();
        foreach (var item in items)
        {
            NavItems.Add(item);
        }

        NavItemsList.ItemsSource = NavItems;

        // Set initial highlight based on current route
        UpdateActiveRoute(_navigationService.CurrentRoute);
    }

    private void OnNavigated(object? sender, string route)
    {
        UpdateActiveRoute(route);
    }

    private void UpdateActiveRoute(string route)
    {
        foreach (var item in NavItems)
        {
            item.IsActive = (item.Route == route);
        }
    }

    private void NavItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is NavItemModel item)
        {
            NavigateToRoute(item.Route);
        }
    }

    private void NavigateToRoute(string route)
    {
        switch (route)
        {
            case "Dashboard":
                _navigationService.NavigateTo<DashboardPage>();
                break;
            case "Exhibitions":
                _navigationService.NavigateTo<ExhibitionsPage>();
                break;
            case "Venues":
                _navigationService.NavigateTo<VenuesPage>();
                break;
            case "Booths":
                _navigationService.NavigateTo<BoothsPage>();
                break;
            case "Companies":
                _navigationService.NavigateTo<CompaniesPage>();
                break;
            case "Events":
                _navigationService.NavigateTo<EventsPage>();
                break;
            case "Tickets":
                _navigationService.NavigateTo<TicketsPage>();
                break;
            case "Analytics":
                _navigationService.NavigateTo<AnalyticsPage>();
                break;
            case "Settings":
                _navigationService.NavigateTo<SettingsPage>();
                break;
        }
    }

    private void Logout_Click(object sender, MouseButtonEventArgs e)
    {
        _sessionService.ClearSession();

        // Open LoginWindow
        var loginWindow = App.Services.GetRequiredService<LoginWindow>();
        loginWindow.Show();

        // Close the current Shell Window
        var parentWindow = Window.GetWindow(this);
        parentWindow?.Close();
    }
}

public partial class NavItemModel : ObservableObject
{
    public string Label { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isActive;
}
