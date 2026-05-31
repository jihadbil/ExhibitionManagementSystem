# ✅ ملف المهام — بناء طبقة الوصول للبيانات (DAL)

> **الحالة الإجمالية:** 🎉 مكتمل بالكامل (اكتملت جميع المراحل 1 إلى 11)
> **إجمالي المهام:** ~120 مهمة (11 مرحلة)

---

## 📋 القرارات التصميمية المعتمدة

- **القرار #1 — UnitOfWork كمصدر وحيد:** الـ DI يسجّل `IUnitOfWork` فقط. كل الـ Repos تُدار داخله بـ Lazy Initialization.
- **القرار #2 — AuditLog أوتوماتيكي:** كل INSERT/UPDATE/DELETE يُسجَّل تلقائياً عبر `ChangeTracker` في `SaveChangesAsync`.

---

## 🟢 المرحلة 1 — إعداد هيكل المجلدات

> **التبعيات:** لا توجد — تبدأ أولاً

- [x] **1.1** إنشاء مجلد `Repositories/Interfaces/` داخل `ExhibitionManagementSystem.DataAccess`
- [x] **1.2** إنشاء مجلد `Repositories/Implementations/` داخل `ExhibitionManagementSystem.DataAccess`
- [x] **1.3** إنشاء مجلد `Extensions/` داخل `ExhibitionManagementSystem.DataAccess`

---

## 🔵 المرحلة 2 — IGenericRepository<T> و GenericRepository<T>

> **التبعيات:** المرحلة 1 مكتملة
> **الملفات:** `Repositories/Interfaces/IGenericRepository.cs` و`Repositories/Implementations/GenericRepository.cs`

### 2.1 — إنشاء `IGenericRepository<T>`

- [x] **2.1.1** إنشاء الملف `Repositories/Interfaces/IGenericRepository.cs`
- [x] **2.1.2** تعريف `namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces`
- [x] **2.1.3** إضافة `using` اللازمة: `System.Linq.Expressions`، `ExhibitionManagementSystem.Models.Interfaces`
- [x] **2.1.4** إضافة method `Task<T?> GetByIdAsync(object id)`
- [x] **2.1.5** إضافة method `Task<IReadOnlyList<T>> GetAllAsync()`
- [x] **2.1.6** إضافة method `Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate)`
- [x] **2.1.7** إضافة method `Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)`
- [x] **2.1.8** إضافة method `Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)`
- [x] **2.1.9** إضافة method `Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)`
- [x] **2.1.10** إضافة method `Task<IReadOnlyList<T>> GetAllWithIncludesAsync(params Expression<Func<T, object>>[] includes)`
- [x] **2.1.11** إضافة method `Task<T?> GetByIdWithIncludesAsync(object id, params Expression<Func<T, object>>[] includes)`
- [x] **2.1.12** إضافة method `Task<(IReadOnlyList<T> Items, int TotalCount)> GetPagedAsync(...)`
- [x] **2.1.13** إضافة methods الكتابة: `AddAsync`, `AddRangeAsync`, `Update`, `UpdateRange`, `Remove`, `RemoveRange`
- [x] **2.1.14** إضافة methods الـ Soft Delete: `SoftDeleteAsync(object id, string deletedByUserId)`, `RestoreAsync(object id)`, `GetDeletedAsync()`
- [x] **2.1.15** إضافة methods الـ Raw Queryable: `AsQueryable()`, `AsQueryableIgnoringSoftDelete()`

### 2.2 — إنشاء `GenericRepository<T>`

- [x] **2.2.1** إنشاء الملف `Repositories/Implementations/GenericRepository.cs`
- [x] **2.2.2** تعريف `namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations`
- [x] **2.2.3** إضافة `using` اللازمة: EF Core، Interfaces، Models.Interfaces
- [x] **2.2.4** تعريف الـ Constructor: `(ApplicationDbContext context)` مع تعيين `_dbSet`
- [x] **2.2.5** تطبيق `GetByIdAsync` — يستخدم `_dbSet.FindAsync(id)` (cache-friendly)
- [x] **2.2.6** تطبيق `GetAllAsync` — يستخدم `AsNoTracking().ToListAsync()`
- [x] **2.2.7** تطبيق `FindAsync` — `_dbSet.AsNoTracking().Where(predicate).ToListAsync()`
- [x] **2.2.8** تطبيق `FirstOrDefaultAsync`، `ExistsAsync`، `CountAsync`
- [x] **2.2.9** تطبيق `GetAllWithIncludesAsync` — يُطبّق `.Include()` على كل عنصر في `includes`
- [x] **2.2.10** تطبيق `GetByIdWithIncludesAsync`
- [x] **2.2.11** تطبيق `GetPagedAsync` — Skip/Take مع OrderBy اختياري
- [x] **2.2.12** تطبيق `AddAsync`، `AddRangeAsync`، `Update`، `UpdateRange`، `Remove`، `RemoveRange`
- [x] **2.2.13** تطبيق `SoftDeleteAsync`:
  - التحقق من `entity is ISoftDeletable`
  - تعيين `IsDeleted = true`، `DeletedAt = DateTime.UtcNow`، `DeletedByUserId`
  - رمي `NotSupportedException` إذا لم يطبق `ISoftDeletable`
