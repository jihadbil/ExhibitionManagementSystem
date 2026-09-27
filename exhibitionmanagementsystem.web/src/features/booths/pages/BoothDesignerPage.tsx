import React, { useState, useRef, useEffect, useCallback } from 'react';
import { useSearchParams } from 'react-router-dom';
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
  Sparkles,
  Layers,
  Building,
  Store
} from 'lucide-react';
import { useToast } from '../../../app/providers/ToastContext';
import { api } from '../../../services/api';

interface VisualBooth {
  id: string; // string or number string
  dbId?: number;
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

interface Venue {
  venueID: number;
  name: string;
}

interface Hall {
  hallID: number;
  venueID: number;
  hallName: string;
  floorPlanWidth?: number;
  floorPlanHeight?: number;
}

interface ApiBooth {
  boothID: number;
  hallID: number;
  boothNumber: string;
  currentAreaSqM: number;
  width?: number;
  height?: number;
  posX?: number;
  posY?: number;
  shapeType?: string;
  status: string;
}

export const BoothDesignerPage: React.FC = () => {
  const { showToast } = useToast();
  const [searchParams] = useSearchParams();

  // Venues and Halls
  const [venues, setVenues] = useState<Venue[]>([]);
  const [halls, setHalls] = useState<Hall[]>([]);
  const [selectedVenueId, setSelectedVenueId] = useState<string>('');
  const [selectedHallId, setSelectedHallId] = useState<string>('');
  const [isLoadingHalls, setIsLoadingHalls] = useState(false);
  const [isLoadingBooths, setIsLoadingBooths] = useState(false);
  const [isSavingLayout, setIsSavingLayout] = useState(false);

  // Canvas State
  const [booths, setBooths] = useState<VisualBooth[]>([]);
  const [selectedBoothId, setSelectedBoothId] = useState<string | null>(null);
  const [zoomScale, setZoomScale] = useState(1.0);
  const [snapToGrid, setSnapToGrid] = useState(true);
  const [showGrid] = useState(true);

  // Dragging state
  const [isDragging, setIsDragging] = useState(false);
  const [dragOffset, setDragOffset] = useState({ x: 0, y: 0 });
  const canvasRef = useRef<HTMLDivElement>(null);

  const selectedBooth = booths.find(b => b.id === selectedBoothId) || null;

  // Collision detection between booths
  const checkCollisions = useCallback((currentBooths: VisualBooth[]): VisualBooth[] => {
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
  }, []);

  // 1. Load Venues on mount
  useEffect(() => {
    const loadVenues = async () => {
      try {
        const response = await api.get<Venue[]>('/api/venues');
        const data = response.data ?? [];
        setVenues(data);
        if (data.length > 0) {
          setSelectedVenueId(data[0].venueID.toString());
        }
      } catch {
        showToast('تعذر تحميل المواقع للمصمم.', 'danger');
      }
    };
    void loadVenues();
  }, [showToast]);

  // 2. Load Halls when Venue changes
  useEffect(() => {
    if (!selectedVenueId) {
      setHalls([]);
      setSelectedHallId('');
      return;
    }

    const loadHalls = async () => {
      setIsLoadingHalls(true);
      try {
        const response = await api.get<Hall[]>(`/api/halls/venue/${selectedVenueId}`);
        const data = response.data ?? [];
        setHalls(data);
        if (data.length > 0) {
          const hallIdFromUrl = searchParams.get('hallId');
          const matched = data.find(h => h.hallID.toString() === hallIdFromUrl);
          setSelectedHallId(matched ? matched.hallID.toString() : data[0].hallID.toString());
        } else {
          setSelectedHallId('');
          setBooths([]);
        }
      } catch {
        showToast('تعذر تحميل قاعات الموقع المختار.', 'danger');
      } finally {
        setIsLoadingHalls(false);
      }
    };

    void loadHalls();
  }, [selectedVenueId, searchParams, showToast]);

  // 3. Load Booths for selected Hall
  const loadBoothsForHall = useCallback(async (hallId: string) => {
    setIsLoadingBooths(true);
    try {
      const response = await api.get<ApiBooth[]>(`/api/booths/hall/${hallId}`);
      const data = response.data ?? [];

      if (data.length === 0) {
        setBooths([]);
        setSelectedBoothId(null);
        return;
      }

      // Map ApiBooths to VisualBooths with default positions if not set
      const mapped: VisualBooth[] = data.map((b, index) => {
        const defaultX = 50 + (index % 5) * 140;
        const defaultY = 50 + Math.floor(index / 5) * 130;
        const width = b.width ? Math.round(b.width * 25) : 100;
        const height = b.height ? Math.round(b.height * 25) : 100;

        let category: VisualBooth['category'] = 'Standard';
        if (b.shapeType === 'VIP') category = 'VIP';
        else if (b.shapeType === 'Corner') category = 'Corner';
        else if (b.shapeType === 'Premium') category = 'Featured';

        let status: VisualBooth['status'] = 'Available';
        if (b.status === 'Reserved' || b.status === 'Occupied') status = 'Reserved';
        else if (b.status === 'PendingReview') status = 'PendingReview';

        return {
          id: `booth_${b.boothID}`,
          dbId: b.boothID,
          number: b.boothNumber,
          x: b.posX !== undefined && b.posX !== null ? Number(b.posX) : defaultX,
          y: b.posY !== undefined && b.posY !== null ? Number(b.posY) : defaultY,
          width: width > 40 ? width : 100,
          height: height > 40 ? height : 100,
          category,
          status,
          price: Math.round(b.currentAreaSqM * 150) || 2000,
          shape: 'Rectangle',
        };
      });

      const checked = checkCollisions(mapped);
      setBooths(checked);
      if (checked.length > 0) {
        setSelectedBoothId(checked[0].id);
      }
    } catch {
      showToast('تعذر تحميل أجنحة القاعة من الخادم.', 'danger');
    } finally {
      setIsLoadingBooths(false);
    }
  }, [checkCollisions, showToast]);

  useEffect(() => {
    if (selectedHallId) {
      void loadBoothsForHall(selectedHallId);
    }
  }, [selectedHallId, loadBoothsForHall]);

  // Dragging event handlers
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
    newX = Math.max(10, Math.min(newX, 1200));
    newY = Math.max(10, Math.min(newY, 800));

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

  // Add new booth
  const addNewBooth = async () => {
    if (!selectedHallId) {
      showToast('يرجى اختيار قاعة أولاً.', 'warning');
      return;
    }

    const newNumber = `B-${100 + booths.length + 1}`;
    try {
      const response = await api.post<ApiBooth>('/api/booths', {
        hallID: Number(selectedHallId),
        boothNumber: newNumber,
        originalAreaSqM: 12,
        width: 4,
        height: 3,
        posX: 60,
        posY: 60,
        shapeType: 'Standard',
      });

      const b = response.data;
      const newVisual: VisualBooth = {
        id: `booth_${b.boothID}`,
        dbId: b.boothID,
        number: b.boothNumber,
        x: 60,
        y: 60,
        width: 100,
        height: 100,
        category: 'Standard',
        status: 'Available',
        price: 1800,
        shape: 'Rectangle',
      };

      setBooths(prev => checkCollisions([...prev, newVisual]));
      setSelectedBoothId(newVisual.id);
      showToast(`تم إنشاء الجناح رقم ${newNumber} في الخادم.`, 'success');
    } catch {
      showToast('فشل إنشاء الجناح في الخادم.', 'danger');
    }
  };

  // Delete selected booth
  const deleteSelectedBooth = async () => {
    if (!selectedBoothId || !selectedBooth) return;
    if (!window.confirm(`هل تريد حذف الجناح «${selectedBooth.number}» من الخادم؟`)) return;

    if (selectedBooth.dbId) {
      try {
        await api.delete(`/api/booths/${selectedBooth.dbId}`);
        setBooths(prev => prev.filter(b => b.id !== selectedBoothId));
        setSelectedBoothId(null);
        showToast('تم حذف الجناح بنجاح من الخادم.', 'success');
      } catch {
        showToast('تعذر حذف الجناح؛ قد يكون مرتبطاً بحجز.', 'danger');
      }
    } else {
      setBooths(prev => prev.filter(b => b.id !== selectedBoothId));
      setSelectedBoothId(null);
    }
  };

  // Save all layout positions to server
  const saveLayout = async () => {
    if (booths.length === 0) {
      showToast('لا توجد أجنحة لحفظها في هذا المخطط.', 'info');
      return;
    }

    setIsSavingLayout(true);
    try {
      const updates = booths.map(b => {
        if (!b.dbId) return Promise.resolve();
        return api.put(`/api/booths/${b.dbId}`, {
          boothNumber: b.number,
          status: b.status,
          posX: Math.round(b.x),
          posY: Math.round(b.y),
          width: Math.round(b.width / 25),
          height: Math.round(b.height / 25),
          shapeType: b.category,
        });
      });

      await Promise.all(updates);
      showToast('تم حفظ إحداثيات ومخطط الأجنحة في الخادم بنجاح!', 'success');
    } catch {
      showToast('فشل حفظ المخطط بالكامل. يرجى إعادة المحاولة.', 'danger');
    } finally {
      setIsSavingLayout(false);
    }
  };

  // Auto arrange booths in a tidy grid
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
    showToast('تمت إعادة ترتيب الأجنحة في القاعة.', 'info');
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
            تخطيط بصري تفاعلي مع كشف التصادم، حفظ الإحداثيات، وتوزيع الأجنحة في القاعة.
          </p>
        </div>

        {/* Location & Hall Selectors */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px', flexWrap: 'wrap' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
            <Building size={16} style={{ color: 'var(--primary)' }} />
            <select
              className="input-field"
              style={{ width: '180px', height: '38px', padding: '0 10px', fontSize: '13px' }}
              value={selectedVenueId}
              onChange={e => setSelectedVenueId(e.target.value)}
            >
              {venues.map(v => (
                <option key={v.venueID} value={v.venueID.toString()}>{v.name}</option>
              ))}
            </select>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
            <Layers size={16} style={{ color: 'var(--primary)' }} />
            <select
              className="input-field"
              style={{ width: '180px', height: '38px', padding: '0 10px', fontSize: '13px' }}
              value={selectedHallId}
              onChange={e => setSelectedHallId(e.target.value)}
              disabled={isLoadingHalls || halls.length === 0}
            >
              {halls.length === 0 && <option value="">لا توجد قاعات</option>}
              {halls.map(h => (
                <option key={h.hallID} value={h.hallID.toString()}>{h.hallName}</option>
              ))}
            </select>
          </div>

          <button
            onClick={addNewBooth}
            className="btn-secondary"
            style={{ height: '38px', padding: '0 14px' }}
            disabled={!selectedHallId}
          >
            <Plus size={16} />
            <span>جناح جديد</span>
          </button>

          <button
            onClick={saveLayout}
            className="btn-primary"
            style={{ height: '38px', padding: '0 18px' }}
            disabled={isSavingLayout || !selectedHallId}
          >
            <Save size={16} />
            <span>{isSavingLayout ? 'جاري الحفظ…' : 'حفظ المخطط'}</span>
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
        {/* Main Interactive Canvas Panel */}
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
              gap: '6px',
              backgroundColor: 'rgba(255, 255, 255, 0.95)',
              backdropFilter: 'blur(8px)',
              padding: '6px 10px',
              borderRadius: 'var(--radius-lg)',
              border: '1px solid var(--border-main)',
              boxShadow: 'var(--shadow-md)'
            }}
          >
            <button
              onClick={() => setZoomScale(prev => Math.min(prev + 0.1, 1.8))}
              className="btn-icon"
              title="تكبير (+)"
            >
              <ZoomIn size={16} />
            </button>
            <span style={{ fontSize: '12px', fontWeight: 600, color: 'var(--text-secondary)', minWidth: '40px', textAlign: 'center' }}>
              {Math.round(zoomScale * 100)}%
            </span>
            <button
              onClick={() => setZoomScale(prev => Math.max(prev - 0.1, 0.5))}
              className="btn-icon"
              title="تصغير (-)"
            >
              <ZoomOut size={16} />
            </button>
            <button
              onClick={() => setZoomScale(1.0)}
              className="btn-icon"
              title="إعادة ضبط المقياس"
            >
              <Maximize2 size={16} />
            </button>

