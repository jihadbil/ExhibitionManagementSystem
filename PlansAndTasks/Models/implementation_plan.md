# خطة إصلاح طبقة النماذج — نظام إدارة المعارض

## الهدف

تحسين جودة وصحة طبقة النماذج `ExhibitionManagementSystem.Models` بمعالجة العيوب المكتشفة دون المساس بالبنية الأساسية للنظام، مع الحفاظ على الاتساق وقابلية التوسع للمرحلة الأونلاين.

> [!IMPORTANT]
> الخطة تتجاهل الـ DTOs بالكامل كما طُلب. كل الإصلاحات محصورة في `ExhibitionManagementSystem.Models` و`ExhibitionManagementSystem.DataAccess`.

---

## المراحل: ترتيب التنفيذ

```
المرحلة 1 → Enums المفقودة (لا تبعيات)
المرحلة 2 → Interfaces موحدة (تعتمد عليها المرحلة 3)
المرحلة 3 → تطبيق الـ Interfaces على النماذج (تعتمد على 1 و2)
المرحلة 4 → إصلاحات هيكلية (تعتمد على 1)
المرحلة 5 → تحديث ApplicationDbContext (تعتمد على 1 و2 و3 و4)
```

---

## المرحلة 1 — إضافة Enums المفقودة

**المجلد المستهدف:** `ExhibitionManagementSystem.Models/Enums/`

---

### [NEW] `TicketStatus.cs`

حالياً `Ticket.Status` من نوع `string` مما يسمح بقيم عشوائية. نستبدله بـ enum متسق مع باقي النظام.

```csharp
namespace ExhibitionManagementSystem.Models.Enums;

public enum TicketStatus
{
    Active,
    Used,
    Cancelled,
    Expired
}
```

---

### [NEW] `RegistrationStatus.cs`

حالياً `ScheduleRegistration.Status` من نوع `string`.

```csharp
namespace ExhibitionManagementSystem.Models.Enums;

public enum RegistrationStatus
{
    Registered,
    Attended,
    Cancelled,
    NoShow
}
```

---

### [NEW] `SubscriptionStatus.cs`

حالياً `TenantSubscription.Status` من نوع `string`.

```csharp
namespace ExhibitionManagementSystem.Models.Enums;

public enum SubscriptionStatus
{
    Trial,
    Active,
    Suspended,
    Expired,
    Cancelled
}
```

---

## المرحلة 2 — بناء Interfaces موحدة

**المجلد الجديد:** `ExhibitionManagementSystem.Models/Interfaces/`

هذه الـ Interfaces تُوفر **عقداً موحداً** بدلاً من تكرار نفس الحقول في كل نموذج.

---

### [NEW] `Interfaces/IAuditableEntity.cs`

تتبع وقت الإنشاء والتعديل لكل كيان.

```csharp
namespace ExhibitionManagementSystem.Models.Interfaces;

/// <summary>
/// يضمن أن الكيان يحتفظ بتواريخ الإنشاء والتعديل.
/// يُطبَّق على جميع النماذج الرئيسية.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
```

---

### [NEW] `Interfaces/ISoftDeletable.cs`

الحذف الناعم: تحديد السجل كمحذوف دون إزالته فعلياً من قاعدة البيانات.

```csharp
namespace ExhibitionManagementSystem.Models.Interfaces;

/// <summary>
/// يُمكّن الحذف الناعم (Soft Delete) على الكيان.
/// السجلات المحذوفة لا تُزال فعلياً بل تُوسم بـ IsDeleted.
/// يجب تطبيق Global Query Filter في ApplicationDbContext.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedByUserId { get; set; }
}
```

---

## المرحلة 3 — تطبيق الـ Interfaces وإضافة حقول التتبع

تُطبَّق `IAuditableEntity` على **جميع النماذج الرئيسية**.
تُطبَّق `ISoftDeletable` على **الكيانات الحيوية** فقط (المالية، العارضون، الزوار، إلخ).

### جدول تطبيق الـ Interfaces