- [x] **2.2.14** تطبيق `RestoreAsync`:
  - استخدام `IgnoreQueryFilters()` للوصول للسجل المحذوف
  - تعيين `IsDeleted = false`، `DeletedAt = null`، `DeletedByUserId = null`
- [x] **2.2.15** تطبيق `GetDeletedAsync` — `_dbSet.IgnoreQueryFilters().Where(IsDeleted == true)`
- [x] **2.2.16** تطبيق `AsQueryable()` و `AsQueryableIgnoringSoftDelete()`

### 2.3 — التحقق

- [x] **2.3.1** `dotnet build ExhibitionManagementSystem.DataAccess` ✅ بدون أخطاء

---

## 🔵 المرحلة 3 — IUnitOfWork و UnitOfWork

> **التبعيات:** المرحلة 2 مكتملة
> **الملفات:** `Repositories/Interfaces/IUnitOfWork.cs` و`Repositories/Implementations/UnitOfWork.cs`

### 3.1 — إنشاء `IUnitOfWork`

- [x] **3.1.1** إنشاء الملف `Repositories/Interfaces/IUnitOfWork.cs`
- [x] **3.1.2** إضافة property `ITenantRepository Tenants { get; }`
- [x] **3.1.3** إضافة property `ITenantSubscriptionRepository TenantSubscriptions { get; }`
- [x] **3.1.4** إضافة property `IVenueRepository Venues { get; }`
- [x] **3.1.5** إضافة property `IHallRepository Halls { get; }`
- [x] **3.1.6** إضافة property `IBoothRepository Booths { get; }`
- [x] **3.1.7** إضافة property `IBoothMergeRepository BoothMerges { get; }`
- [x] **3.1.8** إضافة property `IExhibitionRepository Exhibitions { get; }`
- [x] **3.1.9** إضافة property `IExhibitionScheduleRepository ExhibitionSchedules { get; }`
- [x] **3.1.10** إضافة property `IScheduleRegistrationRepository ScheduleRegistrations { get; }`
- [x] **3.1.11** إضافة property `IExhibitorRepository Exhibitors { get; }`
- [x] **3.1.12** إضافة property `IBoothReservationRepository BoothReservations { get; }`
- [x] **3.1.13** إضافة property `IBoothStaffRepository BoothStaffs { get; }`
- [x] **3.1.14** إضافة property `IProductRepository Products { get; }`
- [x] **3.1.15** إضافة property `IServiceRepository Services { get; }`
- [x] **3.1.16** إضافة property `IPricingPackageRepository PricingPackages { get; }`
- [x] **3.1.17** إضافة property `IBoothPriceRuleRepository BoothPriceRules { get; }`
- [x] **3.1.18** إضافة property `IServicePriceRuleRepository ServicePriceRules { get; }`
- [x] **3.1.19** إضافة property `IInvoiceRepository Invoices { get; }`
- [x] **3.1.20** إضافة property `IPaymentRepository Payments { get; }`
- [x] **3.1.21** إضافة property `IVisitorRepository Visitors { get; }`
- [x] **3.1.22** إضافة property `ITicketRepository Tickets { get; }`
- [x] **3.1.23** إضافة property `ITicketScanRepository TicketScans { get; }`
- [x] **3.1.24** إضافة property `IVisitorRatingRepository VisitorRatings { get; }`
- [x] **3.1.25** إضافة property `IFinancialReportRepository FinancialReports { get; }`
- [x] **3.1.26** إضافة property `ICurrencyRepository Currencies { get; }`
- [x] **3.1.27** إضافة property `IExchangeRateRepository ExchangeRates { get; }`
- [x] **3.1.28** إضافة property `IAuditLogRepository AuditLogs { get; }`
- [x] **3.1.29** إضافة method `Task<int> SaveChangesAsync()`
- [x] **3.1.30** إضافة methods `BeginTransactionAsync()`, `CommitTransactionAsync()`, `RollbackTransactionAsync()`
- [x] **3.1.31** إضافة `IDisposable` للـ Interface

### 3.2 — إنشاء `UnitOfWork`

- [x] **3.2.1** إنشاء الملف `Repositories/Implementations/UnitOfWork.cs`
- [x] **3.2.2** إضافة `using` اللازمة: EF Core، System.Text.Json، Interfaces، Models، Models.Interfaces
- [x] **3.2.3** تعريف `private readonly ApplicationDbContext _context` و `private IDbContextTransaction? _transaction`
- [x] **3.2.4** إضافة 27 حقل Lazy (nullable) لكل Repository: `private ITenantRepository? _tenants;` وهكذا
- [x] **3.2.5** تطبيق 27 property بـ null-coalescing:
  ```
  public ITenantRepository Tenants => _tenants ??= new TenantRepository(_context);
  ```
