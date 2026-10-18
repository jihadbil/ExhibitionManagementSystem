import type { CSSProperties } from 'react';
import { Building2 } from 'lucide-react';
import { QRCodeSVG } from 'qrcode.react';

export interface BadgeCardData {
  fullName: string;
  companyName?: string;
  jobTitle?: string;
  qrCode: string;
  barcode?: string;
  exhibitionName: string;
  venueAndHallName?: string;
  datesText?: string;
  boothNumber?: string;
}

const code39Patterns: Record<string, string> = {
  '0': 'nnnwwnwnn', '1': 'wnnwnnnnw', '2': 'nnwwnnnnw', '3': 'wnwwnnnnn',
  '4': 'nnnwwnnnw', '5': 'wnnwwnnnn', '6': 'nnwwwnnnn', '7': 'nnnwnnwnw',
  '8': 'wnnwnnwnn', '9': 'nnwwnnwnn', A: 'wnnnnwnnw', B: 'nnwnnwnnw',
  C: 'wnwnnwnnn', D: 'nnnnwwnnw', E: 'wnnnwwnnn', F: 'nnwnwwnnn',
  G: 'nnnnnwwnw', H: 'wnnnnwwnn', I: 'nnwnnwwnn', J: 'nnnnwwwnn',
  K: 'wnnnnnnww', L: 'nnwnnnnww', M: 'wnwnnnnwn', N: 'nnnnwnnww',
  O: 'wnnnwnnwn', P: 'nnwnwnnwn', Q: 'nnnnnnwww', R: 'wnnnnnwwn',
  S: 'nnwnnnwwn', T: 'nnnnwnwwn', U: 'wwnnnnnnw', V: 'nwwnnnnnw',
  W: 'wwwnnnnnn', X: 'nwnnwnnnw', Y: 'wwnnwnnnn', Z: 'nwwnwnnnn',
  '-': 'nwnnnnwnw', '.': 'wwnnnnwnn', ' ': 'nwwnnnwnn', '$': 'nwnwnwnnn',
  '/': 'nwnwnnnwn', '+': 'nwnnnwnwn', '%': 'nnnwnwnwn', '*': 'nwnnwnwnn',
};

const LinearBarcode = ({ value }: { value: string }) => {
  const normalized = value.toUpperCase().replace(/[^0-9A-Z. $/+%-]/g, '-').slice(0, 32);
  const encoded = `*${normalized}*`;
  const bars: Array<{ x: number; width: number }> = [];
  let x = 10;

  for (const character of encoded) {
    const pattern = code39Patterns[character];
    pattern.split('').forEach((module, index) => {
      const width = module === 'w' ? 5 : 2;
      if (index % 2 === 0) bars.push({ x, width });
      x += width;
    });
    x += 2;
  }

  const totalWidth = x + 8;
  return (
    <svg
      role="img"
      aria-label={`Barcode ${normalized}`}
      viewBox={`0 0 ${totalWidth} 62`}
      style={{ display: 'block', width: '100%', maxWidth: '220px', height: '54px', background: '#ffffff' }}
    >
      <rect width={totalWidth} height="62" fill="#ffffff" />
      {bars.map((bar, index) => (
        <rect key={`${bar.x}-${index}`} x={bar.x} y="2" width={bar.width} height="42" fill="#000000" />
      ))}
      <text x={totalWidth / 2} y="57" textAnchor="middle" fontFamily="monospace" fontSize="10" fill="#111827">
        {normalized}
      </text>
    </svg>
  );
};

export interface BadgeCardDesign {
  widthMm: number;
  heightMm: number;
  orientation: number | 'Vertical' | 'Horizontal';
  headerBgColor: string;
  headerTextColor: string;
  accentColor: string;
  categoryBadgeText: string;
  showLogo: boolean;
  logoUrl?: string;
  showQRCode: boolean;
  showBarcode: boolean;
  showCompanyName: boolean;
  showJobTitle: boolean;
  showHallName: boolean;
  showDates: boolean;
  footerText?: string;
}

export interface BadgePrintPayload extends BadgeCardData, BadgeCardDesign {
  participantTypeTitle: string;
}

interface BadgeCardProps {
  data: BadgeCardData;
  design: BadgeCardDesign;
  className?: string;
  id?: string;
}

type BadgeStyle = CSSProperties & {
  '--badge-width': string;
  '--badge-height': string;
};

