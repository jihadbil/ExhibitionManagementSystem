# ملف المهام التفصيلي — إصلاح نظام إدارة المعارض

---

## 📊 الإحصائيات

- **إجمالي المهام:** 32
- **ملفات مُعدَّلة:** 22
- **ملفات جديدة:** 10
- **Migrations:** 1 شاملة

---

## 🔴 المرحلة الأولى: طبقة النماذج (Models Layer)

### م١ — إصلاح `Ticket.cs`: استبدال `[NotMapped] CreatedAt` بعمود حقيقي
- `[x]` **الملف:** `ExhibitionManagementSystem.Models/Ticket.cs`
- حذف تعريف `[NotMapped] public DateTime CreatedAt { get => IssuedAt; set => IssuedAt = value; }`
- إضافة `public DateTime CreatedAt { get; set; } = DateTime.UtcNow;` كعمود حقيقي
- الإبقاء على `public DateTime IssuedAt { get; set; } = DateTime.UtcNow;` كحقل دلالي

### م٢ — إصلاح `Visitor.cs`: استبدال `[NotMapped] CreatedAt` بعمود حقيقي
- `[x]` **الملف:** `ExhibitionManagementSystem.Models/Visitor.cs`
- حذف تعريف `[NotMapped] public DateTime CreatedAt { get => RegisteredAt; set => RegisteredAt = value; }`
- إضافة `public DateTime CreatedAt { get; set; } = DateTime.UtcNow;` كعمود حقيقي
- الإبقاء على `public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;` كحقل دلالي

### م٣ — إضافة `ISoftDeletable` إلى `Invoice.cs`
- `[x]` **الملف:** `ExhibitionManagementSystem.Models/Invoice.cs`
- تغيير `public class Invoice : IAuditableEntity` إلى `public class Invoice : IAuditableEntity, ISoftDeletable`
- إضافة الحقول:
  ```csharp
  public bool IsDeleted { get; set; } = false;
  public DateTime? DeletedAt { get; set; }
  public string? DeletedByUserId { get; set; }
  ```
- إضافة `using ExhibitionManagementSystem.Models.Interfaces;` إذا لزم

### م٤ — إضافة `IAuditableEntity` إلى `ApplicationRole.cs`
- `[x]` **الملف:** `ExhibitionManagementSystem.Models/ApplicationRole.cs`
- تغيير `public class ApplicationRole : IdentityRole` إلى `public class ApplicationRole : IdentityRole, IAuditableEntity`
- إضافة الحقول:
  ```csharp
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime? UpdatedAt { get; set; }
  ```
- إضافة `using ExhibitionManagementSystem.Models.Interfaces;`

### م٥ — إضافة `IAuditableEntity` و `ISoftDeletable` إلى `BoothStaff.cs`
- `[x]` **الملف:** `ExhibitionManagementSystem.Models/BoothStaff.cs`
- تغيير `public class BoothStaff` إلى `public class BoothStaff : IAuditableEntity, ISoftDeletable`
- إضافة:
  ```csharp
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime? UpdatedAt { get; set; }
  public bool IsDeleted { get; set; } = false;
  public DateTime? DeletedAt { get; set; }
  public string? DeletedByUserId { get; set; }
  ```

### م٦ — حذف `Plan` من `Tenant.cs` وإضافة `CurrentPlan` محسوبة
- `[x]` **الملف:** `ExhibitionManagementSystem.Models/Tenant.cs`
- حذف السطر: `[Required, StringLength(50)] public string Plan { get; set; }`
- إضافة خاصية محسوبة `[NotMapped]`:
  ```csharp
  using System.ComponentModel.DataAnnotations.Schema;
  
  /// <summary>
  /// يُحسب من آخر TenantSubscription نشط — لا يُخزن في قاعدة البيانات.
  /// مصدر الحقيقة الوحيد للخطة هو TenantSubscription.
  /// </summary>
  [NotMapped]
  public string? CurrentPlan => TenantSubscriptions
      .Where(s => s.Status == SubscriptionStatus.Active)
      .OrderByDescending(s => s.StartDate)
      .FirstOrDefault()?.Plan;
  ```
