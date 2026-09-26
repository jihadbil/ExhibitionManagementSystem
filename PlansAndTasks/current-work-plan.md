# خطة العمل الموثقة — ExhibitionManagementSystem

**الإصدار:** 2.0  
**تاريخ التحقق:** 2026-09-17  
**القرار الحالي:** **No-Go** للإطلاق أو إدخال بيانات عملاء حقيقية حتى تدوير الأسرار واختبار الـMigrations على Staging  
**مصادر الخطة:** `readme.md` الجديد + فحص الكود الحالي + Build وفحص NuGet

## 1. الهدف

نقل النظام من نموذج واسع الوظائف وعالي المخاطر إلى منتج إدارة معارض متعدد المستأجرين قابل للتشغيل التجاري، مع إعطاء الأولوية للاحتواء الأمني، عزل المستأجرين، سلامة الحجوزات والماليات، الاختبارات، ثم الجاهزية التشغيلية وتجربة المستخدم.

## 2. تقييم README الجديد

القسم التخطيطي الأول من README، من بوابات القرار G0–G5 وحتى الـPilot، مناسب كأساس للمنتج ويُعتمد على مستوى الأهداف والمخاطر.

أما ملحق «كود الإصلاح الكامل» فلا يُنسخ مباشرة للأسباب التالية:

- يفترض بنية `src/Api`, `src/Domain`, `src/Infrastructure` و`tests/`، بينما المستودع يستخدم ستة مشاريع بأسمائها الحالية.
- يستهدف `.NET 8` في مواضع متعددة، بينما جميع المشاريع الحالية تستهدف `.NET 10`.
- يستخدم أسماء مفترضة مثل `AppUser`, `Booking`, و`AppDbContext` تختلف عن `ApplicationUser`, `BoothReservation`, و`ApplicationDbContext` الفعلية.
- أمثلة CI تشير إلى Solution ومسارات Publish وMigration غير موجودة في المستودع.
- بعض الحلول المقترحة تحتاج تكييفًا مع Identity وUnitOfWork وWPF الحاليين واختبارات قبل اعتمادها.

**القرار:** نعتمد أهداف README ومعايير القبول، وننفذها تدريجيًا على الملفات والأسماء الفعلية المبينة في هذه الخطة.

## 3. خط الأساس الفعلي

| العنصر | الحالة المتحققة |
|---|---|
| بنية الحل | 7 مشاريع بعد إضافة مشروع اختبارات API/Security |
| إطار العمل | .NET 10 وEF Core 10 وWPF |
| حجم المصدر | 491 ملف C# و67 ملف XAML |
| قاعدة البيانات | SQL Server و15 ملف Migration/Designer/Snapshot داخل مجلد Migrations |
| البناء | Rebuild ناجح: 0 أخطاء و0 تحذيرات |
| الاختبارات | 38 اختبارًا ناجحًا في مشروع `ExhibitionManagementSystem.Api.Tests` |
| CI/CD | Workflow لـBuild/Test/NuGet audit/Secret scan/Migration script |
| الاعتماديات | صفر ثغرات NuGet معروفة؛ OpenTK/GLWpfControl/SkiaSharp محدثة والبناء نظيف |
| الفرع الحالي | `codex/security-containment`؛ حماية الفرع الخارجية غير موثقة |
| README والخطة | كلاهما غير متتبعين في Git وقت الفحص |

## 4. المخاطر المؤكدة من الكود

### P0 — احتواء فوري