- [x] **3.2.6** تطبيق `SaveChangesAsync()` بثلاث خطوات:
  - **الخطوة A:** loop على `ChangeTracker.Entries<IAuditableEntity>()` — تعيين `CreatedAt`/`UpdatedAt`
  - **الخطوة B:** استدعاء `BuildAuditEntries(now)` قبل `SaveChangesAsync` الحقيقي
  - **الخطوة C:** حفظ، ثم إضافة AuditLogs وحفظ مرة ثانية
- [x] **3.2.7** تطبيق method `BuildAuditEntries(DateTime now)`:
  - Skip `AuditLog` entities (لتجنب الـ loop اللانهائي)
  - Skip `EntityState.Unchanged` و`EntityState.Detached`
  - بناء `OldValues` من `entry.Properties.Where(p => p.IsModified).OriginalValue`
  - بناء `NewValues` من `entry.Properties.Where(p => p.IsModified).CurrentValue`
  - تعيين `TableName`, `RecordID`, `Action`, `ActionAt`
  - ملاحظة: `TenantID` و`UserId` تُضاف لاحقاً عبر `ICurrentUserService`
- [x] **3.2.8** تطبيق method مساعدة `GetPrimaryKeyValue(EntityEntry entry)` — يستخرج قيمة الـ PK
- [x] **3.2.9** تطبيق `BeginTransactionAsync()` — `_transaction = await _context.Database.BeginTransactionAsync()`
- [x] **3.2.10** تطبيق `CommitTransactionAsync()` و `RollbackTransactionAsync()`
- [x] **3.2.11** تطبيق `Dispose()` — يُتلف `_transaction` و `_context`

### 3.3 — التحقق

- [x] **3.3.1** `dotnet build ExhibitionManagementSystem.DataAccess` ✅ (ستظهر أخطاء بسبب الـ Repos المفقودة — طبيعي في هذه المرحلة)

---

## 🟢 المرحلة 4 — Repositories البسيطة (البنية التحتية)

> **التبعيات:** المرحلة 2 مكتملة
> **Repositories:** Currency, ExchangeRate, AuditLog, Tenant, TenantSubscription

### 4.1 — `ICurrencyRepository` / `CurrencyRepository`

- [x] **4.1.1** إنشاء `Repositories/Interfaces/ICurrencyRepository.cs`
  - `Task<Currency?> GetByCodeAsync(string code)`
  - `Task<bool> IsCodeUniqueAsync(string code, string? excludeCode = null)`
  - `Task<IReadOnlyList<Currency>> GetActiveAsync()`
- [x] **4.1.2** إنشاء `Repositories/Implementations/CurrencyRepository.cs`
  - تطبيق `GetByCodeAsync` — `FirstOrDefaultAsync(c => c.Code == code)`
  - تطبيق `IsCodeUniqueAsync`
  - تطبيق `GetActiveAsync` — `FindAsync(c => c.IsActive)`

### 4.2 — `IExchangeRateRepository` / `ExchangeRateRepository`

- [x] **4.2.1** إنشاء `Repositories/Interfaces/IExchangeRateRepository.cs`
  - `Task<ExchangeRate?> GetRateAsync(string from, string to, DateTime date)`
  - `Task<ExchangeRate?> GetLatestRateAsync(string from, string to)`
  - `Task<decimal> ConvertAsync(string from, string to, decimal amount, DateTime? date = null)`
- [x] **4.2.2** إنشاء `Repositories/Implementations/ExchangeRateRepository.cs`
  - `GetRateAsync` — يبحث بأقرب تاريخ ≤ `date`
  - `GetLatestRateAsync` — `OrderByDescending(r => r.RateDate).FirstOrDefault`
  - `ConvertAsync` — يستخدم `GetLatestRateAsync` ثم يضرب `amount * rate.Rate`

### 4.3 — `IAuditLogRepository` / `AuditLogRepository`

- [x] **4.3.1** إنشاء `Repositories/Interfaces/IAuditLogRepository.cs`
  - `Task<IReadOnlyList<AuditLog>> GetByTableAsync(int tenantId, string tableName)`
  - `Task<IReadOnlyList<AuditLog>> GetByRecordAsync(int tenantId, string tableName, string recordId)`
  - `Task<IReadOnlyList<AuditLog>> GetByUserAsync(int tenantId, string userId)`
  - `Task<IReadOnlyList<AuditLog>> GetByDateRangeAsync(int tenantId, DateTime from, DateTime to)`
  - `Task<int> DeleteOlderThanAsync(int tenantId, DateTime cutoffDate)`
- [x] **4.3.2** إنشاء `Repositories/Implementations/AuditLogRepository.cs`
  - `DeleteOlderThanAsync` — `ExecuteDeleteAsync()` لحذف الدفعي بدون تحميل في الذاكرة

### 4.4 — `ITenantRepository` / `TenantRepository`