            <div style={{ width: '1px', height: '18px', backgroundColor: 'var(--border-main)', margin: '0 4px' }} />

            <button
              onClick={() => setSnapToGrid(!snapToGrid)}
              className={`btn-icon ${snapToGrid ? 'btn-icon-active' : ''}`}
              title="المحاذاة للشبكة (Snap to Grid)"
              style={{
                color: snapToGrid ? 'var(--primary)' : 'var(--text-muted)',
                backgroundColor: snapToGrid ? 'var(--primary-subtle, rgba(99,102,241,0.1))' : 'transparent'
              }}
            >
              <Grid size={16} />
            </button>

            <button
              onClick={autoArrange}
              className="btn-icon"
              title="ترتيب تلقائي للأجنحة"
            >
              <Sparkles size={16} />
            </button>

            {selectedBooth && (
              <button
                onClick={deleteSelectedBooth}
                className="btn-icon"
                title="حذف الجناح المختار"
                style={{ color: 'var(--danger)' }}
              >
                <Trash2 size={16} />
              </button>
            )}
          </div>

          {/* Interactive Canvas Area */}
          <div
            ref={canvasRef}
            style={{
              flex: 1,
              width: '100%',
              minHeight: '680px',
              backgroundColor: '#f8fafc',
              backgroundImage: showGrid
                ? 'radial-gradient(#cbd5e1 1.2px, transparent 1.2px), radial-gradient(#cbd5e1 1.2px, #f8fafc 1.2px)'
                : 'none',
              backgroundSize: '24px 24px',
              backgroundPosition: '0 0, 12px 12px',
              position: 'relative',
              overflow: 'auto',
              cursor: isDragging ? 'grabbing' : 'default',
              userSelect: 'none'
            }}
            onClick={() => setSelectedBoothId(null)}
          >
            {isLoadingBooths ? (
              <div style={{ position: 'absolute', inset: 0, display: 'grid', placeItems: 'center', background: 'rgba(255,255,255,0.7)', zIndex: 10 }}>
                جاري تحميل أجنحة القاعة…
              </div>
            ) : booths.length === 0 ? (
              <div style={{ position: 'absolute', inset: 0, display: 'grid', placeItems: 'center', color: 'var(--text-secondary)', zIndex: 5 }}>
                <div>
                  <Store size={36} style={{ margin: '0 auto 8px', display: 'block', color: 'var(--text-muted)' }} />
                  لا توجد أجنحة في هذه القاعة حتى الآن. اضغط "جناح جديد" لبدء التصميم.
                </div>
              </div>
            ) : null}

