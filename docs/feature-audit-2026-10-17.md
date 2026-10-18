# مراجعة النظام حسب الميزات — من النماذج إلى الويب

> **تحديث بعد الإصلاح:** عولجت الملاحظات F01–F19 واجتاز الخادم 283 اختبارًا والويب 78 اختبارًا. راجع [تقرير الإصلاحات والتحقق](feature-repairs-2026-10-17.md). يسجل متن المراجعة أدناه حالة النظام قبل هذه الإصلاحات، وتبقى تجاربه التشخيصية أدلة تاريخية.

التاريخ: 17 أكتوبر 2026. النطاق: الحالة الحالية لمساحة العمل، بما فيها التعديلات الموجودة قبل المراجعة. استُبعد مشروع سطح المكتب من المراجعة والبناء، واستُبعد اختبار مساعده من تشغيل الاختبارات.

**النتيجة: 19 ملاحظة قائمة: 5 عالية الأولوية P1 و14 متوسطة P2. أُعيد إنتاج 16 منها بالتنفيذ، وثلاث بمراجعة المصدر.** لم تُنفذ إصلاحات لكود التطبيق. أُضيف هذا التقرير ومصادر التجارب وأدلتها فقط. التقارير القديمة تسجل حالات سابقة؛ لم تُحتسب المشكلات التي أصلحتها التعديلات الحالية.

## تقسيم الميزات ومسار التحقق

لكل ميزة جرى تتبع النماذج وقيود التخزين، ثم المستودعات والخدمات وDTO/Mapping، ثم نقاط API والصلاحيات، ثم استدعاءات الويب وعرض الاستجابة. هذا فحص موجه للمسارات المهمة والحالات الحدية، وليس ضمانًا بفحص كل تركيب ممكن للمدخلات.

