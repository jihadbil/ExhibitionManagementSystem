# نظام التصميم الموحد (Unified Design System)
> **النطاق**: الألوان، الأشكال والأزرار، والخطوط (Colors, Shapes & Buttons, Typography).  
> **المرجع الأساسي**: طبقة سطح المكتب (`ExhibitionManagementSystem.DeskTop/Resources`).

---

## 1. 🎨 نظام الألوان (Color Palette & Tokens)

### 1.1 الألوان الأساسية والهوية (Brand Colors)
تعتمد الهوية البصرية على تدرج الأزرق الملكي العميق الممزوج مع الأزرق السماوي المشرق لنقل طابع احترافي، عصري، وعالي التقنية.

| المسمى البرمجي (Token) | الرمز (Hex) | المعاينة | XAML Brush | CSS Variable | الاستخدام |
| :--- | :--- | :---: | :--- | :--- | :--- |
| **Primary** | `#1E40AF` | 🟦 | `PrimaryBrush` | `--primary` | اللون الرئيسي للهوية، الإجراءات الكبرى، الترويسات |
| **Primary Dark** | `#1E3A8A` | 🟦 | `PrimaryDarkBrush` | `--primary-dark` | حالة التحويم (Hover)، العناوين المتباينة |
| **Secondary** | `#0EA5E9` | 🔷 | `SecondaryBrush` | `--secondary` | اللون التكميلي، أزرار التفاعل الخفيفة، اللمسات البصرية |
| **Primary Gradient** | `#1E40AF` ➔ `#0EA5E9` | 🟦➔🔷 | `PrimaryGradientBrush` | `--primary-gradient` | التدرج الأساسي للأزرار البارزة والترويسات |

```css
/* CSS Tokens */
--primary: #1e40af;
--primary-dark: #1e3a8a;
--primary-hover: #1e3a8a;
--secondary: #0ea5e9;
--secondary-hover: #0284c7;
--primary-gradient: linear-gradient(to right, #1e40af, #0ea5e9);
--primary-glow: 0 10px 20px -5px rgba(99, 102, 241, 0.35);
```

---

### 1.2 خلفيات الأسطح والبطاقات (Surfaces & Backgrounds)
مصممة وفق نمط الإضاءة النقي (Pure Light Mode) مع رماديات هادئة تضمن راحة العين لفترات العمل الطويلة.

| المسمى البرمجي (Token) | الرمز (Hex) | المعاينة | XAML Brush | CSS Variable | الاستخدام |
| :--- | :--- | :---: | :--- | :--- | :--- |
| **Main Background** | `#F8F9FD` | ⬜ | `BgMainBrush` | `--bg-main` | الخلفية العامة لتطبيق الويب وسطح المكتب |
| **Card Background** | `#FFFFFF` | ⬜ | `BgCardBrush` | `--bg-card` | خلفية البطاقات، الجداول، والحاويات |
| **Secondary Background** | `#F1F5F9` | ◽ | `BgSecondaryBrush` | `--bg-secondary` | عناصر القوائم، حالات التحويم، القواطع |
| **Input Background** | `#F8FAFC` | ◽ | `BgInputBrush` | `--bg-input` | خلفية حقول الإدخال والمدخلات الافتراضية |
| **Sidebar Hover** | `#EBF5FF` | ◽ | `SidebarHoverBrush` | `--sidebar-hover` | تمييز الروابط في القوائم الجانبية |

---

### 1.3 ألوان النصوص والتباين (Typography Colors)
| المسمى البرمجي (Token) | الرمز (Hex) | XAML Brush | CSS Variable | الاستخدام |
| :--- | :--- | :--- | :--- | :--- |
| **Text Primary** | `#1A1D29` | `TextPrimaryBrush` | `--text-primary` | العناوين الرئيسية والنصوص ذات التباين العالي |
| **Text Secondary** | `#64748B` | `TextSecondaryBrush` | `--text-secondary` | النصوص الوصفية، تسميات الحقول (Labels) |
| **Text Muted** | `#94A3B8` | `TextMutedBrush` | `--text-muted` | نصوص التلميح (Placeholders) والمعلومات الهامشية |
| **Text White** | `#FFFFFF` | `TextWhiteBrush` | `--text-white` | النصوص فوق الأزرار الأساسية والتدرجات |

---

### 1.4 ألوان الحالات الوظيفية (Semantic Colors)
| الحالة | اللون الأساسي | لون النص التابع | لون الخلفية الفاتحة (Badge) | الاستخدام |
| :--- | :--- | :--- | :--- | :--- |
| **النجاح (Success)** | `#10B981` | `#059669` | `#D1FAE5` | الإتمام، التأكيدات، الحالات النشطة |
| **التحذير (Warning)** | `#F59E0B` | `#D97706` | `#FEF3C7` | التنبيهات، المعاملات المعلقة |
| **الخطر (Danger)** | `#EF4444` | `#DC2626` | `#FEE2E2` | الحذف، الأخطاء، الإلغاء، الرفض |
| **المعلومات (Info)** | `#3B82F6` | `#2563EB` | `#DBEAFE` | الإشعارات التوضيحية والروابط |

