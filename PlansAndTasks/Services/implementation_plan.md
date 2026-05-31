# خطة بناء طبقة الخدمات (Service Layer)

## نظرة عامة

بناءً على التحليل الشامل لطبقتي البيانات والنواقل:
- **طبقة Repository**: تحتوي على **29 repository** مغطاة بـ `IUnitOfWork` مع دعم كامل للـ Soft Delete، Pagination، Transactions، و Audit Logging تلقائي.
- **طبقة DTOs**: تحتوي على **~91 ملف** موزعة على 15 مجموعة مع AutoMapper profiles جاهزة.
- **طبقة Services**: فارغة حالياً — تحتوي فقط على ملف `csproj` بدون مراجع.

الهدف: بناء طبقة خدمات نظيفة تربط الـ Repository بالـ DTOs، مع منطق أعمال كامل.

---

## تحليل الطبقات الحالية

### Repository Interfaces المتاحة (عبر IUnitOfWork)
| Repository | عمليات مخصصة مهمة |
|---|---|
| `ITenantRepository` | `GetBySubdomainAsync`, `IsSubdomainUniqueAsync`, `GetWithActiveSubscriptionAsync` |
| `IVenueRepository` | Generic فقط |
| `IHallRepository` | Generic فقط |
| `IBoothRepository` | Generic فقط |
| `IBoothMergeRepository` | Generic فقط |
| `IExhibitionRepository` | `GetByTenant`, `GetByStatus`, `GetActiveExhibitions`, `GetUpcomingExhibitions`, `GetWithVenueAndSchedules` |
| `IExhibitorRepository` | `GetByUserId`, `GetByCategory`, `GetWithReservations`, `SearchAsync` |
| `IBoothReservationRepository` | `GetByExhibition`, `GetByExhibitor`, `GetByStatus`, `IsBoothReserved`, `IsMergeReserved`, `GetTotalRevenue`, `GetUnpaidReservations` |
| `IBoothPriceRuleRepository` | `GetApplicableRuleAsync` (محرك التسعير) |
| `IServicePriceRuleRepository` | `GetApplicableRuleAsync` |
| `IInvoiceRepository` | `GetByReservation`, `GetByStatus`, `GetWithPayments`, `GetOverdueInvoices`, `GenerateNextInvoiceNumber` |
| `IPaymentRepository` | `GetByInvoice`, `GetTotalPaid`, `GetByDateRange` |
| `ITicketRepository` | `GetByQRCode`, `GetByExhibition`, `IsQRCodeUnique`, `GetActiveTicketCount` |
| `ITicketScanRepository` | Generic |
| `IVisitorRepository` | `GetByEmail`, `GetByUserId`, `SearchAsync`, `GetWithTickets` |
| `IVisitorRatingRepository` | `GetByExhibition`, `GetAverageRating`, `HasVisitorRated` |
| `ICurrencyRepository` | Generic |
| `IExchangeRateRepository` | Generic |
| `IAuditLogRepository` | Generic |

### نمط البيانات المرتجعة
- الـ Repository يرجع **Domain Models**
- الـ Services تتلقى **DTOs** من الـ Controller وترجع **DTOs** له
- **AutoMapper** هو الجسر بينهما

---

## قرارات تصميمية

- **نمط Result Wrapper**: جميع الـ Service Methods ترجع `ServiceResult<T>` بدلاً من رمي استثناءات عشوائية.
- **Multi-tenancy**: كل service method تأخذ `tenantId` للتأكد من عزل البيانات بين المستأجرين.
- **Authentication داخل نفس الطبقة**: خدمات المصادقة (`IAuthService`) تُبنى مباشرة في مشروع `Services` وتستخدم `UserManager<ApplicationUser>` و `RoleManager<ApplicationRole>` من ASP.NET Identity مُحقنةً عبر DI — ولا تُعالج في طبقة API.
- **JWT**: يُولَّد الـ Token داخل `AuthService` مع دعم Refresh Tokens.

---

## هيكل المشروع المقترح

