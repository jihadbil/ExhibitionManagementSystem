using Microsoft.Extensions.DependencyInjection;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.Services.Implementations;
using ExhibitionManagementSystem.Services.Messaging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;

namespace ExhibitionManagementSystem.Services.Extensions
{
    public static class ServiceLayerExtensions
    {
        public static IServiceCollection AddServiceLayer(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserRoleReader, UserRoleReader>();
            services.AddScoped<ITenantService, TenantService>();
            services.AddScoped<IVenueService, VenueService>();
            services.AddScoped<IHallService, HallService>();
            services.AddScoped<IBoothService, BoothService>();
            services.AddScoped<IExhibitionService, ExhibitionService>();
            services.AddScoped<IExhibitorService, ExhibitorService>();
            services.AddScoped<IPricingService, PricingService>();
            services.AddScoped<IReservationService, ReservationService>();
            services.AddScoped<IFinancialService, FinancialService>();
            services.AddScoped<IInvoiceItemService, InvoiceItemService>();
            services.AddScoped<IVisitorService, VisitorService>();

            services.AddScoped<ITicketService, TicketService>();
            services.AddScoped<ICurrencyService, CurrencyService>();
            services.AddScoped<IServiceManagementService, ServiceManagementService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IBadgeService, BadgeService>();
            services.AddScoped<ISponsorshipService, SponsorshipService>();
            services.TryAddSingleton(TimeProvider.System);
            services.AddOptions<PasswordResetOptions>().BindConfiguration("PasswordReset")
                .Validate(o => o.IsValid(), "PasswordReset requires a valid SMTP sender, host, port and HTTPS reset URL.")
                .ValidateOnStart();
            services.AddScoped<IPasswordResetDelivery, PasswordResetDelivery>();
            services.AddScoped<IPasswordResetEmailTransport, SmtpPasswordResetEmailTransport>();
            services.AddScoped<PasswordResetDispatcher>();
            services.AddHostedService<PasswordResetEmailWorker>();

            return services;
        }
    }
}
