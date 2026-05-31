# خريطة ذهنية شاملة لنظام إدارة المعارض
## Exhibition Management System - Architecture Mind Map

---

## 🎯 الرؤية الكلية للنظام

```
┌─────────────────────────────────────────────────────────────────┐
│         Exhibition Management System (.NET 10)                  │
│                   Multi-Tenant SaaS                             │
└─────────────────────────────────────────────────────────────────┘
			  │
			  ├─────────────┬──────────────┬──────────────┐
			  │             │              │              │
		 ┌────▼───┐  ┌─────▼──┐  ┌──────▼────┐  ┌──────▼─────┐
		 │ Tenant │  │  Users │  │  Venues   │  │ Financial  │
		 │ Mgmt   │  │ & Roles│  │ & Booths  │  │ System     │
		 └────────┘  └────────┘  └───────────┘  └────────────┘
```

---

## 📊 الهيكل الهرمي الكامل

```
EXHIBITION MANAGEMENT SYSTEM
│
├── 🏢 TENANT MANAGEMENT (العزل التام)
│   ├── Tenant (الشركة/العميل)
│   ├── TenantSubscription (الاشتراك والخطة)
│   ├── ApplicationUser (المستخدمون)
│   ├── ApplicationRole (الأدوار)
│   └── AuditLog (التدقيق الشامل)
│
├── 🌐 MASTER DATA (البيانات الأساسية)
│   ├── Currency (العملات المدعومة)
│   ├── ExchangeRate (أسعار الصرف)
│   └── [Shared across all tenants]
│
├── 🏛️ VENUE MANAGEMENT (إدارة الأماكن)
│   ├── Venue (الموقع الجغرافي)
│   │   └── Hall (القاعات)
│   │       └── Booth (الأكشاك)
│   │           ├── BoothStatus (Available/Reserved/Merged/Maintenance)
│   │           ├── BoothMerge (دمج الأكشاك)
│   │           │   └── BoothMergeItem (عناصر الدمج)
│   │           └── Position & Dimensions (الموقع والأبعاد)
│   │
│   └── Exhibition (المعرض)
│       ├── ExhibitionStatus (Planning/Open/Closed/Cancelled)
│       ├── ExhibitionSchedule (الجدول الزمني)
│       │   └── ScheduleRegistration (التسجيلات)
│       ├── Exhibitor (العارضون)
│       └── BoothReservation (الحجوزات)
│
├── 🎪 BOOKING SYSTEM (نظام الحجوزات)
│   ├── BoothReservation (حجز الكشك)
│   │   ├── ReservationStatus (Pending/Confirmed/Cancelled)
│   │   ├── ReservationService (الخدمات المحجوزة)
│   │   ├── BoothStaff (موظفو الكشك)
│   │   └── LogisticNotes (ملاحظات لوجستية)
│   │
│   └── Booth Selection Logic:
│       ├── Available Booths
│       ├── Match Requirements
│       ├── Calculate Pricing
│       └── Confirm & Reserve
│
├── 💰 FINANCIAL SYSTEM (النظام المالي)
│   ├── Pricing Models:
│   │   ├── PricingPackage (حزم التسعير)
│   │   ├── Service (الخدمات الفردية)
│   │   ├── BoothPriceRule (قواعل تسعير الأكشاك)
│   │   └── ServicePriceRule (قواعد تسعير الخدمات)
│   │
│   ├── PackageService (الخدمات في الحزم)
│   │
│   ├── Invoice Management:
│   │   ├── Invoice (الفاتورة)
│   │   │   ├── SubTotal + TaxRate + TaxAmount = TotalAmount
│   │   │   └── InvoiceStatus (فترة دوران متقدمة)
│   │   │
│   │   └── Payment Processing:
│   │       ├── Payment (تسجيل الدفع)
│   │       ├── PaymentMethod (طرق متعددة)
│   │       └── PaymentStatus (Completed/Pending/Failed/Refunded)
│   │
│   └── Multi-Currency Support:
│       ├── BoothReservation.CurrencyCode
│       ├── BoothReservation.ExchangeRateUsed
│       ├── BoothReservation.AmountInBaseCurrency
│       └── Precise: decimal(18,6)
│
├── 🎫 VISITOR & TICKET SYSTEM (نظام الزوار والتذاكر)
│   ├── Visitor (الزائر)
│   │   ├── FullName, Contact Info
│   │   ├── Nationality, Type
│   │   └── RegisteredAt (تاريخ التسجيل)
│   │
│   ├── Ticket (التذكرة)
│   │   ├── TicketType (أنواع مختلفة)
│   │   ├── QRCode (رمز فريد - قيد Unique)
│   │   ├── Price & Currency
│   │   ├── ValidDate (صلاحية)
│   │   ├── Status
│   │   └── IssuedAt
│   │
│   ├── TicketScan (مسح التذكرة)
│   │   ├── ScanDateTime (الوقت الدقيق)
│   │   ├── GateName (اسم البوابة)
│   │   ├── Direction (In/Out)
│   │   └── ScannedByUser (من قام به)
│   │
│   └── VisitorRating (التقييمات)
│       ├── Score (1-5) - Check Constraint
│       ├── Comment
│       ├── RatedAt
│       └── قد يكون للمعرض أو العارض
│
├── 📈 REPORTING & ANALYTICS
│   ├── FinancialReport (التقارير المالية)
│   ├── Derived from:
│   │   ├── Invoice (الفواتير)
│   │   ├── Payment (الدفعات)
│   │   ├── BoothReservation (الحجوزات)
│   │   └── ExchangeRate (أسعار الصرف)
│   │
│   └── Visitor Analytics:
│       ├── Total Visitors (من Ticket)
│       ├── Daily Count (من TicketScan)
│       ├── Peak Hours
│       ├── Ratings & Comments
│       └── Demographics
│
└── 🔐 SECURITY & AUDIT
	├── AuditLog (تسجيل شامل)
	│   ├── TableName + RecordID
	│   ├── Action (Create/Update/Delete)
	│   ├── OldValues → NewValues
	│   ├── ActionAt + IPAddress
	│   └── UserId (من قام)
	│
	├── Multi-Tenant Isolation:
	│   ├── TenantID في كل جدول
	│   ├── ApplicationRole.TenantID
	│   └── عزل كامل للبيانات
	│
	├── Delete Behavior (DeleteBehavior.Restrict):
	│   ├── منع الحذف المتسلسل غير المقصود
	│   ├── حماية البيانات المالية
	│   ├── منع النتائج اليتيمة
	│   └── سلامة العلاقات
	│
	└── Authentication:
		├── ApplicationUser (من Identity)
		├── JWT Bearer Tokens
		├── Azure AD Integration
		└── Role-Based Authorization

```