| # | الميزة | النماذج والمستودعات | الخدمات وAPI | الويب ونتيجة التحقق |
|---|---|---|---|---|
| 1 | التسجيل والدخول والجلسات وكلمات المرور | ApplicationUser، RefreshSession، Identity | AuthService، AccessTokenSessionValidator، Auth/Profile | Login، Registration، PasswordRecovery، auth.storage، api: **F01**. اختبارات تعطيل الحساب وإبطال الجلسات السابقة ناجحة. |
| 2 | المستخدمون والأدوار | ApplicationUser/ApplicationRole | Users/Roles، TenantAdministration | UsersPage: إنشاء وتفعيل وحذف المستخدمين؛ توجد فجوة إدارة أدوار الحسابات القائمة موضحة أدناه. |
| 3 | المؤسسات والاشتراكات والدعوات | Tenant، TenantSubscription، TenantInvitation | TenantService، AdminService، Tenants | TenantsPage: **F13**؛ إنشاء المؤسسات وإعادة إرسال الدعوات غير موصولين بالصفحة. |
| 4 | المواقع والقاعات | Venue، Hall | VenueService، HallService، Venues/Halls | VenuesPage، VenueDesigner: **F02 وF11**. |
| 5 | تصميم الموقع ومخططات الأجنحة | SitePlanJSON، SitePlanVersion، بيانات هندسة Hall/Booth | حفظ الموقع وتخطيط الأجنحة، تحقق الحدود والتعارض | VenueDesigner، BoothDesigner؛ اختبارات الهندسة والاستيراد والتراجع والتزامن السابقة ناجحة ضمن نطاقها. لا ملاحظة إضافية مؤكدة في هذه الجولة. |
| 6 | المعارض ودورة حياتها | Exhibition وعلاقاته وتواريخه | ExhibitionService، ExhibitionsController | ExhibitionsPage: **F19**؛ اختبارات حمايات الحذف والتواريخ السابقة ناجحة. |
| 7 | الفعاليات والجدول الزمني | ExhibitionSchedule، Hall | مستودع الجدول، Add/Update/RemoveSchedule | EventsPage: **F10 وF12**. |
| 8 | الشركات العارضة | Exhibitor وPreferredCurrency | ExhibitorService، DTO/Mapping، Exhibitors | CompaniesPage: **F10**؛ حمايات حذف عارض ذي فواتير أو حجوزات ناجحة. |
| 9 | الأجنحة والدمج والتوفر | Booth، BoothMerge، BoothMergeItem | BoothService، BoothsController | BoothsPage والمصمم؛ اختبارات الأسعار والتوفر والحجز حسب المعرض ناجحة ضمن نطاقها، مع ملاحظة التسعير **F09**. |
| 10 | الحجوزات وخدماتها والاعتماد والإلغاء | BoothReservation، ReservationService، Invoice | ReservationService ومعاملات الإنشاء | ReservationsPage: **F03 وF06**؛ اختبارات الإنشاء الذري والخدمات وحماية الفاتورة الصادرة ناجحة. |
| 11 | الخدمات والتسعير والعملات | Service، BoothPriceRule، ServicePriceRule، PricingPackage، ExchangeRate | PricingService، ServiceManagementService، CurrencyService | ServicesPage، حاسبة الأسعار، ExchangeRatesPanel: **F09**؛ بعض مسارات الإدارة لا تصل إلى الويب. |
| 12 | الفواتير والدفعات والاسترداد | Invoice، InvoiceItem، Payment، CreditNote | FinancialService، InvoiceItemService، FinancialController | InvoicesPage: **F06** وفجوة إنشاء الاسترداد من الويب. اختبارات الأرصدة وتعدد العملات ودقة البنود السابقة ناجحة. |
| 13 | الرعاة والباقات والعقود والمساحات | Sponsor، SponsorshipPackage، SponsorshipContract، AdvertisingSpace، SponsorAdAssignment | SponsorshipService ومستودعاته وController | SponsorshipPage: **F04 وF05 وF07 وF08**؛ إدارة العقود والمساحات في الويب غير مكتملة. |
| 14 | الزوار والتذاكر والحضور | Visitor، Ticket، TicketScan | VisitorService، TicketService، TicketScanPolicy | TicketsPage وCheckInKioskPage: **F16 وF17 وF18**. اختبارات منع إعادة الدخول والتعارض السابقة ناجحة. |
| 15 | الشارات وتصاريح الطاقم | BadgeTemplate، BoothStaff، Ticket | BadgeService، BadgesController | BadgeDesigner، معاينة التذكرة: **F16 وF18**؛ توجد فجوة إدارة الطاقم من الويب. |
| 16 | التقارير ولوحة التحكم والتدقيق | FinancialReport، Expense، AuditLog | ReportService، DashboardController، AdminService | Dashboard، Analytics، AdminAudit: **F14 وF15** وفجوات المصروفات والتقارير المحفوظة. اختبارات العملات والحضور السابقة ناجحة. |

## الملاحظات العالية P1

### F01 — رد تجديد جلسة قديمة يمكنه استبدال رموز حساب آخر

إذا بدأ طلب تجديد لحساب A ثم سُجّل الدخول بحساب B قبل وصول الرد، تستدعي الواجهة `updateSessionTokens` على الجلسة الموجودة حين وصول الرد. لا تتحقق من أن الجلسة الحالية هي التي بدأت طلب التجديد. أثبتت محاكاة تنفيذ وحدتي التخزين وعميل API الأصليتين بقاء `userId=account-B` و`tenantID=2` مع استبدال رمزيهما برمزي A، وإعادة الطلب بتفويض A.

هذا خلط لهوية الجلسة المعروضة والهوية المستخدمة في الطلبات، وليس إثباتًا لتجاوز عزل المؤسسات في الخادم. كما أن فشل تجديد قديم يستدعي `expireSession` دون التحقق من هوية الجلسة الحالية.

المواقع: [api.ts:112](../exhibitionmanagementsystem.web/src/services/api.ts:112)، [اعتماد الرد:133](../exhibitionmanagementsystem.web/src/services/api.ts:133)، [auth.storage.ts:34](../exhibitionmanagementsystem.web/src/features/auth/services/auth.storage.ts:34).

