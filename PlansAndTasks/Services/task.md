# ملف المهام: بناء طبقة الخدمات (Service Layer)

## الحالة العامة: 🟢 مكتملة (تم إنجاز جميع المراحل من 0 إلى 18)

---

## المرحلة 0: تحضير المشروع والبنية الأساسية

### 0.1 تعديل ملف المشروع
- [x] **[MODIFY]** تعديل `ExhibitionManagementSystem.Services.csproj`:
  - [x] إضافة `ProjectReference` لـ `ExhibitionManagementSystem.DataAccess`
  - [x] إضافة `ProjectReference` لـ `ExhibitionManagementSystem.Models.DTOs`
  - [x] إضافة `<FrameworkReference Include="Microsoft.AspNetCore.App" />`

### 0.2 تعديل نموذج ApplicationUser
- [x] **[MODIFY]** تعديل `ExhibitionManagementSystem.Models/ApplicationUser.cs`:
  - [x] إضافة حقل `public string? RefreshToken { get; set; }`
  - [x] إضافة حقل `public DateTime? RefreshTokenExpiry { get; set; }`

### 0.3 إنشاء Migration لقاعدة البيانات
- [x] تشغيل أمر `dotnet ef migrations add AddRefreshTokenToUser` في مشروع DataAccess
- [x] تشغيل `dotnet ef database update`

### 0.4 إنشاء البنية الأساسية للمشروع
- [x] **[NEW]** إنشاء مجلد `Common/`
- [x] **[NEW]** إنشاء مجلد `Interfaces/`
- [x] **[NEW]** إنشاء مجلد `Implementations/`
- [x] **[NEW]** إنشاء مجلد `Extensions/`

### 0.5 إنشاء ServiceResult
- [x] **[NEW]** إنشاء `Common/ServiceResult.cs`:
  - [x] تعريف الخاصيات: `IsSuccess`, `Data`, `ErrorMessage`, `ErrorCode`
  - [x] إضافة method ثابتة `Success(T data)`
  - [x] إضافة method ثابتة `Failure(string message, string? code = null)`
  - [x] إنشاء النسخة غير العامة `ServiceResult` للعمليات بدون إرجاع بيانات

### 0.6 إعداد ملف appsettings
- [x] **[MODIFY]** إضافة قسم `JwtSettings` في `appsettings.json` للمشروع الرئيسي:
  - [x] `SecretKey`
  - [x] `Issuer`
  - [x] `Audience`
  - [x] `AccessTokenExpiryMinutes`
  - [x] `RefreshTokenExpiryDays`

---

## المرحلة 1: AuthService — خدمة المصادقة

### 1.1 الواجهة
- [x] **[NEW]** إنشاء `Interfaces/IAuthService.cs`:
  - [x] `LoginAsync(LoginRequestDto dto) → ServiceResult<LoginResponseDto>`
  - [x] `RegisterAsync(RegisterRequestDto dto) → ServiceResult<UserManagementDto>`
  - [x] `LogoutAsync(string userId) → ServiceResult`
  - [x] `RefreshTokenAsync(RefreshTokenRequestDto dto) → ServiceResult<RefreshTokenResponseDto>`
  - [x] `RevokeTokenAsync(string userId) → ServiceResult`
  - [x] `ChangePasswordAsync(string userId, ChangePasswordDto dto) → ServiceResult`
  - [x] `ForgotPasswordAsync(ResetPasswordRequestDto dto) → ServiceResult`
  - [x] `ResetPasswordAsync(ResetPasswordConfirmDto dto) → ServiceResult`
  - [x] `GetProfileAsync(string userId) → ServiceResult<UserProfileDto>`
  - [x] `UpdateProfileAsync(string userId, UpdateProfileDto dto) → ServiceResult<UserProfileDto>`
  - [x] `GetUsersAsync(int tenantId, int page, int pageSize) → ServiceResult<PagedResultDto<UserManagementDto>>`
  - [x] `GetUserByIdAsync(string userId) → ServiceResult<UserManagementDto>`
  - [x] `CreateUserAsync(int tenantId, UserManagementCreateDto dto) → ServiceResult<UserManagementDto>`
  - [x] `UpdateUserStatusAsync(string userId, bool isActive) → ServiceResult`
  - [x] `DeleteUserAsync(string userId) → ServiceResult`
  - [x] `GetRolesAsync(int tenantId) → ServiceResult<IList<RoleDto>>`
  - [x] `AssignRoleAsync(AssignRoleDto dto) → ServiceResult`
  - [x] `RemoveRoleAsync(string userId, string roleName) → ServiceResult`

