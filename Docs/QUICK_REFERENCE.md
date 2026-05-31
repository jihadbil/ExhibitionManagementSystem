# مرجع سريع - Quick Reference Guide
## Exhibition Management System

---

## 📋 البنية السريعة للنظام

### المتاحف والكيانات الرئيسية

```
┌─────────────────────────────────────────────────────────┐
│                   SYSTEM CORE                           │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Tenant (العميل/المستأجر)                             │
│  ├─ TenantID: مفتاح أساسي                              │
│  ├─ CompanyName: اسم الشركة                            │
│  ├─ Subdomain: نطاق فريد                               │
│  ├─ Plan: خطة الاشتراك                                 │
│  ├─ BaseCurrency: العملة الأساسية                      │
│  └─ IsActive: حالة التفعيل                             │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## 🔐 الهيكل الأمني

```
APPLICATION USER
├─ Inherits from IdentityUser
├─ TenantID (فصل المستأجرين)
├─ FullName
├─ IsActive
└─ LastLogin

APPLICATION ROLE
├─ Inherits from IdentityRole
├─ TenantID (أدوار منفصلة)
└─ Permissions

AUDIT LOG
├─ LogID: معرف السجل
├─ TenantID: العميل
├─ UserId: من قام
├─ TableName: أي جدول
├─ RecordID: أي سجل
├─ Action: CREATE/UPDATE/DELETE
├─ OldValues/NewValues: التغييرات
├─ ActionAt: متى
└─ IPAddress: من أين
```

---

## 🎪 هيكل الفعالية

```
EXHIBITION
├─ ExhibitionID
├─ TenantID
├─ VenueID
├─ Name: اسم المعرض
├─ Type: نوع المعرض
├─ Edition: الإصدار
├─ StartDate → EndDate
├─ Status: Planning/Open/Closed/Cancelled
├─ ExpectedVisitors
├─ EntryFee: رسم الدخول
├─ Description
└─ CreatedAt

	↓ يحتوي على

EXHIBITION SCHEDULE
├─ ScheduleID
├─ ExhibitionID
├─ HallID
├─ EventType: نوع الحدث
├─ StartTime → EndTime
└─ Description

	↓ تسجيلات

SCHEDULE REGISTRATION
├─ RegistrationID
├─ ScheduleID
├─ VisitorID (فريد لكل زائر لكل جدول)
└─ RegisteredAt
```

---

## 🏛️ هيكل المكان

```
VENUE (الموقع)
├─ VenueID
├─ TenantID
├─ Name: اسم الموقع
├─ Address: العنوان
├─ City/Country: المدينة والدولة
├─ TotalCapacity: السعة الكلية
├─ MapImageURL: صورة الخريطة
└─ IsActive

	↓

HALL (القاعة)
├─ HallID
├─ VenueID
├─ HallName
├─ AreaSqM: المساحة
├─ MaxBooths: الحد الأقصى للأكشاك
├─ FloorPlanWidth/Height
├─ FloorPlanJSON: الخطة المعقدة
└─ IsActive

	↓

BOOTH (الكشك)
├─ BoothID
├─ HallID
├─ BoothNumber: الرقم الفريد
├─ OriginalAreaSqM/CurrentAreaSqM
├─ Status: Available/Reserved/Merged/Maintenance
├─ IsMerged: هل مدمج؟
├─ MergeID: معرف الدمج
├─ PosX/PosY/Width/Height: الموقع والأبعاد
├─ RotationAngle: زاوية الدوران
├─ ShapeType: نوع الشكل
└─ ShapePolygonJSON: بيانات الشكل المعقد
```

---

## 🎫 هيكل الحجز

```
BOOTH RESERVATION
├─ ReservationID
├─ ExhibitorID: من يحجز
├─ BoothID: أي كشك
├─ ExhibitionID: أي معرض
├─ MergeID: أم مدمج؟
├─ BoothTypeSelected: النوع المختار
├─ RequestedAreaSqM: المساحة المطلوبة
├─ AllocatedAreaSqM: المساحة المخصصة
├─ ExhibitorCategory: فئة العارض
├─ BoothAmount: سعر الكشك
├─ ServicesAmount: سعر الخدمات
├─ TotalAmount: المجموع
├─ CurrencyCode: العملة
├─ ExchangeRateUsed: سعر الصرف المستخدم
├─ AmountInBaseCurrency: بالعملة الأساسية
├─ Status: Pending/Confirmed/Cancelled
├─ ReservationDate: تاريخ الحجز
├─ LogisticNotes: ملاحظات
└─ CreatedByUserId: من قام به

	↓ ترتبط بـ

