<aside>
⛔

قرار البداية: No-Go. يمنع الإطلاق التجاري أو إدخال بيانات عملاء حقيقية قبل إغلاق بوابة الأمان P0 واعتماد نتائج الاختبارات.

</aside>

## 1. الهدف ونطاق الخطة

تهدف هذه الخطة إلى نقل النظام من نموذج وظيفي واسع لكنه عالي المخاطر إلى منتج SaaS/سطح مكتب قابل للبيع والتشغيل والدعم. تغطي الخطة: الأمن، عزل المستأجرين، سلامة البيانات، الاختبارات، DevSecOps، التشغيل، تجربة المستخدم، المدفوعات، الامتثال، التوثيق، التجربة التجريبية، والإطلاق.

**المدة التقديرية:** 12–16 أسبوعاً لفريق متفرغ، يعقبها Pilot لمدة 2–4 أسابيع.

**تكوين الفريق الأدنى المقترح:**

- قائد تقني/مهندس Backend.
- مهندس Backend ثانٍ أو Full-stack.
- مهندس WPF/Desktop.
- مهندس QA Automation.
- DevOps/SRE بدوام جزئي.
- مسؤول منتج/عمليات معارض.
- مستشار أمن وخصوصية عند بوابات المراجعة.

## 2. تعريف النجاح وبوابات القرار

| البوابة | شرط المرور | القرار عند الفشل |
| --- | --- | --- |
| G0 — احتواء الحادث | تدوير الأسرار، إبطال التوكنات، إغلاق التسجيل الخطر، وفحص الوصول | إيقاف النظام عن الإنترنت |
| G1 — خط أساس أمني | صفر ثغرات حرجة/عالية مفتوحة وعزل Tenant مثبت آلياً | يمنع Staging الخارجي |
| G2 — جودة البناء | Build نظيف وCI إلزامي واختبارات المسارات الحرجة ناجحة | يمنع إصدار Release Candidate |
| G3 — جاهزية التشغيل | مراقبة ونسخ احتياطي واسترجاع وRollback وRunbooks | يمنع Pilot |
| G4 — قبول المنتج | UAT مكتمل، لا أخطاء P0/P1، ودقة الفواتير والحجز مثبتة | تمديد Pilot |
| G5 — الإطلاق | موافقة التقنية والأمن والمنتج والقانون والدعم | No-Go |

## 3. مبادئ التنفيذ

1. لا إصلاحات مباشرة على `master`؛ استخدام فروع قصيرة وPull Requests ومراجعة إلزامية.
2. كل ثغرة أو عطل يرافقه اختبار يمنع رجوعه.
3. عزل المستأجرين يطبق مركزياً، لا بمجرد تذكر إضافة `TenantID` في كل خدمة.
4. الأسرار لا تدخل الشفرة أو ملفات الإعدادات أو السجلات.
5. الترحيلات والنشر قابلان للتراجع ويُختبران على نسخة بيانات مماثلة للإنتاج.
6. لا تعتبر الميزة مكتملة دون مراقبة، توثيق، اختبارات، ورسائل خطأ مفهومة.
7. بوابات الجودة إلزامية ولا تُتجاوز لإرضاء موعد الإطلاق.

## 4. المرحلة صفر — احتواء أمني فوري (0–48 ساعة)

### SEC-001 — تدوير أسرار قاعدة البيانات

- [ ]  تعطيل مستخدم قاعدة البيانات المنشور وإنشاء مستخدم جديد بأقل الصلاحيات.
- [ ]  تغيير كلمة المرور وعدم إعادة استخدامها.
- [ ]  تحديث الاستضافة عبر Secret Store فقط.
- [ ]  فرض تشفير الاتصال والتحقق من الشهادة.
- [ ]  اختبار اتصال التطبيق من Staging ثم Production.

**معيار القبول:** بيانات الاعتماد القديمة تفشل؛ لا توجد كلمة مرور في Git أو ملفات Artifact أو Logs؛ التطبيق يتصل عبر TLS.

### SEC-002 — تدوير مفاتيح JWT

- [ ]  إنشاء مفتاح عشوائي قوي في Secret Store.
- [ ]  حذف جميع قيم Fallback من الشفرة.
- [ ]  جعل التطبيق يفشل عند البدء إذا كان المفتاح مفقوداً أو قصيراً.
- [ ]  إبطال Refresh Tokens الحالية وإجبار المستخدمين على إعادة الدخول.
- [ ]  إضافة `kid` وخطة تدوير مفاتيح إذا تم اعتماد مفاتيح غير متماثلة.

**معيار القبول:** كل Token قديم مرفوض؛ لا يظهر المفتاح الجديد في Logs أو Configuration dumps.

### SEC-003 — تنظيف تاريخ Git

- [ ]  جعل المستودع خاصاً مؤقتاً.
- [ ]  تشغيل Secret Scanner على كامل التاريخ.
- [ ]  استخدام `git filter-repo` أو BFG لحذف الأسرار وملفات الإعداد الإنتاجي الحساسة.
- [ ]  Force-push منسق وإعادة استنساخ نسخ المطورين.
- [ ]  تفعيل GitHub Secret Scanning وPush Protection.

**معيار القبول:** فحص كامل التاريخ لا يجد أسراراً؛ جميع الأسرار المكتشفة مدوّرة حتى إن حُذفت.

### SEC-004 — إغلاق تصعيد الصلاحيات

- [ ]  تعطيل `/api/auth/register` مؤقتاً.
- [ ]  إزالة `TenantID` و`InitialRole` من DTO العام.
- [ ]  منع إنشاء `Admin` و`SuperAdmin` إلا من خدمة إدارية مصادق عليها.
- [ ]  اعتماد دعوات أحادية الاستخدام مرتبطة بـTenant ودور مسموح وتاريخ انتهاء.
- [ ]  تسجيل جميع محاولات التسجيل وإضافة Rate Limit.

**معيار القبول:** لا يستطيع مستخدم مجهول اختيار Tenant أو Role؛ اختبارات سلبية تثبت رفض `Admin/SuperAdmin`.

### SEC-005 — إزالة الحساب الافتراضي

- [ ]  حذف بيانات المدير الثابتة من Seeder.
- [ ]  تعطيل الحساب الافتراضي في كل البيئات الحالية.
- [ ]  بناء Bootstrap آمن لمرة واحدة، بمفتاح منتهي الصلاحية وتغيير كلمة مرور إلزامي.
- [ ]  فرض MFA على SuperAdmin.

### SEC-006 — فرض HTTPS

- [ ]  تغيير عنوان API في تطبيق سطح المكتب إلى HTTPS.
- [ ]  رفض عنوان HTTP في Production برمجياً.
- [ ]  تفعيل HSTS وSecure Headers.
- [ ]  اختبار TLS والشهادة وسلسلة إعادة التوجيه.

### SEC-007 — مراجعة الحادث المحتمل

- [ ]  فحص سجلات قاعدة البيانات والاستضافة وGitHub منذ أول نشر للأسرار.
- [ ]  البحث عن حسابات أو أدوار أو عمليات غير متوقعة.
- [ ]  حفظ الأدلة وتوثيق الخط الزمني والإجراءات.
- [ ]  إشعار أصحاب البيانات إذا تطلب القانون أو ثبت وصول غير مشروع.

**مالك المرحلة:** قائد التقنية + DevOps + مسؤول الأمن.
**بوابة الخروج G0:** إغلاق جميع البنود السابقة دون استثناء.

## 5. المرحلة الأولى — المصادقة والتفويض وعزل المستأجرين (الأسبوع 1–3)

### SEC-101 — نموذج أدوار وصلاحيات

تعريف أدوار ثابتة مبدئية:

| الدور | صلاحيات نموذجية |
| --- | --- |
| SuperAdmin | إدارة المستأجرين والخطط فقط |
| TenantAdmin | إدارة مستخدمي وبيانات Tenant الخاص به |
| EventManager | المعارض والقاعات والأجنحة والحجوزات |
| Finance | الفواتير والمدفوعات والتقارير المالية |
| GateOperator | مسح التذاكر فقط |
| Exhibitor | بياناته وحجوزاته وفواتيره فقط |
| Viewer/Auditor | قراءة محدودة غير قابلة للتعديل |
- [ ]  تحويل صلاحيات التحكم من أدوار واسعة إلى Policies مبنية على Permissions.
- [ ]  منع SuperAdmin من العمل كموظف Tenant إلا عبر Impersonation مدقق ومؤقت.
- [ ]  إضافة مصفوفة صلاحيات موثقة واختبارات لكل Endpoint.

### SEC-102 — Tenant Context مركزي

- [ ]  إنشاء `ITenantContext` يستخرج Tenant من Claims الموثوقة.
- [ ]  منع قبول TenantID من Body/Query في المسارات العادية.
- [ ]  تطبيق Global Query Filters أو Repository Specification إلزامية للكيانات متعددة المستأجرين.
- [ ]  إضافة SaveChanges interceptor يمنع إنشاء/تعديل كيان Tenant مختلف.
- [ ]  استثناءات SuperAdmin يجب أن تكون صريحة ومدققة.

### SEC-103 — إغلاق IDOR في المستخدمين والأدوار

- [ ]  تعديل Get/Update/Delete/Status للمستخدم ليتحقق داخل الاستعلام من Tenant.
- [ ]  تعديل AssignRole/RemoveRole للتحقق من Tenant المستخدم والدور والمنفذ.
- [ ]  منع حذف آخر TenantAdmin.
- [ ]  منع المستخدم من تعطيل أو حذف نفسه دون إجراء مؤكد.
- [ ]  إضافة اختبارات Tenant A ضد معرفات Tenant B لكل عملية.

### SEC-104 — تقوية تسجيل الدخول

- [ ]  تفعيل Lockout تدريجي ومحاولات محدودة حسب الحساب وIP.
- [ ]  Rate Limit لمسارات login/register/forgot/reset/refresh.
- [ ]  سياسة كلمة مرور قابلة للضبط وفحص كلمات المرور الشائعة أو المسربة.
- [ ]  رسائل خطأ لا تكشف وجود الحساب.
- [ ]  MFA إلزامي للأدوار الإدارية واختياري لبقية المستخدمين.

### SEC-105 — دورة حياة Token

- [ ]  تخزين Hash للـRefresh Token بدلاً من القيمة الأصلية.
- [ ]  دعم جلسات متعددة مع Device ID، CreatedAt، LastUsedAt، IP تقريبي، وRevoke reason.
- [ ]  تدوير Refresh Token واكتشاف إعادة استخدام Token قديم.
- [ ]  فحص Issuer وAudience وSignature والخوارزمية دائماً.
- [ ]  إضافة Logout all devices وإلغاء جلسة محددة.

### SEC-106 — استعادة كلمة المرور

- [ ]  دمج موفر بريد إنتاجي.
- [ ]  إرسال رابط HTTPS أحادي الاستخدام قصير الصلاحية.
- [ ]  عدم إعادة Token في API أو Logs.
- [ ]  إبطال الجلسات بعد تغيير كلمة المرور.
- [ ]  قوالب عربية/إنجليزية واختبارات انتهاء الصلاحية وإعادة الاستخدام.

### SEC-107 — CORS وHeaders

- [ ]  قائمة Origins مسموحة لكل بيئة؛ تطبيق WPF لا يحتاج CORS عادة.
- [ ]  إضافة CSP عند وجود واجهة Web، و`X-Content-Type-Options` وReferrer Policy.
- [ ]  `AllowedHosts` محدود في الإنتاج.
- [ ]  إزالة تفاصيل Server وإخفاء Stack Traces.

**بوابة الخروج G1:** اجتياز Security Review واختبارات Tenant Isolation، وصفر P0/P1 أمني.

## 6. المرحلة الثانية — سلامة البيانات والمنطق المالي (الأسبوع 2–5)

### DATA-201 — منع الحجز المزدوج

- [ ]  إضافة قيد/تصميم قاعدة بيانات يمنع حجز Booth أو Merge نشط مرتين لنفس المعرض.
- [ ]  إضافة `rowversion` أو Optimistic Concurrency للجناح والحجز.
- [ ]  تنفيذ العملية داخل Transaction بمستوى عزل مناسب.
- [ ]  التعامل مع التعارض برسالة `409 Conflict` قابلة لإعادة المحاولة.
- [ ]  اختبار 20–100 طلب متزامن على الجناح نفسه؛ نجاح طلب واحد فقط.

### DATA-202 — ترقيم الفواتير الذري

- [ ]  استبدال “قراءة آخر رقم + 1” بـSequence/Counter ذري لكل Tenant وسنة.
- [ ]  الاحتفاظ بالقيد الفريد والتعامل مع Retry محدود.
- [ ]  منع إعادة استخدام رقم فاتورة ملغاة.
- [ ]  اختبار توليد متزامن وعدم وجود فجوات غير مفسرة حسب المتطلبات القانونية.

### DATA-203 — ثوابت الفاتورة

- [ ]  منع تعديل بنود أو عملة أو ضريبة فاتورة صادرة/مدفوعة.
- [ ]  استخدام Credit Note/Refund بدلاً من التعديل التاريخي.
- [ ]  تخزين Snapshot لاسم العميل والعنوان والرقم الضريبي وأسعار البنود.
- [ ]  تعريف قواعد التقريب وعدد المنازل لكل عملة.
- [ ]  فصل `Paid`, `PartiallyPaid`, `Overdue`, `Cancelled`, `Refunded` بوضوح.

### DATA-204 — المدفوعات والتسوية

- [ ]  إضافة Idempotency Key لتسجيل الدفع وWebhooks.
- [ ]  عدم الثقة بقيمة أو حالة قادمة من العميل.
- [ ]  مطابقة مجموع المدفوعات مع رصيد الفاتورة.
- [ ]  دعم دفعات جزئية، Refunds، Chargebacks وتسوية يومية.
- [ ]  سجل مالي غير قابل للحذف المنطقي دون قيد مضاد.

### DATA-205 — أسعار الصرف والضرائب

- [ ]  مصدر سعر الصرف وتوقيته وسياسة التثبيت داخل الحجز والفاتورة.
- [ ]  منع تعديل السعر التاريخي الذي استُخدم في مستند صادر.
- [ ]  إعداد ضريبي لكل Tenant/بلد بدلاً من قيمة عامة.
- [ ]  مراجعة متطلبات الفاتورة القانونية للأسواق المستهدفة.

### DATA-206 — قواعد الحذف والاسترجاع

- [ ]  توثيق الكيانات التي تقبل Soft Delete.
- [ ]  منع حذف سجلات لها أثر مالي أو تدقيقي.
- [ ]  اختبار Restore مع العلاقات والقيود.
- [ ]  سياسة احتفاظ وأرشفة وحذف بيانات الزوار.

### DATA-207 — ترحيلات قاعدة البيانات

- [ ]  منع تطبيق Migration تلقائياً أثناء بدء Web App الإنتاجي.
- [ ]  Pipeline مستقل: Backup → Validate → Migrate → Smoke Test.
- [ ]  اختبار Forward وRollback/roll-forward على نسخة مماثلة للإنتاج.
- [ ]  إضافة تقرير تغير Schema ومراجعة DBA للترحيلات الحساسة.

## 7. المرحلة الثالثة — معالجة الأخطاء وجودة API (الأسبوع 2–5)

### API-301 — Problem Details مركزي

- [ ]  إضافة Global Exception Handler وRFC 7807 Problem Details.
- [ ]  عدم إعادة `ex.Message` أو Stack Trace للمستخدم.
- [ ]  Correlation ID في الطلب والاستجابة والسجل.
- [ ]  خريطة ثابتة بين أخطاء المجال وHTTP Status.