export const BadgeCard = ({ data, design, className = '', id }: BadgeCardProps) => {
  const horizontal = design.orientation === 2 || design.orientation === 'Horizontal';
  const location = [data.venueAndHallName, data.boothNumber].filter(Boolean).join(' | ');
  const style: BadgeStyle = {
    '--badge-width': `${design.widthMm}mm`,
    '--badge-height': `${design.heightMm}mm`,
    width: horizontal ? '440px' : '320px',
    minHeight: horizontal ? '280px' : '460px',
    backgroundColor: '#ffffff',
    borderRadius: '16px',
    overflow: 'hidden',
    boxShadow: 'var(--shadow-xl)',
    border: '1px solid #e2e8f0',
    display: 'flex',
    flexDirection: 'column',
    position: 'relative',
  };

  return (
    <div id={id} className={`badge-card ${className}`.trim()} style={style}>
      <div
        aria-hidden="true"
        style={{
          position: 'absolute',
          top: '8px',
          left: '50%',
          transform: 'translateX(-50%)',
          width: '36px',
          height: '7px',
          borderRadius: '4px',
          backgroundColor: 'rgba(0, 0, 0, 0.2)',
          zIndex: 20,
        }}
      />

      <div
        style={{
          backgroundColor: design.headerBgColor,
          padding: horizontal ? '18px 20px 14px' : '24px 18px 16px',
          textAlign: 'center',
          color: design.headerTextColor,
          position: 'relative',
        }}
      >
        {design.showLogo && (
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '6px', marginBottom: '4px' }}>
            {design.logoUrl ? (
              <img src={design.logoUrl} alt="" style={{ maxWidth: '110px', maxHeight: '34px', objectFit: 'contain' }} />
            ) : (
              <>
                <Building2 size={16} />
                <span style={{ fontSize: '12px', fontWeight: 800, letterSpacing: '0.5px' }}>ExpoManager System</span>
              </>
            )}
          </div>
        )}
        <h2 style={{ fontSize: horizontal ? '15px' : '14px', fontWeight: 700, margin: 0, opacity: 0.95 }}>
          {data.exhibitionName}
        </h2>
        {design.showDates && data.datesText && (
          <div style={{ fontSize: '10px', marginTop: '3px', opacity: 0.9 }}>{data.datesText}</div>
        )}
      </div>

      <div
        style={{
          backgroundColor: design.accentColor,
          color: '#ffffff',
          padding: '6px 12px',
          textAlign: 'center',
          fontSize: '11px',
          fontWeight: 800,
          letterSpacing: '1px',
        }}
      >
        {design.categoryBadgeText}
      </div>

      <div
        style={{
          padding: horizontal ? '18px 22px' : '24px 20px',
          display: horizontal ? 'grid' : 'flex',
          gridTemplateColumns: horizontal ? (design.showQRCode || design.showBarcode ? '1fr auto' : '1fr') : undefined,
          flexDirection: horizontal ? undefined : 'column',
          alignItems: 'center',
          gap: '14px',
          textAlign: horizontal ? 'right' : 'center',
          flex: 1,
          justifyContent: horizontal ? 'space-between' : 'center',
        }}
      >
        <div style={{ display: 'flex', flexDirection: 'column', gap: '4px', alignItems: horizontal ? 'flex-start' : 'center' }}>
          <h3 style={{ fontSize: horizontal ? '20px' : '18px', fontWeight: 800, color: '#0f172a', margin: 0 }}>
            {data.fullName}
          </h3>
          {design.showJobTitle && data.jobTitle && (
            <p style={{ fontSize: '13px', fontWeight: 600, color: '#64748b', margin: 0 }}>{data.jobTitle}</p>
          )}
          {design.showCompanyName && data.companyName && (
            <p style={{ fontSize: '12px', color: '#475569', margin: 0, maxWidth: horizontal ? '260px' : '280px', lineHeight: 1.3 }}>
              {data.companyName}
            </p>
          )}
          {design.showHallName && location && (
            <div
              style={{
                backgroundColor: '#f1f5f9',
                padding: '4px 12px',
                borderRadius: '14px',
                fontSize: '11px',
                fontWeight: 700,
                color: '#334155',
                alignSelf: horizontal ? 'flex-start' : 'center',
                marginTop: '6px',
              }}
            >
              {location}
            </div>
          )}
        </div>

        {(design.showQRCode || (design.showBarcode && data.barcode)) && (
          <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: '7px' }}>
            {design.showQRCode && (
              <div style={{ padding: '8px', backgroundColor: '#ffffff', border: '1px solid #e2e8f0', borderRadius: '10px', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                <QRCodeSVG value={data.qrCode} size={horizontal ? 82 : 78} level="M" />
              </div>
            )}
            {design.showBarcode && data.barcode && (
              <div style={{ width: horizontal ? '170px' : '230px', maxWidth: '100%', padding: '4px 6px', background: '#ffffff', border: '1px solid #e2e8f0', borderRadius: '7px' }}>
                <LinearBarcode value={data.barcode} />
              </div>
            )}
          </div>
        )}
      </div>

      {design.footerText && (
        <div style={{ padding: '8px 16px', borderTop: '1px dashed #cbd5e1', textAlign: 'center', fontSize: '10px', color: '#64748b', backgroundColor: '#f8fafc' }}>
          {design.footerText}
        </div>
      )}
    </div>
  );
};