### 1.2 التطبيق
- [x] **[NEW]** إنشاء `Implementations/AuthService.cs`:
  - [x] حقن `UserManager<ApplicationUser>`, `SignInManager<ApplicationUser>`, `RoleManager<ApplicationRole>`
  - [x] حقن `IConfiguration` لقراءة JWT settings
  - [x] حقن `IMapper` و `IUnitOfWork`
  - [x] تطبيق `LoginAsync`:
    - [x] البحث بالبريد الإلكتروني
    - [x] التحقق من `IsActive`
    - [x] التحقق من كلمة المرور
    - [x] توليد JWT Access Token بالـ Claims (UserId, Email, TenantID, Roles)
    - [x] توليد Refresh Token وحفظه في DB
    - [x] تحديث `LastLogin`
    - [x] إرجاع `LoginResponseDto` مكتملاً (AccessToken, RefreshToken, ExpiresAt, UserId, FullName, TenantID, Roles)
  - [x] تطبيق `RegisterAsync`:
    - [x] التحقق من عدم تكرار البريد
    - [x] التحقق من وجود Tenant
    - [x] إنشاء `ApplicationUser` بـ `UserManager.CreateAsync`
    - [x] تعيين `InitialRole` إن وُجد
  - [x] تطبيق `LogoutAsync`: إبطال RefreshToken
  - [x] تطبيق `RefreshTokenAsync`:
    - [x] التحقق من صحة Access Token (مع `ValidateLifetime = false`)
    - [x] التحقق من تطابق Refresh Token وصلاحيته
    - [x] توليد Tokens جديدة وحفظها
  - [x] تطبيق `RevokeTokenAsync`: حذف RefreshToken من DB
  - [x] تطبيق `ChangePasswordAsync`: `UserManager.ChangePasswordAsync`
  - [x] تطبيق `ForgotPasswordAsync`: `GeneratePasswordResetTokenAsync` ← إرجاع Token للـ API
  - [x] تطبيق `ResetPasswordAsync`: `UserManager.ResetPasswordAsync`
  - [x] تطبيق `GetProfileAsync` + `UpdateProfileAsync`
  - [x] تطبيق `GetUsersAsync`: استعلام Identity مع فلتر `TenantID`
  - [x] تطبيق `GetUserByIdAsync`
  - [x] تطبيق `CreateUserAsync`
  - [x] تطبيق `UpdateUserStatusAsync`: تعديل `IsActive`
  - [x] تطبيق `DeleteUserAsync`: `UserManager.DeleteAsync`
  - [x] تطبيق `GetRolesAsync`: `RoleManager` مع فلتر `TenantID`
  - [x] تطبيق `AssignRoleAsync`: `UserManager.AddToRoleAsync`
  - [x] تطبيق `RemoveRoleAsync`: `UserManager.RemoveFromRoleAsync`
  - [x] إضافة private method `GenerateJwtToken(ApplicationUser user, IList<string> roles) → string`
  - [x] إضافة private method `GenerateRefreshToken() → string`

---

## المرحلة 2: TenantService

### 2.1 الواجهة
- [x] **[NEW]** إنشاء `Interfaces/ITenantService.cs`:
  - [x] `GetAllAsync(int page, int pageSize) → ServiceResult<PagedResultDto<TenantDto>>`
  - [x] `GetByIdAsync(int tenantId) → ServiceResult<TenantDto>`
  - [x] `CreateAsync(TenantCreateDto dto) → ServiceResult<TenantDto>`
  - [x] `UpdateAsync(int tenantId, TenantUpdateDto dto) → ServiceResult<TenantDto>`
  - [x] `DeleteAsync(int tenantId) → ServiceResult`
  - [x] `GetActiveSubscriptionAsync(int tenantId) → ServiceResult<TenantSubscriptionDto>`

### 2.2 التطبيق
- [x] **[NEW]** إنشاء `Implementations/TenantService.cs`:
  - [x] حقن `IUnitOfWork` و `IMapper`
  - [x] تطبيق `GetAllAsync`: استخدام `GetPagedAsync`
  - [x] تطبيق `GetByIdAsync`: إرجاع `NOT_FOUND` إذا لم يوجد
  - [x] تطبيق `CreateAsync`:
    - [x] التحقق من تفرد الـ Subdomain بـ `IsSubdomainUniqueAsync`
    - [x] Map DTO → Entity → حفظ → Map Entity → DTO
  - [x] تطبيق `UpdateAsync`:
    - [x] التحقق من وجود المستأجر
    - [x] التحقق من تفرد الـ Subdomain (استثناء المستأجر الحالي)
    - [x] تحديث وحفظ
  - [x] تطبيق `DeleteAsync`: `SoftDeleteAsync`
  - [x] تطبيق `GetActiveSubscriptionAsync`: `GetWithActiveSubscriptionAsync`

---

## المرحلة 3: VenueService