```
ExhibitionManagementSystem.Services/
├── Common/
│   └── ServiceResult.cs
├── Interfaces/
│   ├── IAuthService.cs              ← جديد
│   ├── ITenantService.cs
│   ├── IVenueService.cs
│   ├── IHallService.cs
│   ├── IBoothService.cs
│   ├── IExhibitionService.cs
│   ├── IExhibitorService.cs
│   ├── IReservationService.cs
│   ├── IFinancialService.cs
│   ├── IPricingService.cs
│   ├── IVisitorService.cs
│   ├── ITicketService.cs
│   ├── ICurrencyService.cs
│   ├── IServiceManagementService.cs
│   ├── IReportService.cs
│   └── IAdminService.cs
├── Implementations/
│   ├── AuthService.cs               ← جديد
│   ├── TenantService.cs
│   ├── VenueService.cs
│   ├── HallService.cs
│   ├── BoothService.cs
│   ├── ExhibitionService.cs
│   ├── ExhibitorService.cs
│   ├── ReservationService.cs
│   ├── FinancialService.cs
│   ├── PricingService.cs
│   ├── VisitorService.cs
│   ├── TicketService.cs
│   ├── CurrencyService.cs
│   ├── ServiceManagementService.cs
│   ├── ReportService.cs
│   └── AdminService.cs
└── Extensions/
    └── ServiceLayerExtensions.cs
```

**إجمالي الملفات**: ~34 ملف

---

## المرحلة 0: خدمة المصادقة (AuthService)

> هذه المرحلة تُنفَّذ أولاً لأن خدمات أخرى (مثل VisitorService وExhibitorService) ستعتمد عليها عند إنشاء مستخدمين مرتبطين.

### التبعيات المطلوبة

يجب إضافة `Microsoft.AspNetCore.Identity` إلى مشروع الخدمات عبر حقن `UserManager` و `RoleManager` و `SignInManager` من DI.