- [x] **4.4.1** إنشاء `Repositories/Interfaces/ITenantRepository.cs`
  - `Task<Tenant?> GetBySubdomainAsync(string subdomain)`
  - `Task<bool> IsSubdomainUniqueAsync(string subdomain, int? excludeId = null)`
  - `Task<Tenant?> GetWithActiveSubscriptionAsync(int tenantId)`
- [x] **4.4.2** إنشاء `Repositories/Implementations/TenantRepository.cs`
  - `GetWithActiveSubscriptionAsync` — Include TenantSubscriptions + Where Active

### 4.5 — `ITenantSubscriptionRepository` / `TenantSubscriptionRepository`

- [x] **4.5.1** إنشاء `Repositories/Interfaces/ITenantSubscriptionRepository.cs`
  - `Task<TenantSubscription?> GetActiveSubscriptionAsync(int tenantId)`
  - `Task<IReadOnlyList<TenantSubscription>> GetByTenantAsync(int tenantId)`
  - `Task<IReadOnlyList<TenantSubscription>> GetExpiringSubscriptionsAsync(int daysAhead)`
- [x] **4.5.2** إنشاء `Repositories/Implementations/TenantSubscriptionRepository.cs`
  - `GetExpiringSubscriptionsAsync` — `EndDate <= DateTime.UtcNow.AddDays(daysAhead)`

### 4.6 — التحقق

- [x] **4.6.1** `dotnet build ExhibitionManagementSystem.DataAccess` ✅ (أخطاء Repos المفقودة فقط)

---

## 🟢 المرحلة 5 — Repositories الجغرافية

> **التبعيات:** المرحلة 2 مكتملة
> **Repositories:** Venue, Hall, Booth, BoothMerge

### 5.1 — `IVenueRepository` / `VenueRepository`

- [x] **5.1.1** إنشاء `Repositories/Interfaces/IVenueRepository.cs`
  - `Task<IReadOnlyList<Venue>> GetByTenantAsync(int tenantId)`
  - `Task<Venue?> GetWithHallsAsync(int venueId)`
  - `Task<IReadOnlyList<Venue>> GetActiveVenuesAsync(int tenantId)`
- [x] **5.1.2** إنشاء `Repositories/Implementations/VenueRepository.cs`
  - `GetWithHallsAsync` — Include Halls (غير المحذوفة)

### 5.2 — `IHallRepository` / `HallRepository`

- [x] **5.2.1** إنشاء `Repositories/Interfaces/IHallRepository.cs`
  - `Task<IReadOnlyList<Hall>> GetByVenueAsync(int venueId)`
  - `Task<Hall?> GetWithBoothsAsync(int hallId)`
  - `Task<IReadOnlyList<Hall>> GetAvailableHallsAsync(int venueId)`
- [x] **5.2.2** إنشاء `Repositories/Implementations/HallRepository.cs`
  - `GetWithBoothsAsync` — Include Booths + IgnoreQueryFilters للمحذوف اختياري

### 5.3 — `IBoothRepository` / `BoothRepository`

- [x] **5.3.1** إنشاء `Repositories/Interfaces/IBoothRepository.cs`
  - `Task<IReadOnlyList<Booth>> GetByHallAsync(int hallId)`
  - `Task<IReadOnlyList<Booth>> GetAvailableBoothsAsync(int hallId)`
  - `Task<IReadOnlyList<Booth>> GetByStatusAsync(int hallId, BoothStatus status)`
  - `Task<Booth?> GetWithMergeInfoAsync(int boothId)`
  - `Task<IReadOnlyList<Booth>> GetBoothsForFloorPlanAsync(int hallId)`
- [x] **5.3.2** إنشاء `Repositories/Implementations/BoothRepository.cs`
  - `GetAvailableBoothsAsync` — `Status == BoothStatus.Available && !IsMerged`
  - `GetBoothsForFloorPlanAsync` — يُرجع جميع الأكشاك بما فيها المحجوزة (للرسم)

### 5.4 — `IBoothMergeRepository` / `BoothMergeRepository`

- [x] **5.4.1** إنشاء `Repositories/Interfaces/IBoothMergeRepository.cs`
  - `Task<BoothMerge?> GetWithItemsAsync(int mergeId)`
  - `Task<IReadOnlyList<BoothMerge>> GetByExhibitionAsync(int exhibitionId)`
  - `Task<bool> HasActiveReservationAsync(int mergeId)`
- [x] **5.4.2** إنشاء `Repositories/Implementations/BoothMergeRepository.cs`
  - `GetWithItemsAsync` — Include MergeItems + Include Booth

### 5.5 — التحقق

- [x] **5.5.1** `dotnet build ExhibitionManagementSystem.DataAccess` ✅

---

## 🟢 المرحلة 6 — Repositories المعرض

> **التبعيات:** المرحلة 2 مكتملة
> **Repositories:** Exhibition, ExhibitionSchedule, ScheduleRegistration

### 6.1 — `IExhibitionRepository` / `ExhibitionRepository`

