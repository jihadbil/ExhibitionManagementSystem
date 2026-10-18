# مراجعة النظام — 17 أكتوبر 2026

> نُفذت الإصلاحات البرمجية لاحقًا بطلب المستخدم. الحالة الحالية ونتائج التحقق في [تقرير الإصلاحات](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/docs/system-repairs-2026-10-17.md). التفاصيل التالية تسجل حالة النظام قبل الإصلاح.

راجعت النسخة الحالية في مساحة العمل، بما فيها التعديلات الموجودة قبل بدء المراجعة. نتيجة المراجعة قبل الإصلاح: **9 ملاحظات قائمة: واحدة حرجة P0 معروفة سابقًا، وملاحظتان عاليتان P1، وست ملاحظات P2**. ست من الملاحظات الجديدة أُثبتت بالتنفيذ على SQL، وواحدة بفحص التبعيات، وواحدة بمراجعة كود سطح المكتب. لم أعد عرض المشكلات القديمة التي نجحت اختبارات إصلاحها.

اقتصرت الإضافات في المستودع على هذا التقرير ومصدر التجارب التشخيصية. لم أصلح كود التشغيل، ولم أشغّل مهيئ الحسابات، ولم أعدّل بيانات المؤسسات الفعلية أو أرسل بريدًا. استُخدمت قواعد SQL مؤقتة بأسماء فريدة، وحُذفت بعد كل تجربة. جرى إنشاء بيانات التجهيز بصلاحية اختبار، ثم تنفيذ عمليات الخدمة بسياق مؤسسة لا يسمح بتجاوز العزل.

## النتائج مرتبة حسب الأولوية

### A01 — P0: إعادة كلمة مرور الحساب الإداري الثابتة وصلاحياته عند التشغيل

الملاحظة السابقة F01 ما زالت موجودة. يستدعي `Program` مهيئ البيانات في جميع البيئات. ويحتوي المهيئ بيانات دخول ثابتة؛ إن تغيرت كلمة مرور الحساب المعروف يعيد ضبطها إلى القيمة المضمنة، ويعيد أدواره المفقودة وتفعيله. تغيير كلمة المرور أو تعطيل الحساب لا يكفي ما دام هذا المسار يعمل عند إعادة تشغيل الخادم.

**الدليل:** مراجعة المسار الحالي؛ لم أجرّبه على حساب فعلي. المواقع: [استدعاء المهيئ](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem/Program.cs:167)، [إعادة تعيين كلمة المرور](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.DataAccess/Seeder/DataSeeder.cs:116)، [إعادة الأدوار والتفعيل](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.DataAccess/Seeder/DataSeeder.cs:127).

**المعالجة:** فصل تهيئة حساب الاختبار عن تشغيل الإنتاج، وإزالة كلمة المرور من المصدر، وجعل إنشاء أول مدير إجراءً صريحًا لمرة واحدة. لا تعِد تعيين بيانات دخول حساب قائم أو صلاحياته عند التشغيل. هذه أولوية قبل النشر حتى لو بقي الحساب مطلوبًا أثناء الاختبار.

### A02 — P1: حذف معرض يخفي عقد رعايته القائم

أنشأت معرضًا له عقد رعاية بقيمة 750 LYD دون حجوزات أجنحة أو تذاكر. كانت قراءة العقد ناجحة. قبل `DeleteAsync` حذف المعرض، ثم أصبحت قراءة العقد تعيد `CONTRACT_NOT_FOUND` مع بقاء سجل العقد في SQL. فحص التبعيات يغطي الحجوزات والتذاكر والفعاليات وبعض البيانات المالية، ويغفل عقود الرعاية؛ تضمين المعرض المطلوب في مستودع العقود يُسقط العقد بعد الحذف الناعم.

**الدليل:** تجربة `DeleteExhibitionWithSponsorship`: `BeforeVisible=true`, `DeleteAccepted=true`, `AfterVisible=false`, `ContractStillStored=true`.

**المواقع:** [فحص تبعيات حذف المعرض](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/ExhibitionService.cs:228)، [تضمين المعرض عند قراءة العقد](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.DataAccess/Repositories/Implementations/SponsorshipContractRepository.cs:43).

**المعالجة:** منع حذف معرض له عقد رعاية أو تاريخ مرتبط به، بما فيه السجلات المحذوفة ناعمًا، مع حماية الفحص والكتابة من التزامن. اختبر قراءة العقد وتاريخه بعد جميع عمليات حذف الكيانات الأصلية.