| المعرّف | الخطر المثبت | الدليل/المسار | الأثر |
|---|---|---|---|
| SEC-001 | اتصال قاعدة البيانات ومفتاح JWT مهيآن في ملفات متتبعة | `ExhibitionManagementSystem/appsettings.json` و`appsettings.Production.json` | اختراق قاعدة البيانات وتزوير Tokens |
| SEC-002 | طباعة سلسلة اتصال قاعدة البيانات كاملة عند التشغيل | `ExhibitionManagementSystem/Program.cs` | تسريب بيانات الاعتماد إلى Console/Logs |
| SEC-003 | تسجيل عام مجهول يختار `TenantID` و`InitialRole` ويمكنه إنشاء الدور | `AuthController.cs`, `RegisterRequestDto.cs`, `AuthService.cs` | تصعيد إلى Admin/SuperAdmin وعبور Tenant |
| SEC-004 | مدير افتراضي بكلمة مرور ثابتة ويُشغّل Seeder عند بدء API | `DataSeeder.cs`, `Program.cs` | دخول معروف إلى النظام |
| SEC-005 | مفاتيح JWT احتياطية ثابتة داخل الشفرة | `Program.cs`, `AuthService.cs` | تزوير Tokens عند غياب الإعداد |
| SEC-006 | تطبيق Desktop يتصل بعنوان HTTP عام في التطوير والإنتاج | ملفا `ExhibitionManagementSystem.DeskTop/appsettings*.json` | اعتراض بيانات الدخول والجلسات |
| SEC-007 | لا يوجد عزل Tenant مركزي | `ApplicationDbContext.cs` يطبق Filter للحذف المنطقي فقط | تسريب أو تعديل بيانات بين العملاء |
| SEC-008 | عمليات مستخدمين وأدوار لا تتحقق من Tenant المنفذ | `UsersController.cs`, `RolesController.cs`, `AuthService.cs` | IDOR وإدارة مستخدمي عميل آخر |

### P1 — أمان وسلامة بيانات

| المعرّف | الخطر المثبت | الملاحظة |
|---|---|---|
| SEC-101 | Login يكشف الفرق بين مستخدم غير موجود وكلمة مرور خاطئة | يسهل تعداد الحسابات |
| SEC-102 | `lockoutOnFailure: false` ولا يوجد Rate Limiting | هجمات تخمين كلمات المرور |
| SEC-103 | Refresh Token محفوظ كنص خام وحقل واحد لكل مستخدم | سرقة الجلسة وصعوبة إدارة الأجهزة |
| SEC-104 | التحقق أثناء Refresh يعطل فحص Issuer وAudience | تحقق JWT أضعف من مسار الدخول |
| SEC-105 | CORS يسمح بأي Origin/Header/Method و`AllowedHosts` عام | توسعة سطح الهجوم عند إضافة عملاء Web |
| SEC-106 | Audit يختار أول مستخدم/Tenant عند غياب السياق ويسجل قيم الخصائص | سجل غير موثوق واحتمال تخزين بيانات حساسة |
| DATA-101 | الحجز يستخدم Check ثم Insert بلا قيد فريد أو RowVersion | حجز مزدوج تحت التزامن |
| DATA-102 | رقم الفاتورة مبني على قراءة آخر رقم ثم `+1` | سباق يؤدي إلى تعارض وفشل إصدار |
| DATA-103 | الدفعة لا تمنع مبلغًا صفريًا/سالبًا ولا تتحقق من تطابق العملة | رصيد وحالة فاتورة غير صحيحة |
| DATA-104 | لا توجد Idempotency للحجز أو الدفع | تكرار العمليات عند إعادة المحاولة |
| API-101 | 21 موضعًا محتملًا لتمرير تفاصيل الاستثناء، وعدة خدمات تعيد `ex.Message` | كشف تفاصيل داخلية |
| API-102 | لا يوجد Exception Handler/ProblemDetails/Correlation ID مركزي | أخطاء غير متسقة وصعبة التشخيص |
| API-103 | Pagination بلا حدود موحدة | استهلاك موارد أو قيم سالبة غير صالحة |

### P1 — جودة وتسليم

- لا توجد اختبارات Unit أو Integration أو Security أو E2E.
- لا يوجد CI أو Secret Scan أو فحص حزم آلي.
- توجد ثغرة NuGet عالية الخطورة مثبتة في `Microsoft.OpenApi 2.0.0`.
- إصدار LiveCharts المطلوب غير موجود ويتم حل إصدار مختلف تلقائيًا.
- OpenTK وGLWpfControl وSkiaSharp تُستعاد لأطر .NET Framework قديمة بدل `net10.0-windows`.
- Dashboard يحتوي قيمة مؤقتة، بينما أغلب `NotImplementedException` الأخرى هي `ConvertBack` لمحولات WPF أحادية الاتجاه ويجب تقييمها قبل اعتبارها أخطاء.

## 5. حدود العمل

حتى اجتياز G1:

- لا ميزات تجارية جديدة.
- لا بيانات عملاء حقيقية.
- لا نشر عام أو Pilot خارجي.
- لا نسخ حرفي لملحق الكود في README.
- كل إصلاح أمني أو مالي يجب أن يصاحبه اختبار رجوع.

