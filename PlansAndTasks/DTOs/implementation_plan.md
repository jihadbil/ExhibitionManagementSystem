# خطة تنفيذ طبقة نواقل البيانات (DTOs) مع AutoMapper

## الهدف

بناء طبقة `ExhibitionManagementSystem.Models.DTOs` شاملة لتغليف جميع بيانات النماذج (Domain Entities) في كائنات نقل محكمة ومنظمة، مع إعداد تحويل تلقائي باستخدام مكتبة **AutoMapper**، وتغطية كاملة لـ **DTOs المصادقة (Auth)** التي تعمل بشكل مستقل عن AutoMapper.

---

## تحليل طبقة النماذج الحالية

### الكيانات الموجودة (33 نموذج)

| الكيان | النوع | الملاحظات |
|--------|-------|-----------|
| `Tenant` | Core | متعدد المستأجرين - أساسي لكل الكيانات |
| `ApplicationUser` | Identity | IdentityUser ممتد |
| `ApplicationRole` | Identity | IdentityRole |
| `Currency` | Lookup | مفتاح رمزي (3 أحرف) |
| `ExchangeRate` | Lookup | أسعار الصرف |
| `Venue` | Core | قاعة المعارض الكبرى |
| `Hall` | Core | صالات داخل الـ Venue |
| `Booth` | Core | الأكشاك - تحتوي بيانات مكانية (PosX,Y,W,H) |
| `BoothMerge` | Core | دمج أكشاك |
| `BoothMergeItem` | Core | عناصر الدمج |
| `Exhibition` | Core | المعرض الرئيسي |
| `ExhibitionSchedule` | Core | جدول الفعاليات |
| `Exhibitor` | Core | الشركات العارضة |
| `BoothReservation` | Transactional | حجز الكشك - مركز المعاملات |
| `ReservationService` | Transactional | خدمات ضمن الحجز |
| `Service` | Catalog | قائمة الخدمات |
| `ServicePriceRule` | Pricing | قواعد تسعير الخدمات |
| `BoothPriceRule` | Pricing | قواعد تسعير الأكشاك |
| `PricingPackage` | Pricing | الباقات التسعيرية |
| `PackageService` | Pricing | خدمات ضمن الباقة |
| `Invoice` | Financial | الفاتورة |
| `Payment` | Financial | المدفوعات |
| `Ticket` | Visitor | تذاكر الزوار |
| `TicketScan` | Visitor | تسجيل دخول الزوار |
| `Visitor` | Visitor | بيانات الزائر |
| `VisitorRating` | Visitor | تقييمات الزوار |
| `TenantSubscription` | Admin | اشتراكات المستأجرين |
| `Product` | Catalog | المنتجات |
| `ScheduleRegistration` | Visitor | تسجيل في الجدول |
| `BoothStaff` | Core | موظفو الكشك |
| `FinancialReport` | Report | التقارير المالية |
| `AuditLog` | System | سجل التدقيق |

### الأنماط المشتركة في النماذج

- **`IAuditableEntity`**: `CreatedAt`, `UpdatedAt`
- **`ISoftDeletable`**: `IsDeleted`, `DeletedAt`, `DeletedByUserId`
- **حقول Enum**: 14 enum مختلف في مجلد `Enums/`
- **حقول مكانية (Booth)**: `PosX`, `PosY`, `Width`, `Height`, `RotationAngle`, `ShapeType`, `ShapePolygonJSON`
- **حقول مالية**: `decimal` مع `TypeName` محدد

---

## اختيار مكتبة التحويل: AutoMapper

**السبب**: AutoMapper هي المكتبة الأكثر نضجًا وانتشارًا في نظام .NET لتحويل الكائنات تلقائيًا.

### المميزات المستخدمة

| الميزة | الاستخدام |
|---------|-----------|
| `CreateMap<TSource, TDest>()` | التعيين الأساسي |
| `ReverseMap()` | التحويل في الاتجاهين |
| `ForMember()` | تخصيص حقول معينة |
| `ProjectTo<T>()` | الإسقاط المباشر على IQueryable (لطبقة DataAccess) |
| `Profile` | تنظيم التعيينات في ملفات منفصلة |
| `MapperConfiguration` | التكوين المركزي |

---