- [x] **6.1.1** إنشاء `Repositories/Interfaces/IExhibitionRepository.cs`
  - `Task<IReadOnlyList<Exhibition>> GetByTenantAsync(int tenantId)`
  - `Task<IReadOnlyList<Exhibition>> GetByStatusAsync(int tenantId, ExhibitionStatus status)`
  - `Task<Exhibition?> GetWithVenueAndSchedulesAsync(int exhibitionId)`
  - `Task<IReadOnlyList<Exhibition>> GetActiveExhibitionsAsync(int tenantId)`
  - `Task<IReadOnlyList<Exhibition>> GetUpcomingExhibitionsAsync(int tenantId, int count = 5)`
  - `Task<Exhibition?> GetWithReservationsAndInvoicesAsync(int exhibitionId)`
- [x] **6.1.2** إنشاء `Repositories/Implementations/ExhibitionRepository.cs`
  - `GetActiveExhibitionsAsync` — Status In (Planning, Open, Running)
  - `GetUpcomingExhibitionsAsync` — `StartDate > DateTime.UtcNow`, OrderBy StartDate, Take count
  - `GetWithReservationsAndInvoicesAsync` — Include BoothReservations + ThenInclude Invoice

### 6.2 — `IExhibitionScheduleRepository` / `ExhibitionScheduleRepository`

- [x] **6.2.1** إنشاء `Repositories/Interfaces/IExhibitionScheduleRepository.cs`
  - `Task<IReadOnlyList<ExhibitionSchedule>> GetByExhibitionAsync(int exhibitionId)`
  - `Task<IReadOnlyList<ExhibitionSchedule>> GetByHallAsync(int hallId)`
  - `Task<IReadOnlyList<ExhibitionSchedule>> GetByDateRangeAsync(int exhibitionId, DateTime from, DateTime to)`
- [x] **6.2.2** إنشاء `Repositories/Implementations/ExhibitionScheduleRepository.cs`

### 6.3 — `IScheduleRegistrationRepository` / `ScheduleRegistrationRepository`

- [x] **6.3.1** إنشاء `Repositories/Interfaces/IScheduleRegistrationRepository.cs`
  - `Task<IReadOnlyList<ScheduleRegistration>> GetByScheduleAsync(int scheduleId)`
  - `Task<IReadOnlyList<ScheduleRegistration>> GetByVisitorAsync(int visitorId)`
  - `Task<bool> IsVisitorRegisteredAsync(int scheduleId, int visitorId)`
  - `Task<int> GetRegistrationCountAsync(int scheduleId)`
- [x] **6.3.2** إنشاء `Repositories/Implementations/ScheduleRegistrationRepository.cs`
  - `IsVisitorRegisteredAsync` — `ExistsAsync(r => r.ScheduleID == scheduleId && r.VisitorID == visitorId)`

### 6.4 — التحقق

- [x] **6.4.1** `dotnet build ExhibitionManagementSystem.DataAccess` ✅

---

## 🟢 المرحلة 7 — Repositories التشغيلية

> **التبعيات:** المرحلة 2 مكتملة
> **Repositories:** Exhibitor, BoothReservation, BoothStaff, Product

### 7.1 — `IExhibitorRepository` / `ExhibitorRepository`

- [x] **7.1.1** إنشاء `Repositories/Interfaces/IExhibitorRepository.cs`
  - `Task<IReadOnlyList<Exhibitor>> GetByTenantAsync(int tenantId)`
  - `Task<Exhibitor?> GetByUserIdAsync(string userId)`
  - `Task<IReadOnlyList<Exhibitor>> GetByCategoryAsync(int tenantId, ExhibitorCategory category)`
  - `Task<Exhibitor?> GetWithReservationsAsync(int exhibitorId)`
  - `Task<bool> ExistsForUserAsync(string userId)`
  - `Task<IReadOnlyList<Exhibitor>> SearchAsync(int tenantId, string searchTerm)`
- [x] **7.1.2** إنشاء `Repositories/Implementations/ExhibitorRepository.cs`
  - `SearchAsync` — يبحث في `CompanyName`, `ContactPerson`, `Email` (LIKE %term%)

### 7.2 — `IBoothReservationRepository` / `BoothReservationRepository` (الأهم)

- [x] **7.2.1** إنشاء `Repositories/Interfaces/IBoothReservationRepository.cs`
  - `Task<IReadOnlyList<BoothReservation>> GetByExhibitionAsync(int exhibitionId)`
  - `Task<IReadOnlyList<BoothReservation>> GetByExhibitorAsync(int exhibitorId)`
  - `Task<IReadOnlyList<BoothReservation>> GetByStatusAsync(int exhibitionId, ReservationStatus status)`
  - `Task<BoothReservation?> GetWithInvoiceAsync(int reservationId)`
  - `Task<BoothReservation?> GetWithServicesAsync(int reservationId)`
  - `Task<BoothReservation?> GetFullDetailAsync(int reservationId)`
  - `Task<bool> IsBoothReservedAsync(int boothId, int exhibitionId)`
  - `Task<bool> IsMergeReservedAsync(int mergeId, int exhibitionId)`
  - `Task<decimal> GetTotalRevenueAsync(int exhibitionId)`
  - `Task<IReadOnlyList<BoothReservation>> GetUnpaidReservationsAsync(int exhibitionId)`