## 6. مراحل التنفيذ

### المرحلة 0 — الاحتواء الأمني (0–48 ساعة)

#### إجراءات خارج المستودع — مالكها صاحب النظام/DevOps

- [ ] تدوير مستخدم وكلمة مرور قاعدة البيانات فورًا وتعطيل القيم القديمة.
- [ ] تدوير مفتاح JWT وإبطال الجلسات الحالية.
- [ ] تحديد هل المستودع كان عامًا؛ إن كان كذلك، جعله خاصًا مؤقتًا.
- [ ] تشغيل Secret Scanner على كامل تاريخ Git، لا على HEAD فقط.
- [ ] فحص سجلات الاستضافة وقاعدة البيانات بحثًا عن دخول أو أدوار غير متوقعة.
- [ ] تفعيل HTTPS صالح على عنوان API قبل إعادة السماح باستخدام Desktop.

#### تغييرات داخل المستودع

- [x] `EMS-SEC-001`: إزالة الأسرار والقيم الإنتاجية من `appsettings*.json` واستخدام متغيرات البيئة/User Secrets/Secret Store.
- [x] `EMS-SEC-002`: حذف طباعة Connection String من `Program.cs` وإضافة Redaction للسجلات.
- [x] `EMS-SEC-003`: تعطيل `POST /api/auth/register` العام وإزالة `TenantID` و`InitialRole` من عقد التسجيل العام.
- [x] `EMS-SEC-004`: إيقاف إنشاء المدير الافتراضي وSeeders ذوي كلمات المرور الثابتة.
- [x] `EMS-SEC-005`: حذف JWT fallback وجعل التطبيق يفشل عند غياب مفتاح صالح.
- [x] `EMS-SEC-006`: فرض HTTPS في Desktop للإنتاج ورفض HTTP غير المحلي.
- [x] `EMS-SEC-007`: تقييد CORS و`AllowedHosts` حسب البيئة.
- [x] `EMS-DEP-001`: إزالة ثغرة `Microsoft.OpenApi 2.0.0` وتثبيت إصدار آمن متوافق.
- [x] `EMS-TEST-001`: إنشاء أول مشروع اختبارات للتسجيل المجهول والقيم الأمنية وعزل Tenant.

**بوابة G0:** القيم القديمة لا تعمل، التسجيل الخطر مغلق، لا اتصال HTTP إنتاجي، لا حساب افتراضي، ولا أسرار مطبوعة أو متتبعة في النسخة الحالية.

### المرحلة 1 — المصادقة والتفويض وعزل المستأجرين (الأسبوع 1–2)

- [x] إنشاء `ITenantContext` في طبقة DataAccess من Claims موثوقة.
- [x] إضافة واجهة Tenant-owned واضحة للكيانات ذات `TenantID` المباشر.
- [x] تطبيق Tenant Query Filter مركزي مع الحفاظ على Soft Delete filter؛ تم دمج الشرطين.
- [x] إضافة SaveChanges guard يمنع إنشاء أو تعديل كيان معلّم يخص Tenant آخر.
- [x] تمرير Tenant المنفذ إلى Get/Update/Delete/Status/AssignRole/RemoveRole والتحقق داخل الاستعلام.
- [x] منع TenantAdmin من منح `SuperAdmin` أو إنشاء أدوار خارج قائمته.
- [x] منع حذف آخر TenantAdmin وتعطيل/حذف المستخدم لنفسه دون مسار مؤكد.
- [x] توحيد رسالة Login وتفعيل Lockout وRate Limiting.
- [x] استبدال Refresh Token الواحد بجدول جلسات متعددة الأجهزة مع Hash وتدوير واكتشاف إعادة الاستخدام وإلغاء جميع الجلسات.
- [ ] جعل Reset Password يرسل رابطًا آمنًا ولا يعيد Token لأي مستهلك.
- [ ] إضافة Permissions وسياسات أدق من `AdminOnly` للمحاسبة والبوابة وإدارة الحدث.

**اختبارات القبول:** مصفوفة Tenant A ضد Tenant B لكل قراءة/تعديل/حذف/دور؛ وكل محاولة عبور ترجع 404 أو 403 دون كشف وجود السجل.