| النموذج | `IAuditableEntity` | `ISoftDeletable` | ملاحظة |
|---|:---:|:---:|---|
| `Tenant` | ✅ (لديه CreatedAt) | ❌ | لا يُحذف مستأجر |
| `TenantSubscription` | ✅ | ❌ | سجل تاريخي |
| `Venue` | ✅ | ✅ | قد تُغلق أماكن |
| `Hall` | ✅ | ✅ | قد تُغلق قاعات |
| `Booth` | ✅ | ✅ | أكشاك قد تُلغى |
| `Exhibition` | ✅ (لديه CreatedAt) | ✅ | معارض قد تُلغى |
| `Exhibitor` | ✅ (لديه IsActive) | ✅ | عارضون قد يُوقفون |
| `BoothReservation` | ✅ | ✅ | حجوزات → سجل مالي |
| `Invoice` | ✅ | ❌ | لا تُحذف فواتير أبداً |
| `Payment` | ✅ | ❌ | لا تُحذف مدفوعات |
| `Service` | ✅ | ✅ | خدمات قد تُوقف |
| `PricingPackage` | ✅ | ✅ | باقات قد تُلغى |
| `Visitor` | ✅ | ✅ | زوار قد يُحذفون (GDPR) |
| `Ticket` | ✅ | ✅ | تذاكر قد تُلغى |
| `Product` | ✅ | ✅ | منتجات قد تُزال |
| `BoothPriceRule` | ✅ | ✅ | قواعد أسعار |
| `ServicePriceRule` | ✅ | ✅ | قواعد أسعار |

---

### التغييرات المطلوبة لكل نموذج

#### [MODIFY] `Tenant.cs`
- إضافة تطبيق `IAuditableEntity`
- إضافة `UpdatedAt`

#### [MODIFY] `TenantSubscription.cs`
- إضافة تطبيق `IAuditableEntity`
- إضافة `UpdatedAt`
- **تغيير `Status` من `string` إلى `SubscriptionStatus` enum**

#### [MODIFY] `Venue.cs`
- إضافة `IAuditableEntity` + `ISoftDeletable`
- إضافة `CreatedAt`, `UpdatedAt`, `IsDeleted`, `DeletedAt`, `DeletedByUserId`

#### [MODIFY] `Hall.cs`
- إضافة `IAuditableEntity` + `ISoftDeletable`
- إضافة `CreatedAt`, `UpdatedAt`, `IsDeleted`, `DeletedAt`, `DeletedByUserId`

#### [MODIFY] `Booth.cs`
- إضافة `IAuditableEntity` + `ISoftDeletable`
- إضافة `CreatedAt`, `UpdatedAt`, `IsDeleted`, `DeletedAt`, `DeletedByUserId`

#### [MODIFY] `Exhibition.cs`
- إضافة تطبيق `IAuditableEntity` (لديه `CreatedAt` بالفعل)
- إضافة `UpdatedAt`
- إضافة `ISoftDeletable`

#### [MODIFY] `Exhibitor.cs`
- إضافة `IAuditableEntity`
- إضافة `CreatedAt`, `UpdatedAt`
- إضافة `ISoftDeletable` (يستبدل `IsActive` جزئياً أو يعمل معه)
- **إضافة `UserId` اختياري** مع FK إلى `ApplicationUser`

#### [MODIFY] `BoothReservation.cs`
- إضافة `IAuditableEntity`
- إضافة `UpdatedAt`
- إضافة `ISoftDeletable`

#### [MODIFY] `Invoice.cs`
- إضافة `IAuditableEntity`
- إضافة `UpdatedAt`

#### [MODIFY] `Payment.cs`
- إضافة `IAuditableEntity`
- إضافة `UpdatedAt`

#### [MODIFY] `Service.cs`
- إضافة `IAuditableEntity`
- إضافة `CreatedAt`, `UpdatedAt`
- إضافة `ISoftDeletable`

#### [MODIFY] `PricingPackage.cs`
- إضافة `IAuditableEntity`
- إضافة `CreatedAt`, `UpdatedAt`
- إضافة `ISoftDeletable`

#### [MODIFY] `ServicePriceRule.cs`
- إضافة `IAuditableEntity`
- إضافة `CreatedAt`, `UpdatedAt`
- إضافة `ISoftDeletable`
- **تغيير `ExhibitorCategory` من `string` إلى `ExhibitorCategory` enum**

#### [MODIFY] `BoothPriceRule.cs`
- إضافة `IAuditableEntity`
- إضافة `CreatedAt`, `UpdatedAt`
- إضافة `ISoftDeletable`

#### [MODIFY] `Visitor.cs`
- إضافة `IAuditableEntity` (لديه `RegisteredAt`)
- إضافة `UpdatedAt`
- إضافة `ISoftDeletable`
- **إضافة `UserId` اختياري** مع FK إلى `ApplicationUser`

#### [MODIFY] `Ticket.cs`
- إضافة `IAuditableEntity` (لديه `IssuedAt`)
- إضافة `UpdatedAt`
- إضافة `ISoftDeletable`
- **تغيير `Status` من `string` إلى `TicketStatus` enum**