### A03 — P1: تقريب كمية بند الفاتورة يترك المبلغ غير متوافق مع القيم المخزنة

طلب إنشاء بند بكمية `0.015` وسعر وحدة `100` اجتاز التحقق ونجح. أعادت الخدمة الكمية `0.015` والمبلغ `1.50`، لكن SQL حفظ الكمية `0.02`؛ لذلك حاصل الكمية والسعر المخزنين هو `2.00`، بينما مبلغ البند بقي `1.50`. تُحسب القيمة قبل تقريب عمود `Quantity` ذي الدقة `decimal(18,2)`، ثم تعاد القيم المتتبعة قبل التقريب. يظهر الحساب نفسه في التعديل أيضًا؛ تجربة هذه الجولة نفذت الإنشاء.

**الدليل:** تجربة `InvoiceItemQuantityPrecision`: `validates=true`, `IsSuccess=true`, `SavedQuantity=0.02`, `UnitPrice=100`, `TotalPrice=1.50`, `ExpectedFromStoredValues=2.00`.

**المواقع:** [حساب المبلغ في الإنشاء](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/InvoiceItemService.cs:82)، [الحساب في التعديل](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/InvoiceItemService.cs:124)، [دقة الكمية المخزنة](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Models/InvoiceItem.cs:38).

**المعالجة:** رفض الكميات ذات الدقة غير المدعومة، أو تطبيعها إلى الدقة المخزنة قبل الحساب. طبّق القاعدة في الإنشاء والتعديل، وتحقق من أن الاستجابة وإجمالي الفاتورة يطابقان القيم التي يعاد تحميلها من SQL.

### A04 — P2: تبعية تشفير بإصدار يشمله تحذيران أمنيان حديثان

أظهر `dotnet list ... package --vulnerable --include-transitive --no-restore` وجود `System.Security.Cryptography.Pkcs 10.0.0`، مع تحذير High وآخر Moderate. أكد `dotnet nuget why` وصولها عبر `Microsoft.Data.SqlClient` وعبر مكونات `Microsoft.Identity.Web`. كما أن بيئة التشغيل المحلية المثبتة تستخدم .NET 10.0.12.

