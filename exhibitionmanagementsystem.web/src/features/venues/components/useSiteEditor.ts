import { useEffect, useLayoutEffect, useReducer, useRef, useState, type MouseEvent as MouseInput, type PointerEvent as PointerInput } from 'react';
import { clampPoint, directEditAction, distance, editable, emptyDocument, expandSelection, historyReducer, insidePage, isClosed, makeShape, marqueeIds, minimumPageBounds, moveShapes, rectanglePoints, replaceWorldPoints, resizePage, resizeShape, round, shapeBounds, snap, validateDocument, worldPoints, type EditorDocument, type EditorShape, type Point, type ShapeKind, type Tool } from '../utils/siteEditor';
import { alignShapes, snapDrawingPoint, snapShapeMove, type AlignmentGuide, type AlignmentMode } from '../utils/siteAlignment';
export type Camera = Point & { zoom: number };
type Gesture = { kind: 'pan'; start: Point; camera: Camera } | { kind: 'marquee'; start: Point; additive: boolean }
  | { kind: 'pageResize'; start: Point; before: EditorDocument; edge: 'e' | 's' | 'se' }
  | { kind: 'move'; start: Point; before: EditorDocument; ids: string[] }
  | { kind: 'resize' | 'rotate' | 'node'; before: EditorDocument; shape: EditorShape; index: number }
  | { kind: 'draw'; before: EditorDocument; tool: ShapeKind; start: Point; points: Point[] };
