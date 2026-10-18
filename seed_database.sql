-- ==============================================================================
-- منظومة إدارة المعارض والمؤتمرات - Exhibition Management System
-- سكريبت ملء قاعدة البيانات بالبيانات النموذجية الشاملة (Seed Database Script)
-- ==============================================================================
-- التاريخ: 2026-10-10
-- الغرض: ملء جميع جداول طبقة النماذج ببيانات متكاملة ومترابطة لتجربة كافة وظائف النظام:
--   1. أسعار الصرف والعملات (Exchange Rates)
--   2. اشتراكات المستأجر (Tenant Subscriptions)
--   3. المواقع والقاعات (Venues & Halls)
--   4. الأجنحة ومخططات القاعات (Booths & 2D Floor Plans)
--   5. قواعد تسعير الأجنحة (Booth Price Rules)
--   6. الخدمات الإضافية وقواعد تسعيرها (Services)
--   7. باقات الخدمات المجمعة (Pricing Packages & Package Services)
--   8. المعارض: نشطة، قادمة، ومنتهية (Exhibitions)
--   9. الشركات العارضة (Exhibitors)
--  10. المنتجات المعروضة (Products)
--  11. حجوزات الأجنحة والخدمات الملحقة وكوادر الأجنحة (Reservations & Staff)
--  12. الفواتير والمدفوعات والحوالات (Invoices & Payments)
--  13. الفعاليات والجلسات الحوارية (Exhibition Schedules)
--  14. الزوار، تسجيل الفعاليات، التذاكر، وعمليات مسح البوابات (Tickets & Scans)
--  15. تقييمات الزوار واستطلاعات الرأي (Visitor Ratings & Reviews)
--  16. قوالب الشارات والبطاقات التعريفية (Badge Templates)
--  17. الرعاة، باقات الرعاية، العقود، والمساحات الإعلانية (Sponsorships & Ads)
--  18. المصروفات التشغيلية والتقرير المالي للأرباح والخسائر (Expenses & P&L)
-- ==============================================================================

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
BEGIN TRANSACTION;

BEGIN TRY

DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @RateDate DATE = CAST(@Now AS DATE);

-- -----------------------------------------------------------------------------
-- 0. استرجاع المستأجر والمستخدم المشرف الأساسي
-- -----------------------------------------------------------------------------
DECLARE @TenantID INT = (SELECT TOP 1 TenantID FROM Tenants ORDER BY TenantID);
DECLARE @AdminUserId NVARCHAR(450) = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = 'masoud');

IF (@TenantID IS NULL)
BEGIN
    RAISERROR(N'خطأ: لم يتم العثور على أي مستأجر في قاعدة البيانات. يرجى تشغيل النظام أولاً لإنشاء المستأجر الافتراضي.', 16, 1);
    RETURN;
END;

IF (@AdminUserId IS NULL)
BEGIN
    SET @AdminUserId = (SELECT TOP 1 Id FROM AspNetUsers ORDER BY Id);
END;

-- -----------------------------------------------------------------------------
-- 1. أسعار الصرف (Exchange Rates)
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM ExchangeRates WHERE FromCurrency = 'USD' AND ToCurrency = 'LYD' AND RateDate = @RateDate)
BEGIN
    INSERT INTO ExchangeRates (FromCurrency, ToCurrency, Rate, RateDate, Source, CreatedByUserId, CreatedAt)
    VALUES 
    ('USD', 'LYD', 4.850000, @RateDate, N'مصرف ليبيا المركزي', @AdminUserId, @Now),
    ('EUR', 'LYD', 5.250000, @RateDate, N'مصرف ليبيا المركزي', @AdminUserId, @Now),
    ('SAR', 'LYD', 1.293000, @RateDate, N'مصرف ليبيا المركزي', @AdminUserId, @Now),
    ('AED', 'LYD', 1.320000, @RateDate, N'مصرف ليبيا المركزي', @AdminUserId, @Now),
    ('GBP', 'LYD', 6.150000, @RateDate, N'مصرف ليبيا المركزي', @AdminUserId, @Now);
END

-- -----------------------------------------------------------------------------
-- 2. اشتراك المستأجر (Tenant Subscription)
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM TenantSubscriptions WHERE TenantID = @TenantID)
BEGIN
    INSERT INTO TenantSubscriptions (TenantID, [Plan], StartDate, EndDate, MonthlyFee, CurrencyCode, Status, CreatedAt)
    VALUES 
    (@TenantID, N'Enterprise Unlimited', DATEADD(MONTH, -3, @RateDate), DATEADD(MONTH, 9, @RateDate), 1500.00, 'LYD', 'Active', @Now);
END

-- -----------------------------------------------------------------------------
-- 3. تحديث الموقع وإضافة قاعات جديدة (Venues & Halls)
-- -----------------------------------------------------------------------------
UPDATE Venues 
SET Name = N'مركز طرابلس الدولي للمعارض والمؤتمرات',
    Address = N'شارع عمر المختار - المدينة الرياضية، طرابلس',
    City = N'طرابلس',
    Country = N'ليبيا',
    TotalCapacity = 15000,
    IsActive = 1
WHERE VenueID = 1;

IF NOT EXISTS (SELECT 1 FROM Halls WHERE VenueID = 1 AND HallName LIKE N'%الابتكار%')
BEGIN
    INSERT INTO Halls (VenueID, HallName, AreaSqM, MaxBooths, FloorPlanWidth, FloorPlanHeight, FloorPlanJSON, IsActive, CreatedAt, IsDeleted)
    VALUES (1, N'قاعة الابتكار والتقنية (Hall 2)', 2500.00, 80, 60.00, 42.00, '{}', 1, @Now, 0);
END

IF NOT EXISTS (SELECT 1 FROM Halls WHERE VenueID = 1 AND HallName LIKE N'%المؤتمرات%')
BEGIN
    INSERT INTO Halls (VenueID, HallName, AreaSqM, MaxBooths, FloorPlanWidth, FloorPlanHeight, FloorPlanJSON, IsActive, CreatedAt, IsDeleted)
    VALUES (1, N'قاعة المؤتمرات والندوات (Hall 3)', 1500.00, 40, 40.00, 35.00, '{}', 1, @Now, 0);
END

-- -----------------------------------------------------------------------------
-- 4. أجنحة القاعة 1 مع الإحداثيات (Booths)
-- -----------------------------------------------------------------------------
DECLARE @Hall1ID INT = (SELECT TOP 1 HallID FROM Halls WHERE VenueID = 1 ORDER BY HallID);

DECLARE @BoothsToInsert TABLE (
    BoothNumber NVARCHAR(20),
    Area DECIMAL(10,2),
    PosX DECIMAL(10,2),
    PosY DECIMAL(10,2),
    Width DECIMAL(10,2),
    Height DECIMAL(10,2),
    BoothType NVARCHAR(50),
    ShapeType NVARCHAR(20),
    Status NVARCHAR(20)
);

INSERT INTO @BoothsToInsert VALUES
(N'VIP-01', 36.00,  5.00,  5.00, 6.00, 6.00, 'Corner',   'Rect', 'Reserved'),
(N'VIP-02', 36.00, 15.00,  5.00, 6.00, 6.00, 'Corner',   'Rect', 'Reserved'),
(N'ISL-01', 48.00, 25.00,  5.00, 8.00, 6.00, 'Premium',  'Rect', 'Reserved'),
(N'B-01',   18.00,  5.00, 15.00, 6.00, 3.00, 'Standard', 'Rect', 'Reserved'),
(N'B-02',   18.00, 13.00, 15.00, 6.00, 3.00, 'Standard', 'Rect', 'Reserved'),
(N'B-03',   18.00, 21.00, 15.00, 6.00, 3.00, 'Standard', 'Rect', 'Available'),
(N'B-04',   18.00, 29.00, 15.00, 6.00, 3.00, 'Standard', 'Rect', 'Available'),
(N'C-01',   24.00,  5.00, 22.00, 6.00, 4.00, 'Standard', 'Rect', 'Available'),
(N'C-02',   24.00, 13.00, 22.00, 6.00, 4.00, 'Standard', 'Rect', 'Available'),
(N'C-03',   24.00, 21.00, 22.00, 6.00, 4.00, 'Standard', 'Rect', 'Available'),
(N'C-04',   24.00, 29.00, 22.00, 6.00, 4.00, 'Corner',   'Rect', 'Available'),
(N'D-01',   30.00,  5.00, 30.00, 6.00, 5.00, 'Standard', 'Rect', 'Available'),
(N'D-02',   30.00, 13.00, 30.00, 6.00, 5.00, 'Standard', 'Rect', 'Available');