---

### 1.5 تدرجات تصنيفات المعارض (Exhibition Type Categories)
| التصنيف | التدرج اللوني | الألوان (Hex) |
| :--- | :--- | :--- |
| **تقني (Tech)** | `linear-gradient(to right, #3B82F6, #06B6D4)` | أزرق ➔ سماوي |
| **طبي (Medical)** | `linear-gradient(to right, #10B981, #14B8A6)` | أخضر ➔ تيل |
| **صناعي (Industrial)** | `linear-gradient(to right, #F97316, #DC2626)` | برتقالي ➔ أحمر |
| **تجاري (Commercial)** | `linear-gradient(to right, #EC4899, #F43F5E)` | وردي ➔ أحمر توتي |
| **تعليمي (Educational)** | `linear-gradient(to right, #0284C7, #1E40AF)` | أزرق سماوي ➔ أزرق ملكي |
| **سيارات (Automotive)** | `linear-gradient(to right, #6B7280, #475569)` | رمادي فضي ➔ رمادي حديدي |

---

## 2. 🔲 نظام الأشكال والأزرار (Shapes, Elevation & Buttons)

### 2.1 مقياس استدارة الحواف (Corner Radius Scale)
| المسمى | القيمة | الاستخدام النموذجي |
| :--- | :---: | :--- |
| `RadiusSm` | **`6px`** | الأوسمة الصغيرة (Badges)، بطاقات الحالات المصغرة |
| `RadiusMd` | **`8px`** | أزرار الأيقونات (`40x40`)، أزرار الجداول المصغرة (`28px`) |
| `RadiusLg` | **`12px`** | **المعيار الذهبي**: الأزرار الرئيسية، حقول الإدخال، صناديق الحوار |
| `RadiusXl` | **`16px`** | بطاقات البيانات الكبرى (`StandardCardStyle`)، الحاويات |
| `RadiusFull` | **`999px`** | حبوب التصفية (Pills)، مؤشرات الحالة الدائرية |

---

### 2.2 الظلال والتأثيرات الحركية (Elevation & Shadows)
- **ظل البطاقات الافتراضي**:  
  `box-shadow: 0 1px 3px 0 rgba(0, 0, 0, 0.08);`  
  (في WPF: `DropShadowEffect Color="#000000" BlurRadius="6" ShadowDepth="1" Opacity="0.08"`).
- **ظل البطاقات عند التحويم (Card Hover)**:  
  `box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.1);` بتأثير انتقال ناعم مدته `0.2s`.
- **التوهج اللوني للأزرار (Glow Shadows)**:
  - زر الأساسي: `box-shadow: 0 10px 20px -5px rgba(99, 102, 241, 0.35);`
  - زر الخطر: `box-shadow: 0 10px 20px -5px rgba(239, 68, 68, 0.3);`
- **التفاعل الحركي المصغر (Micro-Interaction)**:
  - عند الضغط أو التمرير على الأزرار الأساسية: تقليص طفيف بالحجم بنسبة `transform: scale(0.98);` لمدة `0.1s`.

---

### 2.3 تشريح الأزرار الموحدة (Button Variants)

#### 1. الزر الأساسي (Primary Button)
- **الخلفية**: التدرج اللوني `--primary-gradient` (`#1E40AF` ➔ `#0EA5E9`).
- **النص**: أبيض `#FFFFFF`، وزن `Medium (500)`، حجم `14px`.
- **الحشو الداخلي**: `12px 24px` (أفقي 24px، عمودي 12px).
- **الاستدارة**: `12px` (`--radius-lg`).
- **الحدود**: `0` (بدون حدود).
- **التفاعل**: `scale(0.98)` مع توهج ناعم.

#### 2. الزر الثانوي (Secondary / Outline Button)
- **الخلفية**: أبيض نقي `#FFFFFF`.
- **الإطار**: حد رفيع `1px solid #E2E8F0`.
- **النص**: رمادي رصين `#64748B`.
- **الاستدارة**: `12px`.
- **الحشو الداخلي**: `12px 24px`.
- **التفاعل**: تحول الخلفية إلى `#F1F5F9` والنص إلى `#1A1D29`.

#### 3. زر الخطر (Danger Button)
- **الخلفية**: لون صلب `#EF4444`.
- **النص**: أبيض `#FFFFFF`.
- **الاستدارة**: `12px`.
- **الحشو الداخلي**: `12px 24px`.
- **التفاعل**: `scale(0.98)` مع ظل توهج أحمر `#EF4444`.

#### 4. زر الأيقونة (Icon Button)
- **الأبعاد**: مربع مثالي `40x40px`.
- **الخلفية**: شفافة `transparent`.
- **الاستدارة**: `8px` (`--radius-md`).
- **التفاعل**: خلفية رمادية ناعمة `#F1F5F9` مع تلوين الأيقونة بـ `--primary`.

#### 5. أزرار الجداول المصغرة (Small Action Buttons)
- **الارتفاع**: `28px` مدمج لعدم استهلاك مساحة السجلات.
- **الحشو**: `4px 10px`.
- **حجم الخط**: `12px`.
- **الاستدارة**: `8px`.

