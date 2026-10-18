import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { RefreshCw, Save } from 'lucide-react';
import { useToast } from '../../../app/providers/ToastContext';
import { api } from '../../../services/api';
import { getApiErrorMessage } from '../../../services/apiError';

interface CurrencyOption {
  currencyCode: string;
  currencyName: string;
  symbol: string;
  isActive: boolean;
}

interface ExchangeRateItem {
  exchangeRateID: number;
  fromCurrency: string;
  toCurrency: string;
  rate: number;
  rateDate: string;
  source: string;
}

export const ExchangeRatesPanel = () => {
  const { showToast } = useToast();
  const [currencies, setCurrencies] = useState<CurrencyOption[]>([]);
  const [rates, setRates] = useState<ExchangeRateItem[]>([]);
  const [fromCurrency, setFromCurrency] = useState('USD');
  const [toCurrency, setToCurrency] = useState('LYD');
  const [rate, setRate] = useState('');
  const [rateDate, setRateDate] = useState(new Date().toISOString().slice(0, 10));
  const [source, setSource] = useState('إدخال يدوي');
  const [saving, setSaving] = useState(false);
  const [loading, setLoading] = useState(false);

  const loadRates = useCallback(async (from: string) => {
    if (!from) return;
    setLoading(true);
    try {
      const response = await api.get<ExchangeRateItem[]>(`/api/currencies/rates/${from}`);
      setRates((response.data ?? []).sort((a, b) => b.rateDate.localeCompare(a.rateDate)));
    } catch (error) {
      showToast(getApiErrorMessage(error, 'تعذر تحميل أسعار الصرف.'), 'danger');
    } finally {
      setLoading(false);
    }
  }, [showToast]);

  useEffect(() => {
    void api.get<CurrencyOption[]>('/api/currencies')
      .then(response => {
        const active = (response.data ?? []).filter(item => item.isActive);
        setCurrencies(active);
        if (!active.some(item => item.currencyCode === fromCurrency) && active[0]) {
          setFromCurrency(active[0].currencyCode);
        }
      })
      .catch(error => showToast(getApiErrorMessage(error, 'تعذر تحميل العملات.'), 'danger'));
  }, [fromCurrency, showToast]);

  useEffect(() => {
    void loadRates(fromCurrency);
  }, [fromCurrency, loadRates]);

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    const numericRate = Number(rate);
    if (fromCurrency === toCurrency) {
      showToast('يجب أن تختلف عملة المصدر عن عملة الهدف.', 'warning');
      return;
    }
    if (!Number.isFinite(numericRate) || numericRate <= 0) {
      showToast('أدخل سعر صرف صحيحًا أكبر من صفر.', 'warning');
      return;
    }

    setSaving(true);
    try {
      await api.put('/api/currencies/rates', {
        fromCurrency,
        toCurrency,
        rate: numericRate,
        rateDate,
        source: source.trim() || 'إدخال يدوي',
      });
      showToast('تم حفظ سعر الصرف وتثبيته للتعاملات الجديدة.', 'success');
      setRate('');
      await loadRates(fromCurrency);
    } catch (error) {
      showToast(getApiErrorMessage(error, 'تعذر حفظ سعر الصرف.'), 'danger');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div style={{ display: 'grid', gap: '20px', gridTemplateColumns: 'minmax(320px, .8fr) minmax(420px, 1.2fr)' }}>
      <form className="card" onSubmit={handleSubmit} style={{ display: 'grid', gap: '14px', alignContent: 'start' }}>
        <div>
          <h3 style={{ margin: 0 }}>إضافة أو تحديث سعر صرف</h3>
          <p style={{ color: 'var(--text-secondary)', fontSize: '12px', marginBottom: 0 }}>
            الأسعار عالمية ويستخدمها النظام في الحجوزات الجديدة فقط؛ الفواتير القديمة تحتفظ بلقطة سعرها.
          </p>
        </div>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
          <div className="form-group" style={{ margin: 0 }}>
            <label className="form-label">من</label>
            <select className="input-field" value={fromCurrency} onChange={event => setFromCurrency(event.target.value)}>
              {currencies.map(item => <option key={item.currencyCode} value={item.currencyCode}>{item.currencyName} ({item.currencyCode})</option>)}
            </select>
          </div>
          <div className="form-group" style={{ margin: 0 }}>
            <label className="form-label">إلى</label>
            <select className="input-field" value={toCurrency} onChange={event => setToCurrency(event.target.value)}>
              {currencies.map(item => <option key={item.currencyCode} value={item.currencyCode}>{item.currencyName} ({item.currencyCode})</option>)}
            </select>
          </div>
        </div>
        <div className="form-group" style={{ margin: 0 }}>
          <label className="form-label">سعر التحويل</label>
          <input className="input-field" type="number" min="0.000001" step="0.000001" required value={rate} onChange={event => setRate(event.target.value)} placeholder={`1 ${fromCurrency} = ؟ ${toCurrency}`} />
        </div>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
          <div className="form-group" style={{ margin: 0 }}>
            <label className="form-label">تاريخ السعر</label>
            <input className="input-field" type="date" required max={new Date().toISOString().slice(0, 10)} value={rateDate} onChange={event => setRateDate(event.target.value)} />
          </div>
          <div className="form-group" style={{ margin: 0 }}>
            <label className="form-label">المصدر</label>
            <input className="input-field" maxLength={50} value={source} onChange={event => setSource(event.target.value)} />
          </div>
        </div>
        <button className="btn-primary" type="submit" disabled={saving}>
          <Save size={16} /> {saving ? 'جاري الحفظ…' : 'حفظ سعر الصرف'}
        </button>
      </form>

      <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
        <div style={{ padding: '16px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderBottom: '1px solid var(--border-main)' }}>
          <strong>الأسعار المسجلة من {fromCurrency}</strong>
          <button className="btn-secondary" onClick={() => void loadRates(fromCurrency)} disabled={loading}>
            <RefreshCw size={14} /> تحديث
          </button>
        </div>
        <div className="table-container">
          <table className="data-table">
            <thead><tr><th>التحويل</th><th>السعر</th><th>التاريخ</th><th>المصدر</th></tr></thead>
            <tbody>
              {rates.map(item => (
                <tr key={item.exchangeRateID}>
                  <td>{item.fromCurrency} → {item.toCurrency}</td>
                  <td><strong>{Number(item.rate).toLocaleString('en-US', { maximumFractionDigits: 6 })}</strong></td>
                  <td>{new Date(item.rateDate).toLocaleDateString('ar-LY')}</td>
                  <td>{item.source}</td>
                </tr>
              ))}
              {!loading && rates.length === 0 && <tr><td colSpan={4} style={{ textAlign: 'center', padding: '28px' }}>لا توجد أسعار مسجلة لهذه العملة.</td></tr>}
              {loading && <tr><td colSpan={4} style={{ textAlign: 'center', padding: '28px' }}>جاري التحميل…</td></tr>}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
