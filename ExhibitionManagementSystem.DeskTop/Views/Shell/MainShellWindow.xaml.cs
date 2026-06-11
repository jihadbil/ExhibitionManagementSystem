using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;
using ExhibitionManagementSystem.DeskTop.Views.Dashboard;

namespace ExhibitionManagementSystem.DeskTop.Views.Shell;

public partial class MainShellWindow : Window
{
    public MainShellWindow()
    {
        InitializeComponent();
    }

    private void MainShellWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // تسجيل الـ Frame في NavigationService
        var navService = App.Services.GetRequiredService<INavigationService>();
        navService.SetFrame(MainFrame);

        // تهيئة Toast
        var notifService = App.Services.GetRequiredService<INotificationService>();
        ToastHost.Initialize(notifService);

        // التنقل الأولي إلى Dashboard
        navService.NavigateTo<DashboardPage>();
    }
}