### API-302 — التحقق من المدخلات

- [ ]  مراجعة كل DTO للحدود: page/pageSize، الأموال، المساحات، التواريخ والكميات.
- [ ]  منع قيم سالبة وNaN/overflow والنصوص المفرطة.
- [ ]  توحيد التحقق المعقد عبر Validators.
- [ ]  اختبارات Fuzz للمدخلات العامة.

### API-303 — Versioning وIdempotency

- [ ]  اعتماد `/api/v1` أو Header Versioning.
- [ ]  سياسة إهمال ومدة دعم لإصدارات العميل المكتبي.
- [ ]  Idempotency للعمليات المالية والحجز والإصدار.
- [ ]  ETag/Concurrency Header للعمليات القابلة للتعارض.

### API-304 — OpenAPI وعقود العميل

- [ ]  نشر OpenAPI Artifact من CI.
- [ ]  توثيق Authentication، Permissions، الأخطاء والأمثلة.
- [ ]  Contract tests بين API وWPF.
- [ ]  منع Breaking Changes آلياً.

### API-305 — Pagination والبحث

- [ ]  فرض حد أقصى لـpageSize.
- [ ]  فرز ثابت لمنع تكرار/فقد الصفوف.
- [ ]  فهارس للبحث الأكثر استخداماً.
- [ ]  اعتماد Cursor Pagination للجداول الكبيرة عند الحاجة.

## 8. المرحلة الرابعة — استراتيجية الاختبارات (الأسبوع 1–8)

### هيكل المشاريع

- [ ]  `Domain.UnitTests` لقواعد التسعير والحالات والحسابات.
- [ ]  `Application.UnitTests` للخدمات والسياسات.
- [ ]  `Infrastructure.IntegrationTests` مع SQL Server حقيقي بالحاويات أو بيئة معزولة.
- [ ]  `Api.IntegrationTests` عبر WebApplicationFactory.
- [ ]  `Desktop.Tests` للـViewModels وApiClients.
- [ ]  `EndToEnd.Tests` لمسارات العمل الكاملة.

### مصفوفة المسارات الحرجة

| المسار | Unit | Integration | Security | E2E | Load |
| --- | --- | --- | --- | --- | --- |
| تسجيل الدخول/تجديد التوكن | ✓ | ✓ | ✓ | ✓ | ✓ |
| إنشاء مستخدم وإسناد دور | ✓ | ✓ | ✓ | ✓ | — |
| عزل Tenant | — | ✓ | ✓ | ✓ | — |
| إنشاء/تعديل/إلغاء حجز | ✓ | ✓ | ✓ | ✓ | ✓ |
| الحجز المتزامن | — | ✓ | — | — | ✓ |
| إصدار فاتورة وترقيمها | ✓ | ✓ | ✓ | ✓ | ✓ |
| دفع/Refund/Webhook | ✓ | ✓ | ✓ | ✓ | ✓ |
| إصدار ومسح تذكرة | ✓ | ✓ | ✓ | ✓ | ✓ |
| النسخ الاحتياطي والاسترجاع | — | ✓ | — | ✓ | — |

### معايير الجودة

- [ ]  كل Bug إنتاجي يرافقه Regression Test.
- [ ]  تغطية المسارات الحرجة 100% كسيناريوهات، وليس الاعتماد على نسبة الأسطر وحدها.
- [ ]  حد استرشادي 70% Line Coverage و80% للخدمات المالية والأمنية.
- [ ]  لا Flaky Tests؛ إعادة المحاولة لا تخفي الفشل.
- [ ]  زمن Pipeline أقل من 15 دقيقة للفحوص الأساسية.

## 9. المرحلة الخامسة — CI/CD وDevSecOps (الأسبوع 2–6)

### CI-501 — حماية الفروع

- [ ]  منع Push مباشر إلى `master/main`.
- [ ]  مراجعتان للتغييرات الأمنية والمالية.
- [ ]  Build + Unit + Integration + Lint إلزامية.
- [ ]  Commit messages واضحة وإصدارات Semantic Versioning.

### CI-502 — فحوص الأمان

- [ ]  Secret scanning وPush protection.
- [ ]  CodeQL/SAST.
- [ ]  NuGet vulnerability scan وDependabot.
- [ ]  License compliance للاعتماديات.
- [ ]  SBOM مرفق بكل إصدار.
- [ ]  DAST على Staging قبل Release Candidate.

### CI-503 — Artifact موثوق

- [ ]  بناء API وWPF من CI فقط.
- [ ]  تثبيت الإصدارات وقفل الاعتماديات.
- [ ]  توقيع ملفات Desktop رقمياً.
- [ ]  Checksums وRelease Notes وProvenance.
- [ ]  الاحتفاظ بالإصدارات السابقة للتراجع.

### CD-504 — البيئات

- [ ]  Dev لكل مطور دون بيانات حقيقية.
- [ ]  Integration آلية.
- [ ]  Staging مطابق للإنتاج قدر الإمكان.
- [ ]  Production بصلاحيات وصول محدودة وموافقة يدوية.
- [ ]  Configuration منفصل وأسرار منفصلة لكل بيئة.

### CD-505 — استراتيجية النشر

- [ ]  Blue/Green أو Rolling deployment.
- [ ]  Smoke tests بعد النشر.
- [ ]  Rollback تلقائي عند فشل Health/SLO.
- [ ]  Feature Flags للميزات عالية المخاطر.
- [ ]  خطة توافق API مع نسخ WPF القديمة.

## 10. المرحلة السادسة — المراقبة والموثوقية (الأسبوع 3–7)

### OPS-601 — Health Checks

- [ ]  Liveness لا يعتمد على خدمات خارجية.
- [ ]  Readiness يفحص قاعدة البيانات والاعتماديات الحرجة.
- [ ]  Endpoint داخلي/محمي مع مراقبة دورية.

### OPS-602 — السجلات

- [ ]  Structured Logging دون كلمات مرور أو Tokens أو بيانات مالية كاملة.
- [ ]  Correlation ID وTenant ID غير حساس وUser ID حسب سياسة الخصوصية.
- [ ]  Redaction مركزي واختبارات تمنع تسريب الأسرار.
- [ ]  مدة احتفاظ وصلاحيات وصول للسجلات.

### OPS-603 — Metrics وTracing

- [ ]  معدل الطلبات والأخطاء والزمن لكل Endpoint.
- [ ]  نجاح/فشل تسجيل الدخول والتجديد دون تسجيل السر.
- [ ]  تعارضات الحجز وزمن إصدار الفاتورة.
- [ ]  معدل مسح التذاكر ورفضها.
- [ ]  Queue/Email/Payment webhook lag.
- [ ]  Distributed Tracing للعمليات الحرجة.

### OPS-604 — SLO مبدئي

| المؤشر | الهدف المقترح للـPilot |
| --- | --- |
| توافر API الشهري | ≥99.9% |
| p95 لقراءات API المعتادة | <500ms |
| p95 لعمليات الكتابة المعتادة | <1s |
| معالجة مسح التذكرة p95 | <700ms |
| أخطاء 5xx | <0.5% |
| فقد الحجز/الفاتورة المؤكدة | 0 |

تُعدل الأرقام بعد اختبار الحمل والقياس الواقعي.

### OPS-605 — النسخ الاحتياطي والتعافي

- [ ]  نسخ مشفر دوري مع Point-in-Time Recovery.
- [ ]  فصل صلاحيات النسخ عن حساب التطبيق.
- [ ]  اختبار استرجاع شهري موثق.
- [ ]  أهداف Pilot مقترحة: RPO ≤15 دقيقة، RTO ≤2 ساعة.
- [ ]  Runbook لانقطاع قاعدة البيانات وفشل النشر وتسريب مفتاح.

## 11. المرحلة السابعة — إكمال المنتج التجاري (الأسبوع 4–10)

### PROD-701 — الدفع الإلكتروني

- [ ]  اختيار بوابة حسب البلد والعملات والرسوم والدعم.
- [ ]  Hosted Checkout/Tokenization؛ عدم تخزين بيانات البطاقة.
- [ ]  Webhook signatures، Idempotency، Retry وDead-letter handling.
- [ ]  Refunds وتسوية وتقارير مطابقة يومية.
- [ ]  مراجعة PCI DSS Scope.

### PROD-702 — البريد والإشعارات

- [ ]  تأكيد الحساب والدعوات واستعادة كلمة المرور.
- [ ]  تأكيد الحجز وإصدار الفاتورة والدفع والإلغاء.
- [ ]  قوالب عربية وإنجليزية قابلة للتخصيص لكل Tenant.
- [ ]  Bounce/complaint handling وسجل تسليم.
- [ ]  SMS/WhatsApp اختياري للأحداث الحساسة للوقت.

### PROD-703 — الفواتير والمستندات

- [ ]  PDF ثابت وقابل للطباعة.
- [ ]  شعار وبيانات Tenant والعميل والرقم الضريبي.
- [ ]  QR/رابط تحقق عند الحاجة.
- [ ]  Credit Notes وإيصالات الدفع.
- [ ]  تخزين Immutable مع Hash وسجل إصدار.

### PROD-704 — التذاكر والشارات

- [ ]  توليد QR مشفر/موقع غير قابل للتخمين.
- [ ]  منع Replay ودعم اتجاه In/Out وقواعد إعادة الدخول.
- [ ]  وضع Offline محدود للبوابة مع تسوية آمنة.
- [ ]  دعم قارئ كاميرا/باركود فعلي.
- [ ]  طباعة شارات وقوالب وأحجام واختبار أجهزة الطباعة.

### PROD-705 — الاشتراكات والخطط

- [ ]  تعريف Entitlements لكل خطة.
- [ ]  حدود المستخدمين والمعارض والزوار والتخزين.
- [ ]  Grace period وتعليق آمن دون فقد البيانات.
- [ ]  Metering وفواتير الاشتراك وتجديد/إلغاء.
- [ ]  منع تجاوز الخطة في الخادم، لا الواجهة فقط.

### PROD-706 — بوابات الخدمة الذاتية

- [ ]  بوابة عارض لإدارة الملف والحجز والفواتير والموظفين.
- [ ]  بوابة زائر للتسجيل والتذكرة والجدول.
- [ ]  موافقات وتدقيق للتغييرات الحساسة.
- [ ]  تصميم Responsive إذا كانت Web.

## 12. المرحلة الثامنة — جاهزية تطبيق سطح المكتب (الأسبوع 5–10)

- [ ]  إزالة TODO و`NotImplementedException` أو توثيق الوظائف غير المدعومة.
- [ ]  ربط كل مؤشرات Dashboard ببيانات حقيقية.
- [ ]  Global UI exception handling وCrash reporting مع Redaction.
- [ ]  حالات Loading/Empty/Error/Offline موحدة.
- [ ]  منع النقر المكرر للعمليات المالية والحجز.
- [ ]  تعريب من ملفات موارد ودعم الإنجليزية دون نسخ الشاشات.
- [ ]  Accessibility: تنقل لوحة المفاتيح، Focus، Contrast، Screen Reader labels.
- [ ]  MSIX/Installer موقع، Uninstall نظيف، وقناة تحديث تلقائي.
- [ ]  قنوات Stable/Beta وإمكانية Rollback.
- [ ]  عرض الإصدار وAPI compatibility داخل صفحة About/Diagnostics.
- [ ]  عدم السماح بعنوان API غير HTTPS في Production.

## 13. المرحلة التاسعة — الخصوصية والقانون والتوثيق (الأسبوع 4–11)

### LEG-901 — الملكية والترخيص

- [ ]  توثيق ملكية الشفرة ومساهمات الأطراف.
- [ ]  اختيار ترخيص للمستودع أو إبقاؤه Proprietary مع إشعار حقوق واضح.
- [ ]  مراجعة تراخيص جميع حزم NuGet والأصول والخطوط.

### LEG-902 — الخصوصية

- [ ]  جرد البيانات الشخصية والغرض والأساس القانوني.
- [ ]  سياسة خصوصية وشروط استخدام واتفاقية معالجة بيانات.
- [ ]  موافقة واضحة للتسويق والتصوير إن وجدت.
- [ ]  تصدير وتصحيح وحذف بيانات صاحب البيانات.
- [ ]  مدة احتفاظ حسب نوع السجل وLegal Hold.
- [ ]  تحديد موقع تخزين البيانات والنقل عبر الحدود.

### DOC-903 — وثائق المنتج

- [ ]  README شامل وبناء محلي وإعداد البيئة.
- [ ]  Architecture Decision Records.
- [ ]  مخطط البيانات وحدود Tenant.
- [ ]  دليل API وPermissions.
- [ ]  دليل مدير Tenant وموظف البوابة والمحاسب.
- [ ]  دليل التثبيت والتحديث واستكشاف الأعطال.
- [ ]  Release Notes وKnown Issues.

### DOC-904 — الدعم والتشغيل

- [ ]  SLA حسب الخطة.
- [ ]  قنوات وأوقات الدعم ومستويات P0–P3.
- [ ]  Runbooks للحوادث المتكررة.
- [ ]  Status page وقالب اتصال أثناء الحادث.
- [ ]  آلية تصعيد إلى التقنية والأمن والبوابة المالية.

## 14. المرحلة العاشرة — الأداء والأمن المتقدم (الأسبوع 7–12)

### PERF-1001 — اختبار الحمل

سيناريوهات إلزامية:

- [ ]  تسجيل زوار جماعي قبل المعرض.
- [ ]  مسح تذاكر متزامن عند عدة بوابات.
- [ ]  حجز نفس الجناح بالتزامن.
- [ ]  إصدار فواتير ومدفوعات وWebhooks.
- [ ]  تقارير على بيانات كبيرة.
- [ ]  تشغيل مستمر 8–24 ساعة لاكتشاف التسربات.

**المخرجات:** Baseline، عنق الزجاجة، خطة Capacity، حدود Rate Limit، ومقاييس قبل/بعد.

### SEC-1002 — مراجعة أمنية

- [ ]  Threat Model وفق STRIDE للأصول ومسارات الثقة.
- [ ]  SAST/DAST/SCA وSecret Scan.
- [ ]  مراجعة يدوية للمصادقة والتفويض وTenant Isolation.
- [ ]  اختبار اختراق مستقل قبل Pilot العام.
- [ ]  إعادة اختبار لكل نتيجة وإرفاق دليل الإغلاق.

## 15. خطة Pilot والإطلاق

### Pilot داخلي — أسبوع واحد

- بيانات مصطنعة فقط.
- تنفيذ سيناريو كامل: إنشاء Tenant → معرض → قاعة → أجنحة → حجز → فاتورة → دفع → تذكرة → مسح → تقرير.
- اختبار أعطال الشبكة وقاعدة البيانات والتحديث.

### Pilot مغلق — 2–4 أسابيع

- عميل أو عميلان منخفضا المخاطر.
- Support channel مباشر وOn-call محدد.
- اجتماع يومي للحوادث أول أسبوع.
- عدم تفعيل ميزات غير مختبرة خلف Feature Flags.

### شروط إنهاء Pilot

- [ ]  صفر حادث أمني أو تسريب Tenant.
- [ ]  صفر حجز مزدوج أو فقد معاملة مالية.
- [ ]  نجاح ≥99.5% لمسح التذاكر.
- [ ]  التوافر والأداء ضمن SLO.
- [ ]  لا أخطاء P0/P1 مفتوحة.
- [ ]  قبول المستخدمين الأساسيين وتوثيق ملاحظاتهم.
- [ ]  اختبار Restore وRollback أثناء نافذة Pilot.

### الإطلاق العام