- إضافة `using ExhibitionManagementSystem.Models.Enums;` إذا لم تكن موجودة

### م٧ — إنشاء نموذج `Expense.cs` الجديد
- `[x]` **الملف الجديد:** `ExhibitionManagementSystem.Models/Expense.cs`
- إنشاء النموذج الكامل:
  ```csharp
  using System;
  using System.ComponentModel.DataAnnotations;
  using System.ComponentModel.DataAnnotations.Schema;
  using ExhibitionManagementSystem.Models.Interfaces;

  namespace ExhibitionManagementSystem.Models;

  public class Expense : IAuditableEntity, ISoftDeletable
  {
      [Key] public int ExpenseID { get; set; }
      public int TenantID { get; set; }
      public int ExhibitionID { get; set; }
      [Required, StringLength(200)] public string Description { get; set; }
      [Required, StringLength(100)] public string Category { get; set; }
      [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
      [Required, StringLength(3)] public string CurrencyCode { get; set; }
      [Column(TypeName = "date")] public DateTime ExpenseDate { get; set; }
      [StringLength(500)] public string? Notes { get; set; }
      [StringLength(450)] public string? CreatedByUserId { get; set; }
      public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
      public DateTime? UpdatedAt { get; set; }
      public bool IsDeleted { get; set; } = false;
      public DateTime? DeletedAt { get; set; }
      public string? DeletedByUserId { get; set; }

      [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; }
      [ForeignKey(nameof(ExhibitionID))] public virtual Exhibition Exhibition { get; set; }
      [ForeignKey(nameof(CurrencyCode))] public virtual Currency Currency { get; set; }
      [ForeignKey(nameof(CreatedByUserId))] public virtual ApplicationUser? CreatedByUser { get; set; }
  }
  ```

---

## 🟡 المرحلة الثانية: طبقة البيانات (Data Access Layer)

### م٨ — إصلاح `ExchangeRateRepository.ConvertAsync`: إرجاع `decimal?` بدلاً من Exception
- `[x]` **الملف:** `ExhibitionManagementSystem.DataAccess/Repositories/Implementations/ExchangeRateRepository.cs`
- تغيير توقيع الدالة من `Task<decimal>` إلى `Task<decimal?>`
- استبدال `throw new InvalidOperationException(...)` بـ `return null`

### م٩ — تحديث واجهة `IExchangeRateRepository`
- `[x]` **الملف:** `ExhibitionManagementSystem.DataAccess/Repositories/Interfaces/IExchangeRateRepository.cs`
- تحديث توقيع `ConvertAsync` ليُعيد `Task<decimal?>`

### م١٠ — إضافة `DbSet<Expense>` إلى `ApplicationDbContext.cs`
- `[x]` **الملف:** `ExhibitionManagementSystem.DataAccess/ApplicationDbContext.cs`
- إضافة: `public DbSet<Expense> Expenses { get; set; }`
- إضافة تهيئة العلاقات في `OnModelCreating`:
  ```csharp
  builder.Entity<Expense>()
      .HasOne(e => e.Exhibition)
      .WithMany()
      .HasForeignKey(e => e.ExhibitionID)
      .OnDelete(DeleteBehavior.Restrict);
  
  builder.Entity<Expense>()
      .HasOne(e => e.Tenant)
      .WithMany()
      .HasForeignKey(e => e.TenantID)
      .OnDelete(DeleteBehavior.Restrict);
  
  // فهرس للاستعلام
  builder.Entity<Expense>().HasIndex(e => new { e.ExhibitionID, e.IsDeleted });
  ```

