# قائمة مهام: بناء طبقة DTOs مع AutoMapper

> **الحالة**: ✅ مكتمل (تم إكمال جميع المراحل بنجاح بنسبة 100%)
> **المشروع**: `ExhibitionManagementSystem.Models.DTOs`
> **إجمالي الملفات**: ~87 ملف | **المراحل**: 12

---

## المرحلة 1: تحضير المشروع

- [x] **1.1** تحديث `ExhibitionManagementSystem.Models.DTOs.csproj`
  - إضافة `<PackageReference Include="AutoMapper" Version="14.0.0" />`
  - التأكد من وجود مرجع المشروع `ExhibitionManagementSystem.Models`
  - ضبط `<TargetFramework>net10.0</TargetFramework>`
  - ضبط `<Nullable>enable</Nullable>` و `<ImplicitUsings>enable</ImplicitUsings>`

- [x] **1.2** إنشاء هيكل المجلدات الكامل داخل `ExhibitionManagementSystem.Models.DTOs/`
  - `Common/`
  - `Auth/`
  - `Tenant/`
  - `Currency/`
  - `Venue/`
  - `Hall/`
  - `Booth/`
  - `Exhibition/`
  - `Exhibitor/`
  - `Service/`
  - `Reservation/`
  - `Financial/`
  - `Pricing/`
  - `Visitor/`
  - `Admin/`
  - `Mapping/`
  - `Mapping/Profiles/`

---

## المرحلة 2: DTOs المشتركة (Common)

- [x] **2.1** `Common/AuditDto.cs`
  - `abstract class AuditDto`
  - خصائص: `CreatedAt`, `UpdatedAt`

- [x] **2.2** `Common/PagedResultDto.cs`
  - `class PagedResultDto<T>`
  - خصائص: `Items`, `TotalCount`, `PageNumber`, `PageSize`
  - خاصية محسوبة: `TotalPages`

---

## المرحلة 3: DTOs المصادقة (Auth) — بدون AutoMapper

- [x] **3.1** `Auth/LoginRequestDto.cs`
  - خصائص: `Email` (Required+EmailAddress), `Password` (Required), `RememberMe`

- [x] **3.2** `Auth/LoginResponseDto.cs`
  - خصائص: `AccessToken`, `RefreshToken`, `ExpiresAt`, `UserId`, `FullName`, `Email`, `TenantID`, `TenantName`, `BaseCurrency`, `Roles (List<string>)`

- [x] **3.3** `Auth/RegisterRequestDto.cs`
  - خصائص: `FullName`, `Email`, `Password`, `ConfirmPassword` (Compare), `TenantID`, `InitialRole?`

- [x] **3.4** `Auth/RefreshTokenRequestDto.cs`
  - خصائص: `AccessToken` (Required), `RefreshToken` (Required)

- [x] **3.5** `Auth/RefreshTokenResponseDto.cs`
  - خصائص: `AccessToken`, `RefreshToken`, `ExpiresAt`

- [x] **3.6** `Auth/ChangePasswordDto.cs`
  - خصائص: `CurrentPassword`, `NewPassword` (MinLength 8), `ConfirmNewPassword` (Compare)

- [x] **3.7** `Auth/ResetPasswordRequestDto.cs`
  - خصائص: `Email` (Required+EmailAddress)

- [x] **3.8** `Auth/ResetPasswordConfirmDto.cs`
  - خصائص: `Email`, `Token`, `NewPassword` (MinLength 8), `ConfirmPassword` (Compare)

- [x] **3.9** `Auth/UserProfileDto.cs`
  - خصائص: `UserId`, `FullName`, `Email`, `PhoneNumber`, `TenantID`, `TenantName`, `IsActive`, `LastLogin?`, `Roles`

- [x] **3.10** `Auth/UpdateProfileDto.cs`
  - خصائص: `FullName` (Required+StringLength 100), `PhoneNumber` (StringLength 20)

- [x] **3.11** `Auth/UserManagementDto.cs`
  - خصائص: `UserId`, `FullName`, `Email`, `PhoneNumber`, `TenantID`, `IsActive`, `EmailConfirmed`, `LastLogin?`, `Roles`

- [x] **3.12** `Auth/UserManagementCreateDto.cs`
  - خصائص: `FullName`, `Email`, `Password`, `TenantID`, `Roles`, `IsActive`