### 3.1 الواجهة
- [x] **[NEW]** إنشاء `Interfaces/IVenueService.cs`:
  - [x] `GetByTenantAsync(int tenantId) → ServiceResult<IList<VenueDto>>`
  - [x] `GetSummariesAsync(int tenantId) → ServiceResult<IList<VenueSummaryDto>>`
  - [x] `GetByIdAsync(int tenantId, int venueId) → ServiceResult<VenueDto>`
  - [x] `CreateAsync(int tenantId, VenueCreateDto dto) → ServiceResult<VenueDto>`
  - [x] `UpdateAsync(int tenantId, int venueId, VenueUpdateDto dto) → ServiceResult<VenueDto>`
  - [x] `DeleteAsync(int tenantId, int venueId) → ServiceResult`

### 3.2 التطبيق
- [x] **[NEW]** إنشاء `Implementations/VenueService.cs`:
  - [x] حقن `IUnitOfWork` و `IMapper`
  - [x] تطبيق جميع الـ methods مع التحقق من انتماء الـ Venue للمستأجر الصحيح
  - [x] `DeleteAsync`: `SoftDeleteAsync`

---

## المرحلة 4: HallService

### 4.1 الواجهة
- [x] **[NEW]** إنشاء `Interfaces/IHallService.cs`:
  - [x] `GetByVenueAsync(int tenantId, int venueId) → ServiceResult<IList<HallDto>>`
  - [x] `GetByIdAsync(int tenantId, int hallId) → ServiceResult<HallDto>`
  - [x] `CreateAsync(int tenantId, HallCreateDto dto) → ServiceResult<HallDto>`
  - [x] `UpdateAsync(int tenantId, int hallId, HallUpdateDto dto) → ServiceResult<HallDto>`
  - [x] `DeleteAsync(int tenantId, int hallId) → ServiceResult`

### 4.2 التطبيق
- [x] **[NEW]** إنشاء `Implementations/HallService.cs`:
  - [x] حقن `IUnitOfWork` و `IMapper`
  - [x] التحقق من انتماء القاعة للـ Venue الصحيح للمستأجر
  - [x] تطبيق جميع الـ methods

---

## المرحلة 5: BoothService

### 5.1 الواجهة
- [x] **[NEW]** إنشاء `Interfaces/IBoothService.cs`:
  - [x] `GetByHallAsync(int tenantId, int hallId) → ServiceResult<IList<BoothDto>>`
  - [x] `GetAvailableAsync(int tenantId, int hallId, int exhibitionId) → ServiceResult<IList<BoothSummaryDto>>`
  - [x] `GetByIdAsync(int tenantId, int boothId) → ServiceResult<BoothDto>`
  - [x] `CreateAsync(int tenantId, BoothCreateDto dto) → ServiceResult<BoothDto>`
  - [x] `UpdateAsync(int tenantId, int boothId, BoothUpdateDto dto) → ServiceResult<BoothDto>`
  - [x] `MergeBoothsAsync(int tenantId, BoothMergeCreateDto dto) → ServiceResult<BoothMergeDto>`
  - [x] `UnmergeBoothsAsync(int tenantId, int mergeId) → ServiceResult`

### 5.2 التطبيق
- [x] **[NEW]** إنشاء `Implementations/BoothService.cs`:
  - [x] حقن `IUnitOfWork` و `IMapper`
  - [x] تطبيق `GetAvailableAsync`:
    - [x] جلب جميع الأكشاك في القاعة
    - [x] استبعاد المحجوز منها بفحص `IsBoothReservedAsync`
  - [x] تطبيق `MergeBoothsAsync`:
    - [x] التحقق أن جميع الأكشاك تنتمي لنفس القاعة
    - [x] التحقق أن لا أحدها محجوز (`IsBoothReservedAsync`)
    - [x] `BeginTransactionAsync`
    - [x] إنشاء `BoothMerge` وإضافة `BoothMergeItems`
    - [x] تحديث حالة الأكشاك إلى `Merged`
    - [x] `SaveChangesAsync` ← `CommitTransactionAsync`
  - [x] تطبيق `UnmergeBoothsAsync`:
    - [x] التحقق أن الدمج غير محجوز
    - [x] `BeginTransactionAsync`
    - [x] إعادة الأكشاك إلى الحالة الأصلية
    - [x] Soft Delete للـ BoothMerge
    - [x] `CommitTransactionAsync`

---

## المرحلة 6: ExhibitionService

### 6.1 الواجهة
- [x] **[NEW]** إنشاء `Interfaces/IExhibitionService.cs`:
  - [x] `GetByTenantAsync(int tenantId, int page, int pageSize) → ServiceResult<PagedResultDto<ExhibitionSummaryDto>>`
  - [x] `GetByIdAsync(int tenantId, int exhibitionId) → ServiceResult<ExhibitionDto>`
  - [x] `GetActiveAsync(int tenantId) → ServiceResult<IList<ExhibitionSummaryDto>>`
  - [x] `GetUpcomingAsync(int tenantId, int count) → ServiceResult<IList<ExhibitionSummaryDto>>`
  - [x] `CreateAsync(int tenantId, ExhibitionCreateDto dto) → ServiceResult<ExhibitionDto>`
  - [x] `UpdateAsync(int tenantId, int id, ExhibitionUpdateDto dto) → ServiceResult<ExhibitionDto>`
  - [x] `ChangeStatusAsync(int tenantId, int id, string status) → ServiceResult<ExhibitionDto>`
  - [x] `DeleteAsync(int tenantId, int id) → ServiceResult`
  - [x] `GetSchedulesAsync(int tenantId, int exhibitionId) → ServiceResult<IList<ExhibitionScheduleDto>>`
  - [x] `AddScheduleAsync(int tenantId, ExhibitionScheduleCreateDto dto) → ServiceResult<ExhibitionScheduleDto>`
  - [x] `RemoveScheduleAsync(int tenantId, int scheduleId) → ServiceResult`