RESERVATION SERVICE (الخدمات المحجوزة)
├─ ReservationServiceID
├─ ReservationID
└─ ServiceID
```

---

## 💰 هيكل المالية

```
INVOICE (الفاتورة)
├─ InvoiceID
├─ TenantID
├─ ReservationID
├─ InvoiceNumber: رقم فاتورة فريد
├─ InvoiceDate: تاريخ الإصدار
├─ SubTotal: المبلغ الفرعي
├─ TaxRate: معدل الضريبة (%)
├─ TaxAmount: مبلغ الضريبة
├─ TotalAmount: المبلغ الإجمالي
├─ CurrencyCode: العملة
├─ Status: Open/Paid/Cancelled
├─ DueDate: تاريخ الاستحقاق
└─ Notes: ملاحظات

	↓ تحتوي على

PAYMENT (الدفع)
├─ PaymentID
├─ InvoiceID
├─ PaymentDate: تاريخ الدفع
├─ Amount: المبلغ المدفوع
├─ CurrencyCode: العملة
├─ Method: طريقة الدفع
├─ ReferenceNo: رقم المرجع
├─ Status: Completed/Pending/Failed/Refunded
├─ Notes: ملاحظات
└─ ReceivedByUserId: من استقبل
```

---

## 🌍 العملات وأسعار الصرف

```
CURRENCY (العملة)
├─ CurrencyCode: رمز العملة (USD, EUR, SAR)
├─ CurrencyName: اسم العملة
├─ Symbol: الرمز ($, €, ﷼)
└─ IsActive: مفعل؟

EXCHANGE RATE (سعر الصرف)
├─ RateID
├─ FromCurrency: من عملة
├─ ToCurrency: إلى عملة
├─ Rate: السعر (18,6 decimal)
├─ RateDate: تاريخ السعر
├─ Source: مصدر السعر
├─ CreatedByUserId: من أنشأه
└─ CreatedAt: متى
```

---

## 🎫 هيكل التذاكر

```
VISITOR (الزائر)
├─ VisitorID
├─ TenantID
├─ FullName: الاسم الكامل
├─ Phone: الهاتف
├─ Email: البريد الإلكتروني
├─ Nationality: الجنسية
├─ VisitorType: نوع الزائر
└─ RegisteredAt: تاريخ التسجيل

	↓

TICKET (التذكرة)
├─ TicketID
├─ VisitorID
├─ ExhibitionID
├─ TicketType: نوع التذكرة
├─ Price: السعر
├─ CurrencyCode: العملة
├─ QRCode: رمز QR (فريد)
├─ ValidDate: تاريخ الصلاحية
├─ Status: Active/Entered/Exited
└─ IssuedAt: تاريخ الإصدار

	↓ يتم مسح

TICKET SCAN (مسح التذكرة)
├─ ScanID
├─ TicketID
├─ ScanDateTime: وقت المسح الدقيق
├─ GateName: اسم البوابة
├─ Direction: In/Out
└─ ScannedByUserId: من قام بالمسح

	↓

VISITOR RATING (التقييم)
├─ RatingID
├─ VisitorID
├─ ExhibitionID
├─ ExhibitorID: (اختياري)
├─ Score: 1-5 (Check Constraint)
├─ Comment: التعليق
└─ RatedAt: تاريخ التقييم
```

---

## 🏢 هيكل العارضين

```
EXHIBITOR (العارض)
├─ ExhibitorID
├─ TenantID
├─ CompanyName: اسم الشركة
├─ ContactPerson: الشخص المسؤول
├─ Phone: الهاتف
├─ Email: البريد الإلكتروني
├─ Sector: القطاع
├─ Nationality: الجنسية
├─ ExhibitorCategory: الفئة (Gold/Silver/Bronze)
├─ LogoURL: رابط الشعار
├─ CompanyProfile: ملف الشركة
└─ IsActive: مفعل؟

	↓