- [x] **3.13** `Auth/RoleDto.cs`
  - خصائص: `RoleId`, `Name`, `TenantID`

- [x] **3.14** `Auth/AssignRoleDto.cs`
  - خصائص: `UserId` (Required), `RoleName` (Required)

---

## المرحلة 4: DTOs Lookup

### Currency
- [x] **4.1** `Currency/CurrencyDto.cs`
  - خصائص: `CurrencyCode`, `CurrencyName`, `Symbol`, `IsActive`

### Financial (ExchangeRate)
- [x] **4.2** `Financial/ExchangeRateDto.cs`
  - خصائص: `ExchangeRateID`, `FromCurrency`, `ToCurrency`, `Rate`, `ValidFrom`, `ValidTo?`, `IsActive`

### Tenant
- [x] **4.3** `Tenant/TenantDto.cs` — يرث `AuditDto`
  - خصائص: `TenantID`, `CompanyName`, `Subdomain`, `Plan`, `IsActive`, `TrialEndsAt?`, `BaseCurrency`, `CurrencySymbol`

- [x] **4.4** `Tenant/TenantCreateDto.cs`
  - خصائص: `CompanyName`, `Subdomain`, `Plan`, `BaseCurrency`, `TrialEndsAt?`

- [x] **4.5** `Tenant/TenantUpdateDto.cs`
  - خصائص: `CompanyName`, `Subdomain`, `Plan`, `BaseCurrency`, `IsActive`, `TrialEndsAt?`

---

## المرحلة 5: DTOs البنية التحتية

### Venue
- [x] **5.1** `Venue/VenueDto.cs` — يرث `AuditDto`
  - خصائص: `VenueID`, `TenantID`, `Name`, `Address`, `City`, `Country`, `TotalCapacity?`, `MapImageURL`, `IsActive`, `HallsCount`

- [x] **5.2** `Venue/VenueSummaryDto.cs`
  - خصائص: `VenueID`, `Name`, `City`, `Country`, `IsActive`, `HallsCount`

- [x] **5.3** `Venue/VenueCreateDto.cs`
  - خصائص: `TenantID`, `Name`, `Address`, `City`, `Country`, `TotalCapacity?`, `MapImageURL`

- [x] **5.4** `Venue/VenueUpdateDto.cs`
  - خصائص: `Name`, `Address`, `City`, `Country`, `TotalCapacity?`, `MapImageURL`, `IsActive`

### Hall
- [x] **5.5** `Hall/HallDto.cs` — يرث `AuditDto`
  - خصائص: `HallID`, `VenueID`, `VenueName`, `HallName`, `AreaSqM?`, `MaxBooths?`, `FloorPlanWidth?`, `FloorPlanHeight?`, `FloorPlanJSON`, `IsActive`, `BoothsCount`

- [x] **5.6** `Hall/HallSummaryDto.cs`
  - خصائص: `HallID`, `VenueID`, `HallName`, `AreaSqM?`, `IsActive`, `BoothsCount`

- [x] **5.7** `Hall/HallCreateDto.cs`
  - خصائص: `VenueID`, `HallName`, `AreaSqM?`, `MaxBooths?`, `FloorPlanWidth?`, `FloorPlanHeight?`, `FloorPlanJSON`

- [x] **5.8** `Hall/HallUpdateDto.cs`
  - خصائص: `HallName`, `AreaSqM?`, `MaxBooths?`, `FloorPlanWidth?`, `FloorPlanHeight?`, `FloorPlanJSON`, `IsActive`

### Booth
- [x] **5.9** `Booth/BoothDto.cs` — يرث `AuditDto`
  - خصائص: `BoothID`, `HallID`, `HallName`, `BoothNumber`, `OriginalAreaSqM`, `CurrentAreaSqM`, `Status`, `IsMerged`, `MergeID?`, `PosX?`, `PosY?`, `Width?`, `Height?`, `RotationAngle?`, `ShapeType?`, `ShapePolygonJSON`

- [x] **5.10** `Booth/BoothSummaryDto.cs`
  - خصائص: `BoothID`, `HallID`, `BoothNumber`, `CurrentAreaSqM`, `Status`, `IsMerged`

- [x] **5.11** `Booth/BoothCreateDto.cs`
  - خصائص: `HallID`, `BoothNumber`, `OriginalAreaSqM`, `PosX?`, `PosY?`, `Width?`, `Height?`, `RotationAngle?`, `ShapeType?`, `ShapePolygonJSON`