### م١١ — إنشاء `IExpenseRepository.cs`
- `[x]` **الملف الجديد:** `ExhibitionManagementSystem.DataAccess/Repositories/Interfaces/IExpenseRepository.cs`
  ```csharp
  using System.Collections.Generic;
  using System.Threading.Tasks;
  using ExhibitionManagementSystem.Models;

  namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

  public interface IExpenseRepository : IGenericRepository<Expense>
  {
      Task<IList<Expense>> GetByExhibitionAsync(int exhibitionId);
      Task<decimal> GetTotalExpensesAsync(int exhibitionId);
  }
  ```

### م١٢ — إنشاء `ExpenseRepository.cs`
- `[x]` **الملف الجديد:** `ExhibitionManagementSystem.DataAccess/Repositories/Implementations/ExpenseRepository.cs`
  ```csharp
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading.Tasks;
  using Microsoft.EntityFrameworkCore;
  using ExhibitionManagementSystem.Models;
  using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

  namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations;

  public class ExpenseRepository : GenericRepository<Expense>, IExpenseRepository
  {
      public ExpenseRepository(ApplicationDbContext context) : base(context) { }

      public async Task<IList<Expense>> GetByExhibitionAsync(int exhibitionId)
          => await _dbSet.Where(e => e.ExhibitionID == exhibitionId).ToListAsync();

      public async Task<decimal> GetTotalExpensesAsync(int exhibitionId)
          => await _dbSet.Where(e => e.ExhibitionID == exhibitionId)
                         .SumAsync(e => e.Amount);
  }
  ```

### م١٣ — تحديث `IUnitOfWork` لإضافة `IExpenseRepository`
- `[x]` **الملف:** `ExhibitionManagementSystem.DataAccess/Repositories/Interfaces/IUnitOfWork.cs`
- إضافة: `IExpenseRepository Expenses { get; }`

### م١٤ — تحديث `UnitOfWork.cs` لتسجيل `ExpenseRepository`
- `[x]` **الملف:** `ExhibitionManagementSystem.DataAccess/Repositories/Implementations/UnitOfWork.cs`
- إضافة الخاصية:
  ```csharp
  public IExpenseRepository Expenses { get; }
  ```
- تهيئتها في الـ Constructor:
  ```csharp
  Expenses = new ExpenseRepository(context);
  ```

---

## 🟠 المرحلة الثالثة: طبقة الخدمات وDTOs

### م١٥ — إصلاح `AuthService.ForgotPasswordAsync`: إزالة ثغرة User Enumeration
- `[x]` **الملف:** `ExhibitionManagementSystem.Services/Implementations/AuthService.cs`
- استبدال:
  ```csharp
  // قبل:
  if (user == null)
      return ServiceResult.Failure("المستخدم غير موجود", "USER_NOT_FOUND");
  var token = await _userManager.GeneratePasswordResetTokenAsync(user);
  return ServiceResult.Success(token);
  ```
  بـ:
  ```csharp
  // بعد:
  if (user == null)
      return ServiceResult.Success(); // صامت للأمان — لا نكشف وجود/غياب الحساب
  var token = await _userManager.GeneratePasswordResetTokenAsync(user);
  return ServiceResult.Success(token);
  ```

### م١٦ — إصلاح `ExhibitionService.ChangeStatusAsync`: منطق الفتح والإغلاق
- `[x]` **الملف:** `ExhibitionManagementSystem.Services/Implementations/ExhibitionService.cs`
- **إصلاح منطق الفتح (السطر ~128):**
  ```csharp
  // قبل (خاطئ): يمنع الفتح إذا كان StartDate في الماضي
  if (exhibition.StartDate.Date < DateTime.UtcNow.Date)
      return ServiceResult<ExhibitionDto>.Failure("لا يمكن فتح المعرض لأن تاريخ البدء في الماضي", "INVALID_START_DATE");

  // بعد (صحيح): يمنع الفتح إذا كان StartDate في المستقبل
  if (exhibition.StartDate.Date > DateTime.UtcNow.Date)
      return ServiceResult<ExhibitionDto>.Failure("لا يمكن فتح المعرض قبل تاريخ بدئه", "EXHIBITION_NOT_STARTED_YET");
  ```