- [x] **7.2.2** إنشاء `Repositories/Implementations/BoothReservationRepository.cs`
  - `GetFullDetailAsync` — Include Booth, Include ReservationServices + Service, Include Invoice + Payments
  - `IsBoothReservedAsync` — `ExistsAsync(r => r.BoothID == boothId && r.ExhibitionID == exhibitionId && r.Status != Cancelled)`
  - `GetTotalRevenueAsync` — `SumAsync(r => r.TotalAmount) Where Status != Cancelled`
  - `GetUnpaidReservationsAsync` — Reservations بدون Invoice أو Invoice.Status == Unpaid

### 7.3 — `IBoothStaffRepository` / `BoothStaffRepository`

- [x] **7.3.1** إنشاء `Repositories/Interfaces/IBoothStaffRepository.cs`
  - `Task<IReadOnlyList<BoothStaff>> GetByReservationAsync(int reservationId)`
  - `Task<IReadOnlyList<BoothStaff>> GetByExhibitorAsync(int exhibitorId)`
- [x] **7.3.2** إنشاء `Repositories/Implementations/BoothStaffRepository.cs`

### 7.4 — `IProductRepository` / `ProductRepository`

- [x] **7.4.1** إنشاء `Repositories/Interfaces/IProductRepository.cs`
  - `Task<IReadOnlyList<Product>> GetByExhibitorAsync(int exhibitorId)`
  - `Task<IReadOnlyList<Product>> GetByExhibitionAsync(int exhibitionId)`
  - `Task<IReadOnlyList<Product>> SearchAsync(int tenantId, string searchTerm)`
- [x] **7.4.2** إنشاء `Repositories/Implementations/ProductRepository.cs`

### 7.5 — التحقق

- [x] **7.5.1** `dotnet build ExhibitionManagementSystem.DataAccess` ✅

---

## 🟢 المرحلة 8 — Repositories التسعير

> **التبعيات:** المرحلة 2 مكتملة
> **Repositories:** Service, PricingPackage, BoothPriceRule, ServicePriceRule

### 8.1 — `IServiceRepository` / `ServiceRepository`

- [x] **8.1.1** إنشاء `Repositories/Interfaces/IServiceRepository.cs`
  - `Task<IReadOnlyList<Service>> GetByTenantAsync(int tenantId)`
  - `Task<IReadOnlyList<Service>> GetMandatoryServicesAsync(int tenantId)`
  - `Task<IReadOnlyList<Service>> GetByCategoryAsync(int tenantId, string category)`
  - `Task<Service?> GetWithPriceRulesAsync(int serviceId)`
- [x] **8.1.2** إنشاء `Repositories/Implementations/ServiceRepository.cs`
  - `GetMandatoryServicesAsync` — `FindAsync(s => s.TenantID == tenantId && s.IsMandatory && !s.IsDeleted)`

### 8.2 — `IPricingPackageRepository` / `PricingPackageRepository`

- [x] **8.2.1** إنشاء `Repositories/Interfaces/IPricingPackageRepository.cs`
  - `Task<IReadOnlyList<PricingPackage>> GetByTenantAsync(int tenantId)`
  - `Task<IReadOnlyList<PricingPackage>> GetActivePackagesAsync(int tenantId)`
  - `Task<PricingPackage?> GetWithServicesAsync(int packageId)`
- [x] **8.2.2** إنشاء `Repositories/Implementations/PricingPackageRepository.cs`
  - `GetActivePackagesAsync` — `IsActive && ValidFrom <= today && (ValidTo == null || ValidTo >= today)`

### 8.3 — `IBoothPriceRuleRepository` / `BoothPriceRuleRepository`

- [x] **8.3.1** إنشاء `Repositories/Interfaces/IBoothPriceRuleRepository.cs`
  - `Task<IReadOnlyList<BoothPriceRule>> GetByTenantAsync(int tenantId)`
  - `Task<IReadOnlyList<BoothPriceRule>> GetByExhibitionAsync(int exhibitionId)`
  - `Task<BoothPriceRule?> GetApplicableRuleAsync(int tenantId, int? exhibitionId, BoothType? boothType, ExhibitorCategory? category, decimal areaSqM, DateTime date)`
- [x] **8.3.2** إنشاء `Repositories/Implementations/BoothPriceRuleRepository.cs`
  - `GetApplicableRuleAsync`:
    - ترتيب الأولوية: `ExhibitionID != null` أولاً، ثم `TenantID` فقط
    - فلترة بـ BoothType, Category, MinArea ≤ areaSqM ≤ MaxArea, ValidFrom ≤ date ≤ ValidTo

### 8.4 — `IServicePriceRuleRepository` / `ServicePriceRuleRepository`

