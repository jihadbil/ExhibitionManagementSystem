# خطة بناء طبقة الوصول للبيانات (DAL)
# نظام إدارة المعارض

## نظرة عامة

بعد اكتمال طبقة النماذج بالكامل (45/45 مهمة ✅)، ننتقل إلى بناء طبقة الوصول للبيانات.  
الهدف هو بناء DAL **مرنة، قابلة للاختبار، وآمنة** تعتمد على أنماط معمارية راسخة.

### حالة طبقة النماذج (مدخلات الـ DAL)

| المكوّن | الحالة | الملاحظة |
|---|:---:|---|
| 33 نموذج (`Models/*.cs`) | ✅ مكتمل | جميعها تطبق `IAuditableEntity` و/أو `ISoftDeletable` |
| 14 Enum في `Enums/` | ✅ مكتمل | محوّلة إلى `string` في DB |
| `IAuditableEntity` | ✅ مكتمل | `CreatedAt`, `UpdatedAt` |
| `ISoftDeletable` | ✅ مكتمل | `IsDeleted`, `DeletedAt`, `DeletedByUserId` |
| `ApplicationDbContext` | ✅ مكتمل | Global Soft Delete Filter + Indexes + Constraints |

---

## المعمارية المقترحة

```
ExhibitionManagementSystem.DataAccess/
│
├── ApplicationDbContext.cs          ← موجود ✅
├── DesignTimeDbContextFactory.cs   ← موجود ✅
│
├── Repositories/
│   ├── Interfaces/
│   │   ├── IGenericRepository.cs
│   │   ├── IUnitOfWork.cs
│   │   ├── ITenantRepository.cs
│   │   ├── ITenantSubscriptionRepository.cs
│   │   ├── IVenueRepository.cs
│   │   ├── IHallRepository.cs
│   │   ├── IBoothRepository.cs
│   │   ├── IBoothMergeRepository.cs
│   │   ├── IExhibitionRepository.cs
│   │   ├── IExhibitionScheduleRepository.cs
│   │   ├── IScheduleRegistrationRepository.cs
│   │   ├── IExhibitorRepository.cs
│   │   ├── IBoothReservationRepository.cs
│   │   ├── IBoothStaffRepository.cs
│   │   ├── IProductRepository.cs
│   │   ├── IServiceRepository.cs
│   │   ├── IPricingPackageRepository.cs
│   │   ├── IBoothPriceRuleRepository.cs
│   │   ├── IServicePriceRuleRepository.cs
│   │   ├── IInvoiceRepository.cs
│   │   ├── IPaymentRepository.cs
│   │   ├── IVisitorRepository.cs
│   │   ├── ITicketRepository.cs
│   │   ├── ITicketScanRepository.cs
│   │   ├── IVisitorRatingRepository.cs
│   │   ├── IFinancialReportRepository.cs
│   │   ├── ICurrencyRepository.cs
│   │   ├── IExchangeRateRepository.cs
│   │   └── IAuditLogRepository.cs
│   │
│   └── Implementations/
│       ├── GenericRepository.cs
│       ├── UnitOfWork.cs
│       ├── TenantRepository.cs
│       ├── ... (27 Repository)
│       └── AuditLogRepository.cs
│
└── Extensions/
    └── DataAccessServiceExtensions.cs
```

---

## المرحلة 1 — IGenericRepository<T> (القاعدة)

**الملف:** `Repositories/Interfaces/IGenericRepository.cs`

### العمليات الأساسية

