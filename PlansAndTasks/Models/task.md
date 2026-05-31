# ✅ ملف المهام — إصلاح طبقة النماذج

> **الحالة الإجمالية:** ✅ مكتمل بالكامل
> **إجمالي المهام:** 45 مهمة (6 مراحل)

---

## 🔵 المرحلة 1 — إضافة Enums المفقودة
> **المجلد:** `ExhibitionManagementSystem.Models/Enums/`
> **التبعيات:** لا توجد — تبدأ أولاً

- [x] **1.1** إنشاء ملف `Enums/TicketStatus.cs`
  - [x] تعريف `namespace ExhibitionManagementSystem.Models.Enums`
  - [x] إضافة القيم: `Active, Used, Cancelled, Expired`

- [x] **1.2** إنشاء ملف `Enums/RegistrationStatus.cs`
  - [x] تعريف `namespace ExhibitionManagementSystem.Models.Enums`
  - [x] إضافة القيم: `Registered, Attended, Cancelled, NoShow`

- [x] **1.3** إنشاء ملف `Enums/SubscriptionStatus.cs`
  - [x] تعريف `namespace ExhibitionManagementSystem.Models.Enums`
  - [x] إضافة القيم: `Trial, Active, Suspended, Expired, Cancelled`

- [x] **1.4** التحقق: `dotnet build ExhibitionManagementSystem.Models` ✅ بدون أخطاء

---

## 🔵 المرحلة 2 — بناء Interfaces موحدة
> **المجلد الجديد:** `ExhibitionManagementSystem.Models/Interfaces/`
> **التبعيات:** لا توجد — يمكن التنفيذ مع أو بعد المرحلة 1

- [x] **2.1** إنشاء المجلد `Interfaces/` داخل مشروع Models

- [x] **2.2** إنشاء ملف `Interfaces/IAuditableEntity.cs`
  - [x] تعريف `namespace ExhibitionManagementSystem.Models.Interfaces`
  - [x] إضافة خاصية `DateTime CreatedAt { get; set; }`
  - [x] إضافة خاصية `DateTime? UpdatedAt { get; set; }`
  - [x] إضافة XML doc comment شارح

- [x] **2.3** إنشاء ملف `Interfaces/ISoftDeletable.cs`
  - [x] تعريف `namespace ExhibitionManagementSystem.Models.Interfaces`
  - [x] إضافة خاصية `bool IsDeleted { get; set; }`
  - [x] إضافة خاصية `DateTime? DeletedAt { get; set; }`
  - [x] إضافة خاصية `string? DeletedByUserId { get; set; }`
  - [x] إضافة XML doc comment شارح

- [x] **2.4** التحقق: `dotnet build ExhibitionManagementSystem.Models` ✅ بدون أخطاء

---

## 🔵 المرحلة 3 — تطبيق الـ Interfaces على النماذج وإضافة روابط المستخدمين
> **التبعيات:** المرحلتان 1 و2 مكتملتان

### 3-أ) النماذج التي تحتاج `IAuditableEntity` فقط (لديها `CreatedAt` بالفعل)

- [x] **3.1** تعديل `Tenant.cs`
  - [x] إضافة `using ExhibitionManagementSystem.Models.Interfaces`
  - [x] إضافة `: IAuditableEntity` للكلاس
  - [x] إضافة حقل `public DateTime? UpdatedAt { get; set; }`

- [x] **3.2** تعديل `TenantSubscription.cs`
  - [x] إضافة `using` للـ Interfaces والـ Enums
  - [x] إضافة `: IAuditableEntity` للكلاس
  - [x] إضافة حقل `public DateTime? UpdatedAt { get; set; }`
  - [x] **تغيير نوع `Status`** من `string` إلى `SubscriptionStatus`
  - [x] حذف `[Required, StringLength(20)]` من خاصية `Status`