- [x] **5.12** `Booth/BoothUpdateDto.cs`
  - خصائص: `BoothNumber`, `Status`, `PosX?`, `PosY?`, `Width?`, `Height?`, `RotationAngle?`, `ShapeType?`, `ShapePolygonJSON`

- [x] **5.13** `Booth/BoothMergeDto.cs`
  - خصائص: `MergeID`, `HallID`, `HallName`, `MergedAreaSqM`, `MergeDate`, `Notes`, `BoothItems (List<BoothMergeItemDto>)`

- [x] **5.14** `Booth/BoothMergeCreateDto.cs`
  - خصائص: `HallID`, `BoothIDs (List<int>)`, `Notes`

- [x] **5.15** `Booth/BoothMergeItemDto.cs`
  - خصائص: `MergeItemID`, `MergeID`, `BoothID`, `BoothNumber`, `AreaSqM`

---

## المرحلة 6: DTOs الكيانات الرئيسية

### Exhibition
- [x] **6.1** `Exhibition/ExhibitionDto.cs` — يرث `AuditDto`
  - خصائص: `ExhibitionID`, `TenantID`, `VenueID`, `VenueName`, `Name`, `Type`, `Edition`, `StartDate`, `EndDate`, `Status`, `Description`, `ExpectedVisitors?`, `EntryFee?`, `EntryCurrency`, `CurrencySymbol`

- [x] **6.2** `Exhibition/ExhibitionSummaryDto.cs`
  - خصائص: `ExhibitionID`, `Name`, `Type`, `StartDate`, `EndDate`, `Status`, `VenueName`

- [x] **6.3** `Exhibition/ExhibitionCreateDto.cs`
  - خصائص: `TenantID`, `VenueID`, `Name`, `Type`, `Edition`, `StartDate`, `EndDate`, `Description`, `ExpectedVisitors?`, `EntryFee?`, `EntryCurrency`

- [x] **6.4** `Exhibition/ExhibitionUpdateDto.cs`
  - خصائص: `Name`, `Type`, `Edition`, `StartDate`, `EndDate`, `Status`, `Description`, `ExpectedVisitors?`, `EntryFee?`, `EntryCurrency`

- [x] **6.5** `Exhibition/ExhibitionScheduleDto.cs`
  - خصائص: `ScheduleID`, `ExhibitionID`, `HallID?`, `HallName`, `EventName`, `EventType?`, `StartDateTime`, `EndDateTime`, `SpeakerName`, `MaxAttendees?`, `Description`, `IsPublic`
  - إضافة: `ExhibitionScheduleCreateDto` في نفس المجلد

### Exhibitor
- [x] **6.6** `Exhibitor/ExhibitorDto.cs` — يرث `AuditDto`
  - خصائص: `ExhibitorID`, `TenantID`, `CompanyName`, `ContactPerson`, `Phone`, `Email`, `Sector`, `Nationality`, `ExhibitorCategory`, `LogoURL`, `CompanyProfile`, `IsActive`, `UserId?`

- [x] **6.7** `Exhibitor/ExhibitorSummaryDto.cs`
  - خصائص: `ExhibitorID`, `CompanyName`, `Sector`, `Nationality`, `ExhibitorCategory`, `IsActive`

- [x] **6.8** `Exhibitor/ExhibitorCreateDto.cs`
  - خصائص: `TenantID`, `CompanyName`, `ContactPerson`, `Phone`, `Email`, `Sector`, `Nationality`, `ExhibitorCategory`, `LogoURL`, `CompanyProfile`

- [x] **6.9** `Exhibitor/ExhibitorUpdateDto.cs`
  - خصائص: `CompanyName`, `ContactPerson`, `Phone`, `Email`, `Sector`, `Nationality`, `ExhibitorCategory`, `LogoURL`, `CompanyProfile`, `IsActive`

### Service
- [x] **6.10** `Service/ServiceDto.cs` — يرث `AuditDto`
  - خصائص: `ServiceID`, `TenantID`, `ServiceName`, `Category`, `Unit`, `DefaultPrice?`, `IsMandatory`, `Description`, `IsActive`

- [x] **6.11** `Service/ServiceSummaryDto.cs`
  - خصائص: `ServiceID`, `ServiceName`, `Category`, `Unit`, `DefaultPrice?`, `IsMandatory`, `IsActive`