**بوابة G1:** صفر ثغرات Critical/High مفتوحة واختبارات Tenant Isolation ناجحة 100%.

### المرحلة 2 — سلامة الحجوزات والماليات (الأسبوع 2–4)

- [x] تصميم قيد قاعدة بيانات يمنع حجز Booth أو Merge نشط مرتين لنفس المعرض.
- [x] إضافة RowVersion/Optimistic Concurrency إلى الحجز والفاتورة.
- [ ] معالجة تعارض الحجز كـ`409 Conflict` واختباره بـ20–100 طلب متزامن.
- [x] إنشاء عداد ذري لأرقام الفواتير لكل Tenant وسنة باستخدام عملية SQL واحدة مقفلة.
- [x] منع تعديل البيانات المالية وبنود الفاتورة بعد الإصدار، وإضافة Credit Note/Refund غير قابلين للتعديل.
- [ ] إضافة `[Range]` وتحقيقات مجال للدفعات والأسعار والضرائب والكميات. (أُنجزت الدفعات، وبقية المجالات معلقة)
- [x] رفض المبلغ `<= 0` والتحقق من تطابق عملة الفاتورة.
- [x] إضافة Idempotency Key لإنشاء الحجز وتسجيل الدفع، مع إعادة النتيجة ومنع اختلاف جسم الطلب أو التنفيذ المتزامن.
- [ ] منع الدفع المتزامن الزائد بقيد/تزامن داخل قاعدة البيانات، لا بحساب قبل الحفظ فقط.
- [ ] تعريف سياسة أسعار الصرف والتقريب والضرائب لكل Tenant.
- [ ] اختبار Migrations على قاعدة جديدة وترقية نسخة سابقة مع Backup وRollback/roll-forward.

**بوابة البيانات:** لا حجز مزدوج، لا رقم فاتورة مكرر، لا دفع سالب أو زائد، وكل سيناريو متزامن له اختبار تكامل.

### المرحلة 3 — جودة API والاختبارات وCI (الأسبوع 2–5)

- [x] إضافة Exception Handler وProblem Details موحدين.
- [x] منع إرجاع `ex.Message` وStack Trace من خدمات API؛ تسجيل التفاصيل داخليًا فقط.
- [x] إضافة Correlation ID إلى الطلب والاستجابة والسجل.
- [x] توحيد Validation للدفعات وPagination بحد أقصى ثابت.
- [ ] اعتماد API versioning وخطة توافق مع Desktop.
- [ ] نشر OpenAPI Artifact وإضافة Contract Tests بين API وWPF.
- [ ] إنشاء مشاريع الاختبار التالية تدريجيًا:
  - `ExhibitionManagementSystem.UnitTests`
  - `ExhibitionManagementSystem.IntegrationTests`
  - `ExhibitionManagementSystem.Api.IntegrationTests`
  - `ExhibitionManagementSystem.Desktop.Tests`
- [ ] إضافة CI لـRestore/Build/Test/NuGet audit/Secret scan/CodeQL/SBOM. (أُنجزت الأربعة الأولى والتحقق من Migration؛ CodeQL/SBOM معلّقان)
- [ ] تثبيت حزم NuGet وإضافة lock files بعد استقرار الإصدارات.
- [x] معالجة تحذيرات Nullability الحالية؛ Rebuild كامل نظيف. تفعيل `TreatWarningsAsErrors` في CI ما زال خطوة لاحقة.

**بوابة G2:** Build وCI نظيفان، لا ثغرات High/Critical، وكل المسارات الحرجة لها اختبارات ناجحة.

### المرحلة 4 — تطبيق Desktop (الأسبوع 4–6)

- [x] ترقية LiveCharts إلى الإصدار المستقر `2.0.5` والتحقق من البناء.
- [x] ترقية OpenTK إلى 4.9.4 وGLWpfControl إلى 4.3.6 وSkiaSharp WPF إلى 4.152.0 والتحقق من البناء. اختبار مصمم الأجنحة يدويًا ما زال مطلوبًا.
- [ ] تحديد سياسة الجلسة: ذاكرة فقط أو DPAPI إذا كان “تذكر الدخول” مطلوبًا.
- [x] تصحيح إعادة إرسال HttpRequest بعد Refresh Token عبر نسخة مستقلة قابلة لإعادة الإرسال.
- [ ] إضافة Global exception handling آمن مع Crash ID وRedaction.
- [ ] توحيد Loading/Empty/Error/Offline ومنع النقر المكرر في الحجز والدفع.
- [ ] ربط قيمة Dashboard المؤقتة ببيانات حقيقية.
- [ ] مراجعة محولات WPF ذات `NotImplementedException`: إبقاؤها فقط إذا كانت OneWay ومعلنة كذلك.
- [ ] اختبار RTL، لوحة المفاتيح، Focus، التباين، 125–200% DPI، والطابعات الفعلية.
- [ ] إعداد Installer موقع وقنوات Stable/Beta وRollback.