---

### 2.4 حقول الإدخال والمدخلات (Input Controls)
- **الارتفاع القياسي**: `48px`.
- **الاستدارة**: `12px`.
- **الخلفية الافتراضية**: `#F8FAFC`.
- **الحدود**: حد شفاف `2px` افتراضياً لتجنب القفزات البصرية (Layout Shift).
- **حالة التركيز (Focus State)**:
  - الإطار: `2px solid #2563EB` (أزرق Focus).
  - الخلفية: تتحول إلى الأبيض النقي `#FFFFFF`.

---

## 3. ✍️ نظام الخطوط والطباعة (Typography System)

### 3.1 عائلات الخطوط (Font Families)
- **الخط الأساسي اللاتيني (Latin)**: **`Work Sans`** (المثبت أصلياً في طبقة سطح المكتب `WorkSans-*.ttf`).
- **الخط الأساسي العربي (Arabic)**: **`Cairo`** المتناغم تيبوغرافياً وهندسياً مع أوزان Work Sans.
- **الخط الاحتياطي (Fallback)**: `Segoe UI, system-ui, -apple-system, sans-serif`.

### 3.2 الأوزان المعتمدة (Font Weights)
- **Regular (`400`)**: لنصوص الفقرات، محتويات الجداول، والتلميحات.
- **Medium (`500`)**: لتسميات الحقول (Labels)، نصوص الأزرار، والتبويبات.
- **SemiBold (`600`)**: لعناوين الأقسام، البطاقات، ورؤوس الجداول.
- **Bold (`700`)**: للعناوين الكبرى والأرقام الإحصائية البارزة.

### 3.3 السلم التيبوغرافي القياسي (Type Scale)

| النمط | الحجم (Font Size) | الوزن (Weight) | ارتفاع السطر (Line Height) | اللون الافتراضي | الاستخدام |
| :--- | :---: | :---: | :---: | :--- | :--- |
| **H1** | **`32px`** | Bold (700) | `38.4px` (1.2) | `#1A1D29` | العناوين الرئيسية الكبرى للشاشات والترحيب |
| **H2** | **`24px`** | Bold (700) | `30px` (1.25) | `#1A1D29` | عناوين الوحدات والصفحات |
| **H3** | **`20px`** | SemiBold (600) | `26px` (1.3) | `#1A1D29` | عناوين البطاقات والأقسام |
| **H4** | **`18px`** | SemiBold (600) | `24px` (1.33) | `#1A1D29` | العناوين الفرعية والمجموعات الداخلية |
| **Body Large** | **`16px`** | Medium (500) | `24px` (1.5) | `#1A1D29` | النصوص التمهيدية والمقدمات |
| **Body (الافتراضي)** | **`14px`** | Regular (400) | `21px` (1.5) | `#1A1D29` | النصوص العادية، محتويات الجداول |
| **Label** | **`14px`** | Medium (500) | `20px` (1.4) | `#64748B` | تسميات الحقول وأزرار الإجراءات |
| **Small** | **`12px`** | Regular (400) | `16px` (1.33) | `#94A3B8` | التلميحات المساعدة، التواريخ، والأوسمة |

---

## 4. 📋 جدول المطابقة السريعة بين المنصات (Cross-Platform Mapping)

| العنصر التصميمي | قيمة WPF Desktop (XAML) | قيمة Web (CSS Variable) |
| :--- | :--- | :--- |
| **Primary Color** | `{StaticResource PrimaryBrush}` (`#FF1E40AF`) | `var(--primary)` (`#1e40af`) |
| **Primary Dark** | `{StaticResource PrimaryDarkBrush}` (`#FF1E3A8A`) | `var(--primary-dark)` (`#1e3a8a`) |
| **Secondary Color** | `{StaticResource SecondaryBrush}` (`#FF0EA5E9`) | `var(--secondary)` (`#0ea5e9`) |
| **Primary Gradient** | `{StaticResource PrimaryGradientBrush}` | `var(--primary-gradient)` |
| **Main Background** | `{StaticResource BgMainBrush}` (`#FFF8F9FD`) | `var(--bg-main)` (`#f8f9fd`) |
| **Card Background** | `{StaticResource BgCardBrush}` (`#FFFFFFFF`) | `var(--bg-card)` (`#ffffff`) |
| **Input Background** | `{StaticResource BgInputBrush}` (`#FFF8FAFC`) | `var(--bg-input)` (`#f8fafc`) |
| **Focus Border** | `{StaticResource BorderFocusBrush}` (`#FF2563EB`) | `var(--border-focus)` (`#2563eb`) |
| **Default Radius** | `{StaticResource RadiusLg}` (`12`) | `var(--radius-lg)` (`12px`) |
| **Card Radius** | `{StaticResource RadiusXl}` (`16`) | `var(--radius-xl)` (`16px`) |
| **Button Padding** | `Padding="24,12"` | `padding: 12px 24px;` |
| **Primary Font** | `pack://application:,,,/Resources/Fonts/#Work Sans` | `'Work Sans', 'Cairo', sans-serif` |