            {/* Scaled Container for Booths */}
            <div
              style={{
                transform: `scale(${zoomScale})`,
                transformOrigin: 'top right',
                position: 'absolute',
                top: 0,
                right: 0,
                width: '1200px',
                height: '800px',
                transition: isDragging ? 'none' : 'transform 0.1s ease-out'
              }}
            >
              {booths.map(booth => {
                const isSelected = selectedBoothId === booth.id;

                let borderColor = '#94a3b8';
                let bgColor = '#ffffff';
                let textColor = '#0f172a';

                if (booth.status === 'Reserved') {
                  borderColor = '#6366f1';
                  bgColor = '#e0e7ff';
                  textColor = '#3730a3';
                } else if (booth.status === 'Available') {
                  borderColor = '#10b981';
                  bgColor = '#d1fae5';
                  textColor = '#065f46';
                } else if (booth.status === 'PendingReview') {
                  borderColor = '#f59e0b';
                  bgColor = '#fef3c7';
                  textColor = '#92400e';
                }

                if (booth.hasCollision) {
                  borderColor = '#ef4444';
                  bgColor = '#fee2e2';
                }

                return (
                  <div
                    key={booth.id}
                    onMouseDown={(e) => handleMouseDown(e, booth)}
                    onClick={(e) => {
                      e.stopPropagation();
                      setSelectedBoothId(booth.id);
                    }}
                    style={{
                      position: 'absolute',
                      right: `${booth.x}px`,
                      top: `${booth.y}px`,
                      width: `${booth.width}px`,
                      height: `${booth.height}px`,
                      backgroundColor: bgColor,
                      border: `2px solid ${borderColor}`,
                      borderRadius: '8px',
                      boxShadow: isSelected
                        ? '0 0 0 3px rgba(99, 102, 241, 0.4), var(--shadow-lg)'
                        : 'var(--shadow-sm)',
                      cursor: isDragging && isSelected ? 'grabbing' : 'grab',
                      display: 'flex',
                      flexDirection: 'column',
                      alignItems: 'center',
                      justifyContent: 'center',
                      padding: '8px',
                      textAlign: 'center',
                      zIndex: isSelected ? 15 : 5,
                      transition: isDragging ? 'none' : 'box-shadow 0.15s, border-color 0.15s'
                    }}
                  >
                    {booth.hasCollision && (
                      <div
                        style={{
                          position: 'absolute',
                          top: '-8px',
                          left: '-8px',
                          backgroundColor: '#ef4444',
                          color: '#fff',
                          borderRadius: '50%',
                          padding: '2px',
                          display: 'flex'
                        }}
                        title="تحذير: تداخل مع جناح آخر!"
                      >
                        <AlertTriangle size={12} />
                      </div>
                    )}

                    <span style={{ fontSize: '13px', fontWeight: 800, color: textColor, lineHeight: 1.2 }}>
                      {booth.number}
                    </span>

                    <span style={{ fontSize: '10px', fontWeight: 600, color: textColor, opacity: 0.85, marginTop: '2px' }}>
                      {Math.round((booth.width * booth.height) / 625)} م²
                    </span>

                    {booth.companyName && (
                      <span
                        style={{
                          fontSize: '9px',
                          color: textColor,
                          marginTop: '4px',
                          whiteSpace: 'nowrap',
                          overflow: 'hidden',
                          textOverflow: 'ellipsis',
                          maxWidth: '90%'
                        }}
                      >
                        {booth.companyName}
                      </span>
                    )}
                  </div>
                );
              })}
            </div>
          </div>