```xml
<!-- في ExhibitionManagementSystem.Services.csproj -->
<ItemGroup>
  <ProjectReference Include="..\ExhibitionManagementSystem.DataAccess\..." />
  <ProjectReference Include="..\ExhibitionManagementSystem.Models.DTOs\..." />
</ItemGroup>
<ItemGroup>
  <!-- Identity متاحة عبر ASP.NET Core framework reference -->
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

### [NEW] Interfaces/IAuthService.cs

#### قسم 1: تسجيل الدخول والتسجيل

| Method | Input | Output | وصف |
|---|---|---|---|
| `LoginAsync` | `LoginRequestDto` | `ServiceResult<LoginResponseDto>` | تسجيل دخول وإرجاع JWT + Refresh Token |
| `RegisterAsync` | `RegisterRequestDto` | `ServiceResult<UserManagementDto>` | إنشاء مستخدم جديد وربطه بمستأجر |
| `LogoutAsync` | `string userId` | `ServiceResult` | إبطال Refresh Token |

#### قسم 2: JWT و Refresh Tokens

| Method | Input | Output | وصف |
|---|---|---|---|
| `RefreshTokenAsync` | `RefreshTokenRequestDto` | `ServiceResult<RefreshTokenResponseDto>` | تجديد Access Token باستخدام Refresh Token صالح |
| `RevokeTokenAsync` | `string userId` | `ServiceResult` | إبطال جميع Refresh Tokens للمستخدم |

#### قسم 3: إدارة كلمة المرور

| Method | Input | Output | وصف |
|---|---|---|---|
| `ChangePasswordAsync` | `string userId, ChangePasswordDto` | `ServiceResult` | تغيير كلمة المرور مع التحقق من القديمة |
| `ForgotPasswordAsync` | `ResetPasswordRequestDto` | `ServiceResult` | توليد رمز إعادة التعيين (يُرسل عبر API) |
| `ResetPasswordAsync` | `ResetPasswordConfirmDto` | `ServiceResult` | إعادة تعيين كلمة المرور برمز التحقق |

#### قسم 4: إدارة الملف الشخصي

| Method | Input | Output | وصف |
|---|---|---|---|
| `GetProfileAsync` | `string userId` | `ServiceResult<UserProfileDto>` | جلب بيانات الملف الشخصي |
| `UpdateProfileAsync` | `string userId, UpdateProfileDto` | `ServiceResult<UserProfileDto>` | تحديث الاسم ورقم الهاتف |

#### قسم 5: إدارة المستخدمين والأدوار (للإدارة)

| Method | Input | Output | وصف |
|---|---|---|---|
| `GetUsersAsync` | `int tenantId, int page, int pageSize` | `ServiceResult<PagedResultDto<UserManagementDto>>` | قائمة مستخدمي المستأجر |
| `GetUserByIdAsync` | `string userId` | `ServiceResult<UserManagementDto>` | تفاصيل مستخدم |
| `CreateUserAsync` | `int tenantId, UserManagementCreateDto` | `ServiceResult<UserManagementDto>` | إنشاء مستخدم بواسطة المسؤول |
| `UpdateUserStatusAsync` | `string userId, bool isActive` | `ServiceResult` | تفعيل/تعطيل حساب مستخدم |
| `DeleteUserAsync` | `string userId` | `ServiceResult` | حذف مستخدم |
| `GetRolesAsync` | `int tenantId` | `ServiceResult<IList<RoleDto>>` | قائمة الأدوار المتاحة للمستأجر |
| `AssignRoleAsync` | `AssignRoleDto` | `ServiceResult` | تعيين دور لمستخدم |
| `RemoveRoleAsync` | `string userId, string roleName` | `ServiceResult` | إزالة دور من مستخدم |

### [NEW] Implementations/AuthService.cs

**المتغيرات المُحقنة**:
```csharp
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IConfiguration _configuration;  // لقراءة JWT settings
    private readonly IMapper _mapper;
    // لا يحتاج IUnitOfWork إلا إذا استدعت Repository مباشرة
}
```

**منطق `LoginAsync`**:
1. البحث عن المستخدم بـ `UserManager.FindByEmailAsync`
2. التحقق من `IsActive == true`
3. التحقق من كلمة المرور بـ `SignInManager.CheckPasswordSignInAsync`
4. جلب الأدوار بـ `UserManager.GetRolesAsync`
5. بناء `ClaimsPrincipal` يحتوي: `UserId`, `Email`, `TenantID`, `Roles`
6. توليد **JWT Access Token** (صلاحية قصيرة: 15-60 دقيقة)
7. توليد **Refresh Token** (`Guid.NewGuid().ToString("N")`) وحفظه في `ApplicationUser.RefreshToken` + `RefreshTokenExpiry`
8. تحديث `LastLogin = DateTime.UtcNow`
9. إرجاع `LoginResponseDto`

> **ملاحظة**: يتطلب إضافة حقلين لـ `ApplicationUser`:
> - `string? RefreshToken { get; set; }`
> - `DateTime? RefreshTokenExpiry { get; set; }`

**منطق `RegisterAsync`**:
1. التحقق من عدم وجود البريد الإلكتروني مسبقاً
2. التحقق من وجود المستأجر
3. إنشاء `ApplicationUser` بـ `UserManager.CreateAsync`
4. تعيين الدور الأولي `InitialRole` إذا طُلب
5. إرجاع `UserManagementDto`

**منطق `RefreshTokenAsync`**:
1. التحقق من صحة Access Token (حتى لو منتهي الصلاحية) عبر `TokenValidationParameters` مع `ValidateLifetime = false`
2. استخراج `userId` من Claims
3. التحقق من تطابق `RefreshToken` وعدم انتهاء صلاحيته
4. توليد Access Token جديد + Refresh Token جديد
5. حفظ الـ Refresh Token الجديد في قاعدة البيانات

**منطق `ForgotPasswordAsync`**:
1. البحث عن المستخدم بـ Email
2. توليد رمز إعادة التعيين بـ `UserManager.GeneratePasswordResetTokenAsync`
3. إرجاع `ServiceResult.Success` مع رسالة نجاح (إرسال البريد يُنفَّذ في API Layer بعد استدعاء هذه الخدمة)

---

## المرحلة 1: تحضير المشروع والبنية الأساسية

### [MODIFY] ExhibitionManagementSystem.Services.csproj
إضافة مراجع:
```xml
<ItemGroup>
  <ProjectReference Include="..\ExhibitionManagementSystem.DataAccess\ExhibitionManagementSystem.DataAccess.csproj" />
  <ProjectReference Include="..\ExhibitionManagementSystem.Models.DTOs\ExhibitionManagementSystem.Models.DTOs.csproj" />
</ItemGroup>
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