### 6.2 التطبيق
- [x] **[NEW]** إنشاء `Implementations/ExhibitionService.cs`:
  - [x] حقن `IUnitOfWork` و `IMapper`
  - [x] تطبيق `GetByTenantAsync`: `GetByTenantAsync` + `GetPagedAsync`
  - [x] تطبيق `GetActiveAsync`: `GetActiveExhibitionsAsync`
  - [x] تطبيق `GetUpcomingAsync`: `GetUpcomingExhibitionsAsync`
  - [x] تطبيق `ChangeStatusAsync`:
    - [x] تحويل النص إلى `ExhibitionStatus` Enum
    - [x] التحقق من صحة الانتقال: إذا `Open` ← `StartDate >= Today`
    - [x] حفظ الحالة الجديدة

---

## المرحلة 7: ExhibitorService

### 7.1 الواجهة
- [x] **[NEW]** إنشاء `Interfaces/IExhibitorService.cs`:
  - [x] `GetByTenantAsync(int tenantId, int page, int pageSize) → ServiceResult<PagedResultDto<ExhibitorSummaryDto>>`
  - [x] `SearchAsync(int tenantId, string term) → ServiceResult<IList<ExhibitorSummaryDto>>`
  - [x] `GetByIdAsync(int tenantId, int exhibitorId) → ServiceResult<ExhibitorDto>`
  - [x] `GetByUserIdAsync(int tenantId, string userId) → ServiceResult<ExhibitorDto>`
  - [x] `CreateAsync(int tenantId, ExhibitorCreateDto dto) → ServiceResult<ExhibitorDto>`
  - [x] `UpdateAsync(int tenantId, int id, ExhibitorUpdateDto dto) → ServiceResult<ExhibitorDto>`
  - [x] `DeleteAsync(int tenantId, int id) → ServiceResult`
  - [x] `GetReservationsAsync(int tenantId, int exhibitorId) → ServiceResult<IList<BoothReservationSummaryDto>>`

### 7.2 التطبيق
- [x] **[NEW]** إنشاء `Implementations/ExhibitorService.cs`:
  - [x] حقن `IUnitOfWork` و `IMapper`
  - [x] تطبيق `SearchAsync`: استدعاء `SearchAsync` من Repository
  - [x] تطبيق `GetReservationsAsync`: استدعاء `GetByExhibitorAsync` من `BoothReservations`
  - [x] تطبيق باقي الـ methods مع فلتر `TenantID`

---

## المرحلة 8: PricingService — محرك التسعير

### 8.1 الواجهة
- [x] **[NEW]** إنشاء `Interfaces/IPricingService.cs`:
  - [x] `CalculateBoothPriceAsync(int tenantId, int? exhibitionId, BoothType boothType, ExhibitorCategory category, decimal areaSqM) → ServiceResult<decimal>`
  - [x] `CalculateServicePriceAsync(int tenantId, int serviceId, int? exhibitionId, int quantity) → ServiceResult<decimal>`
  - [x] `GetBoothPriceRulesAsync(int tenantId, int? exhibitionId) → ServiceResult<IList<BoothPriceRuleDto>>`
  - [x] `CreateBoothPriceRuleAsync(int tenantId, BoothPriceRuleCreateDto dto) → ServiceResult<BoothPriceRuleDto>`
  - [x] `UpdateBoothPriceRuleAsync(int tenantId, int ruleId, BoothPriceRuleCreateDto dto) → ServiceResult<BoothPriceRuleDto>`
  - [x] `DeleteBoothPriceRuleAsync(int tenantId, int ruleId) → ServiceResult`
  - [x] `GetServicePriceRulesAsync(int tenantId, int? exhibitionId) → ServiceResult<IList<ServicePriceRuleDto>>`
  - [x] `CreateServicePriceRuleAsync(int tenantId, ServicePriceRuleCreateDto dto) → ServiceResult<ServicePriceRuleDto>`
  - [x] `GetPackagesAsync(int tenantId) → ServiceResult<IList<PricingPackageDto>>`
  - [x] `CreatePackageAsync(int tenantId, PricingPackageCreateDto dto) → ServiceResult<PricingPackageDto>`

