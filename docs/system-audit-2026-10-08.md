# تدقيق النظام — 8 أكتوبر 2026

هذا سجل نتائج التدقيق قبل تنفيذ الإصلاحات. حالة التنفيذ اللاحقة موثقة في [تقرير الإصلاحات](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/docs/system-repairs-2026-10-08.md). أُبقي حساب الاختبار دون تغيير وفق توجيه المستخدم.

## النتيجة التنفيذية

النظام يبني بنجاح، والاختبارات الموجودة ناجحة، لكنه **ليس خاليًا من الأخطاء ولا يمكن اعتماده باعتباره سليمًا بالكامل**. أسفر التدقيق عن 25 نتيجة: واحدة حرجة P0، و14 عالية P1، و10 متوسطة P2. تشمل النتائج عيوبًا أعيد إنتاجها فعليًا وعيوبًا حاسمة من مراجعة الكود؛ يوضح الجدول مستوى الدليل لكل نتيجة. هذا عدد النتائج الموثقة، لا تقدير عدد كل العيوب الممكنة.

لم أطبق إصلاحات إنتاجية في هذه الجولة، لأن الطلب تحليل وتحقق. أُضيف هذا التقرير فقط؛ جميع التغييرات السابقة الموجودة في مساحة العمل حُفظت.

## طريقة الفحص وحدوده

- مراجعة مسارات API والسياسات والخدمات وخرائط DTO ونماذج البيانات والفلاتر والترحيلات، ثم ربطها بمسارات الويب وأجزاء سطح المكتب.
- بناء الحل بما فيه WPF، وتشغيل اختبارات الخادم والواجهة وفحص الحزم.
- تشغيل API وVite على منافذ محلية منفصلة، وقاعدة SQL Server مؤقتة لها ترحيلات النظام كاملة، وحسابات اصطناعية بأدوار مستقلة.
- تشغيل 15 سيناريو خدمة/تحكم تشخيصيًا على SQL الحقيقي، داخل معاملات متراجعة، بعد فحص أولي داخل الذاكرة.
- اختبار HTTP للمصادقة والصلاحيات والتذاكر والفواتير والدفع والإعدادات والاشتراكات والقراءة والتقارير.
- فحص واجهة المتصفح للدخول وصلاحيات الإعدادات وقائمة الفواتير وزر «عرض وسداد» ووجود خيار الطباعة.
- فحص قاعدة البيانات الرئيسية **قراءة فقط** بواسطة عدادات اتساق، دون إخراج أسماء العملاء أو رموز الجلسات أو كلمات المرور أو تفاصيل الاتصال.

لم يحدث نشر أو تعديل حساب حقيقي أو سداد حقيقي. إنشاء السجلات والمدفوعات وحذف الكيانات أثناء الاختبار جرى على البيانات الاصطناعية فقط.

لا يشمل هذا ضمانًا لكل حالات التشغيل: لم يُجر اختبار تحميل طويل، ولا اختبار اختراق خارجي كامل، ولا جميع سباقات التزامن، ولا التشغيل التفاعلي لكل شاشة WPF، ولا إخراج ورقي عبر طابعة، ولا تسليم بريد الدعوات فعليًا. بيانات الإنتاج وبيئة الاستضافة والطابعات تحتاج اختبارات قبول مستقلة.

## ما نجح

| الفحص | النتيجة |
|---|---|
| بناء الحل `dotnet build ExhibitionManagementSystem.slnx --no-restore` | نجاح؛ صفر أخطاء وصفر تحذيرات بناء |
| اختبارات الخادم مع تفعيل SQL | 144 ناجحًا، صفر فشل، صفر تجاوز |
| اختبارات الويب | 3 ناجحة |
| ESLint وTypeScript وبناء Vite | نجاح |
| npm audit | صفر ثغرات معلنة وقت الفحص |
| فحص حزم NuGet مع التبعيات غير المباشرة | لا ثغرات معلنة من المصادر المهيأة وقت الفحص |
| health/live وhealth/ready على النسخة المعزولة | HTTP 200 |
| منع الوصول المالي بلا تسجيل الدخول | HTTP 401 |
| منع قائمة الفواتير عن viewer وGateOperator | HTTP 403 |
| عزل المؤسسة الثانية | قائمة الفواتير فارغة؛ فاتورة/معرض/موقع المؤسسة الأولى أعادت 404 |
| تسجيل دفعة صحيحة بعملة الفاتورة | HTTP 200، وتحويلها إلى العملة الأساسية ظاهر في الواجهة |
| إعادة الدفعة بنفس Idempotency-Key | HTTP 200 ونفس PaymentID |
| إعادة المفتاح بجسم طلب مختلف | HTTP 409 |
| الدفعة بلا مفتاح أو بعملة مخالفة للفاتورة | HTTP 400 |
| تسجيل الخروج | HTTP 204؛ الرمز السابق أصبح 401؛ تجديده رُفض |
| إنشاء تقرير معرض | HTTP 200 |
| قراءة القاعات والحجوزات والخدمات والتسعير والرعاة والعقود والقوالب | HTTP 200 على المسارات الفعلية المختبرة |
| شاشة المالية وزر عرض وسداد | ظهرت النافذة وبنود الفاتورة والدفعات والمتبقي دون صفحة بيضاء في السيناريو المختبر |
| خيار طباعة الفاتورة | الزر موجود في نافذة التفاصيل؛ لم يُختبر الإخراج الورقي |