- **حذف قيد الإغلاق بالكامل (السطر ~133-139):**
  ```csharp
  // حذف هذا الكتلة بالكامل:
  else if (newStatus == ExhibitionStatus.Closed)
  {
      var reservations = await ...
      if (reservations == null || reservations.Count == 0)
          return ServiceResult<ExhibitionDto>.Failure(...);
  }
  ```

### م١٧ — إصلاح `BoothService.MergeBoothsAsync`: تمرير `userId` الحقيقي
- `[x]` **الملف:** `ExhibitionManagementSystem.Services/Implementations/BoothService.cs`
- تحديث توقيع `MergeBoothsAsync` لقبول `string userId`:
  ```csharp
  // قبل:
  public async Task<ServiceResult<BoothMergeDto>> MergeBoothsAsync(int tenantId, BoothMergeCreateDto dto)
  
  // بعد:
  public async Task<ServiceResult<BoothMergeDto>> MergeBoothsAsync(int tenantId, string userId, BoothMergeCreateDto dto)
  ```
- تغيير `MergedByUserId = "System"` إلى `MergedByUserId = userId`

### م١٨ — إصلاح `BoothService.GetAvailableAsync`: حل مشكلة N+1 Query
- `[x]` **الملف:** `ExhibitionManagementSystem.Services/Implementations/BoothService.cs`
- استبدال حلقة `foreach` + `IsBoothReservedAsync` باستعلام واحد:
  ```csharp
  // استعلام واحد لجلب كل IDs المحجوزة
  var reservedBoothIds = await _unitOfWork.BoothReservations.AsQueryable()
      .Where(r => r.ExhibitionID == exhibitionId
                  && r.BoothID.HasValue
                  && r.Status != ReservationStatus.Cancelled)
      .Select(r => r.BoothID!.Value)
      .ToHashSetAsync();
  
  var availableBooths = booths.Where(b => !reservedBoothIds.Contains(b.BoothID)).ToList();
  ```

### م١٩ — تحديث واجهة `IBoothService` لتوقيع `MergeBoothsAsync`
- `[x]` **الملف:** `ExhibitionManagementSystem.Services/Interfaces/IBoothService.cs`
- تحديث التوقيع ليشمل `string userId`

### م٢٠ — إصلاح `FinancialService.GenerateInvoiceForReservationAsync`: ضريبة من IConfiguration
- `[x]` **الملف:** `ExhibitionManagementSystem.Services/Implementations/FinancialService.cs`
- إضافة `IConfiguration _configuration` في Constructor
- استبدال `decimal taxRate = 15.0m;` بـ:
  ```csharp
  decimal taxRate = decimal.TryParse(_configuration["Financial:DefaultTaxRate"], out var cfgRate)
      ? cfgRate : 15.0m;
  ```

### م٢١ — إصلاح `FinancialService.RecordPaymentAsync`: `ReceivedByUserId` من المعامل
- `[x]` **الملف:** `ExhibitionManagementSystem.Services/Implementations/FinancialService.cs`
- تحديث توقيع `RecordPaymentAsync` لقبول `string userId`:
  ```csharp
  public async Task<ServiceResult<PaymentDto>> RecordPaymentAsync(int tenantId, string userId, PaymentCreateDto dto)
  ```
- استخدام `ReceivedByUserId = userId` بدلاً من `dto.ReceivedByUserId`

### م٢٢ — تحديث واجهة `IFinancialService` لتوقيع `RecordPaymentAsync`
- `[x]` **الملف:** `ExhibitionManagementSystem.Services/Interfaces/IFinancialService.cs`
- تحديث التوقيع ليشمل `string userId`