### [NEW] Common/ServiceResult.cs
```csharp
public class ServiceResult<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public string? ErrorMessage { get; }
    public string? ErrorCode { get; }
    
    public static ServiceResult<T> Success(T data) => ...;
    public static ServiceResult<T> Failure(string message, string? code = null) => ...;
}
```

### [NEW] Extensions/ServiceLayerExtensions.cs
```csharp
public static IServiceCollection AddServiceLayer(this IServiceCollection services)
{
    // Auth
    services.AddScoped<IAuthService, AuthService>();
    
    // Core Business
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
```

---

## المرحلة 2: TenantService

### [NEW] Interfaces/ITenantService.cs | [NEW] Implementations/TenantService.cs

| Method | Input | Output |
|---|---|---|
| `GetAllAsync` | `int page, int pageSize` | `ServiceResult<PagedResultDto<TenantDto>>` |
| `GetByIdAsync` | `int tenantId` | `ServiceResult<TenantDto>` |
| `CreateAsync` | `TenantCreateDto` | `ServiceResult<TenantDto>` |
| `UpdateAsync` | `int tenantId, TenantUpdateDto` | `ServiceResult<TenantDto>` |
| `DeleteAsync` | `int tenantId` | `ServiceResult` |
| `GetActiveSubscriptionAsync` | `int tenantId` | `ServiceResult<TenantSubscriptionDto>` |

**منطق مهم**: التحقق من `IsSubdomainUniqueAsync` عند الإنشاء والتحديث.

---

## المرحلة 3: VenueService + HallService + BoothService

### [NEW] Interfaces/IVenueService.cs | [NEW] Implementations/VenueService.cs

| Method | Input | Output |
|---|---|---|
| `GetByTenantAsync` | `int tenantId` | `ServiceResult<IList<VenueDto>>` |
| `GetSummariesAsync` | `int tenantId` | `ServiceResult<IList<VenueSummaryDto>>` |
| `GetByIdAsync` | `int tenantId, int venueId` | `ServiceResult<VenueDto>` |
| `CreateAsync` | `int tenantId, VenueCreateDto` | `ServiceResult<VenueDto>` |
| `UpdateAsync` | `int tenantId, int venueId, VenueUpdateDto` | `ServiceResult<VenueDto>` |
| `DeleteAsync` | `int tenantId, int venueId` | `ServiceResult` |

### [NEW] Interfaces/IHallService.cs | [NEW] Implementations/HallService.cs

| Method | Input | Output |
|---|---|---|
| `GetByVenueAsync` | `int tenantId, int venueId` | `ServiceResult<IList<HallDto>>` |
| `GetByIdAsync` | `int tenantId, int hallId` | `ServiceResult<HallDto>` |
| `CreateAsync` | `int tenantId, HallCreateDto` | `ServiceResult<HallDto>` |
| `UpdateAsync` | `int tenantId, int hallId, HallUpdateDto` | `ServiceResult<HallDto>` |
| `DeleteAsync` | `int tenantId, int hallId` | `ServiceResult` |

### [NEW] Interfaces/IBoothService.cs | [NEW] Implementations/BoothService.cs

| Method | Input | Output |
|---|---|---|
| `GetByHallAsync` | `int tenantId, int hallId` | `ServiceResult<IList<BoothDto>>` |
| `GetAvailableAsync` | `int tenantId, int hallId, int exhibitionId` | `ServiceResult<IList<BoothSummaryDto>>` |
| `GetByIdAsync` | `int tenantId, int boothId` | `ServiceResult<BoothDto>` |
| `CreateAsync` | `int tenantId, BoothCreateDto` | `ServiceResult<BoothDto>` |
| `UpdateAsync` | `int tenantId, int boothId, BoothUpdateDto` | `ServiceResult<BoothDto>` |
| `MergeBoothsAsync` | `int tenantId, BoothMergeCreateDto` | `ServiceResult<BoothMergeDto>` |
| `UnmergeBoothsAsync` | `int tenantId, int mergeId` | `ServiceResult` |

**منطق `MergeBoothsAsync`**:
1. التحقق أن الأكشاك تنتمي لنفس القاعة
2. التحقق أن لا أحد منها محجوز حالياً
3. إنشاء `BoothMerge` وتحديث حالة الأكشاك إلى `Merged`
4. كل العمليات داخل Transaction واحدة

