import { useEffect, useState } from 'react';
import { Eye, EyeOff, Layers, Lock, Unlock, SlidersHorizontal, List, ChevronDown, AlertCircle, Pentagon } from 'lucide-react';
import { editable, kindLabels, layers, polygonArea, round, type EditorDocument, type EditorShape, type EditorIssue } from '../utils/siteEditor';

export const NumberField = ({ label, value, onChange, min, max, step = .1, disabled = false }: { label: string; value: number; onChange: (n: number) => boolean | void; min?: number; max?: number; step?: number; disabled?: boolean }) => {
  const [text, setText] = useState(String(round(value)));
  useEffect(() => setText(String(round(value))), [value]);
  const submit = () => {
    const n = Number(text);
    if (!text.trim() || !Number.isFinite(n)) { setText(String(round(value))); return; }
    const next = Math.max(min ?? -Infinity, Math.min(max ?? Infinity, n));
    setText(String(round(next))); if (next !== value && onChange(next) === false) setText(String(round(value)));
  };
  return <label className="vs-field"><span>{label}</span><input type="number" value={text} step={step} min={min} max={max} disabled={disabled} onChange={e => setText(e.target.value)} onBlur={submit} onKeyDown={e => { if (e.key === 'Enter') e.currentTarget.blur(); }} /></label>;
};