- [x] **3.3** تعديل `Exhibition.cs`
  - [x] إضافة `using` للـ Interfaces
  - [x] إضافة `: IAuditableEntity, ISoftDeletable` للكلاس
  - [x] إضافة حقل `public DateTime? UpdatedAt { get; set; }`
  - [x] إضافة حقل `public bool IsDeleted { get; set; } = false`
  - [x] إضافة حقل `public DateTime? DeletedAt { get; set; }`
  - [x] إضافة حقل `public string? DeletedByUserId { get; set; }`

- [x] **3.4** تعديل `BoothReservation.cs`
  - [x] إضافة `using` للـ Interfaces
  - [x] إضافة `: IAuditableEntity, ISoftDeletable` للكلاس
  - [x] إضافة حقل `public DateTime? UpdatedAt { get; set; }`
  - [x] إضافة حقل `public bool IsDeleted { get; set; } = false`
  - [x] إضافة حقل `public DateTime? DeletedAt { get; set; }`
  - [x] إضافة حقل `public string? DeletedByUserId { get; set; }`

- [x] **3.5** تعديل `Invoice.cs`
  - [x] إضافة `using` للـ Interfaces
  - [x] إضافة `: IAuditableEntity` للكلاس
  - [x] إضافة حقل `public DateTime? UpdatedAt { get; set; }`

- [x] **3.6** تعديل `Payment.cs`
  - [x] إضافة `using` للـ Interfaces
  - [x] إضافة `: IAuditableEntity` للكلاس
  - [x] إضافة حقل `public DateTime? UpdatedAt { get; set; }`

- [x] **3.7** تعديل `ScheduleRegistration.cs`
  - [x] إضافة `using` للـ Interfaces والـ Enums
  - [x] إضافة `: IAuditableEntity` للكلاس
  - [x] إضافة حقل `public DateTime? UpdatedAt { get; set; }`
  - [x] **تغيير نوع `Status`** من `string` إلى `RegistrationStatus`
  - [x] حذف `[Required, StringLength(20)]` من خاصية `Status`

### 3-ب) النماذج التي تحتاج `IAuditableEntity` + إضافة `CreatedAt` جديد

- [x] **3.8** تعديل `Venue.cs`
  - [x] إضافة `using` للـ Interfaces
  - [x] إضافة `: IAuditableEntity, ISoftDeletable` للكلاس
  - [x] إضافة حقل `public DateTime CreatedAt { get; set; } = DateTime.UtcNow`
  - [x] إضافة حقل `public DateTime? UpdatedAt { get; set; }`
  - [x] إضافة حقل `public bool IsDeleted { get; set; } = false`
  - [x] إضافة حقل `public DateTime? DeletedAt { get; set; }`
  - [x] إضافة حقل `public string? DeletedByUserId { get; set; }`
  - [x] تهيئة `Halls` بـ `= new HashSet<Hall>()`

- [x] **3.9** تعديل `Hall.cs`
  - [x] إضافة `using` للـ Interfaces
  - [x] إضافة `: IAuditableEntity, ISoftDeletable` للكلاس
  - [x] إضافة حقل `public DateTime CreatedAt { get; set; } = DateTime.UtcNow`
  - [x] إضافة حقل `public DateTime? UpdatedAt { get; set; }`
  - [x] إضافة حقل `public bool IsDeleted { get; set; } = false`
  - [x] إضافة حقل `public DateTime? DeletedAt { get; set; }`
  - [x] إضافة حقل `public string? DeletedByUserId { get; set; }`
  - [x] تهيئة `Booths` بـ `= new HashSet<Booth>()`

- [x] **3.10** تعديل `Booth.cs`
  - [x] إضافة `using` للـ Interfaces
  - [x] إضافة `: IAuditableEntity, ISoftDeletable` للكلاس
  - [x] إضافة حقل `public DateTime CreatedAt { get; set; } = DateTime.UtcNow`
  - [x] إضافة حقل `public DateTime? UpdatedAt { get; set; }`
  - [x] إضافة حقل `public bool IsDeleted { get; set; } = false`
  - [x] إضافة حقل `public DateTime? DeletedAt { get; set; }`
  - [x] إضافة حقل `public string? DeletedByUserId { get; set; }`