### م٢٣ — إصلاح `CurrencyService.UpsertExchangeRateAsync`: تمرير `userId` الحقيقي
- `[x]` **الملف:** `ExhibitionManagementSystem.Services/Implementations/CurrencyService.cs`
- تحديث توقيع `UpsertExchangeRateAsync` لقبول `string userId`:
  ```csharp
  public async Task<ServiceResult<ExchangeRateDto>> UpsertExchangeRateAsync(string userId, ExchangeRateDto dto)
  ```
- استبدال `CreatedByUserId = "System"` بـ `CreatedByUserId = userId`

### م٢٤ — تحديث واجهة `ICurrencyService` لتوقيع `UpsertExchangeRateAsync`
- `[x]` **الملف:** `ExhibitionManagementSystem.Services/Interfaces/ICurrencyService.cs`
- تحديث التوقيع ليشمل `string userId`

### م٢٥ — إصلاح `CurrencyService.ConvertAmountAsync`: استخدام `decimal?`
- `[x]` **الملف:** `ExhibitionManagementSystem.Services/Implementations/CurrencyService.cs`
- استبدال `try/catch` بمعالجة `decimal?`:
  ```csharp
  public async Task<ServiceResult<decimal>> ConvertAmountAsync(decimal amount, string from, string to)
  {
      var converted = await _unitOfWork.ExchangeRates.ConvertAsync(from, to, amount);
      if (converted == null)
          return ServiceResult<decimal>.Failure($"سعر الصرف من {from} إلى {to} غير متوفر", "EXCHANGE_RATE_NOT_FOUND");
      return ServiceResult<decimal>.Success(converted.Value);
  }
  ```

### م٢٦ — إصلاح `HallService.DeleteAsync`: التحقق من الأكشاك النشطة قبل الحذف
- `[x]` **الملف:** `ExhibitionManagementSystem.Services/Implementations/HallService.cs`
- إضافة قبل `SoftDeleteAsync`:
  ```csharp
  var activeBooths = await _unitOfWork.Booths.FindAsync(b => b.HallID == hallId && !b.IsDeleted);
  if (activeBooths.Any())
      return ServiceResult.Failure("لا يمكن حذف القاعة لوجود أكشاك نشطة بها. احذف الأكشاك أولاً.", "HALL_HAS_ACTIVE_BOOTHS");
  ```

### م٢٧ — إصلاح `AdminService.CreateSubscriptionAsync`: حذف مزامنة `Plan` اليدوية
- `[x]` **الملف:** `ExhibitionManagementSystem.Services/Implementations/AdminService.cs`
- حذف السطرين:
  ```csharp
  // حذف هذين السطرين:
  tenant.Plan = sub.Plan;
  _unitOfWork.Tenants.Update(tenant);
  ```

### م٢٨ — تحديث `ReportService.GenerateExhibitionReportAsync`: ربط بـ `Expense`
- `[x]` **الملف:** `ExhibitionManagementSystem.Services/Implementations/ReportService.cs`
- استبدال `TotalExpenses = 0` بـ:
  ```csharp
  decimal totalExpenses = await _unitOfWork.Expenses.GetTotalExpensesAsync(exhibitionId);
  decimal netProfit = totalRevenue - totalExpenses;
  ```
- تحديث بناء `FinancialReport` ليستخدم القيم المحسوبة

---

## 🔵 المرحلة الرابعة: DTOs والـ Migration

### م٢٩ — إصلاح DTOs المتعددة دفعة واحدة

#### أ) `ExchangeRateDto.cs`
- `[x]` **الملف:** `ExhibitionManagementSystem.Models.DTOs/Financial/ExchangeRateDto.cs`
- حذف الحقول: `ValidFrom`, `ValidTo`, `IsActive`

#### ب) `CurrencyMappingProfile.cs`
- `[x]` **الملف:** `.../Mapping/Profiles/CurrencyMappingProfile.cs`
- حذف تعيينات: `ValidFrom`, `ValidTo`, `IsActive` من `CreateMap<ExchangeRate, ExchangeRateDto>()`