INSERT INTO Booths (TenantID, HallID, BoothNumber, OriginalAreaSqM, CurrentAreaSqM, Status, IsMerged, PosX, PosY, Width, Height, RotationAngle, ShapeType, BoothType, ShapePolygonJSON, CreatedAt, IsDeleted)
SELECT 
    @TenantID, 
    @Hall1ID, 
    b.BoothNumber, 
    b.Area, 
    b.Area, 
    b.Status, 
    0, 
    b.PosX, 
    b.PosY, 
    b.Width, 
    b.Height, 
    0, 
    b.ShapeType, 
    b.BoothType, 
    '', 
    @Now, 
    0
FROM @BoothsToInsert b
WHERE NOT EXISTS (SELECT 1 FROM Booths WHERE HallID = @Hall1ID AND BoothNumber = b.BoothNumber);

-- -----------------------------------------------------------------------------
-- 5. قواعد تسعير الأجنحة (Booth Price Rules)
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM BoothPriceRules WHERE BoothType = 'Corner' AND CurrencyCode = 'LYD')
BEGIN
    INSERT INTO BoothPriceRules (TenantID, BoothType, ExhibitorCategory, RuleName, ProductCategory, PricePerSqM, CurrencyCode, ValidFrom, Notes, CreatedAt, IsDeleted)
    VALUES 
    (@TenantID, 'Corner', 'Local', N'سعر المتر للأجنحة الركنية - محلي', '', 180.00, 'LYD', @RateDate, '', @Now, 0),
    (@TenantID, 'Premium', 'Local', N'سعر المتر للأجنحة المميزة - محلي', '', 220.00, 'LYD', @RateDate, '', @Now, 0),
    (@TenantID, 'Corner', 'International', N'Corner Booth Price - International', '', 300.00, 'USD', @RateDate, '', @Now, 0),
    (@TenantID, 'Premium', 'International', N'Premium Booth Price - International', '', 350.00, 'USD', @RateDate, '', @Now, 0);
END

-- -----------------------------------------------------------------------------
-- 6. الخدمات وقواعد تسعيرها (Services)
-- -----------------------------------------------------------------------------
DECLARE @ServicesToInsert TABLE (
    ServiceName NVARCHAR(200),
    Category NVARCHAR(100),
    Unit NVARCHAR(50),
    DefaultPrice DECIMAL(18,2),
    DefaultCurrencyCode NVARCHAR(3),
    IsMandatory BIT,
    Description NVARCHAR(500)
);

INSERT INTO @ServicesToInsert VALUES
(N'تغذية كهربائية ثلاثية الطور (3-Phase 32A)', N'طاقة وكهرباء', N'وصلة', 350.00, 'LYD', 0, N'توصيل كابل كهربائي صناعي 3-فاز مع قاطع حماية'),
(N'إنترنت فايبر عالي السرعة مخصص (100Mbps)', N'اتصالات وشبكات', N'اشتراك', 250.00, 'LYD', 0, N'اتصال سلكي + واي فاي فائق السرعة مخصص للجناح'),
(N'شاشة عرض ذكية 4K قياس 65 بوصة مع حامل', N'صوتيات ومرئيات', N'جهاز/فترة', 300.00, 'LYD', 0, N'شاشة ذكية فائقة الدقة لعرض الفيديوهات الإعلانية'),
(N'طقم أثاث VIP (طاولة زجاجية + 4 كراسي جلد)', N'أثاث وديكور', N'طقم', 180.00, 'LYD', 0, N'أثاث مريح وفاخر لاستقبال كبار العملاء والزوار'),
(N'خدمة نظافة وتعقيم يومية للجناح', N'خدمات لوجستية', N'يوم', 50.00, 'LYD', 0, N'تنظيف وتلميع وإفراغ السلات صباح كل يوم قبل الافتتاح'),
(N'حارس أمن خاص للجناح (فترة ليلية)', N'أمن وسلامة', N'نوبة', 400.00, 'LYD', 0, N'حراسة مشددة لمقتنيات ومعروضات الجناح خارج أوقات العمل');

INSERT INTO Services (TenantID, ServiceName, Category, Unit, DefaultPrice, DefaultCurrencyCode, IsMandatory, Description, IsActive, CreatedAt, IsDeleted)
SELECT 
    @TenantID, s.ServiceName, s.Category, s.Unit, s.DefaultPrice, s.DefaultCurrencyCode, s.IsMandatory, s.Description, 1, @Now, 0
FROM @ServicesToInsert s
WHERE NOT EXISTS (SELECT 1 FROM Services WHERE ServiceName = s.ServiceName AND TenantID = @TenantID);

-- -----------------------------------------------------------------------------
-- 7. باقات الأسعار المجمعة (Pricing Packages & Package Services)
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM PricingPackages WHERE PackageName LIKE N'%الجناح التقني%')
BEGIN
    INSERT INTO PricingPackages (TenantID, PackageName, Description, TotalPrice, CurrencyCode, ValidFrom, ValidTo, IsActive, CreatedAt, IsDeleted)
    VALUES 
    (@TenantID, N'باقة الجناح التقني المتكامل', N'تشمل إنترنت فايبر + شاشة 4K 65 بوصة + كهرباء 3-فاز بسعر مخفض', 750.00, 'LYD', DATEADD(MONTH, -1, @RateDate), DATEADD(YEAR, 1, @RateDate), 1, @Now, 0);

    DECLARE @PkgId INT = SCOPE_IDENTITY();
    DECLARE @SvcWifi INT = (SELECT TOP 1 ServiceID FROM Services WHERE ServiceName LIKE N'%فايبر%');
    DECLARE @SvcScreen INT = (SELECT TOP 1 ServiceID FROM Services WHERE ServiceName LIKE N'%شاشة%');
    DECLARE @SvcPower INT = (SELECT TOP 1 ServiceID FROM Services WHERE ServiceName LIKE N'%ثلاثية الطور%');

    IF (@SvcWifi IS NOT NULL AND @SvcScreen IS NOT NULL AND @SvcPower IS NOT NULL)
    BEGIN
        INSERT INTO PackageServices (PackageID, ServiceID, Quantity, UnitPrice)
        VALUES 
        (@PkgId, @SvcWifi, 1.0, 220.00),
        (@PkgId, @SvcScreen, 1.0, 260.00),
        (@PkgId, @SvcPower, 1.0, 270.00);
    END
END

-- -----------------------------------------------------------------------------
-- 8. المعارض: نشطة، قادمة، ومنتهية (Exhibitions)
-- -----------------------------------------------------------------------------
UPDATE Exhibitions 
SET Name = N'معرض طرابلس الدولي للاتصالات والتقنية (TITEX 2026)',
    Type = N'تكنولوجيا واتصالات',
    Edition = N'الدورة الحادية عشرة',
    StartDate = DATEADD(DAY, -2, @RateDate),
    EndDate = DATEADD(DAY, 5, @RateDate),
    Status = 'Open',
    Description = N'أكبر تجمع إقليمي لشركات الاتصالات والتحول الرقمي والأمن السيبراني في شمال أفريقيا.',
    ExpectedVisitors = 35000,
    EntryFee = 10.00,
    EntryCurrency = 'LYD'
WHERE ExhibitionID = 1;

IF NOT EXISTS (SELECT 1 FROM Exhibitions WHERE Name LIKE N'%ليبيا الدولي للبناء%')
BEGIN
    INSERT INTO Exhibitions (TenantID, VenueID, Name, Type, Edition, StartDate, EndDate, Status, Description, ExpectedVisitors, EntryFee, EntryCurrency, CreatedAt, IsDeleted)
    VALUES 
    (@TenantID, 1, N'معرض ليبيا الدولي للبناء والتشييد (Libya Build 2026)', N'بناء وإنشاءات', N'الدورة الخامسة عشرة', DATEADD(MONTH, 1, @RateDate), DATEADD(DAY, 7, DATEADD(MONTH, 1, @RateDate)), 'Planning', N'الملتقى الأبرز لشركات المقاولات وتطوير البنية التحتية ومواد البناء الحديثة.', 25000, 15.00, 'LYD', @Now, 0);