نجاح هذه الاختبارات لا يتعارض مع وجود العيوب أدناه؛ كثير من الاختبارات الحالية تفحص الخدمات أو السياسات مباشرة ولا تمر بكل خط أنابيب HTTP. عطل مسح التذاكر مثال واضح: استدعاء الخدمة يعمل، لكن ربط مدخلات endpoint يفشل.

## النتائج المرتبة

P0: حرج، يبدأ به قبل الاعتماد الإنتاجي. P1: عالٍ يؤثر في المال/الأمن/سلامة البيانات أو يعطل وظيفة أساسية. P2: متوسط، يحتاج إصلاحًا قبل إغلاق التدقيق.

| رقم | أولوية | الخلل | الدليل والأثر | موضع الكود |
|---|---|---|---|---|
| F01 | P0 | تهيئة حساب إداري بكلمة مرور ثابتة وإعادة منحه الصلاحيات | عند كل تشغيل يُعاد تعيين كلمة مرور الحساب المعروف إن تغيّرت، وتُعاد أدواره وتفعيله. مراجعة مباشرة لمسار التشغيل؛ لم تُختبر هذه العملية على حسابك الحقيقي. | [DataSeeder.cs:164](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.DataAccess/Seeder/DataSeeder.cs:164) |
| F02 | P1 | مسح التذاكر يتوقف بخطأ HTTP 500 | طلب مسح صحيح بلغ الخادم لكنه فشل في Model Binding قبل الخدمة: سمات التحقق موضوعة على خصائص record بدل معاملات المُنشئ. | [TicketsController.cs:82](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem/Controllers/Visitor/TicketsController.cs:82) |
| F03 | P1 | تسعير الدمج يعتمد على نوع يرسله العميل | على SQL: جناحان VIP، مساحة إجمالية 20، سعر VIP=100/م² وStandard=1/م². إرسال Standard أنشأ الحجز بنجاح بقيمة 20 بدل 2000؛ قواعد الأجنحة المعيّنة لا تُجمع للدمج. | [ReservationService.cs:172](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/ReservationService.cs:172) |
| F04 | P1 | حذف العارض يُخفي فواتيره | على SQL: فاتورة واحدة مخزنة، القائمة أصبحت فارغة بعد حذف العارض، بينما TotalCount بقي 1. الحذف الناعم مع العلاقات المطلوبة والفلاتر يُخفي التاريخ المالي ولا يحذفه فعليًا. | [ExhibitorService.cs:131](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/ExhibitorService.cs:131)؛ [FinancialService.cs:40](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/FinancialService.cs:40) |
| F05 | P1 | حذف الموقع مع وجود بيانات تابعة | حذف موقع يحتوي قاعات ومعارض وحجوزات قُبل. على SQL بقيت 3 معارض مخزنة، لكن قائمة المعارض أعادت صفرًا بسبب فلتر الموقع المحذوف. | [VenueService.cs:277](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/VenueService.cs:277) |
| F06 | P1 | كشف المؤشرات المالية لمستخدمين بلا صلاحية مالية | حساب بلا أدوار وحساب GateOperator: قائمة الفواتير HTTP 403، لكن /dashboard/stats و/revenue-chart أعادا HTTP 200 مع الأرقام المالية. | [DashboardController.cs:19](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem/Controllers/Dashboard/DashboardController.cs:19) |
| F07 | P2 | الاشتراكات تقبل رسومًا سالبة ومددًا وخططًا غير مضبوطة | TenantAdmin سجّل اشتراك Enterprise برسوم -100 ونهاية بعد 50 سنة، HTTP 200. هذا يثبت إنشاء سجل اشتراك غير صحيح. CurrentPlan عرض مشتق من الاشتراكات وليس حقلًا مستقلًا، لذلك قد يتأثر بالسجل؛ لا يثبت الاختبار شراء خطة أو تفعيل مزايا تجارية. | [AdminService.cs:80](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/AdminService.cs:80)؛ [AdminController.cs:15](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem/Controllers/Admin/AdminController.cs:15) |
| F08 | P1 | تعارض صلاحيات إدارة أسعار الصرف بين الواجهة والخادم | SuperAdmin وحده أُحيل إلى unauthorized عند فتح settings. TenantAdmin يمكنه الصفحة لكن تبويب العملات مخفي وPUT rates ممنوع؛ SuperAdmin يستطيع PUT rates مباشرة. الحساب متعدد الأدوار يخفي العيب. | [AppRoutes.tsx:111](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/exhibitionmanagementsystem.web/src/app/routes/AppRoutes.tsx:111)؛ [SettingsPage.tsx:53](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/exhibitionmanagementsystem.web/src/features/settings/pages/SettingsPage.tsx:53) |
| F09 | P1 | خدمة المسح تقبل التذكرة قبل تاريخ صلاحيتها | استدعاء الخدمة مع SQL قبل 2026-10-15 قبل الدخول في 2026-10-08. التحقق يرفض الماضي فقط. عطل HTTP في F02 يحجب هذا العيب حاليًا عن المسار العام لكنه لا يصلحه. | [TicketService.cs:124](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/TicketService.cs:124) |
| F10 | P2 | إصدار التذاكر لا يراعي إلغاء المعرض | على SQL: جعل المعرض Cancelled ثم إصدار تذكرة نجح. غياب التحقق من حالة المعرض وفترة انعقاده في الإصدار والمسح. | [TicketService.cs:27](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/TicketService.cs:27) |
| F11 | P1 | نقل معرض إلى موقع آخر رغم وجود حجوزات | التعديل نجح بينما بقي جناح الحجز في الموقع 1 وأصبح المعرض في موقع آخر؛ علاقة الحجز بموقع المعرض أصبحت غير صحيحة. | [ExhibitionService.cs:121](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/ExhibitionService.cs:121) |
| F12 | P2 | إنشاء المعرض يقبل تواريخ معكوسة ورسم دخول سالب | POST exhibitions بتاريخ نهاية قبل البداية ورسم -10 أعاد HTTP 201. يحتاج تحققًا في DTO والخدمة وكذلك التعديل. | [ExhibitionService.cs:81](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/ExhibitionService.cs:81) |
| F13 | P2 | الفعالية تقبل قاعة خارج موقع المعرض ومدخلات غير منطقية | على SQL قُبلت قاعة من موقع آخر تابع لنفس المؤسسة، ونهاية قبل البداية وMaxAttendees=-1. فحص ملكية المؤسسة لا يكفي لفحص موقع المعرض. | [ExhibitionService.cs:200](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/ExhibitionService.cs:200) |
| F14 | P1 | حذف المؤسسة معطل | DELETE tenants/1 أعاد HTTP 500؛ TenantService يستدعي SoftDeleteAsync لكن Tenant لا يطبق ISoftDeletable. | [TenantService.cs:476](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/TenantService.cs:476) |
| F15 | P2 | مخطط الموقع: JSON صحيح نحويًا قد يسبب HTTP 500 | حفظ PlanJSON='[]' أعاد 500: TryGetProperty يفترض جذرًا من نوع Object؛ التقاط JsonException لا يعالج InvalidOperationException. | [VenueService.cs:128](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/VenueService.cs:128) |
| F16 | P1 | بعض الشارات تحمل QR غير قابل للتحقق | زائر بلا تذكرة: توليد الشارة نجح لكن لم تُنشأ تذكرة، ومسح QR في الخدمة أعاد TICKET_NOT_FOUND. QR طاقم العارض STAFF-* غير معالج أيضًا في البحث عن المشاركين؛ الجزء الخاص بالطاقم مؤكد بمراجعة الكود ولم يُختبر على طابعة. | [BadgeService.cs:213](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/BadgeService.cs:213)؛ [BadgeService.cs:252](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/BadgeService.cs:252) |
| F17 | P1 | تغيير مساحة الجناح لا يحدّث/يحمي الدمج المرتبط | على SQL: أُنشئ دمج صحيح من جناحين عبر الخدمة، ثم تغيير أحدهما إلى 400 م² نجح. بقي TotalAreaSqM للدمج 20 بينما مجموع أعضائه أصبح 410. تسعير الدمج يعتمد على المساحة القديمة. مسار Update لا يحمي حجوزات قائمة أيضًا. | [BoothService.cs:177](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/BoothService.cs:177) |
| F18 | P1 | باقات الخدمات لا تحول عملة سعر الخدمة | خدمة 10 USD داخل باقة LYD بقي سعر وحدتها 10 دون تحويل، رغم وجود USD→LYD=5. معرّف خدمة غير موجود تم تجاهله. الحفظ موزع على مرحلتين بلا معاملة شاملة؛ فشل المرحلة الثانية قد يترك باقة ناقصة، وهذه الجزئية الأخيرة خطر من الكود وليست إعادة إنتاج لفشل حفظ. | [PricingService.cs:407](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/PricingService.cs:407) |
| F19 | P2 | قبول قيم رقمية غير معرفة للتعدادات | تسجيل دفعة method='999' نجح HTTP 200، وظهرت الطريقة 999 في تفاصيل الفاتورة. Enum.TryParse وحده لا يضمن Enum.IsDefined؛ النمط نفسه مستخدم في حالات وأنواع أخرى. | [FinancialService.cs:313](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/FinancialService.cs:313) |
| F20 | P1 | التحليلات تجمع أرصدة شهرية تراكمية كأنها مبالغ مستقلة | في سيناريو SQL تاريخي داخل معاملة متراجعة: مجموع pending الذي تعرضه الواجهة=5990، بينما رصيد آخر شهر=490. pending رصيد نهاية الشهر وليس حركة شهرية قابلة للجمع. | [AnalyticsPage.tsx:10](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/exhibitionmanagementsystem.web/src/features/analytics/pages/AnalyticsPage.tsx:10) |
| F21 | P1 | لوحة سطح المكتب لا تزال تجمع عملات الفواتير مباشرة | DashboardViewModel يجمع TotalAmount وPayment.Amount دون القيم الأساسية المحوّلة، فلا يمكن جمع USD وLYD وEUR بهذا الشكل. يقتصر أيضًا على أول 200 فاتورة، ويجمع التحصيل حسب شهر إنشاء الفاتورة بدل تاريخ الدفعة. | [DashboardViewModel.cs:97](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.DeskTop/ViewModels/Dashboard/DashboardViewModel.cs:97) |
| F22 | P2 | مؤشرات وهمية في سطح المكتب | عدد الأجنحة المحجوزة ثابت صفر، الإيرادات الصفرية تُستبدل بأرقام مصطنعة، توزيع المعارض افتراضي عند عدم وجود بيانات، ومنحنى الزوار مولّد من نسب ثابتة بدل سجلات المسح. | [DashboardViewModel.cs:87](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.DeskTop/ViewModels/Dashboard/DashboardViewModel.cs:87)؛ [AnalyticsViewModel.cs:124](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.DeskTop/ViewModels/Analytics/AnalyticsViewModel.cs:124) |
| F23 | P2 | مدير الفعاليات يرى وظائف لا يستطيع تنفيذها | الواجهة تسمح EventManager بصفحة events؛ إضافة الجدول عبر الحساب نفسه أعادت 403 لأن الخادم يستخدم AdminOnly. يلزم توحيد الصلاحيات وإظهار الأزرار طبقًا لها. | [AppRoutes.tsx:100](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/exhibitionmanagementsystem.web/src/app/routes/AppRoutes.tsx:100)؛ [ExhibitionsController.cs:144](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem/Controllers/Exhibition/ExhibitionsController.cs:144) |
| F24 | P2 | فلاتر الفواتير والإحصاءات في سطح المكتب غير متوافقة | قائمة الفلاتر تتضمن Pending غير الموجودة في InvoiceStatus، وتحذف حالات Draft/Issued/PartiallyPaid. التصفية والإحصاءات تجريان على الصفحة الحالية فقط، لا على كل النتائج. نتيجة Overdue من الخدمة لا تعني أن Status المخزن يساوي Overdue. | [InvoicesViewModel.cs:24](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.DeskTop/ViewModels/Financial/InvoicesViewModel.cs:24) |
| F25 | P2 | نسبة الإشغال في تحليلات سطح المكتب مضروبة مرتين | ReportService يحسب النسبة ×100؛ AnalyticsViewModel يضربها ×100 مرة ثانية: إشغال 100% يُعرض 10000%. مؤكد حسابيًا من المسارين، دون تشغيل WPF تفاعلي. | [AnalyticsViewModel.cs:117](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.DeskTop/ViewModels/Analytics/AnalyticsViewModel.cs:117)؛ [ReportService.cs:83](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/ReportService.cs:83) |

