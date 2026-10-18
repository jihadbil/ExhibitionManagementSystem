# المراجعة العميقة للنظام — 12 أكتوبر 2026

> نُفذت الإصلاحات البرمجية لاحقًا في الجولة نفسها. الحالة الحالية وأدلة التحقق في [تقرير الإصلاحات](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/docs/system-repairs-2026-10-12.md). التفاصيل ومواقع الكود أدناه تسجل الحالة قبل الإصلاح.

## النتيجة

النطاق المعتمد لهذه المراجعة هو تطبيق الويب وخدمات الخادم وقاعدة البيانات التي يعتمد عليها.

تتضمن المراجعة **16 نتيجة جديدة: 10 عالية P1، و5 متوسطة P2، وواحدة منخفضة P3**. أُثبتت 14 نتيجة بالتنفيذ أو بفحص الجودة، ونتيجتان بمراجعة مسار الكود. هناك أيضًا ملاحظة P0 سابقة عن تهيئة الحساب الإداري التجريبي ما زالت موجودة؛ لم تُحسب ضمن النتائج الجديدة. نجاح البناء والاختبارات الحالية لا يعني خلو النظام من الأخطاء. احتُفظ بمعرفات الملاحظات السابقة لتسهيل تتبعها.

هذه مراجعة للنسخة الحالية في مساحة العمل، بما فيها التغييرات غير المحفوظة الموجودة قبل بدء المراجعة. لم أُصلح كود التشغيل، ولم أُعد ضبط الملفات، ولم أغيّر الحسابات الحقيقية أو بيانات المؤسسات الفعلية، ولم أرسل بريدًا. الإضافات هي تقرير المراجعة وأدلة الاختبارات فقط.

## ما فُحص

- الخادم: المصادقة والجلسات والسياسات، عزل المؤسسات، المستودعات وفلاتر البيانات، الحجز وتسعير الأجنحة والخدمات، الفواتير والدفع والإشعارات، الرعاية وتخصيص المساحات، المعارض والفعاليات، القاعات والمخططات، الزوار والتذاكر والشارات والتقارير.
- الويب: توافق الطلبات مع الخادم، تخزين وتجديد الجلسة، الإعدادات، تحميل القوائم والحجوزات، ومصمما الموقع والأجنحة.
- تشغيل الاختبارات الحالية، والبناء، وفحص الجودة، وفحص الحزم. إنشاء قواعد SQL مؤقتة بأسماء فريدة لتجربة الحالات الجديدة، مع سياق مؤسسة فعلي لا يملك تجاوز عزل المؤسسات. حُذفت قواعد هذه الجولة بعد كل سيناريو.
- اختبار HTTP محلي للمقارنة بين صلاحية قراءة الفواتير وقراءة مبالغ الحجوزات من مسار العارض، باستخدام مصادقة اختبار معزولة لا تمر بمهيئ الحسابات الحقيقي.
- تشغيل AuthProvider الفعلي بعد تحويله إلى JavaScript مع بدائل صغيرة لخطافات React وتخزين اصطناعي، لإثبات استبدال رموز الجلسة الجديدة بالقديمة.

لم يُجر اختبار تحميل طويل، أو اختبار اختراق خارجي، أو تشغيل كل سباقات التزامن، أو طباعة ورقية. فحص بعض المسارات هنا كان قراءة للكود، لا قبولًا نهائيًا لكل الوظائف.

## نتائج التحقق

| الفحص | النتيجة |
|---|---|
| بناء الخادم والويب | نجاح |
| اختبارات الخادم الأصلية | 220 اختبارًا ناجحًا عند تفعيل SQL؛ لا تجاوز |
| اختبارات الويب | 61 ناجحة |
| TypeScript وبناء Vite | نجاح |
| ESLint | فشل واحد في ملف أداة المعاينة؛ D17 |
| فحص npm | صفر ثغرات معلنة وقت الفحص |
| فحص حزم الخادم، بما فيه التبعيات غير المباشرة | لا حزم ضعيفة معلنة من المصادر المهيأة وقت الفحص |
| سيناريوهات السلامة الإضافية ضمن نطاق الويب والخادم | 14 حالة: 13 خالفت السلوك الآمن المتوقع، وحالة واحدة نجحت |
| فحص جلسة الويب الإضافي | أُعيدت الرموز القديمة بعد تحديث الاسم |