---

## 🔄 تدفق البيانات الرئيسي

```
┌─────────────────┐
│   New Booking   │
└────────┬────────┘
		 │
		 ▼
┌────────────────────────────┐
│  Exhibitor Application     │
├────────────────────────────┤
│ - Choose Exhibition        │
│ - Select Booth Type        │
│ - Enter Required Area      │
└────────┬───────────────────┘
		 │
		 ▼
┌────────────────────────────┐
│  Pricing Engine            │
├────────────────────────────┤
│ - BoothPriceRule × Factor  │
│ - ServicePriceRule × Rules │
│ - Discount Logic           │
│ - Tax Calculation          │
└────────┬───────────────────┘
		 │
		 ▼
┌────────────────────────────┐
│  Currency Conversion       │
├────────────────────────────┤
│ - Load ExchangeRate        │
│ - Store in Reservation     │
│ - Calculate Base Currency  │
└────────┬───────────────────┘
		 │
		 ▼
┌────────────────────────────┐
│  Create Records            │
├────────────────────────────┤
│ 1. BoothReservation        │
│ 2. Invoice (from Booking)  │
│ 3. Booth.Status = Reserved │
│ 4. AuditLog (record)       │
└────────┬───────────────────┘
		 │
		 ▼
┌────────────────────────────┐
│  Payment Processing        │
├────────────────────────────┤
│ - Send Invoice             │
│ - Track Payment Methods    │
│ - Record Payment           │
│ - Update Invoice.Status    │
│ - Update Reservation.Status│
└────────┬───────────────────┘
		 │
		 ▼
┌────────────────────────────┐
│  Event Confirmation        │
├────────────────────────────┤
│ - Generate QR Tickets      │
│ - Create Badge             │
│ - Send Confirmations       │
│ - Event Starts             │
└────────┬───────────────────┘
		 │
		 ▼
┌────────────────────────────┐
│  Visitor Management        │
├────────────────────────────┤
│ 1. Visitor Registration    │
│ 2. Ticket Creation         │
│ 3. QR Code Scanning        │
│ 4. TicketScan Records      │
│ 5. Entry/Exit Tracking     │
└────────┬───────────────────┘
		 │
		 ▼
┌────────────────────────────┐
│  Post-Event                │
├────────────────────────────┤
│ - Visitor Ratings          │
│ - Generate Reports         │
│ - Financial Summary        │
│ - Analytics                │
└────────────────────────────┘
```