## سجل إعادة الإنتاج الأهم

```text
TENANT_DELETE: NotSupportedException; HTTP 500
SCAN_ENDPOINT: HTTP 500; record validation metadata on property Location
FUTURE_TICKET_SCAN: issued=True; entry_accepted=True; valid_date=2026-10-15
CLOSED_EXHIBITION_TICKET: accepted=True
EXHIBITION_MOVE_WITH_RESERVATION: accepted=True; exhibition_venue!=reserved_booth_venue
EXHIBITION_BAD_DATES: accepted=True; HTTP 201
SCHEDULE_OTHER_VENUE_BAD_DATES: accepted=True
EXHIBITOR_DELETE_INVOICES: stored_invoices=1; returned=0; total_count=1
SUBSCRIPTION_SELF_GRANT: accepted=True; monthly_fee=-100; HTTP 200
INVALID_PLAN_JSON_TYPE: InvalidOperationException; HTTP 500
VISITOR_BADGE_NO_TICKET: badge_created=True; visitor_tickets_persisted=0; TICKET_NOT_FOUND
RESIZE_MERGED_BOOTH: accepted=True; booth_area=400; merge_area=20; member_sum=410
PACKAGE_CURRENCY_MISMATCH: package_currency=LYD; service_currency=USD; unit_price=10
MERGE_VIP_AS_STANDARD: accepted=True; charged=20.00; vip_price=2000
VENUE_DELETE_WITH_DEPENDENTS: deleted=True; stored_exhibitions=3; returned_exhibitions=0
ANALYTICS_CUMULATIVE_BALANCE: pending_sum_ui=5990; latest_pending=490; month_count=12
UNKNOWN_PAYMENT_METHOD_NUMERIC: HTTP 200; method displayed as 999
```

