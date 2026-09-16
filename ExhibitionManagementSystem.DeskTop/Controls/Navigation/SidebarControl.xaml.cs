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
using ExhibitionManagementSystem.DeskTop.Views.Reservations;
using ExhibitionManagementSystem.DeskTop.Views.Financial;
using ExhibitionManagementSystem.DeskTop.Views.Users;
using ExhibitionManagementSystem.DeskTop.Views.Services;
using ExhibitionManagementSystem.DeskTop.Views.Admin;
using ExhibitionManagementSystem.DeskTop.Views.Tenants;
using ExhibitionManagementSystem.DeskTop.Views.Badges;
using ExhibitionManagementSystem.DeskTop.Views.Sponsorship;

namespace ExhibitionManagementSystem.DeskTop.Controls.Navigation;

public partial class SidebarControl : UserControl
{
    private INavigationService _navigationService = null!;
    private SessionService _sessionService = null!;

    public ObservableCollection<NavCategoryModel> Categories { get; } = [];

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

        // Initialize Nav Categories and Items
        var categories = new List<NavCategoryModel>
        {
            new()
            {
                Name = "العامة والتحليلات",
                Items = new ObservableCollection<NavItemModel>
                {
                    new() { Label = "لوحة التحكم",    Icon = "IconDashboard", Route = "Dashboard" },
                    new() { Label = "التحليلات",       Icon = "IconAnalytics", Route = "Analytics" }
                }
            },
            new()
            {
                Name = "إدارة المعارض",
                Items = new ObservableCollection<NavItemModel>
                {
                    new() { Label = "المعارض",         Icon = "IconExhibition", Route = "Exhibitions" },
                    new() { Label = "الفعاليات",       Icon = "IconCalendar", Route = "Events" },
                    new() { Label = "المواقع والقاعات", Icon = "IconLocation", Route = "Venues" },
                    new() { Label = "الأجنحة",         Icon = "IconBooth", Route = "Booths" },
                    new() { Label = "مصمم الشارات",    Icon = "IconBadge", Route = "BadgeDesigner" },
                    new() { Label = "كشك الاستقبال",   Icon = "IconKiosk", Route = "CheckInKiosk" }
                }
            },
            new()
            {
                Name = "المبيعات والحجوزات",
                Items = new ObservableCollection<NavItemModel>
                {
                    new() { Label = "الحجوزات",        Icon = "IconReservation", Route = "Reservations" },
                    new() { Label = "الفواتير",        Icon = "IconInvoice", Route = "Invoices" },
                    new() { Label = "الخدمات والتسعير", Icon = "IconServices", Route = "Services" },
                    new() { Label = "التذاكر والزوار", Icon = "IconTicket", Route = "Tickets" }
                }
            },
            new()
            {
                Name = "العارضون والشركاء",
                Items = new ObservableCollection<NavItemModel>
                {
                    new() { Label = "الشركات العارضة", Icon = "IconCompany", Route = "Companies" },
                    new() { Label = "الرعاة والإعلانات", Icon = "IconSponsor", Route = "Sponsorship" }
                }
            },
            new()
            {
                Name = "إدارة النظام",
                Items = new ObservableCollection<NavItemModel>
                {
                    new() { Label = "المستخدمون والأدوار", Icon = "IconUsers", Route = "Users" },
                    new() { Label = "ملف الشركة", Icon = "IconTenants", Route = "Tenants" },
                    new() { Label = "إدارة النظام",     Icon = "IconAdmin", Route = "Admin"   },
                    new() { Label = "الإعدادات",       Icon = "IconSettings", Route = "Settings" }
                }
            }
        };

        Categories.Clear();
        foreach (var category in categories)
        {
            Categories.Add(category);
        }

        NavItemsList.ItemsSource = Categories;

        // Set initial highlight based on current route
        UpdateActiveRoute(_navigationService.CurrentRoute);
    }

    private void OnNavigated(object? sender, string route)
    {
        UpdateActiveRoute(route);
    }

    private void UpdateActiveRoute(string route)
    {
        foreach (var category in Categories)
        {
            bool hasActive = false;
            foreach (var item in category.Items)
            {
                item.IsActive = (item.Route == route);
                if (item.IsActive)
                {
                    hasActive = true;
                }
            }
            if (hasActive)
            {
                category.IsExpanded = true;
            }
        }
    }

    private void CategoryHeader_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is NavCategoryModel category)
        {
            category.IsExpanded = !category.IsExpanded;
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
            case "BadgeDesigner":
                _navigationService.NavigateTo<BadgeDesignerPage>();
                break;
            case "CheckInKiosk":
                _navigationService.NavigateTo<CheckInKioskPage>();
                break;
            case "Sponsorship":
                _navigationService.NavigateTo<SponsorshipPage>();
                break;
            case "Reservations":
                _navigationService.NavigateTo<ReservationsPage>();
                break;
            case "Invoices":
                _navigationService.NavigateTo<InvoicesPage>();
                break;
            case "Companies":
                _navigationService.NavigateTo<CompaniesPage>();
                break;
            case "Events":
                _navigationService.NavigateTo<EventsPage>();
                break;
            case "Users":
                _navigationService.NavigateTo<UsersPage>();
                break;
            case "Services":
                _navigationService.NavigateTo<ServicesPage>();
                break;
            case "Tickets":
                _navigationService.NavigateTo<TicketsPage>();
                break;
            case "Tenants":
                _navigationService.NavigateTo<TenantsPage>();
                break;
            case "Admin":
                _navigationService.NavigateTo<AdminPage>();
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

public partial class NavCategoryModel : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public ObservableCollection<NavItemModel> Items { get; set; } = [];

    [ObservableProperty]
    private bool _isExpanded;
}

public partial class NavItemModel : ObservableObject
{
    public string Label { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isActive;
}