export const SiteInspector = ({ document: doc, selection, issues, patch, changeDocument, select, renameHall }: {
  document: EditorDocument; selection: string[]; issues: EditorIssue[];
  patch: (changes: Partial<EditorShape>) => void; changeDocument: (doc: EditorDocument) => boolean | void;
  select: (ids: string[]) => void; renameHall: (shape: EditorShape) => void;
}) => {
  const [tab, setTab] = useState<'properties' | 'layers' | 'objects'>('properties');
  const chosen = doc.shapes.filter(s => selection.includes(s.id)), shape = chosen.length === 1 ? chosen[0] : null;
  const locked = shape ? !editable(shape, doc) : true;
  const updateLayer = (id: typeof layers[number]['id'], field: 'visible' | 'locked') => changeDocument({ ...doc, layers: { ...doc.layers, [id]: { ...doc.layers[id], [field]: !doc.layers[id][field] } } });
  return <aside className="vs-inspector" dir="rtl">
    <div className="vs-panel-tabs">
      <button className={tab === 'properties' ? 'active' : ''} onClick={() => setTab('properties')} title="الخصائص"><SlidersHorizontal size={16} /> خصائص</button>
      <button className={tab === 'layers' ? 'active' : ''} onClick={() => setTab('layers')} title="الطبقات"><Layers size={16} /></button>
      <button className={tab === 'objects' ? 'active' : ''} onClick={() => setTab('objects')} title="العناصر"><List size={16} /></button>
    </div>
    <div className="vs-inspector-body">
      {tab === 'properties' && <>
        <div className="vs-section-title">{shape ? kindLabels[shape.kind] : chosen.length ? `${chosen.length} عناصر محددة` : 'إعدادات الصفحة'}<ChevronDown size={13} /></div>
        {shape ? <>
          <div className="vs-object-summary"><span style={{ background: shape.stroke }} /><div><strong>{shape.label || kindLabels[shape.kind]}</strong><small>{shape.kind === 'hall' ? `قاعة مرتبطة #${shape.hallID}` : layers.find(l => l.id === shape.layer)?.label}</small></div><button title={shape.locked ? 'فتح العنصر' : 'قفل العنصر'} onClick={() => patch({ locked: !shape.locked })}>{shape.locked ? <Lock size={15} /> : <Unlock size={15} />}</button></div>
          {locked && <div className="vs-note">العنصر أو طبقته مقفلة. يمكنك تحديده وفتح القفل.</div>}
          <div className="vs-field-grid">
            <NumberField label="X · متر" value={shape.x} disabled={locked} onChange={x => patch({ x })} />
            <NumberField label="Y · متر" value={shape.y} disabled={locked} onChange={y => patch({ y })} />
            <NumberField label="العرض · متر" value={shape.width} min={.2} disabled={locked} onChange={width => patch({ width })} />
            <NumberField label="الارتفاع · متر" value={shape.height} min={.2} disabled={locked} onChange={height => patch({ height })} />
          </div>
          <NumberField label="زاوية الدوران °" value={shape.rotation} disabled={locked} min={0} max={359} step={1} onChange={rotation => patch({ rotation })} />
          <div className="vs-section-title">المظهر</div>
          <label className="vs-field"><span>التسمية</span><input key={`${shape.id}-${shape.label}`} defaultValue={shape.label} disabled={locked || shape.kind === 'hall'} onBlur={e => { if (e.target.value !== shape.label) patch({ label: e.target.value.slice(0, 200) }); }} placeholder="بدون تسمية" /></label>
          {shape.kind === 'hall' && <button className="vs-wide-button" disabled={locked} onClick={() => renameHall(shape)}>تعديل اسم القاعة</button>}
          <div className="vs-field-grid vs-color-fields">
            <label className="vs-field"><span>لون الخط</span><input type="color" value={shape.stroke} disabled={locked} onChange={e => patch({ stroke: e.target.value })} /></label>
            <label className="vs-field"><span>التعبئة</span><input type="color" value={shape.fill === 'none' ? '#ffffff' : shape.fill} disabled={locked} onChange={e => patch({ fill: e.target.value })} /></label>
          </div>
          <label className="vs-checkbox"><input type="checkbox" checked={shape.fill === 'none'} disabled={locked} onChange={e => patch({ fill: e.target.checked ? 'none' : '#ffffff' })} /> بدون تعبئة</label>
          <div className="vs-field-grid"><NumberField label="سماكة الخط · م" value={shape.strokeWidth} min={.02} max={2} disabled={locked} step={.05} onChange={strokeWidth => patch({ strokeWidth })} /><NumberField label="حجم النص · م" value={shape.fontSize} min={.2} max={10} disabled={locked} onChange={fontSize => patch({ fontSize })} /></div>
          <label className="vs-field"><span>الطبقة</span><select value={shape.layer} disabled={locked || shape.kind === 'hall'} onChange={e => patch({ layer: e.target.value as EditorShape['layer'] })}>{layers.map(l => <option key={l.id} value={l.id}>{l.label}</option>)}</select></label>
          {(shape.kind === 'hall' || shape.kind === 'polygon') && <div className="vs-area-card"><Pentagon size={18} /><div><small>مساحة المضلع الفعلية</small><strong>{round(polygonArea(shape.points)).toLocaleString('ar-LY')} م²</strong></div><span>{shape.points.length} زوايا</span></div>}
        </> : <>
          <div className="vs-field-grid"><NumberField label="عرض الموقع · م" value={doc.bounds.width} min={5} max={10000} step={1} onChange={width => changeDocument({ ...doc, bounds: { ...doc.bounds, width } })} /><NumberField label="طول الموقع · م" value={doc.bounds.height} min={5} max={10000} step={1} onChange={height => changeDocument({ ...doc, bounds: { ...doc.bounds, height } })} /></div>
          <label className="vs-field"><span>حجم الشبكة</span><select value={doc.grid.size} onChange={e => changeDocument({ ...doc, grid: { ...doc.grid, size: Number(e.target.value) } })}>{[.25, .5, 1, 2, 5].map(n => <option key={n} value={n}>{n} متر</option>)}</select></label>
          <label className="vs-checkbox"><input type="checkbox" checked={doc.grid.visible} onChange={e => changeDocument({ ...doc, grid: { ...doc.grid, visible: e.target.checked } })} /> إظهار الشبكة</label>
          <label className="vs-checkbox"><input type="checkbox" checked={doc.grid.snap} onChange={e => changeDocument({ ...doc, grid: { ...doc.grid, snap: e.target.checked } })} /> الالتقاط للشبكة</label>
          <div className="vs-note">المحاذاة الذكية تلتقط الحواف والمراكز والخطوط المتوازية. الوردي للمحاذاة والأخضر للتوازي. Shift لتقييد الاتجاه، وAlt لتجاوز الالتقاط.</div>
          {chosen.length > 1 && <div className="vs-note">أوامر المحاذاة والتجميع متاحة في شريط الأدوات.</div>}
        </>}
      </>}
      {tab === 'layers' && <><div className="vs-section-title">طبقات المخطط <small>{layers.length}</small></div>{layers.map(l => <div className="vs-layer-row" key={l.id}><span className="vs-layer-dot" style={{ background: l.color }} /><strong>{l.label}</strong><small>{doc.shapes.filter(s => s.layer === l.id).length}</small><button title="إظهار / إخفاء" onClick={() => updateLayer(l.id, 'visible')}>{doc.layers[l.id].visible ? <Eye size={15} /> : <EyeOff size={15} />}</button><button title="قفل / فتح الطبقة" onClick={() => updateLayer(l.id, 'locked')}>{doc.layers[l.id].locked ? <Lock size={15} /> : <Unlock size={15} />}</button></div>)}<div className="vs-note">قفل الطبقة يمنع التحريك والتعديل والحذف. يبقى تحديد عناصرها متاحاً.</div></>}
      {tab === 'objects' && <><div className="vs-section-title">عناصر الصفحة <small>{doc.shapes.length}</small></div>{[...doc.shapes].reverse().map(s => <button key={s.id} className={`vs-object-row ${selection.includes(s.id) ? 'active' : ''}`} onClick={() => select([s.id])}><span style={{ background: s.stroke }} /><strong>{s.label || kindLabels[s.kind]}</strong>{!editable(s, doc) && <Lock size={13} />}{!doc.layers[s.layer].visible && <EyeOff size={13} />}</button>)}{!doc.shapes.length && <div className="vs-note">ابدأ برسم قاعة أو جدار من لوحة الأدوات.</div>}</>}
      {issues.length > 0 && <section className="vs-issues"><div className="vs-section-title"><AlertCircle size={14} /> فحص المخطط</div>{issues.slice(0, 8).map((issue, i) => <button key={i} onClick={() => { if (issue.id) select([issue.id]); }}>{issue.message}</button>)}</section>}
    </div>
    <div className="vs-inspector-footer"><span className="vs-layer-dot" style={{ background: '#10b981' }} /> تصميم الموقع والقاعات فقط</div>
  </aside>;
};