لإعادة الإنتاج دون المساس بالبيانات الرئيسية: أنشئ قاعدة مؤقتة بالترحيلات، ثم مؤسسة وموقعًا وقاعة ومعرضًا واحتفظ بفاتورة تجريبية. أنشئ مستخدمًا منفصلًا لكل دور. لا تستخدم الحساب الذي يمتلك كل الأدوار لاختبار الصلاحيات لأنه يخفي التعارضات.

- F02: إرسال POST tickets/scan بجسم صحيح يحتوي qrCode وdirection=In وlocation. يجب أن يصل للخدمة أو يرجع خطأ تحقق 400؛ حاليًا 500 قبل التنفيذ.
- F03: أنشئ جناحين VIP بمساحة 10 لكل منهما، قاعدة VIP=100 وقاعدة Standard=1 بنفس العملة، ودمجهما؛ احجز MergeID مع BoothTypeSelected=Standard. حاليًا يختار السعر الرخيص بدل سياسة معتمدة للدمج.
- F04: اعرض الفاتورة، احذف العارض، أعد طلب قائمة الفواتير. لا ينبغي اختفاء المستند التاريخي بسبب حذف طرفه.
- F08: سجّل الدخول بحساب SuperAdmin **فقط**، ثم افتح settings. يجب أن تتاح إدارة الصرف دون منحه أدوار التشغيل المؤسسية الأخرى.
- F20: فاتورة قديمة غير مسددة تبقى في كل رصيد شهري؛ لا تجمع هذه الأرصدة في بطاقة «المتبقي». استخدم الرصيد الحالي أو آخر نقطة وفق التعريف المعتمد.
- F21: استخدم فواتير USD وLYD وEUR ودفعات في أشهر مختلفة؛ قارن شاشة WPF بالقيم الأساسية في API، مع أكثر من 200 فاتورة لاختبار التصفح والتجميع.