- [x] **6.12** `Service/ServiceCreateDto.cs`
  - خصائص: `TenantID`, `ServiceName`, `Category`, `Unit`, `DefaultPrice?`, `IsMandatory`, `Description`

---

## المرحلة 7: DTOs المعاملات (Reservation)

- [x] **7.1** `Reservation/BoothReservationDto.cs` — يرث `AuditDto`
  - خصائص: `ReservationID`, `ExhibitorID`, `ExhibitorName`, `BoothID?`, `BoothNumber`, `ExhibitionID`, `ExhibitionName`, `MergeID?`, `BoothTypeSelected`, `RequestedAreaSqM`, `AllocatedAreaSqM`, `ExhibitorCategory`, `BoothAmount`, `ServicesAmount`, `TotalAmount`, `CurrencyCode`, `CurrencySymbol`, `ExchangeRateUsed`, `AmountInBaseCurrency`, `Status`, `ReservationDate`, `LogisticNotes`, `CreatedByUserId`, `Services (List<ReservationServiceDto>)`

- [x] **7.2** `Reservation/BoothReservationSummaryDto.cs`
  - خصائص: `ReservationID`, `ExhibitorName`, `BoothNumber`, `ExhibitionName`, `TotalAmount`, `CurrencyCode`, `Status`, `ReservationDate`

- [x] **7.3** `Reservation/BoothReservationCreateDto.cs`
  - خصائص: `ExhibitorID`, `BoothID?`, `ExhibitionID`, `MergeID?`, `BoothTypeSelected`, `RequestedAreaSqM`, `ExhibitorCategory`, `CurrencyCode`, `LogisticNotes`

- [x] **7.4** `Reservation/BoothReservationUpdateDto.cs`
  - خصائص: `Status`, `AllocatedAreaSqM`, `BoothAmount`, `ServicesAmount`, `TotalAmount`, `LogisticNotes`, `BoothID?`, `MergeID?`

- [x] **7.5** `Reservation/ReservationServiceDto.cs`
  - خصائص: `ReservationServiceID`, `ReservationID`, `ServiceID`, `ServiceName`, `Quantity`, `UnitPrice`, `CurrencyCode`, `TotalPrice`

- [x] **7.6** `Reservation/ReservationServiceCreateDto.cs`
  - خصائص: `ServiceID`, `Quantity`, `UnitPrice`, `CurrencyCode`

---

## المرحلة 8: DTOs المالية والتسعير

### Financial
- [x] **8.1** `Financial/InvoiceDto.cs` — يرث `AuditDto`
  - خصائص: `InvoiceID`, `TenantID`, `ReservationID`, `ExhibitorName`, `InvoiceNumber`, `InvoiceDate`, `SubTotal`, `TaxRate`, `TaxAmount`, `TotalAmount`, `CurrencyCode`, `CurrencySymbol`, `Status`, `DueDate?`, `Notes`, `Payments (List<PaymentDto>)`

- [x] **8.2** `Financial/InvoiceCreateDto.cs`
  - خصائص: `TenantID`, `ReservationID`, `InvoiceNumber`, `SubTotal`, `TaxRate`, `TotalAmount`, `CurrencyCode`, `DueDate?`, `Notes`

- [x] **8.3** `Financial/PaymentDto.cs` — يرث `AuditDto`
  - خصائص: `PaymentID`, `InvoiceID`, `InvoiceNumber`, `PaymentDate`, `Amount`, `CurrencyCode`, `CurrencySymbol`, `Method`, `ReferenceNo`, `Status`, `Notes`, `ReceivedByUserId`, `ReceivedByName`

- [x] **8.4** `Financial/PaymentCreateDto.cs`
  - خصائص: `InvoiceID`, `Amount`, `CurrencyCode`, `Method`, `ReferenceNo`, `Notes`, `ReceivedByUserId`

- [x] **8.5** `Financial/FinancialReportDto.cs`
  - خصائص: `ReportID`, `TenantID`, `ExhibitionID`, `ExhibitionName`, `TotalRevenue`, `TotalExpenses`, `NetProfit`, `TotalVisitors`, `TotalExhibitors`, `TotalBooths`, `OccupancyRate`, `CurrencyCode`, `GeneratedAt`, `GeneratedByUserId`, `ReportPeriodFrom?`, `ReportPeriodTo?`

