import React, { useState, useRef } from 'react';
import {
  Compass,
  Plus,
  ZoomIn,
  ZoomOut,
  Maximize2,
  Trash2,
  Grid,
  AlertTriangle,
  Save,
  Sparkles
} from 'lucide-react';
import { useToast } from '../../../app/providers/ToastContext';

interface VisualBooth {
  id: string;
  number: string;
  x: number;
  y: number;
  width: number;
  height: number;
  category: 'VIP' | 'Featured' | 'Corner' | 'Standard';
  status: 'Available' | 'Reserved' | 'PendingReview';
  price: number;
  shape: 'Rectangle' | 'LShape';
  companyName?: string;
  hasCollision?: boolean;
}

const initialCanvasBooths: VisualBooth[] = [
  { id: 'b1', number: 'A-101', x: 60, y: 60, width: 140, height: 100, category: 'VIP', status: 'Reserved', price: 4800, shape: 'Rectangle', companyName: 'الشركة الليبية للاتصالات' },
  { id: 'b2', number: 'A-102', x: 230, y: 60, width: 110, height: 100, category: 'Featured', status: 'Reserved', price: 2800, shape: 'Rectangle', companyName: 'مسار للحلول' },
  { id: 'b3', number: 'A-103', x: 370, y: 60, width: 100, height: 100, category: 'Standard', status: 'Available', price: 1800, shape: 'Rectangle' },
  { id: 'b4', number: 'A-104', x: 500, y: 60, width: 100, height: 100, category: 'Standard', status: 'Available', price: 1800, shape: 'Rectangle' },
  { id: 'b5', number: 'B-201', x: 60, y: 220, width: 120, height: 110, category: 'Corner', status: 'PendingReview', price: 3600, shape: 'Rectangle', companyName: 'مؤسسة التقنية العالمية' },
  { id: 'b6', number: 'B-202', x: 210, y: 220, width: 100, height: 100, category: 'Standard', status: 'Available', price: 1800, shape: 'Rectangle' },
  { id: 'b7', number: 'B-203', x: 340, y: 220, width: 100, height: 100, category: 'Standard', status: 'Available', price: 1800, shape: 'Rectangle' },
  { id: 'b8', number: 'VIP-01', x: 480, y: 220, width: 160, height: 120, category: 'VIP', status: 'Reserved', price: 6200, shape: 'Rectangle', companyName: 'المجموعة المصرفية' },
];

