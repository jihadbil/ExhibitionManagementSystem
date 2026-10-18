import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowRight, MousePointer2, Hand, Pentagon, Square, Circle, Minus, Pencil, Type, DoorOpen, Ruler, Columns3, Undo2, Redo2, Save, Download, Upload, Printer, Maximize2, Minimize2, Grid3X3, Lock, Unlock, Copy, Trash2, Group, Ungroup, BringToFront, SendToBack, Check, X, Plus, Building2, CornerDownLeft, HelpCircle, Crosshair, AlertTriangle, PanelRight, SlidersHorizontal } from 'lucide-react';
import { useAuth } from '../../../app/providers/AuthContext';
import { useToast } from '../../../app/providers/ToastContext';
import { api } from '../../../services/api';
import { getApiErrorMessage } from '../../../services/apiError';
import { SiteDrawing } from '../components/SiteShape';
import { SiteInspector } from '../components/SiteInspector';
import { SiteCanvas } from '../components/SiteCanvas';
import { SiteAlignmentMenu } from '../components/SiteAlignmentMenu';
import { PageSizeDialog } from '../components/PageSizeDialog';
import { shortcuts, useSiteEditor } from '../components/useSiteEditor';
import { editable, hydrateDocument, insidePage, kindLabels, makeShape, polygonArea, rectanglePoints, round, serializeDocument, shapeBounds, snap, validateDocument, type EditorShape, type HallSource, type Point, type Tool } from '../utils/siteEditor';
import '../styles/venue-designer.css';

interface PlanResponse { planJSON: string; version: number }
interface Recovery { json: string; version: number }
const icons = { select: MousePointer2, nodes: Pentagon, pan: Hand, hall: Building2, wall: Columns3, line: Minus, rectangle: Square, ellipse: Circle, polygon: Pentagon, freehand: Pencil, text: Type, door: DoorOpen, dimension: Ruler, exit: CornerDownLeft, column: Circle };
const toolNames: Record<Tool, string> = { ...kindLabels, select: 'تحديد', nodes: 'تعديل الزوايا', pan: 'تحريك اللوحة' };
const download = (data: string, mime: string, name: string) => {
  const url = URL.createObjectURL(new Blob([data], { type: mime })), anchor = document.createElement('a');
  anchor.href = url; anchor.download = name; anchor.click(); window.setTimeout(() => URL.revokeObjectURL(url), 1000);
};
const ToolButton = ({ value, active, onClick, compact = false }: { value: Tool; active: boolean; onClick: () => void; compact?: boolean }) => {
  const Icon = icons[value];
  return <button className={`${active ? 'active' : ''} ${compact ? 'compact' : ''}`} title={`${toolNames[value]}${shortcuts[value] ? ` (${shortcuts[value]})` : ''}`} onClick={onClick}><Icon size={compact ? 19 : 20} />{!compact && <><span>{toolNames[value]}</span>{shortcuts[value] && <kbd>{shortcuts[value]}</kbd>}</>}</button>;
};
export const VenueDesignerPage = () => { const { venueId } = useParams(); return <VenueStudio key={venueId} venueId={Number(venueId)} />; };

