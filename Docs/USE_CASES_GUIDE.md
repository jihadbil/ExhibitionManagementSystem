# دليل الحالات الاستخدام العملية
## Exhibition Management System - Use Cases Guide

---

## 📚 جدول المحتويات

1. [حالات استخدام إدارة المعارض](#حالات-استخدام-إدارة-المعارض)
2. [حالات استخدام الحجوزات](#حالات-استخدام-الحجوزات)
3. [حالات استخدام المالية](#حالات-استخدام-المالية)
4. [حالات استخدام الزيارات](#حالات-استخدام-الزيارات)
5. [حالات استخدام الإدارة](#حالات-استخدام-الإدارة)

---

## 🎪 حالات استخدام إدارة المعارض

### USE CASE 1: إنشاء معرض جديد

**الممثلون:** مسؤول الموقع (Site Administrator)

**السيناريو:**
```
1. مسؤول الموقع يقوم بتسجيل الدخول
2. يختار "إنشاء معرض جديد"
3. يملأ التفاصيل:
   - اسم المعرض
   - النوع (تكنولوجيا، هندسة، إلخ)
   - الإصدار (2024، 2025، إلخ)
   - الموقع الجغرافي (Venue)
   - التواريخ (البداية والنهاية)
   - السعة المتوقعة
   - رسم الدخول (إن وجد)
4. النظام ينشئ Exhibition
5. يتم إنشاء AuditLog
```

**النتائج المتوقعة:**
```csharp
Exhibition {
  ExhibitionID: 1,
  TenantID: 1,
  VenueID: 1,
  Name: "Tech Expo 2024",
  Status: ExhibitionStatus.Planning,
  StartDate: 2024-06-01,
  EndDate: 2024-06-03,
  EntryFee: 50.00,
  EntryCurrency: "USD"
}
```

**الفوائد:**
✅ معرض جديد جاهز للحجوزات  
✅ تسجيل كامل في سجل التدقيق  
✅ معلومات موحدة  

---

### USE CASE 2: تخطيط أكشاك المعرض

**الممثلون:** مسؤول التخطيط (Planning Manager)

**السيناريو:**
```
1. فتح المعرض في نظام التخطيط
2. اختيار القاعة المراد تخطيطها
3. إضافة أكشاك:
   - تحديد الموقع (X, Y)
   - حجم الكشك (Width, Height)
   - الشكل (مستطيل/معقد)
   - الرقم الفريد
4. النظام يعرض خريطة تفاعلية
5. إمكانية دمج أكشاك متجاورة
6. حفظ الخطة (Hall.FloorPlanJSON)
```

**الكيانات المستخدمة:**
```csharp
Hall {
  HallID: 1,
  VenueID: 1,
  HallName: "Main Hall",
  AreaSqM: 1000.00,
  MaxBooths: 50,
  FloorPlanJSON: {...} // خريطة معقدة
}

Booth {
  BoothID: 1,
  HallID: 1,
  BoothNumber: "A-01",
  OriginalAreaSqM: 20.00,
  PosX: 10.5,
  PosY: 20.3,
  Width: 5.0,
  Height: 4.0,
  RotationAngle: 0,
  ShapeType: BoothShapeType.Rect,
  Status: BoothStatus.Available
}
```

**الفوائد:**
✅ تخطيط مرئي وديناميكي  
✅ أشكال معقدة مدعومة  
✅ حساب مساحة دقيق  

---

### USE CASE 3: إدارة قوائم الفئات

**الممثلون:** مسؤول الفئات (Category Manager)

**السيناريو:**
```
1. تعريف فئات العارضين:
   - فئة الذهب (Gold)
   - فئة الفضة (Silver)
   - فئة البرونز (Bronze)
   - المشاركون المحليون
2. ربط كل فئة بقواعد تسعير
3. تعيين الامتيازات:
   - حجم الكشك الأساسي
   - الخدمات المضمنة
   - الخصومات
```

**التخزين:**
```csharp
// في ExhibitorCategory Enum
public enum ExhibitorCategory
{
	Gold,
	Silver,
	Bronze,
	Local
}

// في BoothPriceRule
BoothPriceRule {
  RuleID: 1,
  BoothType: "Standard",
  ExhibitorCategory: ExhibitorCategory.Gold,
  BasePrice: 5000.00,
  CurrencyCode: "USD",
  ApplicableFrom: 2024-01-01,
  ApplicableTo: 2024-12-31
}
```

---

## 🎫 حالات استخدام الحجوزات

### USE CASE 4: حجز كشك كعارض

**الممثلون:** العارض (Exhibitor)

**السيناريو الكامل:**

```
1️⃣ تسجيل الدخول
   └─ ApplicationUser (Email-based)
   └─ TenantID محدد

2️⃣ اختيار المعرض
   └─ عرض Exhibition list
   └─ تصفية حسب التاريخ والحالة
   └─ اختيار "Tech Expo 2024"

3️⃣ ملء طلب الحجز
   ┌─ اختيار نوع الكشك:
   │  ├─ Standard 3×3m
   │  ├─ Premium 5×5m
   │  └─ Luxury 7×7m
   │
   ├─ تحديد المساحة:
   │  └─ Requested Area: 25 sqm (5×5)
   │
   ├─ اختيار الخدمات:
   │  ├─ ✓ Electricity (mandatory)
   │  ├─ ☐ Signage Board
   │  ├─ ☐ WiFi Connection
   │  └─ ☐ Staff Assistance
   │
   └─ ملاحظات لوجستية:
	  └─ "Need forklift access"

4️⃣ حساب السعر (Pricing Engine)
   ┌─ Load BoothPriceRule:
   │  └─ Base Price (for Gold Category): 5000.00
   │
   ├─ Load ServicePriceRule:
   │  ├─ Electricity (mandatory): 300.00
   │  ├─ Signage: 150.00
   │  └─ WiFi: 100.00
   │
   ├─ Calculate:
   │  ├─ Booth Amount: 5000.00
   │  ├─ Services Amount: 550.00
   │  ├─ Subtotal: 5550.00
   │  ├─ Tax (10%): 555.00
   │  └─ Total: 6105.00
   │
   └─ Currency Exchange:
	  ├─ Original: USD 6105.00
	  ├─ Load ExchangeRate (USD→SAR): 3.75
	  └─ Base Currency: SAR 22,893.75

5️⃣ إنشاء السجلات
   ┌─ BoothReservation:
   │  ├─ ExhibitorID: 1
   │  ├─ ExhibitionID: 1
   │  ├─ Status: Pending
   │  ├─ BoothAmount: 5000.00
   │  ├─ ServicesAmount: 550.00
   │  ├─ TotalAmount: 6105.00
   │  ├─ CurrencyCode: USD
   │  ├─ ExchangeRateUsed: 3.75
   │  ├─ AmountInBaseCurrency: 22893.75
   │  └─ CreatedByUserId: user@exhibitor.com
   │
   ├─ Invoice:
   │  ├─ InvoiceNumber: INV-2024-001
   │  ├─ SubTotal: 5550.00
   │  ├─ TaxRate: 10%
   │  ├─ TaxAmount: 555.00
   │  ├─ TotalAmount: 6105.00
   │  ├─ Status: Open
   │  └─ DueDate: 2024-05-15
   │
   ├─ Booth Assignment:
   │  ├─ Find Available Booth matching:
   │  │  ├─ Area ≥ 25 sqm
   │  │  └─ Status = Available
   │  ├─ Assign: BoothID 45 (5×5, Area: 25 sqm)
   │  └─ Update Booth.Status = Reserved
   │
   ├─ ReservationService:
   │  ├─ Link to Electricity Service
   │  ├─ Link to Signage Service
   │  └─ Link to WiFi Service
   │
   └─ AuditLog:
	  ├─ TableName: "BoothReservation"
	  ├─ Action: "CREATE"
	  ├─ UserId: "user@exhibitor.com"
	  ├─ NewValues: {...}
	  └─ ActionAt: 2024-05-01 10:30:15 UTC

6️⃣ إرسال الفاتورة
   └─ Email Invoice
   └─ Invoice.Status: Open
   └─ Awaiting Payment

7️⃣ النتيجة النهائية
   ✅ BoothReservation مع Status = Pending
   ✅ Invoice مع Status = Open
   ✅ Booth مع Status = Reserved
   ✅ Services linked
   ✅ Price locked with ExchangeRate
   ✅ كل شيء مُتَتَبَع في AuditLog
```

**الكود الممكن:**
```csharp
// Step 1: Create BoothReservation
var reservation = new BoothReservation
{
	ExhibitorID = 1,
	BoothTypeSelected = BoothType.Premium,
	RequestedAreaSqM = 25.00m,
	ExhibitorCategory = ExhibitorCategory.Gold,
	ExhibitionID = 1,
	BoothAmount = 5000.00m,
	ServicesAmount = 550.00m,
	TotalAmount = 6105.00m,
	CurrencyCode = "USD",
	Status = ReservationStatus.Pending,
	CreatedByUserId = "user@exhibitor.com"
};

// Step 2: Get Current Exchange Rate
var rate = dbContext.ExchangeRates
	.Where(e => e.FromCurrency == "USD" 
			 && e.ToCurrency == tenant.BaseCurrency)
	.OrderByDescending(e => e.RateDate)
	.First();

reservation.ExchangeRateUsed = rate.Rate;
reservation.AmountInBaseCurrency = reservation.TotalAmount * rate.Rate;

dbContext.BoothReservations.Add(reservation);

// Step 3: Create Invoice
var invoice = new Invoice
{
	ReservationID = reservation.ReservationID,
	InvoiceNumber = GenerateInvoiceNumber(tenant.TenantID),
	SubTotal = 5550.00m,
	TaxRate = 10m,
	TaxAmount = 555.00m,
	TotalAmount = 6105.00m,
	CurrencyCode = "USD",
	Status = InvoiceStatus.Open,
	DueDate = DateTime.UtcNow.AddDays(30)
};

dbContext.Invoices.Add(invoice);

// Step 4: Find and Reserve Available Booth
var availableBooth = dbContext.Booths
	.Where(b => b.Hall.VenueID == exhibition.VenueID
			 && b.Status == BoothStatus.Available
			 && b.CurrentAreaSqM >= reservation.RequestedAreaSqM)
	.FirstOrDefault();

if (availableBooth != null)
{
	availableBooth.Status = BoothStatus.Reserved;
	reservation.BoothID = availableBooth.BoothID;
}

// Step 5: Link Services
foreach (var serviceId in selectedServiceIds)
{
	var resService = new ReservationService
	{
		ReservationID = reservation.ReservationID,
		ServiceID = serviceId
	};
	dbContext.ReservationServices.Add(resService);
}

await dbContext.SaveChangesAsync();
```

---

### USE CASE 5: دمج أكشاك متعددة

**الممثلون:** مسؤول الحجوزات (Booking Manager)

**السيناريو:**
```
1. عارض يطلب مساحة أكبر من الكشك الواحد
2. مسؤول الحجوزات يتحقق من الأكشاك المتوفرة
3. يختار 4 أكشاك متجاورة (4×20sqm = 80sqm)
4. النظام ينفذ الدمج:

   ✓ Create BoothMerge (80 sqm total)
   ✓ Create 4 BoothMergeItems (ربط كل كشك)
   ✓ Update 4 Booth.IsMerged = true
   ✓ Update 4 Booth.MergeID = mergeID
   ✓ Update 4 Booth.Status = Merged
   ✓ Link BoothReservation.MergeID
   ✓ Re-calculate pricing based on merged area
   ✓ Update Invoice with new total
   ✓ Log all changes in AuditLog
```

**النتيجة:**
```csharp
BoothMerge {
  MergeID: 1,
  ExhibitionID: 1,
  MergedBoothLabel: "A-01/A-02/B-01/B-02",
  TotalAreaSqM: 80.00m,
  ReservationID: 1,
  MergedAt: 2024-05-01 11:00:00,
  MergedByUserId: "manager@exhibition.com",
  Notes: "Merged for large tech company setup"
}

BoothMergeItems: [
  { MergeID: 1, BoothID: 1, ItemOrder: 1 },
  { MergeID: 1, BoothID: 2, ItemOrder: 2 },
  { MergeID: 1, BoothID: 51, ItemOrder: 3 },
  { MergeID: 1, BoothID: 52, ItemOrder: 4 }
]
```

---

## 💰 حالات استخدام المالية

### USE CASE 6: معالجة الدفع

**الممثلون:** محاسب (Accountant)

**السيناريو:**
```
1️⃣ استقبال الدفع من العارض
   ├─ المبلغ: 6105.00 USD
   ├─ الطريقة: Credit Card
   └─ رقم المرجع: TXN-2024-12345

2️⃣ التحقق من الفاتورة
   └─ Invoice.TotalAmount == 6105.00 ✓
   └─ Invoice.Status == Open ✓

3️⃣ تسجيل الدفع
   ┌─ Create Payment:
   │  ├─ InvoiceID: 1
   │  ├─ Amount: 6105.00
   │  ├─ CurrencyCode: USD
   │  ├─ Method: CreditCard
   │  ├─ ReferenceNo: TXN-2024-12345
   │  ├─ Status: Completed
   │  └─ ReceivedByUserId: "accountant@company.com"
   │
   ├─ Update Invoice:
   │  └─ Status: Paid
   │
   ├─ Update Reservation:
   │  └─ Status: Confirmed
   │
   └─ Log in AuditLog:
	  ├─ TableName: "Payment"
	  ├─ Action: "CREATE"
	  ├─ UserId: "accountant@company.com"
	  └─ OldValues: null
	  └─ NewValues: {...}

4️⃣ النتيجة
   ✅ Payment recorded
   ✅ Invoice.Status = Paid
   ✅ Reservation.Status = Confirmed
   ✅ Exhibitor confirmed
   ✅ Badge can be issued
```

**الكود:**
```csharp
var payment = new Payment
{
	InvoiceID = invoiceId,
	PaymentDate = DateTime.UtcNow,
	Amount = 6105.00m,
	CurrencyCode = "USD",
	Method = PaymentMethod.CreditCard,
	ReferenceNo = "TXN-2024-12345",
	Status = PaymentStatus.Completed,
	ReceivedByUserId = currentUser.Id
};

dbContext.Payments.Add(payment);

// Update Invoice
invoice.Status = InvoiceStatus.Paid;

// Update Reservation
var reservation = invoice.Reservation;
reservation.Status = ReservationStatus.Confirmed;

await dbContext.SaveChangesAsync();
```

---

### USE CASE 7: تقرير مالي شامل

**الممثلون:** مدير المالية (Finance Manager)

**السيناريو:**
```
1. اختيار نطاق التاريخ:
   └─ From: 2024-06-01 To: 2024-06-03

2. استعلام البيانات:
   ├─ Total Invoices: 150
   ├─ Total Amount: SAR 750,000
   ├─ Paid Invoices: 145
   ├─ Paid Amount: SAR 725,000
   ├─ Outstanding: 5
   ├─ Outstanding Amount: SAR 25,000
   │
   └─ Breakdown:
	  ├─ By Exhibitor Category:
	  │  ├─ Gold: SAR 450,000 (60%)
	  │  ├─ Silver: SAR 225,000 (30%)
	  │  └─ Bronze: SAR 75,000 (10%)
	  │
	  ├─ By Booth Type:
	  │  ├─ Standard: SAR 300,000 (40%)
	  │  ├─ Premium: SAR 375,000 (50%)
	  │  └─ Luxury: SAR 75,000 (10%)
	  │
	  └─ By Payment Method:
		 ├─ Credit Card: SAR 500,000 (67%)
		 ├─ Bank Transfer: SAR 200,000 (27%)
		 └─ Cash: SAR 25,000 (3%)
		 └─ Outstanding: SAR 25,000 (3%)

3. التقرير النهائي:
   ✓ Total Revenue: SAR 750,000
   ✓ Collection Rate: 96.7%
   ✓ Outstanding Follow-up: 5 invoices
   ✓ Export to Excel
   ✓ Send to Finance Department
```

**الاستعلام:**
```csharp
var report = dbContext.Invoices
	.Where(i => i.TenantID == tenantId
			 && i.InvoiceDate >= startDate
			 && i.InvoiceDate <= endDate)
	.GroupBy(i => new { i.Status, i.CurrencyCode })
	.Select(g => new
	{
		Status = g.Key.Status,
		Currency = g.Key.CurrencyCode,
		Count = g.Count(),
		TotalAmount = g.Sum(i => i.TotalAmount)
	})
	.ToList();
```

---

## 🎫 حالات استخدام الزيارات

### USE CASE 8: شراء وإدارة التذاكر

**الممثلون:** الزائر (Visitor), موظف البيع (Sales)

**السيناريو:**
```
1️⃣ الزائر يقرر شراء تذكرة
   ├─ اختيار المعرض
   ├─ اختيار نوع التذكرة:
   │  ├─ General Admission (Regular): SAR 50
   │  ├─ Premium Pass (VIP): SAR 100
   │  └─ Student (with ID): SAR 25
   └─ اختيار التاريخ

2️⃣ حساب السعر
   ├─ Base Price: SAR 50
   ├─ Tax (0% for tickets): SAR 0
   └─ Total: SAR 50

3️⃣ إنشاء التذكرة
   ┌─ Create Visitor:
   │  ├─ FullName: "Ahmed Ali"
   │  ├─ Email: "ahmed@example.com"
   │  ├─ Nationality: "Saudi Arabia"
   │  └─ VisitorType: "Regular"
   │
   ├─ Create Ticket:
   │  ├─ VisitorID: 1
   │  ├─ ExhibitionID: 1
   │  ├─ TicketType: "General Admission"
   │  ├─ Price: 50.00
   │  ├─ CurrencyCode: "SAR"
   │  ├─ QRCode: "TICKET-2024-000001-UNIQUE-HASH"
   │  ├─ ValidDate: 2024-06-01
   │  ├─ Status: "Active"
   │  └─ IssuedAt: 2024-05-20 14:30:00
   │
   └─ Send Ticket:
	  ├─ Email with QR Code
	  ├─ Printable PDF
	  └─ Mobile app access

4️⃣ النتيجة
   ✅ Ticket created with unique QR
   ✅ Email sent to visitor
   ✅ Ready for scanning
```

---

### USE CASE 9: مسح التذاكر والدخول

**الممثلون:** موظف البوابة (Gate Officer)

**السيناريو الكامل:**
```
1️⃣ الزائر يصل البوابة
   └─ يظهر التذكرة (ورقية أو رقمية)

2️⃣ موظف البوابة يمسح QR Code
   ├─ يستخدم جهاز الهاتف الذكي أو الماسح الضوئي
   ├─ يقرأ: "TICKET-2024-000001-UNIQUE-HASH"
   └─ النظام يتحقق من الصلاحية:
	  ├─ Ticket exists ✓
	  ├─ Ticket.Status = Active ✓
	  ├─ ValidDate >= today ✓
	  └─ Not already scanned for entry ✓

3️⃣ تسجيل مسح الدخول
   ┌─ Create TicketScan:
   │  ├─ TicketID: 1
   │  ├─ ScanDateTime: 2024-06-01 10:15:30 UTC
   │  ├─ GateName: "Main Gate - Door A"
   │  ├─ Direction: ScanDirection.In
   │  └─ ScannedByUserId: "gate-officer-1"
   │
   ├─ Update Ticket:
   │  └─ Status: "Entered"
   │
   └─ Log Entry:
	  ├─ AuditLog record
	  ├─ تتبع دخول الزائر
	  └─ إحصائيات فورية

4️⃣ السماح بالدخول
   └─ ✅ Entry granted
   └─ Display: "Welcome Ahmed!"
   └─ الزائر يدخل المعرض

5️⃣ الزائر يتجول في المعرض
   ├─ يزور الأكشاك
   ├─ يشاهد العروض
   ├─ يجمع المعلومات
   └─ يستغرق وقتاً

6️⃣ الزائر يغادر المعرض
   └─ يعود إلى البوابة

7️⃣ موظف البوابة يمسح QR Code مرة أخرى
   ├─ يقرأ: "TICKET-2024-000001-UNIQUE-HASH"
   └─ النظام يتحقق:
	  ├─ Already scanned for entry ✓
	  ├─ Not yet scanned for exit ✓
	  └─ Time between in/out valid ✓

8️⃣ تسجيل مسح الخروج
   ┌─ Create TicketScan:
   │  ├─ TicketID: 1
   │  ├─ ScanDateTime: 2024-06-01 14:45:15 UTC
   │  ├─ GateName: "Main Gate - Door A"
   │  ├─ Direction: ScanDirection.Out
   │  └─ ScannedByUserId: "gate-officer-1"
   │
   ├─ Update Ticket:
   │  └─ Status: "Exited"
   │
   └─ Calculate Statistics:
	  ├─ Time in Exhibition: 4h 29m 45s
	  ├─ Entry Time: 10:15 AM
	  ├─ Exit Time: 14:45 PM
	  └─ Duration: recorded for analytics

9️⃣ إحصائيات فعلية
   └─ System updates in real-time:
	  ├─ Today's Attendance: +1
	  ├─ Current in Exhibition: -1
	  ├─ Total Check-ins: incremented
	  ├─ Peak Hour Analysis: updated
	  └─ Occupancy Rate: calculated

🔟 النتيجة النهائية
   ✅ Visitor entry/exit tracked
   ✅ Duration calculated
   ✅ Real-time attendance stats
   ✅ Security maintained (no double entry)
   ✅ Analytics updated
   ✅ Audit trail complete
```

**الكود:**
```csharp
// Step 1: Validate QR Code
var ticket = dbContext.Tickets
	.Include(t => t.TicketScans)
	.FirstOrDefault(t => t.QRCode == scannedQRCode);

if (ticket == null)
	throw new InvalidTicketException("QR Code not found");

// Step 2: Check validation rules
if (ticket.ValidDate < DateTime.Today)
	throw new ExpiredTicketException("Ticket expired");

if (ticket.Status == "Exited")
	throw new AlreadyUsedTicketException("Ticket already used");

// Step 3: Determine direction
bool isEntry = !ticket.TicketScans.Any(ts => ts.Direction == ScanDirection.In);
if (!isEntry && ticket.TicketScans.Count(ts => ts.Direction == ScanDirection.Out) > 0)
	throw new InvalidScanException("Cannot scan exit twice");

// Step 4: Create scan record
var scan = new TicketScan
{
	TicketID = ticket.TicketID,
	ScanDateTime = DateTime.UtcNow,
	GateName = gateName,
	Direction = isEntry ? ScanDirection.In : ScanDirection.Out,
	ScannedByUserId = currentUserId
};

dbContext.TicketScans.Add(scan);

// Step 5: Update ticket status
ticket.Status = isEntry ? "Entered" : "Exited";

await dbContext.SaveChangesAsync();

return new { Success = true, Message = "Entry granted", Direction = scan.Direction };
```

---

### USE CASE 10: تقييم المعرض والعارضين

**الممثلون:** الزائر

**السيناريو:**
```
1️⃣ بعد انتهاء الزيارة
   ├─ الزائر يتلقى بريد إلكتروني تقييم
   └─ أو يملأ استمارة في الموقع

2️⃣ تقييم المعرض العام
   ├─ اختيار النجوم: ⭐⭐⭐⭐ (4 من 5)
   ├─ تعليق: "Great exhibition, well organized!"
   └─ تاريخ: 2024-06-03 16:30:00

3️⃣ تقييم عارض محدد (اختياري)
   ├─ اختيار العارض: "TechCorp"
   ├─ النجوم: ⭐⭐⭐⭐⭐ (5 من 5)
   ├─ تعليق: "Excellent products and demo!"
   └─ تاريخ: 2024-06-03 16:35:00

4️⃣ إنشاء السجلات
   ┌─ VisitorRating (للمعرض):
   │  ├─ RatingID: 1
   │  ├─ VisitorID: 1
   │  ├─ ExhibitionID: 1
   │  ├─ ExhibitorID: null
   │  ├─ Score: 4
   │  ├─ Comment: "Great exhibition, well organized!"
   │  └─ RatedAt: 2024-06-03 16:30:00
   │
   └─ VisitorRating (للعارض):
	  ├─ RatingID: 2
	  ├─ VisitorID: 1
	  ├─ ExhibitionID: 1
	  ├─ ExhibitorID: 2
	  ├─ Score: 5
	  ├─ Comment: "Excellent products and demo!"
	  └─ RatedAt: 2024-06-03 16:35:00

5️⃣ Analytics Update
   └─ Exhibition Average Rating: 4.2/5 (updated)
   └─ Exhibitor Average Rating: 4.8/5 (updated)
   └─ Rating Count: 45 (updated)
```

**الكود:**
```csharp
var exhibitionRating = new VisitorRating
{
	VisitorID = visitorId,
	ExhibitionID = exhibitionId,
	ExhibitorID = null,
	Score = 4,
	Comment = "Great exhibition, well organized!",
	RatedAt = DateTime.UtcNow
};

var exhibitorRating = new VisitorRating
{
	VisitorID = visitorId,
	ExhibitionID = exhibitionId,
	ExhibitorID = exhibitorId,
	Score = 5,
	Comment = "Excellent products and demo!",
	RatedAt = DateTime.UtcNow
};

dbContext.VisitorRatings.AddRange(exhibitionRating, exhibitorRating);
await dbContext.SaveChangesAsync();
```

---

## 🔧 حالات استخدام الإدارة

### USE CASE 11: تقرير الحضور والإحصائيات

**الممثلون:** مدير المعرض (Exhibition Manager)

**السيناريو:**
```
بعد انتهاء المعرض:

1️⃣ تقارير الحضور
   ├─ إجمالي التذاكر المباعة: 5,250
   ├─ الحاضرين الفعليين: 4,950 (94%)
   ├─ عدم الحضور: 300 (6%)
   │
   └─ توزيع يومي:
	  ├─ Day 1 (June 1): 1,200
	  ├─ Day 2 (June 2): 1,850
	  └─ Day 3 (June 3): 1,900

2️⃣ تحليل الحضور
   ├─ Peak Hour: 12:00-14:00 (1,200 visitors)
   ├─ Average Stay: 4h 15m
   ├─ Exit Rate: 94%
   └─ Early Exit: 6%

3️⃣ تصنيف الزوار
   ├─ Regular Tickets: 4,000 (76%)
   ├─ Premium (VIP): 800 (15%)
   ├─ Student Discount: 450 (9%)
   │
   └─ بالجنسية:
	  ├─ Saudi: 3,500 (67%)
	  ├─ UAE: 800 (15%)
	  ├─ Other GCC: 450 (9%)
	  └─ International: 200 (4%)

4️⃣ تصنيف الآراء
   ├─ Average Rating: 4.3/5 ⭐
   ├─ Excellent (5): 60%
   ├─ Good (4): 25%
   ├─ Average (3): 10%
   ├─ Bad (2-1): 5%
   │
   └─ Top Comments:
	  ├─ "Best exhibition ever!"
	  ├─ "Well organized"
	  └─ "Great exhibitors"

5️⃣ Queries Used:
   ┌─ Total Visitors:
   │  SELECT COUNT(DISTINCT VisitorID)
   │  FROM Tickets
   │  WHERE ExhibitionID = 1
   │
   ├─ Daily Attendance:
   │  SELECT CAST(ScanDateTime AS DATE) as Date,
   │         COUNT(*) as Scans
   │  FROM TicketScans
   │  WHERE Direction = In
   │
   ├─ Peak Hour:
   │  SELECT HOUR(ScanDateTime) as Hour,
   │         COUNT(*) as Count
   │  FROM TicketScans
   │  GROUP BY HOUR(ScanDateTime)
   │  ORDER BY Count DESC
   │
   └─ Average Rating:
	  SELECT AVG(CAST(Score AS FLOAT)) as AvgRating
	  FROM VisitorRatings
	  WHERE ExhibitionID = 1
```

---

### USE CASE 12: تتبع الامتثال والتدقيق

**الممثلون:** مسؤول الامتثال (Compliance Officer)

**السيناريو:**
```
1️⃣ تحقيق من تغييرات الفاتورة
   └─ البحث في AuditLog:
	  ├─ TableName = "Invoice"
	  ├─ ActionAt >= 2024-05-01
	  └─ Action IN (CREATE, UPDATE, DELETE)

2️⃣ النتائج
   ├─ Invoice (ID: 1) created by user@exhibitor.com
   │  └─ TotalAmount: 6105.00
   │
   ├─ Invoice (ID: 1) updated by manager@exhibition.com
   │  └─ OldValues: Status = Open
   │  └─ NewValues: Status = Paid
   │
   └─ Payment added by accountant@company.com
	  └─ Amount: 6105.00

3️⃣ التتبع الكامل
   ├─ ✓ من قام بكل عملية (UserId)
   ├─ ✓ متى تم (ActionAt)
   ├─ ✓ من أين (IPAddress)
   ├─ ✓ ماذا تغيّر (OldValues → NewValues)
   └─ ✓ على أي سجل (RecordID)

4️⃣ الامتثال المالي
   ├─ جميع الفواتير مسجلة
   ├─ جميع الدفعات موثقة
   ├─ لا يمكن حذف الدفعات (DeleteBehavior.Restrict)
   └─ سجل كامل للتتبع

5️⃣ Query:
   SELECT * FROM AuditLog
   WHERE TenantID = 1
	 AND TableName = 'Invoice'
	 AND ActionAt BETWEEN '2024-05-01' AND '2024-06-03'
   ORDER BY ActionAt DESC
```

---

## ✅ الخلاصة

يغطي النظام جميع جوانب إدارة المعارض من البداية إلى النهاية:

| المرحلة | الحالات الاستخدام |
|--------|------------------|
| **التخطيط** | إنشاء معرض، تخطيط الأكشاك، إدارة الفئات |
| **الحجوزات** | حجز كشك، دمج أكشاك |
| **المالية** | معالجة الدفع، تقارير مالية |
| **الزيارات** | شراء تذاكر، مسح التذاكر، التقييمات |
| **الإدارة** | تقارير الحضور، التدقيق، الامتثال |

---

*تم التحليل: 2024* 📅