END

IF NOT EXISTS (SELECT 1 FROM Exhibitions WHERE Name LIKE N'%الصناعات الغذائية%')
BEGIN
    INSERT INTO Exhibitions (TenantID, VenueID, Name, Type, Edition, StartDate, EndDate, Status, Description, ExpectedVisitors, EntryFee, EntryCurrency, CreatedAt, IsDeleted)
    VALUES 
    (@TenantID, 1, N'معرض طرابلس للصناعات الغذائية والتغليف 2025', N'أغذية وزراعة', N'الدورة الثامنة', DATEADD(MONTH, -5, @RateDate), DATEADD(DAY, 5, DATEADD(MONTH, -5, @RateDate)), 'Closed', N'المعرض السنوي المتخصص في الصناعات التحويلية والتعبئة والتغليف وتقنيات الأغذية.', 18000, 5.00, 'LYD', @Now, 0);
END

-- -----------------------------------------------------------------------------
-- 9. الشركات العارضة (Exhibitors)
-- -----------------------------------------------------------------------------
DECLARE @ExhibitorsToInsert TABLE (
    CompanyName NVARCHAR(200),
    ContactPerson NVARCHAR(100),
    Phone NVARCHAR(20),
    Email NVARCHAR(200),
    Sector NVARCHAR(100),
    Nationality NVARCHAR(100),
    Category NVARCHAR(20),
    PreferredCurrency NVARCHAR(3),
    CompanyProfile NVARCHAR(MAX)
);

INSERT INTO @ExhibitorsToInsert VALUES
(N'شركة ليبيانا للهاتف المحمول', N'م. علي الصيد الورفلي', '0912223344', 'contact@libyana.ly', N'اتصالات وشبكات الهاتف المحمول', N'ليبي', 'Local', 'LYD', N'المشغل الوطني الرائد لشبكات الاتصالات المتنقلة وحلول الجيل الرابع والمطور لخدمات 5G.'),
(N'شركة الصرح للحلول الرقمية والأنظمة السحابية', N'م. مراد الترهوني', '0925556677', 'sales@sarh-tech.ly', N'حلول برمجية وحوسبة سحابية', N'ليبي', 'Local', 'LYD', N'بيت خبرة رائد في تطوير أنظمة ERP والحلول السحابية المخصصة للمؤسسات الكبرى.'),
(N'مجموعة هواوي للتكنولوجيا - فرع شمال أفريقيا', N'السيد/ تشانغ وي (Zhang Wei)', '0917778899', 'north-africa@huawei.com', N'بنية تحتية وتقنيات الاتصالات', N'صيني', 'International', 'USD', N'الشركة العالمية الرائدة في حلول البنية التحتية للاتصالات والأجهزة الذكية والطاقة الرقمية.'),
(N'شركة السهم للمقاولات والتطوير الهندسي', N'م. سالم الفرجاني', '0941112233', 'info@alsahm.ly', N'إنشاءات ومقاولات عامة', N'ليبي', 'Local', 'LYD', N'شركة وطنية متخصصة في تنفيذ المشروعات الاستراتيجية والمدن الصناعية والمنشآت الذكية.'),
(N'شركة الرواد للتجارة واستيراد التجهيزات الصناعية', N'أ. خيري القمودي', '0918889900', 'khairi@alrowad.ly', N'تجارة وتوريدات صناعية', N'ليبي', 'Local', 'LYD', N'وكيل معتمد لأكبر المصانع الأوروبية والآسيوية في المعدات الثقيلة وخطوط الإنتاج.');

INSERT INTO Exhibitors (TenantID, CompanyName, ContactPerson, Phone, Email, Sector, Nationality, ExhibitorCategory, PreferredCurrencyCode, LogoURL, CompanyProfile, IsActive, CreatedAt, IsDeleted)
SELECT 
    @TenantID, e.CompanyName, e.ContactPerson, e.Phone, e.Email, e.Sector, e.Nationality, e.Category, e.PreferredCurrency, '', e.CompanyProfile, 1, @Now, 0
FROM @ExhibitorsToInsert e
WHERE NOT EXISTS (SELECT 1 FROM Exhibitors WHERE CompanyName = e.CompanyName AND TenantID = @TenantID);

UPDATE Exhibitors
SET ContactPerson = N'م. أحمد المجبري',
    Phone = '0913334455',
    Email = 'corporate@almadar.ly',
    Sector = N'اتصالات وتقنية المعلومات',
    Nationality = N'ليبي',
    CompanyProfile = N'أول مشغل للهاتف المحمول في ليبيا ومزود رئيسي لخدمات النطاق العريض والبيانات والحلول الحكومية.'
WHERE ExhibitorID = 1;

-- -----------------------------------------------------------------------------
-- 10. المنتجات المعروضة (Products)
-- -----------------------------------------------------------------------------
DECLARE @ExMadar INT = 1;
DECLARE @ExLibyana INT = (SELECT TOP 1 ExhibitorID FROM Exhibitors WHERE CompanyName LIKE N'%ليبيانا%');
DECLARE @ExSarh INT = (SELECT TOP 1 ExhibitorID FROM Exhibitors WHERE CompanyName LIKE N'%الصرح%');
DECLARE @ExHuawei INT = (SELECT TOP 1 ExhibitorID FROM Exhibitors WHERE CompanyName LIKE N'%هواوي%');

IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName LIKE N'%5G Ultra%')
BEGIN
    INSERT INTO Products (ExhibitorID, ExhibitionID, ProductName, Category, Description, ImageURL, CreatedAt, IsDeleted)
    VALUES 
    (@ExMadar, 1, N'باقة المدار 5G Ultra للشركات', N'خدمات اتصالات متقدمة', N'اتصال لاسلكي فائق السرعة عبر شبكة الجيل الخامس بسعات ضخمة ومعدل كمون منخفض للغاية.', '', @Now, 0),
    (@ExMadar, 1, N'منصة السحابة السيادية الآمنة', N'حلول سحابية', N'خدمات استضافة سحابية متوافقة مع معايير حماية البيانات الوطنية للمؤسسات المصرفية والحكومية.', '', @Now, 0);
END

IF (@ExLibyana IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Products WHERE ExhibitorID = @ExLibyana))
BEGIN
    INSERT INTO Products (ExhibitorID, ExhibitionID, ProductName, Category, Description, ImageURL, CreatedAt, IsDeleted)
    VALUES 
    (@ExLibyana, 1, N'منظومة سداد للدفع الإلكتروني والتجارة', N'حلول الدفع المالي (FinTech)', N'بوابة دفع إلكتروني فورية عبر الهاتف المحمول تمكن المتاجر والمستهلكين من إتمام المعاملات فورياً.', '', @Now, 0),
    (@ExLibyana, 1, N'حزمة إنترنت الأشياء للمدن الذكية (IoT)', N'إنترنت الأشياء', N'أجهزة استشعار وحلول ذكية للتحكم بالطاقة ومراقبة الشبكات الصناعية وإدارة الأساطيل.', '', @Now, 0);
END

IF (@ExSarh IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Products WHERE ExhibitorID = @ExSarh))
BEGIN
    INSERT INTO Products (ExhibitorID, ExhibitionID, ProductName, Category, Description, ImageURL, CreatedAt, IsDeleted)
    VALUES 
    (@ExSarh, 1, N'نظام الصرح ERP لإدارة الموارد المؤسسية', N'برمجيات مؤسسية', N'نظام متكامل لإدارة الحسابات، الموارد البشرية، المخازن، وإدارة علاقات العملاء باللغتين العربية والإنجليزية.', '', @Now, 0);
END

IF (@ExHuawei IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Products WHERE ExhibitorID = @ExHuawei))
BEGIN
    INSERT INTO Products (ExhibitorID, ExhibitionID, ProductName, Category, Description, ImageURL, CreatedAt, IsDeleted)
    VALUES 
    (@ExHuawei, 1, N'Huawei OceanStor Dorado All-Flash Storage', N'خوادم وتخزين بيانات', N'منظومات تخزين فائقة السرعة والأداء للقطاع المالي والمصرفي وشركات الاتصالات مع حماية بيانات 99.9999%.', '', @Now, 0);