### Pricing
- [x] **8.6** `Pricing/BoothPriceRuleDto.cs` — يرث `AuditDto`
  - خصائص: `RuleID`, `TenantID`, `ExhibitionID?`, `ExhibitionName?`, `BoothType?`, `ExhibitorCategory?`, `ProductCategory`, `PricePerSqM`, `CurrencyCode`, `MinAreaSqM?`, `MaxAreaSqM?`, `ValidFrom`, `ValidTo?`, `Notes`

- [x] **8.7** `Pricing/BoothPriceRuleCreateDto.cs`
  - خصائص: `TenantID`, `ExhibitionID?`, `BoothType?`, `ExhibitorCategory?`, `ProductCategory`, `PricePerSqM`, `CurrencyCode`, `MinAreaSqM?`, `MaxAreaSqM?`, `ValidFrom`, `ValidTo?`, `Notes`

- [x] **8.8** `Pricing/ServicePriceRuleDto.cs` — يرث `AuditDto`
  - خصائص: `RuleID`, `TenantID`, `ServiceID`, `ServiceName`, `ExhibitionID?`, `UnitPrice`, `CurrencyCode`, `ValidFrom`, `ValidTo?`, `Notes`

- [x] **8.9** `Pricing/ServicePriceRuleCreateDto.cs`
  - خصائص: `TenantID`, `ServiceID`, `ExhibitionID?`, `UnitPrice`, `CurrencyCode`, `ValidFrom`, `ValidTo?`, `Notes`

- [x] **8.10** `Pricing/PricingPackageDto.cs` — يرث `AuditDto`
  - خصائص: `PackageID`, `TenantID`, `PackageName`, `Description`, `TotalPrice`, `CurrencyCode`, `CurrencySymbol`, `ValidFrom`, `ValidTo?`, `IsActive`, `Services (List<PackageServiceItemDto>)`

- [x] **8.11** `Pricing/PricingPackageCreateDto.cs`
  - خصائص: `TenantID`, `PackageName`, `Description`, `TotalPrice`, `CurrencyCode`, `ValidFrom`, `ValidTo?`, `ServiceIDs (List<int>)`

- [x] **8.12** `Pricing/PackageServiceItemDto.cs`
  - خصائص: `PackageServiceID`, `PackageID`, `ServiceID`, `ServiceName`, `Quantity`, `UnitPrice`

---

## المرحلة 9: DTOs الزوار (Visitor)

- [x] **9.1** `Visitor/VisitorDto.cs` — يرث `AuditDto`
  - خصائص: `VisitorID`, `TenantID`, `FullName`, `Phone`, `Email`, `Nationality`, `VisitorType`, `RegisteredAt`, `UserId?`, `TicketsCount`

- [x] **9.2** `Visitor/VisitorCreateDto.cs`
  - خصائص: `TenantID`, `FullName`, `Phone`, `Email`, `Nationality`, `VisitorType`

- [x] **9.3** `Visitor/TicketDto.cs` — يرث `AuditDto`
  - خصائص: `TicketID`, `VisitorID`, `VisitorName`, `ExhibitionID`, `ExhibitionName`, `TicketType`, `Price`, `CurrencyCode`, `CurrencySymbol`, `QRCode`, `ValidDate?`, `Status`, `IssuedAt`, `ScansCount`

- [x] **9.4** `Visitor/TicketCreateDto.cs`
  - خصائص: `VisitorID`, `ExhibitionID`, `TicketType`, `Price`, `CurrencyCode`, `ValidDate?`

- [x] **9.5** `Visitor/TicketScanDto.cs`
  - خصائص: `ScanID`, `TicketID`, `QRCode`, `ScanTime`, `ScanDirection`, `ScanLocation`, `ScannedByUserId`

- [x] **9.6** `Visitor/VisitorRatingDto.cs`
  - خصائص: `RatingID`, `VisitorID`, `VisitorName`, `ExhibitionID`, `ExhibitionName`, `Rating`, `Comment`, `RatedAt`

- [x] **9.7** `Visitor/VisitorRatingSummaryDto.cs`
  - خصائص: `ExhibitionID`, `ExhibitionName`, `AverageRating`, `TotalRatings`, `RatingDistribution (Dictionary<int,int>)`

---

## المرحلة 10: DTOs الإدارة (Admin)