الاختبارات التشخيصية تعمدت التأكيد على **السلوك الآمن المطلوب**؛ لذلك فشلها دليل على الخلل، لا فشلًا في الاختبارات الأصلية. سجل التنفيذ الأصلي محفوظ كما هو؛ الأعداد أعلاه تحسب الحالات الداخلة في النطاق المعتمد فقط. حالتا الكسور النقدية تكشفان السبب نفسه، فدُمجتا في D07. الحالة الناجحة كانت حذف باقة رعاية مع وجود عقد: العقد ظل ظاهرًا؛ لم أسجل ادعاء أنه يختفي.

## النتائج الجديدة المرتبة

### D01 — P1: كشف اسم قاعة مؤسسة أخرى عبر مساحة إعلانية

**الدليل:** SQL، بسياق مؤسسة يمنع تجاوز العزل. إنشاء مساحة بمعرض المؤسسة الأولى وHallID وVenueID تابعين لمؤسسة ثانية نجح، وأعادت الاستجابة `HallName = Foreign confidential hall`. الخدمة تتحقق من المعرض فقط قبل حفظ العلاقات، وHall لا يحمل فلتر مؤسسة مستقلًا؛ تضمينه يكشف الاسم الأجنبي. لا يثبت الاختبار قراءة جميع بيانات المؤسسة الأخرى، لكنه يثبت تسريب هذا الحقل وربطًا غير صحيح.

**الموقع:** [SponsorshipService.cs:236](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/SponsorshipService.cs:236).

**الإصلاح:** التحقق من ملكية الموقع والقاعة وتبعية القاعة لموقع المعرض قبل الإنشاء والتعديل؛ لا تعتمد على فلتر الكيان الأساسي وحده.

### D02 — P1: تعديل باقة الرعاية يربطها بمعرض مؤسسة أخرى

**الدليل:** SQL، بسياق مؤسسة محدود. `UpdatePackageAsync` قبل ExhibitionID أجنبيًا وحفظه مع بقاء TenantID للمؤسسة الأصلية. فحص مالك الباقة يسبق Mapping، لكنه لا يفحص مالك المعرض الجديد. مسار تعديل المساحة الإعلانية يستخدم النمط غير المحمي نفسه؛ إثبات هذه الجولة المباشر كان على الباقة.

**الموقع:** [SponsorshipService.cs:158](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/SponsorshipService.cs:158)، [UpdateSpaceAsync:261](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/SponsorshipService.cs:261).

**الإصلاح:** إعادة فحص العلاقات الجديدة قبل Mapping، ومنع نقل باقة أو مساحة لها عقود/تخصيصات قائمة إلى معرض مختلف.

### D03 — P1: مشغل البوابة يقرأ مبالغ الحجوزات رغم منعه من المالية

**الدليل:** HTTP فعلي على المضيف المحلي الاختباري. GateOperator حصل على 403 من `/api/financial/invoices`، وعلى 200 من `/api/exhibitors/1/reservations` مع `totalAmount=100` و`currencyCode=USD`. المسار الثاني يرث تسجيل الدخول فقط ولا يحمل سياسة إدارة الأحداث أو المالية. هذه قراءة داخل المؤسسة نفسها، وليست تسريبًا بين المؤسسات.

**الموقع:** [ExhibitorsController.cs:55](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem/Controllers/Exhibition/ExhibitorsController.cs:55).