---

## المرحلة 4: ExhibitionService

### [NEW] Interfaces/IExhibitionService.cs | [NEW] Implementations/ExhibitionService.cs

| Method | Input | Output |
|---|---|---|
| `GetByTenantAsync` | `int tenantId, int page, int pageSize` | `ServiceResult<PagedResultDto<ExhibitionSummaryDto>>` |
| `GetByIdAsync` | `int tenantId, int exhibitionId` | `ServiceResult<ExhibitionDto>` |
| `GetActiveAsync` | `int tenantId` | `ServiceResult<IList<ExhibitionSummaryDto>>` |
| `GetUpcomingAsync` | `int tenantId, int count` | `ServiceResult<IList<ExhibitionSummaryDto>>` |
| `CreateAsync` | `int tenantId, ExhibitionCreateDto` | `ServiceResult<ExhibitionDto>` |
| `UpdateAsync` | `int tenantId, int id, ExhibitionUpdateDto` | `ServiceResult<ExhibitionDto>` |
| `ChangeStatusAsync` | `int tenantId, int id, string status` | `ServiceResult<ExhibitionDto>` |
| `DeleteAsync` | `int tenantId, int id` | `ServiceResult` |
| `GetSchedulesAsync` | `int tenantId, int exhibitionId` | `ServiceResult<IList<ExhibitionScheduleDto>>` |
| `AddScheduleAsync` | `int tenantId, ExhibitionScheduleCreateDto` | `ServiceResult<ExhibitionScheduleDto>` |
| `RemoveScheduleAsync` | `int tenantId, int scheduleId` | `ServiceResult` |

**منطق الحالات**:
- `Open` ← التحقق أن `StartDate >= Today`
- `Closed` ← التحقق من وجود حجوزات مؤكدة قبل الإغلاق

---

## المرحلة 5: ExhibitorService

### [NEW] Interfaces/IExhibitorService.cs | [NEW] Implementations/ExhibitorService.cs

| Method | Input | Output |
|---|---|---|
| `GetByTenantAsync` | `int tenantId, int page, int pageSize` | `ServiceResult<PagedResultDto<ExhibitorSummaryDto>>` |
| `SearchAsync` | `int tenantId, string term` | `ServiceResult<IList<ExhibitorSummaryDto>>` |
| `GetByIdAsync` | `int tenantId, int exhibitorId` | `ServiceResult<ExhibitorDto>` |
| `GetByUserIdAsync` | `int tenantId, string userId` | `ServiceResult<ExhibitorDto>` |
| `CreateAsync` | `int tenantId, ExhibitorCreateDto` | `ServiceResult<ExhibitorDto>` |
| `UpdateAsync` | `int tenantId, int id, ExhibitorUpdateDto` | `ServiceResult<ExhibitorDto>` |
| `DeleteAsync` | `int tenantId, int id` | `ServiceResult` |
| `GetReservationsAsync` | `int tenantId, int exhibitorId` | `ServiceResult<IList<BoothReservationSummaryDto>>` |

---

## المرحلة 6: PricingService — محرك التسعير

**أهم خدمة في النظام** — تعتمد عليها خدمة الحجز لحساب الأسعار.

### [NEW] Interfaces/IPricingService.cs | [NEW] Implementations/PricingService.cs

| Method | Input | Output |
|---|---|---|
| `CalculateBoothPriceAsync` | `int tenantId, int? exhibitionId, BoothType, ExhibitorCategory, decimal areaSqM` | `ServiceResult<decimal>` |
| `CalculateServicePriceAsync` | `int tenantId, int serviceId, int? exhibitionId, int quantity` | `ServiceResult<decimal>` |
| `GetBoothPriceRulesAsync` | `int tenantId, int? exhibitionId` | `ServiceResult<IList<BoothPriceRuleDto>>` |
| `CreateBoothPriceRuleAsync` | `int tenantId, BoothPriceRuleCreateDto` | `ServiceResult<BoothPriceRuleDto>` |
| `UpdateBoothPriceRuleAsync` | `int tenantId, int ruleId, BoothPriceRuleCreateDto` | `ServiceResult<BoothPriceRuleDto>` |
| `DeleteBoothPriceRuleAsync` | `int tenantId, int ruleId` | `ServiceResult` |
| `GetServicePriceRulesAsync` | `int tenantId, int? exhibitionId` | `ServiceResult<IList<ServicePriceRuleDto>>` |
| `CreateServicePriceRuleAsync` | `int tenantId, ServicePriceRuleCreateDto` | `ServiceResult<ServicePriceRuleDto>` |
| `GetPackagesAsync` | `int tenantId` | `ServiceResult<IList<PricingPackageDto>>` |
| `CreatePackageAsync` | `int tenantId, PricingPackageCreateDto` | `ServiceResult<PricingPackageDto>` |

