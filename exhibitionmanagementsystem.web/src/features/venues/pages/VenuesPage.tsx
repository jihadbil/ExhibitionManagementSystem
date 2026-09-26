import React, { useState } from 'react';
import { MapPin, Building, Plus, Grid, Maximize2 } from 'lucide-react';
import { useToast } from '../../../app/providers/ToastContext';

interface Hall {
  id: number;
  name: string;
  floor: string;
  widthMeters: number;
  lengthMeters: number;
  capacity: number;
  totalBooths: number;
}

interface Venue {
  id: number;
  name: string;
  address: string;
  city: string;
  totalHalls: number;
  halls: Hall[];
}

const mockVenues: Venue[] = [
  {
    id: 1,
    name: 'مركز طرابلس للمعارض والمؤتمرات الدولية',
    address: 'شارع عمر المختار — وسط المدينة',
    city: 'طرابلس',
    totalHalls: 3,
    halls: [
      { id: 101, name: 'القاعة الكبرى (Hall 1)', floor: 'الطابق الأرضي', widthMeters: 50, lengthMeters: 40, capacity: 3500, totalBooths: 60 },
      { id: 102, name: 'صالة الابتكار والتقنية (Hall 2)', floor: 'الطابق الأول', widthMeters: 40, lengthMeters: 30, capacity: 1800, totalBooths: 40 },
      { id: 103, name: 'صالة المؤتمرات والندوات', floor: 'الطابق الثاني', widthMeters: 30, lengthMeters: 25, capacity: 800, totalBooths: 15 }
    ]
  },
  {
    id: 2,
    name: 'مجمع قاعات بنغازي الكبرى للمعارض',
    address: 'طريق المطار الدولي',
    city: 'بنغازي',
    totalHalls: 2,
    halls: [
      { id: 201, name: 'الصالة الرئيسية A', floor: 'الطابق الأرضي', widthMeters: 45, lengthMeters: 35, capacity: 2500, totalBooths: 50 },
      { id: 202, name: 'الصالة B (المعارض الطبية)', floor: 'الطابق الأرضي', widthMeters: 35, lengthMeters: 30, capacity: 1500, totalBooths: 35 }
    ]
  }
];

export const VenuesPage: React.FC = () => {
  const { showToast } = useToast();
  const [venues, setVenues] = useState<Venue[]>(mockVenues);
  const [selectedVenueId, setSelectedVenueId] = useState<number>(1);

  const selectedVenue = venues.find(v => v.id === selectedVenueId) || venues[0];

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '28px' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <MapPin size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)' }}>
              المواقع ومراكز المعارض والقاعات
            </h1>
          </div>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '2px' }}>
            إدارة المنشآت وصالات العرض وأبعاد القاعات وشبكات التخطيط (Master-Detail)
          </p>
        </div>

        <button
          onClick={() => showToast('نموذج إضافة مركز معارض جديد', 'info')}
          className="btn-primary"
          style={{ height: '42px', padding: '0 20px' }}
        >
          <Plus size={16} />
          <span>إضافة مركز معارض</span>
        </button>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '320px 1fr', gap: '24px', alignItems: 'start' }}>
        {/* Venues List (Master) */}
        <div className="card" style={{ padding: '16px', display: 'flex', flexDirection: 'column', gap: '10px' }}>
          <div style={{ fontSize: '14px', fontWeight: 700, color: 'var(--text-primary)', marginBottom: '4px' }}>
            مراكز المعارض المعتمدة:
          </div>

          {venues.map(v => (
            <div
              key={v.id}
              onClick={() => setSelectedVenueId(v.id)}
              style={{
                padding: '14px',
                borderRadius: 'var(--radius-lg)',
                border: v.id === selectedVenueId ? '2px solid var(--primary)' : '1px solid var(--border-main)',
                backgroundColor: v.id === selectedVenueId ? 'var(--primary-light)' : '#ffffff',
                cursor: 'pointer',
                transition: 'all 0.2s ease'
              }}
            >
              <h4 style={{ fontSize: '14px', fontWeight: 800, color: 'var(--text-primary)' }}>{v.name}</h4>
              <div style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '4px' }}>{v.city} — {v.address}</div>
              <div style={{ fontSize: '11px', color: 'var(--primary)', fontWeight: 600, marginTop: '6px' }}>
                {v.totalHalls} قاعات مسجلة
              </div>
            </div>
          ))}
        </div>

        {/* Halls Grid for Selected Venue (Detail) */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <h3 style={{ fontSize: '18px', fontWeight: 800, color: 'var(--text-primary)' }}>
              قاعات {selectedVenue?.name}
            </h3>
            <button
              onClick={() => showToast('إضافة قاعة جديدة', 'info')}
              className="btn-secondary"
              style={{ padding: '6px 14px', fontSize: '12px' }}
            >
              <Plus size={14} />
              <span>إضافة قاعة داخل المركز</span>
            </button>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))', gap: '16px' }}>
            {selectedVenue?.halls.map(hall => (
              <div key={hall.id} className="card" style={{ padding: '20px', display: 'flex', flexDirection: 'column', gap: '12px' }}>
                <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between' }}>
                  <div>
                    <h4 style={{ fontSize: '16px', fontWeight: 800, color: 'var(--text-primary)' }}>{hall.name}</h4>
                    <span style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>{hall.floor}</span>
                  </div>
                  <span className="badge badge-featured">
                    <Grid size={12} />
                    {hall.totalBooths} أجنحة
                  </span>
                </div>

                <div style={{ backgroundColor: 'var(--bg-secondary)', padding: '12px', borderRadius: 'var(--radius-md)', display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px', fontSize: '12px' }}>
                  <div>
                    <span style={{ color: 'var(--text-muted)' }}>الأبعاد:</span>
                    <div style={{ fontWeight: 700, color: 'var(--text-primary)' }}>{hall.widthMeters}م × {hall.lengthMeters}م</div>
                  </div>
                  <div>
                    <span style={{ color: 'var(--text-muted)' }}>المساحة الإجمالية:</span>
                    <div style={{ fontWeight: 700, color: 'var(--text-primary)' }}>{hall.widthMeters * hall.lengthMeters} م²</div>
                  </div>
                  <div>
                    <span style={{ color: 'var(--text-muted)' }}>الطاقة الاستيعابية:</span>
                    <div style={{ fontWeight: 700, color: 'var(--text-primary)' }}>{hall.capacity.toLocaleString()} شخص</div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};