```csharp
public interface IGenericRepository<T> where T : class
{
    // استعلام
    Task<T?> GetByIdAsync(object id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

    // استعلام مع Navigation Properties
    Task<IReadOnlyList<T>> GetAllWithIncludesAsync(
        params Expression<Func<T, object>>[] includes);
    Task<T?> GetByIdWithIncludesAsync(
        object id,
        params Expression<Func<T, object>>[] includes);

    // ترقيم الصفحات
    Task<(IReadOnlyList<T> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, object>>? orderBy = null,
        bool descending = false);

    // كتابة
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);

    // Soft Delete
    Task SoftDeleteAsync(object id, string deletedByUserId);
    Task RestoreAsync(object id);
    Task<IReadOnlyList<T>> GetDeletedAsync();

    // Raw Queryable
    IQueryable<T> AsQueryable();
    IQueryable<T> AsQueryableIgnoringSoftDelete();
}
```

> [!IMPORTANT]
> `SoftDeleteAsync`/`RestoreAsync`/`GetDeletedAsync` تتحقق من `ISoftDeletable` — إذا لم يطبقه الكيان تُرمى `NotSupportedException`.

---

## المرحلة 2 — GenericRepository<T> (التطبيق الأساسي)

**الملف:** `Repositories/Implementations/GenericRepository.cs`

### نقاط التطبيق الحرجة

```csharp
public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    // GetByIdAsync: يستخدم FindAsync (cache-friendly)
    public async Task<T?> GetByIdAsync(object id)
        => await _dbSet.FindAsync(id);

    // GetAllAsync: يحترم Global Query Filter (Soft Delete)
    public async Task<IReadOnlyList<T>> GetAllAsync()
        => await _dbSet.AsNoTracking().ToListAsync();

    // AsQueryableIgnoringSoftDelete: يتجاهل الفلتر
    public IQueryable<T> AsQueryableIgnoringSoftDelete()
        => _dbSet.IgnoreQueryFilters().AsQueryable();

    // SoftDeleteAsync: يعدّل IsDeleted فقط دون حذف فعلي
    public async Task SoftDeleteAsync(object id, string deletedByUserId)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity is ISoftDeletable softDeletable)
        {
            softDeletable.IsDeleted = true;
            softDeletable.DeletedAt = DateTime.UtcNow;
            softDeletable.DeletedByUserId = deletedByUserId;
            _context.Entry(entity).State = EntityState.Modified;
        }
        else throw new NotSupportedException($"{typeof(T).Name} لا يدعم Soft Delete");
    }

    // GetPagedAsync: ترقيم صفحات موحد
    public async Task<(IReadOnlyList<T>, int)> GetPagedAsync(
        int pageNumber, int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, object>>? orderBy = null,
        bool descending = false)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();
        if (predicate != null) query = query.Where(predicate);
        var totalCount = await query.CountAsync();
        if (orderBy != null)
            query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, totalCount);
    }
}
```

---

## المرحلة 3 — IUnitOfWork و UnitOfWork

### الدور
- **IUnitOfWork**: نقطة وصول واحدة لجميع الـ Repositories
- **UnitOfWork**: يعالج `IAuditableEntity` تلقائياً **قبل** الحفظ في `SaveChangesAsync`

### IUnitOfWork

```csharp
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

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
```

### معالجة IAuditableEntity في UnitOfWork.SaveChangesAsync

```csharp
public async Task<int> SaveChangesAsync()
{
    var now = DateTime.UtcNow;

    foreach (var entry in _context.ChangeTracker.Entries<IAuditableEntity>())
    {
        switch (entry.State)
        {
            case EntityState.Added:
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = null;
                break;
            case EntityState.Modified:
                // لا تلمس CreatedAt عند التعديل
                entry.Property(x => x.CreatedAt).IsModified = false;
                entry.Entity.UpdatedAt = now;
                break;
        }
    }

    return await _context.SaveChangesAsync();
}
```

---

## المرحلة 4 — Repositories المخصصة

### 4.1 — ITenantRepository

```csharp
public interface ITenantRepository : IGenericRepository<Tenant>
{
    Task<Tenant?> GetBySubdomainAsync(string subdomain);
    Task<bool> IsSubdomainUniqueAsync(string subdomain, int? excludeId = null);
    Task<Tenant?> GetWithActiveSubscriptionAsync(int tenantId);
}
```

