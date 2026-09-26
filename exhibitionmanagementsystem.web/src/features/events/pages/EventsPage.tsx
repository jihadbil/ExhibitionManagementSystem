import React, { useState } from 'react';
import { Calendar, Clock, MapPin, Users, Plus } from 'lucide-react';
import { useToast } from '../../../app/providers/ToastContext';

interface EventItem {
  id: number;
  title: string;
  exhibitionName: string;
  speaker: string;
  hallName: string;
  date: string;
  time: string;
  category: string;
  attendeesCount: number;
}

const mockEvents: EventItem[] = [
  { id: 1, title: 'الجلسة الافتتاحية: مستقبل الحوسبة السحابية والذكاء الاصطناعي', exhibitionName: 'معرض طرابلس الدولي للتقنية 2026', speaker: 'د. طارق المنصوري', hallName: 'مسرح المؤتمرات الرئيسي', date: '2026-10-15', time: '10:00 ص - 11:30 ص', category: 'Keynote', attendeesCount: 350 },
  { id: 2, title: 'ورشة عمل: بناء بنية تحتية رقمية آمنة للمؤسسات', exhibitionName: 'معرض طرابلس الدولي للتقنية 2026', speaker: 'م. أحمد الفرجاني', hallName: 'قاعة الورش (Room B)', date: '2026-10-16', time: '02:00 م - 04:00 م', category: 'Workshop', attendeesCount: 120 },
  { id: 3, title: 'حلقة نقاش: التحول الرقمي في قطاع الرعاية الصحية والأدوية', exhibitionName: 'المعرض الطبي الدولي (MedExpo)', speaker: 'د. عادل الورفلي', hallName: 'القاعة الطبية الكبرى', date: '2026-11-03', time: '11:00 ص - 12:30 م', category: 'Panel', attendeesCount: 200 }
];

export const EventsPage: React.FC = () => {
  const { showToast } = useToast();
  const [events, setEvents] = useState<EventItem[]>(mockEvents);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '28px' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <Calendar size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)' }}>
              جدول الفعاليات والندوات المصاحبة
            </h1>
          </div>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '2px' }}>
            مواعيد الجلسات الافتتاحية وورش العمل المصاحبة للمعارض والقاعات
          </p>
        </div>

        <button
          onClick={() => showToast('نموذج إضافة فعالية جديدة جاهز', 'info')}
          className="btn-primary"
          style={{ height: '42px', padding: '0 20px' }}
        >
          <Plus size={16} />
          <span>إضافة فعالية جديدة</span>
        </button>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
        {events.map(event => (
          <div
            key={event.id}
            className="card"
            style={{
              padding: '24px',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              gap: '24px',
              borderRight: '4px solid var(--primary)'
            }}
          >
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                <span className="badge badge-info">{event.category}</span>
                <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>{event.exhibitionName}</span>
              </div>
              <h3 style={{ fontSize: '17px', fontWeight: 800, color: 'var(--text-primary)' }}>{event.title}</h3>
              <div style={{ display: 'flex', alignItems: 'center', gap: '18px', fontSize: '13px', color: 'var(--text-secondary)' }}>
                <span style={{ fontWeight: 600 }}>المتحدث: {event.speaker}</span>
                <span>•</span>
                <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                  <MapPin size={15} style={{ color: 'var(--primary)' }} />
                  <span>{event.hallName}</span>
                </div>
              </div>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end', gap: '6px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '13px', fontWeight: 700, color: 'var(--primary)' }}>
                <Clock size={16} />
                <span>{event.time}</span>
              </div>
              <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>{event.date}</span>
              <span className="badge badge-standard" style={{ marginTop: '4px' }}>
                <Users size={12} />
                {event.attendeesCount} مسجل
              </span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