**خوارزمية `CalculateBoothPriceAsync`**:
1. استدعاء `GetApplicableRuleAsync(tenantId, exhibitionId, boothType, category, areaSqM, DateTime.UtcNow)`
2. إذا وُجدت قاعدة → `PricePerSqM × areaSqM`
3. إذا لم توجد قاعدة → `ServiceResult.Failure("لا توجد قاعدة تسعير مناسبة", "PRICING_RULE_NOT_FOUND")`

---

## المرحلة 7: ReservationService

### [NEW] Interfaces/IReservationService.cs | [NEW] Implementations/ReservationService.cs

| Method | Input | Output |
|---|---|---|
| `GetByExhibitionAsync` | `int tenantId, int exhibitionId, int page, int pageSize` | `ServiceResult<PagedResultDto<BoothReservationSummaryDto>>` |
| `GetByExhibitorAsync` | `int tenantId, int exhibitorId` | `ServiceResult<IList<BoothReservationSummaryDto>>` |
| `GetByIdAsync` | `int tenantId, int reservationId` | `ServiceResult<BoothReservationDto>` |
| `CreateAsync` | `int tenantId, string userId, BoothReservationCreateDto` | `ServiceResult<BoothReservationDto>` |
| `UpdateAsync` | `int tenantId, int id, BoothReservationUpdateDto` | `ServiceResult<BoothReservationDto>` |
| `CancelAsync` | `int tenantId, int id` | `ServiceResult` |
| `ApproveAsync` | `int tenantId, int id` | `ServiceResult<BoothReservationDto>` |
| `AddServiceToReservationAsync` | `int tenantId, int reservationId, ReservationServiceCreateDto` | `ServiceResult<ReservationServiceDto>` |
| `RemoveServiceFromReservationAsync` | `int tenantId, int reservationId, int rsId` | `ServiceResult` |
| `GetUnpaidAsync` | `int tenantId, int exhibitionId` | `ServiceResult<IList<BoothReservationSummaryDto>>` |

**منطق `CreateAsync` (أهم Method في النظام)**:
1. التحقق من وجود العارض والمعرض
2. التحقق من حالة المعرض (يجب `Open`)
3. إذا `BoothID` موجود → `IsBoothReservedAsync` → رفض إذا محجوز
4. إذا `MergeID` موجود → `IsMergeReservedAsync` → رفض إذا محجوز
5. استدعاء `IPricingService.CalculateBoothPriceAsync` لحساب سعر الكشك
6. جلب سعر الصرف الحالي إذا كانت العملة مختلفة
7. حفظ الحجز بحالة `Pending`
8. كل العمليات داخل **Transaction**

---

## المرحلة 8: FinancialService

### [NEW] Interfaces/IFinancialService.cs | [NEW] Implementations/FinancialService.cs

| Method | Input | Output |
|---|---|---|
| `GetInvoicesByTenantAsync` | `int tenantId, int page, int pageSize` | `ServiceResult<PagedResultDto<InvoiceDto>>` |
| `GetInvoiceByIdAsync` | `int tenantId, int invoiceId` | `ServiceResult<InvoiceDto>` |
| `GetInvoiceByReservationAsync` | `int tenantId, int reservationId` | `ServiceResult<InvoiceDto>` |
| `GetOverdueInvoicesAsync` | `int tenantId` | `ServiceResult<IList<InvoiceDto>>` |
| `GenerateInvoiceForReservationAsync` | `int tenantId, int reservationId` | `ServiceResult<InvoiceDto>` |
| `CreateInvoiceAsync` | `int tenantId, InvoiceCreateDto` | `ServiceResult<InvoiceDto>` |
| `RecordPaymentAsync` | `int tenantId, PaymentCreateDto` | `ServiceResult<PaymentDto>` |
| `GetPaymentsByInvoiceAsync` | `int tenantId, int invoiceId` | `ServiceResult<IList<PaymentDto>>` |