- [x] **8.4.1** إنشاء `Repositories/Interfaces/IServicePriceRuleRepository.cs`
  - `Task<IReadOnlyList<ServicePriceRule>> GetByServiceAsync(int serviceId)`
  - `Task<IReadOnlyList<ServicePriceRule>> GetByExhibitionAsync(int exhibitionId)`
  - `Task<ServicePriceRule?> GetApplicableRuleAsync(int serviceId, int? exhibitionId, ExhibitorCategory? category, DateTime date)`
- [x] **8.4.2** إنشاء `Repositories/Implementations/ServicePriceRuleRepository.cs`
  - `GetApplicableRuleAsync` — نفس منطق الأولوية: Exhibition أولاً ثم Tenant

### 8.5 — التحقق

- [x] **8.5.1** `dotnet build ExhibitionManagementSystem.DataAccess` ✅

---

## 🔵 المرحلة 9 — Repositories المالية

> **التبعيات:** المرحلة 2 مكتملة
> **Repositories:** Invoice, Payment, FinancialReport

### 9.1 — `IInvoiceRepository` / `InvoiceRepository`

- [x] **9.1.1** إنشاء `Repositories/Interfaces/IInvoiceRepository.cs`
  - `Task<Invoice?> GetByReservationAsync(int reservationId)`
  - `Task<Invoice?> GetByNumberAsync(int tenantId, string invoiceNumber)`
  - `Task<IReadOnlyList<Invoice>> GetByStatusAsync(int tenantId, InvoiceStatus status)`
  - `Task<Invoice?> GetWithPaymentsAsync(int invoiceId)`
  - `Task<IReadOnlyList<Invoice>> GetOverdueInvoicesAsync(int tenantId)`
  - `Task<string> GenerateNextInvoiceNumberAsync(int tenantId)`
- [x] **9.1.2** إنشاء `Repositories/Implementations/InvoiceRepository.cs`
  - `GetOverdueInvoicesAsync` — `DueDate < DateTime.UtcNow && Status != Paid`
  - `GenerateNextInvoiceNumberAsync`:
    - يعدّ آخر رقم للـ Tenant
    - يُنتج رقماً بصيغة `INV-{TenantId:D4}-{Year}-{Seq:D5}`

### 9.2 — `IPaymentRepository` / `PaymentRepository`

- [x] **9.2.1** إنشاء `Repositories/Interfaces/IPaymentRepository.cs`
  - `Task<IReadOnlyList<Payment>> GetByInvoiceAsync(int invoiceId)`
  - `Task<decimal> GetTotalPaidAsync(int invoiceId)`
  - `Task<IReadOnlyList<Payment>> GetByDateRangeAsync(int tenantId, DateTime from, DateTime to)`
- [x] **9.2.2** إنشاء `Repositories/Implementations/PaymentRepository.cs`
  - `GetTotalPaidAsync` — `SumAsync(p => p.Amount) Where Status == Completed`

### 9.3 — `IFinancialReportRepository` / `FinancialReportRepository`

- [x] **9.3.1** إنشاء `Repositories/Interfaces/IFinancialReportRepository.cs`
  - `Task<IReadOnlyList<FinancialReport>> GetByTenantAsync(int tenantId)`
  - `Task<IReadOnlyList<FinancialReport>> GetByExhibitionAsync(int exhibitionId)`
  - `Task<FinancialReport?> GetLatestReportAsync(int tenantId)`
- [x] **9.3.2** إنشاء `Repositories/Implementations/FinancialReportRepository.cs`

### 9.4 — التحقق

- [x] **9.4.1** `dotnet build ExhibitionManagementSystem.DataAccess` ✅

---

## 🔵 المرحلة 10 — Repositories الزوار والتذاكر

> **التبعيات:** المرحلة 2 مكتملة
> **Repositories:** Visitor, Ticket, TicketScan, VisitorRating

### 10.1 — `IVisitorRepository` / `VisitorRepository`

- [x] **10.1.1** إنشاء `Repositories/Interfaces/IVisitorRepository.cs`
  - `Task<IReadOnlyList<Visitor>> GetByTenantAsync(int tenantId)`
  - `Task<Visitor?> GetByUserIdAsync(string userId)`
  - `Task<Visitor?> GetByEmailAsync(int tenantId, string email)`
  - `Task<IReadOnlyList<Visitor>> SearchAsync(int tenantId, string searchTerm)`
  - `Task<Visitor?> GetWithTicketsAsync(int visitorId)`
- [x] **10.1.2** إنشاء `Repositories/Implementations/VisitorRepository.cs`
  - `SearchAsync` — يبحث في `FullName`, `Email`, `Phone`

### 10.2 — `ITicketRepository` / `TicketRepository`