## فحص البيانات الحالية — قراءة فقط

الفحوص الآتية أعادت صفر حالات مخالفة وقت التدقيق:

1. نهاية معرض قبل بدايته.
2. رسوم دخول سالبة.
3. حجز جناح في موقع مخالف لموقع المعرض.
4. نهاية فعالية قبل بدايتها.
5. قاعة فعالية خارج موقع المعرض.
6. فاتورة مرتبطة بعارض محذوف.
7. معرض مرتبط بموقع محذوف.
8. فرق مساحة دمج عن مجموع المساحات الحالية لأجنحته.
9. طريقة دفع خارج قيم PaymentMethod المعرفة.
10. رسوم اشتراك سالبة.

الترحيلات المعلقة في القاعدة الرئيسية: صفر.

هذه نتيجة مطمئنة عن **تلك القيود المحددة في البيانات الحالية** وليست إثباتًا خلو القاعدة من كل عيب. الأعطال أعيد إنتاجها بمدخلات جديدة في قاعدة معزولة، ولذلك يظل منع ظهورها مستقبلاً مطلوبًا.

## نقاط تحتاج قرارًا أو اختبارًا إضافيًا — لا تدخل في عدد العيوب المؤكدة

- سعر التذكرة وعملتها قابلان للإدخال اليدوي؛ إصدار تذكرة صفرية لمعرض ذي رسم دخول قُبل. الواجهة نفسها تعرض سعرًا يدويًا، لذلك يجب حسم هل هذا خصم/سعر خاص مقصود أم يجب ربطه برسوم المعرض وأنواع التذاكر، وتحديد سلطة تغيير السعر.
- معنى ValidDate وتاريخ المعرض يجب توثيقه: تاريخ واحد للدخول أم بداية صلاحية أم نهاية صلاحية؛ ضبط التحقق وفق هذا العقد.
- تعريف اشتراكات المنصة: هل إدارة المؤسسة تسجّل تاريخ اشتراك فقط أم تشتري خطة؟ إن كانت شراء خطة، يجب أن يحدد الخادم السعر والمدد والحالة، لا العميل.
- إلزامية الخدمات عند الإضافة والإزالة وتعديل المسودات تحتاج اختبار قبول موحدًا؛ إزالة الخدمة الإلزامية ليست محمية صراحة في مسار الحذف.
- هندسة الأجنحة: التصادم والدوران والمضلعات وتغيير القاعات بعد الحجز تحتاج حالات قبول إضافية؛ نجاح المصمم بصريًا لا يكفي لصحة مساحة التسعير.
- فشل حفظ نتيجة idempotency بعد نجاح المعاملة، وتعطل العملية أثناء التنفيذ، يحتاج اختبار حقن فشل للتأكد من عدم تكرار الأثر التجاري.
- بدء الحجز والدمج بالتزامن على نفس الجناح، والدفعات المتزامنة المختلفة، يحتاج اختبار ضغط SQL متخصص.
- ظهرت تحذيرات وقت التشغيل عن علاقات مطلوبة مع query filters، وعلاقتي BoothMerge/Reservation، وتحميل عدة مجموعات في استعلام واحد. بعض أثر الفلاتر مؤكد في F04/F05؛ بقية التحذيرات ليست وحدها دليل عطل.
- ظهرت رسالة تحذير ترخيص AutoMapper أثناء التشغيل؛ يجب مراجعة تهيئة الترخيص المناسبة لبيئة الإنتاج. ليست خطأ بناء أو دليل ثغرة.
- الدعوات والبريد والطابعات الحرارية والفواتير المطبوعة والنسخ الاحتياطي والاستعادة تحتاج تحققًا فعليًا مع الإعدادات والأجهزة المقصودة.

