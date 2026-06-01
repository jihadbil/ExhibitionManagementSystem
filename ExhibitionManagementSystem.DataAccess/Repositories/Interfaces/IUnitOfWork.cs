using System;
using System.Threading.Tasks;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        ITenantRepository Tenants { get; }
        ITenantSubscriptionRepository TenantSubscriptions { get; }
        IVenueRepository Venues { get; }
        IHallRepository Halls { get; }
        IBoothRepository Booths { get; }
        IBoothMergeRepository BoothMerges { get; }
        IExhibitionRepository Exhibitions { get; }
        IExhibitionScheduleRepository ExhibitionSchedules { get; }
        IScheduleRegistrationRepository ScheduleRegistrations { get; }
        IExhibitorRepository Exhibitors { get; }
        IBoothReservationRepository BoothReservations { get; }
        IBoothStaffRepository BoothStaffs { get; }
        IProductRepository Products { get; }
        IServiceRepository Services { get; }
        IPricingPackageRepository PricingPackages { get; }
        IBoothPriceRuleRepository BoothPriceRules { get; }
        IServicePriceRuleRepository ServicePriceRules { get; }
        IInvoiceRepository Invoices { get; }
        IPaymentRepository Payments { get; }
        IVisitorRepository Visitors { get; }
        ITicketRepository Tickets { get; }
        ITicketScanRepository TicketScans { get; }
        IVisitorRatingRepository VisitorRatings { get; }
        IFinancialReportRepository FinancialReports { get; }
        ICurrencyRepository Currencies { get; }
        IExchangeRateRepository ExchangeRates { get; }
        IAuditLogRepository AuditLogs { get; }
        IExpenseRepository Expenses { get; }

        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