END

-- -----------------------------------------------------------------------------
-- 11. حجوزات الأجنحة والخدمات الملحقة وكوادر الأجنحة (Reservations & Staff)
-- -----------------------------------------------------------------------------
DECLARE @BoothVIP1 INT = (SELECT TOP 1 BoothID FROM Booths WHERE BoothNumber = 'VIP-01');
DECLARE @BoothVIP2 INT = (SELECT TOP 1 BoothID FROM Booths WHERE BoothNumber = 'VIP-02');
DECLARE @BoothISL1 INT = (SELECT TOP 1 BoothID FROM Booths WHERE BoothNumber = 'ISL-01');
DECLARE @BoothB1   INT = (SELECT TOP 1 BoothID FROM Booths WHERE BoothNumber = 'B-01');

-- حجز شركة المدار الجديد (جناح VIP-01)
IF (@BoothVIP1 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM BoothReservations WHERE ExhibitionID = 1 AND BoothID = @BoothVIP1))
BEGIN
    INSERT INTO BoothReservations (
        TenantID, ExhibitorID, BoothID, ExhibitionID, MergeID, BoothTypeSelected, 
        RequestedAreaSqM, AllocatedAreaSqM, ExhibitorCategory, BoothAmount, ServicesAmount, TotalAmount, 
        CurrencyCode, ExchangeRateUsed, AmountInBaseCurrency, BaseCurrencyCode, 
        BoothSourceAmount, BoothSourceCurrencyCode, BoothExchangeRateUsed, 
        ExchangeRateSource, BoothExchangeRateSource,
        Status, ReservationDate, LogisticNotes, CreatedByUserId, CreatedAt, IsDeleted
    )
    VALUES (
        @TenantID, @ExMadar, @BoothVIP1, 1, NULL, 'Corner',
        36.00, 36.00, 'Local', 6480.00, 900.00, 7380.00,
        'LYD', 1.000000, 7380.00, 'LYD',
        6480.00, 'LYD', 1.000000,
        N'مصرف ليبيا المركزي', N'مصرف ليبيا المركزي',
        'Confirmed', DATEADD(DAY, -10, @Now), N'مطلوب تجهيز شاشات العرض ووصلات الألياف البصرية قبل يوم الافتتاح بـ 24 ساعة.',
        @AdminUserId, @Now, 0
    );

    DECLARE @ResMadar INT = SCOPE_IDENTITY();
    DECLARE @SvcWifiId INT = (SELECT TOP 1 ServiceID FROM Services WHERE ServiceName LIKE N'%فايبر%');
    DECLARE @SvcScreenId INT = (SELECT TOP 1 ServiceID FROM Services WHERE ServiceName LIKE N'%شاشة%');
    DECLARE @SvcPowerId INT = (SELECT TOP 1 ServiceID FROM Services WHERE ServiceName LIKE N'%ثلاثية الطور%');

    IF (@SvcWifiId IS NOT NULL)
    BEGIN
        INSERT INTO ReservationServices (ReservationID, ServiceID, Quantity, UnitPrice, CurrencyCode, TotalPrice, SourceUnitPrice, SourceTotalPrice, SourceCurrencyCode, ExchangeRateUsed, ExchangeRateSource)
        VALUES 
        (@ResMadar, @SvcWifiId, 1, 250.00, 'LYD', 250.00, 250.00, 250.00, 'LYD', 1.0, N'مصرف ليبيا المركزي'),
        (@ResMadar, @SvcScreenId, 1, 300.00, 'LYD', 300.00, 300.00, 300.00, 'LYD', 1.0, N'مصرف ليبيا المركزي'),
        (@ResMadar, @SvcPowerId, 1, 350.00, 'LYD', 350.00, 350.00, 350.00, 'LYD', 1.0, N'مصرف ليبيا المركزي');
    END

    INSERT INTO BoothStaffs (ReservationID, StaffName, Role, Phone, Email, BadgeIssued, BadgeNumber, CreatedAt, IsDeleted)
    VALUES 
    (@ResMadar, N'م. طارق فرج الزنتاني', N'مدير الجناح', '0913331122', 't.zintani@almadar.ly', 1, 'EXH-MAD-001', @Now, 0),
    (@ResMadar, N'أ. سارة مصطفى الورفلي', N'أخصائية علاقات عامة واستقبال', '0913331123', 's.werfalli@almadar.ly', 1, 'EXH-MAD-002', @Now, 0),
    (@ResMadar, N'م. هيثم نوري القرقني', N'مهندس دعم فني وشبكات', '0913331124', 'h.gargani@almadar.ly', 1, 'EXH-MAD-003', @Now, 0);
END

-- حجز شركة ليبيانا للهاتف المحمول (جناح VIP-02)
IF (@ExLibyana IS NOT NULL AND @BoothVIP2 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM BoothReservations WHERE ExhibitionID = 1 AND BoothID = @BoothVIP2))
BEGIN
    INSERT INTO BoothReservations (
        TenantID, ExhibitorID, BoothID, ExhibitionID, MergeID, BoothTypeSelected, 
        RequestedAreaSqM, AllocatedAreaSqM, ExhibitorCategory, BoothAmount, ServicesAmount, TotalAmount, 
        CurrencyCode, ExchangeRateUsed, AmountInBaseCurrency, BaseCurrencyCode, 
        BoothSourceAmount, BoothSourceCurrencyCode, BoothExchangeRateUsed, 
        ExchangeRateSource, BoothExchangeRateSource,
        Status, ReservationDate, LogisticNotes, CreatedByUserId, CreatedAt, IsDeleted
    )
    VALUES (
        @TenantID, @ExLibyana, @BoothVIP2, 1, NULL, 'Corner',
        36.00, 36.00, 'Local', 6480.00, 460.00, 6940.00,
        'LYD', 1.000000, 6940.00, 'LYD',
        6480.00, 'LYD', 1.000000,
        N'مصرف ليبيا المركزي', N'مصرف ليبيا المركزي',
        'Confirmed', DATEADD(DAY, -8, @Now), N'تجهيز الجناح لاستعراض منظومة سداد وتوزيع المواد الدعائية.',
        @AdminUserId, @Now, 0
    );

    DECLARE @ResLibyana INT = SCOPE_IDENTITY();
    DECLARE @SvcFurnitureId INT = (SELECT TOP 1 ServiceID FROM Services WHERE ServiceName LIKE N'%أثاث%');
    DECLARE @SvcCleanId INT = (SELECT TOP 1 ServiceID FROM Services WHERE ServiceName LIKE N'%نظافة%');

    IF (@SvcFurnitureId IS NOT NULL)
    BEGIN
        INSERT INTO ReservationServices (ReservationID, ServiceID, Quantity, UnitPrice, CurrencyCode, TotalPrice, SourceUnitPrice, SourceTotalPrice, SourceCurrencyCode, ExchangeRateUsed, ExchangeRateSource)
        VALUES 
        (@ResLibyana, @SvcFurnitureId, 2, 180.00, 'LYD', 360.00, 180.00, 360.00, 'LYD', 1.0, N'مصرف ليبيا المركزي'),
        (@ResLibyana, @SvcCleanId, 2, 50.00, 'LYD', 100.00, 50.00, 100.00, 'LYD', 1.0, N'مصرف ليبيا المركزي');
    END

    INSERT INTO BoothStaffs (ReservationID, StaffName, Role, Phone, Email, BadgeIssued, BadgeNumber, CreatedAt, IsDeleted)
    VALUES 
    (@ResLibyana, N'م. حسام الدين التاجوري', N'مدير الجناح', '0922224455', 'h.tajouri@libyana.ly', 1, 'EXH-LIB-001', @Now, 0),
    (@ResLibyana, N'أ. إيناس كمال المقريف', N'مسؤولة تسويق وترويج', '0922224456', 'e.megerisi@libyana.ly', 1, 'EXH-LIB-002', @Now, 0);
END

