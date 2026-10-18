import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  ArrowRight,
  Building2,
  Calculator,
  Calendar,
  Compass,
  DollarSign,
  Edit2,
  Layers,
  Plus,
  Search,
  Store,
  Tag,
  Trash2,
  X
} from 'lucide-react';
import { NavLink, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { useAuth } from '../../../app/providers/AuthContext';
import { useToast } from '../../../app/providers/ToastContext';
import { api } from '../../../services/api';
import { getAllItems } from '../../../services/pagination';

interface Exhibition {
  exhibitionID: number;
  name: string;
  type: string;
  startDate: string;
  endDate: string;
  status: string;
  venueID?: number;
  venueName?: string;
  entryCurrency?: string;
}

interface Hall {
  hallID: number;
  venueID: number;
  hallName: string;
}

interface Booth {
  boothID: number;
  hallID: number;
  hallName: string;
  boothNumber: string;
  currentAreaSqM: number;
  originalAreaSqM?: number;
  width?: number;
  height?: number;
  shapeType?: string;
  boothType: string;
  status: string;
  isMerged: boolean;
  posX?: number;
  posY?: number;
  rotationAngle?: number;
  shapePolygonJSON?: string;
  assignedPriceRuleID?: number;
}

interface BoothPage {
  items: Booth[];
  totalCount: number;
}

interface BoothPriceCalculation {
  amount: number;
  currencyCode: string;
  ruleID: number;
}

interface BoothPriceRule {
  ruleID: number;
  exhibitionID?: number;
  exhibitionName?: string;
  boothType?: string;
  exhibitorCategory?: string;
  productCategory?: string;
  pricePerSqM: number;
  currencyCode: string;
  minAreaSqM?: number;
  maxAreaSqM?: number;
  validFrom: string;
  validTo?: string;
  notes?: string;
  ruleName?: string;
}

interface BoothFormData {
  hallID: string;
  boothNumber: string;
  originalAreaSqM: string;
  width: string;
  height: string;
  shapeType: string;
  boothType: string;
  status: string;
}

interface PriceRuleFormData {
  boothType: string;
  exhibitorCategory: string;
  pricePerSqM: string;
  currencyCode: string;
  minAreaSqM: string;
  maxAreaSqM: string;
  ruleName: string;
  notes: string;
}

const emptyBoothForm: BoothFormData = {
  hallID: '',
  boothNumber: '',
  originalAreaSqM: '12',
  width: '4',
  height: '3',
  shapeType: 'Rect',
  boothType: 'Standard',
  status: 'Available',
};

const emptyPriceRuleForm: PriceRuleFormData = {
  boothType: 'Standard',
  exhibitorCategory: 'Local',
  pricePerSqM: '150',
  currencyCode: 'LYD',
  minAreaSqM: '9',
  maxAreaSqM: '100',
  ruleName: 'تسعير قياسي',
  notes: '',
};

const statusNames: Record<string, string> = {
  Available: 'متاح للحجز',
  Reserved: 'محجوز ومؤكد',
  Merged: 'مدمج',
  Maintenance: 'خارج الخدمة للصيانة',
};

const shapeNames: Record<string, string> = {
  Rect: 'مستطيل',
  Rotated: 'مستطيل مدوّر',
  Polygon: 'مضلع مخصص',
};

const boothTypeNames: Record<string, string> = {
  Standard: 'قياسي',
  Corner: 'زاوية',
  Premium: 'مميز',
  VIP: 'كبار الشخصيات',
  Custom: 'مخصص',
};

const exhibitorCategoryNames: Record<string, string> = {
  Local: 'عارض محلي',
  International: 'عارض دولي',
  Government: 'جهة حكومية',
};

const formatDate = (value: string) => {
  if (!value) return '';
  const d = new Date(value);
  return isNaN(d.getTime()) ? value : d.toLocaleDateString('ar-LY');
};

const normalizeDecimalInput = (value: string) => {
  const normalizedDigits = value
    .replace(/[٠-٩]/g, digit => String('٠١٢٣٤٥٦٧٨٩'.indexOf(digit)))
    .replace(/[۰-۹]/g, digit => String('۰۱۲۳۴۵۶۷۸۹'.indexOf(digit)))
    .replace(/[٫,،]/g, '.')
    .replace(/[^\d.]/g, '');

  const [integerPart, ...decimalParts] = normalizedDigits.split('.');
  return decimalParts.length > 0
    ? `${integerPart}.${decimalParts.join('')}`
    : integerPart;
};

const loadAllExhibitionBooths = async (exhibitionId: string) => {
  const pageSize = 200;
  const allBooths: Booth[] = [];
  for (let pageNumber = 1; ; pageNumber += 1) {
    const response = await api.get<BoothPage>(
      `/api/booths?pageNumber=${pageNumber}&pageSize=${pageSize}&exhibitionId=${exhibitionId}`,
    );
    const items = response.data.items ?? [];
    allBooths.push(...items);
    if (items.length < pageSize || allBooths.length >= response.data.totalCount) break;
  }
  return allBooths;
};

export const BoothsPage = () => {
  const { exhibitionId: routeParamId } = useParams<{ exhibitionId: string }>();
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();
  const activeExhibitionId = routeParamId || searchParams.get('exhibitionId') || '';

  const { user } = useAuth();
  const { showToast } = useToast();
  const canManage = user?.roles.some(role => role === 'Admin' || role === 'TenantAdmin' || role === 'SuperAdmin') ?? false;

  // Tabs
  const [activeTab, setActiveTab] = useState<'booths' | 'pricing' | 'designer'>('booths');

  // State
  const [exhibitions, setExhibitions] = useState<Exhibition[]>([]);
  const [currentExhibition, setCurrentExhibition] = useState<Exhibition | null>(null);
  const [booths, setBooths] = useState<Booth[]>([]);
  const [halls, setHalls] = useState<Hall[]>([]);
  const [priceRules, setPriceRules] = useState<BoothPriceRule[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Filters
  const [selectedHallFilter, setSelectedHallFilter] = useState<string>('All');
  const [selectedStatusFilter, setSelectedStatusFilter] = useState<string>('All');
  const [searchQuery, setSearchQuery] = useState('');

  // Booth Modal State
  const [isBoothModalOpen, setIsBoothModalOpen] = useState(false);
  const [editingBoothId, setEditingBoothId] = useState<number | null>(null);
  const [boothForm, setBoothForm] = useState<BoothFormData>(emptyBoothForm);
  const [isSavingBooth, setIsSavingBooth] = useState(false);

  // Price Rule Modal State
  const [isPriceRuleModalOpen, setIsPriceRuleModalOpen] = useState(false);
  const [editingRuleId, setEditingRuleId] = useState<number | null>(null);
  const [priceRuleForm, setPriceRuleForm] = useState<PriceRuleFormData>(emptyPriceRuleForm);
  const [isSavingRule, setIsSavingRule] = useState(false);

  // Calculator State
  const [calcBoothType, setCalcBoothType] = useState('Standard');
  const [calcCategory, setCalcCategory] = useState('Local');
  const [calcArea, setCalcArea] = useState('20');
  const [calcCurrency, setCalcCurrency] = useState('USD');
  const [calcResult, setCalcResult] = useState<{
    targetAmount: number;
    targetCurrency: string;
    baseAmount: number;
    baseCurrency: string;
  } | null>(null);
  const [isCalculating, setIsCalculating] = useState(false);

  // Load Exhibitions list
  const loadExhibitions = useCallback(async () => {
    try {
      const items = await getAllItems<Exhibition>('/api/exhibitions');
      setExhibitions(items);
      return items;
    } catch {
      return [];
    }
  }, []);

  // Load Main Data for the Active Exhibition
  const loadExhibitionData = useCallback(async (exhibId: string) => {
    setLoading(true);
    setError('');
    try {
      // 1. Fetch current exhibition details
      let exh: Exhibition | null = null;
      if (exhibId) {
        const exhRes = await api.get<Exhibition>(`/api/exhibitions/${exhibId}`);
        exh = exhRes.data;
        setCurrentExhibition(exh);
      }

      // 2. Fetch Booths, Halls, Price Rules in parallel
      const [boothsRes, rulesRes, venuesRes] = await Promise.allSettled([
        loadAllExhibitionBooths(exhibId),
        exhibId ? api.get<BoothPriceRule[]>(`/api/pricing/booth-rules?exhibitionId=${exhibId}`) : Promise.resolve({ data: [] }),
        api.get<{ venueID: number; name: string }[]>('/api/venues'),
      ]);

      const loadedBooths = boothsRes.status === 'fulfilled' ? boothsRes.value : [];
      if (boothsRes.status === 'rejected') {
        setError('تعذر تحميل الأجنحة من الخادم.');
      }

      if (rulesRes.status === 'fulfilled' && Array.isArray(rulesRes.value.data)) {
        setPriceRules(rulesRes.value.data);
      } else {
        setPriceRules([]);
      }

      // Collect halls for the exhibition's venue or all venues
      if (venuesRes.status === 'fulfilled' && venuesRes.value.data) {
        const venueList = venuesRes.value.data;
        const relevantVenues = exh?.venueID ? venueList.filter(v => v.venueID === exh?.venueID) : venueList;
        const allHallsPromises = relevantVenues.map(v => api.get<Hall[]>(`/api/halls/venue/${v.venueID}`));
        const hallsResults = await Promise.allSettled(allHallsPromises);
        const collectedHalls: Hall[] = [];
        hallsResults.forEach(res => {
          if (res.status === 'fulfilled' && Array.isArray(res.value.data)) {
            collectedHalls.push(...res.value.data);
          }
        });
        setHalls(collectedHalls);
        const hallIds = new Set(collectedHalls.map(hall => hall.hallID));
        setBooths(loadedBooths.filter(booth => hallIds.has(booth.hallID)));
        if (collectedHalls.length > 0) {
          setBoothForm(prev => prev.hallID
            ? prev
            : { ...prev, hallID: collectedHalls[0].hallID.toString() });
        }
      }
      if (venuesRes.status === 'rejected') {
        setBooths(loadedBooths);
      }
    } catch {
      setError('تعذر تحميل بيانات الأجنحة والتسعير للمعرض.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void (async () => {
      const exhList = await loadExhibitions();
      if (activeExhibitionId) {
        void loadExhibitionData(activeExhibitionId);
      } else if (exhList.length > 0) {
        // Auto-select first exhibition if none provided in URL
        const firstId = exhList[0].exhibitionID.toString();
        setSearchParams({ exhibitionId: firstId }, { replace: true });
      } else {
        setLoading(false);
      }
    })();
  }, [activeExhibitionId, loadExhibitions, loadExhibitionData, setSearchParams]);

  // Statistics
  const stats = useMemo(() => {
    const total = booths.length;
    const available = booths.filter(b => b.status === 'Available').length;
    const reserved = booths.filter(b => b.status === 'Reserved' || b.status === 'Merged').length;
    const pending = booths.filter(b => b.status === 'Maintenance').length;
    const totalArea = booths.reduce((acc, b) => acc + (b.currentAreaSqM || b.originalAreaSqM || 0), 0);
    return { total, available, reserved, pending, totalArea };
  }, [booths]);

  // Filtered Booths
  const filteredBooths = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    return booths.filter(b => {
      const matchesHall = selectedHallFilter === 'All' || b.hallID.toString() === selectedHallFilter;
      const matchesStatus = selectedStatusFilter === 'All' || b.status.toLowerCase() === selectedStatusFilter.toLowerCase();
      const matchesSearch = !q ||
        b.boothNumber.toLowerCase().includes(q) ||
        (b.hallName && b.hallName.toLowerCase().includes(q));
      return matchesHall && matchesStatus && matchesSearch;
    });
  }, [booths, selectedHallFilter, selectedStatusFilter, searchQuery]);

  // Handle Switch Exhibition
  const handleExhibitionChange = (newExhId: string) => {
    if (routeParamId) {
      navigate(`/exhibitions/${newExhId}/booths`);
    } else {
      setSearchParams({ exhibitionId: newExhId });
    }
  };

  // Open Create Booth Modal
  const openCreateBoothModal = () => {
    setEditingBoothId(null);
    setBoothForm({
      ...emptyBoothForm,
      hallID: halls[0]?.hallID.toString() ?? '',
    });
    setIsBoothModalOpen(true);
  };

  // Open Edit Booth Modal
  const openEditBoothModal = (booth: Booth) => {
    setEditingBoothId(booth.boothID);
    setBoothForm({
      hallID: booth.hallID ? booth.hallID.toString() : '',
      boothNumber: booth.boothNumber,
      originalAreaSqM: (booth.currentAreaSqM || booth.originalAreaSqM || 12).toString(),
      width: (booth.width || 4).toString(),
      height: (booth.height || 3).toString(),
      shapeType: booth.shapeType || 'Rect',
      boothType: booth.boothType || 'Standard',
      status: booth.status || 'Available',
    });
    setIsBoothModalOpen(true);
  };

  // Save Booth (Create/Edit)
  const handleSaveBooth = async (e: FormEvent) => {
    e.preventDefault();
    if (!boothForm.boothNumber.trim() || !boothForm.hallID) {
      showToast('يرجى تحديد القاعة ورقم الجناح.', 'warning');
      return;
    }

    setIsSavingBooth(true);
    try {
      if (editingBoothId) {
        await api.put(`/api/booths/${editingBoothId}`, {
          boothNumber: boothForm.boothNumber.trim(),
          status: boothForm.status,
          width: Number(boothForm.width) || null,
          height: Number(boothForm.height) || null,
          shapeType: boothForm.shapeType,
          boothType: boothForm.boothType,
          originalAreaSqM: Number(boothForm.originalAreaSqM),
          posX: booths.find(item => item.boothID === editingBoothId)?.posX ?? null,
          posY: booths.find(item => item.boothID === editingBoothId)?.posY ?? null,
          rotationAngle: booths.find(item => item.boothID === editingBoothId)?.rotationAngle ?? null,
          shapePolygonJSON: booths.find(item => item.boothID === editingBoothId)?.shapePolygonJSON ?? '',
          assignedPriceRuleID: booths.find(item => item.boothID === editingBoothId)?.assignedPriceRuleID ?? null,
        });
        showToast('تم تحديث بيانات الجناح بنجاح.', 'success');
      } else {
        await api.post('/api/booths', {
          hallID: Number(boothForm.hallID),
          boothNumber: boothForm.boothNumber.trim(),
          originalAreaSqM: Number(boothForm.originalAreaSqM) || 12,
          width: Number(boothForm.width) || null,
          height: Number(boothForm.height) || null,
          shapeType: boothForm.shapeType,
          boothType: boothForm.boothType,
        });
        showToast('تمت إضافة الجناح بنجاح للمعرض.', 'success');
      }

      setIsBoothModalOpen(false);
      if (activeExhibitionId) void loadExhibitionData(activeExhibitionId);
    } catch {
      showToast('فشل حفظ بيانات الجناح. تحقق من عدم تكرار رقم الجناح في نفس القاعة.', 'danger');
    } finally {
      setIsSavingBooth(false);
    }
  };

  // Delete Booth
  const handleDeleteBooth = async (booth: Booth) => {
    if (!window.confirm(`هل أنت متأكد من حذف الجناح «${booth.boothNumber}»؟`)) return;

    try {
      await api.delete(`/api/booths/${booth.boothID}`);
      showToast('تم حذف الجناح بنجاح.', 'success');
      if (activeExhibitionId) void loadExhibitionData(activeExhibitionId);
    } catch {
      showToast('تعذر حذف الجناح؛ قد يكون مرتبطاً بحجز مؤكد.', 'danger');
    }
  };

  // Open Create Price Rule Modal
  const openCreatePriceRuleModal = () => {
    setEditingRuleId(null);
    setPriceRuleForm({
      ...emptyPriceRuleForm,
      currencyCode: currentExhibition?.entryCurrency || user?.baseCurrency || 'LYD',
    });
    setIsPriceRuleModalOpen(true);
  };

  // Open Edit Price Rule Modal
  const openEditPriceRuleModal = (rule: BoothPriceRule) => {
    setEditingRuleId(rule.ruleID);
    setPriceRuleForm({
      boothType: rule.boothType || 'Standard',
      exhibitorCategory: rule.exhibitorCategory || 'Local',
      pricePerSqM: rule.pricePerSqM.toString(),
      currencyCode: rule.currencyCode || currentExhibition?.entryCurrency || 'LYD',
      minAreaSqM: (rule.minAreaSqM || 9).toString(),
      maxAreaSqM: (rule.maxAreaSqM || 100).toString(),
      ruleName: rule.ruleName || '',
      notes: rule.notes || '',
    });
    setIsPriceRuleModalOpen(true);
  };

  // Save Price Rule
  const handleSavePriceRule = async (e: FormEvent) => {
    e.preventDefault();
    if (!activeExhibitionId) {
      showToast('يرجى تحديد المعرض أولاً.', 'warning');
      return;
    }

    const pricePerSqM = Number(priceRuleForm.pricePerSqM);
    if (!Number.isFinite(pricePerSqM) || pricePerSqM <= 0) {
      showToast('يرجى إدخال سعر صحيح أكبر من صفر.', 'warning');
      return;
    }

    setIsSavingRule(true);
    try {
      const payload = {
        exhibitionID: Number(activeExhibitionId),
        boothType: priceRuleForm.boothType,
        exhibitorCategory: priceRuleForm.exhibitorCategory,
        productCategory: '',
        pricePerSqM,
        currencyCode: priceRuleForm.currencyCode.toUpperCase(),
        minAreaSqM: Number(priceRuleForm.minAreaSqM) || null,
        maxAreaSqM: Number(priceRuleForm.maxAreaSqM) || null,
        ruleName: priceRuleForm.ruleName.trim() || `تسعير ${boothTypeNames[priceRuleForm.boothType]}`,
        notes: priceRuleForm.notes.trim(),
        validFrom: new Date().toISOString(),
      };

      if (editingRuleId) {
        await api.put(`/api/pricing/booth-rules/${editingRuleId}`, payload);
        showToast('تم تحديث قاعدة التسعير بنجاح.', 'success');
      } else {
        await api.post('/api/pricing/booth-rules', payload);
        showToast('تمت إضافة قاعدة التسعير للمعرض بنجاح.', 'success');
      }

      setIsPriceRuleModalOpen(false);
      if (activeExhibitionId) void loadExhibitionData(activeExhibitionId);
    } catch {
      showToast('فشل حفظ قاعدة التسعير.', 'danger');
    } finally {
      setIsSavingRule(false);
    }
  };

  // Delete Price Rule
  const handleDeletePriceRule = async (rule: BoothPriceRule) => {
    if (!window.confirm(`هل أنت متأكد من حذف قاعدة التسعير «${rule.ruleName || rule.boothType}»؟`)) return;

    try {
      await api.delete(`/api/pricing/booth-rules/${rule.ruleID}`);
      showToast('تم حذف قاعدة التسعير.', 'success');
      if (activeExhibitionId) void loadExhibitionData(activeExhibitionId);
    } catch {
      showToast('تعذر حذف قاعدة التسعير.', 'danger');
    }
  };

  // Calculate Price Interactive Calculator
  const handleCalculatePrice = async () => {
    if (!activeExhibitionId) return;
    setIsCalculating(true);
    try {
      const area = Number(calcArea) || 0;
      const res = await api.post<BoothPriceCalculation>('/api/pricing/calculate/booth/details', {
        exhibitionId: Number(activeExhibitionId),
        boothType: calcBoothType,
        exhibitorCategory: calcCategory,
        areaSqM: area,
      });
      const basePrice = res.data.amount;
      const baseCurrency = res.data.currencyCode;

      let targetPrice = basePrice;
      if (calcCurrency.toUpperCase() !== baseCurrency.toUpperCase()) {
        const convRes = await api.post<number>('/api/currencies/convert', {
          amount: basePrice,
          from: baseCurrency,
          to: calcCurrency
        });
        targetPrice = convRes.data;
      }

      setCalcResult({
        targetAmount: targetPrice,
        targetCurrency: calcCurrency,
        baseAmount: basePrice,
        baseCurrency,
      });
    } catch {
      setCalcResult(null);
      showToast('تعذر حساب السعر: تحقق من قاعدة التسعير وأسعار الصرف المسجلة.', 'warning');
    } finally {
      setIsCalculating(false);
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Top Breadcrumb & Switcher */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '16px', flexWrap: 'wrap' }}>
        <NavLink
          to="/exhibitions"
          className="btn-secondary"
          style={{ display: 'inline-flex', alignItems: 'center', gap: '6px', fontSize: '13px', textDecoration: 'none' }}
        >
          <ArrowRight size={16} />
          <span>العودة لقائمة المعارض</span>
        </NavLink>

        {exhibitions.length > 1 && (
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>تبديل المعرض:</span>
            <select
              className="input-field"
              value={activeExhibitionId}
              onChange={e => handleExhibitionChange(e.target.value)}
              style={{ minWidth: '220px', padding: '6px 12px', fontSize: '13px' }}
            >
              {exhibitions.map(ex => (
                <option key={ex.exhibitionID} value={ex.exhibitionID.toString()}>
                  {ex.name}
                </option>
              ))}
            </select>
          </div>
        )}
      </div>

      {/* Exhibition Hub Banner Card */}
      {currentExhibition && (
        <div
          className="card"
          style={{
            background: 'linear-gradient(135deg, #1e293b 0%, #0f172a 100%)',
            color: '#ffffff',
            padding: '24px 28px',
            borderRadius: 'var(--radius-xl)',
            boxShadow: '0 10px 25px -5px rgba(15, 23, 42, 0.3)',
            display: 'flex',
            flexDirection: 'column',
            gap: '16px'
          }}
        >
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '16px', flexWrap: 'wrap' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
              <div
                style={{
                  width: '48px',
                  height: '48px',
                  borderRadius: 'var(--radius-lg)',
                  background: 'var(--primary-gradient)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  color: '#ffffff',
                  boxShadow: 'var(--primary-glow)'
                }}
              >
                <Store size={26} />
              </div>
              <div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '10px', flexWrap: 'wrap' }}>
                  <h1 style={{ fontSize: '22px', fontWeight: 800, color: '#ffffff', margin: 0 }}>{currentExhibition.name}</h1>
                  <span className="badge badge-info" style={{ background: 'rgba(255,255,255,0.15)', color: '#ffffff', border: '1px solid rgba(255,255,255,0.2)' }}>
                    {currentExhibition.status}
                  </span>
                </div>
                <p style={{ color: '#94a3b8', fontSize: '13px', marginTop: '4px', margin: 0 }}>
                  مركز إدارة وحجز وتسعير الأجنحة والمساحات المخصصة لهذا المعرض.
                </p>
              </div>
            </div>

            {/* Quick action buttons */}
            <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
              <NavLink
                to={`/booths/designer?exhibitionId=${currentExhibition.exhibitionID}`}
                className="btn-secondary"
                style={{
                  background: 'rgba(255,255,255,0.12)',
                  color: '#ffffff',
                  borderColor: 'rgba(255,255,255,0.2)',
                  textDecoration: 'none',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '6px'
                }}
              >
                <Compass size={16} />
                <span>مصمم المخططات 2D</span>
              </NavLink>

              {canManage && (
                <button
                  className="btn-primary"
                  onClick={openCreateBoothModal}
                  style={{ display: 'flex', alignItems: 'center', gap: '6px' }}
                >
                  <Plus size={17} />
                  <span>إضافة جناح جديد</span>
                </button>
              )}
            </div>
          </div>

          <div style={{ display: 'flex', gap: '24px', flexWrap: 'wrap', borderTop: '1px solid rgba(255,255,255,0.1)', paddingTop: '14px', fontSize: '13px', color: '#cbd5e1' }}>
            <span style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
              <Building2 size={15} style={{ color: '#38bdf8' }} />
              الموقع: <strong>{currentExhibition.venueName || 'مركز المعارض'}</strong>
            </span>
            <span style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
              <Calendar size={15} style={{ color: '#38bdf8' }} />
              الفترة: <strong>{formatDate(currentExhibition.startDate)} — {formatDate(currentExhibition.endDate)}</strong>
            </span>
            <span style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
              <DollarSign size={15} style={{ color: '#38bdf8' }} />
              العملة المعتمدة: <strong>{currentExhibition.entryCurrency || 'LYD'}</strong>
            </span>
            <span style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
              <Layers size={15} style={{ color: '#38bdf8' }} />
              القاعات المتاحة: <strong>{halls.length > 0 ? halls.map(h => h.hallName).join('، ') : 'القاعة الكبرى'}</strong>
            </span>
          </div>
        </div>
      )}

      {/* Tabs Bar */}
      <div style={{ display: 'flex', gap: '8px', borderBottom: '2px solid var(--border-main)', paddingBottom: '2px' }}>
        <button
          onClick={() => setActiveTab('booths')}
          style={{
            padding: '10px 18px',
            fontSize: '14px',
            fontWeight: 700,
            color: activeTab === 'booths' ? 'var(--primary)' : 'var(--text-secondary)',
            borderBottom: activeTab === 'booths' ? '3px solid var(--primary)' : '3px solid transparent',
            background: 'transparent',
            borderRadius: '4px 4px 0 0',
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            cursor: 'pointer'
          }}
        >
          <Store size={17} />
          <span>الأجنحة والمساحات ({booths.length})</span>
        </button>

        <button
          onClick={() => setActiveTab('pricing')}
          style={{
            padding: '10px 18px',
            fontSize: '14px',
            fontWeight: 700,
            color: activeTab === 'pricing' ? 'var(--primary)' : 'var(--text-secondary)',
            borderBottom: activeTab === 'pricing' ? '3px solid var(--primary)' : '3px solid transparent',
            background: 'transparent',
            borderRadius: '4px 4px 0 0',
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            cursor: 'pointer'
          }}
        >
          <Tag size={17} />
          <span>قواعد التسعير والحاسبة ({priceRules.length})</span>
        </button>

        <button
          onClick={() => setActiveTab('designer')}
          style={{
            padding: '10px 18px',
            fontSize: '14px',
            fontWeight: 700,
            color: activeTab === 'designer' ? 'var(--primary)' : 'var(--text-secondary)',
            borderBottom: activeTab === 'designer' ? '3px solid var(--primary)' : '3px solid transparent',
            background: 'transparent',
            borderRadius: '4px 4px 0 0',
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            cursor: 'pointer'
          }}
        >
          <Compass size={17} />
          <span>المخطط البصري 2D</span>
        </button>
      </div>

      {error && <div role="alert" style={{ padding: '14px', background: 'var(--danger-bg)', color: 'var(--danger-text)', borderRadius: 'var(--radius-lg)' }}>{error}</div>}

      {/* ━━━━━━━━━━━━━━━━━ TAB 1: BOOTHS & SPACES ━━━━━━━━━━━━━━━━━ */}
      {activeTab === 'booths' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
          {/* Stat KPI Cards */}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: '16px' }}>
            <div className="card" style={{ padding: '16px 20px' }}>
              <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>إجمالي الأجنحة</span>
              <h2 style={{ fontSize: '24px', fontWeight: 800, marginTop: '4px', color: 'var(--text-primary)' }}>{stats.total}</h2>
            </div>
            <div className="card" style={{ padding: '16px 20px', borderLeft: '4px solid var(--success)' }}>
              <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>المتاحة للحجز</span>
              <h2 style={{ fontSize: '24px', fontWeight: 800, marginTop: '4px', color: 'var(--success)' }}>{stats.available}</h2>
            </div>
            <div className="card" style={{ padding: '16px 20px', borderLeft: '4px solid var(--primary)' }}>
              <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>المحجوزة</span>
              <h2 style={{ fontSize: '24px', fontWeight: 800, marginTop: '4px', color: 'var(--primary)' }}>{stats.reserved}</h2>
            </div>
            <div className="card" style={{ padding: '16px 20px', borderLeft: '4px solid var(--warning)' }}>
              <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>قيد المراجعة</span>
              <h2 style={{ fontSize: '24px', fontWeight: 800, marginTop: '4px', color: 'var(--warning)' }}>{stats.pending}</h2>
            </div>
            <div className="card" style={{ padding: '16px 20px', borderLeft: '4px solid #6366f1' }}>
              <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>إجمالي المساحة المعروضة</span>
              <h2 style={{ fontSize: '24px', fontWeight: 800, marginTop: '4px', color: '#6366f1' }}>{stats.totalArea} م²</h2>
            </div>
          </div>

          {/* Filters Bar */}
          <div style={{ display: 'flex', gap: '14px', flexWrap: 'wrap', alignItems: 'center' }}>
            <div className="card" style={{ flex: 1, minWidth: '260px', position: 'relative', padding: 0 }}>
              <Search size={18} style={{ position: 'absolute', right: '16px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }} />
              <input
                className="input-field"
                value={searchQuery}
                onChange={e => setSearchQuery(e.target.value)}
                style={{ paddingRight: '44px', border: 0 }}
                placeholder="ابحث برقم الجناح أو القاعة…"
              />
            </div>

            {/* Hall Filter */}
            <div style={{ minWidth: '180px' }}>
              <select
                className="input-field"
                value={selectedHallFilter}
                onChange={e => setSelectedHallFilter(e.target.value)}
              >
                <option value="All">جميع القاعات ({halls.length})</option>
                {halls.map(h => (
                  <option key={h.hallID} value={h.hallID.toString()}>{h.hallName}</option>
                ))}
              </select>
            </div>

            {/* Status Filter */}
            <div style={{ minWidth: '160px' }}>
              <select
                className="input-field"
                value={selectedStatusFilter}
                onChange={e => setSelectedStatusFilter(e.target.value)}
              >
                <option value="All">جميع الحالات</option>
                <option value="Available">متاح</option>
                <option value="Reserved">محجوز</option>
                <option value="Maintenance">قيد الصيانة</option>
              </select>
            </div>

            {canManage && (
              <button className="btn-primary" onClick={openCreateBoothModal}>
                <Plus size={17} /> إضافة جناح
              </button>
            )}
          </div>

          {/* Booths Table Card */}
          <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
            <div className="table-container" style={{ border: 0 }}>
              <table className="data-table">
                <thead>
                  <tr>
                    <th>رقم الجناح</th>
                    <th>القاعة</th>
                    <th>المساحة</th>
                    <th>الأبعاد (عرض × طول)</th>
                    <th>النوع / الفئة</th>
                    <th>الحالة</th>
                    {canManage && <th>الإجراءات</th>}
                  </tr>
                </thead>
                <tbody>
                  {filteredBooths.map(item => (
                    <tr key={item.boothID}>
                      <td><strong>{item.boothNumber}</strong></td>
                      <td>{item.hallName || '—'}</td>
                      <td>{item.currentAreaSqM} م²</td>
                      <td>{item.width && item.height ? `${item.width} × ${item.height} م` : '—'}</td>
                      <td>{shapeNames[item.shapeType ?? 'Rect'] ?? item.shapeType}</td>
                      <td>
                        <span className={`badge ${
                          item.status === 'Available' ? 'badge-success' :
                          item.status === 'Reserved' || item.status === 'Merged' ? 'badge-primary' : 'badge-warning'
                        }`}>
                          {statusNames[item.status] ?? item.status}
                        </span>
                      </td>
                      {canManage && (
                        <td>
                          <div style={{ display: 'flex', gap: '8px' }}>
                            <button className="btn-icon" title="تعديل الجناح" onClick={() => openEditBoothModal(item)}>
                              <Edit2 size={14} />
                            </button>
                            <button className="btn-icon" title="حذف الجناح" style={{ color: 'var(--danger)' }} onClick={() => void handleDeleteBooth(item)}>
                              <Trash2 size={14} />
                            </button>
                          </div>
                        </td>
                      )}
                    </tr>
                  ))}
                  {!loading && filteredBooths.length === 0 && (
                    <tr><td colSpan={canManage ? 7 : 6} style={{ textAlign: 'center', padding: '30px' }}>لا توجد أجنحة مضافة لهذا المعرض بعد. اضغط على «إضافة جناح» للبدء.</td></tr>
                  )}
                  {loading && (
                    <tr><td colSpan={canManage ? 7 : 6} style={{ textAlign: 'center', padding: '30px' }}>جاري التحميل…</td></tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* ━━━━━━━━━━━━━━━━━ TAB 2: PRICING RULES & CALCULATOR ━━━━━━━━━━━━━━━━━ */}
      {activeTab === 'pricing' && (
        <div style={{ display: 'grid', gridTemplateColumns: 'minmax(320px, 1fr) minmax(320px, 380px)', gap: '24px', alignItems: 'start' }}>
          {/* Rules Table */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div>
                <h2 style={{ fontSize: '18px', fontWeight: 800 }}>جدول أسعار أجنحة المعرض</h2>
                <p style={{ color: 'var(--text-secondary)', fontSize: '13px' }}>قواعد احتساب سعر المتر المربع حسب الفئة ونوع الجناح.</p>
              </div>
              {canManage && (
                <button className="btn-primary" onClick={openCreatePriceRuleModal}>
                  <Plus size={16} /> إضافة قاعدة تسعير
                </button>
              )}
            </div>

            <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
              <div className="table-container" style={{ border: 0 }}>
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>اسم القاعدة</th>
                      <th>نوع الجناح</th>
                      <th>فئة العارض</th>
                      <th>سعر المتر المربع</th>
                      <th>المساحة المسموحة</th>
                      {canManage && <th>الإجراءات</th>}
                    </tr>
                  </thead>
                  <tbody>
                    {priceRules.map(rule => (
                      <tr key={rule.ruleID}>
                        <td><strong>{rule.ruleName || 'تسعير قياسي'}</strong></td>
                        <td>{boothTypeNames[rule.boothType || 'Standard'] || rule.boothType || 'الكل'}</td>
                        <td>{exhibitorCategoryNames[rule.exhibitorCategory || 'Local'] || rule.exhibitorCategory || 'الكل'}</td>
                        <td>
                          <span style={{ fontWeight: 700, color: 'var(--primary)' }}>
                            {rule.pricePerSqM} {rule.currencyCode || currentExhibition?.entryCurrency || 'LYD'}
                          </span>
                          <span style={{ fontSize: '11px', color: 'var(--text-secondary)', display: 'block' }}>لكل م²</span>
                        </td>
                        <td>
                          {rule.minAreaSqM ? `${rule.minAreaSqM} م²` : '0'} — {rule.maxAreaSqM ? `${rule.maxAreaSqM} م²` : 'غير محدد'}
                        </td>
                        {canManage && (
                          <td>
                            <div style={{ display: 'flex', gap: '8px' }}>
                              <button className="btn-icon" title="تعديل قاعدة التسعير" onClick={() => openEditPriceRuleModal(rule)}>
                                <Edit2 size={14} />
                              </button>
                              <button className="btn-icon" title="حذف" style={{ color: 'var(--danger)' }} onClick={() => void handleDeletePriceRule(rule)}>
                                <Trash2 size={14} />
                              </button>
                            </div>
                          </td>
                        )}
                      </tr>
                    ))}
                    {priceRules.length === 0 && (
                      <tr>
                        <td colSpan={canManage ? 6 : 5} style={{ textAlign: 'center', padding: '30px', color: 'var(--text-secondary)' }}>
                          لا توجد قواعد تسعير مخصصة لهذا المعرض بعد. يمكنك إضافة قاعدة جديدة لضبط أسعار المتر المربع.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </div>

          {/* Real-time Price Calculator Widget */}
          <div className="card" style={{ display: 'flex', flexDirection: 'column', gap: '16px', background: 'var(--bg-subtle, #f8fafc)', border: '1px solid var(--border-main)' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
              <div style={{ width: '36px', height: '36px', borderRadius: 'var(--radius-md)', background: 'var(--primary-gradient)', color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                <Calculator size={20} />
              </div>
              <div>
                <h3 style={{ fontSize: '16px', fontWeight: 800 }}>حاسبة تسعير الأجنحة</h3>
                <span style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>حساب التكلفة التقديرية لجناح معين</span>
              </div>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">نوع وفئة الجناح</label>
              <select className="input-field" value={calcBoothType} onChange={e => setCalcBoothType(e.target.value)}>
                {Object.entries(boothTypeNames).map(([k, v]) => (
                  <option key={k} value={k}>{v}</option>
                ))}
              </select>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">فئة العارض</label>
              <select className="input-field" value={calcCategory} onChange={e => setCalcCategory(e.target.value)}>
                {Object.entries(exhibitorCategoryNames).map(([k, v]) => (
                  <option key={k} value={k}>{v}</option>
                ))}
              </select>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">المساحة المطلوبة (م²)</label>
              <input
                className="input-field"
                type="number"
                min="1"
                step="1"
                value={calcArea}
                onChange={e => setCalcArea(e.target.value)}
              />
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">عملة الحساب والعرض</label>
              <select className="input-field" value={calcCurrency} onChange={e => setCalcCurrency(e.target.value)}>
                <option value="USD">دولار أمريكي (USD)</option>
                <option value="LYD">دينار ليبي (LYD)</option>
                <option value="EUR">يورو (EUR)</option>
                <option value="SAR">ريال سعودي (SAR)</option>
                <option value="AED">درهم إماراتي (AED)</option>
                <option value="GBP">جنيه إسترليني (GBP)</option>
                <option value="TRY">ليرة تركية (TRY)</option>
                <option value="EGP">جنيه مصري (EGP)</option>
                <option value="TND">دينار تونسي (TND)</option>
              </select>
            </div>

            <button className="btn-primary" onClick={handleCalculatePrice} disabled={isCalculating}>
              {isCalculating ? 'جاري الحساب…' : 'احسب التكلفة التقديرية'}
            </button>

            {calcResult !== null && (
              <div
                style={{
                  padding: '16px',
                  background: '#ffffff',
                  border: '2px solid var(--primary)',
                  borderRadius: 'var(--radius-lg)',
                  textAlign: 'center',
                  display: 'flex',
                  flexDirection: 'column',
                  gap: '6px'
                }}
              >
                <span style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>إجمالي سعر الجناح التقديري</span>
                <span style={{ fontSize: '24px', fontWeight: 900, color: 'var(--primary)' }}>
                  {calcResult.targetAmount.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })} {calcResult.targetCurrency}
                </span>
                {calcResult.targetCurrency !== calcResult.baseCurrency && (
                  <span style={{ fontSize: '13px', fontWeight: 600, color: 'var(--text-secondary)' }}>
                    (يعادل: {calcResult.baseAmount.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })} {calcResult.baseCurrency})
                  </span>
                )}
                <span style={{ fontSize: '11px', color: 'var(--text-muted)' }}>
                  (المساحة: {calcArea} م² × الفئة: {shapeNames[calcBoothType] || calcBoothType})
                </span>
              </div>
            )}
          </div>
        </div>
      )}

      {/* ━━━━━━━━━━━━━━━━━ TAB 3: VISUAL 2D PLANNER ━━━━━━━━━━━━━━━━━ */}
      {activeTab === 'designer' && (
        <div className="card" style={{ textAlign: 'center', padding: '40px 20px', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '16px' }}>
          <div style={{ width: '64px', height: '64px', borderRadius: '50%', background: 'rgba(59, 130, 246, 0.1)', color: 'var(--primary)', display: 'grid', placeItems: 'center' }}>
            <Compass size={32} />
          </div>
          <div>
            <h2 style={{ fontSize: '20px', fontWeight: 800 }}>مصمم مخططات الأجنحة التفاعلي 2D</h2>
            <p style={{ color: 'var(--text-secondary)', fontSize: '14px', maxWidth: '520px', margin: '8px auto 0' }}>
              يتيح لك مصمم المخططات رسم وتوزيع الأجنحة في قاعات المعرض، دمج وفصل الأجنحة، ومعاينة حالة الحجز بالألوان التفاعلية.
            </p>
          </div>
          <NavLink
            to={`/booths/designer?exhibitionId=${activeExhibitionId}`}
            className="btn-primary"
            style={{ textDecoration: 'none', padding: '12px 24px', fontSize: '14px' }}
          >
            <Compass size={18} />
            <span>فتح مصمم المخططات لهذا المعرض</span>
          </NavLink>
        </div>
      )}

      {/* ━━━━━━━━━━━━━━━━━ MODAL: CREATE / EDIT BOOTH ━━━━━━━━━━━━━━━━━ */}
      {isBoothModalOpen && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 100, background: 'rgba(15,23,42,.45)', display: 'grid', placeItems: 'center', padding: '20px' }}>
          <form className="card" onSubmit={handleSaveBooth} style={{ width: 'min(520px, 100%)', display: 'grid', gap: '14px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h2>{editingBoothId ? 'تعديل بيانات الجناح' : 'إضافة جناح جديد للمعرض'}</h2>
              <button type="button" className="btn-icon" onClick={() => setIsBoothModalOpen(false)}><X size={18} /></button>
            </div>

            {!editingBoothId && (
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">القاعة *</label>
                <select
                  className="input-field"
                  required
                  value={boothForm.hallID}
                  onChange={e => setBoothForm(prev => ({ ...prev, hallID: e.target.value }))}
                >
                  <option value="">اختر القاعة</option>
                  {halls.map(h => (
                    <option key={h.hallID} value={h.hallID.toString()}>{h.hallName}</option>
                  ))}
                </select>
              </div>
            )}

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">رقم أو رمز الجناح *</label>
              <input
                className="input-field"
                required
                placeholder="مثال: A-101 أو VIP-01"
                value={boothForm.boothNumber}
                onChange={e => setBoothForm(prev => ({ ...prev, boothNumber: e.target.value }))}
              />
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '10px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">المساحة (م²)</label>
                <input
                  className="input-field"
                  type="number"
                  step="0.1"
                  required
                  readOnly
                  value={boothForm.originalAreaSqM}
                />
              </div>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">العرض (م)</label>
                <input
                  className="input-field"
                  type="number"
                  step="0.1"
                  value={boothForm.width}
                  onChange={e => setBoothForm(prev => ({
                    ...prev,
                    width: e.target.value,
                    originalAreaSqM: String(Number(e.target.value) * Number(prev.height)),
                  }))}
                />
              </div>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">الطول (م)</label>
                <input
                  className="input-field"
                  type="number"
                  step="0.1"
                  value={boothForm.height}
                  onChange={e => setBoothForm(prev => ({
                    ...prev,
                    height: e.target.value,
                    originalAreaSqM: String(Number(prev.width) * Number(e.target.value)),
                  }))}
                />
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">الشكل على المخطط</label>
                <select
                  className="input-field"
                  value={boothForm.shapeType}
                  onChange={e => setBoothForm(prev => ({ ...prev, shapeType: e.target.value }))}
                >
                  {Object.entries(shapeNames).map(([k, v]) => (
                    <option key={k} value={k}>{v}</option>
                  ))}
                </select>
              </div>

              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">فئة التسعير *</label>
                <select
                  className="input-field"
                  required
                  value={boothForm.boothType}
                  onChange={e => setBoothForm(prev => ({ ...prev, boothType: e.target.value }))}
                >
                  {Object.entries(boothTypeNames).map(([k, v]) => (
                    <option key={k} value={k}>{v}</option>
                  ))}
                </select>
              </div>

              {editingBoothId && (
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">حالة الجناح</label>
                  <select
                    className="input-field"
                    value={boothForm.status}
                    onChange={e => setBoothForm(prev => ({ ...prev, status: e.target.value }))}
                  >
                    <option value="Available">متاح للحجز</option>
                    <option value="Reserved">محجوز ومؤكد</option>
                    <option value="Merged">مدمج</option>
                    <option value="Maintenance">خارج الخدمة للصيانة</option>
                  </select>
                </div>
              )}
            </div>

            <div style={{ display: 'flex', gap: '10px', marginTop: '8px' }}>
              <button className="btn-primary" type="submit" disabled={isSavingBooth} style={{ flex: 1 }}>
                {isSavingBooth ? 'جاري الحفظ…' : editingBoothId ? 'تحديث الجناح' : 'إضافة الجناح'}
              </button>
              <button type="button" className="btn-secondary" onClick={() => setIsBoothModalOpen(false)}>
                إلغاء
              </button>
            </div>
          </form>
        </div>
      )}

      {/* ━━━━━━━━━━━━━━━━━ MODAL: CREATE / EDIT PRICE RULE ━━━━━━━━━━━━━━━━━ */}
      {isPriceRuleModalOpen && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 100, background: 'rgba(15,23,42,.45)', display: 'grid', placeItems: 'center', padding: '20px' }}>
          <form className="card" onSubmit={handleSavePriceRule} style={{ width: 'min(540px, 100%)', display: 'grid', gap: '14px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h2>{editingRuleId ? 'تعديل قاعدة التسعير' : 'إضافة قاعدة تسعير للمعرض'}</h2>
              <button type="button" className="btn-icon" onClick={() => setIsPriceRuleModalOpen(false)}><X size={18} /></button>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">اسم / تسمية القاعدة</label>
              <input
                className="input-field"
                placeholder="مثال: تسعير الأجنحة القياسية - عارضين محليين"
                value={priceRuleForm.ruleName}
                onChange={e => setPriceRuleForm(prev => ({ ...prev, ruleName: e.target.value }))}
              />
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">نوع الجناح *</label>
                <select
                  className="input-field"
                  value={priceRuleForm.boothType}
                  onChange={e => setPriceRuleForm(prev => ({ ...prev, boothType: e.target.value }))}
                >
                  {Object.entries(boothTypeNames).map(([k, v]) => (
                    <option key={k} value={k}>{v}</option>
                  ))}
                </select>
              </div>

              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">فئة العارض *</label>
                <select
                  className="input-field"
                  value={priceRuleForm.exhibitorCategory}
                  onChange={e => setPriceRuleForm(prev => ({ ...prev, exhibitorCategory: e.target.value }))}
                >
                  {Object.entries(exhibitorCategoryNames).map(([k, v]) => (
                    <option key={k} value={k}>{v}</option>
                  ))}
                </select>
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">سعر المتر المربع (لكل م²) *</label>
                <input
                  className="input-field"
                  type="text"
                  inputMode="decimal"
                  pattern="[0-9]+([.][0-9]+)?"
                  required
                  placeholder="150"
                  dir="ltr"
                  value={priceRuleForm.pricePerSqM}
                  onChange={e => setPriceRuleForm(prev => ({
                    ...prev,
                    pricePerSqM: normalizeDecimalInput(e.target.value),
                  }))}
                />
              </div>

              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">العملة المحددة للقاعدة *</label>
                <select
                  className="input-field"
                  value={priceRuleForm.currencyCode}
                  onChange={e => setPriceRuleForm(prev => ({ ...prev, currencyCode: e.target.value }))}
                >
                  <option value="LYD">دينار ليبي (LYD)</option>
                  <option value="USD">دولار أمريكي (USD)</option>
                  <option value="EUR">يورو (EUR)</option>
                  <option value="SAR">ريال سعودي (SAR)</option>
                  <option value="AED">درهم إماراتي (AED)</option>
                  <option value="GBP">جنيه إسترليني (GBP)</option>
                  <option value="TRY">ليرة تركية (TRY)</option>
                  <option value="EGP">جنيه مصري (EGP)</option>
                  <option value="TND">دينار تونسي (TND)</option>
                </select>
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">أدنى مساحة (م²)</label>
                <input
                  className="input-field"
                  type="number"
                  value={priceRuleForm.minAreaSqM}
                  onChange={e => setPriceRuleForm(prev => ({ ...prev, minAreaSqM: e.target.value }))}
                />
              </div>

              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">أقصى مساحة (م²)</label>
                <input
                  className="input-field"
                  type="number"
                  value={priceRuleForm.maxAreaSqM}
                  onChange={e => setPriceRuleForm(prev => ({ ...prev, maxAreaSqM: e.target.value }))}
                />
              </div>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">ملاحظات وشروط</label>
              <textarea
                className="input-field"
                rows={2}
                placeholder="ملاحظات إضافية حول شروط التسعير والخصم..."
                value={priceRuleForm.notes}
                onChange={e => setPriceRuleForm(prev => ({ ...prev, notes: e.target.value }))}
              />
            </div>

            <div style={{ display: 'flex', gap: '10px', marginTop: '8px' }}>
              <button className="btn-primary" type="submit" disabled={isSavingRule} style={{ flex: 1 }}>
                {isSavingRule ? 'جاري الحفظ…' : editingRuleId ? 'تحديث القاعدة' : 'حفظ قاعدة التسعير'}
              </button>
              <button type="button" className="btn-secondary" onClick={() => setIsPriceRuleModalOpen(false)}>
                إلغاء
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
};
