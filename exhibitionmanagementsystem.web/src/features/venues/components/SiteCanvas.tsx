import { useEffect, useLayoutEffect, useRef, useState, type ReactNode } from 'react';
import { Check, Crosshair, Maximize2, Pencil, Pentagon, Scan, Undo2, X, ZoomIn, ZoomOut } from 'lucide-react';
import { SiteShape } from './SiteShape';
import { InlineLabel, SelectionActions, SiteContextMenu } from './SiteQuickActions';
import { editable, distance, isClosed, isPath, pointsBounds, rectanglePoints, unionBounds, worldPoints, type HallSource, type EditorShape } from '../utils/siteEditor';
import type { useSiteEditor } from './useSiteEditor';

export const SiteCanvas = ({ editor: e, hallDraft, available, placeHall, openHalls, openPageSize, title = 'مخطط الموقع', resizePageEnabled = true, renderShape, contextActions, canvasLabel = 'لوحة رسم الموقع والقاعات', paperFill = '#fff', renderContextMenu }: { editor: ReturnType<typeof useSiteEditor>; hallDraft: ReturnType<typeof useSiteEditor>['single']; available: HallSource[]; placeHall: (source: HallSource, at?: { x: number; y: number }) => void; openHalls: () => void; openPageSize: () => void; title?: string; resizePageEnabled?: boolean; renderShape?: (shape: EditorShape) => ReactNode; contextActions?: ReactNode; canvasLabel?: string; paperFill?: string; renderContextMenu?: (position: { x:number;y:number },close:()=>void)=>ReactNode }) => {
  const viewportRef = useRef<HTMLDivElement | null>(null);
  const [contextMenu, setContextMenu] = useState<{ x: number; y: number } | null>(null);
  const { doc, camera, size, single, selected, tool, cursor, polyPoints, svgRef, setSize, setCamera, zoom, down, move, up, cancel, guides, node, marquee, interacting, finishPolygon, setPolyPoints, changeTool, fit, selection, setSelection, worldFromClient, doubleClick, editingTextId, hallMode, wallMode } = e;
  const editingShape = doc.shapes.find(s => s.id === editingTextId);
  useLayoutEffect(() => {
    const viewport = viewportRef.current; if (!viewport) return;
    const observer = new ResizeObserver(entries => { const r = entries[0].contentRect; setSize({ width: r.width, height: r.height }); });
    observer.observe(viewport); return () => observer.disconnect();
  }, [setSize]);
  useEffect(() => {
    const svg = svgRef.current; if (!svg) return;
    const wheel = (event: WheelEvent) => {
      event.preventDefault();
      if (event.shiftKey || event.altKey) setCamera(c => ({ ...c, x: c.x + (event.shiftKey ? event.deltaY : event.deltaX) / c.zoom, y: c.y + (event.shiftKey ? 0 : event.deltaY) / c.zoom }));
      else { const r = svg.getBoundingClientRect(); zoom(Math.exp(-event.deltaY * .002), { x: event.clientX - r.left, y: event.clientY - r.top }); }
    };
    svg.addEventListener('wheel', wheel, { passive: false }); return () => svg.removeEventListener('wheel', wheel);
  });
  useEffect(() => {
    if (!contextMenu) return;
    const outside = (event: PointerEvent) => { if (!(event.target as Element)?.closest('.vs-site-context-menu')) setContextMenu(null); };
    const escape = (event: KeyboardEvent) => { if (event.key === 'Escape') { event.preventDefault(); event.stopPropagation(); setContextMenu(null); svgRef.current?.focus(); } };
    window.addEventListener('pointerdown', outside, true); window.addEventListener('keydown', escape, true);
    return () => { window.removeEventListener('pointerdown', outside, true); window.removeEventListener('keydown', escape, true); };
  }, [contextMenu, svgRef]);
  const width = (size.width || 900) / camera.zoom, height = (size.height || 650) / camera.zoom, handleSize = 7 / camera.zoom;
  const step = camera.zoom > 25 ? 1 : camera.zoom > 8 ? 5 : camera.zoom > 2 ? 10 : 50;
  const xTicks = Array.from({ length: Math.min(120, Math.ceil(width / step) + 2) }, (_, i) => Math.floor(camera.x / step) * step + i * step);
  const yTicks = Array.from({ length: Math.min(120, Math.ceil(height / step) + 2) }, (_, i) => Math.floor(camera.y / step) * step + i * step);
  const pathPoints = [...polyPoints, cursor];
  const hint = polyPoints.length ? 'انقر لإضافة زاوية · Enter أو نقرتان للإنهاء · Backspace لحذف آخر نقطة · Esc للإلغاء' : tool === 'nodes' ? 'اسحب الزوايا · + لإضافة نقطة · Delete لحذفها · Esc للعودة إلى التحريك' : tool === 'hall' && hallMode === 'rectangle' ? 'اسحب لرسم قاعة مستطيلة · يمكنك تعديل زواياها لاحقاً بالنقر المزدوج' : tool === 'hall' ? 'انقر على زوايا القاعة ثم انقر أول نقطة للإغلاق، أو اضغط Enter' : tool === 'wall' && wallMode === 'segment' ? 'اسحب من بداية الجدار إلى نهايته · الأخضر يؤكد استقامة الخط أو توازيه · Alt لتجاوز الالتقاط' : tool === 'wall' || tool === 'polygon' ? 'انقر على نقاط المسار ثم Enter أو نقرتان للإنهاء' : tool === 'select' ? 'نقرتان للتعديل · النقر الأيمن للأوامر · العجلة للتكبير · Space + سحب للتحريك' : tool === 'pan' ? 'اسحب لتحريك اللوحة · عجلة الفأرة للتكبير حول المؤشر' : 'اسحب للرسم؛ تعود أداة التحديد تلقائياً · Shift لتقييد النسبة · Alt لتجاوز الالتقاط';
  return <div className="vs-canvas-column">
    <div className="vs-canvas-tab"><span><Pentagon size={14} /> {title}</span><div className="vs-canvas-tab-actions"><button className={`vs-smart-alignment ${e.smartAlignment ? 'active' : ''}`} aria-label="المحاذاة الذكية" aria-pressed={e.smartAlignment} title="التقاط الحواف والمراكز والخطوط المتوازية؛ Alt لتجاوزه مؤقتاً" onClick={e.toggleSmartAlignment}><Crosshair size={13} /><span>محاذاة ذكية</span></button>{resizePageEnabled ? <button className="vs-page-size-button" aria-label="تغيير مساحة الرسم" title="تغيير عرض وطول مساحة الرسم" disabled={interacting || !!polyPoints.length || !!editingTextId || !!hallDraft} onClick={openPageSize}><Maximize2 size={13} /><span>مساحة الرسم</span><strong dir="ltr">{doc.bounds.width} × {doc.bounds.height} م</strong></button> : <strong dir="ltr">{doc.bounds.width} × {doc.bounds.height} م</strong>}</div></div>
    <div className={`vs-viewport vs-tool-${tool}`} ref={viewportRef} onDragOver={event => { if (event.dataTransfer.types.includes('application/x-site-hall')) { event.preventDefault(); event.dataTransfer.dropEffect = 'copy'; } }} onDrop={event => { event.preventDefault(); const h = available.find(s => s.hallID === Number(event.dataTransfer.getData('application/x-site-hall'))); if (h) placeHall(h, worldFromClient(event.clientX, event.clientY)); }}>
      <svg ref={svgRef} className="vs-canvas" width="100%" height="100%" viewBox={`${camera.x} ${camera.y} ${width} ${height}`} tabIndex={0} role="application" aria-label={canvasLabel} onPointerDown={down} onPointerMove={move} onPointerUp={up} onPointerCancel={cancel} onLostPointerCapture={cancel} onDoubleClick={doubleClick}
        onContextMenu={event => {
          event.preventDefault(); if (polyPoints.length) { finishPolygon(); return; }
          cancel(); const id = (event.target as Element).closest('[data-shape-id]')?.getAttribute('data-shape-id');
          if (id && !selection.includes(id)) setSelection([id]); if (!id) setSelection([]);
          const r = event.currentTarget.getBoundingClientRect(); setContextMenu({ x: event.clientX - r.left, y: event.clientY - r.top });
        }}>
        <defs><pattern id="studio-grid" width={doc.grid.size} height={doc.grid.size} patternUnits="userSpaceOnUse"><circle cx="0" cy="0" r={Math.max(.025, .7 / camera.zoom)} fill="#cbd5e1" /></pattern><filter id="studio-page-shadow" x="-20%" y="-20%" width="140%" height="140%"><feDropShadow dx="0" dy=".7" stdDeviation="1" floodColor="#334155" floodOpacity=".1" /></filter></defs>
        <rect width={doc.bounds.width} height={doc.bounds.height} fill={paperFill} filter="url(#studio-page-shadow)" />
        {doc.grid.visible && <rect width={doc.bounds.width} height={doc.bounds.height} fill="url(#studio-grid)" />}
        <rect width={doc.bounds.width} height={doc.bounds.height} fill="none" stroke="#cbd5e1" strokeWidth={1 / camera.zoom} />
        {doc.shapes.filter(s => doc.layers[s.layer].visible).map(s => <g key={s.id} data-shape-id={s.id} style={{ cursor: tool === 'select' ? editable(s, doc) ? 'move' : 'default' : undefined }}>{renderShape ? renderShape(s) : <SiteShape shape={s} selected={selection.includes(s.id)} hitArea zoom={camera.zoom} />}</g>)}
        {hallDraft && <g opacity=".6"><SiteShape shape={hallDraft} /></g>}
        <g className="vs-alignment-guides" pointerEvents="none">{guides.map((g, i) => g.kind === 'axis' ? <g key={i} data-alignment-guide={g.axis}>
          <line x1={g.axis === 'x' ? g.value : camera.x} y1={g.axis === 'y' ? g.value : camera.y} x2={g.axis === 'x' ? g.value : camera.x + width} y2={g.axis === 'y' ? g.value : camera.y + height} stroke="#db2777" strokeWidth={1.5 / camera.zoom} strokeDasharray={`${6 / camera.zoom} ${4 / camera.zoom}`} />
          <circle cx={g.reference.x} cy={g.reference.y} r={5 / camera.zoom} fill="#fff" stroke="#db2777" strokeWidth={2 / camera.zoom} />
        </g> : <g key={i} data-alignment-guide="parallel">
          {g.reference && <line x1={g.reference[0].x} y1={g.reference[0].y} x2={g.reference[1].x} y2={g.reference[1].y} stroke="#059669" strokeWidth={4 / camera.zoom} opacity=".75" />}
          <line x1={g.from.x} y1={g.from.y} x2={g.to.x} y2={g.to.y} stroke="#059669" strokeWidth={2 / camera.zoom} strokeDasharray={`${6 / camera.zoom} ${4 / camera.zoom}`} />
          <circle cx={g.to.x} cy={g.to.y} r={4 / camera.zoom} fill="#fff" stroke="#059669" strokeWidth={2 / camera.zoom} />
        </g>)}</g>
        {selected.filter(s => doc.layers[s.layer].visible).map(s => <g key={`selection-${s.id}`} transform={`translate(${s.x} ${s.y}) rotate(${s.rotation} ${s.width / 2} ${s.height / 2})`}>
          <rect width={s.width} height={s.height} fill="none" stroke="#2563eb" strokeWidth={1 / camera.zoom} strokeDasharray={!editable(s, doc) ? `${4 / camera.zoom} ${3 / camera.zoom}` : undefined} pointerEvents="none" />
          {single && editable(s, doc) && tool === 'select' && <>
            {rectanglePoints(s.width, s.height).map((p, i) => <rect key={i} data-handle="resize" data-index={i} x={p.x - handleSize / 2} y={p.y - handleSize / 2} width={handleSize} height={handleSize} fill="#fff" stroke="#2563eb" strokeWidth={1.3 / camera.zoom} style={{ cursor: i % 2 ? 'nesw-resize' : 'nwse-resize' }} />)}
            <line x1={s.width / 2} y1="0" x2={s.width / 2} y2={-24 / camera.zoom} stroke="#2563eb" strokeWidth={1 / camera.zoom} pointerEvents="none" /><circle data-handle="rotate" cx={s.width / 2} cy={-24 / camera.zoom} r={handleSize / 1.5} fill="#fff" stroke="#2563eb" strokeWidth={1.3 / camera.zoom} style={{ cursor: 'crosshair' }} />
          </>}
        </g>)}
        {single && tool === 'nodes' && isPath(single) && editable(single, doc) && <g>{worldPoints(single).map((p, i, pts) => <g key={i}>
          <circle data-handle="node" data-index={i} cx={p.x} cy={p.y} r={handleSize / 1.3} fill={node === i ? '#2563eb' : '#fff'} stroke="#2563eb" strokeWidth={1.5 / camera.zoom} style={{ cursor: 'move' }} />
          {(isClosed(single) || i < pts.length - 1) && (() => { const b = pts[(i + 1) % pts.length], x = (p.x + b.x) / 2, y = (p.y + b.y) / 2; return <g><circle data-handle="insert" data-index={i} cx={x} cy={y} r={handleSize / 1.6} fill="#dbeafe" stroke="#60a5fa" strokeWidth={1 / camera.zoom} style={{ cursor: 'copy' }} /><text x={x} y={y} textAnchor="middle" dominantBaseline="central" fontSize={9 / camera.zoom} fill="#2563eb" pointerEvents="none">+</text></g>; })()}
        </g>)}</g>}
        {polyPoints.length > 0 && <g pointerEvents="none"><path d={pathPoints.map((p, i) => `${i ? 'L' : 'M'} ${p.x} ${p.y}`).join(' ') + (tool === 'wall' ? '' : ' Z')} fill={tool === 'wall' ? 'none' : '#dbeafe'} fillOpacity=".45" stroke="#2563eb" strokeWidth={1.5 / camera.zoom} strokeDasharray={`${5 / camera.zoom} ${3 / camera.zoom}`} />{polyPoints.map((p, i) => <circle key={i} cx={p.x} cy={p.y} r={5 / camera.zoom} fill={i ? '#fff' : '#2563eb'} stroke="#2563eb" strokeWidth={1 / camera.zoom} />)}<text x={cursor.x + 12 / camera.zoom} y={cursor.y - 10 / camera.zoom} fontSize={11 / camera.zoom} fill="#2563eb">{distance(polyPoints.at(-1)!, cursor).toFixed(2)} م</text></g>}
        {marquee && (() => { const b = pointsBounds([marquee.start, marquee.end]), crossing = marquee.end.x < marquee.start.x; return <rect {...b} fill={crossing ? '#10b981' : '#2563eb'} fillOpacity=".07" stroke={crossing ? '#10b981' : '#2563eb'} strokeWidth={1 / camera.zoom} strokeDasharray={crossing ? `${4 / camera.zoom} ${3 / camera.zoom}` : undefined} pointerEvents="none" />; })()}
        {resizePageEnabled && tool === 'select' && !editingTextId && <g className="vs-page-resize-handles">
          <rect data-page-resize="e" x={doc.bounds.width - 6 / camera.zoom} y={doc.bounds.height / 2 - 18 / camera.zoom} width={12 / camera.zoom} height={36 / camera.zoom} rx={3 / camera.zoom} fill="#fff" stroke="#2563eb" strokeWidth={1.3 / camera.zoom} style={{ cursor: 'ew-resize' }}><title>اسحب لتغيير عرض مساحة الرسم</title></rect>
          <rect data-page-resize="s" x={doc.bounds.width / 2 - 18 / camera.zoom} y={doc.bounds.height - 6 / camera.zoom} width={36 / camera.zoom} height={12 / camera.zoom} rx={3 / camera.zoom} fill="#fff" stroke="#2563eb" strokeWidth={1.3 / camera.zoom} style={{ cursor: 'ns-resize' }}><title>اسحب لتغيير طول مساحة الرسم</title></rect>
          <rect data-page-resize="se" x={doc.bounds.width - 7 / camera.zoom} y={doc.bounds.height - 7 / camera.zoom} width={14 / camera.zoom} height={14 / camera.zoom} rx={3 / camera.zoom} fill="#2563eb" stroke="#fff" strokeWidth={1 / camera.zoom} style={{ cursor: 'nwse-resize' }}><title>اسحب لتوسيع مساحة الرسم</title></rect>
          <path d={`M ${doc.bounds.width - 2 / camera.zoom} ${doc.bounds.height - 4 / camera.zoom} H ${doc.bounds.width + 4 / camera.zoom} V ${doc.bounds.height + 2 / camera.zoom}`} fill="none" stroke="#fff" strokeWidth={1.2 / camera.zoom} pointerEvents="none" />
        </g>}
      </svg>
      {contextActions === undefined ? <SelectionActions editor={e} /> : contextActions}
      {!!guides.length && <div className="vs-alignment-feedback" role="status"><Check size={15} /><span>{[...new Set(guides.map(g => g.label))].join(' · ')}</span><small>Alt لتجاوز الالتقاط</small></div>}
      {editingShape && <InlineLabel key={editingShape.id} shape={editingShape} editor={e} />}
      {contextMenu && (renderContextMenu ? renderContextMenu(contextMenu,()=>setContextMenu(null)) : <SiteContextMenu editor={e} position={contextMenu} close={() => setContextMenu(null)} />)}
      <div className="vs-ruler-corner">م</div><svg className="vs-ruler-top" aria-hidden="true" width={size.width} height="24">{xTicks.map(x => <g key={x} transform={`translate(${(x - camera.x) * camera.zoom},0)`}><line y1="17" y2="24" stroke="#94a3b8" /><text x="4" y="13">{x}</text></g>)}</svg><svg className="vs-ruler-left" aria-hidden="true" width="24" height={size.height}>{yTicks.map(y => <g key={y} transform={`translate(0,${(y - camera.y) * camera.zoom})`}><line x1="17" x2="24" stroke="#94a3b8" /><text x="3" y="-3" transform="rotate(-90 10 -5)">{y}</text></g>)}</svg>
      {!doc.shapes.length && !interacting && !polyPoints.length && <div className="vs-empty-canvas"><div><Pentagon size={34} /></div><strong>الموقع يبدأ بخطّك</strong><p>ارسم حدود القاعة بأي شكل، وأضف الجدران والتقسيمات والأبواب.</p><button onClick={() => changeTool('hall')}><Pencil size={15} /> ارسم أول قاعة</button>{available.length > 0 && <button className="secondary" onClick={openHalls}>أو أضف قاعة موجودة</button>}</div>}
      {polyPoints.length > 0 && <div className="vs-drawing-actions"><span>{polyPoints.length} نقاط</span><button onClick={finishPolygon}><Check size={14} /> إنهاء الرسم</button><button onClick={() => setPolyPoints(p => p.slice(0, -1))} title="حذف آخر نقطة"><Undo2 size={14} /></button><button onClick={() => setPolyPoints([])} title="إلغاء"><X size={14} /></button></div>}
      <div className="vs-canvas-controls"><button title="تصغير" onClick={() => zoom(.8)}><ZoomOut size={16} /></button><button className="vs-zoom-value" onClick={() => zoom(8 / camera.zoom)}>{Math.round(camera.zoom / 8 * 100)}%</button><button title="تكبير" onClick={() => zoom(1.25)}><ZoomIn size={16} /></button><span /><button title="ملاءمة الصفحة" onClick={() => fit()}><Scan size={16} /></button><button title="ملاءمة المحدد" disabled={!selected.length} onClick={() => selected.length && fit(doc, unionBounds(selected))}><Crosshair size={16} /></button></div>
    </div><div className="vs-context-help"><Crosshair size={13} />{hint}</div>
  </div>;
};