BOOTH STAFF (موظفو الكشك)
├─ StaffID
├─ ReservationID
├─ StaffName: اسم الموظف
├─ Role: الدور
├─ Phone: الهاتف
├─ Email: البريد الإلكتروني
├─ BadgeIssued: هل تم إصدار شارة؟
└─ BadgeNumber: رقم الشارة
```

---

## 🛍️ هيكل الخدمات والتسعير

```
SERVICE (الخدمة)
├─ ServiceID
├─ TenantID
├─ ServiceName: اسم الخدمة
├─ Category: الفئة
├─ Unit: الوحدة
├─ DefaultPrice: السعر الافتراضي
├─ IsMandatory: إلزامية؟
├─ Description: الوصف
└─ IsActive: مفعل؟

PRICING PACKAGE (حزمة التسعير)
├─ PackageID
├─ TenantID
├─ PackageName: اسم الحزمة
├─ Description: الوصف
├─ TotalPrice: السعر الكلي
├─ CurrencyCode: العملة
├─ ValidFrom → ValidTo: فترة الصلاحية
└─ IsActive: مفعل؟

	↓

PACKAGE SERVICE (الخدمات في الحزمة)
├─ PackageServiceID
├─ PackageID
└─ ServiceID

BOOTH PRICE RULE (قاعدة تسعير الكشك)
├─ RuleID
├─ BoothType: نوع الكشك
├─ ExhibitorCategory: فئة العارض
├─ BasePrice: السعر الأساسي
├─ CurrencyCode: العملة
├─ ApplicableFrom → ApplicableTo
└─ IsActive

SERVICE PRICE RULE (قاعدة تسعير الخدمة)
├─ RuleID
├─ ServiceID
├─ Factor: معامل التعديل
├─ ApplicableFrom → ApplicableTo
└─ IsActive
```

---

## 📊 أنواع التعدادات (Enums)

```
ExhibitionStatus:     Planning, Open, Closed, Cancelled
BoothStatus:          Available, Reserved, Merged, Maintenance
BoothType:            Standard, Premium, Luxury
BoothShapeType:       Rect, (Custom shapes)
ReservationStatus:    Pending, Confirmed, Cancelled
ExhibitorCategory:    Gold, Silver, Bronze, Local
InvoiceStatus:        Open, Paid, Cancelled
PaymentMethod:        CreditCard, BankTransfer, Cash, Check
PaymentStatus:        Completed, Pending, Failed, Refunded
ScanDirection:        In, Out
EventType:            Conference, Workshop, Demo, Presentation
```

---

## 🔍 الفهارس الرئيسية (Indexes)

```
╔════════════════════════════════════════════════════════╗
║                COMPOSITE INDEXES                       ║
╠════════════════════════════════════════════════════════╣
║ Exhibition (TenantID, Status)                          ║
║ BoothReservation (ExhibitionID, Status)                ║
║ BoothReservation (ExhibitorID)                         ║
║ Booth (HallID, Status)                                 ║
║ Ticket (ExhibitionID, Status)                          ║
║ TicketScan (TicketID)                                  ║
║ AuditLog (TenantID, ActionAt)                          ║
║ Invoice (ReservationID)                                ║
║ Payment (InvoiceID)                                    ║
╚════════════════════════════════════════════════════════╝

╔════════════════════════════════════════════════════════╗
║              UNIQUE INDEXES                            ║
╠════════════════════════════════════════════════════════╣
║ Tenant.Subdomain                                       ║
║ Ticket.QRCode                                          ║
║ ExchangeRate (FromCurrency, ToCurrency, RateDate)     ║
║ Invoice (TenantID, InvoiceNumber)                      ║
║ ScheduleRegistration (ScheduleID, VisitorID)          ║
╚════════════════════════════════════════════════════════╝
```

---

## ⚙️ معادلات الحسابات

### حساب سعر الحجز
```
BoothAmount = BoothPriceRule.BasePrice × Category Factor
ServicesAmount = Σ(Service.DefaultPrice)
SubTotal = BoothAmount + ServicesAmount
TaxAmount = SubTotal × (TaxRate / 100)
TotalAmount = SubTotal + TaxAmount

