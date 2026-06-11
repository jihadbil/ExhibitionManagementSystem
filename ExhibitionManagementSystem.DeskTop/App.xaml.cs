using System;
using System.Windows;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.Services.Implementations;
using ExhibitionManagementSystem.Services.Extensions;
using ExhibitionManagementSystem.Models.DTOs.Mapping;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;
using ExhibitionManagementSystem.DeskTop.Services.Theme;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using ExhibitionManagementSystem.DeskTop.ViewModels.Auth;
using ExhibitionManagementSystem.DeskTop.ViewModels.Dashboard;
using ExhibitionManagementSystem.DeskTop.ViewModels.Exhibitions;
using ExhibitionManagementSystem.DeskTop.ViewModels.Booths;
using ExhibitionManagementSystem.DeskTop.ViewModels.Companies;
using ExhibitionManagementSystem.DeskTop.ViewModels.Events;
using ExhibitionManagementSystem.DeskTop.ViewModels.Tickets;
using ExhibitionManagementSystem.DeskTop.ViewModels.Analytics;
using ExhibitionManagementSystem.DeskTop.ViewModels.Settings;
using ExhibitionManagementSystem.DeskTop.Views.Auth;
using ExhibitionManagementSystem.DeskTop.Views.Shell;
using ExhibitionManagementSystem.DeskTop.Views.Dashboard;
using ExhibitionManagementSystem.DeskTop.Views.Exhibitions;
using ExhibitionManagementSystem.DeskTop.Views.Booths;
using ExhibitionManagementSystem.DeskTop.Views.Companies;
using ExhibitionManagementSystem.DeskTop.Views.Events;
using ExhibitionManagementSystem.DeskTop.Views.Tickets;
using ExhibitionManagementSystem.DeskTop.Views.Analytics;
using ExhibitionManagementSystem.DeskTop.Views.Settings;

namespace ExhibitionManagementSystem.DeskTop;

public partial class App : Application
{
    private IHost _host = null!;

    // نقطة وصول عامة للـ Services من أي مكان في التطبيق
    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // قراءة appsettings.json
        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        // بناء الـ Host مع DI
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(builder => builder.AddConfiguration(config))
            .ConfigureServices((ctx, services) => ConfigureServices(services, ctx.Configuration))
            .Build();

        Services = _host.Services;
        await _host.StartAsync();

        // تنفيذ Migrations وتغذية البيانات تلقائياً (يُنشئ DB إن لم يكن موجوداً)
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();
            try
            {
                await ExhibitionManagementSystem.DataAccess.DataSeeder.SeedDataAsync(scope.ServiceProvider);
            }
            catch (Exception)
            {
                // Seeding failed or already seeded
            }
        }

        // ✅ دائماً يبدأ بـ LoginWindow
        var loginWindow = Services.GetRequiredService<LoginWindow>();
        loginWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 1. قاعدة البيانات و Identity
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                config.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.MigrationsAssembly(
                    "ExhibitionManagementSystem.DataAccess")));

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 2. Unit of Work & Repositories
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 3. Business Services & AutoMapper
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        services.AddDtoMapping();
        services.AddServiceLayer();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 4. Desktop Services
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        services.AddSingleton<SessionService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IThemeService, ThemeService>();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 5. ViewModels (Transient — instance جديد لكل طلب)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        services.AddTransient<LoginViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ExhibitionsViewModel>();
        services.AddTransient<ExhibitionFormViewModel>();
        services.AddTransient<BoothsViewModel>();
        services.AddTransient<BoothDesignerViewModel>();
        services.AddTransient<CompaniesViewModel>();
        services.AddTransient<EventsViewModel>();
        services.AddTransient<TicketsViewModel>();
        services.AddTransient<AnalyticsViewModel>();
        services.AddTransient<SettingsViewModel>();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 6. Windows & Pages
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        services.AddTransient<LoginWindow>();
        services.AddSingleton<MainShellWindow>(); // Singleton: نافذة واحدة طوال العمر
        services.AddTransient<DashboardPage>();
        services.AddTransient<ExhibitionsPage>();
        services.AddTransient<BoothsPage>();
        services.AddTransient<BoothDesignerPage>();
        services.AddTransient<CompaniesPage>();
        services.AddTransient<EventsPage>();
        services.AddTransient<TicketsPage>();
        services.AddTransient<AnalyticsPage>();
        services.AddTransient<SettingsPage>();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}
