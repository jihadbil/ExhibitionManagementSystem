import React from 'react';
import { NavLink } from 'react-router-dom';
import { MapPin, Calendar, ExternalLink } from 'lucide-react';
import { UpcomingExhibitionItem } from '../types/dashboard.types';

interface UpcomingExhibitionsTableProps {
  exhibitions: UpcomingExhibitionItem[];
}

export const UpcomingExhibitionsTable: React.FC<UpcomingExhibitionsTableProps> = ({ exhibitions }) => {
  const statusPresentation: Record<string, { label: string; className: string }> = {
    Planning: { label: 'قيد التخطيط', className: 'badge-warning' },
    Open: { label: 'مفتوح', className: 'badge-success' },
    Closed: { label: 'مغلق', className: 'badge-info' },
    Cancelled: { label: 'ملغي', className: 'badge-danger' },
  };
  const formatDate = (date: string) => new Date(date).toLocaleDateString('ar-LY');
  const getGradientByType = (type: string) => {
    switch (type.toLowerCase()) {
      case 'technical': return 'var(--tech-gradient)';
      case 'medical': return 'var(--medical-gradient)';
      case 'industrial': return 'var(--industrial-gradient)';
      case 'commercial': return 'var(--commercial-gradient)';
      case 'educational': return 'var(--educational-gradient)';
      case 'automotive': return 'var(--automotive-gradient)';
      default: return 'var(--primary-gradient)';
    }
  };

  return (
    <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
      <div
        style={{
          padding: '20px 24px',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          borderBottom: '1px solid var(--border-main)'
        }}
      >
        <div>
          <h3 style={{ fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)' }}>
            المعارض القادمة والنشطة
          </h3>
          <p style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>متابعة مواقع المعارض وتواريخ الافتتاح</p>
        </div>

        <NavLink
          to="/exhibitions"
          className="btn-secondary"
          style={{ padding: '6px 14px', fontSize: '12px' }}
        >
          <span>عرض جميع المعارض</span>
          <ExternalLink size={14} />
        </NavLink>
      </div>

      <div className="table-container" style={{ border: 'none', borderRadius: 0 }}>
        <table className="data-table">
          <thead>
            <tr>
              <th style={{ width: '45%' }}>المعرض والتخصص</th>
              <th style={{ width: '30%' }}>الموقع والقاعة</th>
              <th style={{ width: '12%' }}>الحالة</th>
              <th style={{ width: '13%', textAlign: 'center' }}>الإجراءات</th>
            </tr>
          </thead>
          <tbody>
            {exhibitions.map((ex) => {
              const gradient = getGradientByType(ex.type);
              return (
                <tr key={ex.exhibitionID}>
                  {/* Name and Type */}
                  <td>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                      <div
                        style={{
                          width: '4px',
                          height: '36px',
                          borderRadius: '2px',
                          background: gradient,
                          flexShrink: 0
                        }}
                      />
                      <div>
                        <div style={{ fontWeight: 700, color: 'var(--text-primary)', fontSize: '14px' }}>
                          {ex.name}
                        </div>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '12px', color: 'var(--text-muted)', marginTop: '2px' }}>
                          <Calendar size={13} />
                          <span>{formatDate(ex.startDate)} إلى {formatDate(ex.endDate)}</span>
                        </div>
                      </div>
                    </div>
                  </td>

                  {/* Location */}
                  <td>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '6px', color: 'var(--text-secondary)' }}>
                      <MapPin size={15} style={{ color: 'var(--primary)', flexShrink: 0 }} />
                      <span style={{ fontSize: '13px' }}>{ex.venueName || 'غير محدد'}</span>
                    </div>
                  </td>

                  {/* Status Badge */}
                  <td>
                    <span
                      className={`badge ${statusPresentation[ex.status]?.className ?? 'badge-info'}`}
                    >
                      {statusPresentation[ex.status]?.label ?? ex.status}
                    </span>
                  </td>

                  {/* Actions */}
                  <td style={{ textAlign: 'center' }}>
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px' }}>
                      <NavLink
                        to={`/exhibitions/${ex.exhibitionID}/booths`}
                        className="btn-icon"
                        title="إدارة أجنحة وتسعير المعرض"
                      >
                        <ExternalLink size={16} />
                      </NavLink>
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
};
