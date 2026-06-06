using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.Extensions;
using ExhibitionManagementSystem.Services.Extensions;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.Services.Implementations;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Mapping;
using ExhibitionManagementSystem.Desktop.Services.Navigation;
using ExhibitionManagementSystem.Desktop.Services.Notifications;
using ExhibitionManagementSystem.Desktop.Services.Theme;
using ExhibitionManagementSystem.Desktop.Services.Auth;
using ExhibitionManagementSystem.Desktop.Services.Dialog;
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

namespace ExhibitionManagementSystem.Desktop
{
    public partial class App : Application
    {
        private readonly IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Add DB and Identity
                    var connectionString = context.Configuration.GetConnectionString("DefaultConnection") 
                        ?? "Server=(local);Database=MyDatabase;Trusted_Connection=True;TrustServerCertificate=True;";
                    
                    services.AddDataAccess(connectionString);
                    
                    services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
                    {
                        options.Password.RequireDigit = true;
                        options.Password.RequireLowercase = true;
                        options.Password.RequireUppercase = true;
                        options.Password.RequiredLength = 8;
                    })
                    .AddEntityFrameworkStores<ApplicationDbContext>()
                    .AddDefaultTokenProviders();

                    services.AddDtoMapping();
                    services.AddServiceLayer();

                    // ===== Services =====
                    services.AddSingleton<INavigationService, NavigationService>();
                    services.AddSingleton<INotificationService, NotificationService>();
                    services.AddSingleton<IThemeService, ThemeService>();
                    services.AddSingleton<ISessionService, SessionService>();
                    services.AddSingleton<IDialogService, DialogService>();


                    // ===== ViewModels =====
                    services.AddTransient<LoginViewModel>();
                    services.AddTransient<DashboardViewModel>();
                    services.AddTransient<ExhibitionsViewModel>();
                    services.AddTransient<ExhibitionDetailViewModel>();
                    services.AddTransient<BoothsViewModel>();
                    services.AddTransient<BoothDesignerViewModel>();
                    services.AddTransient<AnalyticsViewModel>();
                    services.AddTransient<TicketsViewModel>();
                    services.AddTransient<CompaniesViewModel>();
                    services.AddTransient<EventsViewModel>();
                    services.AddTransient<SettingsViewModel>();

                    // Dialog ViewModels
                    services.AddTransient<AddEditExhibitionDialogViewModel>();
                    services.AddTransient<AddTicketDialogViewModel>();
                    services.AddTransient<AddEditBoothDialogViewModel>();
                    services.AddTransient<AddEditCompanyDialogViewModel>();
                    services.AddTransient<AddEditEventDialogViewModel>();

                    // ===== Views =====
                    services.AddTransient<LoginView>();
                    services.AddTransient<DashboardView>();
                    services.AddTransient<ExhibitionsView>();
                    services.AddTransient<ExhibitionDetailView>();
                    services.AddTransient<BoothsView>();
                    services.AddTransient<BoothDesignerView>();
                    services.AddTransient<AnalyticsView>();
                    services.AddTransient<TicketsView>();
                    services.AddTransient<CompaniesView>();
                    services.AddTransient<EventsView>();
                    services.AddTransient<SettingsView>();

                    // Dialog Windows
                    services.AddTransient<AddEditExhibitionDialog>();
                    services.AddTransient<AddTicketDialog>();
                    services.AddTransient<AddEditBoothDialog>();
                    services.AddTransient<AddEditCompanyDialog>();
                    services.AddTransient<AddEditEventDialog>();

                    // ===== Main Windows =====
                    services.AddSingleton<MainWindow>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // تهيئة LiveCharts
            LiveCharts.Configure(config => 
                config
                    .AddSkiaSharp()
                    .AddDefaultMappers()
                    .AddLightTheme()
            );

            await _host.StartAsync();

            // تعيين FlowDirection على مستوى التطبيق ليكون RTL افتراضياً
            FrameworkElement.FlowDirectionProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(FlowDirection.RightToLeft));

            // إظهار النافذة الرئيسية للتطبيق
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            // الانتقال المبدئي إلى صفحة تسجيل الدخول
            var navigationService = _host.Services.GetRequiredService<INavigationService>();
            navigationService.NavigateTo<LoginViewModel>();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync();
            }
            base.OnExit(e);
        }
    }
}