export const shortcuts: Partial<Record<Tool, string>> = { select: 'V', nodes: 'A', pan: 'H', hall: 'B', wall: 'W', line: 'L', rectangle: 'R', ellipse: 'E', freehand: 'P', text: 'T', dimension: 'D' };
export const useSiteEditor = (notify: (message: string) => void, onHall: (shape: EditorShape) => void, blocked: boolean, onRenameHall?: (shape: EditorShape) => void, options?: { prepareDocument?: (next: EditorDocument, previous: EditorDocument) => EditorDocument; allowedTools?: Tool[]; selectionFilter?: (shape: EditorShape) => boolean }) => {
  const [history, dispatch] = useReducer(historyReducer, { document: emptyDocument(), past: [], future: [] });
  const [preview, setPreviewState] = useState<EditorDocument | null>(null), previewRef = useRef<EditorDocument | null>(null);
  const doc = preview ?? history.document;
  const [selection, setSelection] = useState<string[]>([]), [tool, setToolState] = useState<Tool>('select');
  const [camera, setCamera] = useState<Camera>({ x: -8, y: -8, zoom: 6 });
  const [size, setSize] = useState({ width: 0, height: 0 });
  const [cursor, setCursor] = useState<Point>({ x: 0, y: 0 }), [polyPoints, setPolyPoints] = useState<Point[]>([]);
  const [marquee, setMarquee] = useState<{ start: Point; end: Point } | null>(null);
  const [guides, setGuides] = useState<AlignmentGuide[]>([]), [node, setNode] = useState<number | null>(null);
  const [smartAlignment, setSmartAlignment] = useState(true);
  const [interacting, setInteracting] = useState(false);
  const [hallMode, setHallMode] = useState<'rectangle' | 'polygon'>('polygon'), [wallMode, setWallMode] = useState<'segment' | 'path'>('segment');
  const [editingTextId, setEditingTextId] = useState<string | null>(null);
  const svgRef = useRef<SVGSVGElement | null>(null), gesture = useRef<Gesture | null>(null), clipboard = useRef<EditorShape[]>([]), space = useRef(false);
  const lastPointerShape = useRef<string | null>(null);
  const documentRef = useRef(doc);
  useLayoutEffect(() => { documentRef.current = doc; }, [doc]);
  const selected = doc.shapes.filter(s => selection.includes(s.id)), single = selected.length === 1 ? selected[0] : null;
  const setPreview = (d: EditorDocument | null) => { previewRef.current = d; setPreviewState(d); };
  const commit = (d: EditorDocument): boolean => {
    try { dispatch({ type: 'commit', document: options?.prepareDocument ? options.prepareDocument(d, history.document) : d }); return true; }
    catch (error) { notify(error instanceof Error ? error.message : 'تعذر تطبيق التعديل.'); return false; }
  };
  const cancel = () => { gesture.current = null; setPreview(null); setInteracting(false); setMarquee(null); setGuides([]); };
  const changeTool = (t: Tool) => { if (options?.allowedTools && !options.allowedTools.includes(t)) return; cancel(); setPolyPoints([]); setNode(null); setToolState(t); };
  const fit = (target = doc, area?: ReturnType<typeof shapeBounds>) => {
    const b = area ?? { x: 0, y: 0, ...target.bounds };
    const zoom = Math.max(.15, Math.min(70, (size.width - 90) / (b.width || 1), (size.height - 85) / (b.height || 1)));
    setCamera({ x: b.x - (size.width / zoom - b.width) / 2, y: b.y - (size.height / zoom - b.height) / 2, zoom });
  };
  const worldFromClient = (x: number, y: number): Point => {
    const svg = svgRef.current, matrix = svg?.getScreenCTM()?.inverse(); if (!svg || !matrix) return { x: 0, y: 0 };
    const p = svg.createSVGPoint(); p.x = x; p.y = y; const result = p.matrixTransform(matrix); return { x: result.x, y: result.y };
  };
  const point = (e: PointerInput) => worldFromClient(e.clientX, e.clientY);
  const add = (shape: EditorShape) => {
    if (doc.layers[shape.layer].locked) { notify('افتح قفل الطبقة أولاً.'); return; }
    const check = { ...shape, hallID: shape.kind === 'hall' ? shape.hallID ?? -1 : undefined };
    const next = { ...history.document, layers: { ...doc.layers, [shape.layer]: { ...doc.layers[shape.layer], visible: true } }, shapes: [...history.document.shapes, check] };
    const error = validateDocument(next).find(i => i.id === shape.id && i.severity === 'error'); if (error) { notify(error.message); return; }
    if (shape.kind === 'hall' && !shape.hallID) { onHall(shape); return; }
    commit({ ...next, shapes: [...history.document.shapes, shape] }); setSelection([shape.id]);
    if (shape.kind === 'text') setEditingTextId(shape.id);
  };
  const finishPolygon = () => {
    if (!['hall', 'polygon', 'wall'].includes(tool)) return;
    const pts = polyPoints.filter((p, i) => !i || distance(p, polyPoints[i - 1]) > .01);
    if (pts.length > 1 && distance(pts[0], pts.at(-1)!) < .01) pts.pop();
    if (pts.length < (tool === 'wall' ? 2 : 3)) { notify('أضف نقاطاً أكثر لإكمال الرسم.'); return; }
    const shape = makeShape(tool as ShapeKind, pts, crypto.randomUUID());
    const error = validateDocument({ ...doc, shapes: [...doc.shapes, { ...shape, hallID: shape.kind === 'hall' ? -1 : undefined }] }).find(i => i.id === shape.id && i.severity === 'error');
    if (error) { notify(error.message); return; } setPolyPoints([]); add(shape); if (tool !== 'wall') setToolState('select');
  };
  const down = (e: PointerInput<SVGSVGElement>) => {
    if (blocked || (e.button !== 0 && e.button !== 1)) return;
    e.preventDefault(); e.currentTarget.focus(); const p = point(e), target = e.target as Element;
    const drawing = !['select', 'nodes', 'pan', 'freehand'].includes(tool);
    const captured = snapDrawingPoint(doc, p, camera.zoom, { enabled: smartAlignment && drawing, bypass: e.altKey, extra: polyPoints, start: polyPoints.at(-1), direction: !!polyPoints.length, shift: e.shiftKey });
    const pos = captured.point;
    lastPointerShape.current = target.closest('[data-shape-id]')?.getAttribute('data-shape-id') ?? null;
    if (space.current || tool === 'pan' || e.button === 1) gesture.current = { kind: 'pan', start: { x: e.clientX, y: e.clientY }, camera };
    else if (tool === 'select' && target.closest('[data-page-resize]')) {
      gesture.current = { kind: 'pageResize', start: p, before: doc, edge: target.closest('[data-page-resize]')!.getAttribute('data-page-resize') as 'e' | 's' | 'se' };
    }
    else if ((tool === 'hall' && hallMode === 'polygon') || tool === 'polygon' || (tool === 'wall' && wallMode === 'path')) {
      if (tool !== 'wall' && polyPoints.length >= 3 && distance(pos, polyPoints[0]) < 10 / camera.zoom) { finishPolygon(); return; }
      if (!polyPoints.length || distance(polyPoints.at(-1)!, pos) > .01) setPolyPoints([...polyPoints, pos]); setGuides([]); return;
    } else {
      const handle = target.closest('[data-handle]');
      if (handle && single && editable(single, doc)) {
        const kind = handle.getAttribute('data-handle'), index = Number(handle.getAttribute('data-index'));
        if (kind === 'insert') { const pts = worldPoints(single), a = pts[index], b = pts[(index + 1) % pts.length]; pts.splice(index + 1, 0, { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 }); commit({ ...doc, shapes: doc.shapes.map(s => s.id === single.id ? replaceWorldPoints(s, pts) : s) }); setNode(index + 1); return; }
        if (kind === 'node') setNode(index);
        gesture.current = { kind: kind === 'rotate' ? 'rotate' : kind === 'node' ? 'node' : 'resize', before: doc, shape: single, index };
      } else if (tool === 'select' || tool === 'nodes') {
        const id = target.closest('[data-shape-id]')?.getAttribute('data-shape-id');
        if (id) {
          const group = expandSelection([id], doc);
          const ids = e.shiftKey ? selection.includes(id) ? selection.filter(i => !group.includes(i)) : [...new Set([...selection, ...group])] : selection.includes(id) ? selection : group;
          setSelection(ids); setNode(null);
          if (!e.shiftKey && tool === 'select' && ids.length && doc.shapes.filter(s => ids.includes(s.id)).every(s => editable(s, doc))) gesture.current = { kind: 'move', before: doc, start: p, ids };
        } else { gesture.current = { kind: 'marquee', start: p, additive: e.shiftKey }; if (!e.shiftKey) setSelection([]); setNode(null); setMarquee({ start: p, end: p }); }
      } else if (['text', 'door', 'exit', 'column'].includes(tool)) {
        const dims = tool === 'text' ? [10, 2] : tool === 'exit' ? [3, 2] : tool === 'column' ? [1, 1] : [2, 2];
        const a = { x: Math.max(0, Math.min(pos.x, doc.bounds.width - dims[0])), y: Math.max(0, Math.min(pos.y, doc.bounds.height - dims[1])) };
        add(makeShape(tool as ShapeKind, [a, { x: a.x + dims[0], y: a.y + dims[1] }], crypto.randomUUID())); setToolState('select'); return;
      } else gesture.current = { kind: 'draw', before: doc, tool: tool as ShapeKind, start: pos, points: [pos] };
    }
    if (gesture.current) { setInteracting(true); e.currentTarget.setPointerCapture(e.pointerId); }
  };
  const move = (e: PointerInput<SVGSVGElement>) => {
    const p = point(e), pos = clampPoint(snap(p, doc, e.altKey), doc); const g = gesture.current;
    if (!g) {
      const drawing = !blocked && !editingTextId && !['select', 'nodes', 'pan', 'freehand'].includes(tool);
      const captured = snapDrawingPoint(doc, p, camera.zoom, { enabled: smartAlignment && drawing, bypass: e.altKey, extra: polyPoints, start: polyPoints.at(-1), direction: !!polyPoints.length, shift: e.shiftKey });
      setCursor(captured.point); setGuides(drawing ? captured.guides : []); return;
    }
    setCursor(pos);
    if (g.kind === 'pan') { setCamera({ ...g.camera, x: g.camera.x - (e.clientX - g.start.x) / g.camera.zoom, y: g.camera.y - (e.clientY - g.start.y) / g.camera.zoom }); return; }
    if (g.kind === 'marquee') { setMarquee({ start: g.start, end: p }); return; }
    if (g.kind === 'pageResize') {
      const minimum = minimumPageBounds(g.before), dimensions = snap({ x: g.before.bounds.width + p.x - g.start.x, y: g.before.bounds.height + p.y - g.start.y }, g.before, e.altKey);
      const width = g.edge.includes('e') ? Math.min(10000, Math.max(minimum.width, dimensions.x)) : g.before.bounds.width;
      const height = g.edge.includes('s') ? Math.min(10000, Math.max(minimum.height, dimensions.y)) : g.before.bounds.height;
      setPreview(resizePage(g.before, { width, height })); return;
    }
    if (g.kind === 'move') {
      const captured = snapShapeMove(g.before, g.ids, { x: p.x - g.start.x, y: p.y - g.start.y }, camera.zoom, smartAlignment, e.altKey);
      setGuides(captured.guides); setPreview(captured.document); return;
    }
    if (g.kind === 'draw') {
      const captured = snapDrawingPoint(g.before, p, camera.zoom, { enabled: smartAlignment && g.tool !== 'freehand', bypass: e.altKey, extra: [g.start], start: g.start, direction: ['line', 'dimension', 'wall'].includes(g.tool), shift: e.shiftKey });
      let end = captured.point;
      if (e.shiftKey && ['hall', 'rectangle', 'ellipse'].includes(g.tool)) { const d = Math.max(Math.abs(end.x - g.start.x), Math.abs(end.y - g.start.y)); end = clampPoint({ x: g.start.x + Math.sign(end.x - g.start.x || 1) * d, y: g.start.y + Math.sign(end.y - g.start.y || 1) * d }, g.before); }
      if (g.tool === 'freehand') { end = clampPoint(p, g.before); if (g.points.length < 2000 && distance(g.points.at(-1)!, end) > .08) g.points.push(end); }
      setCursor(end); setGuides(g.tool === 'freehand' || (e.shiftKey && ['hall', 'rectangle', 'ellipse'].includes(g.tool)) ? [] : captured.guides);
      const shape = makeShape(g.tool, g.tool === 'freehand' ? g.points : [g.start, end], 'draft-shape');
      if (g.tool === 'hall') shape.points = rectanglePoints(shape.width, shape.height);
      setPreview({ ...g.before, shapes: [...g.before.shapes, shape] }); return;
    }
    let shape = g.shape;
    if (g.kind === 'resize') {
      const captured = snapDrawingPoint(g.before, p, camera.zoom, { enabled: smartAlignment && !e.shiftKey, bypass: e.altKey, exclude: [shape.id] });
      shape = resizeShape(shape, g.index, captured.point, e.shiftKey);
      const corner = worldPoints({ ...shape, kind: 'rectangle' })[g.index];
      setGuides(captured.guides.filter(guide => guide.kind === 'axis' && Math.abs(corner[guide.axis] - guide.value) < .002));
    }
    if (g.kind === 'rotate') { const angle = Math.atan2(p.y - shape.y - shape.height / 2, p.x - shape.x - shape.width / 2) * 180 / Math.PI + 90, step = e.shiftKey ? 15 : 1; shape = { ...shape, rotation: ((Math.round(angle / step) * step) % 360 + 360) % 360 }; }
    if (g.kind === 'node') {
      const pts = worldPoints(shape), start = pts[g.index > 0 ? g.index - 1 : isClosed(shape) ? pts.length - 1 : 1];
      const captured = snapDrawingPoint(g.before, p, camera.zoom, { enabled: smartAlignment, bypass: e.altKey, exclude: [shape.id], extra: pts.filter((_, i) => i !== g.index), start, direction: true, shift: e.shiftKey });
      pts[g.index] = captured.point; shape = replaceWorldPoints(shape, pts); setCursor(captured.point); setGuides(captured.guides);
    }
    if (insidePage(shape, g.before)) setPreview({ ...g.before, shapes: g.before.shapes.map(s => s.id === shape.id ? shape : s) }); else setGuides([]);
  };
  const up = (e: PointerInput<SVGSVGElement>) => {
    const g = gesture.current;
    if (g?.kind === 'marquee') { const ids = marqueeIds(doc, g.start, point(e)).filter(id => !options?.selectionFilter || options.selectionFilter(doc.shapes.find(s=>s.id===id)!)); setSelection(expandSelection(g.additive ? [...new Set([...selection, ...ids])] : ids, doc)); }
    else if (g && previewRef.current) {
      const next = previewRef.current;
      if (g.kind === 'draw') { const s = next.shapes.at(-1)!; if (s.points.length > 1 && (s.width > .15 || s.height > .15)) { add({ ...s, id: crypto.randomUUID() }); if (!['wall', 'freehand'].includes(g.tool)) setToolState('select'); } }
      else { const error = validateDocument(next).find(i => i.severity === 'error' && i.id === ('shape' in g ? g.shape.id : '')); if (error) notify(error.message); else commit(next); }
    }
    cancel(); if (e.currentTarget.hasPointerCapture(e.pointerId)) e.currentTarget.releasePointerCapture(e.pointerId);
  };
  const patch = (changes: Partial<EditorShape>) => {
    if (!single || (Object.keys(changes).some(k => k !== 'locked') && !editable(single, doc))) return;
    if (doc.layers[single.layer].locked || (changes.layer && doc.layers[changes.layer].locked)) { notify('افتح قفل الطبقة أولاً.'); return; }
    let s = { ...single, ...changes };
    if (Object.keys(changes).length === 1 && changes.locked !== undefined) { commit({ ...doc, shapes: doc.shapes.map(item => item.id === s.id ? s : item) }); return; }
    if (changes.width !== undefined || changes.height !== undefined) s = { ...s, points: single.points.map(p => ({ x: round(p.x / single.width * s.width), y: round(p.y / single.height * s.height) })) };
    const next = { ...doc, shapes: doc.shapes.map(item => item.id === s.id ? s : item) }, error = validateDocument(next).find(i => i.id === s.id && i.severity === 'error'); if (error) notify(error.message); else commit(next);
  };
  const editLabel = (shape = single) => {
    if (!shape || !editable(shape, doc)) return;
    cancel(); setSelection([shape.id]);
    if (shape.kind === 'hall') onRenameHall?.(shape);
    else if (['text', 'rectangle', 'ellipse'].includes(shape.kind)) setEditingTextId(shape.id);
  };
  const commitLabel = (id: string, label: string) => {
    const current = documentRef.current, shape = current.shapes.find(s => s.id === id);
    if (shape && editable(shape, current)) commit({ ...current, shapes: current.shapes.map(s => s.id === id ? { ...s, label: label.slice(0, 200) } : s) });
    setEditingTextId(null); svgRef.current?.focus();
  };
  const doubleClick = (e: MouseInput<SVGSVGElement>) => {
    if (blocked || tool === 'pan') return;
    if (polyPoints.length) { finishPolygon(); return; }
    if ((e.target as Element).closest('[data-handle]')) return;
    // Pointer capture can retarget the synthetic double-click to the root SVG.
    const id = (e.target as Element).closest('[data-shape-id]')?.getAttribute('data-shape-id') ?? lastPointerShape.current, shape = doc.shapes.find(s => s.id === id);
    if (!shape) return;
    const action = directEditAction(shape, doc); if (!action) return;
    e.preventDefault(); cancel(); setSelection([shape.id]);
    if (action === 'nodes') setToolState('nodes'); else editLabel(shape);
  };
  const remove = () => {
    if (tool === 'nodes' && single && node !== null && editable(single, doc)) {
      const pts = worldPoints(single); if (pts.length <= (isClosed(single) ? 3 : 2)) return; pts.splice(node, 1);
      const s = replaceWorldPoints(single, pts), next = { ...doc, shapes: doc.shapes.map(item => item.id === s.id ? s : item) };
      const error = validateDocument(next).find(i => i.id === s.id && i.severity === 'error'); if (error) notify(error.message); else { commit(next); setNode(null); } return;
    }
    const ids = new Set(selected.filter(s => editable(s, doc)).map(s => s.id)); commit({ ...doc, shapes: doc.shapes.filter(s => !ids.has(s.id)) }); setSelection(selection.filter(id => !ids.has(id)));
  };
  const copy = () => { clipboard.current = structuredClone(selected.filter(s => s.kind !== 'hall')); if (selected.some(s => s.kind === 'hall')) notify('لكل قاعة سجل مستقل. استخدم رسم قاعة جديدة لنسخ حدودها إلى قاعة أخرى.'); };
  const paste = () => {
    const groups = new Map<string, string>(); const copies = clipboard.current.filter(s => !doc.layers[s.layer].locked).map(s => {
      if (s.groupId && !groups.has(s.groupId)) groups.set(s.groupId, crypto.randomUUID());
      return { ...structuredClone(s), id: crypto.randomUUID(), x: s.x + doc.grid.size, y: s.y + doc.grid.size, locked: false, groupId: s.groupId ? groups.get(s.groupId) : undefined };
    }).filter(s => insidePage(s, doc));
    if (!copies.length) return; commit({ ...doc, shapes: [...doc.shapes, ...copies] }); setSelection(copies.map(s => s.id)); clipboard.current = structuredClone(copies);
  };
  const group = (ungroup = false) => { if (!selected.length || selected.some(s => !editable(s, doc))) return; const groupId = ungroup ? undefined : crypto.randomUUID(); commit({ ...doc, shapes: doc.shapes.map(s => selection.includes(s.id) ? { ...s, groupId } : s) }); };
  const align = (mode: AlignmentMode) => {
    if (interacting || blocked) return;
    const next = alignShapes(doc, selection, mode);
    if (next === doc && selected.length >= 3 && ['horizontal', 'vertical'].includes(mode)) notify('التوزيع يحتاج مساحة كافية بين أول وآخر عنصر. أبعدهما ثم أعد المحاولة.');
    commit(next);
  };
  const order = (front: boolean) => { const ids = new Set(selected.filter(s => editable(s, doc)).map(s => s.id)), chosen = doc.shapes.filter(s => ids.has(s.id)), rest = doc.shapes.filter(s => !ids.has(s.id)); commit({ ...doc, shapes: front ? [...rest, ...chosen] : [...chosen, ...rest] }); };
  const zoom = (factor: number, anchor = { x: size.width / 2, y: size.height / 2 }) => setCamera(c => { const z = Math.max(.15, Math.min(100, c.zoom * factor)); return { x: c.x + anchor.x / c.zoom - anchor.x / z, y: c.y + anchor.y / c.zoom - anchor.y / z, zoom: z }; });
  const undo = (redo = false) => { cancel(); setPolyPoints([]); dispatch({ type: redo ? 'redo' : 'undo' }); };
  const keyDown = (e: KeyboardEvent) => {
    if ((e.target as HTMLElement)?.closest('input,textarea,select,[contenteditable="true"]') || blocked || editingTextId) return;
    const ctrl = e.ctrlKey || e.metaKey, key = e.key.toLowerCase();
    if (e.code === 'Space') { e.preventDefault(); space.current = true; }
    if (key === 'escape') { cancel(); setPolyPoints([]); setNode(null); if (tool === 'select') setSelection([]); setToolState('select'); }
    else if (key === 'f2') { e.preventDefault(); editLabel(); }
    else if (key === 'enter' && polyPoints.length) { e.preventDefault(); finishPolygon(); }
    else if (key === 'backspace' && polyPoints.length) { e.preventDefault(); setPolyPoints(p => p.slice(0, -1)); }
    else if (ctrl && (key === 'z' || key === 'y')) { e.preventDefault(); undo(key === 'y' || e.shiftKey); }
    else if (ctrl && key === 'a') { e.preventDefault(); setSelection(doc.shapes.filter(s => doc.layers[s.layer].visible && (!options?.selectionFilter || options.selectionFilter(s))).map(s => s.id)); }
    else if (ctrl && key === 'c') { e.preventDefault(); copy(); }
    else if (ctrl && key === 'v') { e.preventDefault(); paste(); }
    else if (ctrl && key === 'd') { e.preventDefault(); copy(); paste(); }
    else if (ctrl && key === 'g') { e.preventDefault(); group(e.shiftKey); }
    else if (key === 'delete' || key === 'backspace') { e.preventDefault(); remove(); }
    else if (key.startsWith('arrow') && selection.length) { e.preventDefault(); const n = (doc.grid.snap ? doc.grid.size : .1) * (e.shiftKey ? 10 : 1); commit(moveShapes(doc, selection, { x: key === 'arrowleft' ? -n : key === 'arrowright' ? n : 0, y: key === 'arrowup' ? -n : key === 'arrowdown' ? n : 0 })); }
    else if (!ctrl && !e.altKey) { const entry = Object.entries(shortcuts).find(([, letter]) => letter?.toLowerCase() === key); if (entry) changeTool(entry[0] as Tool); }
  };
  // Event closures deliberately refresh with each committed/preview state.
  useEffect(() => { const up = (e: KeyboardEvent) => { if (e.code === 'Space') space.current = false; }, blur = () => { space.current = false; }; window.addEventListener('keydown', keyDown); window.addEventListener('keyup', up); window.addEventListener('blur', blur); return () => { window.removeEventListener('keydown', keyDown); window.removeEventListener('keyup', up); window.removeEventListener('blur', blur); }; });
  const changeDocument = (next: EditorDocument): boolean => {
    if (interacting || polyPoints.length || editingTextId) return false;
    try {
      const resized = next.bounds.width !== doc.bounds.width || next.bounds.height !== doc.bounds.height ? resizePage(doc, next.bounds) : next;
      return commit({ ...next, bounds: resized.bounds });
    } catch (error) { notify(error instanceof Error ? error.message : 'تعذر تغيير مساحة الرسم.'); return false; }
  };
  return { history, dispatch, doc, documentRef, selection, setSelection, selected, single, tool, changeTool, camera, setCamera, size, setSize, svgRef, cursor, polyPoints, setPolyPoints, finishPolygon, marquee, guides, smartAlignment, toggleSmartAlignment: () => { setSmartAlignment(v => !v); setGuides([]); }, node, interacting, cancel, fit, worldFromClient, down, move, up, patch, remove, copy, paste, group, align, order, zoom, undo, add, commit, changeDocument, hallMode, setHallMode, wallMode, setWallMode, editingTextId, setEditingTextId, editLabel, commitLabel, doubleClick };
};