### 4.2 — ITenantSubscriptionRepository

```csharp
public interface ITenantSubscriptionRepository : IGenericRepository<TenantSubscription>
{
    Task<TenantSubscription?> GetActiveSubscriptionAsync(int tenantId);
    Task<IReadOnlyList<TenantSubscription>> GetByTenantAsync(int tenantId);
    Task<IReadOnlyList<TenantSubscription>> GetExpiringSubscriptionsAsync(int daysAhead);
}
```

### 4.3 — IVenueRepository

```csharp
public interface IVenueRepository : IGenericRepository<Venue>
{
    Task<IReadOnlyList<Venue>> GetByTenantAsync(int tenantId);
    Task<Venue?> GetWithHallsAsync(int venueId);
    Task<IReadOnlyList<Venue>> GetActiveVenuesAsync(int tenantId);
}
```

### 4.4 — IHallRepository

```csharp
public interface IHallRepository : IGenericRepository<Hall>
{
    Task<IReadOnlyList<Hall>> GetByVenueAsync(int venueId);
    Task<Hall?> GetWithBoothsAsync(int hallId);
    Task<IReadOnlyList<Hall>> GetAvailableHallsAsync(int venueId);
}
```

### 4.5 — IBoothRepository

```csharp
public interface IBoothRepository : IGenericRepository<Booth>
{
    Task<IReadOnlyList<Booth>> GetByHallAsync(int hallId);
    Task<IReadOnlyList<Booth>> GetAvailableBoothsAsync(int hallId);
    Task<IReadOnlyList<Booth>> GetByStatusAsync(int hallId, BoothStatus status);
    Task<Booth?> GetWithMergeInfoAsync(int boothId);
    Task<IReadOnlyList<Booth>> GetBoothsForFloorPlanAsync(int hallId);
}
```

### 4.6 — IBoothMergeRepository

```csharp
public interface IBoothMergeRepository : IGenericRepository<BoothMerge>
{
    Task<BoothMerge?> GetWithItemsAsync(int mergeId);
    Task<IReadOnlyList<BoothMerge>> GetByExhibitionAsync(int exhibitionId);
    Task<bool> HasActiveReservationAsync(int mergeId);
}
```

### 4.7 — IExhibitionRepository

```csharp
public interface IExhibitionRepository : IGenericRepository<Exhibition>
{
    Task<IReadOnlyList<Exhibition>> GetByTenantAsync(int tenantId);
    Task<IReadOnlyList<Exhibition>> GetByStatusAsync(int tenantId, ExhibitionStatus status);
    Task<Exhibition?> GetWithVenueAndSchedulesAsync(int exhibitionId);
    Task<IReadOnlyList<Exhibition>> GetActiveExhibitionsAsync(int tenantId);
    Task<IReadOnlyList<Exhibition>> GetUpcomingExhibitionsAsync(int tenantId, int count = 5);
    Task<Exhibition?> GetWithReservationsAndInvoicesAsync(int exhibitionId);
}
```

### 4.8 — IExhibitionScheduleRepository

```csharp
public interface IExhibitionScheduleRepository : IGenericRepository<ExhibitionSchedule>
{
    Task<IReadOnlyList<ExhibitionSchedule>> GetByExhibitionAsync(int exhibitionId);
    Task<IReadOnlyList<ExhibitionSchedule>> GetByHallAsync(int hallId);
    Task<IReadOnlyList<ExhibitionSchedule>> GetByDateRangeAsync(
        int exhibitionId, DateTime from, DateTime to);
}
```

### 4.9 — IScheduleRegistrationRepository

```csharp
public interface IScheduleRegistrationRepository : IGenericRepository<ScheduleRegistration>
{
    Task<IReadOnlyList<ScheduleRegistration>> GetByScheduleAsync(int scheduleId);
    Task<IReadOnlyList<ScheduleRegistration>> GetByVisitorAsync(int visitorId);
    Task<bool> IsVisitorRegisteredAsync(int scheduleId, int visitorId);
    Task<int> GetRegistrationCountAsync(int scheduleId);
}
```