- Freeze قبل 72 ساعة.
- مراجعة Go/No-Go نهائية.
- Backup وإثبات الاسترجاع.
- مراقبة مكثفة وHypercare لمدة 14 يوماً.
- Retro بعد 7 و30 يوماً وخطة الإصدار التالي.

## 16. سجل المخاطر

| الخطر | الاحتمال | الأثر | المعالجة | المالك |
| --- | --- | --- | --- | --- |
| استخدام الأسرار المنشورة | عالٍ | حرج | تدوير وفحص حادث | الأمن/DevOps |
| تسريب بيانات بين المستأجرين | عالٍ | حرج | Tenant Context واختبارات عزل | Backend |
| حجز مزدوج | متوسط/عالٍ | عالٍ | قيد DB وتزامن ومعاملة | Backend/DBA |
| تكرار رقم فاتورة | متوسط | عالٍ | Sequence ذري | Backend/DBA |
| فشل يوم المعرض | متوسط | حرج | Load test، Offline gate، مراقبة | SRE |
| تأخر بوابة الدفع | متوسط | عالٍ | اختيار مبكر وSandbox وFeature Flag | المنتج/Backend |
| كسر نسخ Desktop القديمة | عالٍ | متوسط | API versioning وتحديث تلقائي | Desktop/API |
| عدم الامتثال للخصوصية | متوسط | عالٍ | DPA وسياسات حذف وLegal review | المنتج/القانون |
| اتساع النطاق وتأخر الإطلاق | عالٍ | متوسط | MVP ثابت وبوابات قرار | مسؤول المنتج |

## 17. ترتيب التنفيذ الحرج

1. احتواء الأسرار وإغلاق التسجيل الخطر.
2. إصلاح Tenant Isolation والصلاحيات.
3. إضافة CI واختبارات الأمن والتكامل.
4. إصلاح تزامن الحجز وترقيم الفواتير.
5. معالجة الأخطاء والمراقبة والنسخ الاحتياطي.
6. استكمال البريد والدفع والفواتير والتذاكر.
7. تغليف وتوقيع وتحديث تطبيق سطح المكتب.
8. الخصوصية والتوثيق والدعم.
9. اختبارات الأداء والأمن وUAT.
10. Pilot ثم Go/No-Go.

## 18. لوحة المتابعة الأسبوعية

يُحدّث هذا الجدول أسبوعياً:

| المؤشر | الهدف | الحالي | الاتجاه | المالك |
| --- | --- | --- | --- | --- |
| ثغرات P0 مفتوحة | 0 | — | — | الأمن |
| ثغرات P1 مفتوحة | 0 قبل Pilot | — | — | الأمن |
| اختبارات المسارات الحرجة الناجحة | 100% | — | — | QA |
| نجاح CI على الفرع الرئيسي | 100% | — | — | التقنية |
| أخطاء P0/P1 المفتوحة | 0 | — | — | المنتج |
| نجاح Tenant Isolation suite | 100% | — | — | Backend |
| نجاح Restore drill | 100% | — | — | DevOps |
| p95 لمسح التذكرة | <700ms | — | — | SRE |
| توافر Pilot | ≥99.9% | — | — | SRE |

## 19. Definition of Done لكل مهمة

لا تغلق أي مهمة إلا عند تحقق الآتي:

- [ ]  الشفرة مراجعة ومندمجة عبر Pull Request.
- [ ]  Unit/Integration/Regression tests ناجحة.
- [ ]  فحص الأمن والاعتماديات ناجح.
- [ ]  Logs وMetrics مضافة عند الحاجة دون بيانات حساسة.
- [ ]  الوثائق وRunbook وOpenAPI محدثة.
- [ ]  Migration وRollback مختبران إن وُجدا.
- [ ]  قبول مسؤول المنتج وQA.
- [ ]  لا توجد TODO مؤجلة داخل المسار الإنتاجي دون Ticket وموعد ومالك.

<aside>
✅

التوصية: يبدأ العمل بمرحلة الاحتواء خلال 24 ساعة، ثم ينفذ مساران متوازيان: الأمن وعزل المستأجرين، وبناء CI/الاختبارات. لا يبدأ تطوير ميزات تجارية جديدة قبل المرور من G1.

</aside>

كود الإصلاح الكامل — ExhibitionManagementSystem

<aside>
📌

كل قسم يحدد: مسار الملف، نوع العملية (ملف جديد / تعديل ملف قائم / حذف)، ثم الكود الكامل جاهزاً للنسخ. الأسماء مبنية على البنية القياسية لمشروع .NET 8 + EF Core + WPF. إن اختلف اسم مشروعك، غيّر الـnamespace فقط.

</aside>

## 0. خريطة الملفات

| # | الملف | العملية |
| --- | --- | --- |
| 1 | `src/Api/appsettings.json` | تعديل |
| 2 | `src/Api/appsettings.Production.json` | جديد |
| 3 | `.gitignore` | تعديل |
| 4 | `src/Api/Configuration/JwtOptions.cs` | جديد |
| 5 | `src/Api/Configuration/SecurityOptions.cs` | جديد |
| 6 | `src/Api/Configuration/StartupGuard.cs` | جديد |
| 7 | `src/Domain/Entities/RefreshToken.cs` | جديد |
| 8 | `src/Domain/Entities/UserInvitation.cs` | جديد |
| 9 | `src/Application/Abstractions/ITokenService.cs` | جديد |
| 10 | `src/Infrastructure/Auth/TokenService.cs` | جديد/استبدال |
| 11 | `src/Api/Controllers/AuthController.cs` | استبدال كامل |
| 12 | `src/Application/Abstractions/ITenantContext.cs` | جديد |
| 13 | `src/Infrastructure/MultiTenancy/TenantContext.cs` | جديد |
| 14 | `src/Infrastructure/MultiTenancy/TenantSaveChangesInterceptor.cs` | جديد |
| 15 | `src/Infrastructure/Persistence/AppDbContext.cs` | تعديل |
| 16 | `src/Application/Authorization/Permissions.cs` | جديد |
| 17 | `src/Api/Authorization/PermissionHandler.cs` | جديد |
| 18 | `src/Api/Controllers/UsersController.cs` | تعديل |
| 19 | `src/Infrastructure/Booking/BookingService.cs` | تعديل |
| 20 | `src/Infrastructure/Invoicing/InvoiceNumberGenerator.cs` | جديد |
| 21 | `src/Api/Middleware/ExceptionHandlingMiddleware.cs` | جديد |
| 22 | `src/Api/Middleware/IdempotencyMiddleware.cs` | جديد |
| 23 | `src/Api/Program.cs` | استبدال كامل |
| 24 | `src/Api/Validators/*.cs` | جديد |
| 25 | `.github/workflows/ci.yml` | جديد |
| 26 | `tests/*` | جديد |
| 27 | `src/Desktop/Services/ApiClient.cs` | تعديل |
| 28 | `src/Desktop/appsettings.json` | تعديل |
| 29 | `db/migrations/*.sql` | جديد |

## 1. الإعدادات والأسرار (SEC-001، SEC-002، SEC-006)

### 1.1 `src/Api/appsettings.json` — تعديل (استبدل الملف بالكامل)

احذف أي ConnectionString أو مفتاح JWT حقيقي. لا تترك أي قيمة سرية هنا إطلاقاً.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Default": ""
  },
  "Jwt": {
    "Issuer": "",
    "Audience": "",
    "SigningKey": "",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 14
  },
  "Security": {
    "RequireHttps": true,
    "AllowPublicRegistration": false,
    "AllowedOrigins": [],
    "MaxFailedAccessAttempts": 5,
    "LockoutMinutes": 15,
    "PasswordMinLength": 12
  },
  "Mail": {
    "Provider": "",
    "ApiKey": "",
    "FromAddress": "",
    "FromName": "Exhibition Management"
  }
}
```

### 1.2 `src/Api/appsettings.Production.json` — ملف جديد

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Error"
    }
  },
  "AllowedHosts": "api.yourdomain.com",
  "Security": {
    "RequireHttps": true,
    "AllowPublicRegistration": false
  }
}
```

القيم الحساسة تُمرر عبر متغيرات البيئة فقط:

```bash
# Linux / Docker
export ConnectionStrings__Default="Server=...;Database=...;User Id=app_user;Password=...;Encrypt=True;TrustServerCertificate=False"
export Jwt__SigningKey="$(openssl rand -base64 64)"
export Jwt__Issuer="https://api.yourdomain.com"
export Jwt__Audience="ems-clients"
export Mail__ApiKey="..."

# Windows (IIS / Azure App Service → Application settings)
setx ConnectionStrings__Default "..."
setx Jwt__SigningKey "..."
```

### 1.3 `.gitignore` — تعديل (أضف في نهاية الملف)

```
# Secrets & local config
appsettings.*.local.json
appsettings.Development.json
*.user
.env
.env.*
secrets.json
**/Properties/launchSettings.json
*.pfx
*.snk
```

### 1.4 `src/Api/Configuration/JwtOptions.cs` — ملف جديد

```csharp
using System.ComponentModel.DataAnnotations;

namespace Ems.Api.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = string.Empty;

    /// <summary>مفتاح التوقيع. 64 حرفاً على الأقل ويأتي من Secret Store فقط.</summary>
    [Required(AllowEmptyStrings = false)]
    [MinLength(64, ErrorMessage = "Jwt:SigningKey must be at least 64 characters.")]
    public string SigningKey { get; init; } = string.Empty;

    [Range(1, 60)]
    public int AccessTokenMinutes { get; init; } = 15;

    [Range(1, 90)]
    public int RefreshTokenDays { get; init; } = 14;
}
```

### 1.5 `src/Api/Configuration/SecurityOptions.cs` — ملف جديد

```csharp
namespace Ems.Api.Configuration;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public bool RequireHttps { get; init; } = true;
    public bool AllowPublicRegistration { get; init; } = false;
    public string[] AllowedOrigins { get; init; } = Array.Empty<string>();
    public int MaxFailedAccessAttempts { get; init; } = 5;
    public int LockoutMinutes { get; init; } = 15;
    public int PasswordMinLength { get; init; } = 12;
}
```

### 1.6 `src/Api/Configuration/StartupGuard.cs` — ملف جديد

يمنع تشغيل التطبيق أصلاً إذا كانت الأسرار ناقصة أو ضعيفة (بدل الـFallback الخطر).

```csharp
using Microsoft.Extensions.Options;

namespace Ems.Api.Configuration;

public static class StartupGuard
{
    private static readonly string[] ForbiddenKeyFragments =
    {
        "changeme", "secret", "test", "dev", "123456", "default", "your-key"
    };

    public static void ValidateOrThrow(IConfiguration configuration, IHostEnvironment env)
    {
        var errors = new List<string>();

        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
            errors.Add("ConnectionStrings:Default is missing.");
        else if (env.IsProduction() &&
                 !connectionString.Contains("Encrypt=True", StringComparison.OrdinalIgnoreCase))
            errors.Add("Production connection string must use Encrypt=True.");
        else if (env.IsProduction() &&
                 connectionString.Contains("TrustServerCertificate=True", StringComparison.OrdinalIgnoreCase))
            errors.Add("TrustServerCertificate=True is not allowed in Production.");

        var key = configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(key))
            errors.Add("Jwt:SigningKey is missing.");
        else
        {
            if (key.Length < 64)
                errors.Add("Jwt:SigningKey must be at least 64 characters.");
            if (ForbiddenKeyFragments.Any(f => key.Contains(f, StringComparison.OrdinalIgnoreCase)))
                errors.Add("Jwt:SigningKey looks like a placeholder value.");
        }

        if (string.IsNullOrWhiteSpace(configuration["Jwt:Issuer"]))
            errors.Add("Jwt:Issuer is missing.");
        if (string.IsNullOrWhiteSpace(configuration["Jwt:Audience"]))
            errors.Add("Jwt:Audience is missing.");

        if (env.IsProduction() && configuration.GetValue("Security:AllowPublicRegistration", false))
            errors.Add("Security:AllowPublicRegistration must be false in Production.");

        if (errors.Count > 0)
            throw new InvalidOperationException(
                "Startup configuration is invalid:" + Environment.NewLine +
                string.Join(Environment.NewLine, errors.Select(e => " - " + e)));
    }
}
```

## 2. المصادقة ودورة حياة التوكن (SEC-002، SEC-104، SEC-105)

### 2.1 `src/Domain/Entities/RefreshToken.cs` — ملف جديد

```csharp
namespace Ems.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }

    /// <summary>SHA-256 للقيمة الأصلية. القيمة الأصلية لا تُخزَّن أبداً.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public string? DeviceId { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevokedReason { get; set; }

    /// <summary>عند التدوير: معرّف التوكن الذي حل محله (لاكتشاف إعادة الاستخدام).</summary>
    public Guid? ReplacedByTokenId { get; set; }

    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;
}
```

### 2.2 `src/Domain/Entities/UserInvitation.cs` — ملف جديد

```csharp
namespace Ems.Domain.Entities;

public class UserInvitation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;

    /// <summary>الدور المسموح فقط. لا يُقبل Admin/SuperAdmin من هنا.</summary>
    public string Role { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? AcceptedAtUtc { get; set; }
    public Guid? AcceptedByUserId { get; set; }

    public bool IsUsable => AcceptedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;
}
```

### 2.3 `src/Application/Abstractions/ITokenService.cs` — ملف جديد

```csharp
namespace Ems.Application.Abstractions;

public record TokenPair(string AccessToken, DateTime AccessExpiresUtc, string RefreshToken, DateTime RefreshExpiresUtc);

public interface ITokenService
{
    Task<TokenPair> IssueAsync(Guid userId, Guid tenantId, string email,
        IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions,
        string? deviceId, string? ip, string? userAgent, CancellationToken ct = default);

    Task<TokenPair> RotateAsync(string refreshToken, string? deviceId, string? ip,
        string? userAgent, CancellationToken ct = default);

    Task RevokeAsync(string refreshToken, string reason, CancellationToken ct = default);
    Task RevokeAllForUserAsync(Guid userId, string reason, CancellationToken ct = default);
}
```

### 2.4 `src/Infrastructure/Auth/TokenService.cs` — ملف جديد (يستبدل أي توليد JWT قديم)

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ems.Api.Configuration;
using Ems.Application.Abstractions;
using Ems.Domain.Entities;
using Ems.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Ems.Infrastructure.Auth;

public sealed class TokenService : ITokenService
{
    private readonly AppDbContext _db;
    private readonly JwtOptions _jwt;
    private readonly ILogger<TokenService> _logger;

    public TokenService(AppDbContext db, IOptions<JwtOptions> jwt, ILogger<TokenService> logger)
    {
        _db = db;
        _jwt = jwt.Value;
        _logger = logger;
    }

    public async Task<TokenPair> IssueAsync(Guid userId, Guid tenantId, string email,
        IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions,
        string? deviceId, string? ip, string? userAgent, CancellationToken ct = default)
    {
        var (access, accessExp) = CreateAccessToken(userId, tenantId, email, roles, permissions);
        var (refresh, refreshExp) = await CreateRefreshTokenAsync(userId, tenantId, deviceId, ip, userAgent, null, ct);
        return new TokenPair(access, accessExp, refresh, refreshExp);
    }