### المرحلة 5 — التشغيل والمراقبة والتعافي (الأسبوع 5–7)

- [x] Health checks منفصلة لـLiveness وReadiness مع فحص اتصال قاعدة البيانات.
- [ ] Structured logging مع Redaction وعدم تسجيل Tokens أو Connection Strings.
- [x] إصلاح Audit Log لرفض غياب المستخدم/Tenant بدل اختيار أول سجل.
- [x] استبعاد الحقول الحساسة من OldValues/NewValues.
- [ ] Metrics وTracing للحجز والفواتير والدفع ومسح التذاكر.
- [ ] نسخ احتياطي مشفر واختبار استعادة على جهاز/بيئة مختلفة.
- [ ] Runbooks لتسريب سر، تعطل قاعدة البيانات، فشل Migration، وفشل نشر.
- [ ] نشر Staging بإعدادات وأسرار منفصلة واختبار Rollback.

**بوابة G3:** Restore وRollback مثبتان، المراقبة فعالة، ولا توجد أسرار في السجلات.

### المرحلة 6 — اكتمال المنتج وPilot (الأسبوع 6–12+)

- [ ] اعتماد MVP نهائي: المعرض، القاعات، الأجنحة، العارضون، الحجز، الفواتير، الدفع، الزوار، التذاكر، الشارات، والتقارير.
- [ ] التحقق من وحدات الشارات والرعاة المذكورة كمنجزة بسيناريوهات قبول فعلية.
- [ ] إضافة PDF ثابت للفواتير والإيصالات مع Snapshot وHash.
- [ ] اختيار بوابة دفع ومزود بريد حسب السوق؛ الميزات خلف Feature Flags.
- [ ] إعداد سياسات الخصوصية والاحتفاظ والحذف والترخيص وإشعارات الحزم.
- [ ] Pilot داخلي ببيانات مصطنعة لدورة العمل الكاملة.
- [ ] Pilot مغلق لعميل أو عميلين بعد G0–G3 فقط.
- [ ] اختبار حمل لمسح التذاكر والحجز المتزامن والتقارير.
- [ ] مراجعة أمنية مستقلة وإعادة اختبار النتائج.

**بوابتا G4/G5:** لا P0/P1 مفتوحة، لا تسريب Tenant أو حجز مزدوج أو فقد مالي، UAT وRestore ناجحان، ثم قرار Go/No-Go موثق.

## 7. Sprint الاحتواء الأول

**المدة:** 3–5 أيام كود، ولا تشمل زمن تدوير الأسرار أو مراجعة الاستضافة.  
**الفرع المقترح:** `codex/security-containment`

| الترتيب | المهمة | الملفات الأساسية | معيار الإنجاز |
|---:|---|---|---|
| 1 | تدوير DB/JWT وتعطيل القيم القديمة | خارج Git | القيم القديمة تفشل فعليًا |
| 2 | إزالة الأسرار والطباعة وإضافة Startup validation | `appsettings*.json`, `Program.cs` | التطبيق يفشل بأمان دون إعداد ولا يطبع أسرارًا |
| 3 | تعطيل التسجيل العام الخطر | `AuthController.cs`, `RegisterRequestDto.cs`, `AuthService.cs` | Anonymous لا يستطيع إنشاء مستخدم أو دور |
| 4 | إزالة المدير الافتراضي | `DataSeeder.cs`, `Program.cs` | لا حساب أو كلمة مرور ثابتة عند التشغيل |
| 5 | فرض HTTPS وتقييد CORS/Hosts | API وDesktop appsettings/Startup | Production يرفض HTTP |
| 6 | إصلاح اعتماد Microsoft.OpenApi | ملفات csproj/lock | فحص NuGet دون High/Critical |
| 7 | إضافة اختبارات احتواء | مشروع Tests جديد | اختبارات التسجيل/JWT/config ناجحة |
| 8 | إعداد CI أولي | `.github/workflows/ci.yml` | Build/Test/Audit تعمل على PR |