**الإصلاح:** حماية مسار حجوزات العارض بسياسة متوافقة مع مسار الحجوزات الرئيسي، وإرجاع DTO دون حقول نقدية إذا احتاج موظف البوابة بيانات تشغيلية محدودة.

### D04 — P1: حذف الراعي يخفي عقوده المالية

**الدليل:** SQL. عقد واحد ظاهر قبل الحذف؛ `DeleteSponsorAsync` نجح، ثم أصبحت قائمة العقود فارغة، بينما بقي عقد واحد مخزنًا. Include على Sponsor المطلوب مع فلتر الحذف الناعم يسقط العقد من النتائج.

**الموقع:** [SponsorshipService.cs:92](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/SponsorshipService.cs:92)، [SponsorshipContractRepository.cs:20](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.DataAccess/Repositories/Implementations/SponsorshipContractRepository.cs:20).

**الإصلاح:** رفض حذف الراعي ذي العقود، أو اعتماد تصميم يحفظ ظهور التاريخ المالي صراحة. لا تُجر تغييرات على السجلات القائمة قبل تحديد سياسة العمل المطلوبة.

### D05 — P1: تعديل جناح مضلع دون تغيير الأبعاد يضاعف مساحته رغم الحجز

**الدليل:** SQL. جناح مثلث مساحته 10 م² وأبعاد صندوقه 2×10؛ لديه حجز. إعادة إرسال المساحة والأبعاد الحالية اجتازت `affectsPricing`، ثم ضربت الخدمة Width×Height فحفظت المساحة 20. يستبدل الحساب مساحة المضلع بمساحة المستطيل، بعد فحص القفل.

**الموقع:** [BoothService.cs:183](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/BoothService.cs:183)، [حساب المساحة:201](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/BoothService.cs:201).

**الإصلاح:** حساب المساحة النهائية من الهندسة الفعلية أولًا، ثم مقارنة القيمة المحسوبة بالمحفوظة وتطبيق قفل الحجز/الدمج. استخدم المسار الهندسي نفسه في الحفظ الفردي وحفظ المخطط.

### D07 — P1: الكسور النقدية تغيّر الدفعة عند الحفظ وتكسر حالة الفاتورة والتحويل

**الدليل:** SQL. دفعة 0.015 USD نجحت وأعيدت كـ0.015، لكن المخزن 0.02 وقيمة العملة الأساسية 0.08 بسعر 5، بدل القيمة المتوافقة مع 0.02. دفعة 99.995 على فاتورة 100 حُفظت كـ100.00، والتحويل 499.98، وظلت الحالة PartiallyPaid. التحقق وحساب الحالة يجريان قبل تقريب SQL إلى منزلتين؛ الاستجابة تقرأ الكيان المتتبع بقيمه الأصلية.

**الموقع:** [FinancialService.cs:329](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/FinancialService.cs:329)، [PaymentCreateDto.cs:9](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Models.DTOs/Financial/PaymentCreateDto.cs:9).

**الإصلاح:** رفض دقة نقدية غير مدعومة أو تطبيع المبلغ مرة واحدة قبل فحص الرصيد والحالة والتحويل. طبّق سياسة الدقة نفسها على الإشعارات الدائنة والمبالغ المالية الأخرى؛ Range لا يحدد عدد المنازل العشرية.

### D08 — P1: تحديث الملف الشخصي يعيد رموز جلسة ملغاة بعد التجديد

**الدليل:** تشغيل AuthProvider الفعلي مع تخزين اصطناعي. بعد `updateSessionTokens` ظهر new-access/new-refresh؛ استدعاء `updateCurrentUser` أعاد old-access/old-refresh. React state يحتفظ باستجابة الدخول الأولى بينما التجديد يحدث في التخزين فقط. SettingsPage يستدعي المسار عند حفظ الاسم؛ الخادم يعتبر إعادة استخدام رمز التجديد القديم سببًا لسحب جلسات المستخدم.