### 4.10 — IExhibitorRepository

```csharp
public interface IExhibitorRepository : IGenericRepository<Exhibitor>
{
    Task<IReadOnlyList<Exhibitor>> GetByTenantAsync(int tenantId);
    Task<Exhibitor?> GetByUserIdAsync(string userId);
    Task<IReadOnlyList<Exhibitor>> GetByCategoryAsync(int tenantId, ExhibitorCategory category);
    Task<Exhibitor?> GetWithReservationsAsync(int exhibitorId);
    Task<bool> ExistsForUserAsync(string userId);
    Task<IReadOnlyList<Exhibitor>> SearchAsync(int tenantId, string searchTerm);
}
```

### 4.11 — IBoothReservationRepository (الأهم)

```csharp
public interface IBoothReservationRepository : IGenericRepository<BoothReservation>
{
    Task<IReadOnlyList<BoothReservation>> GetByExhibitionAsync(int exhibitionId);
    Task<IReadOnlyList<BoothReservation>> GetByExhibitorAsync(int exhibitorId);
    Task<IReadOnlyList<BoothReservation>> GetByStatusAsync(
        int exhibitionId, ReservationStatus status);
    Task<BoothReservation?> GetWithInvoiceAsync(int reservationId);
    Task<BoothReservation?> GetWithServicesAsync(int reservationId);
    Task<BoothReservation?> GetFullDetailAsync(int reservationId);
    Task<bool> IsBoothReservedAsync(int boothId, int exhibitionId);
    Task<bool> IsMergeReservedAsync(int mergeId, int exhibitionId);
    Task<decimal> GetTotalRevenueAsync(int exhibitionId);
    Task<IReadOnlyList<BoothReservation>> GetUnpaidReservationsAsync(int exhibitionId);
}
```

### 4.12 — IBoothStaffRepository

```csharp
public interface IBoothStaffRepository : IGenericRepository<BoothStaff>
{
    Task<IReadOnlyList<BoothStaff>> GetByReservationAsync(int reservationId);
    Task<IReadOnlyList<BoothStaff>> GetByExhibitorAsync(int exhibitorId);
}
```

### 4.13 — IProductRepository

```csharp
public interface IProductRepository : IGenericRepository<Product>
{
    Task<IReadOnlyList<Product>> GetByExhibitorAsync(int exhibitorId);
    Task<IReadOnlyList<Product>> GetByExhibitionAsync(int exhibitionId);
    Task<IReadOnlyList<Product>> SearchAsync(int tenantId, string searchTerm);
}
```

### 4.14 — IServiceRepository

```csharp
public interface IServiceRepository : IGenericRepository<Service>
{
    Task<IReadOnlyList<Service>> GetByTenantAsync(int tenantId);
    Task<IReadOnlyList<Service>> GetMandatoryServicesAsync(int tenantId);
    Task<IReadOnlyList<Service>> GetByCategoryAsync(int tenantId, string category);
    Task<Service?> GetWithPriceRulesAsync(int serviceId);
}
```

### 4.15 — IPricingPackageRepository

```csharp
public interface IPricingPackageRepository : IGenericRepository<PricingPackage>
{
    Task<IReadOnlyList<PricingPackage>> GetByTenantAsync(int tenantId);
    Task<IReadOnlyList<PricingPackage>> GetActivePackagesAsync(int tenantId);
    Task<PricingPackage?> GetWithServicesAsync(int packageId);
}
```

### 4.16 — IBoothPriceRuleRepository

```csharp
public interface IBoothPriceRuleRepository : IGenericRepository<BoothPriceRule>
{
    Task<IReadOnlyList<BoothPriceRule>> GetByTenantAsync(int tenantId);
    Task<IReadOnlyList<BoothPriceRule>> GetByExhibitionAsync(int exhibitionId);
    Task<BoothPriceRule?> GetApplicableRuleAsync(
        int tenantId, int? exhibitionId,
        BoothType? boothType, ExhibitorCategory? category,
        decimal areaSqM, DateTime date);
}
```