          {/* Bottom Canvas Footer / Legend */}
          <div
            style={{
              padding: '12px 20px',
              backgroundColor: '#ffffff',
              borderTop: '1px solid var(--border-main)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              fontSize: '12px',
              color: 'var(--text-secondary)'
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
              <span style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                <span style={{ width: '10px', height: '10px', borderRadius: '2px', backgroundColor: '#10b981' }} />
                متاح للحجز ({booths.filter(b => b.status === 'Available').length})
              </span>
              <span style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                <span style={{ width: '10px', height: '10px', borderRadius: '2px', backgroundColor: '#6366f1' }} />
                محجوز ومؤكد ({booths.filter(b => b.status === 'Reserved').length})
              </span>
              <span style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                <span style={{ width: '10px', height: '10px', borderRadius: '2px', backgroundColor: '#f59e0b' }} />
                قيد المراجعة ({booths.filter(b => b.status === 'PendingReview').length})
              </span>
            </div>

            <span>إجمالي الأجنحة بالمخطط: {booths.length}</span>
          </div>
        </div>

        {/* Right Sidebar: Properties Panel */}
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
          <div style={{ borderBottom: '1px solid var(--border-main)', paddingBottom: '12px' }}>
            <h3 style={{ fontSize: '16px', fontWeight: 800, color: 'var(--text-primary)' }}>
              خصائص الجناح المحدد
            </h3>
            <p style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '2px' }}>
              {selectedBooth ? `جناح رقم ${selectedBooth.number}` : 'اضغط على أي جناح في المخطط لتعديله'}
            </p>
          </div>