#### ج) `TenantSubscriptionDto.cs`
- `[x]` **الملف:** `.../Admin/TenantSubscriptionDto.cs`
- تغيير `DateTime? EndDate` إلى `DateTime EndDate`

#### د) `PaymentCreateDto.cs`
- `[x]` **الملف:** `.../Financial/PaymentCreateDto.cs`
- حذف الحقل `[Required][StringLength(450)] public string ReceivedByUserId { get; set; }`

#### هـ) `BoothMergeDto.cs`
- `[x]` **الملف:** `.../Booth/BoothMergeDto.cs`
- إضافة الحقول:
  ```csharp
  public int ExhibitionID { get; set; }
  public string ExhibitionName { get; set; } = string.Empty;
  ```

#### و) `TenantDto.cs`
- `[x]` **الملف:** `.../Tenant/TenantDto.cs`
- تغيير `public string Plan { get; set; }` إلى `public string? CurrentPlan { get; set; }`

#### ز) `TenantMappingProfile.cs`
- `[x]` **الملف:** `.../Mapping/Profiles/TenantMappingProfile.cs`
- إضافة تعيين صريح:
  ```csharp
  CreateMap<Tenant, TenantDto>()
      .ForMember(dest => dest.CurrentPlan, opt => opt.MapFrom(src => src.CurrentPlan));
  ```

### م٣٠ — إنشاء DTOs الجديدة

#### أ) `ExpenseDto.cs` و `ExpenseCreateDto.cs`
- `[x]` **الملف الجديد:** `.../Financial/ExpenseDto.cs`
  ```csharp
  public class ExpenseDto
  {
      public int ExpenseID { get; set; }
      public int TenantID { get; set; }
      public int ExhibitionID { get; set; }
      public string ExhibitionName { get; set; } = string.Empty;
      public string Description { get; set; } = string.Empty;
      public string Category { get; set; } = string.Empty;
      public decimal Amount { get; set; }
      public string CurrencyCode { get; set; } = string.Empty;
      public DateTime ExpenseDate { get; set; }
      public string? Notes { get; set; }
      public DateTime CreatedAt { get; set; }
      public string? CreatedByUserId { get; set; }
  }
  ```
- `[x]` **الملف الجديد:** `.../Financial/ExpenseCreateDto.cs`
  ```csharp
  public class ExpenseCreateDto
  {
      public int ExhibitionID { get; set; }
      [Required, StringLength(200)] public string Description { get; set; } = string.Empty;
      [Required, StringLength(100)] public string Category { get; set; } = string.Empty;
      [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
      [Required, StringLength(3)] public string CurrencyCode { get; set; } = string.Empty;
      public DateTime ExpenseDate { get; set; }
      [StringLength(500)] public string? Notes { get; set; }
  }
  ```

#### ب) `ExpenseMappingProfile.cs`
- `[x]` **الملف الجديد:** `.../Mapping/Profiles/ExpenseMappingProfile.cs`
  ```csharp
  public class ExpenseMappingProfile : Profile
  {
      public ExpenseMappingProfile()
      {
          CreateMap<Expense, ExpenseDto>()
              .ForMember(dest => dest.ExhibitionName, 
                         opt => opt.MapFrom(src => src.Exhibition != null ? src.Exhibition.Name : string.Empty));
          CreateMap<ExpenseCreateDto, Expense>();
      }
  }
  ```

#### ج) `PricingPackageUpdateDto.cs`
- `[x]` **الملف الجديد:** `.../Pricing/PricingPackageUpdateDto.cs`
  ```csharp
  public class PricingPackageUpdateDto
  {
      [Required, StringLength(100)] public string PackageName { get; set; } = string.Empty;
      [StringLength(500)] public string? Description { get; set; }
      [Column(TypeName = "decimal(18,2)")] public decimal BasePrice { get; set; }
      [StringLength(3)] public string CurrencyCode { get; set; } = string.Empty;
      public bool IsActive { get; set; } = true;
  }
  ```