### 8.2 التطبيق
- [x] **[NEW]** إنشاء `Implementations/PricingService.cs`:
  - [x] حقن `IUnitOfWork` و `IMapper`
  - [x] تطبيق `CalculateBoothPriceAsync`:
    - [x] استدعاء `BoothPriceRules.GetApplicableRuleAsync(tenantId, exhibitionId, boothType, category, areaSqM, DateTime.UtcNow)`
    - [x] إذا لم توجد قاعدة → `Failure("PRICING_RULE_NOT_FOUND")`
    - [x] الإرجاع: `rule.PricePerSqM × areaSqM`
  - [x] تطبيق `CalculateServicePriceAsync`:
    - [x] استدعاء `ServicePriceRules.GetApplicableRuleAsync(serviceId, exhibitionId, category, date)`
    - [x] إذا لم توجد قاعدة → `Failure("PRICING_RULE_NOT_FOUND")`
    - [x] الإرجاع: `rule.UnitPrice × quantity`
  - [x] تطبيق `GetBoothPriceRulesAsync`: `GetByTenantAsync` أو `GetByExhibitionAsync`
  - [x] تطبيق `CreateBoothPriceRuleAsync` + `UpdateBoothPriceRuleAsync` + `DeleteBoothPriceRuleAsync`
  - [x] تطبيق `GetServicePriceRulesAsync` + `CreateServicePriceRuleAsync`
  - [x] تطبيق `GetPackagesAsync` + `CreatePackageAsync`

---

## المرحلة 9: ReservationService

### 9.1 الواجهة
- [x] **[NEW]** إنشاء `Interfaces/IReservationService.cs`:
  - [x] `GetByExhibitionAsync(int tenantId, int exhibitionId, int page, int pageSize) → ServiceResult<PagedResultDto<BoothReservationSummaryDto>>`
  - [x] `GetByExhibitorAsync(int tenantId, int exhibitorId) → ServiceResult<IList<BoothReservationSummaryDto>>`
  - [x] `GetByIdAsync(int tenantId, int reservationId) → ServiceResult<BoothReservationDto>`
  - [x] `CreateAsync(int tenantId, string userId, BoothReservationCreateDto dto) → ServiceResult<BoothReservationDto>`
  - [x] `UpdateAsync(int tenantId, int id, BoothReservationUpdateDto dto) → ServiceResult<BoothReservationDto>`
  - [x] `CancelAsync(int tenantId, int id) → ServiceResult`
  - [x] `ApproveAsync(int tenantId, int id) → ServiceResult<BoothReservationDto>`
  - [x] `AddServiceToReservationAsync(int tenantId, int reservationId, ReservationServiceCreateDto dto) → ServiceResult<ReservationServiceDto>`
  - [x] `RemoveServiceFromReservationAsync(int tenantId, int reservationId, int rsId) → ServiceResult`
  - [x] `GetUnpaidAsync(int tenantId, int exhibitionId) → ServiceResult<IList<BoothReservationSummaryDto>>`

### 9.2 التطبيق
- [x] **[NEW]** إنشاء `Implementations/ReservationService.cs`:
  - [x] حقن `IUnitOfWork`, `IMapper`, `IPricingService`
  - [x] تطبيق `GetByExhibitionAsync`: استدعاء `GetByExhibitionAsync` + `GetPagedAsync`
  - [x] تطبيق `GetByIdAsync`: استدعاء `GetFullDetailAsync`
  - [x] تطبيق `CreateAsync` (الأهم):
    - [x] التحقق من وجود Exhibitor وانتمائه للمستأجر
    - [x] التحقق من وجود Exhibition وحالتها `Open`
    - [x] إذا `BoothID` موجود: `IsBoothReservedAsync` → رفض إذا محجوز (`BOOTH_ALREADY_RESERVED`)
    - [x] إذا `MergeID` موجود: `IsMergeReservedAsync` → رفض إذا محجوز (`MERGE_ALREADY_RESERVED`)
    - [x] استدعاء `IPricingService.CalculateBoothPriceAsync`
    - [x] جلب سعر الصرف إذا كانت العملة مختلفة عن العملة الأساسية للمستأجر
    - [x] حساب `AmountInBaseCurrency`
    - [x] `BeginTransactionAsync`
    - [x] إنشاء `BoothReservation` بحالة `Pending`
    - [x] `SaveChangesAsync` ← `CommitTransactionAsync`
  - [x] تطبيق `CancelAsync`: تغيير الحالة إلى `Cancelled`
  - [x] تطبيق `ApproveAsync`: تغيير الحالة إلى `Confirmed`
  - [x] تطبيق `AddServiceToReservationAsync`:
    - [x] استدعاء `IPricingService.CalculateServicePriceAsync`
    - [x] إضافة `ReservationService`
    - [x] تحديث `ServicesAmount` و `TotalAmount` في الحجز
  - [x] تطبيق `RemoveServiceFromReservationAsync`
  - [x] تطبيق `GetUnpaidAsync`: `GetUnpaidReservationsAsync`