- [x] **10.1** `Admin/TenantSubscriptionDto.cs`
  - خصائص: `SubscriptionID`, `TenantID`, `TenantName`, `PlanName`, `StartDate`, `EndDate?`, `Status`, `Price`, `CurrencyCode`

- [x] **10.2** `Admin/AuditLogDto.cs`
  - خصائص: `LogID`, `TenantID`, `UserId`, `UserName`, `TableName`, `RecordID`, `Action`, `OldValues`, `NewValues`, `ActionAt`, `IPAddress`

- [x] **10.3** `Admin/ApplicationUserDto.cs`
  - خصائص: `UserId`, `FullName`, `Email`, `PhoneNumber`, `TenantID`, `TenantName`, `IsActive`, `LastLogin?`, `EmailConfirmed`, `Roles (List<string>)`

---

## المرحلة 11: طبقة AutoMapper (Mapping)

### ملف التجميع الرئيسي
- [x] **11.1** `Mapping/MappingProfile.cs`
  - `public class MappingProfile : Profile { }` — نقطة اكتشاف الـ Assembly

### ملف التسجيل في DI
- [x] **11.2** `Mapping/AutoMapperExtensions.cs`
  - `AddDtoMapping(IServiceCollection services)` extension method
  - `services.AddAutoMapper(typeof(MappingProfile).Assembly)`

### Profiles الفردية (12 Profile)
- [x] **11.3** `Mapping/Profiles/TenantMappingProfile.cs`
  - `Tenant → TenantDto` (مع تسطيح `Currency.Symbol`)
  - `TenantCreateDto → Tenant`
  - `TenantUpdateDto → Tenant`

- [x] **11.4** `Mapping/Profiles/CurrencyMappingProfile.cs`
  - `Currency → CurrencyDto`
  - `ExchangeRate → ExchangeRateDto`

- [x] **11.5** `Mapping/Profiles/VenueMappingProfile.cs`
  - `Venue → VenueDto` (مع تسطيح `Halls.Count`)
  - `Venue → VenueSummaryDto`
  - `VenueCreateDto → Venue` (Ignore: VenueID, CreatedAt, UpdatedAt, IsDeleted)
  - `VenueUpdateDto → Venue`

- [x] **11.6** `Mapping/Profiles/HallMappingProfile.cs`
  - `Hall → HallDto` (مع تسطيح `Venue.Name`, `Booths.Count`)
  - `Hall → HallSummaryDto`
  - `HallCreateDto → Hall`
  - `HallUpdateDto → Hall`

- [x] **11.7** `Mapping/Profiles/BoothMappingProfile.cs`
  - `Booth → BoothDto` (مع تسطيح `Hall.HallName`, `Status.ToString()`)
  - `Booth → BoothSummaryDto`
  - `BoothCreateDto → Booth`
  - `BoothUpdateDto → Booth`
  - `BoothMerge → BoothMergeDto`
  - `BoothMergeItem → BoothMergeItemDto`

- [x] **11.8** `Mapping/Profiles/ExhibitionMappingProfile.cs`
  - `Exhibition → ExhibitionDto` (تسطيح `Venue.Name`, `Currency.Symbol`, `Status.ToString()`)
  - `Exhibition → ExhibitionSummaryDto`
  - `ExhibitionCreateDto → Exhibition`
  - `ExhibitionUpdateDto → Exhibition`
  - `ExhibitionSchedule → ExhibitionScheduleDto`
  - `ExhibitionScheduleCreateDto → ExhibitionSchedule`

- [x] **11.9** `Mapping/Profiles/ExhibitorMappingProfile.cs`
  - `Exhibitor → ExhibitorDto` (`ExhibitorCategory.ToString()`)
  - `Exhibitor → ExhibitorSummaryDto`
  - `ExhibitorCreateDto → Exhibitor`
  - `ExhibitorUpdateDto → Exhibitor`

- [x] **11.10** `Mapping/Profiles/ReservationMappingProfile.cs`
  - `BoothReservation → BoothReservationDto` (تسطيح: `Exhibitor.CompanyName`, `Booth.BoothNumber`, `Exhibition.Name`, `Currency.Symbol`)
  - `BoothReservation → BoothReservationSummaryDto`
  - `BoothReservationCreateDto → BoothReservation`
  - `BoothReservationUpdateDto → BoothReservation`
  - `ReservationService → ReservationServiceDto` (تسطيح `Service.ServiceName`)
  - `ReservationServiceCreateDto → ReservationService`