-- حجز شركة هواوي (جناح جزيرة ISL-01)
IF (@ExHuawei IS NOT NULL AND @BoothISL1 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM BoothReservations WHERE ExhibitionID = 1 AND BoothID = @BoothISL1))
BEGIN
    INSERT INTO BoothReservations (
        TenantID, ExhibitorID, BoothID, ExhibitionID, MergeID, BoothTypeSelected, 
        RequestedAreaSqM, AllocatedAreaSqM, ExhibitorCategory, BoothAmount, ServicesAmount, TotalAmount, 
        CurrencyCode, ExchangeRateUsed, AmountInBaseCurrency, BaseCurrencyCode, 
        BoothSourceAmount, BoothSourceCurrencyCode, BoothExchangeRateUsed, 
        ExchangeRateSource, BoothExchangeRateSource,
        Status, ReservationDate, LogisticNotes, CreatedByUserId, CreatedAt, IsDeleted
    )
    VALUES (
        @TenantID, @ExHuawei, @BoothISL1, 1, NULL, 'Premium',
        48.00, 48.00, 'International', 16800.00, 0.00, 16800.00,
        'USD', 4.850000, 81480.00, 'LYD',
        16800.00, 'USD', 1.000000,
        N'مصرف ليبيا المركزي', N'مصرف ليبيا المركزي',
        'Confirmed', DATEADD(DAY, -12, @Now), N'Special heavy server machinery installation.',
        @AdminUserId, @Now, 0
    );

    DECLARE @ResHuawei INT = SCOPE_IDENTITY();
    INSERT INTO BoothStaffs (ReservationID, StaffName, Role, Phone, Email, BadgeIssued, BadgeNumber, CreatedAt, IsDeleted)
    VALUES 
    (@ResHuawei, N'Mr. David Lin', N'Regional Solution Architect', '+86138001122', 'david.lin@huawei.com', 1, 'EXH-HUA-001', @Now, 0),
    (@ResHuawei, N'م. حمزة البشتي', N'مهندس دعم محلي', '0917772211', 'h.bashti@huawei.ly', 1, 'EXH-HUA-002', @Now, 0);
END

-- حجز شركة الصرح (جناح قياسي B-01)
IF (@ExSarh IS NOT NULL AND @BoothB1 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM BoothReservations WHERE ExhibitionID = 1 AND BoothID = @BoothB1))
BEGIN
    INSERT INTO BoothReservations (
        TenantID, ExhibitorID, BoothID, ExhibitionID, MergeID, BoothTypeSelected, 
        RequestedAreaSqM, AllocatedAreaSqM, ExhibitorCategory, BoothAmount, ServicesAmount, TotalAmount, 
        CurrencyCode, ExchangeRateUsed, AmountInBaseCurrency, BaseCurrencyCode, 
        BoothSourceAmount, BoothSourceCurrencyCode, BoothExchangeRateUsed, 
        ExchangeRateSource, BoothExchangeRateSource,
        Status, ReservationDate, LogisticNotes, CreatedByUserId, CreatedAt, IsDeleted
    )
    VALUES (
        @TenantID, @ExSarh, @BoothB1, 1, NULL, 'Standard',
        18.00, 18.00, 'Local', 2700.00, 0.00, 2700.00,
        'LYD', 1.000000, 2700.00, 'LYD',
        2700.00, 'LYD', 1.000000,
        N'مصرف ليبيا المركزي', N'مصرف ليبيا المركزي',
        'Confirmed', DATEADD(DAY, -5, @Now), N'عرض وتجربة حيّة لمنظومة ERP.',
        @AdminUserId, @Now, 0
    );
END

-- -----------------------------------------------------------------------------
-- 12. الفواتير والمدفوعات (Invoices & Payments)
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM InvoiceSequences WHERE TenantID = @TenantID AND [Year] = 2026)
BEGIN
    INSERT INTO InvoiceSequences (TenantID, [Year], LastValue)
    VALUES (@TenantID, 2026, 10);
END

-- فاتورة وسداد شركة المدار
DECLARE @MadarResId INT = (SELECT TOP 1 ReservationID FROM BoothReservations WHERE ExhibitorID = @ExMadar AND ExhibitionID = 1);
IF (@MadarResId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Invoices WHERE ReservationID = @MadarResId))
BEGIN
    INSERT INTO Invoices (
        TenantID, ReservationID, InvoiceNumber, InvoiceDate, SubTotal, TaxRate, TaxAmount, TotalAmount, 
        CurrencyCode, ExchangeRateUsed, TotalAmountInBaseCurrency, BaseCurrencyCode, Status, DueDate, Notes, CreatedAt, IsDeleted
    )
    VALUES (
        @TenantID, @MadarResId, 'INV-2026-0001', DATEADD(DAY, -9, @Now), 7380.00, 0.00, 0.00, 7380.00,
        'LYD', 1.000000, 7380.00, 'LYD', 'Paid', DATEADD(DAY, 15, @Now), N'فاتورة سداد حجز الجناح VIP-01 والخدمات الإضافية.', @Now, 0
    );

    DECLARE @InvMadarId INT = SCOPE_IDENTITY();

    INSERT INTO InvoiceItems (InvoiceID, ItemName, Quantity, UnitPrice, TotalPrice)
    VALUES 
    (@InvMadarId, N'حجز مساحة جناح ركني VIP-01 (36 م²)', 36.00, 180.00, 6480.00),
    (@InvMadarId, N'إنترنت فايبر عالي السرعة 100Mbps', 1.00, 250.00, 250.00),
    (@InvMadarId, N'شاشة ذكية 4K 65 بوصة مع حامل', 1.00, 300.00, 300.00),
    (@InvMadarId, N'تغذية كهربائية صناعية 3-فاز', 1.00, 350.00, 350.00);

    INSERT INTO Payments (
        TenantID, InvoiceID, PaymentDate, Amount, CurrencyCode, BaseCurrencyCode, ExchangeRateUsed, AmountInBaseCurrency, 
        Method, ReferenceNo, Status, Notes, ReceivedByUserId, CreatedAt
    )
    VALUES (
        @TenantID, @InvMadarId, DATEADD(DAY, -8, @Now), 7380.00, 'LYD', 'LYD', 1.000000, 7380.00,
        'BankTransfer', 'TXN-CBL-2026-88910', 'Completed', N'تم استلام كامل قيمة الفاتورة عبر تحويل مصرفي معتمد.',
        @AdminUserId, @Now
    );
END

-- فاتورة وسداد شركة ليبيانا
DECLARE @LibyanaResId INT = (SELECT TOP 1 ReservationID FROM BoothReservations WHERE ExhibitorID = @ExLibyana AND ExhibitionID = 1);
IF (@LibyanaResId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Invoices WHERE ReservationID = @LibyanaResId))
BEGIN
    INSERT INTO Invoices (
        TenantID, ReservationID, InvoiceNumber, InvoiceDate, SubTotal, TaxRate, TaxAmount, TotalAmount, 
        CurrencyCode, ExchangeRateUsed, TotalAmountInBaseCurrency, BaseCurrencyCode, Status, DueDate, Notes, CreatedAt, IsDeleted
    )
    VALUES (
        @TenantID, @LibyanaResId, 'INV-2026-0002', DATEADD(DAY, -7, @Now), 6940.00, 0.00, 0.00, 6940.00,
        'LYD', 1.000000, 6940.00, 'LYD', 'Paid', DATEADD(DAY, 15, @Now), N'فاتورة حجز الجناح VIP-02 والخدمات.', @Now, 0
    );

    DECLARE @InvLibyanaId INT = SCOPE_IDENTITY();

    INSERT INTO InvoiceItems (InvoiceID, ItemName, Quantity, UnitPrice, TotalPrice)
    VALUES 
    (@InvLibyanaId, N'حجز مساحة جناح ركني VIP-02 (36 م²)', 36.00, 180.00, 6480.00),
    (@InvLibyanaId, N'طقم أثاث كبار الشخصيات VIP', 2.00, 180.00, 360.00),
    (@InvLibyanaId, N'خدمة نظافة وتعقيم يومية للجناح', 2.00, 50.00, 100.00);

    INSERT INTO Payments (
        TenantID, InvoiceID, PaymentDate, Amount, CurrencyCode, BaseCurrencyCode, ExchangeRateUsed, AmountInBaseCurrency, 
        Method, ReferenceNo, Status, Notes, ReceivedByUserId, CreatedAt
    )
    VALUES (
        @TenantID, @InvLibyanaId, DATEADD(DAY, -6, @Now), 6940.00, 'LYD', 'LYD', 1.000000, 6940.00,
        'BankTransfer', 'TXN-NCB-2026-11442', 'Completed', N'سداد كلي عبر صك مصدق - مصرف الجمهورية.',
        @AdminUserId, @Now
    );