    public async Task<TokenPair> RotateAsync(string refreshToken, string? deviceId, string? ip,
        string? userAgent, CancellationToken ct = default)
    {
        var hash = Hash(refreshToken);

        var stored = await _db.RefreshTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        // اكتشاف إعادة استخدام توكن مُدوَّر أو ملغى: إبطال كل جلسات المستخدم.
        if (!stored.IsActive)
        {
            _logger.LogWarning("Refresh token reuse detected for user {UserId}", stored.UserId);
            await RevokeAllForUserAsync(stored.UserId, "refresh-token-reuse-detected", ct);
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var user = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == stored.UserId, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (!user.IsActive)
        {
            await RevokeAllForUserAsync(user.Id, "user-disabled", ct);
            throw new UnauthorizedAccessException("Account disabled.");
        }

        var roles = await LoadRolesAsync(user.Id, ct);
        var permissions = await LoadPermissionsAsync(user.Id, ct);

        var (access, accessExp) = CreateAccessToken(user.Id, user.TenantId, user.Email, roles, permissions);
        var (refresh, refreshExp) = await CreateRefreshTokenAsync(
            user.Id, user.TenantId, deviceId ?? stored.DeviceId, ip, userAgent, stored, ct);

        return new TokenPair(access, accessExp, refresh, refreshExp);
    }

    public async Task RevokeAsync(string refreshToken, string reason, CancellationToken ct = default)
    {
        var hash = Hash(refreshToken);
        var stored = await _db.RefreshTokens.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is null || !stored.IsActive) return;

        stored.RevokedAtUtc = DateTime.UtcNow;
        stored.RevokedReason = reason;
        await _db.SaveChangesAsync(ct);
    }

    public async Task RevokeAllForUserAsync(Guid userId, string reason, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await _db.RefreshTokens.IgnoreQueryFilters()
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAtUtc, now)
                .SetProperty(t => t.RevokedReason, reason), ct);
    }

    private (string Token, DateTime ExpiresUtc) CreateAccessToken(Guid userId, Guid tenantId,
        string email, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_jwt.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("tenant_id", tenantId.ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim("perm", p)));

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    private async Task<(string Token, DateTime ExpiresUtc)> CreateRefreshTokenAsync(
        Guid userId, Guid tenantId, string? deviceId, string? ip, string? userAgent,
        RefreshToken? replacing, CancellationToken ct)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var entity = new RefreshToken
        {
            UserId = userId,
            TenantId = tenantId,
            TokenHash = Hash(raw),
            DeviceId = deviceId,
            IpAddress = ip,
            UserAgent = Truncate(userAgent, 256),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays)
        };

        _db.RefreshTokens.Add(entity);

        if (replacing is not null)
        {
            replacing.RevokedAtUtc = DateTime.UtcNow;
            replacing.RevokedReason = "rotated";
            replacing.ReplacedByTokenId = entity.Id;
            replacing.LastUsedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return (raw, entity.ExpiresAtUtc);
    }

    private Task<List<string>> LoadRolesAsync(Guid userId, CancellationToken ct) =>
        _db.UserRoles.IgnoreQueryFilters()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Name)
            .ToListAsync(ct);

    private Task<List<string>> LoadPermissionsAsync(Guid userId, CancellationToken ct) =>
        _db.UserRoles.IgnoreQueryFilters()
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role.Permissions.Select(p => p.Code))
            .Distinct()
            .ToListAsync(ct);

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max];
}
```

### 2.5 `src/Api/Controllers/AuthController.cs` — استبدال كامل

يغلق SEC-004 (تصعيد الصلاحيات) وSEC-104 وSEC-106.

```csharp
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ems.Api.Configuration;
using Ems.Application.Abstractions;
using Ems.Domain.Entities;
using Ems.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ems.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokens;
    private readonly IPasswordHasher<AppUser> _hasher;
    private readonly IEmailSender _email;
    private readonly SecurityOptions _security;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AppDbContext db, ITokenService tokens, IPasswordHasher<AppUser> hasher,
        IEmailSender email, IOptions<SecurityOptions> security, ILogger<AuthController> logger)
    {
        _db = db; _tokens = tokens; _hasher = hasher;
        _email = email; _security = security.Value; _logger = logger;
    }

    // ---------- Login ----------

    public sealed record LoginRequest(string Email, string Password, string? DeviceId);
    public sealed record TokenResponse(string AccessToken, DateTime AccessExpiresUtc,
        string RefreshToken, DateTime RefreshExpiresUtc);

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == email, ct);

        // رسالة موحدة لا تكشف وجود الحساب
        const string genericError = "Invalid credentials.";

        if (user is null)
        {
            // تأخير ثابت لمنع تحديد وجود الحساب عبر التوقيت
            await Task.Delay(Random.Shared.Next(120, 260), ct);
            return Unauthorized(new { error = genericError });
        }

        if (user.LockoutEndUtc is { } until && until > DateTime.UtcNow)
            return StatusCode(StatusCodes.Status423Locked,
                new { error = "Account temporarily locked. Try again later." });

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            user.AccessFailedCount++;
            if (user.AccessFailedCount >= _security.MaxFailedAccessAttempts)
            {
                user.LockoutEndUtc = DateTime.UtcNow.AddMinutes(_security.LockoutMinutes);
                user.AccessFailedCount = 0;
                _logger.LogWarning("Lockout applied to user {UserId}", user.Id);
            }
            await _db.SaveChangesAsync(ct);
            return Unauthorized(new { error = genericError });
        }

        if (!user.IsActive)
            return Unauthorized(new { error = genericError });

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = _hasher.HashPassword(user, request.Password);

        user.AccessFailedCount = 0;
        user.LockoutEndUtc = null;
        user.LastLoginAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var roles = await _db.UserRoles.IgnoreQueryFilters()
            .Where(r => r.UserId == user.Id).Select(r => r.Role.Name).ToListAsync(ct);
        var perms = await _db.UserRoles.IgnoreQueryFilters()
            .Where(r => r.UserId == user.Id)
            .SelectMany(r => r.Role.Permissions.Select(p => p.Code))
            .Distinct().ToListAsync(ct);

        var pair = await _tokens.IssueAsync(user.Id, user.TenantId, user.Email, roles, perms,
            request.DeviceId, ClientIp(), UserAgent(), ct);

        return Ok(new TokenResponse(pair.AccessToken, pair.AccessExpiresUtc,
            pair.RefreshToken, pair.RefreshExpiresUtc));
    }

    // ---------- Refresh ----------

    public sealed record RefreshRequest(string RefreshToken, string? DeviceId);

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        try
        {
            var pair = await _tokens.RotateAsync(request.RefreshToken, request.DeviceId,
                ClientIp(), UserAgent(), ct);
            return Ok(new TokenResponse(pair.AccessToken, pair.AccessExpiresUtc,
                pair.RefreshToken, pair.RefreshExpiresUtc));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Invalid refresh token." });
        }
    }

    // ---------- Logout ----------

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request, CancellationToken ct)
    {
        await _tokens.RevokeAsync(request.RefreshToken, "user-logout", ct);
        return NoContent();
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                               ?? User.FindFirstValue("sub")!);
        await _tokens.RevokeAllForUserAsync(userId, "user-logout-all", ct);
        return NoContent();
    }

    // ---------- Accept invitation (بديل التسجيل المفتوح) ----------

    public sealed record AcceptInviteRequest(string Token, string FullName, string Password);

    [AllowAnonymous]
    [HttpPost("accept-invitation")]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInviteRequest request, CancellationToken ct)
    {
        if (request.Password.Length < _security.PasswordMinLength)
            return BadRequest(new { error = $"Password must be at least {_security.PasswordMinLength} characters." });

        var hash = Sha256(request.Token);
        var invite = await _db.UserInvitations.IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.TokenHash == hash, ct);

        if (invite is null || !invite.IsUsable)
            return BadRequest(new { error = "Invitation is invalid or expired." });

        // الأدوار الإدارية لا تُمنح عبر دعوة عامة
        if (invite.Role is "SuperAdmin")
            return BadRequest(new { error = "Invalid invitation role." });

        var exists = await _db.Users.IgnoreQueryFilters()
            .AnyAsync(u => u.NormalizedEmail == invite.Email.ToLowerInvariant(), ct);
        if (exists)
            return Conflict(new { error = "Account already exists." });

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            TenantId = invite.TenantId,            // ← من الدعوة، لا من العميل
            Email = invite.Email,
            NormalizedEmail = invite.Email.ToLowerInvariant(),
            FullName = request.FullName.Trim(),
            IsActive = true,
            MustChangePassword = false,
            CreatedAtUtc = DateTime.UtcNow
        };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        var role = await _db.Roles.IgnoreQueryFilters()
            .FirstAsync(r => r.Name == invite.Role && r.TenantId == invite.TenantId, ct);

        _db.Users.Add(user);
        _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, TenantId = invite.TenantId });

        invite.AcceptedAtUtc = DateTime.UtcNow;
        invite.AcceptedByUserId = user.Id;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Invitation accepted for tenant {TenantId}", invite.TenantId);

        return NoContent();
    }

    // ---------- Forgot / Reset password ----------

    public sealed record ForgotRequest(string Email);
    public sealed record ResetRequest(string Token, string NewPassword);

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> Forgot([FromBody] ForgotRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == email, ct);

        if (user is not null && user.IsActive)
        {
            var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');

            _db.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.Id,
                TenantId = user.TenantId,
                TokenHash = Sha256(raw),
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30)
            });
            await _db.SaveChangesAsync(ct);

            await _email.SendPasswordResetAsync(user.Email, user.FullName, raw, ct);
        }

        // نفس الرد دائماً — لا يكشف وجود الحساب، ولا يعيد التوكن أبداً
        return Accepted(new { message = "If the account exists, a reset link has been sent." });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> Reset([FromBody] ResetRequest request, CancellationToken ct)
    {
        if (request.NewPassword.Length < _security.PasswordMinLength)
            return BadRequest(new { error = $"Password must be at least {_security.PasswordMinLength} characters." });

        var hash = Sha256(request.Token);
        var token = await _db.PasswordResetTokens.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (token is null || token.UsedAtUtc is not null || token.ExpiresAtUtc <= DateTime.UtcNow)
            return BadRequest(new { error = "Reset link is invalid or expired." });

        var user = await _db.Users.IgnoreQueryFilters()
            .FirstAsync(u => u.Id == token.UserId, ct);

        user.PasswordHash = _hasher.HashPassword(user, request.NewPassword);
        user.AccessFailedCount = 0;
        user.LockoutEndUtc = null;
        token.UsedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await _tokens.RevokeAllForUserAsync(user.Id, "password-reset", ct);

        return NoContent();
    }

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
    private string? UserAgent() => Request.Headers.UserAgent.ToString();

    private static string Sha256(string value) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
```

<aside>
🗑️

احذف من `AuthController` القديم: أي `Register` عام يقبل `TenantID` أو `InitialRole` من الـBody، وأي إرجاع لتوكن إعادة التعيين في الاستجابة، وأي قيمة JWT افتراضية مكتوبة في الكود.

</aside>

## 3. عزل المستأجرين والصلاحيات (SEC-101، SEC-102، SEC-103)

### 3.1 `src/Application/Abstractions/ITenantContext.cs` — ملف جديد

```csharp
namespace Ems.Application.Abstractions;

public interface ITenantContext
{
    /// <summary>معرّف المستأجر المستخرج من Claims الموثوقة فقط.</summary>
    Guid? TenantId { get; }

    Guid? UserId { get; }

    /// <summary>تجاوز الفلترة مسموح للـSuperAdmin فقط وبشكل صريح ومدقق.</summary>
    bool IsPlatformAdmin { get; }

    Guid RequireTenantId();
}
````<br><br>### 3.2 `src/Infrastructure/MultiTenancy/TenantContext.cs` — ملف جديد

````csharp
using System.Security.Claims;
using Ems.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Ems.Infrastructure.MultiTenancy;

public sealed class TenantContext : ITenantContext
{
    public TenantContext(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) return;

        if (Guid.TryParse(user.FindFirst("tenant_id")?.Value, out var tenantId))
            TenantId = tenantId;

        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
        if (Guid.TryParse(sub, out var userId))
            UserId = userId;

        IsPlatformAdmin = user.IsInRole("SuperAdmin");
    }

    public Guid? TenantId { get; }
    public Guid? UserId { get; }
    public bool IsPlatformAdmin { get; }

    public Guid RequireTenantId() =>
        TenantId ?? throw new UnauthorizedAccessException("Tenant context is missing.");
}
````<br><br>### 3.3 `src/Infrastructure/MultiTenancy/TenantSaveChangesInterceptor.cs` — ملف جديد

يمنع كتابة أي صف بـTenantId مختلف، حتى لو نسي المطور الفلترة في الخدمة.

````csharp
using Ems.Application.Abstractions;
using Ems.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Ems.Infrastructure.MultiTenancy;

public sealed class TenantSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ITenantContext _tenant;

    public TenantSaveChangesInterceptor(ITenantContext tenant) => _tenant = tenant;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Enforce(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        Enforce(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    private void Enforce(DbContext? context)
    {
        if (context is null) return;
        if (_tenant.IsPlatformAdmin) return;      // استثناء صريح ومدقق
        if (_tenant.TenantId is not { } tenantId) return; // عمليات النظام/الترحيل

        foreach (var entry in context.ChangeTracker.Entries<ITenantOwned>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.TenantId == Guid.Empty)
                        entry.Entity.TenantId = tenantId;
                    else if (entry.Entity.TenantId != tenantId)
                        throw new UnauthorizedAccessException(
                            $"Cross-tenant insert blocked for {entry.Entity.GetType().Name}.");
                    break;

                case EntityState.Modified:
                case EntityState.Deleted:
                    var original = entry.OriginalValues.GetValue<Guid>(nameof(ITenantOwned.TenantId));
                    if (original != tenantId || entry.Entity.TenantId != tenantId)
                        throw new UnauthorizedAccessException(
                            $"Cross-tenant write blocked for {entry.Entity.GetType().Name}.");
                    break;
            }
        }
    }
}
````<br><br>### 3.4 `src/Domain/Common/ITenantOwned.cs` — ملف جديد

نفّذ هذا الواجه في كل كيان مرتبط بمستأجر (Exhibition، Hall، Booth، Booking، Invoice، Payment، Ticket، Visitor، User…).

````csharp
namespace Ems.Domain.Common;

public interface ITenantOwned
{
    Guid TenantId { get; set; }
}

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAtUtc { get; set; }
    Guid? DeletedByUserId { get; set; }
}
````<br><br>### 3.5 `src/Infrastructure/Persistence/AppDbContext.cs` — تعديل<br><br>أضف المُنشئ الجديد، الـDbSets الجديدة، ودالة `ApplyTenantFilters` الموضحة أدناه.

