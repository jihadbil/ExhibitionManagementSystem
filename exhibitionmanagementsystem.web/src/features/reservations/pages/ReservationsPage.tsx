import React, { useState } from 'react';
import { FileCheck2, Search, Store, Building, CheckCircle2, XCircle, Clock } from 'lucide-react';
import { useToast } from '../../../app/providers/ToastContext';

interface ReservationItem {
  id: number;
  reservationCode: string;
  companyName: string;
  boothNumber: string;
  exhibitionName: string;
  requestDate: string;
  status: 'Approved' | 'Pending' | 'Rejected';
  totalPrice: number;
}

const mockReservations: ReservationItem[] = [
  { id: 1, reservationCode: 'RES-2026-001', companyName: 'الشركة الليبية للاتصالات', boothNumber: 'VIP-01', exhibitionName: 'معرض طرابلس للتقنية 2026', requestDate: '2026-09-12', status: 'Approved', totalPrice: 12000 },
  { id: 2, reservationCode: 'RES-2026-002', companyName: 'شركة مسار للحلول الرقمية', boothNumber: 'A-102', exhibitionName: 'معرض طرابلس للتقنية 2026', requestDate: '2026-09-15', status: 'Approved', totalPrice: 5600 },
  { id: 3, reservationCode: 'RES-2026-003', companyName: 'مؤسسة التقنية والشبكات', boothNumber: 'B-201', exhibitionName: 'معرض طرابلس للتقنية 2026', requestDate: '2026-09-18', status: 'Pending', totalPrice: 8000 },
  { id: 4, reservationCode: 'RES-2026-004', companyName: 'شركة التقدم الهندسي', boothNumber: 'B-202', exhibitionName: 'معرض طرابلس للتقنية 2026', requestDate: '2026-09-19', status: 'Pending', totalPrice: 3600 },
];

export const ReservationsPage: React.FC = () => {
  const { showToast } = useToast();
  const [reservations, setReservations] = useState<ReservationItem[]>(mockReservations);
  const [filter, setFilter] = useState<'All' | 'Approved' | 'Pending'>('All');

  const handleApprove = (id: number) => {
    setReservations(prev => prev.map(r => r.id === id ? { ...r, status: 'Approved' } : r));
    showToast('تم اعتماد طلب الحجز بنجاح وإصدار العقد المبدئي!', 'success');
  };

  const handleReject = (id: number) => {
    setReservations(prev => prev.map(r => r.id === id ? { ...r, status: 'Rejected' } : r));
    showToast('تم رفض طلب الحجز وإشعار العارض', 'info');
  };

  const filtered = reservations.filter(r => filter === 'All' || r.status === filter);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '28px' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <FileCheck2 size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)' }}>
              طلبات الحجز وعقود الأجنحة
            </h1>
          </div>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '2px' }}>
            مراجعة واعتماد طلبات حجز أجنحة المعارض وتوليد العقود الرسمية
          </p>
        </div>
      </div>

      <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
        <div className="table-container" style={{ border: 'none', borderRadius: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>رقم الطلب</th>
                <th>الشركة المتقدمة</th>
                <th>الجناح المطلوب</th>
                <th>المعرض</th>
                <th>تاريخ التقديم</th>
                <th>قيمة العقد</th>
                <th>الحالة</th>
                <th style={{ textAlign: 'center' }}>الإجراءات</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map(res => (
                <tr key={res.id}>
                  <td>
                    <span style={{ fontWeight: 800, color: 'var(--text-primary)' }}>{res.reservationCode}</span>
                  </td>
                  <td>
                    <span style={{ fontWeight: 600, color: 'var(--text-primary)' }}>{res.companyName}</span>
                  </td>
                  <td>
                    <span className="badge badge-featured">{res.boothNumber}</span>
                  </td>
                  <td>
                    <span style={{ color: 'var(--text-secondary)' }}>{res.exhibitionName}</span>
                  </td>
                  <td>
                    <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>{res.requestDate}</span>
                  </td>
                  <td>
                    <span style={{ fontWeight: 800, color: 'var(--text-primary)' }}>{res.totalPrice.toLocaleString()} د.ل</span>
                  </td>
                  <td>
                    <span className={`badge ${res.status === 'Approved' ? 'badge-success' : res.status === 'Pending' ? 'badge-warning' : 'badge-danger'}`}>
                      {res.status === 'Approved' ? 'معتمد' : res.status === 'Pending' ? 'قيد المراجعة' : 'مرفوض'}
                    </span>
                  </td>
                  <td style={{ textAlign: 'center' }}>
                    {res.status === 'Pending' ? (
                      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px' }}>
                        <button
                          onClick={() => handleApprove(res.id)}
                          className="btn-primary"
                          style={{ padding: '6px 12px', fontSize: '12px' }}
                        >
                          اعتماد
                        </button>
                        <button
                          onClick={() => handleReject(res.id)}
                          className="btn-secondary"
                          style={{ padding: '6px 12px', fontSize: '12px', color: 'var(--danger)' }}
                        >
                          رفض
                        </button>
                      </div>
                    ) : (
                      <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>—</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