## هيكل مشروع DTOs المقترح

```
ExhibitionManagementSystem.Models.DTOs/
├── ExhibitionManagementSystem.Models.DTOs.csproj   ← تحديث (إضافة AutoMapper)
│
├── Common/
│   ├── AuditDto.cs              ← حقول المراجعة المشتركة (Base)
│   └── PagedResultDto.cs        ← نتائج الصفحات
│
├── Tenant/
│   ├── TenantDto.cs
│   ├── TenantCreateDto.cs
│   └── TenantUpdateDto.cs
│
├── Venue/
│   ├── VenueDto.cs
│   ├── VenueCreateDto.cs
│   ├── VenueUpdateDto.cs
│   └── VenueSummaryDto.cs
│
├── Hall/
│   ├── HallDto.cs
│   ├── HallCreateDto.cs
│   ├── HallUpdateDto.cs
│   └── HallSummaryDto.cs
│
├── Booth/
│   ├── BoothDto.cs              ← شامل للبيانات المكانية
│   ├── BoothCreateDto.cs
│   ├── BoothUpdateDto.cs
│   ├── BoothSummaryDto.cs
│   ├── BoothMergeDto.cs
│   ├── BoothMergeCreateDto.cs
│   └── BoothMergeItemDto.cs
│
├── Exhibition/
│   ├── ExhibitionDto.cs
│   ├── ExhibitionCreateDto.cs
│   ├── ExhibitionUpdateDto.cs
│   ├── ExhibitionSummaryDto.cs
│   └── ExhibitionScheduleDto.cs
│
├── Exhibitor/
│   ├── ExhibitorDto.cs
│   ├── ExhibitorCreateDto.cs
│   ├── ExhibitorUpdateDto.cs
│   └── ExhibitorSummaryDto.cs
│
├── Reservation/
│   ├── BoothReservationDto.cs
│   ├── BoothReservationCreateDto.cs
│   ├── BoothReservationUpdateDto.cs
│   ├── ReservationServiceDto.cs
│   └── ReservationServiceCreateDto.cs
│
├── Financial/
│   ├── InvoiceDto.cs
│   ├── InvoiceCreateDto.cs
│   ├── PaymentDto.cs
│   ├── PaymentCreateDto.cs
│   ├── FinancialReportDto.cs
│   └── ExchangeRateDto.cs
│
├── Pricing/
│   ├── BoothPriceRuleDto.cs
│   ├── BoothPriceRuleCreateDto.cs
│   ├── ServicePriceRuleDto.cs
│   ├── ServicePriceRuleCreateDto.cs
│   ├── PricingPackageDto.cs
│   └── PricingPackageCreateDto.cs
│
├── Service/
│   ├── ServiceDto.cs
│   ├── ServiceCreateDto.cs
│   └── ServiceSummaryDto.cs
│
├── Visitor/
│   ├── VisitorDto.cs
│   ├── VisitorCreateDto.cs
│   ├── TicketDto.cs
│   ├── TicketCreateDto.cs
│   ├── TicketScanDto.cs
│   ├── VisitorRatingDto.cs
│   └── VisitorRatingSummaryDto.cs
│
├── Currency/
│   └── CurrencyDto.cs
│
├── Admin/
│   ├── TenantSubscriptionDto.cs
│   ├── AuditLogDto.cs
│   └── ApplicationUserDto.cs
│
├── Auth/                              ← ✨ جديد - لا تستخدم AutoMapper
│   ├── LoginRequestDto.cs
│   ├── LoginResponseDto.cs
│   ├── RegisterRequestDto.cs
│   ├── RefreshTokenRequestDto.cs
│   ├── RefreshTokenResponseDto.cs
│   ├── ChangePasswordDto.cs
│   ├── ResetPasswordRequestDto.cs
│   ├── ResetPasswordConfirmDto.cs
│   ├── UserProfileDto.cs
│   ├── UpdateProfileDto.cs
│   ├── UserManagementDto.cs
│   ├── UserManagementCreateDto.cs
│   ├── RoleDto.cs
│   └── AssignRoleDto.cs
│
└── Mapping/
    ├── MappingProfile.cs              ← Profile رئيسي (AutoMapper)
    ├── Profiles/
    │   ├── TenantMappingProfile.cs
    │   ├── VenueMappingProfile.cs
    │   ├── HallMappingProfile.cs
    │   ├── BoothMappingProfile.cs
    │   ├── ExhibitionMappingProfile.cs
    │   ├── ExhibitorMappingProfile.cs
    │   ├── ReservationMappingProfile.cs
    │   ├── FinancialMappingProfile.cs
    │   ├── PricingMappingProfile.cs
    │   ├── ServiceMappingProfile.cs
    │   ├── VisitorMappingProfile.cs
    │   └── AdminMappingProfile.cs
    └── AutoMapperExtensions.cs        ← DI Registration Helper
```