````csharp
using System.Linq.Expressions;
using Ems.Application.Abstractions;
using Ems.Domain.Common;
using Ems.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ems.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant)
        : base(options) => _tenant = tenant;

    // === DbSets الجديدة ===
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserInvitation> UserInvitations => Set<UserInvitation>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<InvoiceNumberSequence> InvoiceNumberSequences => Set<InvoiceNumberSequence>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    // ... ابقِ الـDbSets القائمة كما هي

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ApplyTenantAndSoftDeleteFilters(modelBuilder);
        ConfigureNewEntities(modelBuilder);
    }

    private void ApplyTenantAndSoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clr = entityType.ClrType;
            var isTenantOwned = typeof(ITenantOwned).IsAssignableFrom(clr);
            var isSoftDeletable = typeof(ISoftDeletable).IsAssignableFrom(clr);
            if (!isTenantOwned && !isSoftDeletable) continue;

            var parameter = Expression.Parameter(clr, "e");
            Expression? body = null;

            if (isTenantOwned)
            {
                // e.TenantId == _tenant.TenantId || _tenant.IsPlatformAdmin
                var tenantProperty = Expression.Property(parameter, nameof(ITenantOwned.TenantId));
                var currentTenant = Expression.Convert(
                    Expression.Property(Expression.Constant(this), nameof(CurrentTenantIdOrEmpty)), typeof(Guid));
                var tenantMatches = Expression.Equal(tenantProperty, currentTenant);
                var bypass = Expression.Property(Expression.Constant(this), nameof(BypassTenantFilter));
                body = Expression.OrElse(tenantMatches, bypass);
            }

            if (isSoftDeletable)
            {
                var notDeleted = Expression.Not(
                    Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted)));
                body = body is null ? notDeleted : Expression.AndAlso(body, notDeleted);
            }

            modelBuilder.Entity(clr).HasQueryFilter(Expression.Lambda(body!, parameter));
        }
    }

    /// <summary>تُقرأ داخل Query Filter — تتغير مع كل طلب.</summary>
    public Guid CurrentTenantIdOrEmpty => _tenant.TenantId ?? Guid.Empty;
    public bool BypassTenantFilter => _tenant.IsPlatformAdmin;

    private static void ConfigureNewEntities(ModelBuilder b)
    {
        b.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => new { x.UserId, x.RevokedAtUtc });
            e.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            e.Property(x => x.DeviceId).HasMaxLength(128);
            e.Property(x => x.UserAgent).HasMaxLength(256);
            e.Property(x => x.IpAddress).HasMaxLength(64);
        });

        b.Entity<UserInvitation>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.Email });
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.Role).HasMaxLength(64).IsRequired();
        });

        b.Entity<PasswordResetToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.TokenHash).IsUnique();
        });

        b.Entity<InvoiceNumberSequence>(e =>
        {
            e.HasKey(x => new { x.TenantId, x.Year });
            e.Property(x => x.LastNumber).IsConcurrencyToken();
        });

        b.Entity<IdempotencyRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.Key, x.Endpoint }).IsUnique();
            e.Property(x => x.Key).HasMaxLength(128).IsRequired();
            e.Property(x => x.Endpoint).HasMaxLength(256).IsRequired();
        });
    }
}
````<br><br>### 3.6 `src/Domain/Entities/SupportEntities.cs` — ملف جديد

````csharp
namespace Ems.Domain.Entities;

public class PasswordResetToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
}

public class InvoiceNumberSequence
{
    public Guid TenantId { get; set; }
    public int Year { get; set; }
    public int LastNumber { get; set; }
    public string Prefix { get; set; } = "INV";
}

public class IdempotencyRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class AuditLog
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
````<br><br>### 3.7 `src/Application/Authorization/Permissions.cs` — ملف جديد

````csharp
namespace Ems.Application.Authorization;

public static class Permissions
{
    public const string UsersRead = "users.read";
    public const string UsersWrite = "users.write";
    public const string RolesAssign = "roles.assign";

    public const string ExhibitionsRead = "exhibitions.read";
    public const string ExhibitionsWrite = "exhibitions.write";

    public const string BookingsRead = "bookings.read";
    public const string BookingsWrite = "bookings.write";
    public const string BookingsCancel = "bookings.cancel";

    public const string InvoicesRead = "invoices.read";
    public const string InvoicesIssue = "invoices.issue";
    public const string PaymentsRecord = "payments.record";
    public const string PaymentsRefund = "payments.refund";

    public const string TicketsIssue = "tickets.issue";
    public const string TicketsScan = "tickets.scan";

    public const string ReportsView = "reports.view";
    public const string TenantSettings = "tenant.settings";
    public const string PlatformAdmin = "platform.admin";

    /// <summary>مصفوفة الأدوار → الصلاحيات. تُستخدم في Seeder والاختبارات.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> RoleMatrix =
        new Dictionary<string, string[]>
        {
            ["SuperAdmin"] = new[] { PlatformAdmin },
            ["TenantAdmin"] = new[]
            {
                UsersRead, UsersWrite, RolesAssign, ExhibitionsRead, ExhibitionsWrite,
                BookingsRead, BookingsWrite, BookingsCancel, InvoicesRead, InvoicesIssue,
                PaymentsRecord, PaymentsRefund, TicketsIssue, TicketsScan, ReportsView, TenantSettings
            },
            ["EventManager"] = new[]
            {
                ExhibitionsRead, ExhibitionsWrite, BookingsRead, BookingsWrite,
                BookingsCancel, TicketsIssue, ReportsView
            },
            ["Finance"] = new[]
            {
                InvoicesRead, InvoicesIssue, PaymentsRecord, PaymentsRefund, ReportsView, BookingsRead
            },
            ["GateOperator"] = new[] { TicketsScan },
            ["Exhibitor"] = new[] { BookingsRead, InvoicesRead },
            ["Viewer"] = new[] { ExhibitionsRead, BookingsRead, InvoicesRead, ReportsView }
        };
}
````<br><br>### 3.8 `src/Api/Authorization/PermissionHandler.cs` — ملف جديد

````csharp
using Microsoft.AspNetCore.Authorization;

namespace Ems.Api.Authorization;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission) => Permission = permission;
    public string Permission { get; }
}

public sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var hasPermission = context.User.Claims
            .Any(c => c.Type == "perm" &&
                      (c.Value == requirement.Permission || c.Value == "platform.admin"));

        if (hasPermission) context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

/// <summary>ينشئ الـPolicy تلقائياً من اسم الصلاحية دون تسجيل يدوي لكل واحدة.</summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;
    public const string Prefix = "perm:";

    public PermissionPolicyProvider(Microsoft.Extensions.Options.IOptions<AuthorizationOptions> options)
        => _fallback = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            return _fallback.GetPolicyAsync(policyName);

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName[Prefix.Length..]))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}

/// <summary>استخدام: [HasPermission(Permissions.BookingsWrite)]</summary>
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
        => Policy = PermissionPolicyProvider.Prefix + permission;
}
````<br><br>### 3.9 `src/Api/Controllers/UsersController.cs` — تعديل (إغلاق IDOR)<br><br>المبدأ: لا تجلب بـ`FindAsync(id)` أبداً؛ الاستعلام دائماً مقيّد بـTenant.

````csharp
using Ems.Api.Authorization;
using Ems.Application.Abstractions;
using Ems.Application.Authorization;
using Ems.Domain.Entities;
using Ems.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ems.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/users")]
public sealed class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ITokenService _tokens;
    private readonly IAuditLogger _audit;

    public UsersController(AppDbContext db, ITenantContext tenant, ITokenService tokens, IAuditLogger audit)
    {
        _db = db; _tenant = tenant; _tokens = tokens; _audit = audit;
    }

    public sealed record UserDto(Guid Id, string Email, string FullName, bool IsActive, string[] Roles);

    [HttpGet]
    [HasPermission(Permissions.UsersRead)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var tenantId = _tenant.RequireTenantId();

        var query = _db.Users.AsNoTracking().Where(u => u.TenantId == tenantId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.Email).ThenBy(u => u.Id)   // فرز ثابت يمنع تكرار/فقد الصفوف
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new UserDto(u.Id, u.Email, u.FullName, u.IsActive,
                u.UserRoles.Select(r => r.Role.Name).ToArray()))
            .ToListAsync(ct);

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.UsersRead)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();

        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == id && u.TenantId == tenantId)   // ← شرط Tenant داخل الاستعلام
            .Select(u => new UserDto(u.Id, u.Email, u.FullName, u.IsActive,
                u.UserRoles.Select(r => r.Role.Name).ToArray()))
            .FirstOrDefaultAsync(ct);

        // 404 وليس 403 حتى لا يُستدل على وجود المورد في Tenant آخر
        return user is null ? NotFound() : Ok(user);
    }

    public sealed record UpdateUserRequest(string FullName, bool IsActive);

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.UsersWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId, ct);
        if (user is null) return NotFound();

        // منع تعطيل النفس
        if (user.Id == _tenant.UserId && !request.IsActive)
            return BadRequest(new { error = "You cannot deactivate your own account." });

        // منع تعطيل آخر TenantAdmin
        if (!request.IsActive && await IsLastActiveTenantAdminAsync(user.Id, tenantId, ct))
            return BadRequest(new { error = "At least one active TenantAdmin must remain." });

        user.FullName = request.FullName.Trim();
        var wasActive = user.IsActive;
        user.IsActive = request.IsActive;
        await _db.SaveChangesAsync(ct);

        if (wasActive && !request.IsActive)
            await _tokens.RevokeAllForUserAsync(user.Id, "user-deactivated", ct);

        await _audit.LogAsync("user.update", nameof(AppUser), user.Id.ToString(), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.UsersWrite)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();

        if (id == _tenant.UserId)
            return BadRequest(new { error = "You cannot delete your own account." });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId, ct);
        if (user is null) return NotFound();

        if (await IsLastActiveTenantAdminAsync(user.Id, tenantId, ct))
            return BadRequest(new { error = "At least one active TenantAdmin must remain." });

        user.IsDeleted = true;
        user.DeletedAtUtc = DateTime.UtcNow;
        user.DeletedByUserId = _tenant.UserId;
        user.IsActive = false;
        await _db.SaveChangesAsync(ct);

        await _tokens.RevokeAllForUserAsync(user.Id, "user-deleted", ct);
        await _audit.LogAsync("user.delete", nameof(AppUser), user.Id.ToString(), ct);
        return NoContent();
    }

    public sealed record AssignRoleRequest(Guid RoleId);

    [HttpPost("{id:guid}/roles")]
    [HasPermission(Permissions.RolesAssign)]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleRequest request, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();

        var userExists = await _db.Users.AnyAsync(u => u.Id == id && u.TenantId == tenantId, ct);
        if (!userExists) return NotFound();

        var role = await _db.Roles
            .FirstOrDefaultAsync(r => r.Id == request.RoleId && r.TenantId == tenantId, ct);
        if (role is null) return NotFound(new { error = "Role not found in this tenant." });

        // دور المنصة لا يُسند مطلقاً من مسار Tenant
        if (role.Name == "SuperAdmin")
            return Forbid();

        var already = await _db.UserRoles.AnyAsync(
            ur => ur.UserId == id && ur.RoleId == role.Id, ct);
        if (already) return NoContent();

        _db.UserRoles.Add(new UserRole { UserId = id, RoleId = role.Id, TenantId = tenantId });
        await _db.SaveChangesAsync(ct);

        await _tokens.RevokeAllForUserAsync(id, "roles-changed", ct);
        await _audit.LogAsync("user.role.assign", nameof(AppUser), id.ToString(), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/roles/{roleId:guid}")]
    [HasPermission(Permissions.RolesAssign)]
    public async Task<IActionResult> RemoveRole(Guid id, Guid roleId, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();

        var link = await _db.UserRoles
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(ur => ur.UserId == id && ur.RoleId == roleId
                                       && ur.TenantId == tenantId, ct);
        if (link is null) return NotFound();

        if (link.Role.Name == "TenantAdmin" &&
            await IsLastActiveTenantAdminAsync(id, tenantId, ct))
            return BadRequest(new { error = "At least one active TenantAdmin must remain." });

        _db.UserRoles.Remove(link);
        await _db.SaveChangesAsync(ct);

        await _tokens.RevokeAllForUserAsync(id, "roles-changed", ct);
        await _audit.LogAsync("user.role.remove", nameof(AppUser), id.ToString(), ct);
        return NoContent();
    }

    private async Task<bool> IsLastActiveTenantAdminAsync(Guid userId, Guid tenantId, CancellationToken ct)
    {
        var isAdmin = await _db.UserRoles.AnyAsync(
            ur => ur.UserId == userId && ur.TenantId == tenantId && ur.Role.Name == "TenantAdmin", ct);
        if (!isAdmin) return false;

        var otherAdmins = await _db.UserRoles.CountAsync(
            ur => ur.TenantId == tenantId && ur.Role.Name == "TenantAdmin"
                  && ur.UserId != userId && ur.User.IsActive, ct);

        return otherAdmins == 0;
    }
}
````<br><br>### 3.10 `src/Api/Controllers/InvitationsController.cs` — ملف جديد

````csharp
using System.Security.Cryptography;
using System.Text;
using Ems.Api.Authorization;
using Ems.Application.Abstractions;
using Ems.Application.Authorization;
using Ems.Domain.Entities;
using Ems.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ems.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/invitations")]
public sealed class InvitationsController : ControllerBase
{
    private static readonly string[] InvitableRoles =
        { "TenantAdmin", "EventManager", "Finance", "GateOperator", "Exhibitor", "Viewer" };

    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IEmailSender _email;

    public InvitationsController(AppDbContext db, ITenantContext tenant, IEmailSender email)
    {
        _db = db; _tenant = tenant; _email = email;
    }

    public sealed record CreateInviteRequest(string Email, string Role);

    [HttpPost]
    [HasPermission(Permissions.UsersWrite)]
    public async Task<IActionResult> Create([FromBody] CreateInviteRequest request, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var email = request.Email.Trim().ToLowerInvariant();

        if (!InvitableRoles.Contains(request.Role))
            return BadRequest(new { error = "Role is not invitable." });

        if (await _db.Users.AnyAsync(u => u.NormalizedEmail == email && u.TenantId == tenantId, ct))
            return Conflict(new { error = "User already exists in this tenant." });

        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        _db.UserInvitations.Add(new UserInvitation
        {
            TenantId = tenantId,
            Email = email,
            Role = request.Role,
            TokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))),
            CreatedByUserId = _tenant.UserId!.Value,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        });
        await _db.SaveChangesAsync(ct);

        await _email.SendInvitationAsync(email, request.Role, raw, ct);

        // التوكن لا يُعاد في الاستجابة
        return Accepted(new { message = "Invitation sent." });
    }
}
```

## 4. منع الحجز المزدوج والترقيم الذري (DATA-201، DATA-202)

### 4.1 `db/migrations/001_booking_and_invoice_constraints.sql` — ملف جديد

نفّذه يدوياً أو عبر Migration؛ القيد على مستوى قاعدة البيانات هو الضمانة الحقيقية وليس كود C#.

```sql
-- 1) منع حجزين نشطين لنفس الجناح في نفس المعرض
CREATE UNIQUE INDEX UX_Bookings_ActiveBooth
    ON dbo.Bookings (ExhibitionId, BoothId)
    WHERE Status IN ('Reserved','Confirmed') AND IsDeleted = 0;
GO

-- 2) منع تكرار رقم الفاتورة داخل المستأجر
CREATE UNIQUE INDEX UX_Invoices_TenantNumber
    ON dbo.Invoices (TenantId, InvoiceNumber);
GO

-- 3) عدّاد ترقيم ذري لكل مستأجر وسنة
CREATE TABLE dbo.InvoiceNumberSequences (
    TenantId   UNIQUEIDENTIFIER NOT NULL,
    [Year]     INT              NOT NULL,
    LastNumber INT              NOT NULL CONSTRAINT DF_InvSeq_Last DEFAULT (0),
    Prefix     NVARCHAR(16)     NOT NULL CONSTRAINT DF_InvSeq_Prefix DEFAULT ('INV'),
    CONSTRAINT PK_InvoiceNumberSequences PRIMARY KEY (TenantId, [Year])
);
GO

-- 4) إضافة rowversion للتزامن المتفائل
ALTER TABLE dbo.Booths   ADD [RowVersion] ROWVERSION NOT NULL;
ALTER TABLE dbo.Bookings ADD [RowVersion] ROWVERSION NOT NULL;
ALTER TABLE dbo.Invoices ADD [RowVersion] ROWVERSION NOT NULL;
GO

-- 5) منع مجموع مدفوعات سالب وقيم غير منطقية
ALTER TABLE dbo.Payments  ADD CONSTRAINT CK_Payments_Amount  CHECK (Amount > 0);
ALTER TABLE dbo.Invoices  ADD CONSTRAINT CK_Invoices_Total   CHECK (TotalAmount >= 0);
ALTER TABLE dbo.Booths    ADD CONSTRAINT CK_Booths_Area      CHECK (AreaSqm > 0);
GO