### 4.17 — IServicePriceRuleRepository

```csharp
public interface IServicePriceRuleRepository : IGenericRepository<ServicePriceRule>
{
    Task<IReadOnlyList<ServicePriceRule>> GetByServiceAsync(int serviceId);
    Task<IReadOnlyList<ServicePriceRule>> GetByExhibitionAsync(int exhibitionId);
    Task<ServicePriceRule?> GetApplicableRuleAsync(
        int serviceId, int? exhibitionId,
        ExhibitorCategory? category, DateTime date);
}
```

### 4.18 — IInvoiceRepository

```csharp
public interface IInvoiceRepository : IGenericRepository<Invoice>
{
    Task<Invoice?> GetByReservationAsync(int reservationId);
    Task<Invoice?> GetByNumberAsync(int tenantId, string invoiceNumber);
    Task<IReadOnlyList<Invoice>> GetByStatusAsync(int tenantId, InvoiceStatus status);
    Task<Invoice?> GetWithPaymentsAsync(int invoiceId);
    Task<IReadOnlyList<Invoice>> GetOverdueInvoicesAsync(int tenantId);
    Task<string> GenerateNextInvoiceNumberAsync(int tenantId);
}
```

### 4.19 — IPaymentRepository

```csharp
public interface IPaymentRepository : IGenericRepository<Payment>
{
    Task<IReadOnlyList<Payment>> GetByInvoiceAsync(int invoiceId);
    Task<decimal> GetTotalPaidAsync(int invoiceId);
    Task<IReadOnlyList<Payment>> GetByDateRangeAsync(
        int tenantId, DateTime from, DateTime to);
}
```

### 4.20 — IVisitorRepository

```csharp
public interface IVisitorRepository : IGenericRepository<Visitor>
{
    Task<IReadOnlyList<Visitor>> GetByTenantAsync(int tenantId);
    Task<Visitor?> GetByUserIdAsync(string userId);
    Task<Visitor?> GetByEmailAsync(int tenantId, string email);
    Task<IReadOnlyList<Visitor>> SearchAsync(int tenantId, string searchTerm);
    Task<Visitor?> GetWithTicketsAsync(int visitorId);
}
```

### 4.21 — ITicketRepository

```csharp
public interface ITicketRepository : IGenericRepository<Ticket>
{
    Task<Ticket?> GetByQRCodeAsync(string qrCode);
    Task<IReadOnlyList<Ticket>> GetByVisitorAsync(int visitorId);
    Task<IReadOnlyList<Ticket>> GetByExhibitionAsync(int exhibitionId);
    Task<IReadOnlyList<Ticket>> GetByStatusAsync(int exhibitionId, TicketStatus status);
    Task<bool> IsQRCodeUniqueAsync(string qrCode);
    Task<int> GetActiveTicketCountAsync(int exhibitionId);
}
```

### 4.22 — ITicketScanRepository

```csharp
public interface ITicketScanRepository : IGenericRepository<TicketScan>
{
    Task<IReadOnlyList<TicketScan>> GetByTicketAsync(int ticketId);
    Task<TicketScan?> GetLastScanAsync(int ticketId);
    Task<int> GetTodayScansCountAsync(int exhibitionId);
}
```

### 4.23 — IVisitorRatingRepository

```csharp
public interface IVisitorRatingRepository : IGenericRepository<VisitorRating>
{
    Task<IReadOnlyList<VisitorRating>> GetByExhibitionAsync(int exhibitionId);
    Task<IReadOnlyList<VisitorRating>> GetByExhibitorAsync(int exhibitorId);
    Task<double> GetAverageRatingAsync(int exhibitionId);
    Task<bool> HasVisitorRatedAsync(int visitorId, int exhibitionId);
}
```