- [x] **10.2.1** إنشاء `Repositories/Interfaces/ITicketRepository.cs`
  - `Task<Ticket?> GetByQRCodeAsync(string qrCode)`
  - `Task<IReadOnlyList<Ticket>> GetByVisitorAsync(int visitorId)`
  - `Task<IReadOnlyList<Ticket>> GetByExhibitionAsync(int exhibitionId)`
  - `Task<IReadOnlyList<Ticket>> GetByStatusAsync(int exhibitionId, TicketStatus status)`
  - `Task<bool> IsQRCodeUniqueAsync(string qrCode)`
  - `Task<int> GetActiveTicketCountAsync(int exhibitionId)`
- [x] **10.2.2** إنشاء `Repositories/Implementations/TicketRepository.cs`
  - `GetByQRCodeAsync` — `IgnoreQueryFilters` للتذاكر المحذوفة أيضاً (للتحقق عند الدخول)
  - `GetActiveTicketCountAsync` — `CountAsync(t => t.ExhibitionID == id && t.Status == Active)`

### 10.3 — `ITicketScanRepository` / `TicketScanRepository`

- [x] **10.3.1** إنشاء `Repositories/Interfaces/ITicketScanRepository.cs`
  - `Task<IReadOnlyList<TicketScan>> GetByTicketAsync(int ticketId)`
  - `Task<TicketScan?> GetLastScanAsync(int ticketId)`
  - `Task<int> GetTodayScansCountAsync(int exhibitionId)`
- [x] **10.3.2** إنشاء `Repositories/Implementations/TicketScanRepository.cs`
  - `GetTodayScansCountAsync` — `ScanTime.Date == DateTime.UtcNow.Date`

### 10.4 — `IVisitorRatingRepository` / `VisitorRatingRepository`

- [x] **10.4.1** إنشاء `Repositories/Interfaces/IVisitorRatingRepository.cs`
  - `Task<IReadOnlyList<VisitorRating>> GetByExhibitionAsync(int exhibitionId)`
  - `Task<IReadOnlyList<VisitorRating>> GetByExhibitorAsync(int exhibitorId)`
  - `Task<double> GetAverageRatingAsync(int exhibitionId)`
  - `Task<bool> HasVisitorRatedAsync(int visitorId, int exhibitionId)`
- [x] **10.4.2** إنشاء `Repositories/Implementations/VisitorRatingRepository.cs`
  - `GetAverageRatingAsync` — `AverageAsync(r => r.Score)`

### 10.5 — التحقق

- [x] **10.5.1** `dotnet build ExhibitionManagementSystem.DataAccess` ✅

---

## 🔵 المرحلة 11 — DI Registration + التحقق النهائي

> **التبعيات:** المراحل 2 إلى 10 مكتملة

### 11.1 — إنشاء `DataAccessServiceExtensions.cs`

- [x] **11.1.1** إنشاء `Extensions/DataAccessServiceExtensions.cs`
- [x] **11.1.2** تعريف `namespace ExhibitionManagementSystem.DataAccess.Extensions`
- [x] **11.1.3** إضافة `using Microsoft.Extensions.DependencyInjection`، `Microsoft.EntityFrameworkCore`
- [x] **11.1.4** تعريف `public static class DataAccessServiceExtensions`
- [x] **11.1.5** تعريف `public static IServiceCollection AddDataAccess(this IServiceCollection services, string connectionString)`
- [x] **11.1.6** إضافة `services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString))`
- [x] **11.1.7** إضافة `services.AddScoped<IUnitOfWork, UnitOfWork>()` — **سطر واحد فقط للـ Repos**
- [x] **11.1.8** إرجاع `services`

### 11.2 — إكمال UnitOfWork (ربط الـ Repositories المكتملة)

- [x] **11.2.1** التحقق من أن جميع الـ Lazy properties في `UnitOfWork` تشير للـ Implementations الصحيحة
- [x] **11.2.2** التحقق من أن `IUnitOfWork` يحتوي جميع الـ properties

### 11.3 — التحقق النهائي

- [x] **11.3.1** `dotnet build ExhibitionManagementSystem.DataAccess` ✅ بدون أخطاء أو Warnings
- [x] **11.3.2** `dotnet build` للـ Solution كاملاً ✅ بدون أخطاء

---

## 📊 ملخص التقدم

| المرحلة | الوصف | الحالة | عدد المهام |
|---|---|:---:|:---:|
| 1 | هيكل المجلدات | ✅ | 3 |
| 2 | IGenericRepository + GenericRepository | ✅ | 18 |
| 3 | IUnitOfWork + UnitOfWork (مع AuditLog) | ✅ | 35 |
| 4 | Repos البنية التحتية (5 repos) | ✅ | 12 |
| 5 | Repos الجغرافية (4 repos) | ✅ | 10 |
| 6 | Repos المعرض (3 repos) | ✅ | 8 |
| 7 | Repos التشغيلية (4 repos) | ✅ | 10 |
| 8 | Repos التسعير (4 repos) | ✅ | 10 |
| 9 | Repos المالية (3 repos) | ✅ | 8 |
| 10 | Repos الزوار والتذاكر (4 repos) | ✅ | 10 |
| 11 | DI Extension + تحقق نهائي | ✅ | 8 |
| **المجموع** | | **✅** | **~132** |