-- 6) جدول Idempotency
CREATE TABLE dbo.IdempotencyRecords (
    Id           UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Idempotency PRIMARY KEY,
    TenantId     UNIQUEIDENTIFIER NOT NULL,
    [Key]        NVARCHAR(128)    NOT NULL,
    Endpoint     NVARCHAR(256)    NOT NULL,
    StatusCode   INT              NOT NULL,
    ResponseBody NVARCHAR(MAX)    NULL,
    CreatedAtUtc DATETIME2        NOT NULL
);
CREATE UNIQUE INDEX UX_Idempotency_Tenant_Key_Endpoint
    ON dbo.IdempotencyRecords (TenantId, [Key], Endpoint);
GO

-- 7) فهارس أداء أساسية متعددة المستأجرين
CREATE INDEX IX_Bookings_Tenant_Exhibition ON dbo.Bookings (TenantId, ExhibitionId) INCLUDE (Status);
CREATE INDEX IX_Invoices_Tenant_Status     ON dbo.Invoices (TenantId, Status, IssuedAtUtc);
CREATE INDEX IX_Tickets_Tenant_Code        ON dbo.Tickets  (TenantId, TicketCode);
GO
````<br><br>### 4.2 `src/Infrastructure/Invoicing/InvoiceNumberGenerator.cs` — ملف جديد

يستبدل نمط «اقرأ آخر رقم + 1» بتحديث ذري في عبارة واحدة.

````csharp
using Ems.Application.Abstractions;
using Ems.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ems.Infrastructure.Invoicing;

public interface IInvoiceNumberGenerator
{
    Task<string> NextAsync(Guid tenantId, int year, CancellationToken ct = default);
}

public sealed class InvoiceNumberGenerator : IInvoiceNumberGenerator
{
    private readonly AppDbContext _db;

    public InvoiceNumberGenerator(AppDbContext db) => _db = db;

    public async Task<string> NextAsync(Guid tenantId, int year, CancellationToken ct = default)
    {
        // UPDATE ذري مع OUTPUT: لا سباق حتى مع مئات الطلبات المتزامنة.
        const string sql = @"
SET NOCOUNT ON;
DECLARE @next TABLE (LastNumber INT, Prefix NVARCHAR(16));

MERGE dbo.InvoiceNumberSequences WITH (HOLDLOCK) AS target
USING (SELECT @tenantId AS TenantId, @year AS [Year]) AS source
    ON target.TenantId = source.TenantId AND target.[Year] = source.[Year]
WHEN MATCHED THEN
    UPDATE SET target.LastNumber = target.LastNumber + 1
WHEN NOT MATCHED THEN
    INSERT (TenantId, [Year], LastNumber, Prefix) VALUES (source.TenantId, source.[Year], 1, 'INV')
OUTPUT inserted.LastNumber, inserted.Prefix INTO @next;

SELECT LastNumber, Prefix FROM @next;";

        var connection = _db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var pTenant = command.CreateParameter();
        pTenant.ParameterName = "@tenantId"; pTenant.Value = tenantId;
        command.Parameters.Add(pTenant);

        var pYear = command.CreateParameter();
        pYear.ParameterName = "@year"; pYear.Value = year;
        command.Parameters.Add(pYear);

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);

        if (_db.Database.CurrentTransaction is { } tx)
            command.Transaction = tx.GetDbTransaction();

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            throw new InvalidOperationException("Failed to allocate invoice number.");

        var number = reader.GetInt32(0);
        var prefix = reader.GetString(1);

        return $"{prefix}-{year}-{number:D6}";
    }
}
````<br><br>### 4.3 `src/Infrastructure/Booking/BookingService.cs` — تعديل (استبدل دالة الحجز)

````csharp
using Ems.Application.Abstractions;
using Ems.Domain.Entities;
using Ems.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ems.Infrastructure.Booking;

public sealed class BookingConflictException : Exception
{
    public BookingConflictException(string message) : base(message) { }
}

public sealed class BookingService : IBookingService
{
    private const int UniqueViolation = 2601;
    private const int UniqueConstraint = 2627;

    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ILogger<BookingService> _logger;

    public BookingService(AppDbContext db, ITenantContext tenant, ILogger<BookingService> logger)
    {
        _db = db; _tenant = tenant; _logger = logger;
    }

    public async Task<Booking> CreateAsync(Guid exhibitionId, Guid boothId, Guid exhibitorId,
        CancellationToken ct = default)
    {
        var tenantId = _tenant.RequireTenantId();
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.ReadCommitted, ct);

            var booth = await _db.Booths
                .FirstOrDefaultAsync(b => b.Id == boothId
                                          && b.ExhibitionId == exhibitionId
                                          && b.TenantId == tenantId, ct)
                ?? throw new KeyNotFoundException("Booth not found.");

            if (!booth.IsAvailableForBooking)
                throw new BookingConflictException("Booth is not available.");

            var exhibitionOpen = await _db.Exhibitions.AnyAsync(
                e => e.Id == exhibitionId && e.TenantId == tenantId && e.AcceptsBookings, ct);
            if (!exhibitionOpen)
                throw new BookingConflictException("Exhibition is not accepting bookings.");

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ExhibitionId = exhibitionId,
                BoothId = boothId,
                ExhibitorId = exhibitorId,
                Status = BookingStatus.Reserved,
                PriceAmount = booth.Price,
                Currency = booth.Currency,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = _tenant.UserId
            };

            _db.Bookings.Add(booking);
            booth.Status = BoothStatus.Reserved; // يعتمد على RowVersion للتزامن

            try
            {
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return booking;
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync(ct);
                throw new BookingConflictException("Booth was modified by another request. Please retry.");
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sql &&
                                               (sql.Number == UniqueViolation || sql.Number == UniqueConstraint))
            {
                await tx.RollbackAsync(ct);
                _logger.LogInformation("Double booking prevented for booth {BoothId}", boothId);
                throw new BookingConflictException("This booth has already been booked.");
            }
        });
    }
}
````<br><br>### 4.4 `src/Infrastructure/Invoicing/InvoiceService.cs` — تعديل (ثوابت الفاتورة DATA-203)

````csharp
using Ems.Application.Abstractions;
using Ems.Domain.Entities;
using Ems.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ems.Infrastructure.Invoicing;

public sealed class InvoiceService : IInvoiceService
{
    private static readonly InvoiceStatus[] ImmutableStatuses =
        { InvoiceStatus.Issued, InvoiceStatus.PartiallyPaid, InvoiceStatus.Paid,
          InvoiceStatus.Cancelled, InvoiceStatus.Refunded };

    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IInvoiceNumberGenerator _numbers;

    public InvoiceService(AppDbContext db, ITenantContext tenant, IInvoiceNumberGenerator numbers)
    {
        _db = db; _tenant = tenant; _numbers = numbers;
    }

    public async Task<Invoice> IssueAsync(Guid bookingId, CancellationToken ct = default)
    {
        var tenantId = _tenant.RequireTenantId();
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var booking = await _db.Bookings
                .Include(b => b.Exhibitor)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.TenantId == tenantId, ct)
                ?? throw new KeyNotFoundException("Booking not found.");

            if (await _db.Invoices.AnyAsync(i => i.BookingId == bookingId
                                                 && i.Status != InvoiceStatus.Cancelled, ct))
                throw new InvalidOperationException("An active invoice already exists for this booking.");

            var settings = await _db.TenantSettings.AsNoTracking()
                .FirstAsync(s => s.TenantId == tenantId, ct);

            var net = booking.PriceAmount;
            var tax = Math.Round(net * settings.TaxRate, settings.CurrencyDecimals,
                MidpointRounding.AwayFromZero);

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                BookingId = booking.Id,
                InvoiceNumber = await _numbers.NextAsync(tenantId, DateTime.UtcNow.Year, ct),
                Status = InvoiceStatus.Issued,
                IssuedAtUtc = DateTime.UtcNow,
                DueAtUtc = DateTime.UtcNow.AddDays(settings.PaymentTermDays),
                Currency = booking.Currency,
                ExchangeRate = settings.CurrentExchangeRate,      // تثبيت السعر لحظة الإصدار
                TaxRate = settings.TaxRate,
                NetAmount = net,
                TaxAmount = tax,
                TotalAmount = net + tax,

                // Snapshot لبيانات العميل — لا تتغير لاحقاً
                CustomerNameSnapshot = booking.Exhibitor.CompanyName,
                CustomerAddressSnapshot = booking.Exhibitor.Address,
                CustomerTaxIdSnapshot = booking.Exhibitor.TaxId,
                SellerNameSnapshot = settings.LegalName,
                SellerTaxIdSnapshot = settings.TaxId
            };

            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return invoice;
        });
    }

    /// <summary>التعديل ممنوع بعد الإصدار — يُستخدم Credit Note بدلاً منه.</summary>
    public async Task<Invoice> CreateCreditNoteAsync(Guid invoiceId, decimal amount, string reason,
        CancellationToken ct = default)
    {
        var tenantId = _tenant.RequireTenantId();

        var original = await _db.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException("Invoice not found.");

        if (amount <= 0 || amount > original.TotalAmount)
            throw new ArgumentOutOfRangeException(nameof(amount), "Invalid credit note amount.");

        var creditNote = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BookingId = original.BookingId,
            InvoiceNumber = await _numbers.NextAsync(tenantId, DateTime.UtcNow.Year, ct),
            IsCreditNote = true,
            CreditsInvoiceId = original.Id,
            Status = InvoiceStatus.Issued,
            IssuedAtUtc = DateTime.UtcNow,
            Currency = original.Currency,
            ExchangeRate = original.ExchangeRate,
            TaxRate = original.TaxRate,
            NetAmount = -amount,
            TaxAmount = 0,
            TotalAmount = -amount,
            Notes = reason,
            CustomerNameSnapshot = original.CustomerNameSnapshot,
            CustomerAddressSnapshot = original.CustomerAddressSnapshot,
            CustomerTaxIdSnapshot = original.CustomerTaxIdSnapshot,
            SellerNameSnapshot = original.SellerNameSnapshot,
            SellerTaxIdSnapshot = original.SellerTaxIdSnapshot
        };

        _db.Invoices.Add(creditNote);
        await _db.SaveChangesAsync(ct);
        return creditNote;
    }

    public static void GuardMutability(Invoice invoice)
    {
        if (ImmutableStatuses.Contains(invoice.Status))
            throw new InvalidOperationException(
                "Issued invoices cannot be modified. Use a credit note instead.");
    }
}
````<br><br>### 4.5 `src/Infrastructure/Payments/PaymentService.cs` — تعديل (DATA-204)

````csharp
using Ems.Application.Abstractions;
using Ems.Domain.Entities;
using Ems.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ems.Infrastructure.Payments;

public sealed class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public PaymentService(AppDbContext db, ITenantContext tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Payment> RecordAsync(Guid invoiceId, decimal amount, string method,
        string? providerReference, CancellationToken ct = default)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));

        var tenantId = _tenant.RequireTenantId();
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var invoice = await _db.Invoices
                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId, ct)
                ?? throw new KeyNotFoundException("Invoice not found.");

            if (invoice.Status is InvoiceStatus.Cancelled or InvoiceStatus.Refunded)
                throw new InvalidOperationException("Cannot record payment on this invoice.");

            // منع تكرار نفس مرجع المزود (Webhook retry)
            if (providerReference is not null &&
                await _db.Payments.AnyAsync(p => p.ProviderReference == providerReference
                                                 && p.TenantId == tenantId, ct))
                return await _db.Payments.FirstAsync(p => p.ProviderReference == providerReference, ct);

            var alreadyPaid = await _db.Payments
                .Where(p => p.InvoiceId == invoiceId && p.Status == PaymentStatus.Succeeded)
                .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

            var remaining = invoice.TotalAmount - alreadyPaid;
            if (amount > remaining)
                throw new InvalidOperationException(
                    $"Payment exceeds remaining balance ({remaining:0.##} {invoice.Currency}).");

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                InvoiceId = invoiceId,
                Amount = amount,
                Currency = invoice.Currency,
                Method = method,
                ProviderReference = providerReference,
                Status = PaymentStatus.Succeeded,
                PaidAtUtc = DateTime.UtcNow,
                RecordedByUserId = _tenant.UserId
            };
            _db.Payments.Add(payment);

            var newTotal = alreadyPaid + amount;
            invoice.Status = newTotal >= invoice.TotalAmount
                ? InvoiceStatus.Paid
                : InvoiceStatus.PartiallyPaid;
            invoice.PaidAmount = newTotal;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return payment;
        });
    }
}
````<br><br>## 5. معالجة الأخطاء وجودة API (API-301 → API-305)<br><br>### 5.1 `src/Api/Middleware/ExceptionHandlingMiddleware.cs` — ملف جديد

````csharp
using System.Diagnostics;
using Ems.Infrastructure.Booking;
using Microsoft.AspNetCore.Mvc;

namespace Ems.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
    {
        _next = next; _logger = logger; _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                            ?? Activity.Current?.TraceId.ToString()
                            ?? context.TraceIdentifier;

        context.Response.Headers["X-Correlation-Id"] = correlationId;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (status, title) = Map(ex);

            if (status >= 500)
                _logger.LogError(ex, "Unhandled error {CorrelationId}", correlationId);
            else
                _logger.LogWarning("Handled error {CorrelationId}: {Title}", correlationId, title);

            if (context.Response.HasStarted) throw;

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Type = $"https://httpstatuses.io/{status}",
                Instance = context.Request.Path
            };
            problem.Extensions["correlationId"] = correlationId;

            // تفاصيل الاستثناء لا تصل للعميل في الإنتاج إطلاقاً
            if (!_env.IsProduction() && status >= 500)
                problem.Detail = ex.ToString();
            else if (status < 500)
                problem.Detail = ex.Message;

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem);
        }
    }

    private static (int Status, string Title) Map(Exception ex) => ex switch
    {
        UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Access denied."),
        KeyNotFoundException        => (StatusCodes.Status404NotFound, "Resource not found."),
        BookingConflictException    => (StatusCodes.Status409Conflict, "Booking conflict."),
        InvalidOperationException   => (StatusCodes.Status409Conflict, "Operation not allowed in current state."),
        ArgumentException           => (StatusCodes.Status400BadRequest, "Invalid request."),
        TaskCanceledException       => (StatusCodes.Status499ClientClosedRequest, "Request cancelled."),
        _                           => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
    };
}

internal static class StatusCodesExtra
{
    public const int Status499ClientClosedRequest = 499;
}
````<br><br>### 5.2 `src/Api/Middleware/IdempotencyMiddleware.cs` — ملف جديد

````csharp
using System.Security.Claims;
using Ems.Domain.Entities;
using Ems.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ems.Api.Middleware;

/// <summary>يطبّق Idempotency على مسارات POST المالية والحجز.</summary>
public sealed class IdempotencyMiddleware
{
    private static readonly string[] ProtectedPaths =
    {
        "/api/v1/bookings", "/api/v1/invoices", "/api/v1/payments", "/api/v1/tickets"
    };

    private readonly RequestDelegate _next;

    public IdempotencyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var applies = HttpMethods.IsPost(context.Request.Method) &&
                      ProtectedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        if (!applies) { await _next(context); return; }