المعالجة: ربط التجديد بمعرّف أو إصدار الجلسة التي بدأته، ومقارنة الرمز/الجلسة قبل تحديث التخزين أو إفراغه أو إعادة الطلبات المنتظرة. إلغاء نتائج التجديد القديمة عند تغيير الحساب.

### F02 — حذف موقع وإنشاء قاعة فيه بالتزامن ينجحان معًا

على SQL، أُوقف الحذف بعد فحص عدم وجود التبعيات، ثم أُنشئت قاعة من الخدمة تحت الموقع نفسه، ثم استُكمل الحذف. نجحت العمليتان، وأصبح الموقع محذوفًا والقاعة محفوظة. إعادة قراءة القاعة عبر الخدمة أعادت `HALL_NOT_FOUND` لأن تضمين الموقع يستبعد الموقع المحذوف.

الدليل: `deleteAccepted=true`, `hallCreateAccepted=true`, `venueDeleted=true`, `hallStored=true`, `hallVisible=false`. لا تشمل حماية حذف المعرض والقاعة السابقة حذف الموقع.

المواقع: [VenueService.DeleteAsync:310](../ExhibitionManagementSystem.Services/Implementations/VenueService.cs:310)، [HallService.CreateAsync:51](../ExhibitionManagementSystem.Services/Implementations/HallService.cs:51).

المعالجة: حماية فحص التبعيات والحذف بمعاملة مناسبة، مع مشاركة مسارات إنشاء القاعات والمعارض في بروتوكول القفل/التزامن نفسه. اختبار أن عملية واحدة فقط تنجح وأن السجل التابع يبقى قابلًا للقراءة.

### F03 — إنشاء الحجز من الويب يفشل عند ترك الملاحظات فارغة

تحوّل الواجهة الحقل الفارغ إلى `null`، بينما `LogisticNotes` في DTO غير قابل لـnull؛ يضيف MVC تحققًا إلزاميًا ضمنيًا. بإرسال طلب الواجهة نفسه أعاد API **400** مع `The LogisticNotes field is required.`. الطلب نفسه مع `logisticNotes: ""` نجح على SQL وأعاد **201**.

المواقع: [ReservationsPage.tsx:289](../exhibitionmanagementsystem.web/src/features/reservations/pages/ReservationsPage.tsx:289)، [BoothReservationCreateDto.cs:33](../ExhibitionManagementSystem.Models.DTOs/Reservation/BoothReservationCreateDto.cs:33).

المعالجة: توحيد معنى الحقل الاختياري؛ إرسال نص فارغ أو جعل DTO يقبل null مع تطبيعها في الخدمة. إضافة اختبار HTTP بطلب الواجهة الافتراضي.

### F04 — وجود عقد رعاية يكسر عرض تبويب العقود

تتوقع الواجهة `sponsorName`, `contractAmount`, `signedAt` وحالة نصية. يعيد الخادم `sponsorCompanyName`, `totalAmount`, `signedDate` و`status` عددية، مع `statusName` النصي. مرّرت استجابة HTTP فعلية إلى مكوّن الصفحة الأصلي: فشل العرض عند `contractAmount.toLocaleString()`. وبعد تكييف أسماء الحقول الثلاثة فقط فشل عند `c.status.toLowerCase()`.

المواقع: [SponsorshipPage.tsx:63](../exhibitionmanagementsystem.web/src/features/sponsorship/pages/SponsorshipPage.tsx:63)، [العرض:552](../exhibitionmanagementsystem.web/src/features/sponsorship/pages/SponsorshipPage.tsx:552)، [DTO الخادم](../ExhibitionManagementSystem.Models.DTOs/Sponsorship/SponsorshipContractDto.cs:8).

المعالجة: مطابقة عقد الاستجابة واستخدام `statusName` أو تحويل محكوم، مع اختبار عرض عقد واحد فعليًا. نجاح TypeScript لا يتحقق من JSON الواصل وقت التشغيل.

### F05 — إنشاء باقة رعاية من الويب يُرفض دائمًا بسبب صيغة المستوى