END

-- -----------------------------------------------------------------------------
-- 13. الفعاليات وورش العمل المصاحبة (Exhibition Schedules)
-- -----------------------------------------------------------------------------
DECLARE @Hall3ID INT = (SELECT TOP 1 HallID FROM Halls WHERE VenueID = 1 AND HallName LIKE N'%المؤتمرات%');
IF (@Hall3ID IS NULL) SET @Hall3ID = @Hall1ID;

IF NOT EXISTS (SELECT 1 FROM ExhibitionSchedules WHERE ExhibitionID = 1)
BEGIN
    INSERT INTO ExhibitionSchedules (ExhibitionID, HallID, EventName, EventType, StartDateTime, EndDateTime, SpeakerName, MaxAttendees, Description, IsPublic)
    VALUES 
    (1, @Hall3ID, N'حفل الافتتاح الرسمي وإطلاق استراتيجية التحول الرقمي 2026', 'Ceremony', 
     DATEADD(HOUR, 10, CAST(@RateDate AS DATETIME2)), DATEADD(HOUR, 12, CAST(@RateDate AS DATETIME2)), N'معالي وزير الاتصالات والمعلوماتية', 300, N'مراسم الافتتاح الرسمي وقص الشريط والكلمات الافتتاحية للوفود الرسمية.', 1),
     
    (1, @Hall3ID, N'جلسة حوارية: السيادة الرقمية وأمن البيانات الوطنية في عصر السحابة', 'Lecture', 
     DATEADD(HOUR, 13, CAST(@RateDate AS DATETIME2)), DATEADD(HOUR, 15, CAST(@RateDate AS DATETIME2)), N'د. منير الشيباني & م. طارق الزنتاني', 200, N'مناقشة الأطر التشريعية والأمنية لحماية البيانات السيادية داخل الدولة الليبية.', 1),

    (1, @Hall3ID, N'ورشة عمل: تطبيقات الذكاء الاصطناعي التوليدي في القطاع المصرفي', 'Workshop', 
     DATEADD(DAY, 1, DATEADD(HOUR, 11, CAST(@RateDate AS DATETIME2))), DATEADD(DAY, 1, DATEADD(HOUR, 13, CAST(@RateDate AS DATETIME2))), N'م. عصام الغرياني - خبير ذكاء اصطناعي', 150, N'عرض عملي لنماذج الذكاء الاصطناعي وكيفية توظيفها في تحسين خدمة العملاء وكشف الاحتيال المالي.', 1);
END

-- -----------------------------------------------------------------------------
-- 14. الزوار، التسجيل في الفعاليات، التذاكر، وعمليات مسح البوابات
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM Visitors WHERE Email = 'walid.zarrouq@almadar.ly')
BEGIN
    INSERT INTO Visitors (TenantID, FullName, Email, Phone, Nationality, VisitorType, RegisteredAt, CreatedAt, IsDeleted)
    VALUES 
    (@TenantID, N'أ. وليد الزروق', 'walid.zarrouq@almadar.ly', '0919988776', N'ليبي', 'VIP', DATEADD(DAY, -4, @Now), @Now, 0),
    (@TenantID, N'م. فاطمة المبروك التائب', 'fatima.mabrouk@cbl.gov.ly', '0928877665', N'ليبي', 'Delegate', DATEADD(DAY, -3, @Now), @Now, 0),
    (@TenantID, N'السيد/ روبرت ميلر (Robert Miller)', 'robert.m@globaltelecom.co.uk', '+447911122334', N'بريطاني', 'Buyer', DATEADD(DAY, -2, @Now), @Now, 0);
END

DECLARE @Sched1 INT = (SELECT TOP 1 ScheduleID FROM ExhibitionSchedules WHERE ExhibitionID = 1 ORDER BY ScheduleID);
DECLARE @Sched2 INT = (SELECT TOP 1 ScheduleID FROM ExhibitionSchedules WHERE ExhibitionID = 1 ORDER BY ScheduleID DESC);

DECLARE @VisWalid INT = (SELECT TOP 1 VisitorID FROM Visitors WHERE Email = 'walid.zarrouq@almadar.ly');
DECLARE @VisFatima INT = (SELECT TOP 1 VisitorID FROM Visitors WHERE Email = 'fatima.mabrouk@cbl.gov.ly');
DECLARE @VisRobert INT = (SELECT TOP 1 VisitorID FROM Visitors WHERE Email = 'robert.m@globaltelecom.co.uk');

IF (@Sched1 IS NOT NULL AND @VisWalid IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ScheduleRegistrations WHERE ScheduleID = @Sched1 AND VisitorID = @VisWalid))
BEGIN
    INSERT INTO ScheduleRegistrations (ScheduleID, VisitorID, RegisteredAt, Status, CreatedAt)
    VALUES 
    (@Sched1, @VisWalid, DATEADD(DAY, -2, @Now), 'Registered', @Now);
END

IF (@Sched1 IS NOT NULL AND @VisFatima IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ScheduleRegistrations WHERE ScheduleID = @Sched1 AND VisitorID = @VisFatima))
BEGIN
    INSERT INTO ScheduleRegistrations (ScheduleID, VisitorID, RegisteredAt, Status, CreatedAt)
    VALUES 
    (@Sched1, @VisFatima, DATEADD(DAY, -2, @Now), 'Attended', @Now);
END

IF (@Sched2 IS NOT NULL AND @VisRobert IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ScheduleRegistrations WHERE ScheduleID = @Sched2 AND VisitorID = @VisRobert))
BEGIN
    INSERT INTO ScheduleRegistrations (ScheduleID, VisitorID, RegisteredAt, Status, CreatedAt)
    VALUES 
    (@Sched2, @VisRobert, DATEADD(DAY, -1, @Now), 'Registered', @Now);
END

-- إصدار التذاكر
IF (@VisWalid IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Tickets WHERE ExhibitionID = 1 AND VisitorID = @VisWalid))
BEGIN
    INSERT INTO Tickets (TenantID, VisitorID, ExhibitionID, TicketType, Price, CurrencyCode, QRCode, ValidDate, Status, IssuedAt, CreatedAt, IsDeleted)
    VALUES 
    (@TenantID, @VisWalid, 1, 'VIP Pass', 0.00, 'LYD', 'TITEX26-VIP-W001-9981A', @RateDate, 'Active', DATEADD(DAY, -2, @Now), @Now, 0);

    DECLARE @TktWalidId INT = SCOPE_IDENTITY();
    INSERT INTO TicketScans (TicketID, ScanDateTime, GateName, Direction, ScannedByUserId)
    VALUES 
    (@TktWalidId, DATEADD(HOUR, -3, @Now), N'بوابة كبار الشخصيات (VIP Gate 1)', 'In', @AdminUserId),
    (@TktWalidId, DATEADD(HOUR, -1, @Now), N'بوابة كبار الشخصيات (VIP Gate 1)', 'Out', @AdminUserId);
END

IF (@VisFatima IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Tickets WHERE ExhibitionID = 1 AND VisitorID = @VisFatima))
BEGIN
    INSERT INTO Tickets (TenantID, VisitorID, ExhibitionID, TicketType, Price, CurrencyCode, QRCode, ValidDate, Status, IssuedAt, CreatedAt, IsDeleted)
    VALUES 
    (@TenantID, @VisFatima, 1, 'Delegate Pass', 10.00, 'LYD', 'TITEX26-DEL-F002-4412B', @RateDate, 'Active', DATEADD(DAY, -2, @Now), @Now, 0);

    DECLARE @TktFatimaId INT = SCOPE_IDENTITY();
    INSERT INTO TicketScans (TicketID, ScanDateTime, GateName, Direction, ScannedByUserId)
    VALUES 
    (@TktFatimaId, DATEADD(HOUR, -2, @Now), N'البوابة الرئيسية (Main Gate A)', 'In', @AdminUserId);