**منطق `GenerateInvoiceForReservationAsync`**:
1. التحقق أن الحجز موجود ومؤكد (`Confirmed`)
2. التحقق أنه لا توجد فاتورة مسبقة
3. توليد رقم الفاتورة عبر `GenerateNextInvoiceNumberAsync`
4. حساب `TaxAmount = SubTotal × TaxRate / 100`
5. إنشاء الفاتورة بحالة `Issued`

**منطق `RecordPaymentAsync`**:
1. التحقق من وجود الفاتورة وعدم اكتمال الدفع
2. التحقق أن المبلغ لا يتجاوز الرصيد المتبقي
3. حفظ الدفعة
4. إذا `TotalPaid >= Invoice.TotalAmount` → تحديث الفاتورة إلى `Paid`

---

## المرحلة 9: VisitorService + TicketService

### [NEW] Interfaces/IVisitorService.cs | [NEW] Implementations/VisitorService.cs

| Method | Input | Output |
|---|---|---|
| `GetByTenantAsync` | `int tenantId, int page, int pageSize` | `ServiceResult<PagedResultDto<VisitorDto>>` |
| `SearchAsync` | `int tenantId, string term` | `ServiceResult<IList<VisitorDto>>` |
| `GetByIdAsync` | `int tenantId, int visitorId` | `ServiceResult<VisitorDto>` |
| `RegisterAsync` | `int tenantId, VisitorCreateDto` | `ServiceResult<VisitorDto>` |
| `SubmitRatingAsync` | `int tenantId, int visitorId, int exhibitionId, int rating, string? comment` | `ServiceResult<VisitorRatingDto>` |
| `GetRatingSummaryAsync` | `int tenantId, int exhibitionId` | `ServiceResult<VisitorRatingSummaryDto>` |

### [NEW] Interfaces/ITicketService.cs | [NEW] Implementations/TicketService.cs

| Method | Input | Output |
|---|---|---|
| `IssueTicketAsync` | `int tenantId, TicketCreateDto` | `ServiceResult<TicketDto>` |
| `GetByVisitorAsync` | `int tenantId, int visitorId` | `ServiceResult<IList<TicketDto>>` |
| `GetByExhibitionAsync` | `int tenantId, int exhibitionId` | `ServiceResult<IList<TicketDto>>` |
| `ScanTicketAsync` | `int tenantId, string qrCode, string direction, string? location, string scannedByUserId` | `ServiceResult<TicketScanDto>` |
| `GetScanHistoryAsync` | `int tenantId, int ticketId` | `ServiceResult<IList<TicketScanDto>>` |

**منطق `IssueTicketAsync`**:
- توليد `QRCode` فريد بناءً على `Guid.NewGuid()` + ExhibitionID + VisitorID
- التحقق من `IsQRCodeUniqueAsync`

**منطق `ScanTicketAsync`**:
1. جلب التذكرة بـ QR Code
2. التحقق من `Status == Active`
3. التحقق من `ValidDate`
4. تسجيل `TicketScan`

---

## المرحلة 10: CurrencyService

### [NEW] Interfaces/ICurrencyService.cs | [NEW] Implementations/CurrencyService.cs

| Method | Input | Output |
|---|---|---|
| `GetAllAsync` | - | `ServiceResult<IList<CurrencyDto>>` |
| `GetExchangeRatesAsync` | `string fromCurrency` | `ServiceResult<IList<ExchangeRateDto>>` |
| `GetCurrentRateAsync` | `string from, string to` | `ServiceResult<decimal>` |
| `ConvertAmountAsync` | `decimal amount, string from, string to` | `ServiceResult<decimal>` |
| `UpsertExchangeRateAsync` | `ExchangeRateDto` | `ServiceResult<ExchangeRateDto>` |

---

## المرحلة 11: ServiceManagementService

> ملاحظة: الاسم `IServiceManagementService` لتجنب التعارض مع كلمة `IServiceService`.

### [NEW] Interfaces/IServiceManagementService.cs | [NEW] Implementations/ServiceManagementService.cs