Exchange Rate Calculation:
AmountInBaseCurrency = TotalAmount × ExchangeRate.Rate
```

### حساب مدة الزيارة
```
Duration = Exit Scan Time - Entry Scan Time
Average Duration = Σ(Duration) / Count(Visitors)
```

### معدل التحصيل
```
Collection Rate = Paid Invoices / Total Invoices × 100%
Outstanding = Total Invoices - Paid Invoices
Days Overdue = Today - Invoice.DueDate
```

---

## 📈 Queries الشائعة

```sql
-- إجمالي الإيرادات
SELECT SUM(TotalAmount) 
FROM Invoice 
WHERE Status = 'Paid' 
  AND TenantID = @TenantID

-- الحضور اليومي
SELECT CAST(ScanDateTime AS DATE) as Date, COUNT(*) as Count
FROM TicketScan
WHERE Direction = 'In'
  AND ExhibitionID = @ExhibitionID
GROUP BY CAST(ScanDateTime AS DATE)

-- أوقات الذروة
SELECT HOUR(ScanDateTime) as Hour, COUNT(*) as Count
FROM TicketScan
GROUP BY HOUR(ScanDateTime)
ORDER BY Count DESC

-- متوسط التقييم
SELECT AVG(CAST(Score AS FLOAT)) as AvgRating
FROM VisitorRating
WHERE ExhibitionID = @ExhibitionID

-- أكشاك متوفرة
SELECT * FROM Booth
WHERE Status = 'Available'
  AND CurrentAreaSqM >= @RequestedArea
  AND HallID = @HallID

-- سجل التدقيق
SELECT * FROM AuditLog
WHERE TenantID = @TenantID
  AND ActionAt BETWEEN @StartDate AND @EndDate
ORDER BY ActionAt DESC
```

---

## 🔐 Check Constraints

```sql
-- تقييم من 1 إلى 5
ALTER TABLE VisitorRating
ADD CONSTRAINT CK_VISITOR_RATINGS_Score 
CHECK (Score >= 1 AND Score <= 5)

-- معدل الضريبة غير سالب
ALTER TABLE Invoice
ADD CONSTRAINT CK_INVOICE_TaxRate
CHECK (TaxRate >= 0)

-- المساحات موجبة
ALTER TABLE Booth
ADD CONSTRAINT CK_BOOTH_Area
CHECK (OriginalAreaSqM > 0 AND CurrentAreaSqM > 0)
```

---

## 🚀 Performance Tips

```
✅ استخدم Indexes الموجودة
✅ تجنب N+1 Query Problem (استخدم Include)
✅ استخدم Paging للبيانات الكبيرة
✅ Cache البيانات الثابتة (Currency, Services)
✅ استخدم Lazy Loading للعلاقات غير المهمة
✅ Batch Operations عندما يكون ممكناً
✅ استخدم Stored Procedures للعمليات المعقدة
```

---

## 📱 الأشياء المهمة

```
⚠️ DeleteBehavior.Restrict على البيانات المالية
⚠️ TenantID Filtering لمنع تسرب البيانات
⚠️ ExchangeRate Lookup قبل كل عملية مالية
⚠️ Validation على جميع المدخلات
⚠️ Audit Logging لجميع التغييرات الهامة
⚠️ Error Handling الشامل
⚠️ Transaction Management للعمليات المعقدة
```

---

## 🎯 خطوات التطوير الشائعة

```
1. Backup قاعدة البيانات
2. تحديث النماذج (Models)
3. إضافة Migration
4. تحديث DbContext
5. تحديث الـ Services/Repositories
6. إضافة Tests
7. تحديث الـ APIs
8. تحديث الـ DTOs
9. Testing شامل
10. Deployment
```

---

## 📚 ملفات التوثيق الأخرى

```
📄 SYSTEM_FEATURES_ANALYSIS.md - تحليل شامل للميزات
📄 ARCHITECTURE_MINDMAP.md - خريطة معمارية بصرية
📄 USE_CASES_GUIDE.md - دليل حالات الاستخدام العملية
📄 FEATURES_SUMMARY.md - ملخص الميزات
📄 QUICK_REFERENCE.md - هذا الملف
```

---

**آخر تحديث: 2024** 📅

للمزيد من المعلومات، راجع المستندات الأخرى 📖