### 4.24 — IFinancialReportRepository

```csharp
public interface IFinancialReportRepository : IGenericRepository<FinancialReport>
{
    Task<IReadOnlyList<FinancialReport>> GetByTenantAsync(int tenantId);
    Task<IReadOnlyList<FinancialReport>> GetByExhibitionAsync(int exhibitionId);
    Task<FinancialReport?> GetLatestReportAsync(int tenantId);
}
```

### 4.25 — ICurrencyRepository

```csharp
public interface ICurrencyRepository : IGenericRepository<Currency>
{
    Task<Currency?> GetByCodeAsync(string code);
    Task<bool> IsCodeUniqueAsync(string code, string? excludeCode = null);
    Task<IReadOnlyList<Currency>> GetActiveAsync();
}
```

### 4.26 — IExchangeRateRepository

```csharp
public interface IExchangeRateRepository : IGenericRepository<ExchangeRate>
{
    Task<ExchangeRate?> GetRateAsync(string from, string to, DateTime date);
    Task<ExchangeRate?> GetLatestRateAsync(string from, string to);
    Task<decimal> ConvertAsync(string from, string to, decimal amount, DateTime? date = null);
}
```

### 4.27 — IAuditLogRepository

```csharp
public interface IAuditLogRepository : IGenericRepository<AuditLog>
{
    Task<IReadOnlyList<AuditLog>> GetByTableAsync(int tenantId, string tableName);
    Task<IReadOnlyList<AuditLog>> GetByRecordAsync(
        int tenantId, string tableName, string recordId);
    Task<IReadOnlyList<AuditLog>> GetByUserAsync(int tenantId, string userId);
    Task<IReadOnlyList<AuditLog>> GetByDateRangeAsync(
        int tenantId, DateTime from, DateTime to);
    Task<int> DeleteOlderThanAsync(int tenantId, DateTime cutoffDate);
}
```

---

## المرحلة 5 — DI Registration

**الملف:** `Extensions/DataAccessServiceExtensions.cs`

> [!NOTE]
> بناءً على القرار #1، الـ DI يسجّل **`IUnitOfWork` فقط**.
> كل الـ Repositories تُبنى داخل `UnitOfWork` وتُعيَّن بـ lazy initialization.

```csharp
public static class DataAccessServiceExtensions
{
    public static IServiceCollection AddDataAccess(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // نقطة تسجيل واحدة فقط — كل الـ Repos تُدار من داخل UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
```

### UnitOfWork — Lazy Initialization للـ Repositories

```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    // Lazy fields — تُبنى عند الطلب الأول فقط
    private ITenantRepository? _tenants;
    private IVenueRepository? _venues;
    // ... باقي الـ Repositories

    public ITenantRepository Tenants
        => _tenants ??= new TenantRepository(_context);
    public IVenueRepository Venues
        => _venues ??= new VenueRepository(_context);
    // ... إلخ

    public async Task<int> SaveChangesAsync()
    {
        var now = DateTime.UtcNow;

        // 1. معالجة IAuditableEntity
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

        // 2. تسجيل AuditLog أوتوماتيكي (القرار #2)
        var auditEntries = BuildAuditEntries(now);

        var result = await _context.SaveChangesAsync();

        // إضافة الـ AuditLogs بعد الحفظ (لأن IDs تُولَّد بعده)
        if (auditEntries.Any())
        {
            _context.AuditLogs.AddRange(auditEntries);
            await _context.SaveChangesAsync();
        }

        return result;
    }

    private IEnumerable<AuditLog> BuildAuditEntries(DateTime now)
    {
        var entries = new List<AuditLog>();
        foreach (var entry in _context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog) continue; // تجنب loop لانهائي
            if (entry.State is not (EntityState.Added
                or EntityState.Modified or EntityState.Deleted)) continue;

            entries.Add(new AuditLog
            {
                TableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name,
                RecordID  = GetPrimaryKeyValue(entry),
                Action    = entry.State.ToString(), // Added / Modified / Deleted
                OldValues = entry.State != EntityState.Added
                    ? JsonSerializer.Serialize(entry.Properties
                        .Where(p => p.IsModified)
                        .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue?.ToString()))
                    : null,
                NewValues = entry.State != EntityState.Deleted
                    ? JsonSerializer.Serialize(entry.Properties
                        .Where(p => p.IsModified)
                        .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue?.ToString()))
                    : null,
                ActionAt = now,
                // TenantID و UserId تُحقن عبر ICurrentUserService (يُضاف لاحقاً)
            });
        }
        return entries;
    }
}
```

