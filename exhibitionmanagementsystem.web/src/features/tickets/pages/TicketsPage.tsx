import React, { useState } from 'react';
import { Ticket, Search, Plus, QrCode, CheckCircle2, User, Phone, Mail, Printer } from 'lucide-react';
import { QRCodeSVG } from 'qrcode.react';
import { useToast } from '../../../app/providers/ToastContext';

interface TicketItem {
  id: string;
  ticketNumber: string;
  visitorName: string;
  visitorEmail: string;
  visitorPhone: string;
  exhibitionName: string;
  ticketType: 'General' | 'VIP' | 'Student';
  issueDate: string;
  isCheckedIn: boolean;
}

const mockTickets: TicketItem[] = [
  { id: '1', ticketNumber: 'TKT-2026-VIP-9941', visitorName: 'د. طارق مصطفى المنصوري', visitorEmail: 'tarek@example.ly', visitorPhone: '+218 91 1122334', exhibitionName: 'معرض طرابلس للتقنية 2026', ticketType: 'VIP', issueDate: '2026-09-20', isCheckedIn: true },
  { id: '2', ticketNumber: 'TKT-2026-8812', visitorName: 'م. أحمد الهادي الفرجاني', visitorEmail: 'ahmed@example.ly', visitorPhone: '+218 92 4455667', exhibitionName: 'معرض طرابلس للتقنية 2026', ticketType: 'General', issueDate: '2026-09-21', isCheckedIn: true },
  { id: '3', ticketNumber: 'TKT-2026-7731', visitorName: 'د. عادل محمود الورفلي', visitorEmail: 'adel@example.ly', visitorPhone: '+218 91 7788990', exhibitionName: 'معرض طرابلس للتقنية 2026', ticketType: 'General', issueDate: '2026-09-22', isCheckedIn: false },
];

export const TicketsPage: React.FC = () => {
  const { showToast } = useToast();
  const [tickets, setTickets] = useState<TicketItem[]>(mockTickets);
  const [search, setSearch] = useState('');
  const [activeTicket, setActiveTicket] = useState<TicketItem | null>(null);

  const filtered = tickets.filter(t =>
    t.ticketNumber.toLowerCase().includes(search.toLowerCase()) ||
    t.visitorName.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '28px' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <Ticket size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)' }}>
              إدارة التذاكر والزوار
            </h1>
          </div>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '2px' }}>
            إصدار التذاكر الإلكترونية، توليد رموز QR، ومتابعة سجلات التحقق
          </p>
        </div>

        <button
          onClick={() => showToast('نموذج إصدار تذكرة فورية', 'info')}
          className="btn-primary"
          style={{ height: '42px', padding: '0 20px' }}
        >
          <Plus size={16} />
          <span>إصدار تذكرة جديدة</span>
        </button>
      </div>

      <div className="card" style={{ padding: '16px 20px', display: 'flex', alignItems: 'center', gap: '16px' }}>
        <div style={{ position: 'relative', width: '380px', maxWidth: '100%' }}>
          <Search size={18} style={{ position: 'absolute', right: '14px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }} />
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="بحث برقم التذكرة أو اسم الزائر..."
            className="input-field"
            style={{ paddingRight: '42px', height: '42px' }}
          />
        </div>
      </div>

      <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
        <div className="table-container" style={{ border: 'none', borderRadius: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th style={{ width: '18%' }}>رقم التذكرة</th>
                <th style={{ width: '22%' }}>اسم الزائر</th>
                <th style={{ width: '20%' }}>المعرض</th>
                <th style={{ width: '12%' }}>الفئة</th>
                <th style={{ width: '14%' }}>تاريخ الإصدار</th>
                <th style={{ width: '14%' }}>حالة الحضور</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map(ticket => (
                <tr key={ticket.id} onClick={() => setActiveTicket(ticket)} style={{ cursor: 'pointer' }}>
                  <td>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                      <QrCode size={16} style={{ color: 'var(--primary)' }} />
                      <span style={{ fontWeight: 800, color: 'var(--text-primary)' }}>{ticket.ticketNumber}</span>
                    </div>
                  </td>
                  <td>
                    <div style={{ fontWeight: 700, color: 'var(--text-primary)' }}>{ticket.visitorName}</div>
                    <div style={{ fontSize: '11px', color: 'var(--text-muted)' }}>{ticket.visitorEmail}</div>
                  </td>
                  <td>
                    <span style={{ color: 'var(--text-secondary)', fontSize: '13px' }}>{ticket.exhibitionName}</span>
                  </td>
                  <td>
                    <span className={`badge ${ticket.ticketType === 'VIP' ? 'badge-vip' : 'badge-standard'}`}>
                      {ticket.ticketType}
                    </span>
                  </td>
                  <td>
                    <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>{ticket.issueDate}</span>
                  </td>
                  <td>
                    <span className={`badge ${ticket.isCheckedIn ? 'badge-success' : 'badge-warning'}`}>
                      {ticket.isCheckedIn ? 'تم الدخول ✓' : 'لم يدخل بعد'}
                    </span>
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
