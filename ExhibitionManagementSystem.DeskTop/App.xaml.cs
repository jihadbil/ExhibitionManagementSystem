using System;
using System.Windows;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.DeskTop.ApiClients.Base;
using ExhibitionManagementSystem.DeskTop.ApiClients.Auth;
using ExhibitionManagementSystem.DeskTop.ApiClients.Exhibitions;
using ExhibitionManagementSystem.DeskTop.ApiClients.Booths;
using ExhibitionManagementSystem.DeskTop.ApiClients.Reservations;
using ExhibitionManagementSystem.DeskTop.ApiClients.Financial;
using ExhibitionManagementSystem.DeskTop.ApiClients.Venues;
using ExhibitionManagementSystem.DeskTop.ApiClients.Halls;
using ExhibitionManagementSystem.DeskTop.ApiClients.Exhibitors;
using ExhibitionManagementSystem.DeskTop.ApiClients.Services;
using ExhibitionManagementSystem.DeskTop.ApiClients.Pricing;
using ExhibitionManagementSystem.DeskTop.ApiClients.Visitors;
using ExhibitionManagementSystem.DeskTop.ApiClients.Tickets;
using ExhibitionManagementSystem.DeskTop.ApiClients.Reports;
using ExhibitionManagementSystem.DeskTop.ApiClients.Dashboard;
using ExhibitionManagementSystem.DeskTop.ApiClients.Currency;
using ExhibitionManagementSystem.DeskTop.ApiClients.Admin;
using ExhibitionManagementSystem.DeskTop.ApiClients.Tenants;
using ExhibitionManagementSystem.DeskTop.Services.Navigation;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;
using ExhibitionManagementSystem.DeskTop.Services.Theme;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using ExhibitionManagementSystem.DeskTop.ViewModels.Auth;
using ExhibitionManagementSystem.DeskTop.ViewModels.Admin;
using ExhibitionManagementSystem.DeskTop.ViewModels.Tenants;
using ExhibitionManagementSystem.DeskTop.ViewModels.Dashboard;
using ExhibitionManagementSystem.DeskTop.ViewModels.Exhibitions;
using ExhibitionManagementSystem.DeskTop.ViewModels.Booths;
using ExhibitionManagementSystem.DeskTop.ViewModels.Companies;
using ExhibitionManagementSystem.DeskTop.ViewModels.Events;
using ExhibitionManagementSystem.DeskTop.ViewModels.Tickets;
using ExhibitionManagementSystem.DeskTop.ViewModels.Analytics;
using ExhibitionManagementSystem.DeskTop.ViewModels.Settings;
using ExhibitionManagementSystem.DeskTop.ViewModels.Venues;
using ExhibitionManagementSystem.DeskTop.ViewModels.Reservations;
using ExhibitionManagementSystem.DeskTop.ViewModels.Financial;
using ExhibitionManagementSystem.DeskTop.ViewModels.Users;
using ExhibitionManagementSystem.DeskTop.ViewModels.ServiceMgmt;
using ExhibitionManagementSystem.DeskTop.Views.Auth;
using ExhibitionManagementSystem.DeskTop.Views.Admin;
using ExhibitionManagementSystem.DeskTop.Views.Tenants;
using ExhibitionManagementSystem.DeskTop.Views.Shell;
using ExhibitionManagementSystem.DeskTop.Views.Dashboard;
using ExhibitionManagementSystem.DeskTop.Views.Exhibitions;
using ExhibitionManagementSystem.DeskTop.Views.Booths;
using ExhibitionManagementSystem.DeskTop.Views.Companies;
using ExhibitionManagementSystem.DeskTop.Views.Events;
using ExhibitionManagementSystem.DeskTop.Views.Tickets;
using ExhibitionManagementSystem.DeskTop.Views.Analytics;
using ExhibitionManagementSystem.DeskTop.Views.Settings;
using ExhibitionManagementSystem.DeskTop.Views.Venues;
using ExhibitionManagementSystem.DeskTop.Views.Reservations;
using ExhibitionManagementSystem.DeskTop.Views.Financial;
using ExhibitionManagementSystem.DeskTop.Views.Users;
using ExhibitionManagementSystem.DeskTop.Views.Services;
using ExhibitionManagementSystem.DeskTop.ApiClients.Badges;
using ExhibitionManagementSystem.DeskTop.ApiClients.Sponsorship;
using ExhibitionManagementSystem.DeskTop.Services.Printing;
using ExhibitionManagementSystem.DeskTop.ViewModels.Badges;
using ExhibitionManagementSystem.DeskTop.ViewModels.Sponsorship;
using ExhibitionManagementSystem.DeskTop.Views.Badges;
using ExhibitionManagementSystem.DeskTop.Views.Sponsorship;

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



        // ✅ دائماً يبدأ بـ LoginWindow
        var loginWindow = Services.GetRequiredService<LoginWindow>();
        loginWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 1. HttpClient & Refresh Token Handler
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        var apiBaseUrl = config["ApiBaseUrl"] 
            ?? throw new InvalidOperationException("ApiBaseUrl configuration is missing!");

        services.AddTransient<TokenRefreshHandler>();

        services.AddHttpClient("ApiClient", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        })
        .AddHttpMessageHandler<TokenRefreshHandler>();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 2. API Clients (Business Implementations)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        services.AddSingleton<SessionService>();
        
        services.AddTransient<IAuthService, AuthApiClient>();
        services.AddTransient<IExhibitionService, ExhibitionApiClient>();
        services.AddTransient<IBoothService, BoothApiClient>();
        services.AddTransient<IReservationService, ReservationApiClient>();
        services.AddTransient<IFinancialService, FinancialApiClient>();
        services.AddTransient<IInvoiceItemService, InvoiceItemApiClient>();
        services.AddTransient<IVenueService, VenueApiClient>();

        services.AddTransient<IHallService, HallApiClient>();
        services.AddTransient<IExhibitorService, ExhibitorApiClient>();
        services.AddTransient<IServiceManagementService, ServiceApiClient>();
        services.AddTransient<IPricingService, PricingApiClient>();
        services.AddTransient<IVisitorService, VisitorApiClient>();
        services.AddTransient<ITicketService, TicketApiClient>();
        services.AddTransient<IReportService, ReportApiClient>();
        services.AddTransient<IDashboardService, DashboardApiClient>();
        services.AddTransient<ICurrencyService, CurrencyApiClient>();
        services.AddTransient<IAdminService, AdminApiClient>();
        services.AddTransient<ITenantService, TenantsApiClient>();
        services.AddTransient<IBadgeService, BadgeApiClient>();
        services.AddTransient<ISponsorshipService, SponsorshipApiClient>();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 3. Desktop Services
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<BadgePrintService>();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 5. ViewModels (Transient — instance جديد لكل طلب)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        services.AddTransient<LoginViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ExhibitionsViewModel>();
        services.AddTransient<ExhibitionFormViewModel>();
        services.AddTransient<BoothsViewModel>();
        services.AddTransient<BoothDesignerViewModel>();
        services.AddTransient<NodifyBoothDesignerViewModel>();
        services.AddTransient<CompaniesViewModel>();
        services.AddTransient<ExhibitorFormViewModel>();
        services.AddTransient<EventsViewModel>();
        services.AddTransient<TicketsViewModel>();
        services.AddTransient<AnalyticsViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<VenuesViewModel>();
        services.AddTransient<VenueFormViewModel>();
        services.AddTransient<HallFormViewModel>();
        services.AddTransient<ReservationsViewModel>();
        services.AddTransient<ReservationFormViewModel>();
        services.AddTransient<InvoicesViewModel>();
        services.AddTransient<InvoiceDetailViewModel>();
        services.AddTransient<UsersViewModel>();
        services.AddTransient<UserFormViewModel>();
        services.AddTransient<ServicesViewModel>();
        services.AddTransient<AdminViewModel>();
        services.AddTransient<TenantsViewModel>();
        services.AddTransient<BadgeDesignerViewModel>();
        services.AddTransient<CheckInKioskViewModel>();
        services.AddTransient<SponsorshipViewModel>();
        services.AddTransient<SponsorFormViewModel>();
        services.AddTransient<SponsorshipPackageFormViewModel>();
        services.AddTransient<AdvertisingSpaceFormViewModel>();
        services.AddTransient<SponsorshipContractFormViewModel>();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 6. Windows & Pages
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        services.AddTransient<LoginWindow>();
        services.AddTransient<MainShellWindow>(); // Transient: إنشاء نافذة جديدة لكل تسجيل دخول
        services.AddTransient<DashboardPage>();
        services.AddTransient<ExhibitionsPage>();
        services.AddTransient<BoothsPage>();
        services.AddTransient<BoothDesignerPage>();
        services.AddTransient<NodifyBoothDesignerPage>();
        services.AddTransient<CompaniesPage>();
        services.AddTransient<EventsPage>();
        services.AddTransient<TicketsPage>();
        services.AddTransient<AnalyticsPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<VenuesPage>();
        services.AddTransient<ReservationsPage>();
        services.AddTransient<InvoicesPage>();
        services.AddTransient<UsersPage>();
        services.AddTransient<ServicesPage>();
        services.AddTransient<AdminPage>();
        services.AddTransient<TenantsPage>();
        services.AddTransient<BadgeDesignerPage>();
        services.AddTransient<CheckInKioskPage>();
        services.AddTransient<SponsorshipPage>();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}