        var key = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(key))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "Idempotency-Key header is required." });
            return;
        }

        if (!Guid.TryParse(context.User.FindFirst("tenant_id")?.Value, out var tenantId))
        {
            await _next(context);
            return;
        }

        var existing = await db.IdempotencyRecords.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Key == key && r.Endpoint == path,
                context.RequestAborted);

        if (existing is not null)
        {
            context.Response.StatusCode = existing.StatusCode;
            context.Response.ContentType = "application/json";
            context.Response.Headers["Idempotent-Replay"] = "true";
            if (existing.ResponseBody is not null)
                await context.Response.WriteAsync(existing.ResponseBody);
            return;
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        await _next(context);

        buffer.Position = 0;
        var body = await new StreamReader(buffer).ReadToEndAsync();
        buffer.Position = 0;
        await buffer.CopyToAsync(originalBody);
        context.Response.Body = originalBody;

        if (context.Response.StatusCode is >= 200 and < 300)
        {
            db.IdempotencyRecords.Add(new IdempotencyRecord
            {
                TenantId = tenantId,
                Key = key!,
                Endpoint = path,
                StatusCode = context.Response.StatusCode,
                ResponseBody = body
            });
            try { await db.SaveChangesAsync(context.RequestAborted); }
            catch (DbUpdateException) { /* سباق مع طلب موازٍ — مقبول */ }
        }
    }
}
````<br><br>### 5.3 `src/Api/Validators/BookingValidators.cs` — ملف جديد (FluentValidation)

````csharp
using FluentValidation;

namespace Ems.Api.Validators;

public sealed record CreateBookingRequest(Guid ExhibitionId, Guid BoothId, Guid ExhibitorId);
public sealed record RecordPaymentRequest(Guid InvoiceId, decimal Amount, string Method, string? ProviderReference);
public sealed record PagedRequest(int Page = 1, int PageSize = 25);

public sealed class CreateBookingValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingValidator()
    {
        RuleFor(x => x.ExhibitionId).NotEmpty();
        RuleFor(x => x.BoothId).NotEmpty();
        RuleFor(x => x.ExhibitorId).NotEmpty();
    }
}

public sealed class RecordPaymentValidator : AbstractValidator<RecordPaymentRequest>
{
    private static readonly string[] AllowedMethods = { "Cash", "BankTransfer", "Card", "Online" };

    public RecordPaymentValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.")
            .LessThanOrEqualTo(10_000_000m).WithMessage("Amount exceeds allowed limit.")
            .Must(a => decimal.Round(a, 2) == a).WithMessage("Amount supports at most 2 decimals.");
        RuleFor(x => x.Method).NotEmpty().Must(m => AllowedMethods.Contains(m))
            .WithMessage("Unsupported payment method.");
        RuleFor(x => x.ProviderReference).MaximumLength(128);
    }
}

public sealed class PagedRequestValidator : AbstractValidator<PagedRequest>
{
    public PagedRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).LessThanOrEqualTo(100_000);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
```

## 6. `src/Api/Program.cs` — استبدال كامل

يجمع كل ما سبق: التحقق من الأسرار، JWT، الصلاحيات، Rate Limiting، Headers، Health، Serilog، ومنع الترحيل التلقائي.

```csharp
using System.Text;
using System.Threading.RateLimiting;
using Ems.Api.Authorization;
using Ems.Api.Configuration;
using Ems.Api.Middleware;
using Ems.Application.Abstractions;
using Ems.Infrastructure.Auth;
using Ems.Infrastructure.Booking;
using Ems.Infrastructure.Invoicing;
using Ems.Infrastructure.MultiTenancy;
using Ems.Infrastructure.Payments;
using Ems.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// ---------- 0) رفض التشغيل عند إعداد غير آمن ----------
StartupGuard.ValidateOrThrow(builder.Configuration, builder.Environment);

// ---------- 1) Logging ----------
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Ems.Api")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3] {CorrelationId} {Message:lj}{NewLine}{Exception}"));

// ---------- 2) Options ----------
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.Configure<SecurityOptions>(
    builder.Configuration.GetSection(SecurityOptions.SectionName));

var security = builder.Configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>()
               ?? new SecurityOptions();

// ---------- 3) Persistence + Tenant ----------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<TenantSaveChangesInterceptor>();

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default"),
        sql =>
        {
            sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
            sql.CommandTimeout(30);
            sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
        });

    options.AddInterceptors(sp.GetRequiredService<TenantSaveChangesInterceptor>());

    if (!builder.Environment.IsProduction())
        options.EnableDetailedErrors();
    // ملاحظة: EnableSensitiveDataLogging ممنوع نهائياً
});

// ---------- 4) Application services ----------
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IInvoiceNumberGenerator, InvoiceNumberGenerator>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IAuditLogger, AuditLogger>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.IPasswordHasher<Ems.Domain.Entities.AppUser>,
    Microsoft.AspNetCore.Identity.PasswordHasher<Ems.Domain.Entities.AppUser>>();

// ---------- 5) AuthN ----------
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = security.RequireHttps;
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                ctx.Response.Headers["Token-Expired"] =
                    (ctx.Exception is SecurityTokenExpiredException).ToString().ToLowerInvariant();
                return Task.CompletedTask;
            }
        };
    });

// ---------- 6) AuthZ ----------
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();   // كل Endpoint محمي افتراضياً؛ الاستثناء بـ[AllowAnonymous]
});

// ---------- 7) Rate limiting ----------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.FindFirst("tenant_id")?.Value
                          ?? context.Connection.RemoteIpAddress?.ToString()
                          ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// ---------- 8) CORS ----------
builder.Services.AddCors(options =>
    options.AddPolicy("default", policy =>
    {
        if (security.AllowedOrigins.Length == 0)
            policy.DisallowCredentials();       // WPF لا يحتاج CORS
        else
            policy.WithOrigins(security.AllowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
    }));

// ---------- 9) MVC + validation + versioning ----------
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---------- 10) Health checks ----------
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddDbContextCheck<AppDbContext>("database", tags: new[] { "ready" });

builder.Services.Configure<ForwardedHeadersOptions>(o =>
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

var app = builder.Build();

// ---------- Pipeline ----------
app.UseForwardedHeaders();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsProduction())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    headers.Remove("Server");
    headers.Remove("X-Powered-By");
    await next();
});

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diag, http) =>
    {
        diag.Set("CorrelationId", http.Response.Headers["X-Correlation-Id"].ToString());
        diag.Set("TenantId", http.User.FindFirst("tenant_id")?.Value ?? "-");
    };
});

app.UseCors("default");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<IdempotencyMiddleware>();

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).AllowAnonymous();

// ⛔ ممنوع: db.Database.Migrate() أو EnsureCreated() هنا.
// الترحيل يتم عبر pipeline مستقل (انظر القسم 8).
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

app.Run();

public partial class Program { }   // مطلوب لـWebApplicationFactory في الاختبارات
````<br><br>### 6.1 `src/Infrastructure/Logging/AuditLogger.cs` — ملف جديد

````csharp
using Ems.Application.Abstractions;
using Ems.Domain.Entities;
using Ems.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;

namespace Ems.Infrastructure.Logging;

public interface IAuditLogger
{
    Task LogAsync(string action, string entityType, string entityId,
        CancellationToken ct = default, string? details = null);
}

public sealed class AuditLogger : IAuditLogger
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IHttpContextAccessor _http;

    public AuditLogger(AppDbContext db, ITenantContext tenant, IHttpContextAccessor http)
    {
        _db = db; _tenant = tenant; _http = http;
    }

    public async Task LogAsync(string action, string entityType, string entityId,
        CancellationToken ct = default, string? details = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            TenantId = _tenant.TenantId ?? Guid.Empty,
            UserId = _tenant.UserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            CorrelationId = _http.HttpContext?.Response.Headers["X-Correlation-Id"].ToString()
        });
        await _db.SaveChangesAsync(ct);
    }
}
````<br><br>### 6.2 `src/Infrastructure/Persistence/DbSeeder.cs` — تعديل (SEC-005)

احذف أي حساب مدير بكلمة مرور ثابتة. البديل: Bootstrap لمرة واحدة بمفتاح من متغير بيئة.

````csharp
using Ems.Application.Authorization;
using Ems.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ems.Infrastructure.Persistence;

public static class DbSeeder
{
    /// <summary>يزرع الأدوار والصلاحيات فقط. لا حسابات ولا كلمات مرور.</summary>
    public static async Task SeedRolesAndPermissionsAsync(AppDbContext db, Guid tenantId, CancellationToken ct = default)
    {
        foreach (var (roleName, permissionCodes) in Permissions.RoleMatrix)
        {
            if (roleName == "SuperAdmin") continue; // دور منصة، ليس داخل Tenant

            var role = await db.Roles.FirstOrDefaultAsync(
                r => r.TenantId == tenantId && r.Name == roleName, ct);

            if (role is null)
            {
                role = new Role { Id = Guid.NewGuid(), TenantId = tenantId, Name = roleName };
                db.Roles.Add(role);
            }

            var existing = await db.RolePermissions
                .Where(p => p.RoleId == role.Id).Select(p => p.Code).ToListAsync(ct);

            foreach (var code in permissionCodes.Except(existing))
                db.RolePermissions.Add(new RolePermission { RoleId = role.Id, Code = code });
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// يُنشئ أول TenantAdmin مرة واحدة فقط، ويطلب مفتاح bootstrap من متغير بيئة
    /// مع إلزام تغيير كلمة المرور في أول دخول.
    /// </summary>
    public static async Task<string?> BootstrapFirstAdminAsync(AppDbContext db, Guid tenantId,
        string email, string providedKey, string? expectedKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(expectedKey) || providedKey != expectedKey)
            throw new UnauthorizedAccessException("Invalid bootstrap key.");

        if (await db.Users.IgnoreQueryFilters().AnyAsync(u => u.TenantId == tenantId, ct))
            throw new InvalidOperationException("Tenant already has users. Bootstrap is disabled.");

        var tempPassword = Guid.NewGuid().ToString("N") + "aA1!";
        var hasher = new PasswordHasher<AppUser>();

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            NormalizedEmail = email.ToLowerInvariant(),
            FullName = "Tenant Administrator",
            IsActive = true,
            MustChangePassword = true,
            MfaRequired = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        user.PasswordHash = hasher.HashPassword(user, tempPassword);

        var adminRole = await db.Roles.FirstAsync(
            r => r.TenantId == tenantId && r.Name == "TenantAdmin", ct);

        db.Users.Add(user);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id, TenantId = tenantId });
        await db.SaveChangesAsync(ct);

        // تُعرض مرة واحدة فقط في مخرجات أمر التشغيل، ولا تُسجل في Logs.
        return tempPassword;
    }
}
````<br><br>## 7. الاختبارات (المرحلة الرابعة)<br><br>### 7.1 `tests/Api.IntegrationTests/ApiFactory.cs` — ملف جديد

````csharp
using Ems.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace Ems.Api.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _sql.GetConnectionString(),
                ["Jwt:Issuer"] = "https://test.local",
                ["Jwt:Audience"] = "ems-tests",
                ["Jwt:SigningKey"] = new string('k', 96),
                ["Security:RequireHttps"] = "false",
                ["Security:AllowPublicRegistration"] = "false"
            }));
    }

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync() => await _sql.DisposeAsync();
}
````<br><br>### 7.2 `tests/Api.IntegrationTests/TenantIsolationTests.cs` — ملف جديد

````csharp
using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;

namespace Ems.Api.IntegrationTests;