          {selectedBooth ? (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">رقم الجناح / الرمز</label>
                <input
                  type="text"
                  value={selectedBooth.number}
                  onChange={(e) => updateSelectedBooth('number', e.target.value)}
                  className="input-field"
                />
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">العرض (px)</label>
                  <input
                    type="number"
                    value={selectedBooth.width}
                    onChange={(e) => updateSelectedBooth('width', Math.max(40, Number(e.target.value)))}
                    className="input-field"
                  />
                </div>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">الطول (px)</label>
                  <input
                    type="number"
                    value={selectedBooth.height}
                    onChange={(e) => updateSelectedBooth('height', Math.max(40, Number(e.target.value)))}
                    className="input-field"
                  />
                </div>
              </div>

              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">الفئة والنوع</label>
                <select
                  value={selectedBooth.category}
                  onChange={(e) => updateSelectedBooth('category', e.target.value)}
                  className="input-field"
                >
                  <option value="Standard">Standard — قياسي</option>
                  <option value="Corner">Corner — ناصية</option>
                  <option value="Featured">Featured — مميز</option>
                  <option value="VIP">VIP — خاص فاخر</option>
                </select>
              </div>

              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">حالة الجناح</label>
                <select
                  value={selectedBooth.status}
                  onChange={(e) => updateSelectedBooth('status', e.target.value)}
                  className="input-field"
                >
                  <option value="Available">متاح للحجز</option>
                  <option value="Reserved">محجوز ومؤكد</option>
                  <option value="PendingReview">قيد المراجعة والتدقيق</option>
                </select>
              </div>

              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">سعر الحجز التقديري (د.ل)</label>
                <input
                  type="number"
                  value={selectedBooth.price}
                  onChange={(e) => updateSelectedBooth('price', Number(e.target.value))}
                  className="input-field"
                />
              </div>

              <div
                style={{
                  padding: '12px',
                  backgroundColor: 'var(--bg-main)',
                  borderRadius: 'var(--radius-md)',
                  fontSize: '12px',
                  color: 'var(--text-secondary)',
                  display: 'flex',
                  flexDirection: 'column',
                  gap: '6px'
                }}
              >
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <span>الإحداثيات:</span>
                  <strong>X: {Math.round(selectedBooth.x)} | Y: {Math.round(selectedBooth.y)}</strong>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <span>المساحة المحسوبة:</span>
                  <strong>{Math.round((selectedBooth.width * selectedBooth.height) / 625)} م²</strong>
                </div>
              </div>

              <button
                onClick={deleteSelectedBooth}
                className="btn-secondary"
                style={{ color: 'var(--danger)', borderColor: 'var(--danger-border)' }}
              >
                <Trash2 size={16} />
                <span>حذف الجناح</span>
              </button>
            </div>
          ) : (
            <div style={{ textAlign: 'center', padding: '40px 10px', color: 'var(--text-muted)' }}>
              <Compass size={32} style={{ margin: '0 auto 8px', opacity: 0.5 }} />
              <p style={{ fontSize: '13px' }}>قم باختيار جناح من المخطط لعرض وتعديل أبعاده وبياناته</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