#### [MODIFY] `ScheduleRegistration.cs`
- إضافة `IAuditableEntity` (لديه `RegisteredAt`)
- إضافة `UpdatedAt`
- **تغيير `Status` من `string` إلى `RegistrationStatus` enum**

#### [MODIFY] `Product.cs`
- إضافة `IAuditableEntity`
- إضافة `CreatedAt`, `UpdatedAt`
- إضافة `ISoftDeletable`

---

## المرحلة 4 — الإصلاحات الهيكلية المتنوعة

---

### 4.1 — إصلاح Navigation Properties غير المُهيَّأة

#### [MODIFY] `Hall.cs`
```csharp
// قبل:
public virtual ICollection<Booth> Booths { get; set; }

// بعد:
public virtual ICollection<Booth> Booths { get; set; } = new HashSet<Booth>();
```

#### [MODIFY] `Venue.cs`
```csharp
// قبل:
public virtual ICollection<Hall> Halls { get; set; }

// بعد:
public virtual ICollection<Hall> Halls { get; set; } = new HashSet<Hall>();
```

#### [MODIFY] `BoothMerge.cs`
```csharp
// قبل:
public virtual ICollection<BoothMergeItem> MergeItems { get; set; }

// بعد:
public virtual ICollection<BoothMergeItem> MergeItems { get; set; } = new HashSet<BoothMergeItem>();
```

---

### 4.2 — إعادة تسمية المفاتيح الرئيسية غير المتسقة

#### [MODIFY] `PackageService.cs`
```csharp
// قبل:
[Key] public int ID { get; set; }

// بعد:
[Key] public int PackageServiceID { get; set; }
```

#### [MODIFY] `ReservationService.cs`
```csharp
// قبل:
[Key] public int ID { get; set; }

// بعد:
[Key] public int ReservationServiceID { get; set; }
```

---

### 4.3 — إضافة Check Constraint منطقي على `BoothReservation`

في `ApplicationDbContext.OnModelCreating` نضيف قيداً يمنع وجود `BoothID` و`MergeID` معاً في نفس الوقت:

```csharp
builder.Entity<BoothReservation>().ToTable(t =>
    t.HasCheckConstraint(
        "CK_BoothReservation_BoothOrMerge",
        "NOT (BoothID IS NOT NULL AND MergeID IS NOT NULL)"
    )
);
```

---

### 4.4 — توثيق `FinancialReport` كـ Snapshot

#### [MODIFY] `FinancialReport.cs`
إضافة حقلين لتوثيح طبيعة التقرير:
```csharp
// إضافة:
[Column(TypeName = "date")] public DateTime? ReportPeriodFrom { get; set; }
[Column(TypeName = "date")] public DateTime? ReportPeriodTo { get; set; }
```

---

## المرحلة 5 — تحديث `ApplicationDbContext`

#### [MODIFY] `ApplicationDbContext.cs`

**5.1 — تسجيل تحويلات الـ Enums الجديدة** في `OnModelCreating`:
```csharp
builder.Entity<Ticket>().Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
builder.Entity<ScheduleRegistration>().Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
builder.Entity<TenantSubscription>().Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
builder.Entity<ServicePriceRule>().Property(e => e.ExhibitorCategory).HasConversion<string>().HasMaxLength(20);
```

**5.2 — إضافة Global Query Filters للـ Soft Delete** لكل كيان يطبق `ISoftDeletable`:
```csharp
// في OnModelCreating — يُطبَّق على كل الكيانات التي تطبق ISoftDeletable تلقائياً
foreach (var entityType in builder.Model.GetEntityTypes())
{
    if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
    {
        var parameter = Expression.Parameter(entityType.ClrType, "e");
        var body = Expression.Equal(
            Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted)),
            Expression.Constant(false)
        );
        builder.Entity(entityType.ClrType).HasQueryFilter(
            Expression.Lambda(body, parameter)
        );
    }
}
```

**5.3 — إضافة فهارس للـ Soft Delete** لتجنب استعلام السجلات المحذوفة ببطء:
```csharp
// مثال على الفهارس الإضافية:
builder.Entity<Exhibitor>().HasIndex(e => new { e.TenantID, e.IsDeleted });
builder.Entity<Visitor>().HasIndex(v => new { v.TenantID, v.IsDeleted });
builder.Entity<Booth>().HasIndex(b => new { b.HallID, b.IsDeleted, b.Status });
```