ترسل الصفحة `level: "Gold"` أو اسم المستوى المختار. DTO يستخدم enum عددية، ولم يُضبط محول لقبول الأسماء النصية. طلب الواجهة أعاد **400** بسبب `$.level`، والطلب نفسه مع `level: 2` أعاد **200**.

المواقع: [SponsorshipPage.tsx:259](../exhibitionmanagementsystem.web/src/features/sponsorship/pages/SponsorshipPage.tsx:259)، [SponsorshipPackageCreateDto](../ExhibitionManagementSystem.Models.DTOs/Sponsorship/SponsorshipPackageDto.cs:26).

المعالجة: اتفاق صريح على تمثيل المستوى في الطرفين، مع مراعاة أن صفحات أخرى، مثل مصمم الشارات، تعتمد حاليًا على قيم enum العددية.

## الملاحظات المتوسطة P2

### F06 — الشرطة الأخيرة في مسار الدفعات تتجاوز حماية تكرار الطلب

يقارن middleware المسار كاملًا دون تطبيع، بينما يقبل MVC المسار بشرطة أخيرة. في تجربة HTTP مع middleware الحقيقي، طلب `/api/financial/payments` دون مفتاح أعاد **400**. تكرار الطلب مرتين على `/api/financial/payments/` دون المفتاح أعاد **200 و200** وسجّل دفعتين. يشترك مسار الحجوزات في المطابقة نفسها؛ التجربة التنفيذية كانت على الدفعات.

الموقع: [IdempotencyMiddleware.cs:138](../ExhibitionManagementSystem/Middleware/IdempotencyMiddleware.cs:138).

المعالجة: ربط الحماية ببيانات endpoint أو توحيد المسار قبل المقارنة؛ يجب أن يخضع المساران للمفتاح نفسه. هذا التجاوز لا يلغي فحص رصيد الفاتورة، لكنه يلغي ضمان منع تنفيذ الأمر المكرر.

### F07 — حذف مساحة إعلانية مخصصة يخفي التخصيص من العقد

قُبل حذف مساحة مرتبطة بعقد قائم. بعد تفريغ التتبع وإعادة القراءة على SQL، انخفضت تخصيصات العقد من **1 إلى 0**، مع بقاء سجل التخصيص في قاعدة البيانات. لا يتحقق مسار الحذف من التخصيصات، وتُسقطها قراءة العلاقة المطلوبة مع المساحة المحذوفة.

الموقع: [SponsorshipService.DeleteSpaceAsync:324](../ExhibitionManagementSystem.Services/Implementations/SponsorshipService.cs:324).

المعالجة: منع حذف المساحة ذات التخصيص أو حفظ تاريخ قابل للقراءة من العقد، مع حماية التحقق من التزامن. لا يكفي بقاء الصف الخام في SQL.

### F08 — حذف باقة مرتبطة بعقد يمحو اسم الباقة من قراءة العقد

نجح حذف باقة مشتراة مرتبطة بعقد قائم؛ تغير `PackageName` من `Purchased Gold` إلى `null` بعد إعادة القراءة. يبقى العقد، لكنه يفقد تفاصيل الباقة المعروضة.

الموقع: [SponsorshipService.DeletePackageAsync:209](../ExhibitionManagementSystem.Services/Implementations/SponsorshipService.cs:209).

المعالجة: منع حذف باقة لها عقود، أو الاحتفاظ بلقطة شروطها وتسميتها داخل العقد مع استراتيجية قراءة تاريخية واضحة.

### F09 — أسعار الخدمات والتسعير تقبل دقة/حجمًا لا يدعمهما التخزين

نجح إنشاء قاعدة سعر متر وخدمة افتراضية بقيمة `0.015`. أعادت الخدمتان **0.015**، بينما حفظ SQL **0.02** في العمودين `decimal(18,2)`. كذلك مرّت `decimal.MaxValue` لسعر الخدمة من التحقق ثم رمت الخدمة `DbUpdateException` دون نتيجة خطأ محكومة. لا تشمل حمايات الدقة السابقة لبنود الفواتير هذه المسارات.

