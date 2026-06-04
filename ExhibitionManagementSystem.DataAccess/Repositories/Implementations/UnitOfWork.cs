using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private IDbContextTransaction? _transaction;

        // Lazy repository backing fields
        private ITenantRepository? _tenants;
        private ITenantSubscriptionRepository? _tenantSubscriptions;
        private IVenueRepository? _venues;
        private IHallRepository? _halls;
        private IBoothRepository? _booths;
        private IBoothMergeRepository? _boothMerges;
        private IExhibitionRepository? _exhibitions;
        private IExhibitionScheduleRepository? _exhibitionSchedules;
        private IScheduleRegistrationRepository? _scheduleRegistrations;
        private IExhibitorRepository? _exhibitors;
        private IBoothReservationRepository? _boothReservations;
        private IBoothStaffRepository? _boothStaffs;
        private IProductRepository? _products;
        private IServiceRepository? _services;
        private IPricingPackageRepository? _pricingPackages;
        private IBoothPriceRuleRepository? _boothPriceRules;
        private IServicePriceRuleRepository? _servicePriceRules;
        private IInvoiceRepository? _invoices;
        private IPaymentRepository? _payments;
        private IVisitorRepository? _visitors;
        private ITicketRepository? _tickets;
        private ITicketScanRepository? _ticketScans;
        private IVisitorRatingRepository? _visitorRatings;
        private IFinancialReportRepository? _financialReports;
        private ICurrencyRepository? _currencies;
        private IExchangeRateRepository? _exchangeRates;
        private IAuditLogRepository? _auditLogs;
        private IExpenseRepository? _expenses;

        public UnitOfWork(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        // Lazy properties exposing repositories
        public ITenantRepository Tenants => _tenants ??= new TenantRepository(_context);
        public ITenantSubscriptionRepository TenantSubscriptions => _tenantSubscriptions ??= new TenantSubscriptionRepository(_context);
        public IVenueRepository Venues => _venues ??= new VenueRepository(_context);
        public IHallRepository Halls => _halls ??= new HallRepository(_context);
        public IBoothRepository Booths => _booths ??= new BoothRepository(_context);
        public IBoothMergeRepository BoothMerges => _boothMerges ??= new BoothMergeRepository(_context);
        public IExhibitionRepository Exhibitions => _exhibitions ??= new ExhibitionRepository(_context);
        public IExhibitionScheduleRepository ExhibitionSchedules => _exhibitionSchedules ??= new ExhibitionScheduleRepository(_context);
        public IScheduleRegistrationRepository ScheduleRegistrations => _scheduleRegistrations ??= new ScheduleRegistrationRepository(_context);
        public IExhibitorRepository Exhibitors => _exhibitors ??= new ExhibitorRepository(_context);
        public IBoothReservationRepository BoothReservations => _boothReservations ??= new BoothReservationRepository(_context);
        public IBoothStaffRepository BoothStaffs => _boothStaffs ??= new BoothStaffRepository(_context);
        public IProductRepository Products => _products ??= new ProductRepository(_context);
        public IServiceRepository Services => _services ??= new ServiceRepository(_context);
        public IPricingPackageRepository PricingPackages => _pricingPackages ??= new PricingPackageRepository(_context);
        public IBoothPriceRuleRepository BoothPriceRules => _boothPriceRules ??= new BoothPriceRuleRepository(_context);
        public IServicePriceRuleRepository ServicePriceRules => _servicePriceRules ??= new ServicePriceRuleRepository(_context);
        public IInvoiceRepository Invoices => _invoices ??= new InvoiceRepository(_context);
        public IPaymentRepository Payments => _payments ??= new PaymentRepository(_context);
        public IVisitorRepository Visitors => _visitors ??= new VisitorRepository(_context);
        public ITicketRepository Tickets => _tickets ??= new TicketRepository(_context);
        public ITicketScanRepository TicketScans => _ticketScans ??= new TicketScanRepository(_context);
        public IVisitorRatingRepository VisitorRatings => _visitorRatings ??= new VisitorRatingRepository(_context);
        public IFinancialReportRepository FinancialReports => _financialReports ??= new FinancialReportRepository(_context);
        public ICurrencyRepository Currencies => _currencies ??= new CurrencyRepository(_context);
        public IExchangeRateRepository ExchangeRates => _exchangeRates ??= new ExchangeRateRepository(_context);
        public IAuditLogRepository AuditLogs => _auditLogs ??= new AuditLogRepository(_context);
        public IExpenseRepository Expenses => _expenses ??= new ExpenseRepository(_context);

        public async Task<int> SaveChangesAsync()
        {
            var now = DateTime.UtcNow;

            // 1. Audit properties handling for IAuditableEntity
            foreach (var entry in _context.ChangeTracker.Entries<IAuditableEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = now;
                        entry.Entity.UpdatedAt = null;
                        break;
                    case EntityState.Modified:
                        entry.Property(x => x.CreatedAt).IsModified = false;
                        entry.Entity.UpdatedAt = now;
                        break;
                }
            }

            // Resolve context values from HTTP Request
            var httpContext = _httpContextAccessor.HttpContext;
            var userId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                userId = _context.Users.OrderBy(u => u.Id).Select(u => u.Id).FirstOrDefault();
            }

            var tenantIdClaim = httpContext?.User?.FindFirst("TenantId")?.Value;
            int.TryParse(tenantIdClaim, out var tenantId);

            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            if (ipAddress.Length > 50)
            {
                ipAddress = ipAddress.Substring(0, 50);
            }

            // 2. Build audit log entries pre-save (to capture modified states/values)
            var auditWrappers = new List<AuditEntryWrapper>();
            foreach (var entry in _context.ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog) 
                    continue; // Skip AuditLog entities to avoid recursive calls

                if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged) 
                    continue;

                // Determine TenantID for this entity's audit log
                var tenantIdProp = entry.Entity.GetType().GetProperty("TenantID");
                var entityTenantId = tenantIdProp != null ? (int?)tenantIdProp.GetValue(entry.Entity) : null;
                var logTenantId = entityTenantId ?? tenantId;
                if (logTenantId == 0)
                {
                    logTenantId = _context.Tenants.OrderBy(t => t.TenantID).Select(t => t.TenantID).FirstOrDefault();
                    if (logTenantId == 0) logTenantId = 1;
                }

                var auditLog = new AuditLog
                {
                    TenantID = logTenantId,
                    UserId = userId ?? string.Empty,
                    TableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name,
                    Action = entry.State.ToString(),
                    ActionAt = now,
                    RecordID = "0", // Temp value to satisfy compiler nullable check before post-save population
                    IPAddress = ipAddress,
                    OldValues = entry.State != EntityState.Added
                        ? JsonSerializer.Serialize(entry.Properties
                            .Where(p => entry.State == EntityState.Deleted || p.IsModified)
                            .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue?.ToString()))
                        : string.Empty,
                    NewValues = entry.State != EntityState.Deleted
                        ? JsonSerializer.Serialize(entry.Properties
                            .Where(p => entry.State == EntityState.Added || p.IsModified)
                            .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue?.ToString()))
                        : string.Empty
                };

                auditWrappers.Add(new AuditEntryWrapper { Entry = entry, AuditLog = auditLog });
            }

            // 3. Save actual entities
            var result = await _context.SaveChangesAsync();

            // 4. Save audit logs with resolved PK values
            if (auditWrappers.Any())
            {
                foreach (var wrapper in auditWrappers)
                {
                    wrapper.AuditLog.RecordID = GetPrimaryKeyValue(wrapper.Entry);
                }

                _context.AuditLogs.AddRange(auditWrappers.Select(w => w.AuditLog));
                await _context.SaveChangesAsync();
            }

            return result;
        }

        private string GetPrimaryKeyValue(EntityEntry entry)
        {
            var key = entry.Metadata.FindPrimaryKey();
            if (key == null) 
                return "0";

            var values = key.Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString());
            return string.Join(",", values);
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        private class AuditEntryWrapper
        {
            public EntityEntry Entry { get; set; } = null!;
            public AuditLog AuditLog { get; set; } = null!;
        }
    }
}