---

## أنواع DTOs لكل كيان

### القاعدة العامة لكل كيان

| نوع DTO | الغرض | يحتوي على |
|---------|-------|-----------|
| `XxxDto` (Full) | القراءة الكاملة | كل الحقول + الكائنات المرتبطة المسطّحة |
| `XxxSummaryDto` | قوائم وبحث | الحقول الأساسية فقط للأداء |
| `XxxCreateDto` | إنشاء سجل جديد | حقول الإدخال بدون ID وتواريخ النظام |
| `XxxUpdateDto` | تحديث سجل | حقول قابلة للتعديل |

---

## تفاصيل DTOs الرئيسية

### 1. Common/AuditDto.cs
```csharp
namespace ExhibitionManagementSystem.Models.DTOs.Common;

public abstract class AuditDto
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

### 2. Common/PagedResultDto.cs
```csharp
namespace ExhibitionManagementSystem.Models.DTOs.Common;

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
```

### 3. Exhibition/ExhibitionDto.cs
```csharp
public class ExhibitionDto : AuditDto
{
    public int ExhibitionID { get; set; }
    public int TenantID { get; set; }
    public int VenueID { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string Edition { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; }          // Enum → string
    public string Description { get; set; }
    public int? ExpectedVisitors { get; set; }
    public decimal? EntryFee { get; set; }
    public string EntryCurrency { get; set; }
    public string VenueName { get; set; }       // ← مسطّح من Venue
    public string CurrencySymbol { get; set; }  // ← مسطّح من Currency
}
```

### 4. Booth/BoothDto.cs
```csharp
public class BoothDto : AuditDto
{
    public int BoothID { get; set; }
    public int HallID { get; set; }
    public string HallName { get; set; }        // مسطّح
    public string BoothNumber { get; set; }
    public decimal OriginalAreaSqM { get; set; }
    public decimal CurrentAreaSqM { get; set; }
    public string Status { get; set; }
    public bool IsMerged { get; set; }
    public int? MergeID { get; set; }
    // بيانات مكانية
    public decimal? PosX { get; set; }
    public decimal? PosY { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public decimal? RotationAngle { get; set; }
    public string? ShapeType { get; set; }
    public string ShapePolygonJSON { get; set; }
}
```

### 5. Reservation/BoothReservationDto.cs (الأكثر تعقيدًا)
```csharp
public class BoothReservationDto : AuditDto
{
    public int ReservationID { get; set; }
    public int ExhibitorID { get; set; }
    public string ExhibitorName { get; set; }   // مسطّح
    public int? BoothID { get; set; }
    public string BoothNumber { get; set; }     // مسطّح
    public int ExhibitionID { get; set; }
    public string ExhibitionName { get; set; }  // مسطّح
    public string BoothTypeSelected { get; set; }
    public decimal RequestedAreaSqM { get; set; }
    public decimal AllocatedAreaSqM { get; set; }
    public string ExhibitorCategory { get; set; }
    public decimal BoothAmount { get; set; }
    public decimal ServicesAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; }
    public string CurrencySymbol { get; set; }  // مسطّح
    public decimal ExchangeRateUsed { get; set; }
    public decimal AmountInBaseCurrency { get; set; }
    public string Status { get; set; }
    public DateTime ReservationDate { get; set; }
    public string LogisticNotes { get; set; }
    public List<ReservationServiceDto> Services { get; set; } = [];
}
```

---

## تفاصيل Auth DTOs

> [!IMPORTANT]
> DTOs المصادقة **لا تُعيَّن بـ AutoMapper** — تُملأ يدويًا في `AuthService`
> باستخدام `UserManager<ApplicationUser>` و`SignInManager` من ASP.NET Identity.

### Auth/LoginRequestDto.cs
```csharp
public class LoginRequestDto
{
    [Required, EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }

    public bool RememberMe { get; set; } = false;
}
```

### Auth/LoginResponseDto.cs
```csharp
public class LoginResponseDto
{
    public string AccessToken { get; set; }     // JWT Token
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string UserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public int TenantID { get; set; }
    public string TenantName { get; set; }      // مسطّح من Tenant
    public string BaseCurrency { get; set; }    // مسطّح من Tenant
    public List<string> Roles { get; set; } = [];
}
```

### Auth/RegisterRequestDto.cs
```csharp
public class RegisterRequestDto
{
    [Required, StringLength(100)]
    public string FullName { get; set; }

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; }

    [Required, MinLength(8)]
    public string Password { get; set; }

    [Required, Compare(nameof(Password))]
    public string ConfirmPassword { get; set; }

    public int TenantID { get; set; }
    public string? InitialRole { get; set; }    // الدور الأولي (Admin/Staff...)
}
```

### Auth/RefreshTokenRequestDto.cs
```csharp
public class RefreshTokenRequestDto
{
    [Required] public string AccessToken { get; set; }
    [Required] public string RefreshToken { get; set; }
}
```

### Auth/RefreshTokenResponseDto.cs
```csharp
public class RefreshTokenResponseDto
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
}
```

### Auth/ChangePasswordDto.cs
```csharp
public class ChangePasswordDto
{
    [Required] public string CurrentPassword { get; set; }
    [Required, MinLength(8)] public string NewPassword { get; set; }
    [Required, Compare(nameof(NewPassword))] public string ConfirmNewPassword { get; set; }
}
```

### Auth/ResetPasswordRequestDto.cs
```csharp
// الخطوة 1: المستخدم يدخل بريده الإلكتروني
public class ResetPasswordRequestDto
{
    [Required, EmailAddress]
    public string Email { get; set; }
}
```

### Auth/ResetPasswordConfirmDto.cs
```csharp
// الخطوة 2: تأكيد إعادة التعيين عبر Token
public class ResetPasswordConfirmDto
{
    [Required] public string Email { get; set; }
    [Required] public string Token { get; set; }         // من رابط البريد
    [Required, MinLength(8)] public string NewPassword { get; set; }
    [Required, Compare(nameof(NewPassword))] public string ConfirmPassword { get; set; }
}
```

### Auth/UserProfileDto.cs
```csharp
// عرض بيانات المستخدم الحالي (GET /me)
public class UserProfileDto
{
    public string UserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public int TenantID { get; set; }
    public string TenantName { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLogin { get; set; }
    public List<string> Roles { get; set; } = [];
}
```

### Auth/UpdateProfileDto.cs
```csharp
public class UpdateProfileDto
{
    [Required, StringLength(100)]
    public string FullName { get; set; }