المواقع: [PricingService.cs:300](../ExhibitionManagementSystem.Services/Implementations/PricingService.cs:300)، [ServiceManagementService.cs:47](../ExhibitionManagementSystem.Services/Implementations/ServiceManagementService.cs:47)، [BoothPriceRule](../ExhibitionManagementSystem.Models/BoothPriceRule.cs)، [Service](../ExhibitionManagementSystem.Models/Service.cs).

المعالجة: التحقق من عدد المنازل والحد الأقصى وفق العمود، أو التطبيع قبل الحساب والحفظ وبناء الاستجابة؛ تطبيق القاعدة في الإنشاء والتعديل. الإثبات هنا لسعر المتر والسعر الافتراضي للخدمة تحديدًا.

### F10 — قيم enum غير صالحة تصل إلى Mapping وتسبب HTTP 500

أعاد طلب إنشاء عارض مع `exhibitorCategory: "Invalid"` **500** بدل خطأ تحقق. وأعاد إنشاء فعالية مع `eventType: " "` **500**: يتجاوز النص الأبيض فحص الخدمة باستخدام `IsNullOrWhiteSpace`، ثم يحاول Mapping تفسيره باستخدام `Enum.Parse` بعد فحص مختلف هو `IsNullOrEmpty`.

المواقع: [ExhibitorMappingProfile.cs:22](../ExhibitionManagementSystem.Models.DTOs/Mapping/Profiles/ExhibitorMappingProfile.cs:22)، [ExhibitionService.cs:289](../ExhibitionManagementSystem.Services/Implementations/ExhibitionService.cs:289)، [ExhibitionMappingProfile.cs:67](../ExhibitionManagementSystem.Models.DTOs/Mapping/Profiles/ExhibitionMappingProfile.cs:67).

المعالجة: تطبيع القيمة مرة واحدة، ثم `TryParse` و`IsDefined` قبل Mapping؛ إعادة 400 برسالة محددة للإنشاء والتعديل.

### F11 — قائمة القاعات تعرض عدد الأجنحة صفرًا رغم وجود أجنحة

تعتمد `BoothsCount` في Mapping على مجموعة الأجنحة، بينما لا يحمل `GetByVenueAsync` المجموعة. أثبتت تجربة SQL: `boothsCount=0` و`storedBooths=1`. تستخدم VenuesPage الاستجابة لعرض بيانات القاعات.

المواقع: [HallRepository.cs:16](../ExhibitionManagementSystem.DataAccess/Repositories/Implementations/HallRepository.cs:16)، [HallMappingProfile](../ExhibitionManagementSystem.Models.DTOs/Mapping/Profiles/HallMappingProfile.cs:13).

المعالجة: إسقاط عدد الأجنحة في الاستعلام أو تحميل المجموعة المطلوبة. تحقق بسياق جديد حتى لا يخفي تتبع EF نقص التحميل.

### F12 — اسم قاعة الفعالية يختفي عند جلب الجدول

يرجع مستودع الجدول السجلات دون تضمين Hall؛ يعيد Mapping اسمًا فارغًا. في التجربة كانت القاعة محفوظة باسم `Hall`، بينما أعادت خدمة الجدول `HallName=""`. تعرض EventsPage «غير محددة» رغم وجود HallID صحيح.

المواقع: [ExhibitionScheduleRepository.cs:17](../ExhibitionManagementSystem.DataAccess/Repositories/Implementations/ExhibitionScheduleRepository.cs:17)، [ExhibitionService.cs:277](../ExhibitionManagementSystem.Services/Implementations/ExhibitionService.cs:277).

المعالجة: تحميل/إسقاط اسم القاعة في مسار القائمة واختباره بعد إعادة إنشاء السياق.

### F13 — قائمة المؤسسات تعرض عدم وجود خطة لمؤسسة مشتركة

`CurrentPlan` خاصية محسوبة من `TenantSubscriptions`، لكن استعلام القائمة العام لا يحمل الاشتراكات. أعادت القائمة `CurrentPlan=null`، بينما أعاد مسار الاشتراك النشط للمؤسسة نفسها خطة `Enterprise`. لذلك تعرض TenantsPage «لا توجد خطة».