END

IF (@VisRobert IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Tickets WHERE ExhibitionID = 1 AND VisitorID = @VisRobert))
BEGIN
    INSERT INTO Tickets (TenantID, VisitorID, ExhibitionID, TicketType, Price, CurrencyCode, QRCode, ValidDate, Status, IssuedAt, CreatedAt, IsDeleted)
    VALUES 
    (@TenantID, @VisRobert, 1, 'Buyer Pass', 15.00, 'LYD', 'TITEX26-BUY-R003-8871C', @RateDate, 'Active', DATEADD(DAY, -1, @Now), @Now, 0);

    DECLARE @TktRobertId INT = SCOPE_IDENTITY();
    INSERT INTO TicketScans (TicketID, ScanDateTime, GateName, Direction, ScannedByUserId)
    VALUES 
    (@TktRobertId, DATEADD(HOUR, -2, @Now), N'البوابة الرئيسية (Main Gate B)', 'In', @AdminUserId);
END

-- -----------------------------------------------------------------------------
-- 15. تقييمات الزوار (Visitor Ratings)
-- -----------------------------------------------------------------------------
IF (@VisWalid IS NOT NULL AND NOT EXISTS (SELECT 1 FROM VisitorRatings WHERE ExhibitionID = 1 AND VisitorID = @VisWalid))
BEGIN
    INSERT INTO VisitorRatings (VisitorID, ExhibitionID, ExhibitorID, Score, Comment, RatedAt)
    VALUES 
    (@VisWalid, 1, @ExMadar, 5, N'تنظيم استثنائي وجناح شركة المدار قدم تجربة مبهرة لتقنيات الجيل الخامس، وفريق العمل كان محترفاً جداً.', DATEADD(HOUR, -1, @Now)),
    (@VisWalid, 1, NULL, 5, N'معرض متميز يفوق التوقعات، القاعات مكيفة ومجهزة بأحدث الأنظمة والتسجيل كان سلساً وبدون أي تأخير.', DATEADD(HOUR, -1, @Now));
END

IF (@VisFatima IS NOT NULL AND NOT EXISTS (SELECT 1 FROM VisitorRatings WHERE ExhibitionID = 1 AND VisitorID = @VisFatima))
BEGIN
    INSERT INTO VisitorRatings (VisitorID, ExhibitionID, ExhibitorID, Score, Comment, RatedAt)
    VALUES 
    (@VisFatima, 1, @ExSarh, 4, N'حلول برمجية واعدة لمنظومة ERP المحلية، والورش المصاحبة للمعرض كانت غنية بالمعلومات القيمة.', DATEADD(HOUR, -1, @Now));
END

IF (@VisRobert IS NOT NULL AND NOT EXISTS (SELECT 1 FROM VisitorRatings WHERE ExhibitionID = 1 AND VisitorID = @VisRobert))
BEGIN
    INSERT INTO VisitorRatings (VisitorID, ExhibitionID, ExhibitorID, Score, Comment, RatedAt)
    VALUES 
    (@VisRobert, 1, NULL, 5, N'Great organization, modern venue, and excellent B2B networking opportunities in North Africa.', DATEADD(HOUR, -1, @Now));
END

-- -----------------------------------------------------------------------------
-- 16. قوالب الشارات وبطاقات الدخول (Badge Templates)
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM BadgeTemplates WHERE TargetParticipantType = 'Exhibitor')
BEGIN
    INSERT INTO BadgeTemplates (
        TenantID, ExhibitionID, TemplateName, TargetParticipantType, WidthMm, HeightMm, Orientation, 
        HeaderBgColor, HeaderTextColor, AccentColor, CategoryBadgeText, ShowCompanyName, ShowJobTitle, 
        ShowQRCode, ShowBarcode, ShowLogo, ShowHallName, ShowDates, IsDefault, CreatedAt, IsDeleted
    )
    VALUES 
    (@TenantID, 1, N'شارة العارضين الرسمية (Exhibitor Badge)', 'Exhibitor', 86, 120, 'Vertical', 
     '#1E3A8A', '#FFFFFF', '#F59E0B', N'عارض / EXHIBITOR', 1, 1, 1, 0, 1, 1, 1, 1, @Now, 0),

    (@TenantID, 1, N'شارة كبار الشخصيات (VIP Pass)', 'VIP', 100, 70, 'Horizontal', 
     '#111827', '#FFFFFF', '#EAB308', N'كبار الشخصيات / VIP', 1, 1, 1, 0, 1, 1, 1, 1, @Now, 0),

    (@TenantID, 1, N'شارة المتحدثين والخبراء (Speaker)', 'Speaker', 86, 120, 'Vertical', 
     '#7C3AED', '#FFFFFF', '#A855F7', N'متحدث / SPEAKER', 1, 1, 1, 0, 1, 1, 1, 1, @Now, 0);
END

-- -----------------------------------------------------------------------------
-- 17. الرعاية، الباقات، العقود، والمساحات الإعلانية (Sponsorships & Ads)
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM SponsorshipPackages WHERE ExhibitionID = 1)
BEGIN
    INSERT INTO SponsorshipPackages (
        TenantID, ExhibitionID, PackageName, Level, Price, CurrencyCode, MaxSponsorsCount, EntitlementsDescription, IsActive, CreatedAt, IsDeleted
    )
    VALUES 
    (@TenantID, 1, N'باقة الراعي البلاتيني الرئيسي (Platinum Sponsor)', 'Platinum', 50000.00, 'LYD', 1, 
     N'شعار الراعي البلاتيني في كافة المطبوعات والحملات الإعلانية + مساحة جناح 72 م² مجاناً + كلمة افتتاحية رئيسية + 20 تذكرة VIP.', 1, @Now, 0),

    (@TenantID, 1, N'باقة الراعي الذهبي (Gold Sponsor)', 'Gold', 30000.00, 'LYD', 2, 
     N'شعار الراعي الذهبي في البهو الرئيسي والموقع الإلكتروني + مساحة جناح 36 م² مجاناً + 10 تذاكر VIP.', 1, @Now, 0),

    (@TenantID, 1, N'باقة الراعي الفضي (Silver Sponsor)', 'Silver', 15000.00, 'LYD', 4, 
     N'شعار الراعي الفضي على حقائب المعرض وبطاقات الدخول + 5 تذاكر VIP.', 1, @Now, 0);
END

IF NOT EXISTS (SELECT 1 FROM Sponsors WHERE CompanyName LIKE N'%مصرف التجارة والتنمية%')
BEGIN
    INSERT INTO Sponsors (TenantID, CompanyName, ContactPerson, Phone, Email, WebsiteUrl, LogoUrl, CompanyProfile, IsActive, CreatedAt, IsDeleted)
    VALUES 
    (@TenantID, N'مصرف التجارة والتنمية (BCD)', N'أ. عبد السلام الكيلاني', '0914445566', 'sponsorship@bcd.ly', 'https://bcd.ly', '', N'المصرف التجاري الوطني الرائد في تقديم الخدمات المصرفية الرقمية وحلول الدفع الإلكتروني.', 1, @Now, 0),
    (@TenantID, N'الشركة القابضة للاتصالات (LPTIC)', N'م. إبراهيم الزوي', '0915551122', 'media@lptic.ly', 'https://lptic.ly', '', N'المظلة الاستراتيجية لقطاع الاتصالات وتقنية المعلومات والتحول الرقمي في الدولة الليبية.', 1, @Now, 0);
END

DECLARE @PkgDiamond INT = (SELECT TOP 1 PackageID FROM SponsorshipPackages WHERE Level = 'Platinum' AND ExhibitionID = 1);
DECLARE @PkgGold INT = (SELECT TOP 1 PackageID FROM SponsorshipPackages WHERE Level = 'Gold' AND ExhibitionID = 1);
DECLARE @SpnLptic INT = (SELECT TOP 1 SponsorID FROM Sponsors WHERE CompanyName LIKE N'%القابضة للاتصالات%');
DECLARE @SpnBcd INT = (SELECT TOP 1 SponsorID FROM Sponsors WHERE CompanyName LIKE N'%مصرف التجارة%');