- [x] **3.11** تعديل `Service.cs`
  - [x] إضافة `using` للـ Interfaces
  - [x] إضافة `: IAuditableEntity, ISoftDeletable` للكلاس
  - [x] إضافة حقل `public DateTime CreatedAt { get; set; } = DateTime.UtcNow`
  - [x] إضافة حقل `public DateTime? UpdatedAt { get; set; }`
  - [x] إضافة حقل `public bool IsDeleted { get; set; } = false`
  - [x] إضافة حقل `public DateTime? DeletedAt { get; set; }`
  - [x] إضافة حقل `public string? DeletedByUserId { get; set; }`

- [x] **3.12** تعديل `PricingPackage.cs`
  - [x] إضافة `using` للـ Interfaces
  - [x] إضافة `: IAuditableEntity, ISoftDeletable` للكلاس
  - [x] إضافة حقل `public DateTime CreatedAt { get; set; } = DateTime.UtcNow`
  - [x] إضافة حقل `public DateTime? UpdatedAt { get; set; }`
  - [x] إضافة حقل `public bool IsDeleted { get; set; } = false`
  - [x] إضافة حقل `public DateTime? DeletedAt { get; set; }`
  - [x] إضافة حقل `public string? DeletedByUserId { get; set; }`

- [x] **3.13** تعديل `BoothPriceRule.cs`
  - [x] إضافة `using` للـ Interfaces
  - [x] إضافة `: IAuditableEntity, ISoftDeletable` للكلاس
  - [x] إضافة حقل `public DateTime CreatedAt { get; set; } = DateTime.UtcNow`
  - [x] إضافة حقل `public DateTime? UpdatedAt { get; set; }`
  - [x] إضافة حقل `public bool IsDeleted { get; set; } = false`
  - [x] إضافة حقل `public DateTime? DeletedAt { get; set; }`
  - [x] إضافة حقل `public string? DeletedByUserId { get; set; }`

- [x] **3.14** تعديل `ServicePriceRule.cs`
  - [x] إضافة `using` للـ Interfaces والـ Enums
  - [x] إضافة `: IAuditableEntity, ISoftDeletable` للكلاس
  - [x] إضافة حقل `public DateTime CreatedAt { get; set; } = DateTime.UtcNow`
  - [x] إضافة حقل `public DateTime? UpdatedAt { get; set; }`
  - [x] إضافة حقل `public bool IsDeleted { get; set; } = false`
  - [x] إضافة حقل `public DateTime? DeletedAt { get; set; }`
  - [x] إضافة حقل `public string? DeletedByUserId { get; set; }`
  - [x] **تغيير نوع `ExhibitorCategory`** من `string` إلى `ExhibitorCategory?`
  - [x] حذف `[StringLength(20)]` من خاصية `ExhibitorCategory`

- [x] **3.15** تعديل `Product.cs`
  - [x] إضافة `using` للـ Interfaces
  - [x] إضافة `: IAuditableEntity, ISoftDeletable` للكلاس
  - [x] إضافة حقل `public DateTime CreatedAt { get; set; } = DateTime.UtcNow`
  - [x] إضافة حقل `public DateTime? UpdatedAt { get; set; }`
  - [x] إضافة حقل `public bool IsDeleted { get; set; } = false`
  - [x] إضافة حقل `public DateTime? DeletedAt { get; set; }`
  - [x] إضافة حقل `public string? DeletedByUserId { get; set; }`

### 3-ج) النماذج الحيوية مع ربط بـ ApplicationUser