| Method | Input | Output |
|---|---|---|
| `GetByTenantAsync` | `int tenantId` | `ServiceResult<IList<ServiceDto>>` |
| `GetByIdAsync` | `int tenantId, int serviceId` | `ServiceResult<ServiceDto>` |
| `CreateAsync` | `int tenantId, ServiceCreateDto` | `ServiceResult<ServiceDto>` |
| `UpdateAsync` | `int tenantId, int serviceId, ServiceCreateDto` | `ServiceResult<ServiceDto>` |
| `DeactivateAsync` | `int tenantId, int serviceId` | `ServiceResult` |

---

## المرحلة 12: ReportService

### [NEW] Interfaces/IReportService.cs | [NEW] Implementations/ReportService.cs

| Method | Input | Output |
|---|---|---|
| `GenerateExhibitionReportAsync` | `int tenantId, int exhibitionId, string userId` | `ServiceResult<FinancialReportDto>` |
| `GetReportByIdAsync` | `int tenantId, int reportId` | `ServiceResult<FinancialReportDto>` |

**منطق `GenerateExhibitionReportAsync`**:
- `TotalRevenue` ← `GetTotalRevenueAsync(exhibitionId)`
- `TotalVisitors` ← `GetActiveTicketCountAsync(exhibitionId)`
- `OccupancyRate` = (محجوز / إجمالي أكشاك) × 100
- حفظ التقرير في `FinancialReports`

---

## المرحلة 13: AdminService

### [NEW] Interfaces/IAdminService.cs | [NEW] Implementations/AdminService.cs

| Method | Input | Output |
|---|---|---|
| `GetAuditLogsAsync` | `int tenantId, int page, int pageSize` | `ServiceResult<PagedResultDto<AuditLogDto>>` |
| `GetAuditLogsByEntityAsync` | `int tenantId, string tableName, string recordId` | `ServiceResult<IList<AuditLogDto>>` |
| `GetSubscriptionHistoryAsync` | `int tenantId` | `ServiceResult<IList<TenantSubscriptionDto>>` |
| `CreateSubscriptionAsync` | `int tenantId, TenantSubscriptionDto` | `ServiceResult<TenantSubscriptionDto>` |

---

## التغييرات على Program.cs (API الرئيسي)

```csharp
// تسجيل Identity (يجب قبل AddServiceLayer)
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options => { ... })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* JWT settings من appsettings */ });

builder.Services.AddDataAccess(connectionString);  // موجود
builder.Services.AddDtoMapping();                   // من طبقة DTOs
builder.Services.AddServiceLayer();                 // جديد ← يُضيف 16 خدمة (بما فيها Auth)
```

### تغييرات مطلوبة على ApplicationUser

يجب إضافة حقلين جديدين لدعم Refresh Tokens:

```csharp
// في ExhibitionManagementSystem.Models/ApplicationUser.cs
public string? RefreshToken { get; set; }
public DateTime? RefreshTokenExpiry { get; set; }
```

ثم إضافة Migration جديد لقاعدة البيانات:
```
dotnet ef migrations add AddRefreshTokenToUser
dotnet ef database update
```

### إعدادات JWT في appsettings.json

```json
"JwtSettings": {
  "SecretKey": "...",
  "Issuer": "ExhibitionManagementSystem",
  "Audience": "ExhibitionManagementSystemClients",
  "AccessTokenExpiryMinutes": 60,
  "RefreshTokenExpiryDays": 7
}
```

---

## خطة التحقق

```
dotnet build ExhibitionManagementSystem.Services
dotnet build  ← يبني الـ Solution كاملاً
```

---

## ملاحظات

- **AuthService يستخدم `UserManager` و `SignInManager`** المُسجَّلَيْن في DI من طبقة API — لا يحتاج AuthService إلى تسجيلهما بنفسه.
- **BoothStaff, ScheduleRegistration, Products**: موجودة في Repository ولكن لا يوجد DTOs لها — ستُضاف لاحقاً.
- **Migration مطلوب** لإضافة `RefreshToken` و `RefreshTokenExpiry` على `ApplicationUser`.
- **إجمالي الملفات**: ~34 ملف (1 csproj + 1 ServiceResult + 1 Extensions + 16 Interfaces + 15 Implementations)