**5.4 — إضافة FK Configurations الجديدة** لعلاقات `UserId` في `Exhibitor` و`Visitor`:
```csharp
builder.Entity<Exhibitor>()
    .HasOne(e => e.User)
    .WithMany()
    .HasForeignKey(e => e.UserId)
    .OnDelete(DeleteBehavior.Restrict);

builder.Entity<Visitor>()
    .HasOne(v => v.User)
    .WithMany()
    .HasForeignKey(v => v.UserId)
    .OnDelete(DeleteBehavior.Restrict);
```

**5.5 — إضافة Check Constraint لـ BoothReservation** (من المرحلة 4.3).

---

## خلاصة الملفات المؤثرة

### ملفات جديدة [NEW] — 5 ملفات

| الملف | المرحلة |
|---|---|
| `Enums/TicketStatus.cs` | 1 |
| `Enums/RegistrationStatus.cs` | 1 |
| `Enums/SubscriptionStatus.cs` | 1 |
| `Interfaces/IAuditableEntity.cs` | 2 |
| `Interfaces/ISoftDeletable.cs` | 2 |

### ملفات معدلة [MODIFY] — 20 ملف

| الملف | التعديلات |
|---|---|
| `Tenant.cs` | + UpdatedAt، + IAuditableEntity |
| `TenantSubscription.cs` | + UpdatedAt، Status → SubscriptionStatus enum |
| `Venue.cs` | + IAuditableEntity، + ISoftDeletable، + HashSet |
| `Hall.cs` | + IAuditableEntity، + ISoftDeletable، + HashSet |
| `Booth.cs` | + IAuditableEntity، + ISoftDeletable |
| `Exhibition.cs` | + UpdatedAt، + ISoftDeletable |
| `Exhibitor.cs` | + IAuditableEntity، + ISoftDeletable، + UserId FK |
| `BoothReservation.cs` | + IAuditableEntity، + ISoftDeletable |
| `Invoice.cs` | + IAuditableEntity |
| `Payment.cs` | + IAuditableEntity |
| `Service.cs` | + IAuditableEntity، + ISoftDeletable |
| `PricingPackage.cs` | + IAuditableEntity، + ISoftDeletable |
| `ServicePriceRule.cs` | + IAuditableEntity، + ISoftDeletable، ExhibitorCategory → enum |
| `BoothPriceRule.cs` | + IAuditableEntity، + ISoftDeletable |
| `Visitor.cs` | + IAuditableEntity، + ISoftDeletable، + UserId FK |
| `Ticket.cs` | + IAuditableEntity، + ISoftDeletable، Status → TicketStatus enum |
| `ScheduleRegistration.cs` | + IAuditableEntity، Status → RegistrationStatus enum |
| `Product.cs` | + IAuditableEntity، + ISoftDeletable |
| `PackageService.cs` | إعادة تسمية ID → PackageServiceID |
| `ReservationService.cs` | إعادة تسمية ID → ReservationServiceID |
| `BoothMerge.cs` | + HashSet على MergeItems |
| `FinancialReport.cs` | + ReportPeriodFrom، + ReportPeriodTo |
| `ApplicationDbContext.cs` | + Enum conversions، + Global Soft Delete Filters، + Indexes، + FK configs، + Check Constraint |

---

## خطة التحقق

### بعد كل مرحلة
```bash
dotnet build ExhibitionManagementSystem.Models
dotnet build ExhibitionManagementSystem.DataAccess
```

### بعد المرحلة 5 (Migration)
```bash
# إنشاء migration جديدة لرصد كل التغييرات
dotnet ef migrations add FixModelsLayer --project ExhibitionManagementSystem.DataAccess --startup-project ExhibitionManagementSystem

# مراجعة الـ migration المُنشأة قبل التطبيق
# ثم التطبيق:
dotnet ef database update --project ExhibitionManagementSystem.DataAccess --startup-project ExhibitionManagementSystem
```

> [!WARNING]
> **مراجعة الـ Migration مهمة جداً:**
> - تغيير `Status` من `string` إلى `Enum` في جداول بها بيانات سيتطلب migration script يحول القيم القديمة
> - إعادة تسمية `ID` إلى `PackageServiceID` / `ReservationServiceID` ستحذف العمود القديم وتنشئ جديداً إذا كان الجدول يحتوي بيانات — راجع الـ Migration Script يدوياً

> [!NOTE]
> **ترتيب التنفيذ صارم:** لا تبدأ مرحلة إلا بعد اكتمال ما قبلها والتحقق من البناء الناجح.