IF (@PkgDiamond IS NOT NULL AND @SpnLptic IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SponsorshipContracts WHERE ExhibitionID = 1 AND SponsorID = @SpnLptic))
BEGIN
    INSERT INTO SponsorshipContracts (
        TenantID, ExhibitionID, SponsorID, PackageID, ContractNumber, TotalAmount, CurrencyCode, 
        Status, SignedDate, PaymentDueDate, SpecialTerms, Notes, CreatedByUserId, CreatedAt, IsDeleted
    )
    VALUES (
        @TenantID, 1, @SpnLptic, @PkgDiamond, 'SPN-2026-TITEX-001', 50000.00, 'LYD',
        'Confirmed', DATEADD(DAY, -15, @Now), DATEADD(DAY, 10, @Now),
        N'تلتزم إدارة المعرض بعرض شعار LPTIC في صدارة جميع الشاشات الرقمية والمنصة الرئيسية.',
        N'عقد الرعاية الماسية الحصري لمعرض TITEX 2026.', @AdminUserId, @Now, 0
    );
END

IF (@PkgGold IS NOT NULL AND @SpnBcd IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SponsorshipContracts WHERE ExhibitionID = 1 AND SponsorID = @SpnBcd))
BEGIN
    INSERT INTO SponsorshipContracts (
        TenantID, ExhibitionID, SponsorID, PackageID, ContractNumber, TotalAmount, CurrencyCode, 
        Status, SignedDate, PaymentDueDate, SpecialTerms, Notes, CreatedByUserId, CreatedAt, IsDeleted
    )
    VALUES (
        @TenantID, 1, @SpnBcd, @PkgGold, 'SPN-2026-TITEX-002', 30000.00, 'LYD',
        'Confirmed', DATEADD(DAY, -12, @Now), DATEADD(DAY, 15, @Now),
        N'تخصيص ركن خاص بنقاط البيع وفتح الحسابات الفورية في بهو المعرض.',
        N'عقد الرعاية الذهبية مع مصرف التجارة والتنمية.', @AdminUserId, @Now, 0
    );
END

-- المساحات الإعلانية
IF NOT EXISTS (SELECT 1 FROM AdvertisingSpaces WHERE ExhibitionID = 1)
BEGIN
    INSERT INTO AdvertisingSpaces (
        TenantID, ExhibitionID, VenueID, HallID, SpaceCode, SpaceName, SpaceType, DimensionsDescription, 
        BasePrice, CurrencyCode, IsAvailable, LocationNotes, PhotoUrl, CreatedAt, IsDeleted
    )
    VALUES 
    (@TenantID, 1, 1, NULL, 'ADV-SCR-01', N'شاشة المدخل الرئيسية الرقمية العملاقة', 'DigitalScreen', '1920x1080 px (LED 6x4m)', 
     6000.00, 'LYD', 0, N'أعلى البوابة الرئيسية مباشرة، يشاهدها 100% من زوار المعرض.', '', @Now, 0),

    (@TenantID, 1, 1, @Hall1ID, 'ADV-BAN-01', N'لافتة البهو المركزي المعلقة (Central Banner)', 'HangingBanner', '10m x 4m Double-Sided', 
     4000.00, 'LYD', 0, N'معلقة في قلب القاعة 1 بارتفاع 8 أمتار.', '', @Now, 0),

    (@TenantID, 1, 1, @Hall3ID, 'ADV-STG-01', N'شاشة خلفية منصة المؤتمرات الرئيسية', 'ConferenceHall', '8m x 3m 4K LED Screen', 
     5000.00, 'LYD', 1, N'خلف المتحدثين والضيوف الرسميين في قاعة المؤتمرات.', '', @Now, 0);
END

DECLARE @ContractLptic INT = (SELECT TOP 1 ContractID FROM SponsorshipContracts WHERE ContractNumber LIKE N'%001%');
DECLARE @ContractBcd INT = (SELECT TOP 1 ContractID FROM SponsorshipContracts WHERE ContractNumber LIKE N'%002%');
DECLARE @SpaceScr INT = (SELECT TOP 1 SpaceID FROM AdvertisingSpaces WHERE SpaceCode = 'ADV-SCR-01');
DECLARE @SpaceBan INT = (SELECT TOP 1 SpaceID FROM AdvertisingSpaces WHERE SpaceCode = 'ADV-BAN-01');

IF (@ContractLptic IS NOT NULL AND @SpaceScr IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SponsorAdAssignments WHERE SpaceID = @SpaceScr))
BEGIN
    INSERT INTO SponsorAdAssignments (ContractID, SpaceID, AssignedDate, AssetStatus, ArtworkFileUrl, Notes, CreatedAt, IsDeleted)
    VALUES (@ContractLptic, @SpaceScr, @Now, 'Approved', '', N'عرض فيديو التحول الرقمي عالي الدقة مدته 30 ثانية كل 3 دقائق.', @Now, 0);
END

IF (@ContractBcd IS NOT NULL AND @SpaceBan IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SponsorAdAssignments WHERE SpaceID = @SpaceBan))
BEGIN
    INSERT INTO SponsorAdAssignments (ContractID, SpaceID, AssignedDate, AssetStatus, ArtworkFileUrl, Notes, CreatedAt, IsDeleted)
    VALUES (@ContractBcd, @SpaceBan, @Now, 'Approved', '', N'بانر مصرف التجارة والتنمية للخدمات الرقمية.', @Now, 0);
END

-- -----------------------------------------------------------------------------
-- 18. المصروفات التشغيلية والتقرير المالي (Expenses & Financial Report)
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM Expenses WHERE ExhibitionID = 1)
BEGIN
    INSERT INTO Expenses (
        TenantID, ExhibitionID, Description, Category, Amount, CurrencyCode, BaseCurrencyCode, 
        ExchangeRateUsed, AmountInBaseCurrency, ExpenseDate, CreatedByUserId, CreatedAt, IsDeleted
    )
    VALUES 
    (@TenantID, 1, N'تجهيز وتركيب شاشات العرض وأنظمة الصوت والإضاءة المسرحية', N'صوتيات ومرئيات', 22500.00, 'LYD', 'LYD', 1.000000, 22500.00, DATEADD(DAY, -5, @RateDate), @AdminUserId, @Now, 0),
    (@TenantID, 1, N'الحملة الإعلانية الرقمية واللوحات الطرقية في طرابلس', N'دعاية وإعلام', 18000.00, 'LYD', 'LYD', 1.000000, 18000.00, DATEADD(DAY, -10, @RateDate), @AdminUserId, @Now, 0),
    (@TenantID, 1, N'خدمات الضيافة والاستقبال وحفل الافتتاح الرسمي للوفود', N'ضيافة ومراسم', 14500.00, 'LYD', 'LYD', 1.000000, 14500.00, DATEADD(DAY, -2, @RateDate), @AdminUserId, @Now, 0),
    (@TenantID, 1, N'تأمين المعرض وخدمات الحراسة والسلامة المهنية', N'أمن وسلامة', 8500.00, 'LYD', 'LYD', 1.000000, 8500.00, DATEADD(DAY, -3, @RateDate), @AdminUserId, @Now, 0),
    (@TenantID, 1, N'طباعة الشارات والدليل الرسمي للمشاركين ومواد الهوية البصرية', N'طباعة وتجهيز', 6500.00, 'LYD', 'LYD', 1.000000, 6500.00, DATEADD(DAY, -4, @RateDate), @AdminUserId, @Now, 0);
END

IF NOT EXISTS (SELECT 1 FROM FinancialReports WHERE ExhibitionID = 1)
BEGIN
    INSERT INTO FinancialReports (
        TenantID, ExhibitionID, TotalRevenue, TotalExpenses, NetProfit, TotalVisitors, TotalExhibitors, TotalBooths, OccupancyRate, CurrencyCode, GeneratedByUserId, GeneratedAt
    )
    VALUES (
        @TenantID, 1, 104390.00, 70000.00, 34390.00, 3500, 6, 13, 85.50, 'LYD', @AdminUserId, @Now
    );
END

COMMIT TRANSACTION;
PRINT N'تم ملء قاعدة البيانات بنجاح تام!';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    RAISERROR(@ErrMsg, @ErrSeverity, 1);
END CATCH;