---

## ترتيب التنفيذ

```
المرحلة 1 → IGenericRepository.cs + GenericRepository.cs
المرحلة 2 → IUnitOfWork.cs + UnitOfWork.cs
المرحلة 3 → Repos بسيطة: Currency, ExchangeRate, AuditLog, Tenant, TenantSubscription
المرحلة 4 → Repos الجغرافية: Venue, Hall, Booth, BoothMerge
المرحلة 5 → Repos المعرض: Exhibition, ExhibitionSchedule, ScheduleRegistration
المرحلة 6 → Repos التشغيلية: Exhibitor, BoothReservation, BoothStaff, Product
المرحلة 7 → Repos التسعير: Service, PricingPackage, BoothPriceRule, ServicePriceRule
المرحلة 8 → Repos المالية: Invoice, Payment, FinancialReport
المرحلة 9 → Repos الزوار: Visitor, Ticket, TicketScan, VisitorRating
المرحلة 10 → DataAccessServiceExtensions.cs
المرحلة 11 → dotnet build + التحقق
```

---

## خلاصة الملفات

### ملفات جديدة [NEW]

| النوع | العدد |
|---|:---:|
| Repository Interfaces (`Repositories/Interfaces/`) | 29 |
| Repository Implementations (`Repositories/Implementations/`) | 29 |
| Extension Methods (`Extensions/`) | 1 |
| **المجموع** | **59** |

### ملفات موجودة (لا تعديل مطلوب)

| الملف | السبب |
|---|---|
| `ApplicationDbContext.cs` | مكتمل ✅ |
| `DesignTimeDbContextFactory.cs` | مكتمل ✅ |

---

## القرارات التصميمية المعتمدة

> [!NOTE]
> **القرار #1 — نقطة وصول واحدة عبر `IUnitOfWork`:**
> جميع الـ Repositories تُسجَّل **داخل `UnitOfWork` فقط** ولا تُحقن مباشرة من الـ DI Container.
> الطبقات العليا (Services) تتعامل حصراً مع `IUnitOfWork` للوصول لأي Repository.

> [!NOTE]
> **القرار #2 — `AuditLog` أوتوماتيكي:**
> كل `INSERT` / `UPDATE` / `DELETE` على أي كيان يُسجَّل تلقائياً في جدول `AuditLogs`
> عبر `UnitOfWork.SaveChangesAsync` باستخدام `ChangeTracker`.
> لا حاجة لاستدعاء يدوي من الـ Services.

> [!NOTE]
> **بخصوص `GetApplicableRuleAsync`** في `BoothPriceRule` و`ServicePriceRule`:
> المنطق التجاري لاختيار القاعدة الأنسب (الأولوية حسب Exhibition ثم Tenant) سيبقى في الـ DAL كاستعلام بحت، ويُستخدم من طبقة الخدمات.

---

## خطة التحقق

```bash
# بعد كل مرحلة:
dotnet build ExhibitionManagementSystem.DataAccess

# التحقق النهائي:
dotnet build  # كامل الـ Solution بدون أخطاء أو Warnings
```

> [!WARNING]
> لا نحتاج Migration جديدة — طبقة الـ DAL لا تغير مخطط قاعدة البيانات.