## 8. ترتيب الملفات الفعلية للبدء

1. `ExhibitionManagementSystem/Program.cs`
2. `ExhibitionManagementSystem/appsettings.json`
3. `ExhibitionManagementSystem/appsettings.Production.json`
4. `ExhibitionManagementSystem/Controllers/Auth/AuthController.cs`
5. `ExhibitionManagementSystem.Models.DTOs/Auth/RegisterRequestDto.cs`
6. `ExhibitionManagementSystem.Services/Implementations/AuthService.cs`
7. `ExhibitionManagementSystem.DataAccess/Seeder/DataSeeder.cs`
8. `ExhibitionManagementSystem.DataAccess/ApplicationDbContext.cs`
9. `ExhibitionManagementSystem/Controllers/Auth/UsersController.cs`
10. `ExhibitionManagementSystem/Controllers/Auth/RolesController.cs`
11. `ExhibitionManagementSystem.Services/Implementations/ReservationService.cs`
12. `ExhibitionManagementSystem.DataAccess/Repositories/Implementations/InvoiceRepository.cs`
13. `ExhibitionManagementSystem.Services/Implementations/FinancialService.cs`

## 9. Definition of Done

لا تغلق أي مهمة إلا إذا:

- نُفذت عبر فرع قصير وPull Request ومراجعة مناسبة للمخاطر.
- أضيف اختبار يمنع رجوع الخلل ونجح محليًا وفي CI.
- لم تُضف أسرار أو تحذيرات أو استثناءات صامتة جديدة.
- اختُبر Tenant A ضد Tenant B عند التعامل مع بيانات مستأجر.
- اختُبرت حالات التزامن عند الحجز أو الماليات.
- Migration وخطة الرجوع مختبرتان عند تغيير Schema.
- رسائل المستخدم آمنة ومفهومة والتفاصيل في سجل منقح.
- حُدثت الوثائق وOpenAPI وRunbook عند الحاجة.

## 10. مؤشرات المتابعة الأسبوعية

| المؤشر | الحالي | هدف G1/G2 |
|---|---:|---:|
| P0 المؤكدة | 8 | 0 |
| مشاريع الاختبار | 1 (38 اختبارًا ناجحًا) | 3 على الأقل عند G2 |
| Workflows | 1: CI + Security scan + Migration validation | CI إلزامي + Security scan |
| ثغرات NuGet High | 0 | 0 |
| اختبارات Tenant Isolation | اختبارات مركزية أولية ناجحة؛ المصفوفة الكاملة معلقة | 100% من المصفوفة |
| اختبارات الحجز/الفاتورة المتزامنة | 0 | ناجحة 100% |
| تحذيرات البناء والحزم | 0 في Rebuild كامل | 0 |

## 11. القرارات المطلوبة من مالك المنتج أثناء التنفيذ

- هل التسجيل الذاتي مطلوب أصلًا، أم تكون جميع الحسابات عبر دعوات؟ التوصية: الدعوات فقط في النسخة الأولى.
- هل SuperAdmin منفصل تمامًا عن مستخدمي Tenant؟ التوصية: نعم، مع Impersonation مؤقت ومدقق عند الضرورة.
- ما الدول والعملات والضرائب المستهدفة أولًا؟ يؤثر ذلك في الفواتير والدفع والخصوصية.
- هل Desktop يحتاج “تذكر الدخول”؟ إن لم يكن مطلوبًا تبقى Tokens في الذاكرة فقط.
- ما طابعات الشارات والبوابة المالية ومزود البريد المطلوب دعمها في Pilot؟

## 12. الخطوة التالية

الخطوة التالية هي تطبيق Migration `AddTenantSessionsIdempotencyAndCreditNotes` على نسخة Staging مأخوذة من بيانات فعلية بعد Backup، ثم تشغيل اختبارات التزامن للحجز والدفع واختبار مصمم الأجنحة يدويًا دون اعتماد لقطات شاشة. تبقى إجراءات تدوير الأسرار وفحص تاريخ Git خارج المستودع حواجز إطلاق إلزامية.