نُشر التحذيران في 13 أكتوبر 2026؛ كلاهما يشمل إصدارات الحزمة من 10.0.0 إلى 10.0.12، والإصدار المصحح 10.0.13. الأول يتعلق باستهلاك المعالج أثناء التحقق من توقيع CMS، والثاني بفك ترميز CMS على Windows. التفاصيل: [تحذير Microsoft عالي الشدة CVE-2026-84008](https://github.com/advisories/GHSA-846f-f764-76mx)، [تحذير Microsoft المتوسط CVE-2026-70162](https://github.com/advisories/GHSA-2fmv-rv2c-59x7).

**حد الدليل:** وجود الإصدار المتأثر مؤكد؛ لم أثبت وصول مدخلات مستخدم النظام إلى الدوال المتأثرة، ولم أنفذ هجوم حجب خدمة. تصنيف P2 هنا لأولوية المعالجة في التطبيق، ولا يغير تصنيف High للحزمة في المصدر.

**المواقع:** [مرجع SQL Server الذي يجلب SqlClient](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Models/ExhibitionManagementSystem.Models.csproj:16)، [مراجع هوية Microsoft](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem/ExhibitionManagementSystem.csproj:21).

**المعالجة:** رفع الحزمة المحلولة وبيئة التشغيل إلى الإصدار المصحح أو إصدار مدعوم أحدث، ثم إعادة فحص التبعيات والبناء والاختبارات. فحص 12 أكتوبر السابق لا يشمل تحذيرات نُشرت بعده.

### A05 — P2: عدد الزوار في التقرير ينخفض عندما يدخل الزائر

أصدرت تذكرة نشطة في بيانات الاختبار وولّدت تقرير المعرض: `TotalVisitors=1`. نجح مسح الدخول، وأصبح هناك سجل دخول واحد. التقرير التالي أعاد `TotalVisitors=0`. يعتمد التقرير على عدد التذاكر التي ما زالت `Active`، بينما المسح الصحيح يحوّل التذكرة إلى `Used`؛ لذا يقيس الحقل التذاكر غير المستخدمة بدل زوار المعرض.

**الدليل:** تجربة `ReportVisitorsAfterEntry`: التقرير قبل الدخول وبعده نجح، والمسح نجح، والعدد تغير من 1 إلى 0 مع `StoredEntryScans=1`.

**المواقع:** [مصدر عدد الزوار في التقرير](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/ReportService.cs:57)، [حصر العد في التذاكر النشطة](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.DataAccess/Repositories/Implementations/TicketRepository.cs:43).

**المعالجة:** حساب الحضور الفعلي من عمليات الدخول مع تمييز الزوار عند الحاجة، وفصل هذا المؤشر عن عدد التذاكر المصدرة أو غير المستخدمة. يجب ألا يؤدي مسح دخول ناجح إلى إنقاص عدد زوار التقرير.

### A06 — P2: قالب شارة يقبل ارتباطًا بمعرض مؤسسة أخرى

بسياق المؤسسة الأولى دون تجاوز للعزل، نجح إنشاء قالب شارة يرسل `ExhibitionID` لمعرض المؤسسة الثانية. حُفظ القالب تحت `TenantID=1` مع `ExhibitionID=2` الأجنبي. لا يتحقق مسار الإنشاء من ملكية المعرض؛ ومسار التعديل ينقل القيمة بالـMapping دون التحقق نفسه.

**الدليل:** تجربة `BadgeTemplateForeignExhibition` على SQL: `CreateAccepted=true`, `SavedTenantId=1`, `SavedExhibitionId=ForeignExhibitionId=2`. هذا يثبت علاقة أجنبية غير صحيحة؛ لا يثبت تسريب بيانات المعرض الأجنبي.

**المواقع:** [إنشاء القالب](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/BadgeService.cs:82)، [تعديل ارتباط القالب](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/BadgeService.cs:121).

**المعالجة:** عند وجود `ExhibitionID`، تحقق من وجود معرض فعال تابع للمؤسسة الحالية قبل تغيير القالب أو القوالب الافتراضية. طبّق القاعدة في الإنشاء والتعديل مع اختبار رفض معرف مؤسسة أخرى.

### A07 — P2: نقل معرض يترك مساحاته الإعلانية في الموقع القديم

أنشأت معرضًا بلا حجوزات أو فعاليات أو دمج، مع مساحة إعلانية مرتبطة بقاعة موقعه. نجح نقل المعرض إلى موقع آخر من المؤسسة نفسها. بعد إعادة القراءة أصبح `Exhibition.VenueID=2`، بينما بقي `AdvertisingSpace.VenueID=1` وموقع قاعتها 1. يتحقق تعديل المساحة نفسها من تطابق الموقع مع المعرض، لكن تعديل المعرض يتجاوز هذا الاتساق.

**الدليل:** تجربة `MoveExhibitionWithAdvertisingSpace`: `UpdateAccepted=true`, `ExhibitionVenue=2`, `AdvertisingVenue=1`, `AdvertisingHallVenue=1`.

**الموقع:** [فحص تغيير موقع المعرض](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/ExhibitionService.cs:158).

**المعالجة:** منع نقل المعرض مع وجود مساحات أو تخصيصات إعلانية مرتبطة بالموقع، أو تنفيذ نقل صريح متكامل يعيد التحقق من جميع العلاقات قبل الحفظ.

### A08 — P2: حذف قاعة تملك فعالية مجدولة يترك الفعالية دون قاعة ظاهرة

أنشأت قاعة بلا أجنحة، مرتبطة بفعالية مجدولة. نجح حذف القاعة، وبقيت الفعالية ومعرف قاعتها في SQL، لكن تضمين القاعة أعاد `null` بسبب الحذف الناعم. يفحص الحذف الأجنحة النشطة فقط، ويغفل الفعاليات والعلاقات الأخرى.

**الدليل:** تجربة `DeleteHallWithScheduledEvent`: `DeleteAccepted=true`, `ScheduleStored=true`, `ScheduleHallId=2`, `HallVisible=false`.

**الموقع:** [فحص تبعيات حذف القاعة](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.Services/Implementations/HallService.cs:126).

**المعالجة:** رفض الحذف عند وجود فعالية مجدولة أو مساحة إعلانية مرتبطة، أو إعادة إسنادها صراحة داخل معاملة قبل الحذف. احتفظ بسياق الفعاليات القائمة عند إعادة قراءتها.

### A09 — P2: محددات المعارض في سطح المكتب تقف عند أول 100 معرض

يطلب نموذج الحجز ومحدد المعرض في شاشة التذاكر `GetByTenantAsync(..., 1, 100)` مرة واحدة، ثم يبني خيارات القائمة من تلك الصفحة وحدها. عند تجاوز 100 معرض تختفي البقية من هذين المسارين. يظهر النمط أيضًا في محددات أخرى، وإن كانت اختبارات اكتمال صفحات الويب ناجحة.

**الدليل:** مراجعة الكود؛ نجح بناء WPF، ولم أشغّل هذه القوائم تفاعليًا أو أجر تجربة WPF فيها 101 معرض.

**المواقع:** [معارض نموذج الحجز](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.DeskTop/ViewModels/Reservations/ReservationFormViewModel.cs:128)، [معارض شاشة التذاكر](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/ExhibitionManagementSystem.DeskTop/ViewModels/Tickets/TicketsViewModel.cs:208).

**المعالجة:** البحث والترقيم من الخادم، أو تحميل الصفحات إلى اكتمالها عند الحاجة لقائمة محلية. لا تعرض نجاح التحميل إذا فشلت صفحة لاحقة.

## نتائج الفحوصات وحدودها

| الفحص | النتيجة |
|---|---|
| اختبارات الخادم دون SQL في التشغيل الأول | 225 ناجحة، 4 مجموعات SQL متجاوزة |
| اختبارات الخادم بعد تفعيل SQL | **247 ناجحة، صفر فشل، صفر تجاوز** |
| اختبارات الويب | **65 ناجحة، صفر فشل** |
| ESLint | نجاح |
| TypeScript وبناء Vite | نجاح |
| بناء تطبيق سطح المكتب | نجاح، صفر أخطاء وصفر تحذيرات في البناء دون استعادة الحزم |
| `npm audit --json` | صفر ثغرات معلنة وقت المراجعة |
| فحص حزم الخادم المباشرة وغير المباشرة | تبعية واحدة لها تحذيران، High وModerate؛ A04 |
| التجارب التشخيصية الإضافية على SQL | 6 سيناريوهات؛ كل منها كشف الخلل المبين أعلاه |

نجاح الاختبارات الحالية لا يغطي الحالات التشخيصية الجديدة. جرى فحص المصادقة وتجديد الجلسة والصلاحيات وعزل المؤسسات والخدمات المالية والحجوزات والتذاكر والرعاية والحذف والشارات ومحددات الواجهات بقراءة الكود والاختبارات المتاحة. لم تُنفذ مراجعة تفاعلية لكل شاشة، أو تجربة طباعة فعلية، أو اختبار تحميل طويل، أو اختبار اختراق خارجي، أو استغلال للثغرات المنشورة.

## إعادة تشغيل الأدلة

المصدر محفوظ في [system-audit-2026-10-17-probes.cs.txt](C:/Users/Masoud/source/repos/ExhibitionManagementSystem/docs/system-audit-2026-10-17-probes.cs.txt). نُفذ من مشروع Console مؤقت خارج المستودع، يستهدف `net10.0`، ويرجع إلى مشروع `ExhibitionManagementSystem.Api.Tests` مع `FrameworkReference` إلى `Microsoft.AspNetCore.App`. يُنسخ المصدر إلى `Program.cs` في مشروع التشخيص. يستخدم المصدر SQL محليًا بتوثيق Windows، ويولّد أسماء `EMS_Review_20261017_<guid>` ويحذف قواعده في `finally`. يجب توجيهه إلى خادم اختبار يسمح بإنشاء قواعد مؤقتة.

يمكن ضبط `EMS_AUDIT_PROBES` على أسماء السيناريوهات مفصولة بفواصل لتشغيل بعضها. عند تركه فارغًا ينفذ الستة. يطبع `BrokenInvariant=true` عندما يثبت الخلل؛ هذه أداة رصد تشخيصية، ولم تُضف إلى اختبارات المشروع المعتادة.

أوامر الفحوصات المستخدمة من جذر المستودع:

```powershell
$env:EMS_SQL_TEST_CONNECTION='Server=localhost;Database=master;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5'
dotnet test ExhibitionManagementSystem.Api.Tests/ExhibitionManagementSystem.Api.Tests.csproj --no-build --no-restore --verbosity minimal
dotnet build ExhibitionManagementSystem.DeskTop/ExhibitionManagementSystem.DeskTop.csproj --no-restore --verbosity minimal
dotnet list ExhibitionManagementSystem/ExhibitionManagementSystem.csproj package --vulnerable --include-transitive --no-restore
dotnet nuget why ExhibitionManagementSystem/ExhibitionManagementSystem.csproj System.Security.Cryptography.Pkcs
```

ومن مجلد `exhibitionmanagementsystem.web`: `npm run test`, `npm run lint`, `npm run build`, `npm audit --json`.