---

## 💾 نموذج البيانات العلائقي

```
╔═══════════════════════════════════════════════════════════════════════╗
║                         TENANT (نقطة المركز)                         ║
╠═══════════════════════════════════════════════════════════════════════╣
║ TenantID (PK) | CompanyName | Subdomain (U) | Plan | BaseCurrency (FK)║
╚═══════════════════════════════════════════════════════════════════════╝
		 │
	┌────┼────┬──────────┬───────────┬──────────┬──────────┐
	│    │    │          │           │          │          │
	▼    ▼    ▼          ▼           ▼          ▼          ▼
   USER APP VENUE    EXHIBITION   EXHIBITOR  SERVICES  AUDIT-LOG
   (Many)(Role) │       │            │          │         │
		  │     │       │            │          │         │
		  │     └───┬──────┬────┬─────┴──┬───────┴─────┐    │
		  │         │      │    │        │             │    │
		  │         ▼      ▼    ▼        ▼             ▼    │
		  │        HALL  SCHEDULE  BOOTH-RES    PRICING-PKG │
		  │         │    REGISTR      │             │       │
		  │         │                 ▼             ▼       │
		  │         └─────┬──────► BOOTH         PACKAGE-SRV
		  │               │        MERGE            │
		  │               ▼         │               │
		  │            BOOTH      MERGE-ITEM     (Services)
		  │         │    │    │
		  │         ▼    ▼    ▼
		  │        POS  SHAPE STAFF
		  │        DIM  JSON
		  │
		  ▼
	  ┌───────────────────────────┐
	  │    RESERVATION LIFECYCLE  │
	  └───────────────────────────┘
			   │
			   ▼
		  INVOICE (from Reservation)
			   │
		  ┌────┴─────┐
		  ▼           ▼
		PAYMENT   SERVICES-LINKED
			   │
			   ▼
		  EXCHANGE-RATE (for currency)
			   │
			   ▼
		  ┌─────────────┐
		  │  TICKET     │
		  │   (Visitor) │
		  └──────┬──────┘
				 │
				 ▼
			TICKET-SCAN
		 (Entry/Exit tracking)
				 │
				 ▼
			VISITOR-RATING
		 (Post-event feedback)
```

---

## 📊 مثال على تدفق حجز كامل