const VenueStudio = ({ venueId }: { venueId: number }) => {
  const { user } = useAuth(), { showToast } = useToast(), navigate = useNavigate();
  const [name, setName] = useState(''), [sources, setSources] = useState<HallSource[]>([]);
  const [loading, setLoading] = useState(true), [loadError, setLoadError] = useState('');
  const [fullscreen, setFullscreen] = useState(false), [showHelp, setShowHelp] = useState(false), [library, setLibrary] = useState<'shapes' | 'halls'>('shapes');
  const [libraryOpen, setLibraryOpen] = useState(false), [inspectorOpen, setInspectorOpen] = useState(false);
  const [pageSizeOpen, setPageSizeOpen] = useState(false);
  const [saveState, setSaveState] = useState<'ready' | 'saving' | 'saved' | 'error' | 'conflict'>('ready'), [savedJSON, setSavedJSON] = useState('');
  const [recovery, setRecovery] = useState<Recovery | null>(null), [hallDraft, setHallDraft] = useState<EditorShape | null>(null);
  const [hallName, setHallName] = useState(''), [hallLink, setHallLink] = useState('new'), [savingHall, setSavingHall] = useState(false), [renameShape, setRenameShape] = useState<EditorShape | null>(null);
  const busy = useRef(false), version = useRef(0), mounted = useRef(true), initiallyFitted = useRef(false), fileRef = useRef<HTMLInputElement | null>(null);
  const printRef = useRef<HTMLDivElement | null>(null);
  const editor = useSiteEditor(message => showToast(message, 'warning'), shape => { setHallDraft(shape); setHallName(''); setHallLink('new'); }, !!hallDraft || !!renameShape || !!recovery || showHelp || pageSizeOpen || loading, shape => { setRenameShape(shape); setHallName(shape.label); });
  const { doc, history, selected, single, tool, camera, size, interacting, polyPoints } = editor;
  const committedJSON = useMemo(() => serializeDocument(history.document), [history.document]), dirty = committedJSON !== savedJSON;
  const issues = useMemo(() => validateDocument(history.document), [history.document]), errors = issues.filter(i => i.severity === 'error');
  const placed = new Set(doc.shapes.filter(s => s.kind === 'hall').map(s => s.hallID)), available = sources.filter(h => !placed.has(h.hallID));
  const storageKey = `venue-studio:${user?.tenantID ?? 0}:${user?.userId ?? ''}:${venueId}`;

  useEffect(() => {
    mounted.current = true; const abort = new AbortController();
    void (async () => {
      try {
        if (!Number.isInteger(venueId) || venueId <= 0) throw new Error('معرف الموقع غير صالح.');
        const [venue, halls, plan] = await Promise.all([
          api.get<{ name: string }>(`/api/venues/${venueId}`, { signal: abort.signal }),
          api.get<HallSource[]>(`/api/halls/venue/${venueId}`, { signal: abort.signal }),
          api.get<PlanResponse>(`/api/venues/${venueId}/site-plan`, { signal: abort.signal }),
        ]);
        if (abort.signal.aborted) return;
        const next = hydrateDocument(plan.data.planJSON, halls.data);
        setSources(halls.data); setName(venue.data.name); version.current = plan.data.version;
        editor.dispatch({ type: 'reset', document: next }); setSavedJSON(serializeDocument(next));
        try { const local = localStorage.getItem(storageKey); if (local) { const parsed = JSON.parse(local) as Recovery; if (parsed.json && parsed.json !== serializeDocument(next)) setRecovery(parsed); } } catch { /* Browser recovery is optional. */ }
      } catch (error) { if (!abort.signal.aborted) setLoadError(error instanceof Error && !('response' in error) ? error.message : getApiErrorMessage(error, 'تعذر تحميل الموقع.')); }
      finally { if (!abort.signal.aborted) setLoading(false); }
    })();
    return () => { mounted.current = false; abort.abort(); };
  // dispatch is stable; loading is scoped to this venue and user.
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [venueId, storageKey]);
  useEffect(() => {
    if (!loading && !loadError && !initiallyFitted.current && size.width > 100) { initiallyFitted.current = true; editor.fit(history.document); }
  // Initial fit must not reset camera after every drawing change.
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [loading, loadError, size.width, size.height]);

  const save = useCallback(async (manual = false): Promise<boolean> => {
    if (busy.current || saveState === 'conflict' || interacting || polyPoints.length || editor.editingTextId || hallDraft || renameShape || recovery) return false;
    const snapshot = editor.documentRef.current, validation = validateDocument(snapshot).filter(i => i.severity === 'error');
    if (validation.length) { if (manual) showToast(validation[0].message, 'warning'); return false; }
    const json = serializeDocument(snapshot); if (json === savedJSON) return true;
    busy.current = true; setSaveState('saving');
    try {
      const response = await api.put<PlanResponse>(`/api/venues/${venueId}/site-plan`, { planJSON: json, expectedVersion: version.current });
      if (!mounted.current) return true;
      version.current = response.data.version; setSavedJSON(json); setSaveState('saved');
      try { if (serializeDocument(editor.documentRef.current) === json) localStorage.removeItem(storageKey); else localStorage.setItem(storageKey, JSON.stringify({ json: serializeDocument(editor.documentRef.current), version: response.data.version })); } catch { /* Successful remote save does not depend on local storage. */ }
      if (manual) showToast('تم حفظ تصميم الموقع.', 'success'); return true;
    } catch (error) {
      if (!mounted.current) return false;
      const code = (error as { response?: { data?: { errorCode?: string } } }).response?.data?.errorCode;
      setSaveState(code === 'SITE_PLAN_CONFLICT' ? 'conflict' : 'error'); showToast(code === 'SITE_PLAN_CONFLICT' ? 'توجد نسخة أحدث. صدّر مسودتك قبل إعادة تحميل نسخة الخادم.' : getApiErrorMessage(error, 'تعذر الحفظ. احتفظ بمسودتك أو أعد المحاولة.'), 'danger'); return false;
    } finally { busy.current = false; }
  }, [saveState, interacting, polyPoints.length, editor.editingTextId, hallDraft, renameShape, recovery, savedJSON, showToast, venueId, storageKey, editor.documentRef]);
  useEffect(() => {
    if (loading || loadError || !dirty || recovery) return;
    try { localStorage.setItem(storageKey, JSON.stringify({ json: committedJSON, version: version.current })); } catch { /* beforeunload still protects unsaved work. */ }
  }, [committedJSON, dirty, loading, loadError, storageKey, recovery]);
  useEffect(() => {
    if (loading || !dirty || errors.length || interacting || polyPoints.length || editor.editingTextId || hallDraft || renameShape || recovery || ['error', 'conflict', 'saving'].includes(saveState)) return;
    const timer = window.setTimeout(() => { void save(); }, 1500); return () => window.clearTimeout(timer);
  }, [loading, dirty, committedJSON, errors.length, interacting, polyPoints.length, editor.editingTextId, hallDraft, renameShape, recovery, saveState, save]);
  useEffect(() => {
    const protect = (event: BeforeUnloadEvent) => { if (dirty || interacting || polyPoints.length || editor.editingTextId || hallDraft) { event.preventDefault(); event.returnValue = ''; } };
    const keys = (event: KeyboardEvent) => { if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 's') { event.preventDefault(); if (!showHelp) void save(true); } };
    window.addEventListener('beforeunload', protect); window.addEventListener('keydown', keys); return () => { window.removeEventListener('beforeunload', protect); window.removeEventListener('keydown', keys); };
  }, [dirty, interacting, polyPoints.length, editor.editingTextId, hallDraft, showHelp, save]);

  const placeHall = (source: HallSource, at?: Point) => {
    if (placed.has(source.hallID)) { const s = doc.shapes.find(s => s.hallID === source.hallID)!; editor.setSelection([s.id]); editor.fit(doc, shapeBounds(s)); return; }
    const w = source.floorPlanWidth && source.floorPlanWidth > 0 ? source.floorPlanWidth : 20, h = source.floorPlanHeight && source.floorPlanHeight > 0 ? source.floorPlanHeight : 15;
    const center = at ?? { x: camera.x + size.width / camera.zoom / 2, y: camera.y + size.height / camera.zoom / 2 };
    const a = snap({ x: Math.max(0, Math.min(doc.bounds.width - w, center.x - w / 2)), y: Math.max(0, Math.min(doc.bounds.height - h, center.y - h / 2)) }, doc);
    const s = makeShape('hall', [a, { x: a.x + w, y: a.y + h }], crypto.randomUUID()); s.points = rectanglePoints(w, h); s.hallID = source.hallID; s.label = source.hallName;
    if (!insidePage(s, doc)) { showToast('وسّع الموقع ليستوعب القاعة.', 'warning'); return; } editor.add(s); editor.changeTool('select');
  };
  const bindHall = async () => {
    if (!hallDraft || savingHall || doc.layers.halls.locked) return; setSavingHall(true);
    try {
      let source: HallSource;
      if (hallLink === 'new') { if (!hallName.trim()) return; const response = await api.post<HallSource>('/api/halls', { venueID: venueId, hallName: hallName.trim(), areaSqM: round(polygonArea(hallDraft.points)), floorPlanWidth: round(hallDraft.width), floorPlanHeight: round(hallDraft.height), isActive: true }); source = response.data; setSources(current => [...current, source]); }
      else { source = sources.find(h => h.hallID === Number(hallLink))!; if (!source || placed.has(source.hallID)) throw new Error('اختر قاعة غير موضوعة في المخطط.'); }
      const s = { ...hallDraft, hallID: source.hallID, label: source.hallName };
      editor.commit({ ...history.document, layers: { ...history.document.layers, halls: { visible: true, locked: false } }, shapes: [...history.document.shapes, s] });
      editor.setSelection([s.id]); setHallDraft(null); editor.changeTool('select');
    } catch (error) { showToast(getApiErrorMessage(error, error instanceof Error ? error.message : 'تعذر ربط القاعة.'), 'danger'); } finally { setSavingHall(false); }
  };
  const renameHall = async () => {
    if (!renameShape || !hallName.trim() || savingHall) return; const source = sources.find(h => h.hallID === renameShape.hallID)!; setSavingHall(true);
    try {
      // Preserve every other hall field from the API response, including its separate interior plan.
      const response = await api.put<HallSource>(`/api/halls/${source.hallID}`, { ...source, hallName: hallName.trim() });
      setSources(list => list.map(h => h.hallID === source.hallID ? response.data : h));
      editor.dispatch({ type: 'renameHall', hallID: source.hallID, name: hallName.trim() }); setRenameShape(null);
    } catch (error) { showToast(getApiErrorMessage(error, 'تعذر تعديل الاسم.'), 'danger'); } finally { setSavingHall(false); }
  };
  const importFile = async (file: File) => {
    try { if (file.size > 2000000) throw new Error('الملف أكبر من 2 ميجابايت.'); const next = hydrateDocument(await file.text(), sources); if (validateDocument(next).some(i => i.severity === 'error')) throw new Error('المخطط المستورد يحتوي أخطاء هندسية.'); editor.cancel(); editor.setPolyPoints([]); editor.commit(next); editor.setSelection([]); editor.fit(next); showToast('تم الاستيراد. يمكنك التراجع عنه.', 'success'); } catch (error) { showToast(error instanceof Error ? error.message : 'تعذر الاستيراد.', 'danger'); }
  };
  const restore = () => { if (!recovery) return; try { const next = hydrateDocument(recovery.json, sources); editor.commit(next); if (recovery.version !== version.current) setSaveState('conflict'); setRecovery(null); editor.fit(next); } catch { showToast('تعذر استعادة المسودة. نزّلها للاحتفاظ بها.', 'danger'); } };
  const leave = async () => { if (interacting || polyPoints.length || hallDraft) { showToast('أكمل الرسم أو اضغط Esc لإلغائه قبل المغادرة.', 'warning'); return; } if (!dirty || await save(true)) navigate(`/venues/${venueId}/halls`); };
  const exportSVG = () => { const svg = printRef.current?.querySelector('svg'); if (svg) download(new XMLSerializer().serializeToString(svg), 'image/svg+xml;charset=utf-8', `site-${venueId}.svg`); };
  const button = (t: Tool, compact = false) => <ToolButton key={t} value={t} compact={compact} active={tool === t} onClick={() => { editor.changeTool(t); setLibraryOpen(false); }} />;
  if (loading) return <div className="vs-loading"><Building2 size={32} /><strong>جاري فتح استوديو الموقع…</strong></div>;
  if (loadError) return <div className="vs-loading"><AlertTriangle size={32} /><strong>{loadError}</strong><button onClick={() => window.location.reload()}>إعادة المحاولة</button></div>;

  return <div className={`vs-studio ${fullscreen ? 'vs-fullscreen' : ''} ${libraryOpen ? 'vs-library-open' : ''} ${inspectorOpen ? 'vs-inspector-open' : ''}`} dir="rtl">
    <header className="vs-header"><button className="vs-back" title="حفظ والعودة إلى القاعات" onClick={() => void leave()}><ArrowRight size={19} /></button><div className="vs-brand"><span><Pentagon size={24} /></span><div><strong>استوديو الموقع<span> / {name}</span></strong><small>تصميم الموقع والقاعات</small></div></div><div className={`vs-save-status ${saveState}`}><span />{saveState === 'saving' ? 'جاري الحفظ…' : saveState === 'conflict' ? 'تعارض النسخة' : saveState === 'error' ? 'تعذر الحفظ · أعد المحاولة' : errors.length ? 'راجع أخطاء المخطط' : dirty ? 'مسودة · بانتظار الحفظ' : 'جميع التغييرات محفوظة'}</div><div className="vs-header-actions"><button className="vs-mobile-toggle" title="إظهار أدوات الرسم" onClick={() => { setLibraryOpen(!libraryOpen); setInspectorOpen(false); }}><PanelRight size={17} /></button><button className="vs-mobile-toggle" title="إظهار الخصائص والطبقات" onClick={() => { setInspectorOpen(!inspectorOpen); setLibraryOpen(false); }}><SlidersHorizontal size={17} /></button><button onClick={() => setShowHelp(true)} title="دليل الأدوات"><HelpCircle size={17} /></button><button onClick={() => setFullscreen(!fullscreen)} title="توسيع مساحة العمل">{fullscreen ? <Minimize2 size={17} /> : <Maximize2 size={17} />}</button><button className="vs-export-button" onClick={exportSVG}><Download size={16} /> تصدير SVG</button><button className="vs-save-button" disabled={saveState === 'saving' || saveState === 'conflict' || interacting || !!polyPoints.length} onClick={() => void save(true)}><Save size={16} /> حفظ</button></div></header>
    {saveState === 'conflict' && <div className="vs-conflict"><AlertTriangle size={15} /> توجد نسخة أحدث على الخادم. صدّر مسودتك قبل إعادة التحميل.<button onClick={() => download(serializeDocument(doc), 'application/json', `site-${venueId}-draft.json`)}>تنزيل المسودة</button><button onClick={() => window.location.reload()}>تحميل نسخة الخادم</button></div>}
    <nav className="vs-toolbar" aria-label="أوامر الرسم"><div className="vs-tool-group">{(['select', 'nodes', 'pan'] as Tool[]).map(t => button(t, true))}</div><div className="vs-tool-group"><button disabled={!history.past.length || interacting} title="تراجع Ctrl+Z" onClick={() => editor.undo()}><Undo2 size={17} /></button><button disabled={!history.future.length || interacting} title="إعادة Ctrl+Y" onClick={() => editor.undo(true)}><Redo2 size={17} /></button></div><div className="vs-tool-group"><button disabled={!selected.length} title="نسخ Ctrl+C" onClick={editor.copy}><Copy size={17} /></button><button disabled={!selected.some(s => editable(s, doc))} title="حذف Delete" onClick={editor.remove}><Trash2 size={17} /></button><button disabled={!single || doc.layers[single.layer].locked} title="قفل / فتح العنصر" onClick={() => single && editor.patch({ locked: !single.locked })}>{single?.locked ? <Unlock size={17} /> : <Lock size={17} />}</button></div><div className="vs-tool-group"><SiteAlignmentMenu editor={editor} /><button disabled={selected.length < 2} title="تجميع Ctrl+G" onClick={() => editor.group()}><Group size={17} /></button><button disabled={!selected.some(s => s.groupId)} title="فك التجميع Ctrl+Shift+G" onClick={() => editor.group(true)}><Ungroup size={17} /></button></div><div className="vs-tool-group"><button disabled={!selected.length} title="إحضار للأمام" onClick={() => editor.order(true)}><BringToFront size={17} /></button><button disabled={!selected.length} title="إرسال للخلف" onClick={() => editor.order(false)}><SendToBack size={17} /></button></div><div className="vs-toolbar-spacer" /><div className="vs-tool-group"><button onClick={() => fileRef.current?.click()} title="استيراد ملف مخطط JSON"><Upload size={16} /></button><button onClick={() => download(serializeDocument(doc), 'application/json', `site-${venueId}.json`)} title="تنزيل ملف قابل للتعديل"><Download size={16} /></button><button title="طباعة / حفظ PDF" onClick={() => window.print()}><Printer size={16} /></button></div><input ref={fileRef} type="file" accept=".json,application/json" hidden onChange={event => { const file = event.target.files?.[0]; if (file) void importFile(file); event.target.value = ''; }} /></nav>
    <div className="vs-workspace"><aside className="vs-library"><div className="vs-panel-tabs"><button className={library === 'shapes' ? 'active' : ''} onClick={() => setLibrary('shapes')}>أدوات الرسم</button><button className={library === 'halls' ? 'active' : ''} onClick={() => setLibrary('halls')}>القاعات <small>{sources.length}</small></button></div><div className="vs-library-body">{library === 'shapes' ? <><div className="vs-section-title">حدود القاعات <small>رسم يدوي</small></div><div className="vs-hall-tool">{button('hall')}<div className="vs-draw-mode"><button className={editor.hallMode === 'rectangle' ? 'active' : ''} onClick={() => { editor.setHallMode('rectangle'); editor.changeTool('hall'); }}>سحب مستطيل</button><button className={editor.hallMode === 'polygon' ? 'active' : ''} onClick={() => { editor.setHallMode('polygon'); editor.changeTool('hall'); }}>نقاط حرة</button></div><p>{editor.hallMode === 'rectangle' ? 'اسحب لرسم القاعة؛ انقرها مرتين لتعديل الزوايا.' : 'انقر الزوايا ثم انقر أول نقطة لإغلاق القاعة.'}</p></div><div className="vs-section-title">الإنشاء والتقسيم</div><div className="vs-shape-tools">{(['wall', 'door', 'column', 'line'] as Tool[]).map(t => button(t))}</div>{tool === 'wall' && <div className="vs-draw-mode vs-wall-mode"><button className={editor.wallMode === 'segment' ? 'active' : ''} onClick={() => { editor.setWallMode('segment'); editor.setPolyPoints([]); }}>جدار بالسحب</button><button className={editor.wallMode === 'path' ? 'active' : ''} onClick={() => editor.setWallMode('path')}>عدة نقاط</button></div>}<div className="vs-section-title">أشكال حرة</div><div className="vs-shape-tools">{(['rectangle', 'ellipse', 'polygon', 'freehand'] as Tool[]).map(t => button(t))}</div><div className="vs-section-title">توضيحات ومسارات</div><div className="vs-shape-tools">{(['text', 'dimension', 'exit'] as Tool[]).map(t => button(t))}</div><div className="vs-library-tip"><Crosshair size={18} /><strong>كل زاوية تحت تحكمك</strong><p>استخدم «تعديل الزوايا» لتشكيل قاعة على حرف L أو لإضافة ارتدادات ومداخل.</p></div></> : <><div className="vs-section-title">قاعات الموقع <small>{sources.length}</small></div><p className="vs-library-caption">اسحب القاعة إلى الصفحة أو انقر عليها؛ انقرها مرتين لتغيير زواياها.</p>{sources.map(h => <button draggable={!placed.has(h.hallID)} onDragStart={event => event.dataTransfer.setData('application/x-site-hall', String(h.hallID))} className={`vs-hall-item ${placed.has(h.hallID) ? 'placed' : ''}`} key={h.hallID} onClick={() => placeHall(h)}><span><Building2 size={20} /></span><div><strong>{h.hallName}</strong><small>{placed.has(h.hallID) ? 'موجودة في المخطط' : 'جاهزة للرسم'}</small></div>{placed.has(h.hallID) ? <Check size={15} /> : <Plus size={15} />}</button>)}<button className="vs-wide-button" onClick={() => { editor.changeTool('hall'); setLibrary('shapes'); }}><Plus size={15} /> رسم قاعة جديدة</button></>}</div><div className="vs-library-footer"><span>الموقع</span><strong>{doc.bounds.width} × {doc.bounds.height} م</strong></div></aside><SiteCanvas editor={editor} hallDraft={hallDraft} available={available} placeHall={placeHall} openHalls={() => { setLibrary('halls'); setLibraryOpen(true); }} openPageSize={() => setPageSizeOpen(true)} /><SiteInspector document={doc} selection={editor.selection} issues={issues} patch={editor.patch} changeDocument={editor.changeDocument} select={editor.setSelection} renameHall={shape => { setRenameShape(shape); setHallName(shape.label); }} /></div>
    <footer className="vs-statusbar"><div><span className="vs-active-tool">{toolNames[tool]}</span><span>{doc.shapes.length} عناصر</span><span>{doc.shapes.filter(s => s.kind === 'hall').length} قاعات</span>{errors.length > 0 && <span className="vs-error-count"><AlertTriangle size={12} /> {errors.length} أخطاء</span>}</div><div><span dir="ltr">X {editor.cursor.x.toFixed(2)} · Y {editor.cursor.y.toFixed(2)} m</span><button className={doc.grid.snap ? 'active' : ''} onClick={() => editor.commit({ ...doc, grid: { ...doc.grid, snap: !doc.grid.snap } })}><Grid3X3 size={13} /> التقاط الشبكة</button></div></footer>
    {pageSizeOpen && <PageSizeDialog document={doc} close={() => setPageSizeOpen(false)} apply={bounds => { const next = { ...doc, bounds }; if (!editor.changeDocument(next)) return false; editor.fit(next); return true; }} />}
    {(hallDraft || renameShape) && <div className="vs-modal-backdrop"><form className="vs-modal" onSubmit={event => { event.preventDefault(); void (renameShape ? renameHall() : bindHall()); }}><div className="vs-modal-header"><span><Building2 size={23} /></span><div><h2>{renameShape ? 'تعديل اسم القاعة' : 'حدود القاعة جاهزة'}</h2><p>{hallDraft ? `${hallDraft.points.length} زوايا · ${round(polygonArea(hallDraft.points))} م² مساحة فعلية` : 'الاسم مرتبط ببيانات القاعة في الموقع.'}</p></div><button type="button" disabled={savingHall} onClick={() => { setHallDraft(null); setRenameShape(null); }}><X size={18} /></button></div>{hallDraft && <label className="vs-field"><span>ربط الحدود بقاعة</span><select value={hallLink} onChange={event => setHallLink(event.target.value)} disabled={savingHall}><option value="new">إنشاء قاعة جديدة</option>{available.map(h => <option key={h.hallID} value={h.hallID}>{h.hallName}</option>)}</select></label>}{(hallLink === 'new' || renameShape) && <label className="vs-field"><span>اسم القاعة</span><input autoFocus required maxLength={200} value={hallName} disabled={savingHall} onChange={event => setHallName(event.target.value)} placeholder="مثال: القاعة الرئيسية" /></label>}<div className="vs-note">تُحفظ حدود القاعة وزواياها في تصميم الموقع. الجدران الداخلية تُرسم بأداة الجدار.</div><div className="vs-modal-actions"><button type="submit" className="vs-save-button" disabled={savingHall}>{savingHall ? 'جاري الحفظ…' : renameShape ? 'حفظ الاسم' : 'إضافة القاعة للمخطط'}</button><button type="button" disabled={savingHall} onClick={() => { setHallDraft(null); setRenameShape(null); }}>إلغاء</button></div></form></div>}
    {recovery && <div className="vs-modal-backdrop"><div className="vs-modal"><h2>توجد مسودة محلية غير محفوظة</h2><p>يمكنك استعادتها أو متابعة نسخة الخادم. إذا كانت نسخة الخادم أحدث، تبقى المسودة قابلة للتصدير دون الكتابة فوقها.</p><div className="vs-modal-actions"><button className="vs-save-button" onClick={restore}>استعادة المسودة</button><button onClick={() => download(recovery.json, 'application/json', `site-${venueId}-recovery.json`)}>تنزيل المسودة</button><button onClick={() => { try { localStorage.removeItem(storageKey); } catch { /* Storage may be unavailable. */ } setRecovery(null); }}>استخدام نسخة الخادم</button></div></div></div>}
    {showHelp && <div className="vs-modal-backdrop" onClick={event => { if (event.target === event.currentTarget) setShowHelp(false); }}><div className="vs-modal vs-help-modal"><div className="vs-modal-header"><HelpCircle size={24} /><h2>ارسم كما تفكر</h2><button onClick={() => setShowHelp(false)}><X size={18} /></button></div><div className="vs-help-row"><Pentagon size={23} /><div><strong>قاعات بكل الأشكال</strong><p>اختر القاعة B ثم اسحب لرسم مستطيل، أو اختر نقاط حرة لرسم قاعة بأي زوايا. أغلق الحدود بالنقر على أول نقطة أو بالنقر الأيمن.</p></div></div><div className="vs-help-row"><Columns3 size={23} /><div><strong>تقسيمات داخلية</strong><p>اختر الجدار W ثم اسحب من بدايته إلى نهايته. لرسم جدار متعدد الزوايا اختر عدة نقاط. اضغط Esc للعودة للتحريك.</p></div></div><div className="vs-help-row"><Pentagon size={23} /><div><strong>تعديل الزوايا A</strong><p>انقر القاعة مرتين لفتح الزوايا. اسحب الزوايا أو انقر + لإضافة نقطة، ثم Esc للعودة للتحريك. انقر النص مرتين لتحريره، أو استخدم النقر الأيمن للوصول للأوامر.</p></div></div><div className="vs-shortcut-grid">{[['Ctrl Z / Y', 'تراجع / إعادة'], ['Ctrl C / V', 'نسخ / لصق'], ['Ctrl G', 'تجميع'], ['Ctrl Shift G', 'فك التجميع'], ['Ctrl S', 'حفظ'], ['Space + سحب', 'تحريك اللوحة'], ['عجلة الفأرة', 'تكبير حول المؤشر'], ['Shift / Alt', 'تقييد الرسم / تجاوز الالتقاط']].map(([key, text]) => <div key={key}><span>{text}</span><kbd>{key}</kbd></div>)}</div></div></div>}
    <div ref={printRef} className="vs-print-sheet"><header><h1>{name}</h1><p>مخطط الموقع والقاعات · {doc.bounds.width} × {doc.bounds.height} م · {dirty ? 'مسودة' : 'نسخة محفوظة'}</p></header><SiteDrawing document={doc} /><footer>المساحة الفعلية للقاعات: {round(doc.shapes.filter(s => s.kind === 'hall').reduce((n, s) => n + polygonArea(s.points), 0))} م²</footer></div>
  </div>;
};