- [x] **3.16** تعديل `Exhibitor.cs`
  - [x] إضافة `using` للـ Interfaces
  - [x] إضافة `: IAuditableEntity, ISoftDeletable` للكلاس
  - [x] إضافة حقل `public DateTime CreatedAt { get; set; } = DateTime.UtcNow`
  - [x] إضافة حقل `public DateTime? UpdatedAt { get; set; }`
  - [x] إضافة حقل `public bool IsDeleted { get; set; } = false`
  - [x] إضافة حقل `public DateTime? DeletedAt { get; set; }`
  - [x] إضافة حقل `public string? DeletedByUserId { get; set; }`
  - [x] إضافة حقل `[StringLength(450)] public string? UserId { get; set; }`
  - [x] إضافة `[ForeignKey(nameof(UserId))] public virtual ApplicationUser? User { get; set; }`

- [x] **3.17** تعديل `Visitor.cs`
  - [x] إضافة `using` للـ Interfaces
  - [x] إضافة `: IAuditableEntity, ISoftDeletable` للكلاس
  - [x] إضافة حقل `public DateTime? UpdatedAt { get; set; }`
  - [x] إضافة حقل `public bool IsDeleted { get; set; } = false`
  - [x] إضافة حقل `public DateTime? DeletedAt { get; set; }`
  - [x] إضافة حقل `public string? DeletedByUserId { get; set; }`
  - [x] إضافة حقل `[StringLength(450)] public string? UserId { get; set; }`
  - [x] إضافة `[ForeignKey(nameof(UserId))] public virtual ApplicationUser? User { get; set; }`
  - [x] ملاحظة: `RegisteredAt` يُعادل `CreatedAt` — لا حاجة لإعادة تسميته

- [x] **3.18** تعديل `Ticket.cs`
  - [x] إضافة `using` للـ Interfaces والـ Enums
  - [x] إضافة `: IAuditableEntity, ISoftDeletable` للكلاس
  - [x] إضافة حقل `public DateTime? UpdatedAt { get; set; }`
  - [x] إضافة حقل `public bool IsDeleted { get; set; } = false`
  - [x] إضافة حقل `public DateTime? DeletedAt { get; set; }`
  - [x] إضافة حقل `public string? DeletedByUserId { get; set; }`
  - [x] **تغيير نوع `Status`** من `string` إلى `TicketStatus`
  - [x] حذف `[Required, StringLength(20)]` من خاصية `Status`

- [x] **3.19** التحقق: `dotnet build ExhibitionManagementSystem.Models` ✅ بدون أخطاء

---

## 🔵 المرحلة 4 — الإصلاحات الهيكلية المتنوعة
> **التبعيات:** المرحلة 1 مكتملة

- [x] **4.1** تعديل `BoothMerge.cs` — تهيئة `MergeItems`
  - [x] تغيير `public virtual ICollection<BoothMergeItem> MergeItems { get; set; }` إلى `= new HashSet<BoothMergeItem>()`

- [x] **4.2** تعديل `PackageService.cs` — إعادة تسمية PK
  - [x] تغيير `[Key] public int ID { get; set; }` إلى `[Key] public int PackageServiceID { get; set; }`

- [x] **4.3** تعديل `ReservationService.cs` — إعادة تسمية PK
  - [x] تغيير `[Key] public int ID { get; set; }` إلى `[Key] public int ReservationServiceID { get; set; }`

- [x] **4.4** تعديل `FinancialReport.cs` — إضافة فترة التقرير
  - [x] إضافة `[Column(TypeName = "date")] public DateTime? ReportPeriodFrom { get; set; }`
  - [x] إضافة `[Column(TypeName = "date")] public DateTime? ReportPeriodTo { get; set; }`

- [x] **4.5** التحقق: `dotnet build ExhibitionManagementSystem.Models` ✅ بدون أخطاء

---

## 🔵 المرحلة 5 — تحديث `ApplicationDbContext`
> **التبعيات:** المراحل 1 و2 و3 و4 مكتملة
> **الملف:** `ExhibitionManagementSystem.DataAccess/ApplicationDbContext.cs`