## خطة التصحيح المقترحة ومعايير القبول

### توجيهات المستخدم المعتمدة بعد عرض الخطة

- النظام ما زال في مرحلة الاختبار. الحفاظ على الحساب الإداري التجريبي وكلمة مروره وصلاحياته الحالية؛ لا حذف للحساب أو إعادة إعداد جذرية أو تبديل لبيانات دخوله في هذه المرحلة. فصل التهيئة التجريبية عن إعداد الإنتاج يُراجع عند تجهيز النظام للإنتاج، ولا يُنفذ كتغيير مفاجئ أثناء الاختبار. ملاحظة F01 تبقى خطرًا يجب معالجته قبل الاعتماد الإنتاجي.
- F04: منع حذف العارض إذا كانت له أي فاتورة مرتبطة، مهما كانت حالتها. لا أرشفة للعارض ولا نقل لبياناته كحل لهذه المشكلة. التحقق يُطبق على الخادم قبل أي تعديل، وتعرض الواجهة سبب الرفض بوضوح.

### المرحلة 1: الأمان واستعادة المسارات الأساسية

- F01: لا تغيير جذري في تهيئة الحساب التجريبي أثناء الاختبار، ولا حذف للحساب أو تغيير لكلمة مروره وصلاحياته الحالية. عند تجهيز الإنتاج تُفصل التهيئة التجريبية ويُوقف فرض بيانات الدخول والصلاحيات تلقائيًا على الحسابات القائمة، مع اختبارات إعادة التشغيل.
- F02 وF14: إصلاح ربط طلب المسح وتعريف حذف/أرشفة المؤسسة. اختبارات HTTP فعلية لا مجرد استدعاء الخدمة.
- F06 وF08 وF23: مصفوفة واحدة واضحة لأدوار الواجهة والخادم، واختبار كل دور منفردًا.