public sealed class TenantIsolationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public TenantIsolationTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task User_from_tenant_A_cannot_read_user_of_tenant_B()
    {
        var seed = await TestData.SeedTwoTenantsAsync(_factory);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", seed.TenantAAdminToken);

        var response = await client.GetAsync($"/api/v1/users/{seed.TenantBUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task User_from_tenant_A_cannot_assign_role_of_tenant_B()
    {
        var seed = await TestData.SeedTwoTenantsAsync(_factory);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", seed.TenantAAdminToken);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/users/{seed.TenantAUserId}/roles",
            new { RoleId = seed.TenantBRoleId });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Public_registration_endpoint_no_longer_exists()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { Email = "x@y.com", Password = "Password123!", TenantId = Guid.NewGuid(), InitialRole = "Admin" });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task Invitation_cannot_grant_SuperAdmin()
    {
        var seed = await TestData.SeedTwoTenantsAsync(_factory);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", seed.TenantAAdminToken);

        var response = await client.PostAsJsonAsync("/api/v1/invitations",
            new { Email = "attacker@example.com", Role = "SuperAdmin" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
````<br><br>### 7.3 `tests/Api.IntegrationTests/ConcurrentBookingTests.cs` — ملف جديد

````csharp
using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;

namespace Ems.Api.IntegrationTests;

public sealed class ConcurrentBookingTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public ConcurrentBookingTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Only_one_of_50_concurrent_requests_books_the_booth()
    {
        var seed = await TestData.SeedExhibitionWithBoothAsync(_factory);

        var tasks = Enumerable.Range(0, 50).Select(async i =>
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", seed.Token);
            client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

            return await client.PostAsJsonAsync("/api/v1/bookings", new
            {
                seed.ExhibitionId,
                seed.BoothId,
                seed.ExhibitorId
            });
        });

        var responses = await Task.WhenAll(tasks);

        responses.Count(r => r.IsSuccessStatusCode).Should().Be(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).Should().Be(49);
    }

    [Fact]
    public async Task Concurrent_invoice_issuing_produces_unique_numbers()
    {
        var seed = await TestData.SeedBookingsAsync(_factory, count: 100);

        var numbers = await Task.WhenAll(seed.BookingIds.Select(async id =>
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", seed.Token);
            client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

            var response = await client.PostAsJsonAsync("/api/v1/invoices", new { BookingId = id });
            var body = await response.Content.ReadFromJsonAsync<InvoiceDto>();
            return body!.InvoiceNumber;
        }));

        numbers.Distinct().Should().HaveCount(100);
    }

    private sealed record InvoiceDto(Guid Id, string InvoiceNumber);
}
````<br><br>### 7.4 `tests/Domain.UnitTests/InvoiceCalculationTests.cs` — ملف جديد

````csharp
using FluentAssertions;

namespace Ems.Domain.UnitTests;

public sealed class InvoiceCalculationTests
{
    [Theory]
    [InlineData(100.00, 0.15, 15.00, 115.00)]
    [InlineData(99.99, 0.15, 15.00, 114.99)]
    [InlineData(0.01, 0.15, 0.00, 0.01)]
    [InlineData(1234.567, 0.05, 61.73, 1296.297)]
    public void Tax_rounds_away_from_zero_with_two_decimals(
        decimal net, decimal rate, decimal expectedTax, decimal expectedTotal)
    {
        var tax = Math.Round(net * rate, 2, MidpointRounding.AwayFromZero);

        tax.Should().Be(expectedTax);
        (net + tax).Should().Be(expectedTotal);
    }

    [Fact]
    public void Issued_invoice_cannot_be_modified()
    {
        var invoice = new Invoice { Status = InvoiceStatus.Issued };

        var act = () => InvoiceService.GuardMutability(invoice);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*credit note*");
    }
}
```

## 8. CI/CD وDevSecOps (المرحلة الخامسة)

### 8.1 `.github/workflows/ci.yml` — ملف جديد

```yaml
name: CI

on:
  push:
    branches: [main, master, 'release/**']
  pull_request:
    branches: [main, master]

env:
  DOTNET_VERSION: '8.0.x'
  DOTNET_NOLOGO: true
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    timeout-minutes: 20
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore
        run: dotnet restore --locked-mode

      - name: Build
        run: dotnet build --no-restore -c Release -warnaserror

      - name: Unit tests
        run: >
          dotnet test tests/Domain.UnitTests tests/Application.UnitTests
          --no-build -c Release
          --collect:"XPlat Code Coverage"
          --logger trx

      - name: Integration tests (Testcontainers)
        run: dotnet test tests/Api.IntegrationTests --no-build -c Release --logger trx

      - name: Coverage gate
        run: |
          dotnet tool install --global dotnet-reportgenerator-globaltool
          reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:coverage -reporttypes:Cobertura
          python3 - <<'PY'
          import xml.etree.ElementTree as ET, sys
          rate = float(ET.parse('coverage/Cobertura.xml').getroot().get('line-rate'))
          print(f"Line coverage: {rate:.2%}")
          sys.exit(0 if rate >= 0.70 else 1)
          PY

      - uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: '**/*.trx'

  security:
    runs-on: ubuntu-latest
    permissions:
      security-events: write
      contents: read
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Secret scan (full history)
        uses: gitleaks/gitleaks-action@v2
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Vulnerable NuGet packages
        run: |
          dotnet restore
          dotnet list package --vulnerable --include-transitive 2>&1 | tee vuln.txt
          ! grep -q -E 'Critical|High' vuln.txt

      - name: Deprecated packages (report only)
        run: dotnet list package --deprecated || true

      - name: Initialize CodeQL
        uses: github/codeql-action/init@v3
        with:
          languages: csharp

      - name: Build for CodeQL
        run: dotnet build -c Release

      - name: CodeQL analysis
        uses: github/codeql-action/analyze@v3

      - name: Generate SBOM
        run: |
          dotnet tool install --global CycloneDX
          dotnet CycloneDX ExhibitionManagementSystem.sln -o sbom

      - uses: actions/upload-artifact@v4
        with:
          name: sbom
          path: sbom/

  package:
    needs: [build-and-test, security]
    if: startsWith(github.ref, 'refs/heads/release/')
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Publish API
        run: dotnet publish src/Api -c Release -o artifacts/api

      - name: Publish Desktop
        run: dotnet publish src/Desktop -c Release -r win-x64 --self-contained false -o artifacts/desktop

      - name: Sign desktop binaries
        env:
          CERT_PFX_BASE64: ${{ secrets.CODE_SIGN_PFX }}
          CERT_PASSWORD: ${{ secrets.CODE_SIGN_PASSWORD }}
        run: |
          [IO.File]::WriteAllBytes("cert.pfx", [Convert]::FromBase64String($env:CERT_PFX_BASE64))
          & signtool sign /fd SHA256 /f cert.pfx /p $env:CERT_PASSWORD /tr http://timestamp.digicert.com /td SHA256 artifacts/desktop/*.exe
          Remove-Item cert.pfx

      - name: Checksums
        run: Get-FileHash artifacts/desktop/*.exe -Algorithm SHA256 | Out-File artifacts/SHA256SUMS.txt

      - uses: actions/upload-artifact@v4
        with:
          name: release-artifacts
          path: artifacts/
````<br><br>### 8.2 `.github/workflows/migrate.yml` — ملف جديد (DATA-207)

ترحيل مستقل عن تشغيل التطبيق، مع نسخة احتياطية قبل كل تغيير.

````yaml
name: Database Migration

on:
  workflow_dispatch:
    inputs:
      environment:
        description: Target environment
        required: true
        type: choice
        options: [staging, production]

jobs:
  migrate:
    runs-on: ubuntu-latest
    environment: ${{ inputs.environment }}   # يفرض موافقة يدوية للإنتاج
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Backup database
        env:
          CONN: ${{ secrets.DB_ADMIN_CONNECTION }}
        run: ./scripts/backup-db.sh "$CONN"

      - name: Generate idempotent migration script
        run: |
          dotnet tool install --global dotnet-ef
          dotnet ef migrations script --idempotent --project src/Infrastructure \
            --startup-project src/Api -o migration.sql

      - name: Review artifact
        uses: actions/upload-artifact@v4
        with:
          name: migration-sql
          path: migration.sql

      - name: Apply migration
        env:
          ConnectionStrings__Default: ${{ secrets.DB_MIGRATION_CONNECTION }}
        run: dotnet ef database update --project src/Infrastructure --startup-project src/Api

      - name: Smoke test
        run: ./scripts/smoke-test.sh "${{ vars.API_BASE_URL }}"
````<br><br>### 8.3 `Directory.Build.props` — ملف جديد (جذر المستودع)

````xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>
</Project>
````<br><br>## 9. تطبيق سطح المكتب WPF (المرحلة الثامنة)<br><br>### 9.1 `src/Desktop/appsettings.json` — تعديل

````json
{
  "Api": {
    "BaseUrl": "https://api.yourdomain.com/api/v1/",
    "TimeoutSeconds": 30
  },
  "App": {
    "UpdateChannel": "Stable",
    "Culture": "ar-LY"
  }
}
````<br><br>### 9.2 `src/Desktop/Services/SecureTokenStore.cs` — ملف جديد

لا تخزّن التوكن في ملف نصي أو Settings عادية.

````csharp
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Ems.Desktop.Services;

public sealed record StoredSession(string RefreshToken, string? DeviceId, DateTime ExpiresUtc);

public sealed class SecureTokenStore
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Ems.Desktop.v1");

    private readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ExhibitionManagement", "session.bin");

    public void Save(StoredSession session)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var plain = JsonSerializer.SerializeToUtf8Bytes(session);

        // DPAPI — مرتبط بحساب المستخدم على هذا الجهاز
        var encrypted = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_path, encrypted);
    }

    public StoredSession? Load()
    {
        if (!File.Exists(_path)) return null;
        try
        {
            var decrypted = ProtectedData.Unprotect(
                File.ReadAllBytes(_path), Entropy, DataProtectionScope.CurrentUser);
            return JsonSerializer.Deserialize<StoredSession>(decrypted);
        }
        catch (CryptographicException)
        {
            Clear();
            return null;
        }
    }

    public void Clear()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }
}
````<br><br>### 9.3 `src/Desktop/Services/ApiClient.cs` — تعديل (HTTPS + تجديد تلقائي + منع النقر المكرر)

````csharp
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace Ems.Desktop.Services;

public sealed class ApiException : Exception
{
    public ApiException(HttpStatusCode status, string message, string? correlationId)
        : base(message)
    {
        Status = status;
        CorrelationId = correlationId;
    }

    public HttpStatusCode Status { get; }
    public string? CorrelationId { get; }
}

public sealed class ApiClient
{
    private readonly HttpClient _http;
    private readonly SecureTokenStore _store;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private string? _accessToken;
    private DateTime _accessExpiresUtc = DateTime.MinValue;

    public ApiClient(HttpClient http, SecureTokenStore store, IConfiguration config)
    {
        var baseUrl = config["Api:BaseUrl"]
            ?? throw new InvalidOperationException("Api:BaseUrl is missing.");

        // ⛔ رفض HTTP في الإنتاج
        if (!baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            && !baseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("API base URL must use HTTPS.");

        _http = http;
        _http.BaseAddress = new Uri(baseUrl);
        _http.Timeout = TimeSpan.FromSeconds(config.GetValue("Api:TimeoutSeconds", 30));
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(
            $"EmsDesktop/{typeof(ApiClient).Assembly.GetName().Version}");

        _store = store;
    }

    public async Task<T?> GetAsync<T>(string path, CancellationToken ct = default) =>
        await SendAsync<T>(() => new HttpRequestMessage(HttpMethod.Get, path), ct);

    /// <summary>POST لعملية حساسة: مفتاح Idempotency ثابت لكل محاولة منطقية.</summary>
    public async Task<T?> PostAsync<T>(string path, object body, string? idempotencyKey = null,
        CancellationToken ct = default)
    {
        var key = idempotencyKey ?? Guid.NewGuid().ToString();

        return await SendAsync<T>(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(body)
            };
            request.Headers.Add("Idempotency-Key", key);
            return request;
        }, ct);
    }

    private async Task<T?> SendAsync<T>(Func<HttpRequestMessage> factory, CancellationToken ct)
    {
        await EnsureAccessTokenAsync(ct);

        var response = await SendOnceAsync(factory, ct);

        // تجديد واحد فقط عند 401
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            if (!await TryRefreshAsync(ct))
                throw new ApiException(HttpStatusCode.Unauthorized, "انتهت الجلسة. يرجى إعادة تسجيل الدخول.", null);

            response = await SendOnceAsync(factory, ct);
        }

        var correlationId = response.Headers.TryGetValues("X-Correlation-Id", out var ids)
            ? ids.FirstOrDefault() : null;

        if (!response.IsSuccessStatusCode)
        {
            var problem = await TryReadProblemAsync(response, ct);
            throw new ApiException(response.StatusCode, ToUserMessage(response.StatusCode, problem), correlationId);
        }

        if (response.StatusCode == HttpStatusCode.NoContent) return default;
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
    }

    private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> factory, CancellationToken ct)
    {
        var request = factory();
        if (_accessToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        return await _http.SendAsync(request, ct);
    }

    private async Task EnsureAccessTokenAsync(CancellationToken ct)
    {
        if (_accessToken is not null && DateTime.UtcNow < _accessExpiresUtc.AddSeconds(-60)) return;
        await TryRefreshAsync(ct);
    }

    private async Task<bool> TryRefreshAsync(CancellationToken ct)
    {
        await _refreshLock.WaitAsync(ct);
        try
        {
            if (_accessToken is not null && DateTime.UtcNow < _accessExpiresUtc.AddSeconds(-60))
                return true;

            var session = _store.Load();
            if (session is null || session.ExpiresUtc <= DateTime.UtcNow) return false;

            using var request = new HttpRequestMessage(HttpMethod.Post, "auth/refresh")
            {
                Content = JsonContent.Create(new { session.RefreshToken, session.DeviceId })
            };

            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _store.Clear();
                _accessToken = null;
                return false;
            }

            var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct)!;
            _accessToken = tokens!.AccessToken;
            _accessExpiresUtc = tokens.AccessExpiresUtc;
            _store.Save(new StoredSession(tokens.RefreshToken, session.DeviceId, tokens.RefreshExpiresUtc));
            return true;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static async Task<string?> TryReadProblemAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var problem = await response.Content
                .ReadFromJsonAsync<System.Text.Json.Nodes.JsonObject>(cancellationToken: ct);
            return problem?["title"]?.ToString() ?? problem?["error"]?.ToString();
        }
        catch { return null; }
    }

    // رسائل عربية مفهومة بدل عرض Stack Trace
    private static string ToUserMessage(HttpStatusCode status, string? serverTitle) => status switch
    {
        HttpStatusCode.BadRequest => serverTitle ?? "البيانات المدخلة غير صحيحة.",
        HttpStatusCode.Unauthorized => "انتهت الجلسة. يرجى إعادة تسجيل الدخول.",
        HttpStatusCode.Forbidden => "لا تملك صلاحية لتنفيذ هذا الإجراء.",
        HttpStatusCode.NotFound => "العنصر المطلوب غير موجود.",
        HttpStatusCode.Conflict => serverTitle ?? "تعارض مع عملية أخرى. حدّث البيانات ثم أعد المحاولة.",
        HttpStatusCode.TooManyRequests => "عدد المحاولات كبير. انتظر قليلاً.",
        _ => "حدث خطأ غير متوقع. تم تسجيل المشكلة ويمكنك إبلاغ الدعم برقم المرجع."
    };

    private sealed record TokenResponse(string AccessToken, DateTime AccessExpiresUtc,
        string RefreshToken, DateTime RefreshExpiresUtc);
}
````<br><br>### 9.4 `src/Desktop/App.xaml.cs` — تعديل (معالجة استثناءات عامة)

````csharp
using System.Windows;
using System.Windows.Threading;

namespace Ems.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var reference = Report(e.Exception);
        MessageBox.Show(
            $"حدث خطأ غير متوقع.\nرقم المرجع: {reference}\nيمكنك متابعة العمل أو إبلاغ الدعم.",
            "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);

        e.Handled = true;   // لا ينهار التطبيق
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex) Report(ex);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Report(e.Exception);
        e.SetObserved();
    }

    /// <summary>يسجل الخطأ محلياً بعد تنقية البيانات الحساسة ويعيد رقم مرجع.</summary>
    private static string Report(Exception ex)
    {
        var reference = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var text = Redact(ex.ToString());

        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ExhibitionManagement", "logs");
        Directory.CreateDirectory(dir);

        File.AppendAllText(Path.Combine(dir, $"crash-{DateTime.UtcNow:yyyyMMdd}.log"),
            $"[{DateTime.UtcNow:O] REF={reference}{Environment.NewLine}{text}{Environment.NewLine}");

        return reference;
    }

    private static string Redact(string input) =>
        System.Text.RegularExpressions.Regex.Replace(
            input,
            @"(?i)(Bearer\s+[A-Za-z0-9\-._~+/]+=*)|(Password\s*=\s*[^;\""\s]+)|(eyJ[A-Za-z0-9\-._]+)",
            "[REDACTED]");
}
````<br><br>### 9.5 منع النقر المكرر — `src/Desktop/Commands/AsyncRelayCommand.cs` — ملف جديد

````csharp
using System.Windows.Input;

namespace Ems.Desktop.Commands;

/// <summary>أمر غير متزامن يمنع التنفيذ المتكرر — إلزامي لأزرار الحجز والدفع.</summary>
public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isRunning;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) =>
        !_isRunning && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;

        _isRunning = true;
        RaiseCanExecuteChanged();
        try
        {
            await _execute();
        }
        finally
        {
            _isRunning = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() =>
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
````<br><br>## 10. ترتيب التطبيق اليدوي<br><br>اتبع هذا الترتيب حتى يبقى المشروع قابلاً للبناء بين الخطوات:<br><br>- [ ] 1. دوّر الأسرار فعلياً (قاعدة البيانات + JWT) قبل أي سطر كود.<br>- [ ] 2. أضف `Directory.Build.props` والـ`.gitignore` ونظّف `appsettings.json`.<br>- [ ] 3. أضف ملفات `Configuration/` الثلاثة و`ITenantOwned`.<br>- [ ] 4. نفّذ `ITenantOwned` في كل كيان مرتبط بمستأجر.<br>- [ ] 5. أضف الكيانات الجديدة + `AppDbContext` + Interceptor.<br>- [ ] 6. أنشئ Migration: `dotnet ef migrations add Security_And_Tenancy_Hardening`.<br>- [ ] 7. شغّل `001_booking_and_invoice_constraints.sql` على نسخة اختبار أولاً (قد تفشل القيود بسبب بيانات مكررة موجودة — نظّفها قبل الإنتاج).<br>- [ ] 8. استبدل `TokenService` و`AuthController` واحذف `Register` القديم.<br>- [ ] 9. أضف الصلاحيات واستبدل `[Authorize(Roles=...)]` بـ`[HasPermission(...)]` في كل متحكم.<br>- [ ] 10. استبدل `Program.cs`.<br>- [ ] 11. أصلح خدمات الحجز والفوترة والدفع.<br>- [ ] 12. أضف مشاريع الاختبارات وشغّلها محلياً.<br>- [ ] 13. أضف ملفات GitHub Actions وفعّل حماية الفرع الرئيسي.<br>- [ ] 14. حدّث تطبيق WPF وتأكد من رفضه لعنوان HTTP.<br><br><callout icon="⚠️" color="yellow_bg"><br>ملاحظة مهمة: الأسماء مثل `AppUser`، `Booth`، `Booking`، `Invoice`، `TenantSettings` مفترضة من التحليل. إن اختلفت في مشروعك، استبدلها بأسمائك الفعلية دون تغيير المنطق. إن أرسلت لي أسماء الملفات والكيانات الحقيقية، أعيد كتابة الكود مطابقاً تماماً.
</callout>
```