- [x] **5.1** إضافة `using` للـ Interfaces والـ Enums الجديدة في أعلى الملف
  - [x] `using ExhibitionManagementSystem.Models.Interfaces;`
  - [x] `using System.Linq.Expressions;`

- [x] **5.2** إضافة تحويلات الـ Enums الجديدة في `OnModelCreating`
  - [x] `Ticket.Status` → `HasConversion<string>().HasMaxLength(20)`
  - [x] `ScheduleRegistration.Status` → `HasConversion<string>().HasMaxLength(20)`
  - [x] `TenantSubscription.Status` → `HasConversion<string>().HasMaxLength(20)`
  - [x] `ServicePriceRule.ExhibitorCategory` → `HasConversion<string>().HasMaxLength(20)`

- [x] **5.3** إضافة Global Query Filter للـ Soft Delete تلقائياً
  - [x] كتابة loop على `builder.Model.GetEntityTypes()`
  - [x] التحقق من `typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)`
  - [x] بناء `Expression.Lambda` لـ `IsDeleted == false`
  - [x] تطبيق `HasQueryFilter` على كل كيان

- [x] **5.4** إضافة FK Configurations لعلاقات المستخدم الجديدة
  - [x] `Exhibitor → ApplicationUser` (FK: UserId، OnDelete: Restrict)
  - [x] `Visitor → ApplicationUser` (FK: UserId، OnDelete: Restrict)

- [x] **5.5** إضافة Check Constraint على `BoothReservation`
  - [x] قيد `CK_BoothReservation_BoothOrMerge` يمنع وجود BoothID وMergeID معاً

- [x] **5.6** إضافة Indexes الجديدة للـ Soft Delete
  - [x] `Exhibitor` → Index على `(TenantID, IsDeleted)`
  - [x] `Visitor` → Index على `(TenantID, IsDeleted)`
  - [x] `Booth` → Index مُحدَّث على `(HallID, IsDeleted, Status)`
  - [x] `BoothReservation` → Index مُحدَّث يشمل `IsDeleted`

- [x] **5.7** التحقق: `dotnet build ExhibitionManagementSystem.DataAccess` ✅ بدون أخطاء
- [x] **5.8** التحقق: `dotnet build` لكامل الـ Solution ✅ بدون أخطاء

---

## 🔵 المرحلة 6 — إنشاء وتطبيق Database Migration

- [x] **6.1** مراجعة يدوية للتغييرات قبل إنشاء الـ Migration

- [x] **6.2** إنشاء Migration جديدة
  - [x] تشغيل `dotnet ef migrations add FixModelsLayer`

- [x] **6.3** مراجعة ملف الـ Migration المُنشأ
  - [x] التحقق من أن تغيير `Ticket.Status` محفوظ (نفس العمود وليس حذف وإنشاء)
  - [x] التحقق من صحة `PackageServiceID` و`ReservationServiceID`
  - [x] التحقق من وجود `Check Constraint` في الـ Migration

- [x] **6.4** تطبيق الـ Migration
  - [x] تشغيل `dotnet ef database update`

- [x] **6.5** التحقق النهائي: بناء كامل الـ Solution ✅ بدون أخطاء

---

## 📊 ملخص التقدم

| المرحلة | الوصف | الحالة | عدد المهام |
|---|---|:---:|:---:|
| 1 | Enums المفقودة | ✅ | 4 |
| 2 | Interfaces موحدة | ✅ | 4 |
| 3 | تطبيق الـ Interfaces على النماذج | ✅ | 19 |
| 4 | الإصلاحات الهيكلية | ✅ | 5 |
| 5 | تحديث ApplicationDbContext | ✅ | 8 |
| 6 | Database Migration | ✅ | 5 |
| **المجموع** | | **✅** | **45** |