### المرحلة 2: المال وسلامة البيانات

- F03 وF17: سياسة دمج وتسعير موثوقة من الخادم، وتثبيت/إدارة تغيير المساحات بعد الحجز، مع اختبار تبديل BoothTypeSelected وخلاف مساحة الدمج.
- F04: منع حذف العارض صاحب الفواتير على الخادم، مع رسالة واضحة في الواجهة؛ معيار القبول: محاولة الحذف تُرفض وتبقى بيانات العارض والفواتير دون أي تغيير. اختبار المنع لفواتير المسودة والصادرة والجزئية والمدفوعة والملغاة، والسماح بالحذف عند عدم وجود فواتير وبشرط استيفاء بقية قيود العلاقات.
- F05 وF11: منع حذف/نقل المواقع التي لها التزامات، وتحديد سياسة التعامل مع تاريخ الموقع دون إخفاء المستندات. هذا مستقل عن F04 الذي اعتمد له المستخدم منع حذف العارض دون أرشفة.
- F18 وF20 وF21: عملة مصدر لكل مبلغ، تحويل موثق، تجميع بالعملة الأساسية، وعدم جمع الأرصدة التراكمية. مطابقة ويب/WPF/API لنفس البيانات.

### المرحلة 3: دورة التذاكر والمدخلات

- F09 وF10 وF16: ربط QR بمشارك/تصريح محفوظ وصالح، والتحقق من حالة المعرض والتاريخ وصلاحية الدور.
- F07 وF12 وF13 وF15 وF19: تواريخ ومدد وقيم موجبة/غير سالبة، فحص نوع JSON، وEnum.IsDefined، وإرجاع 400/409 مناسب بدل 500.

### المرحلة 4: جودة الواجهة والقبول الشامل

- F22 وF24 وF25: إزالة البيانات المصطنعة وتوحيد الحالات والتجميع والنسب.
- إضافة اختبارات متصفح للأدوار والحجز والخدمات وبنود الفاتورة والسداد والطباعة.
- اعتماد مصفوفة سيناريوهات: معرض واحد، عارض LYD وآخر USD وثالث EUR؛ حجز منفرد ودمج؛ خدمات اختيارية وإلزامية؛ فاتورة مسودة وصادرة وجزئية ومستردة؛ أرشفة الأطراف؛ إعادة الطلب؛ والدخول والخروج.
- لا يغلق التدقيق إلا بعد إصلاح النتائج وإعادة تشغيل اختبارات الخدمات وSQL وHTTP والمتصفح، وتجربة الطباعة والبريد على بيئة قبول مناسبة.

## أثر الجولة

لا إصلاحات إنتاجية أو نشر في هذه الجولة. التقرير هو مخرج التحليل. أُوقف خادم API وخادم Vite المؤقتان وأُغلق تبويب الاختبار، وحُذفت قاعدة SQL المؤقتة بنجاح بعد التحقق من اسمها الدقيق. قاعدة البيانات الرئيسية لم تُعدّل. حُذف فقط محتوى الاختبار الاصطناعي؛ يمكن إعادة إنشائه من السيناريوهات، وليس استرجاعه من سلة مهملات. لا يحتوي التقرير على بيانات الدخول الفعلية.