    [StringLength(20)]
    public string PhoneNumber { get; set; }
}
```

### Auth/UserManagementDto.cs
```csharp
// للأدمن - عرض قائمة المستخدمين
public class UserManagementDto
{
    public string UserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public int TenantID { get; set; }
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime? LastLogin { get; set; }
    public List<string> Roles { get; set; } = [];
}
```

### Auth/UserManagementCreateDto.cs
```csharp
// للأدمن - إنشاء مستخدم جديد مباشرة
public class UserManagementCreateDto
{
    [Required, StringLength(100)] public string FullName { get; set; }
    [Required, EmailAddress] public string Email { get; set; }
    [Required, MinLength(8)] public string Password { get; set; }
    public int TenantID { get; set; }
    public List<string> Roles { get; set; } = [];
    public bool IsActive { get; set; } = true;
}
```

### Auth/RoleDto.cs
```csharp
public class RoleDto
{
    public string RoleId { get; set; }
    public string Name { get; set; }
    public int TenantID { get; set; }
}
```

### Auth/AssignRoleDto.cs
```csharp
public class AssignRoleDto
{
    [Required] public string UserId { get; set; }
    [Required] public string RoleName { get; set; }
}
```

---

## إعداد AutoMapper

### التبعيات المطلوبة (NuGet)

```xml
<!-- في ExhibitionManagementSystem.Models.DTOs.csproj -->
<PackageReference Include="AutoMapper" Version="14.0.0" />
```

> **ملاحظة**: AutoMapper 14.x أزال `AutoMapper.Extensions.Microsoft.DependencyInjection` ودمجها في الحزمة الأساسية.

### بنية MappingProfile

```csharp
// Mapping/Profiles/ExhibitionMappingProfile.cs
using AutoMapper;

namespace ExhibitionManagementSystem.Models.DTOs.Mapping.Profiles;

public class ExhibitionMappingProfile : Profile
{
    public ExhibitionMappingProfile()
    {
        // Exhibition → ExhibitionDto (مع تسطيح الكائنات المرتبطة)
        CreateMap<Exhibition, ExhibitionDto>()
            .ForMember(d => d.Status,        o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.VenueName,     o => o.MapFrom(s => s.Venue != null ? s.Venue.Name : null))
            .ForMember(d => d.CurrencySymbol,o => o.MapFrom(s => s.Currency != null ? s.Currency.Symbol : null));

        // Exhibition → ExhibitionSummaryDto
        CreateMap<Exhibition, ExhibitionSummaryDto>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

        // ExhibitionCreateDto → Exhibition
        CreateMap<ExhibitionCreateDto, Exhibition>()
            .ForMember(d => d.ExhibitionID, o => o.Ignore())
            .ForMember(d => d.CreatedAt,    o => o.Ignore())
            .ForMember(d => d.UpdatedAt,    o => o.Ignore())
            .ForMember(d => d.IsDeleted,    o => o.Ignore());
    }
}
```

### التسجيل في Dependency Injection

```csharp
// Mapping/AutoMapperExtensions.cs
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace ExhibitionManagementSystem.Models.DTOs.Mapping;

public static class AutoMapperExtensions
{
    public static IServiceCollection AddDtoMapping(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(MappingProfile).Assembly);
        return services;
    }
}
```

### نقطة التجميع الرئيسية

```csharp
// Mapping/MappingProfile.cs - AutoMapper يكتشف تلقائيًا كل Profile في نفس الـ Assembly
public class MappingProfile : Profile { }
```

---

## خطة التنفيذ التفصيلية (12 مرحلة)

### المرحلة 1: تحضير المشروع
- [ ] تحديث `ExhibitionManagementSystem.Models.DTOs.csproj` لإضافة AutoMapper 14
- [ ] إنشاء هيكل المجلدات الكامل (بما فيها `Auth/`)

### المرحلة 2: DTOs المشتركة (Common)
- [ ] `Common/AuditDto.cs`
- [ ] `Common/PagedResultDto.cs`

### المرحلة 3: DTOs المصادقة (Auth) ← بدون AutoMapper
- [ ] `Auth/LoginRequestDto.cs`
- [ ] `Auth/LoginResponseDto.cs`
- [ ] `Auth/RegisterRequestDto.cs`
- [ ] `Auth/RefreshTokenRequestDto.cs`
- [ ] `Auth/RefreshTokenResponseDto.cs`
- [ ] `Auth/ChangePasswordDto.cs`
- [ ] `Auth/ResetPasswordRequestDto.cs`
- [ ] `Auth/ResetPasswordConfirmDto.cs`
- [ ] `Auth/UserProfileDto.cs`
- [ ] `Auth/UpdateProfileDto.cs`
- [ ] `Auth/UserManagementDto.cs`
- [ ] `Auth/UserManagementCreateDto.cs`
- [ ] `Auth/RoleDto.cs`
- [ ] `Auth/AssignRoleDto.cs`

### المرحلة 4: DTOs Lookup
- [ ] `Currency/CurrencyDto.cs`
- [ ] `Financial/ExchangeRateDto.cs`
- [ ] `Tenant/TenantDto.cs` + Create + Update

### المرحلة 5: DTOs البنية التحتية
- [ ] `Venue/` — VenueDto, VenueSummaryDto, VenueCreateDto, VenueUpdateDto
- [ ] `Hall/` — HallDto, HallSummaryDto, HallCreateDto, HallUpdateDto
- [ ] `Booth/` — BoothDto, BoothSummaryDto, BoothCreateDto, BoothUpdateDto, BoothMergeDto, BoothMergeCreateDto, BoothMergeItemDto

### المرحلة 6: DTOs الكيانات الرئيسية
- [ ] `Exhibition/` — 5 DTOs
- [ ] `Exhibitor/` — 4 DTOs
- [ ] `Service/` — ServiceDto, ServiceCreateDto, ServiceSummaryDto

### المرحلة 7: DTOs المعاملات
- [ ] `Reservation/` — 5 DTOs (BoothReservation + ReservationService)

### المرحلة 8: DTOs المالية والتسعير
- [ ] `Financial/` — InvoiceDto, InvoiceCreateDto, PaymentDto, PaymentCreateDto, FinancialReportDto
- [ ] `Pricing/` — 6 DTOs (BoothPriceRule, ServicePriceRule, PricingPackage)

### المرحلة 9: DTOs الزوار
- [ ] `Visitor/` — VisitorDto, VisitorCreateDto, TicketDto, TicketCreateDto, TicketScanDto, VisitorRatingDto, VisitorRatingSummaryDto

### المرحلة 10: DTOs الإدارة
- [ ] `Admin/` — TenantSubscriptionDto, AuditLogDto, ApplicationUserDto

### المرحلة 11: طبقة AutoMapper
- [ ] `Mapping/MappingProfile.cs` — نقطة التجميع
- [ ] `Mapping/Profiles/` — 12 Profile منفصل
- [ ] `Mapping/AutoMapperExtensions.cs` — DI Helper

### المرحلة 12: التحقق
- [ ] `dotnet build` بدون أخطاء
- [ ] `config.AssertConfigurationIsValid()` للتحقق من اكتمال التعيينات

---

## قرارات تصميمية مهمة

> [!IMPORTANT]
> ### Enum → String
> كل خصائص Enum في النماذج ستُحوَّل إلى `string` في DTOs.
> سبب القرار: الأمان من التحويل بين إصدارات البيانات، وسهولة التسلسل JSON.

> [!IMPORTANT]
> ### تسطيح (Flattening) العلاقات
> DTOs من نوع Full تحتوي على حقول مسطّحة من الكائنات المرتبطة
> (مثل: `VenueName` بدل `Venue.Name`). DTOs من نوع Summary تحتوي على الحد الأدنى فقط.

> [!NOTE]
> ### AutoMapper ProjectTo<T>()
> طبقة DataAccess ستستخدم `ProjectTo<T>()` بدل `Map<T>()` عند الاستعلام
> للاستفادة من ترجمة LINQ إلى SQL مباشرةً وتحسين الأداء.

> [!NOTE]
> ### لا Circular References
> DTOs لا تحتوي على مراجع دائرية — كل DTO يعرض فقط الكائنات الأبناء
> وليس الكائنات الآباء (BoothDto لا يحتوي HallDto كاملاً بل HallName فقط).

> [!NOTE]
> ### AutoMapper 14.x
> النسخة 14 دمجت `Microsoft.Extensions.DependencyInjection` في الحزمة الأساسية.
> يستخدم `services.AddAutoMapper(assembly)` مباشرةً بدون حزمة إضافية.

> [!IMPORTANT]
> ### Auth DTOs لا تستخدم AutoMapper
> كائنات المصادقة (`Auth/`) تُبنى يدويًا داخل `AuthService` باستخدام:
> - `UserManager<ApplicationUser>` لعمليات CRUD على المستخدمين
> - `SignInManager<ApplicationUser>` لتسجيل الدخول
> - لا يوجد `AuthMappingProfile` — هذا مقصود وليس نسيانًا.

---

## ملخص الإحصائيات

| الفئة | عدد الملفات |
|-------|-------------|
| Common | 2 |
| Auth (بدون AutoMapper) | 14 |
| Currency | 1 |
| Tenant | 3 |
| Venue | 4 |
| Hall | 4 |
| Booth | 7 |
| Exhibition | 5 |
| Exhibitor | 4 |
| Service | 3 |
| Reservation | 5 |
| Financial | 6 |
| Pricing | 6 |
| Visitor | 7 |
| Admin | 3 |
| Mapping Profiles | 13 |
| **المجموع** | **~87 ملف** |