---

## المرحلة 10: FinancialService

### 10.1 الواجهة
- [x] **[NEW]** إنشاء `Interfaces/IFinancialService.cs`:
  - [x] `GetInvoicesByTenantAsync(int tenantId, int page, int pageSize) → ServiceResult<PagedResultDto<InvoiceDto>>`
  - [x] `GetInvoiceByIdAsync(int tenantId, int invoiceId) → ServiceResult<InvoiceDto>`
  - [x] `GetInvoiceByReservationAsync(int tenantId, int reservationId) → ServiceResult<InvoiceDto>`
  - [x] `GetOverdueInvoicesAsync(int tenantId) → ServiceResult<IList<InvoiceDto>>`
  - [x] `GenerateInvoiceForReservationAsync(int tenantId, int reservationId) → ServiceResult<InvoiceDto>`
  - [x] `CreateInvoiceAsync(int tenantId, InvoiceCreateDto dto) → ServiceResult<InvoiceDto>`
  - [x] `RecordPaymentAsync(int tenantId, PaymentCreateDto dto) → ServiceResult<PaymentDto>`
  - [x] `GetPaymentsByInvoiceAsync(int tenantId, int invoiceId) → ServiceResult<IList<PaymentDto>>`

### 10.2 التطبيق
- [x] **[NEW]** إنشاء `Implementations/FinancialService.cs`:
  - [x] حقن `IUnitOfWork` و `IMapper`
  - [x] تطبيق `GetInvoicesByTenantAsync`: فلتر بـ `TenantID`
  - [x] تطبيق `GetOverdueInvoicesAsync`: `GetOverdueInvoicesAsync`
  - [x] تطبيق `GenerateInvoiceForReservationAsync`:
    - [x] التحقق من وجود الحجز وحالته `Confirmed`
    - [x] التحقق من عدم وجود فاتورة مسبقة (`GetByReservationAsync`)
    - [x] توليد رقم فاتورة: `GenerateNextInvoiceNumberAsync`
    - [x] حساب: `SubTotal = reservation.TotalAmount`
    - [x] حساب: `TaxAmount = SubTotal × TaxRate / 100`
    - [x] حساب: `TotalAmount = SubTotal + TaxAmount`
    - [x] إنشاء `Invoice` بحالة `Issued`
  - [x] تطبيق `RecordPaymentAsync`:
    - [x] التحقق من وجود الفاتورة وأنها غير مدفوعة
    - [x] حساب المبلغ المدفوع: `GetTotalPaidAsync`
    - [x] التحقق: `(TotalPaid + newAmount) <= Invoice.TotalAmount`
    - [x] حفظ `Payment`
    - [x] إذا `TotalPaid + newAmount >= Invoice.TotalAmount` → تحديث الفاتورة إلى `Paid`

---

## المرحلة 11: VisitorService

### 11.1 الواجهة
- [x] **[NEW]** إنشاء `Interfaces/IVisitorService.cs`:
  - [x] `GetByTenantAsync(int tenantId, int page, int pageSize) → ServiceResult<PagedResultDto<VisitorDto>>`
  - [x] `SearchAsync(int tenantId, string term) → ServiceResult<IList<VisitorDto>>`
  - [x] `GetByIdAsync(int tenantId, int visitorId) → ServiceResult<VisitorDto>`
  - [x] `RegisterAsync(int tenantId, VisitorCreateDto dto) → ServiceResult<VisitorDto>`
  - [x] `SubmitRatingAsync(int tenantId, int visitorId, int exhibitionId, int rating, string? comment) → ServiceResult<VisitorRatingDto>`
  - [x] `GetRatingSummaryAsync(int tenantId, int exhibitionId) → ServiceResult<VisitorRatingSummaryDto>`

### 11.2 التطبيق
- [x] **[NEW]** إنشاء `Implementations/VisitorService.cs`:
  - [x] حقن `IUnitOfWork` و `IMapper`
  - [x] تطبيق `RegisterAsync`:
    - [x] التحقق من عدم تكرار البريد في نفس المستأجر: `GetByEmailAsync`
  - [x] تطبيق `SubmitRatingAsync`:
    - [x] التحقق من عدم وجود تقييم سابق: `HasVisitorRatedAsync`
    - [x] التحقق من حدود التقييم (1-5)
    - [x] إنشاء `VisitorRating`
  - [x] تطبيق `GetRatingSummaryAsync`:
    - [x] جلب التقييمات بـ `GetByExhibitionAsync`
    - [x] حساب المتوسط بـ `GetAverageRatingAsync`
    - [x] إرجاع `VisitorRatingSummaryDto`

---

## المرحلة 12: TicketService

