import { useCallback, useEffect, useState } from 'react';
import { FileCheck2 } from 'lucide-react';
import { useToast } from '../../../app/providers/ToastContext';
import { api } from '../../../services/api';

interface ExhibitionSummary {
  exhibitionID: number;
  name: string;
}

interface ReservationSummary {
  reservationID: number;
  exhibitorName: string;
  boothNumber: string;
  exhibitionName: string;
  totalAmount: number;
  currencyCode: string;
  status: string;
  reservationDate: string;
}

interface PagedResult<T> {
  items: T[];
}

export const ReservationsPage = () => {
  const { showToast } = useToast();
  const [exhibitions, setExhibitions] = useState<ExhibitionSummary[]>([]);
  const [selectedExhibitionID, setSelectedExhibitionID] = useState('');
  const [reservations, setReservations] = useState<ReservationSummary[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const loadExhibitions = async () => {
      try {
        const response = await api.get<PagedResult<ExhibitionSummary>>('/api/exhibitions?page=1&pageSize=100');
        const items = response.data.items ?? [];
        setExhibitions(items);
        setSelectedExhibitionID(items[0]?.exhibitionID.toString() ?? '');
      } catch {
        setError('تعذر تحميل قائمة المعارض.');
        setIsLoading(false);
      }
    };
    void loadExhibitions();
  }, []);

  const loadReservations = useCallback(async () => {
    await Promise.resolve();
    if (!selectedExhibitionID) {
      setReservations([]);
      setIsLoading(false);
      return;
    }

    setIsLoading(true);
    setError('');
    try {
      const response = await api.get<PagedResult<ReservationSummary>>(
        `/api/reservations/exhibition/${selectedExhibitionID}?page=1&pageSize=100`,
      );
      setReservations(response.data.items ?? []);
    } catch {
      setError('تعذر تحميل حجوزات المعرض من الخادم.');
    } finally {
      setIsLoading(false);
    }
  }, [selectedExhibitionID]);

  useEffect(() => {
    // إعادة التحميل مطلوبة عند تغيير المعرض المختار.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void loadReservations();
  }, [loadReservations]);

  const changeStatus = async (reservationID: number, action: 'approve' | 'cancel') => {
    try {
      await api.patch(`/api/reservations/${reservationID}/${action}`);
      showToast(action === 'approve' ? 'تم اعتماد الحجز في الخادم.' : 'تم إلغاء الحجز في الخادم.', 'success');
      await loadReservations();
    } catch {
      showToast('تعذر تحديث الحجز. تحقق من حالته وصلاحياتك.', 'danger');
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
          <FileCheck2 size={24} style={{ color: 'var(--primary)' }} />
          <h1 style={{ fontSize: '24px', fontWeight: 800 }}>طلبات الحجز وعقود الأجنحة</h1>
        </div>
        <p style={{ color: 'var(--text-secondary)', fontSize: '13px' }}>الاعتماد والإلغاء يُنفذان مباشرة في الخادم.</p>
      </div>

      <div className="card">
        <label className="form-label" htmlFor="reservation-exhibition">المعرض</label>
        <select
          id="reservation-exhibition"
          className="input-field"
          value={selectedExhibitionID}
          onChange={event => setSelectedExhibitionID(event.target.value)}
        >
          {exhibitions.length === 0 && <option value="">لا توجد معارض</option>}
          {exhibitions.map(exhibition => (
            <option key={exhibition.exhibitionID} value={exhibition.exhibitionID}>{exhibition.name}</option>
          ))}
        </select>
      </div>

      {error && <div role="alert" style={{ padding: '14px', background: 'var(--danger-bg)', color: 'var(--danger-text)', borderRadius: 'var(--radius-lg)' }}>{error}</div>}

      <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
        <div className="table-container" style={{ border: 0 }}>
          <table className="data-table">
            <thead>
              <tr><th>رقم الطلب</th><th>العارض</th><th>الجناح</th><th>التاريخ</th><th>القيمة</th><th>الحالة</th><th>الإجراءات</th></tr>
            </thead>
            <tbody>
              {reservations.map(reservation => (
                <tr key={reservation.reservationID}>
                  <td>#{reservation.reservationID}</td>
                  <td>{reservation.exhibitorName}</td>
                  <td>{reservation.boothNumber}</td>
                  <td>{new Date(reservation.reservationDate).toLocaleDateString('ar-LY')}</td>
                  <td>{reservation.totalAmount.toLocaleString()} {reservation.currencyCode}</td>
                  <td><span className="badge badge-info">{reservation.status}</span></td>
                  <td>
                    {reservation.status.toLowerCase() === 'pending' ? (
                      <div style={{ display: 'flex', gap: '8px' }}>
                        <button className="btn-primary" onClick={() => void changeStatus(reservation.reservationID, 'approve')}>اعتماد</button>
                        <button className="btn-secondary" onClick={() => void changeStatus(reservation.reservationID, 'cancel')}>إلغاء</button>
                      </div>
                    ) : '—'}
                  </td>
                </tr>
              ))}
              {!isLoading && reservations.length === 0 && <tr><td colSpan={7} style={{ textAlign: 'center' }}>لا توجد حجوزات لهذا المعرض.</td></tr>}
              {isLoading && <tr><td colSpan={7} style={{ textAlign: 'center' }}>جاري التحميل…</td></tr>}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
