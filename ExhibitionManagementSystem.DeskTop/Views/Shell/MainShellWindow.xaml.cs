using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
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

    private void MainFrame_Navigated(object sender, NavigationEventArgs e)
    {
        if (MainFrame.Content is FrameworkElement page)
        {
            page.RenderTransform = new TranslateTransform();
            page.Opacity = 0;

            var fadeIn = (Storyboard)FindResource("FadeInStoryboard");
            var slideIn = (Storyboard)FindResource("SlideInFromRightStoryboard");

            fadeIn.Begin(page);
            slideIn.Begin(page);
        }
    }
}