```
┌─ EXHIBITOR Registration ─┐
│  ├─ Create ApplicationUser
│  ├─ Create Exhibitor (Company)
│  └─ Create ApplicationRole (Exhibitor role)
└────────────────────────────────┤
								  │
								  ▼
					┌─ Browse Exhibition ─┐
					│ ├─ View Exhibitions │
					│ ├─ Filter by Status │
					│ └─ Select Exhibition│
					└────────────────────────┤
											│
											▼
						  ┌─ Request Booth ─┐
						  │ ├─ Choose Type   │
						  │ ├─ Request Area  │
						  │ └─ Select Services
						  └────────────────────┤
											  │
											  ▼
					┌──────────────────────────────────┐
					│  SYSTEM CALCULATION ENGINE       │
					├──────────────────────────────────┤
					│ 1. Load BoothPriceRule          │
					│ 2. Calculate: Base × Factor      │
					│ 3. Load ServicePriceRule        │
					│ 4. Sum Services                 │
					│ 5. Calculate Subtotal           │
					│ 6. Apply BoothTax               │
					│ 7. Calculate Total              │
					│ 8. Load ExchangeRate            │
					│ 9. Convert to Base Currency     │
					│ 10. Final Amount in Base        │
					└──────────┬───────────────────────┘
							   │
							   ▼
					┌──────────────────────────────────┐
					│  CREATE RECORDS                  │
					├──────────────────────────────────┤
					│ ✓ BoothReservation               │
					│   └─ Status: Pending            │
					│   └─ Store ExchangeRate          │
					│   └─ Store AmountInBase          │
					│                                  │
					│ ✓ Invoice                        │
					│   └─ Status: Open               │
					│   └─ DueDate: +30 days          │
					│   └─ TaxAmount calculated       │
					│                                  │
					│ ✓ Find Available Booth           │
					│   └─ Match Area + Type           │
					│   └─ Reserve Booth               │
					│   └─ Booth.Status: Reserved     │
					│                                  │
					│ ✓ AuditLog                       │
					│   └─ Action: CREATE              │
					│   └─ User: ExhibitorUser        │
					│   └─ Timestamp + IP              │
					└──────────┬───────────────────────┘
							   │
							   ▼
					┌──────────────────────────────────┐
					│  SEND INVOICE & WAIT FOR PAYMENT │
					└──────────────────────────────────┘
							   │
						┌──────┴──────┐
						▼             ▼
				 [PAYMENT]      [HOLD/CANCEL]
						│             │
						│             ▼
						│    ┌─────────────────────┐
						│    │ CANCEL RESERVATION  │
						│    ├─────────────────────┤
						│    │ • Booth.Status: Avl │
						│    │ • Invoice: Cancelled│
						│    │ • Audit Log Entry   │
						│    └─────────────────────┘
						│
						▼
			┌──────────────────────────────┐
			│  PAYMENT PROCESSING          │
			├──────────────────────────────┤
			│ • Receive Payment             │
			│ • Verify Amount               │
			│ • Check Currency              │
			│ • Record Payment in DB        │
			│ • Update Invoice.Status: Paid │
			│ • Update Reservation: Confirmed
			│ • Generate Badge              │
			│ • Create Tickets (if needed)  │
			│ • Send Confirmation           │
			│ • Audit Log Entry             │
			└──────────────────────────────┘
						│
						▼
			┌──────────────────────────────┐
			│  EVENT DAY                    │
			├──────────────────────────────┤
			│ • Visitors Arrive             │
			│ • Show Tickets                │
			│ • Scan QR Code                │
			│ • Record TicketScan (In)      │
			│ • Allow Entry                 │
			│                               │
			│ [VISIT EXHIBITION]            │
			│                               │
			│ • Ready to Leave              │
			│ • Scan QR Code Again          │
			│ • Record TicketScan (Out)     │
			│ • Track Exit                  │
			└──────────────────────────────┘
						│
						▼
			┌──────────────────────────────┐
			│  POST-EVENT                   │
			├──────────────────────────────┤
			│ • Leave Rating (1-5)          │
			│ • Write Comment               │
			│ • Rate Exhibitor (optional)   │
			│ • Generate Analytics          │
			│ • Financial Report            │
			│ • Visitor Statistics          │
			│ • Attendance Summary          │
			│ • Revenue Analysis            │
			└──────────────────────────────┘
```

---

## 🔒 طبقات الأمان

```
┌──────────────────────────────────────────────────────────────┐
│              AUTHENTICATION & AUTHORIZATION                   │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  Layer 1: Azure AD                                           │
│  ├─ OAuth 2.0 / OIDC                                         │
│  ├─ MFA Support                                              │
│  └─ Enterprise SSO                                           │
│                         │                                    │
│  Layer 2: JWT Bearer    │                                    │
│  ├─ Signed Tokens       │                                    │
│  ├─ Expiration          │                                    │
│  └─ Refresh Logic       │                                    │
│                         ▼                                    │
│  Layer 3: ApplicationUser                                    │
│  ├─ TenantID (Multi-Tenant Isolation)                        │
│  ├─ Roles (Fine-grained)                                     │
│  └─ Permissions (Policy-based)                               │
│                                                               │
└──────────────────────────────────────────────────────────────┘
						 │
						 ▼
┌──────────────────────────────────────────────────────────────┐
│              DATA ACCESS LAYER PROTECTION                     │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  1. TenantID Filtering                                       │
│     └─ Every query checks: where TenantID == CurrentUser.TenantID
│                                                               │
│  2. Foreign Key Constraints                                  │
│     └─ Prevent orphaned records                              │
│                                                               │
│  3. DeleteBehavior.Restrict                                  │
│     └─ Prevent cascading deletes that break data integrity   │
│                                                               │
│  4. Check Constraints                                        │
│     └─ Score between 1-5 enforced at DB level                │
│                                                               │
│  5. Unique Constraints                                       │
│     └─ QRCode, Subdomain, etc. are unique per-table          │
│                                                               │
└──────────────────────────────────────────────────────────────┘
						 │
						 ▼
┌──────────────────────────────────────────────────────────────┐
│              AUDIT & COMPLIANCE LAYER                         │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  AuditLog for Every Material Change:                         │
│  ├─ Who: UserId (FK to ApplicationUser)                      │
│  ├─ What: TableName + RecordID                               │
│  ├─ When: ActionAt (DateTime.UtcNow)                         │
│  ├─ Where: IPAddress                                         │
│  ├─ Action: CREATE, UPDATE, DELETE                           │
│  └─ Details: OldValues → NewValues (JSON)                    │
│                                                               │
│  Financial Records Immutable:                                │
│  ├─ Payment: Cannot Delete (Restrict)                        │
│  ├─ Invoice: Cannot Delete (Restrict)                        │
│  └─ Reservation: Cannot Delete (Restrict)                    │
│                                                               │
└──────────────────────────────────────────────────────────────┘
```