**الموقع:** [AuthContext.tsx:43](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/exhibitionmanagementsystem.web/src/app/providers/AuthContext.tsx:43)، [api.ts:133](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/exhibitionmanagementsystem.web/src/services/api.ts:133).

**الإصلاح:** توحيد مصدر حالة الجلسة، وتحديث حالة React عند تجديد الرموز. تحديث الاسم يجب أن يدمج مع أحدث جلسة مخزنة ويحافظ على رموزها الحالية.

### D09 — P1: قبول عقود تتجاوز سعة باقة الرعاية

**الدليل:** SQL. باقة MaxSponsorsCount=1 ولها عقد قائم؛ إنشاء عقد ثانٍ نجح، وأصبح العدد 2. `CreateContractAsync` يفحص وجود الباقة وتبعيتها، دون فحص السعة أو IsActive.

**الموقع:** [SponsorshipService.cs:334](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/SponsorshipService.cs:334).

**الإصلاح:** فحص فعالية الباقة وعدد العقود المحتسبة داخل معاملة تمنع تجاوز السعة بالتزامن، وتحديد الحالات التي تحجز المقعد.

### D10 — P1: حفظ مخطط الموقع يضيّق حدود القاعة حول أجنحة قائمة

**الدليل:** SQL. مخطط schemaVersion=4 صحيح هندسيًا حفظ حدود قاعة 1×1، مع بقاء جناح محجوز أبعاده 2×10 داخلها. حفظ الموقع لا يراجع الأجنحة في القاعات؛ التحقق من احتواء الأجنحة يجري عند حفظ الأجنحة لاحقًا فقط.

**الموقع:** [VenueService.cs:262](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/VenueService.cs:262).

**الإصلاح:** قبل حفظ حدود القاعات والعوائق، افحص احتواء الأجنحة القائمة وعدم تقاطعها مع العوائق الجديدة. نسّق هذا الفحص مع معاملات حفظ الأجنحة حتى لا تنجح عمليتان متعارضتان.

### D11 — P2: تعديل القاعة يقبل أبعادًا سالبة وJSON تالفًا

**الدليل:** SQL. PUT المكافئ للخدمة قبل FloorPlanWidth=-1 وFloorPlanJSON=`not-json` وحفظهما. DTO بلا قيود لهذه الحقول؛ الخدمة تنفذ Mapping والحفظ مباشرة، متجاوزة تحقق المصمم المتخصص.

**الموقع:** [HallService.cs:76](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/HallService.cs:76)، [HallUpdateDto.cs:14](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Models.DTOs/Hall/HallUpdateDto.cs:14).

**الإصلاح:** منع الحفظ العام من الكتابة المباشرة للهندسة، أو مشاركة التحقق من البنية والحجم والأبعاد والتعارض بين مسارات الحفظ العامة ومسارات المصمم في الخادم.

### D12 — P2: تغيير مدة المعرض يترك فعالياته خارج المدة الجديدة

**الدليل:** SQL. فعالية في اليوم +4؛ تحديث نهاية المعرض إلى +1 نجح وبقيت الفعالية كما هي. توجد حماية عند إنشاء/تعديل الفعالية، لكنها لا تعمل عند تعديل المعرض الأب. يمكن أن تتأثر أيام التذاكر أيضًا بالنمط نفسه؛ اختبار هذه الجولة كان على الفعالية.

**الموقع:** [ExhibitionService.cs:151](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/ExhibitionService.cs:151).

**الإصلاح:** رفض تقليص المدة بما يستبعد فعاليات أو أيام تذاكر قائمة، أو تنفيذ تعديل مترابط واضح داخل معاملة وفق سياسة العمل.

### D13 — P2: حذف فعالية لها تسجيلات يلقي استثناء قاعدة بيانات