### 12.1 الواجهة
- [x] **[NEW]** إنشاء `Interfaces/ITicketService.cs`:
  - [x] `IssueTicketAsync(int tenantId, TicketCreateDto dto) → ServiceResult<TicketDto>`
  - [x] `GetByVisitorAsync(int tenantId, int visitorId) → ServiceResult<IList<TicketDto>>`
  - [x] `GetByExhibitionAsync(int tenantId, int exhibitionId) → ServiceResult<IList<TicketDto>>`
  - [x] `ScanTicketAsync(int tenantId, string qrCode, string direction, string? location, string scannedByUserId) → ServiceResult<TicketScanDto>`
  - [x] `GetScanHistoryAsync(int tenantId, int ticketId) → ServiceResult<IList<TicketScanDto>>`

### 12.2 التطبيق
- [x] **[NEW]** إنشاء `Implementations/TicketService.cs`:
  - [x] حقن `IUnitOfWork` و `IMapper`
  - [x] تطبيق `IssueTicketAsync`:
    - [x] التحقق من وجود Visitor والمعرض
    - [x] توليد QR Code: `$"{exhibitionId}-{visitorId}-{Guid.NewGuid():N}"`
    - [x] التحقق من تفرد الـ QR Code: `IsQRCodeUniqueAsync`
    - [x] إنشاء `Ticket` بحالة `Active`
  - [x] تطبيق `ScanTicketAsync`:
    - [x] جلب التذكرة: `GetByQRCodeAsync`
    - [x] التحقق: `Status == Active`
    - [x] التحقق: `ValidDate == null || ValidDate >= Today`
    - [x] إنشاء `TicketScan` مع الـ Direction والـ Location
    - [x] حفظ

---

## المرحلة 13: CurrencyService

### 13.1 الواجهة
- [x] **[NEW]** إنشاء `Interfaces/ICurrencyService.cs`:
  - [x] `GetAllAsync() → ServiceResult<IList<CurrencyDto>>`
  - [x] `GetExchangeRatesAsync(string fromCurrency) → ServiceResult<IList<ExchangeRateDto>>`
  - [x] `GetCurrentRateAsync(string from, string to) → ServiceResult<decimal>`
  - [x] `ConvertAmountAsync(decimal amount, string from, string to) → ServiceResult<decimal>`
  - [x] `UpsertExchangeRateAsync(ExchangeRateDto dto) → ServiceResult<ExchangeRateDto>`

### 13.2 التطبيق
- [x] **[NEW]** إنشاء `Implementations/CurrencyService.cs`:
  - [x] حقن `IUnitOfWork` و `IMapper`
  - [x] تطبيق `GetCurrentRateAsync`:
    - [x] إذا `from == to` → إرجاع `1.0m`
    - [x] البحث عن أحدث سعر صرف
    - [x] إذا لم يوجد → `Failure("EXCHANGE_RATE_NOT_FOUND")`
  - [x] تطبيق `ConvertAmountAsync`:
    - [x] جلب السعر بـ `GetCurrentRateAsync`
    - [x] إرجاع `amount × rate`
  - [x] تطبيق `UpsertExchangeRateAsync`:
    - [x] البحث عن سعر موجود لنفس الزوج واليوم
    - [x] إذا وُجد → تحديث، إذا لم يوجد → إنشاء

---

## المرحلة 14: ServiceManagementService

### 14.1 الواجهة
- [x] **[NEW]** إنشاء `Interfaces/IServiceManagementService.cs`:
  - [x] `GetByTenantAsync(int tenantId) → ServiceResult<IList<ServiceDto>>`
  - [x] `GetByIdAsync(int tenantId, int serviceId) → ServiceResult<ServiceDto>`
  - [x] `CreateAsync(int tenantId, ServiceCreateDto dto) → ServiceResult<ServiceDto>`
  - [x] `UpdateAsync(int tenantId, int serviceId, ServiceCreateDto dto) → ServiceResult<ServiceDto>`
  - [x] `DeactivateAsync(int tenantId, int serviceId) → ServiceResult`

### 14.2 التطبيق
- [x] **[NEW]** إنشاء `Implementations/ServiceManagementService.cs`:
  - [x] حقن `IUnitOfWork` و `IMapper`
  - [x] تطبيق `DeactivateAsync`: تعيين `IsActive = false`
  - [x] تطبيق باقي الـ methods مع فلتر `TenantID`

---

## المرحلة 15: ReportService

### 15.1 الواجهة
- [x] **[NEW]** إنشاء `Interfaces/IReportService.cs`:
  - [x] `GenerateExhibitionReportAsync(int tenantId, int exhibitionId, string userId) → ServiceResult<FinancialReportDto>`
  - [x] `GetReportByIdAsync(int tenantId, int reportId) → ServiceResult<FinancialReportDto>`