---

## ⚡ مؤشرات الأداء

```
DATABASE INDEXES FOR PERFORMANCE:

┌─────────────────────────────────────────────────────────┐
│           COMPOSITE INDEXES (للاستعلامات الشائعة)      │
├─────────────────────────────────────────────────────────┤
│ Exhibition (TenantID, Status)                           │
│ ├─ البحث عن معارض بحالة معينة                         │
│ └─ أداء: O(log n) بدلاً من O(n)                        │
│                                                         │
│ BoothReservation (ExhibitionID, Status)                 │
│ ├─ تحديد حالة الحجوزات لكل معرض                      │
│ └─ أداء محسّنة: ✓                                      │
│                                                         │
│ Booth (HallID, Status)                                  │
│ ├─ إيجاد أكشاك متوفرة في قاعة                        │
│ └─ تخطيط سريع الأكشاك                                  │
│                                                         │
│ Ticket (ExhibitionID, Status)                           │
│ ├─ إحصائيات الحضور                                    │
│ └─ تقارير فورية                                        │
│                                                         │
│ AuditLog (TenantID, ActionAt)                           │
│ ├─ تتبع الأنشطة حسب التاريخ                          │
│ └─ تقارير الامتثال سريعة                               │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│         UNIQUE INDEXES (لمنع التكرار والتتبع)        │
├─────────────────────────────────────────────────────────┤
│ Tenant.Subdomain (UNIQUE)                               │
│ └─ كل عميل له Subdomain فريد                           │
│                                                         │
│ Ticket.QRCode (UNIQUE)                                  │
│ └─ كل تذكرة لها QR Code فريد (منع التزوير)            │
│                                                         │
│ ExchangeRate (FromCurrency, ToCurrency, RateDate) (U)  │
│ └─ سعر صرف واحد فقط لكل زوج عملات يومياً              │
│                                                         │
│ Invoice (TenantID, InvoiceNumber) (U)                   │
│ └─ رقم فاتورة فريد لكل مستأجر                         │
│                                                         │
│ ScheduleRegistration (ScheduleID, VisitorID) (U)       │
│ └─ تسجيل واحد فقط لكل زائر في كل جدول               │
└─────────────────────────────────────────────────────────┘
```

---

## 🎯 الملخص النهائي

```
╔═══════════════════════════════════════════════════════════════╗
║          EXHIBITION MANAGEMENT SYSTEM - CAPABILITIES          ║
╠═══════════════════════════════════════════════════════════════╣
║                                                               ║
║  📊 45+ Entity Models                                        ║
║  🔐 Multi-Tenant Architecture                                ║
║  💼 Enterprise-Grade Security                                ║
║  💰 Advanced Financial Management                            ║
║  🌍 Multi-Currency Support                                   ║
║  🎫 Complete Ticketing System                                ║
║  📈 Real-time Analytics                                      ║
║  🔄 Comprehensive Audit Trail                                ║
║  ⚡ Optimized Database Performance                            ║
║  🛡️ Data Integrity & Compliance                              ║
║                                                               ║
║  ✅ Production-Ready (.NET 10)                                ║
║  ✅ Scalable Architecture                                     ║
║  ✅ Cloud-Native Design                                       ║
║  ✅ Future-Proof Extensibility                                ║
║                                                               ║
╚═══════════════════════════════════════════════════════════════╝
```

---

*Last Updated: 2024* 📅