**الدليل:** SQL. فعالية لها ScheduleRegistration واحد؛ `RemoveScheduleAsync` ألقى DbUpdateException بسبب المفتاح الخارجي Restrict. لا تعالج الخدمة الحالة؛ مع GlobalExceptionHandler الحالي تصبح استجابة المسار HTTP 500. الاستثناء أُعيد إنتاجه بالخدمة؛ توصيف HTTP هنا مستنتج من معالجة الاستثناء الحالية.

**الموقع:** [ExhibitionService.cs:357](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/ExhibitionService.cs:357).

**الإصلاح:** فحص التسجيلات وإرجاع تعارض مفهوم، أو توفير إلغاء يحفظ تاريخ التسجيل. لا تحذف التسجيلات تلقائيًا دون سياسة عمل.

### D14 — P2: العقد الملغى يبقي باقة الرعاية ممتلئة في البيانات المعروضة

**الدليل:** SQL. إلغاء العقد الوحيد نجح؛ `GetPackageByIdAsync` أعاد AllocatedSponsorsCount=1 وIsSoldOut=true. Mapping يحسب جميع العقود دون استبعاد Cancelled. هذا مستقل عن غياب فحص السعة في D09: الأول يخطئ في العرض، والثاني في القبول.

**الموقع:** [SponsorshipMappingProfile.cs:18](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Models.DTOs/Mapping/Profiles/SponsorshipMappingProfile.cs:18).

**الإصلاح:** حساب السعة المحجوزة باستخدام حالات العقود المحددة في قاعدة العمل، ومشاركة الحساب بين DTO وفحص إنشاء العقد.

### D15 — P1: استعادة كلمة المرور تعد بإرسال رسالة دون مسار إرسال

**الدليل:** مراجعة كاملة لمسار الكود. `ForgotPasswordAsync` يولد الرمز ويرجعه في ServiceResult فقط؛ AuthController يتجاهله ويعيد رسالة «ستصلك رسالة». لا يوجد استدعاء مرسل بريد لهذا المسار؛ مرسل SMTP الموجود مخصص للدعوات. لم أرسل رسالة فعلية أو أغيّر كلمة مرور أثناء المراجعة.

**الموقع:** [AuthService.cs:291](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/AuthService.cs:291)، [AuthController.cs:82](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem/Controllers/Auth/AuthController.cs:82).

**الإصلاح:** إضافة إرسال رابط الاستعادة عبر قناة معتمدة أو قائمة انتظار موثوقة، مع الاحتفاظ بالاستجابة العامة التي لا تكشف وجود الحساب وتسجيل فشل التسليم داخليًا.

### D16 — P2: صفحات الويب تقطع القوائم عند أول صفحة

**الدليل:** مراجعة الكود. الحجوزات تطلب page=1&pageSize=100 فقط وتبحث/تفلتر العناصر المحلية دون تنقل صفحات. الشركات تقف عند 150، والمستخدمون عند 100، وقائمة الزوار في التذاكر عند 200. العناصر التالية موجودة في الخادم لكنها لا تدخل القائمة أو البحث أو اختيار الزائر. تحميل الأجنحة في الحجوزات يستخدم حلقة صفحات بالفعل، لكن بقية هذه القوائم لا تفعل ذلك.

**الموقع:** [ReservationsPage.tsx:175](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/exhibitionmanagementsystem.web/src/features/reservations/pages/ReservationsPage.tsx:175)، [CompaniesPage.tsx:98](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/exhibitionmanagementsystem.web/src/features/companies/pages/CompaniesPage.tsx:98)، [UsersPage.tsx:80](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/exhibitionmanagementsystem.web/src/features/users/pages/UsersPage.tsx:80)، [TicketsPage.tsx:157](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/exhibitionmanagementsystem.web/src/features/tickets/pages/TicketsPage.tsx:157).

**الإصلاح:** إضافة صفحات وبحث على الخادم مع totalCount، واستخدام اختيار يبحث عن الزائر/العارض بدل تنزيل أول عدد ثابت فقط.