export const BoothDesignerPage: React.FC = () => {
  const { showToast } = useToast();
  const [booths, setBooths] = useState<VisualBooth[]>(initialCanvasBooths);
  const [selectedBoothId, setSelectedBoothId] = useState<string | null>('b1');
  const [zoomScale, setZoomScale] = useState(1.0);
  const [snapToGrid, setSnapToGrid] = useState(true);
  const [showGrid] = useState(true);

  // Dragging state
  const [isDragging, setIsDragging] = useState(false);
  const [dragOffset, setDragOffset] = useState({ x: 0, y: 0 });
  const canvasRef = useRef<HTMLDivElement>(null);

  const selectedBooth = booths.find(b => b.id === selectedBoothId) || null;

  // Collision detection between booths
  const checkCollisions = (currentBooths: VisualBooth[]): VisualBooth[] => {
    return currentBooths.map(b1 => {
      const collision = currentBooths.some(b2 => {
        if (b1.id === b2.id) return false;
        return (
          b1.x < b2.x + b2.width &&
          b1.x + b1.width > b2.x &&
          b1.y < b2.y + b2.height &&
          b1.y + b1.height > b2.y
        );
      });
      return { ...b1, hasCollision: collision };
    });
  };

  const handleMouseDown = (e: React.MouseEvent, booth: VisualBooth) => {
    e.stopPropagation();
    setSelectedBoothId(booth.id);
    setIsDragging(true);

    if (canvasRef.current) {
      const rect = canvasRef.current.getBoundingClientRect();
      const mouseX = (e.clientX - rect.left) / zoomScale;
      const mouseY = (e.clientY - rect.top) / zoomScale;
      setDragOffset({
        x: mouseX - booth.x,
        y: mouseY - booth.y
      });
    }
  };

  const handleMouseMove = (e: React.MouseEvent) => {
    if (!isDragging || !selectedBoothId || !canvasRef.current) return;

    const rect = canvasRef.current.getBoundingClientRect();
    const mouseX = (e.clientX - rect.left) / zoomScale;
    const mouseY = (e.clientY - rect.top) / zoomScale;

    let newX = mouseX - dragOffset.x;
    let newY = mouseY - dragOffset.y;

    if (snapToGrid) {
      const gridSize = 20;
      newX = Math.round(newX / gridSize) * gridSize;
      newY = Math.round(newY / gridSize) * gridSize;
    }

    // Boundary constraints
    newX = Math.max(10, Math.min(newX, 900));
    newY = Math.max(10, Math.min(newY, 600));

    setBooths(prev => {
      const updated = prev.map(b => (b.id === selectedBoothId ? { ...b, x: newX, y: newY } : b));
      return checkCollisions(updated);
    });
  };

  const handleMouseUp = () => {
    setIsDragging(false);
  };

  const updateSelectedBooth = (key: keyof VisualBooth, value: unknown) => {
    if (!selectedBoothId) return;
    setBooths(prev => {
      const updated = prev.map(b => (b.id === selectedBoothId ? { ...b, [key]: value } : b));
      return checkCollisions(updated);
    });
  };

  const addNewBooth = () => {
    const newNumber = `C-${100 + booths.length + 1}`;
    const newId = `b_${Date.now()}`;
    const newBooth: VisualBooth = {
      id: newId,
      number: newNumber,
      x: 100,
      y: 100,
      width: 100,
      height: 100,
      category: 'Standard',
      status: 'Available',
      price: 2000,
      shape: 'Rectangle'
    };

    setBooths(prev => checkCollisions([...prev, newBooth]));
    setSelectedBoothId(newId);
    showToast(`تمت إضافة جناح جديد رقم ${newNumber}`, 'success');
  };

  const deleteSelectedBooth = () => {
    if (!selectedBoothId) return;
    setBooths(prev => prev.filter(b => b.id !== selectedBoothId));
    setSelectedBoothId(null);
    showToast('تم حذف الجناح من المخطط', 'info');
  };

  const saveLayout = () => {
    showToast('هذا المصمم يعمل محلياً حالياً؛ لم يُحفظ المخطط في الخادم.', 'warning');
  };

  const autoArrange = () => {
    let startX = 60;
    let startY = 60;
    const spacing = 20;

    const arranged = booths.map((b) => {
      const x = startX;
      const y = startY;
      startX += b.width + spacing;
      if (startX > 750) {
        startX = 60;
        startY += 130;
      }
      return { ...b, x, y };
    });

    setBooths(checkCollisions(arranged));
    showToast('تمت إعادة ترتيب الأجنحة تلقائياً في القاعة', 'info');
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
      {/* Header Bar */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <Compass size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)' }}>
              مصمم الأجنحة والمخططات البصري 2D
            </h1>
          </div>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '2px' }}>
            قاعة المعارض الرئيسية (Hall 1) — معرض طرابلس الدولي للتقنية والابتكار 2026
          </p>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          <button
            onClick={addNewBooth}
            className="btn-secondary"
            style={{ height: '42px', padding: '0 16px' }}
          >
            <Plus size={16} />
            <span>إضافة جناح</span>
          </button>

          <button
            onClick={saveLayout}
            className="btn-primary"
            style={{ height: '42px', padding: '0 20px' }}
          >
            <Save size={16} />
            <span>حفظ المخطط</span>
          </button>
        </div>
      </div>

      {/* Split View Container (SCREEN_26 from DESIGN.md) */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: '1fr 340px',
          gap: '24px',
          minHeight: '700px'
        }}
      >
        {/* Center/Main Interactive Canvas Panel */}
        <div
          className="card"
          style={{
            padding: 0,
            display: 'flex',
            flexDirection: 'column',
            position: 'relative',
            overflow: 'hidden',
            backgroundColor: '#ffffff'
          }}
          onMouseMove={handleMouseMove}
          onMouseUp={handleMouseUp}
        >
          {/* Floating Top Toolbar */}
          <div
            style={{
              position: 'absolute',
              top: '16px',
              right: '16px',
              zIndex: 20,
              display: 'flex',
              alignItems: 'center',
              gap: '8px',
              padding: '6px 12px',
              borderRadius: 'var(--radius-xl)',
              backgroundColor: '#ffffff',
              boxShadow: 'var(--shadow-md)',
              border: '1px solid var(--border-main)'
            }}
          >
            <button
              onClick={() => setZoomScale(prev => Math.min(prev + 0.1, 1.8))}
              className="btn-icon"
              title="تكبير"
              style={{ width: '32px', height: '32px' }}
            >
              <ZoomIn size={16} />
            </button>
            <span style={{ fontSize: '12px', fontWeight: 700, color: 'var(--text-secondary)', minWidth: '40px', textAlign: 'center' }}>
              {(zoomScale * 100).toFixed(0)}%
            </span>
            <button
              onClick={() => setZoomScale(prev => Math.max(prev - 0.1, 0.6))}
              className="btn-icon"
              title="تصغير"
              style={{ width: '32px', height: '32px' }}
            >
              <ZoomOut size={16} />
            </button>
            <button
              onClick={() => setZoomScale(1.0)}
              className="btn-icon"
              title="استعادة المقياس 100%"
              style={{ width: '32px', height: '32px' }}
            >
              <Maximize2 size={15} />
            </button>

            <span style={{ width: '1px', height: '18px', backgroundColor: 'var(--border-main)' }} />

            <button
              onClick={() => setSnapToGrid(!snapToGrid)}
              className="btn-icon"
              title="الالتقاط التلقائي بالشبكة (Snap to Grid)"
              style={{
                width: '32px',
                height: '32px',
                color: snapToGrid ? 'var(--primary)' : 'var(--text-muted)',
                backgroundColor: snapToGrid ? 'var(--primary-light)' : 'transparent'
              }}
            >
              <Grid size={16} />
            </button>

            <button
              onClick={autoArrange}
              className="btn-icon"
              title="ترتيب تلقائي للأجنحة"
              style={{ width: '32px', height: '32px', color: 'var(--text-secondary)' }}
            >
              <Sparkles size={16} />
            </button>
          </div>

          {/* Canvas Workspace Area */}
          <div
            ref={canvasRef}
            onClick={() => setSelectedBoothId(null)}
            style={{
              flex: 1,
              width: '100%',
              minHeight: '660px',
              backgroundColor: '#f8fafc',
              position: 'relative',
              cursor: isDragging ? 'grabbing' : 'default',
              overflow: 'hidden',
              backgroundImage: showGrid
                ? 'radial-gradient(circle, #cbd5e1 1px, transparent 1px)'
                : 'none',
              backgroundSize: `${20 * zoomScale}px ${20 * zoomScale}px`,
              userSelect: 'none'
            }}
          >
            {/* Inner Scaled Canvas Board */}
            <div
              style={{
                transform: `scale(${zoomScale})`,
                transformOrigin: 'top right',
                position: 'absolute',
                top: 0,
                right: 0,
                width: '1000px',
                height: '750px'
              }}
            >
              {/* Hall Boundary Outline */}
              <div
                style={{
                  position: 'absolute',
                  top: '20px',
                  right: '20px',
                  width: '960px',
                  height: '710px',
                  border: '2px dashed #94a3b8',
                  borderRadius: '16px',
                  pointerEvents: 'none',
                  display: 'flex',
                  alignItems: 'flex-start',
                  justifyContent: 'flex-start',
                  padding: '12px'
                }}
              >
                <span style={{ fontSize: '11px', fontWeight: 700, color: '#94a3b8', background: '#f8fafc', padding: '2px 8px', borderRadius: '4px' }}>
                  حدود القاعة الرئيسية (50م × 40م)
                </span>
              </div>

              {/* Render Booths */}
              {booths.map(b => {
                const isSelected = b.id === selectedBoothId;
                const isReserved = b.status === 'Reserved';
                const isAvailable = b.status === 'Available';

                // Colors from DESIGN.md
                let border = isAvailable ? '#10b981' : isReserved ? '#6366f1' : '#f59e0b';
                let bg = isAvailable ? '#ecfdf5' : isReserved ? '#f5f3ff' : '#fffbeb';
                let textColor = isAvailable ? '#059669' : isReserved ? '#4f46e5' : '#d97706';

                if (b.hasCollision) {
                  border = '#ef4444';
                  bg = '#fee2e2';
                  textColor = '#dc2626';
                }

                return (
                  <div
                    key={b.id}
                    onMouseDown={(e) => handleMouseDown(e, b)}
                    style={{
                      position: 'absolute',
                      right: `${b.x}px`,
                      top: `${b.y}px`,
                      width: `${b.width}px`,
                      height: `${b.height}px`,
                      backgroundColor: bg,
                      border: `2px solid ${border}`,
                      borderRadius: '8px',
                      boxShadow: isSelected ? '0 0 0 3px rgba(99, 102, 241, 0.4), var(--shadow-lg)' : 'var(--shadow-sm)',
                      display: 'flex',
                      flexDirection: 'column',
                      alignItems: 'center',
                      justifyContent: 'center',
                      padding: '8px',
                      cursor: 'grab',
                      transition: isDragging ? 'none' : 'box-shadow 0.2s ease',
                      zIndex: isSelected ? 15 : 5
                    }}
                  >
                    <div style={{ display: 'flex', alignItems: 'center', gap: '4px', fontWeight: 800, fontSize: '13px', color: textColor }}>
                      <span>{b.number}</span>
                      {b.category === 'VIP' && (
                        <span style={{ fontSize: '10px', background: '#7c3aed', color: '#fff', padding: '1px 4px', borderRadius: '3px' }}>
                          VIP
                        </span>
                      )}
                    </div>

                    <div style={{ fontSize: '10px', color: 'var(--text-secondary)', marginTop: '2px', fontWeight: 600 }}>
                      {((b.width / 20) * 1).toFixed(0)}م × {((b.height / 20) * 1).toFixed(0)}م
                    </div>

                    {b.companyName && (
                      <div
                        style={{
                          fontSize: '11px',
                          color: textColor,
                          fontWeight: 700,
                          textAlign: 'center',
                          whiteSpace: 'nowrap',
                          overflow: 'hidden',
                          textOverflow: 'ellipsis',
                          maxWidth: '90%',
                          marginTop: '4px'
                        }}
                      >
                        {b.companyName}
                      </div>
                    )}

                    {b.hasCollision && (
                      <div style={{ position: 'absolute', top: '-8px', right: '-8px', background: '#ef4444', color: '#fff', borderRadius: '50%', padding: '2px' }}>
                        <AlertTriangle size={14} />
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          </div>
        </div>

        {/* Left Side (320px): Booth Properties Panel */}
        <div
          className="card"
          style={{
            padding: '24px',
            display: 'flex',
            flexDirection: 'column',
            gap: '20px',
            height: 'fit-content'
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', borderBottom: '1px solid var(--border-main)', paddingBottom: '14px' }}>
            <h3 style={{ fontSize: '16px', fontWeight: 800, color: 'var(--text-primary)' }}>
              خصائص الجناح المحدد
            </h3>
            {selectedBooth && (
              <span className={`badge ${selectedBooth.status === 'Available' ? 'badge-success' : selectedBooth.status === 'Reserved' ? 'badge-info' : 'badge-warning'}`}>
                {selectedBooth.status === 'Available' ? 'متاح' : selectedBooth.status === 'Reserved' ? 'محجوز' : 'قيد المراجعة'}
              </span>
            )}
          </div>

          {selectedBooth ? (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              {selectedBooth.hasCollision && (
                <div
                  style={{
                    padding: '10px 14px',
                    borderRadius: 'var(--radius-md)',
                    background: 'var(--danger-bg)',
                    color: 'var(--danger-text)',
                    fontSize: '12px',
                    fontWeight: 600,
                    display: 'flex',
                    alignItems: 'center',
                    gap: '8px'
                  }}
                >
                  <AlertTriangle size={16} />
                  <span>تحذير: هذا الجناح يتصادم أو يتداخل مع جناح آخر!</span>
                </div>
              )}

              {/* Booth ID/Number */}
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">معرف ورقم الجناح</label>
                <input
                  type="text"
                  value={selectedBooth.number}
                  onChange={(e) => updateSelectedBooth('number', e.target.value)}
                  className="input-field"
                />
              </div>

              {/* Dimensions */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">العرض (متر)</label>
                  <input
                    type="number"
                    value={Math.round(selectedBooth.width / 20)}
                    onChange={(e) => updateSelectedBooth('width', Math.max(40, (parseInt(e.target.value, 10) || 3) * 20))}
                    className="input-field"
                  />
                </div>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">الطول (متر)</label>
                  <input
                    type="number"
                    value={Math.round(selectedBooth.height / 20)}
                    onChange={(e) => updateSelectedBooth('height', Math.max(40, (parseInt(e.target.value, 10) || 3) * 20))}
                    className="input-field"
                  />
                </div>
              </div>

              {/* Category */}
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">فئة الجناح</label>
                <select
                  value={selectedBooth.category}
                  onChange={(e) => updateSelectedBooth('category', e.target.value)}
                  className="input-field"
                >
                  <option value="VIP">VIP خاص</option>
                  <option value="Featured">جناح مميز (Featured)</option>
                  <option value="Corner">جناح زاوية (Corner)</option>
                  <option value="Standard">جناح قياسي (Standard)</option>
                </select>
              </div>

              {/* Status */}
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">حالة الحجز</label>
                <select
                  value={selectedBooth.status}
                  onChange={(e) => updateSelectedBooth('status', e.target.value)}
                  className="input-field"
                >
                  <option value="Available">متاح للحجز (Available)</option>
                  <option value="Reserved">محجوز ومعتمد (Reserved)</option>
                  <option value="PendingReview">قيد المراجعة (Pending)</option>
                </select>
              </div>

              {/* Price */}
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">سعر الإيجار (د.ل)</label>
                <input
                  type="number"
                  value={selectedBooth.price}
                  onChange={(e) => updateSelectedBooth('price', parseInt(e.target.value, 10) || 0)}
                  className="input-field"
                />
              </div>

              {/* Company Allocated */}
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">الشركة العارضة المخصصة</label>
                <input
                  type="text"
                  value={selectedBooth.companyName || ''}
                  onChange={(e) => updateSelectedBooth('companyName', e.target.value)}
                  placeholder="لم يتم التخصيص بعد"
                  className="input-field"
                />
              </div>

              {/* Delete Button */}
              <button
                type="button"
                onClick={deleteSelectedBooth}
                className="btn-danger"
                style={{ marginTop: '8px', fontSize: '13px' }}
              >
                <Trash2 size={16} />
                <span>حذف هذا الجناح من القاعة</span>
              </button>
            </div>
          ) : (
            <div style={{ textAlign: 'center', padding: '40px 10px', color: 'var(--text-muted)' }}>
              <Compass size={40} style={{ margin: '0 auto 12px', strokeWidth: 1.5 }} />
              <p style={{ fontSize: '13px' }}>
                اضغط على أي جناح في المخطط لتعديل مقاساته وسعره وبياناته
              </p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