#### د) `ServicePriceRuleUpdateDto.cs`
- `[x]` **الملف الجديد:** `.../Pricing/ServicePriceRuleUpdateDto.cs`
  ```csharp
  public class ServicePriceRuleUpdateDto
  {
      public string ExhibitorCategory { get; set; } = string.Empty;
      [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }
      [StringLength(3)] public string CurrencyCode { get; set; } = string.Empty;
      public int? MinQuantity { get; set; }
      public int? MaxQuantity { get; set; }
      public bool IsActive { get; set; } = true;
  }
  ```

### م٣١ — تسجيل `ExpenseMappingProfile` في نقطة التهيئة
- `[x]` **الملف:** `ExhibitionManagementSystem.Models.DTOs/Mapping/MappingProfile.cs` (أو نقطة تسجيل الـ Profiles)
- إضافة `ExpenseMappingProfile` إلى قائمة الـ Profiles المُسجَّلة

### م٣٢ — إنشاء Migration الشاملة وتحديث قاعدة البيانات
- `[x]` التحقق من سلامة النماذج قبل إنشاء Migration
- `[x]` إنشاء Migration:
  ```bash
  dotnet ef migrations add FixAuditSoftDeleteExpenseAndTenantPlan `
    --project ExhibitionManagementSystem.DataAccess `
    --startup-project ExhibitionManagementSystem
  ```
- `[x]` مراجعة ملف Migration والتحقق من التغييرات:
  - إضافة `CreatedAt` لجداول `Tickets` و `Visitors`
  - إضافة أعمدة `ISoftDeletable` لجداول `Invoices` و `BoothStaffs`
  - إضافة أعمدة `IAuditableEntity` لجدول `AspNetRoles`
  - **حذف** عمود `Plan` من جدول `Tenants`
  - إنشاء جدول `Expenses` الجديد
- `[x]` تطبيق Migration:
  ```bash
  dotnet ef database update `
    --project ExhibitionManagementSystem.DataAccess `
    --startup-project ExhibitionManagementSystem
  ```
- `[x]` التحقق من بناء المشروع بدون أخطاء:
  ```bash
  dotnet build ExhibitionManagementSystem.slnx
  ```

---

## 📋 ترتيب التنفيذ الموصى به

```
م١ → م٢ → م٣ → م٤ → م٥ → م٦ → م٧    (النماذج أولاً)
         ↓
م٨ → م٩ → م١٠ → م١١ → م١٢ → م١٣ → م١٤    (Data Access)
         ↓
م١٥ → م١٦ → م١٧ → م١٨ → م١٩ → م٢٠    (الخدمات)
م٢١ → م٢٢ → م٢٣ → م٢٤ → م٢٥ → م٢٦    (تكملة الخدمات)
م٢٧ → م٢٨                              (إصلاحات Admin وReport)
         ↓
م٢٩ → م٣٠ → م٣١                       (DTOs)
         ↓
م٣٢                                    (Migration + Build)
```

---

## ✅ معايير الاكتمال

- [x] المشروع يُبنى بدون أخطاء (`dotnet build`)
- [x] Migration تطبّق بنجاح على قاعدة البيانات
- [x] لا توجد `[NotMapped] CreatedAt` في أي نموذج يُطبّق `IAuditableEntity`
- [x] `AuthService.ForgotPasswordAsync` لا تكشف عن وجود/غياب المستخدم
- [x] `ExhibitionService.ChangeStatusAsync` يسمح بالفتح عند `StartDate <= Today`
- [x] `PaymentCreateDto` لا يحتوي `ReceivedByUserId`
- [x] `ExchangeRateDto` لا يحتوي حقولاً غير موجودة في النموذج
- [x] `Tenant` لا يحتوي عمود `Plan` في قاعدة البيانات
- [x] `TotalExpenses` في التقرير المالي محسوب من جدول `Expenses` الحقيقي
