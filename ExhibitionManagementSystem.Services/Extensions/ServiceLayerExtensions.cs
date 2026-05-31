using Microsoft.Extensions.DependencyInjection;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.Services.Implementations;

namespace ExhibitionManagementSystem.Services.Extensions
{
    public static class ServiceLayerExtensions
    {
        public static IServiceCollection AddServiceLayer(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITenantService, TenantService>();
            services.AddScoped<IVenueService, VenueService>();
            services.AddScoped<IHallService, HallService>();
            services.AddScoped<IBoothService, BoothService>();
            services.AddScoped<IExhibitionService, ExhibitionService>();
            services.AddScoped<IExhibitorService, ExhibitorService>();
            services.AddScoped<IPricingService, PricingService>();
            services.AddScoped<IReservationService, ReservationService>();
            services.AddScoped<IFinancialService, FinancialService>();
            services.AddScoped<IVisitorService, VisitorService>();
            services.AddScoped<ITicketService, TicketService>();
            services.AddScoped<ICurrencyService, CurrencyService>();
            services.AddScoped<IServiceManagementService, ServiceManagementService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IAdminService, AdminService>();

            return services;
        }
    }
}