### 15.2 التطبيق
- [x] **[NEW]** إنشاء `Implementations/ReportService.cs`:
  - [x] حقن `IUnitOfWork` و `IMapper`
  - [x] تطبيق `GenerateExhibitionReportAsync`:
    - [x] التحقق من وجود المعرض وانتمائه للمستأجر
    - [x] `TotalRevenue` ← `BoothReservations.GetTotalRevenueAsync(exhibitionId)`
    - [x] `TotalVisitors` ← `Tickets.GetActiveTicketCountAsync(exhibitionId)`
    - [x] `TotalBooths` ← عدد الأكشاك الكلي في المعرض
    - [x] `ReservedBooths` ← عدد الأكشاك المحجوزة
    - [x] `OccupancyRate` = `(ReservedBooths / TotalBooths) × 100`
    - [x] إنشاء `FinancialReport` وحفظه
    - [x] إرجاع `FinancialReportDto`

---

## المرحلة 16: AdminService

### 16.1 الواجهة
- [x] **[NEW]** إنشاء `Interfaces/IAdminService.cs`:
  - [x] `GetAuditLogsAsync(int tenantId, int page, int pageSize) → ServiceResult<PagedResultDto<AuditLogDto>>`
  - [x] `GetAuditLogsByEntityAsync(int tenantId, string tableName, string recordId) → ServiceResult<IList<AuditLogDto>>`
  - [x] `GetSubscriptionHistoryAsync(int tenantId) → ServiceResult<IList<TenantSubscriptionDto>>`
  - [x] `CreateSubscriptionAsync(int tenantId, TenantSubscriptionDto dto) → ServiceResult<TenantSubscriptionDto>`

### 16.2 التطبيق
- [x] **[NEW]** إنشاء `Implementations/AdminService.cs`:
  - [x] حقن `IUnitOfWork` و `IMapper`
  - [x] تطبيق `GetAuditLogsAsync`: فلتر بـ `TenantID` + ترتيب تنازلي بالتاريخ
  - [x] تطبيق `GetAuditLogsByEntityAsync`: فلتر بـ `TenantID` + `TableName` + `RecordID`
  - [x] تطبيق `GetSubscriptionHistoryAsync`: جلب جميع اشتراكات المستأجر
  - [x] تطبيق `CreateSubscriptionAsync`: إنشاء اشتراك جديد

---

## المرحلة 17: ServiceLayerExtensions + تسجيل DI

### 17.1 إنشاء ملف الـ Extensions
- [x] **[NEW]** إنشاء `Extensions/ServiceLayerExtensions.cs`:
  - [x] تسجيل جميع الـ 16 خدمة كـ `Scoped`
  - [x] الترتيب: `IAuthService` أولاً ثم بقية الخدمات

### 17.2 تحديث Program.cs في المشروع الرئيسي
- [x] **[MODIFY]** تعديل `Program.cs` في `ExhibitionManagementSystem`:
  - [x] إضافة `AddIdentity<ApplicationUser, ApplicationRole>()` مع الإعدادات
  - [x] إضافة `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`
  - [x] إضافة `builder.Services.AddServiceLayer()`
  - [x] إضافة الـ NuGet Package: `Microsoft.AspNetCore.Authentication.JwtBearer`

---

## المرحلة 18: التحقق والبناء

### 18.1 البناء
- [x] تشغيل `dotnet build ExhibitionManagementSystem.Services`
- [x] تشغيل `dotnet build` للـ Solution الكامل
- [x] تصحيح أي أخطاء في الـ Namespaces أو المراجع

### 18.2 التحقق من DI
- [x] التأكد من تسجيل جميع الخدمات في `ServiceLayerExtensions`
- [x] التأكد من تسجيل Identity قبل `AddServiceLayer`

### 18.3 تحديث ملف المهام
- [x] تحديث هذا الملف بوضع `[x]` على المهام المنجزة بعد كل مرحلة

---

## ملخص المراحل

| # | المرحلة | الملفات | الحالة |
|---|---|---|---|
| 0 | تحضير المشروع + ServiceResult | 5 ملفات | 🟢 |
| 1 | AuthService | 2 ملف | 🟢 |
| 2 | TenantService | 2 ملف | 🟢 |
| 3 | VenueService | 2 ملف | 🟢 |
| 4 | HallService | 2 ملف | 🟢 |
| 5 | BoothService | 2 ملف | 🟢 |
| 6 | ExhibitionService | 2 ملف | 🟢 |
| 7 | ExhibitorService | 2 ملف | 🟢 |
| 8 | PricingService | 2 ملف | 🟢 |
| 9 | ReservationService | 2 ملف | 🟢 |
| 10 | FinancialService | 2 ملف | 🟢 |
| 11 | VisitorService | 2 ملف | 🟢 |
| 12 | TicketService | 2 ملف | 🟢 |
| 13 | CurrencyService | 2 ملف | 🟢 |
| 14 | ServiceManagementService | 2 ملف | 🟢 |
| 15 | ReportService | 2 ملف | 🟢 |
| 16 | AdminService | 2 ملف | 🟢 |
| 17 | DI Extensions + Program.cs | 2 ملف | 🟢 |
| 18 | التحقق والبناء | — | 🟢 |
| **المجموع** | | **~34 ملف** | **100%** |