المواقع: [TenantService.cs:60](../ExhibitionManagementSystem.Services/Implementations/TenantService.cs:60)، [Tenant.CurrentPlan](../ExhibitionManagementSystem.Models/Tenant.cs)، [TenantsPage](../exhibitionmanagementsystem.web/src/features/tenants/pages/TenantsPage.tsx:38).

المعالجة: إسقاط الخطة النشطة في الاستعلام مع ترتيب زمني متسق، أو تحميل الاشتراكات اللازمة.

### F14 — فلاتر وإحصاءات التدقيق لا تطابق أسماء العمليات المخزنة

يكتب UnitOfWork قيم EF مثل `Added`, `Modified`, `Deleted`. تتوقع الصفحة في الفلتر قيمًا مثل `insert`, `update`, `delete` بالمساواة؛ ويبحث عداد الإضافات عن `insert/create`. بتشغيل المكوّن الأصلي بسجل `Added` واحد، كان إجمالي السجلات **1** وعدد الإضافات **0**، وأخفى فلتر الإضافة السجل كله.

المواقع: [UnitOfWork.cs:191](../ExhibitionManagementSystem.DataAccess/Repositories/Implementations/UnitOfWork.cs:191)، [AdminAuditPage.tsx:165](../exhibitionmanagementsystem.web/src/features/admin-audit/pages/AdminAuditPage.tsx:165)، [عداد الإضافة:185](../exhibitionmanagementsystem.web/src/features/admin-audit/pages/AdminAuditPage.tsx:185).

المعالجة: تحويل أسماء العمليات إلى قيم موحدة قبل العد والتصفية والترجمة، مع دعم السجلات التاريخية.

### F15 — سجل التدقيق في الويب يتوقف عند أول 200 عملية

**دليل مصدر:** تطلب الصفحة `page=1&pageSize=200` مرة واحدة، وتتجاهل العدد الكلي، ثم تطبق البحث والتصفية والإحصاءات محليًا. لا توجد آلية لجلب الصفحات التالية؛ لا يمكن الوصول إلى السجلات الأقدم من هذه الشاشة وتظهر إحصاءات عينة محدودة.

الموقع: [AdminAuditPage.tsx:138](../exhibitionmanagementsystem.web/src/features/admin-audit/pages/AdminAuditPage.tsx:138).

المعالجة: ترقيم وتصفية على الخادم مع إظهار العدد الحقيقي، أو قيد معلن وواضح على «آخر 200 عملية» مع مسار للوصول إلى البقية.

### F16 — مشغل البوابة يرى زر معاينة التذكرة لكن طلبه يُرفض

تسمح صفحة التذاكر ومسار القراءة بدور `GateOperator` ويظهر زر «عرض التذكرة» دون تقييد. يستدعي الزر مسار Badges الذي يتطلب `BadgeManagement` المستبعد منه هذا الدور. أثبت HTTP أن قراءة التذاكر للمشغل **200**، ومعاينة شارتها **403**؛ الطلب نفسه لمدير المؤسسة **200**.

المواقع: [TicketsPage.tsx:503](../exhibitionmanagementsystem.web/src/features/tickets/pages/TicketsPage.tsx:503)، [BadgesController.cs:13](../ExhibitionManagementSystem/Controllers/Badges/BadgesController.cs:13)، [AuthorizationPolicies.cs:35](../ExhibitionManagementSystem/Security/AuthorizationPolicies.cs:35).

المعالجة: فصل صلاحية قراءة/طباعة الشارة عن تعديل القوالب ومنح الدور المطلوب وفق سير الاستقبال، أو إزالة الإجراء غير المسموح من واجهته.

### F17 — التذكرة النشطة تُعرض بلون الخطأ

يعيد الخادم `status: "Active"`، بينما تعتبر الواجهة التذكرة صالحة فقط إذا كانت `"Valid"`، وهي ليست حالة معرفة في النموذج. مرّرت JSON تذكرة حقيقية إلى المكوّن الأصلي فنتج `<span class="badge badge-danger">Active</span>`.