### D17 — P3: فحص جودة الويب يفشل بسبب استيراد غير مستخدم

**الدليل:** `npm run lint` أعاد خطأ `@typescript-eslint/no-unused-vars` في rectanglePoints، مع نجاح بناء Vite. موضعه في أداة معاينة الموقع، لكنه ضمن الفحص الرسمي للمشروع.

**الموقع:** [preview-api.ts:10](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/exhibitionmanagementsystem.web/tools/site-studio-preview/preview-api.ts:10).

**الإصلاح:** حذف الاستيراد غير المستخدم أو استخدامه إذا كان جزءًا مطلوبًا من المعاينة.

## الملاحظة السابقة التي ما زالت قائمة

ملاحظة **F01 / P0** في [تدقيق 8 أكتوبر](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/docs/system-audit-2026-10-08.md) عن حساب إداري معروف وتهيئته المتكررة ما زالت موجودة؛ أُعيد فحص الكود الحالي في [DataSeeder.cs:116](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.DataAccess/Seeder/DataSeeder.cs:116)، وهو يعيد كلمة المرور والأدوار والتفعيل. يسجل [تقرير الإصلاحات السابق](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/docs/system-repairs-2026-10-08.md) أنها مؤجلة بطلب المستخدم أثناء الاختبار. لم أعد اختبار كلمة المرور على حساب حقيقي، ولم أغير هذا المسار. يظل فصل تهيئة الاختبار عن تشغيل الإنتاج عملًا مطلوبًا قبل النشر.

## ترتيب الإصلاح المقترح

1. إغلاق علاقات الرعاية التي تعبر المؤسسات، ثم تسريب مبالغ الحجوزات: D01–D03.
2. تصحيح حفظ الأموال والجلسة وحماية التاريخ المالي: D04 وD07 وD08، وإكمال الاستعادة D15.
3. توحيد تحقق الهندسة بين مسارات الحفظ في الويب والخادم: D05 وD10 وD11.
4. ضبط سعة الرعاية ومدة المعرض وحذف الفعاليات: D09 وD12–D14.
5. إصلاح اكتمال القوائم وفحص الجودة: D16 وD17، ثم تشغيل الاختبارات المعيارية الجديدة مع الاختبارات الأصلية.

## الأدلة القابلة لإعادة التشغيل

- [سجل الحالات الإضافية](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/docs/deep-audit-2026-10-12-probes.txt): نتائج SQL وHTTP والبيانات الاصطناعية فقط.
- [مصدر الحالات التشخيصية](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/docs/deep-audit-2026-10-12-probes.cs.txt): أُخرج من مشروع الاختبارات بعد المراجعة حتى لا تبقى الاختبارات المعروفة الفشل ضمن التشغيل العادي. لإعادة الفحص يُنسخ إلى مجلد Regression في مشروع الاختبارات ويُشغّل فلتر DeepAudit20261012Probes بعد تعيين EMS_SQL_TEST_CONNECTION لخادم اختبار يسمح بإنشاء قواعد مؤقتة. يعتمد على أدوات الاختبار الموجودة في المشروع؛ لا يشغّل Program أو DataSeeder.
- [فحص AuthProvider](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/docs/deep-audit-2026-10-12-auth-probe.mjs)، [نتيجته](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/docs/deep-audit-2026-10-12-auth-probe.txt): يشغّل باستخدام Node مع `--experimental-strip-types` بعد تثبيت تبعيات الويب.
- [سجل الاختبارات الأصلية مع SQL](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/docs/deep-audit-2026-10-12-baseline-tests.txt).
- [نتيجة npm](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/docs/deep-audit-2026-10-12-npm.json)، [نتيجة NuGet](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/docs/deep-audit-2026-10-12-nuget.txt).

هذه النتائج لا تعني أن عدد أخطاء تطبيق الويب وخادمه الكامل 16 فقط؛ هي النتائج المثبتة أو المحسومة في نطاق هذه الجولة.