- [x] **11.11** `Mapping/Profiles/FinancialMappingProfile.cs`
  - `Invoice → InvoiceDto` (تسطيح `Reservation.Exhibitor.CompanyName`, `Currency.Symbol`, `Status.ToString()`)
  - `InvoiceCreateDto → Invoice`
  - `Payment → PaymentDto` (تسطيح `Invoice.InvoiceNumber`, `Currency.Symbol`, `Method.ToString()`)
  - `PaymentCreateDto → Payment`
  - `FinancialReport → FinancialReportDto` (تسطيح `Exhibition.Name`)

- [x] **11.12** `Mapping/Profiles/PricingMappingProfile.cs`
  - `BoothPriceRule → BoothPriceRuleDto` (`BoothType?.ToString()`, `ExhibitorCategory?.ToString()`)
  - `BoothPriceRuleCreateDto → BoothPriceRule`
  - `ServicePriceRule → ServicePriceRuleDto` (تسطيح `Service.ServiceName`)
  - `ServicePriceRuleCreateDto → ServicePriceRule`
  - `PricingPackage → PricingPackageDto` (تسطيح `Currency.Symbol`, `PackageServices`)
  - `PricingPackageCreateDto → PricingPackage`
  - `PackageService → PackageServiceItemDto` (تسطيح `Service.ServiceName`)

- [x] **11.13** `Mapping/Profiles/ServiceMappingProfile.cs`
  - `Service → ServiceDto`
  - `Service → ServiceSummaryDto`
  - `ServiceCreateDto → Service`

- [x] **11.14** `Mapping/Profiles/VisitorMappingProfile.cs`
  - `Visitor → VisitorDto` (تسطيح `Tickets.Count`)
  - `VisitorCreateDto → Visitor`
  - `Ticket → TicketDto` (تسطيح `Visitor.FullName`, `Exhibition.Name`, `Currency.Symbol`, `Status.ToString()`, `TicketScans.Count`)
  - `TicketCreateDto → Ticket`
  - `TicketScan → TicketScanDto` (`ScanDirection.ToString()`)
  - `VisitorRating → VisitorRatingDto` (تسطيح `Visitor.FullName`, `Exhibition.Name`)

- [x] **11.15** `Mapping/Profiles/AdminMappingProfile.cs`
  - `TenantSubscription → TenantSubscriptionDto` (تسطيح `Tenant.CompanyName`, `Status.ToString()`)
  - `AuditLog → AuditLogDto` (تسطيح `User.FullName`)
  - `ApplicationUser → ApplicationUserDto` (تسطيح `Tenant.CompanyName`)

---

## المرحلة 12: التحقق والاختبار

- [x] **12.1** بناء المشروع
  ```
  dotnet build ExhibitionManagementSystem.Models.DTOs
  ```
  التأكد من: لا أخطاء، لا تحذيرات تعيين

- [x] **12.2** التحقق من صحة تكوين AutoMapper
  ```csharp
  var config = new MapperConfiguration(cfg =>
      cfg.AddMaps(typeof(MappingProfile).Assembly));
  config.AssertConfigurationIsValid();
  ```

- [x] **12.3** مراجعة نهائية للتأكد من:
  - جميع Enum تُحوَّل إلى `string`
  - جميع حقول Navigation Properties غير المطلوبة مُتجاهَلة بـ `Ignore()`
  - جميع CreateDtos تُهمل: `XxxID`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, `DeletedAt`, `DeletedByUserId`
  - Auth DTOs لا تحتوي على Mapping Profile

---

## 📊 ملخص الإحصائيات

| المرحلة | عدد المهام | عدد الملفات |
|---------|-----------|------------|
| 1 - تحضير | 2 | 1 |
| 2 - Common | 2 | 2 |
| 3 - Auth | 14 | 14 |
| 4 - Lookup | 5 | 5 |
| 5 - البنية التحتية | 15 | 15 |
| 6 - كيانات رئيسية | 12 | 13 |
| 7 - Reservation | 6 | 6 |
| 8 - المالية والتسعير | 12 | 12 |
| 9 - الزوار | 7 | 7 |
| 10 - الإدارة | 3 | 3 |
| 11 - Mapping | 15 | 13 |
| 12 - تحقق | 3 | — |
| **المجموع** | **96** | **~91 ملف** |