الموقع: [TicketsPage.tsx:497](../exhibitionmanagementsystem.web/src/features/tickets/pages/TicketsPage.tsx:497).

المعالجة: استخدام حالات TicketStatus الفعلية وترجمتها وتحديد أسلوب عرض كل منها، مع اختبار تذكرة نشطة وأخرى مستخدمة وملغاة.

### F18 — الطباعة الآلية بالكشك لا تبني شارة الزائر

**دليل مصدر:** بعد نجاح المسح تبني الصفحة بطاقة تأكيد باسم «تذكرة رقم ...» وشركة ثابتة، ثم تنفذ `window.print()`؛ لا تجلب BadgePrintPayload ولا تعرض BadgeCard أو QR التذكرة في هدف الطباعة. هدف `.kiosk-print-target` نفسه هو بطاقة التأكيد. كما تُطلب الطباعة مباشرة بعد تحديث حالة React دون انتظار ظهور الهدف الجديد.

المواقع: [CheckInKioskPage.tsx:62](../exhibitionmanagementsystem.web/src/features/kiosk/pages/CheckInKioskPage.tsx:62)، [طلب الطباعة:77](../exhibitionmanagementsystem.web/src/features/kiosk/pages/CheckInKioskPage.tsx:77)، [هدف الطباعة:154](../exhibitionmanagementsystem.web/src/features/kiosk/pages/CheckInKioskPage.tsx:154).

المعالجة: جلب بيانات الشارة المرتبطة بالتذكرة، عرض الاسم الحقيقي والرمز والقالب في هدف طباعة مخصص، ثم بدء الطباعة بعد اكتمال العرض. لم تنفذ تجربة طابعة فعلية؛ الملاحظة عن مسار البيانات والمكوّن المطبوع.

### F19 — فشل تحميل تفاصيل المعرض يسمح بحفظ قيم بديلة تمحو بياناته

**دليل مصدر:** عند فشل طلب التفاصيل، يفتح `catch` نموذج التعديل بموقع القائمة الأول ووصف فارغ، ثم يسمح بالحفظ. تعديل الاسم مثلًا يرسل هذه القيم إلى الخدمة، فيمحو الوصف، وقد يغير الموقع إن لم توجد تبعيات تمنع النقل. فشل القراءة المؤقت يتحول إلى تعديل بيانات لم يقصده المستخدم.

الموقع: [ExhibitionsPage.tsx:197](../exhibitionmanagementsystem.web/src/features/exhibitions/pages/ExhibitionsPage.tsx:197).

المعالجة: منع فتح نموذج قابل للحفظ قبل نجاح قراءة التفاصيل الكاملة، وإظهار فشل التحميل وإعادة المحاولة دون إنشاء قيم بديلة لحقول قائمة.

## فجوات وصول الميزات إلى الويب

هذه فجوات تنفيذ مؤكدة في المسارات المتاحة، منفصلة عن الملاحظات الـ19؛ ترتيب استكمالها يتطلب تحديد نطاق المنتج:

- **الاسترداد والإشعارات الدائنة:** توجد API للإنشاء، لكن InvoicesPage تعرض السجلات ولا توفر إجراء إنشاء. إلغاء حجز مدفوع يحتاج تسوية أولًا؛ لا يكتمل هذا السير من الويب الحالي.
- **المصروفات والتقارير المحفوظة:** توجد نماذج ومستودعات للمصروفات وحسابها في التقرير، ولا يوجد مسار إدخال مصروف في API/الويب. توجد Reports API للتوليد والقراءة، دون صفحة ويب تستدعيها؛ صفحة Analytics تستخدم مخطط Dashboard فقط.
- **المؤسسات والصلاحيات:** TenantsPage قائمة قراءة دون إنشاء/تعديل/تعطيل أو إعادة إرسال دعوة. UsersPage تنشئ حسابًا بدور واحد، دون ربط مساري إسناد/إزالة دور لحساب قائم.
- **الرعاية:** تبويبا العقود والمساحات للعرض، ولا يوفران إجراءات إنشاء/إدارة العقد وتخصيص المساحات، رغم وجود مساراتها في الخادم.
- **قواعد الخدمات وباقات التسعير:** توجد نماذج وخدمات وAPI، دون واجهات إدارة متصلة في الويب.
- **المنتجات وطاقم العارض وتسجيل الفعاليات:** توجد نماذج ومستودعات؛ لا توجد دورة إدارة كاملة من API إلى الويب. إصدار شارة موظف يعتمد على وجود BoothStaff سابقًا، دون مسار ويب لإنشاء هذا السجل.
- **انتهاء الاشتراك:** توجد تواريخ وحالات للاشتراك، لكن التحقق من الجلسة يعتمد على `Tenant.IsActive` دون اشتراط اشتراك ساري. تحديد أن الانتهاء يجب أن يمنع العمليات قرار وظيفي؛ لم أعده ثغرة أو ملاحظة مؤكدة دون هذا الشرط.

## التحقق والأدلة وحدود الفحص

| الفحص | النتيجة |
|---|---|
| اختبارات الخادم الحالية مع SQL وقواعد مؤقتة، دون اختبار مساعد سطح المكتب | **265 ناجحة، صفر فشل، صفر تجاوز** |
| اختبارات الويب الحالية | **65 ناجحة** |
| TypeScript وبناء Vite | نجاح |
| ESLint | نجاح |
| تجارب المراجعة الجديدة على الخدمات وHTTP وSQL، بما فيها تزامن حذف الموقع | **11 تجربة تشخيصية تثبت السلوك الحالي** |
| تجارب وحدات الويب الأصلية وعرض React بالاستجابات الفعلية | **5 تحققات ناجحة في إثبات المشكلات** |

نجاح التجارب التشخيصية يعني نجاح إعادة إنتاج العيوب الموصوفة؛ ليست اختبارات تثبت إصلاحها. بعض التجارب تثبت أكثر من ملاحظة. نجحت مجموعة الخادم الحالية رغم هذه العيوب لأن تغطيتها لا تشمل جميع عقود الويب والحالات الحدية والتداخلات الزمنية الجديدة.

أُنشئت بيانات صناعية فقط في قواعد SQL ذات بادئة `EMS_Feature_`، وحُذفت في نهاية التجارب؛ تحقق استعلام أخير من عدم بقاء أي منها. لم يُشغّل Program أو مهيئ حساباته، ولم تُستخدم حسابات فعلية أو يُرسل SMTP. لم تُختبر طابعة أو كاميرا فعلية، ولم تُنفذ جولة يدوية كاملة داخل متصفح لكل شاشة أو اختبار حمل شامل.

الملفات:

- [أدلة التنفيذ المجمعة](feature-audit-2026-10-17-evidence.json).
- [مصدر تجارب الخادم وSQL وHTTP](feature-audit-2026-10-17-probes.cs.txt).
- [مصدر تجارب وحدات ومكونات الويب](feature-audit-2026-10-17-web-probes.cjs).

حُفظ مشروع التجارب المعزول وسجلات TRX في `%TEMP%\EMS_Feature_Audit_20261017`. مصدر C# محفوظ كملف نصي في docs ومضمّن صراحة في ذلك المشروع المؤقت، ولا يغير اختبارات التطبيق المعتادة. استُخدمت بيئة التشغيل المعزولة `C:\Users\Masoud\.dotnet\ems-10.0.13`، واتصال Windows المحلي بقاعدة مؤقتة عبر `EMS_SQL_TEST_CONNECTION`.

ترتيب المعالجة المقترح: **F01–F06 أولًا**، ثم حماية تاريخ الرعاية **F07–F08**، ثم الدقة والتحقق **F09–F10**، ثم اتساق البيانات والعرض وسير الاستقبال. بعد المعالجة تُحوّل التجارب التشخيصية إلى اختبارات تتوقع السلوك الصحيح، وتُضاف اختبارات عقود تربط JSON الخادم بمكونات الويب.
