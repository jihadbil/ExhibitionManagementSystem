using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using ExhibitionManagementSystem.DeskTop.Views.Settings;

namespace ExhibitionManagementSystem.DeskTop.Controls.Navigation;

public partial class TopBarControl : UserControl
{
    private INavigationService _navigationService = null!;
    private SessionService _sessionService = null!;

    public string FullName { get; private set; } = string.Empty;

    public TopBarControl()
    {
        InitializeComponent();

        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            return;

        Loaded += TopBarControl_Loaded;
    }

    private void TopBarControl_Loaded(object sender, RoutedEventArgs e)
    {
        _navigationService = App.Services.GetRequiredService<INavigationService>();
        _sessionService = App.Services.GetRequiredService<SessionService>();

        _navigationService.Navigated += OnNavigated;

        // Initialize User Profile Display
        FullName = _sessionService.FullName;
        if (!string.IsNullOrEmpty(FullName))
        {
            var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                UserInitialsText.Text = parts[0][0].ToString().ToUpper();
            }
        }
        else
        {
            UserInitialsText.Text = "أ"; // Default admin initial ("أدمن")
        }

        // Set initial title
        UpdateTitle(_navigationService.CurrentRoute);
    }

    private void OnNavigated(object? sender, string route)
    {
        UpdateTitle(route);
    }

    private void UpdateTitle(string route)
    {
        PageTitleText.Text = route switch
        {
            "Dashboard" => "لوحة التحكم",
            "Exhibitions" => "إدارة المعارض",
            "Booths" => "الأجنحة والقاعات",
            "Companies" => "الشركات العارضة",
            "Events" => "الفعاليات والمحاضرات",
            "Tickets" => "التذاكر والزوار",
            "Analytics" => "التقارير والتحليلات",
            "Settings" => "إعدادات النظام",
            _ => "ExpoManager"
        };
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        _navigationService.NavigateTo<SettingsPage>();
    }
}
