// Geometry and document operations are independent of React and screen coordinates.
export type Point = { x: number; y: number };
export type Bounds = Point & { width: number; height: number };
export type LayerId = 'halls' | 'structure' | 'movement' | 'annotations';
export type ShapeKind = 'hall' | 'wall' | 'line' | 'rectangle' | 'ellipse' | 'polygon' | 'freehand' | 'text' | 'door' | 'dimension' | 'exit' | 'column';
export type Tool = 'select' | 'nodes' | 'pan' | ShapeKind;
export interface EditorShape extends Bounds {
  id: string;
  kind: ShapeKind;
  layer: LayerId;
  rotation: number;
  points: Point[];
  label: string;
  fill: string;
  stroke: string;
  strokeWidth: number;
  fontSize: number;
  locked: boolean;
  hallID?: number;
  groupId?: string;
}
export interface HallSource { hallID: number; hallName: string; floorPlanWidth?: number; floorPlanHeight?: number; isActive: boolean }
export interface EditorDocument {
  schemaVersion: 4;
  unit: 'meter';
  bounds: { width: number; height: number };
  grid: { size: number; snap: boolean; visible: boolean };
  layers: Record<LayerId, { visible: boolean; locked: boolean }>;
  shapes: EditorShape[];
}
export const layers: { id: LayerId; label: string; color: string }[] = [
  { id: 'halls', label: 'القاعات', color: '#2563eb' },
  { id: 'structure', label: 'الجدران والأبواب', color: '#475569' },
  { id: 'movement', label: 'المسارات والمخارج', color: '#059669' },
  { id: 'annotations', label: 'النصوص والقياسات', color: '#d97706' },
];
export const kindLabels: Record<ShapeKind, string> = { hall: 'قاعة', wall: 'جدار', line: 'خط', rectangle: 'مستطيل', ellipse: 'دائرة / بيضاوي', polygon: 'مضلع', freehand: 'رسم حر', text: 'نص', door: 'باب', dimension: 'قياس', exit: 'مخرج', column: 'عمود' };
export const round = (n: number) => Math.round(n * 1000) / 1000;
export const distance = (a: Point, b: Point) => Math.hypot(b.x - a.x, b.y - a.y);
export const emptyDocument = (): EditorDocument => ({
  schemaVersion: 4, unit: 'meter', bounds: { width: 100, height: 75 },
  grid: { size: 1, snap: true, visible: true },
  layers: Object.fromEntries(layers.map(l => [l.id, { visible: true, locked: false }])) as EditorDocument['layers'], shapes: [],
});
export const snap = (p: Point, doc: EditorDocument, bypass = false): Point => {
  const size = doc.grid.snap && !bypass ? doc.grid.size : 0.01;
  return { x: round(Math.round(p.x / size) * size), y: round(Math.round(p.y / size) * size) };
};
export const clampPoint = (p: Point, doc: EditorDocument): Point => ({ x: Math.max(0, Math.min(doc.bounds.width, p.x)), y: Math.max(0, Math.min(doc.bounds.height, p.y)) });
export const pointsBounds = (points: Point[]): Bounds => {
  const xs = points.map(p => p.x), ys = points.map(p => p.y);
  const x = Math.min(...xs), y = Math.min(...ys);
  return { x, y, width: Math.max(...xs) - x, height: Math.max(...ys) - y };
};
export const rectanglePoints = (w: number, h: number): Point[] => [{ x: 0, y: 0 }, { x: w, y: 0 }, { x: w, y: h }, { x: 0, y: h }];
export const makeShape = (kind: ShapeKind, worldPoints: Point[], id: string): EditorShape => {
  const b = pointsBounds(worldPoints);
  const layer: LayerId = kind === 'hall' ? 'halls' : ['text', 'dimension'].includes(kind) ? 'annotations' : kind === 'exit' ? 'movement' : 'structure';
  const shape: EditorShape = {
    id, kind, layer, ...b, width: Math.max(0.01, b.width), height: Math.max(0.01, b.height), rotation: 0,
    points: worldPoints.map(p => ({ x: round(p.x - b.x), y: round(p.y - b.y) })),
    label: kind === 'text' ? 'نص جديد' : kind === 'hall' ? 'قاعة جديدة' : '',
    fill: kind === 'hall' ? '#eff6ff' : kind === 'exit' ? '#dcfce7' : kind === 'column' ? '#cbd5e1' : '#ffffff',
    stroke: kind === 'hall' ? '#2563eb' : kind === 'exit' ? '#059669' : '#334155',
    strokeWidth: kind === 'wall' ? 0.3 : 0.12, fontSize: kind === 'hall' ? 1.6 : 1.2, locked: false,
  };
  if (['rectangle', 'ellipse', 'text', 'door', 'exit', 'column'].includes(kind)) shape.points = rectanglePoints(shape.width, shape.height);
  return shape;
};
export const isPath = (s: EditorShape) => ['hall', 'polygon', 'wall', 'line', 'dimension', 'freehand'].includes(s.kind);
export const isClosed = (s: EditorShape) => s.kind === 'hall' || s.kind === 'polygon';
export const localToWorld = (s: EditorShape, p: Point): Point => {
  const a = s.rotation * Math.PI / 180, dx = p.x - s.width / 2, dy = p.y - s.height / 2;
  return { x: s.x + s.width / 2 + dx * Math.cos(a) - dy * Math.sin(a), y: s.y + s.height / 2 + dx * Math.sin(a) + dy * Math.cos(a) };
};
export const worldToLocal = (s: EditorShape, p: Point): Point => {
  const a = -s.rotation * Math.PI / 180, dx = p.x - s.x - s.width / 2, dy = p.y - s.y - s.height / 2;
  return { x: s.width / 2 + dx * Math.cos(a) - dy * Math.sin(a), y: s.height / 2 + dx * Math.sin(a) + dy * Math.cos(a) };
};
export const worldPoints = (s: EditorShape): Point[] => (isPath(s) ? s.points : rectanglePoints(s.width, s.height)).map(p => localToWorld(s, p));
export const shapeBounds = (s: EditorShape): Bounds => {
  if (s.kind === 'ellipse' || s.kind === 'column') {
    const a = s.rotation * Math.PI / 180, rx = s.width / 2, ry = s.height / 2;
    const w = Math.hypot(rx * Math.cos(a), ry * Math.sin(a)), h = Math.hypot(rx * Math.sin(a), ry * Math.cos(a));
    return { x: s.x + rx - w, y: s.y + ry - h, width: w * 2, height: h * 2 };
  }
  return pointsBounds(worldPoints(s));
};
export const unionBounds = (shapes: EditorShape[]): Bounds => pointsBounds(shapes.flatMap(s => {
  const b = shapeBounds(s); return [{ x: b.x, y: b.y }, { x: b.x + b.width, y: b.y + b.height }];
}));
export const polygonArea = (p: Point[]): number => Math.abs(p.reduce((sum, a, i) => { const b = p[(i + 1) % p.length]; return sum + a.x * b.y - b.x * a.y; }, 0)) / 2;
export const pathLength = (p: Point[], closed = false) => p.reduce((n, point, i) => n + (i ? distance(p[i - 1], point) : 0), 0) + (closed && p.length ? distance(p[0], p[p.length - 1]) : 0);
const cross = (a: Point, b: Point, c: Point) => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
const onSegment = (a: Point, b: Point, p: Point) => Math.abs(cross(a, b, p)) < 1e-8 && p.x >= Math.min(a.x, b.x) - 1e-8 && p.x <= Math.max(a.x, b.x) + 1e-8 && p.y >= Math.min(a.y, b.y) - 1e-8 && p.y <= Math.max(a.y, b.y) + 1e-8;
const segmentsIntersect = (a: Point, b: Point, c: Point, d: Point) => {
  const ac = cross(a, b, c), ad = cross(a, b, d), ca = cross(c, d, a), cb = cross(c, d, b);
  return (ac * ad < 0 && ca * cb < 0) || onSegment(a, b, c) || onSegment(a, b, d) || onSegment(c, d, a) || onSegment(c, d, b);
};
export const selfIntersects = (p: Point[]): boolean => {
  for (let i = 0; i < p.length; i++) for (let j = i + 1; j < p.length; j++) {
    if (j === i + 1 || (i === 0 && j === p.length - 1)) continue;
    if (segmentsIntersect(p[i], p[(i + 1) % p.length], p[j], p[(j + 1) % p.length])) return true;
  }
  return false;
};
export const pointInPolygon = (p: Point, polygon: Point[]): boolean => {
  let inside = false;
  for (let i = 0, j = polygon.length - 1; i < polygon.length; j = i++) {
    const a = polygon[i], b = polygon[j];
    if (onSegment(a, b, p)) return true;
    if (((a.y > p.y) !== (b.y > p.y)) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
  }
  return inside;
};
export const insidePage = (s: EditorShape, doc: EditorDocument) => {
  const b = shapeBounds(s), e = 0.00001;
  return b.x >= -e && b.y >= -e && b.x + b.width <= doc.bounds.width + e && b.y + b.height <= doc.bounds.height + e;
};
export const minimumPageBounds = (doc: EditorDocument) => {
  const required = doc.shapes.reduce((result, shape) => {
    const b = shapeBounds(shape);
    return { width: Math.max(result.width, b.x + b.width), height: Math.max(result.height, b.y + b.height) };
  }, { width: 5, height: 5 });
  return { width: Math.ceil(required.width * 1000) / 1000, height: Math.ceil(required.height * 1000) / 1000 };
};
export const resizePage = (doc: EditorDocument, bounds: EditorDocument['bounds']): EditorDocument => {
  if (![bounds.width, bounds.height].every(n => Number.isFinite(n) && n >= 5 && n <= 10000)) throw new Error('أبعاد مساحة الرسم يجب أن تكون بين 5 و10000 متر.');
  const next = { width: round(bounds.width), height: round(bounds.height) }, minimum = minimumPageBounds(doc);
  // Expansion remains possible even when an imported drawing already has geometric errors.
  if ((next.width < doc.bounds.width && next.width < minimum.width) || (next.height < doc.bounds.height && next.height < minimum.height)) throw new Error('هذه الأبعاد ستقصّ عناصر المخطط. وسّع المساحة أو حرّك العناصر قبل تصغيرها.');
  return { ...doc, bounds: next };
};
export const editable = (s: EditorShape, doc: EditorDocument) => !s.locked && !doc.layers[s.layer].locked;
export const expandSelection = (ids: string[], doc: EditorDocument): string[] => {
  const groups = new Set(doc.shapes.filter(s => ids.includes(s.id) && s.groupId).map(s => s.groupId));
  return doc.shapes.filter(s => ids.includes(s.id) || (s.groupId && groups.has(s.groupId))).map(s => s.id);
};
export const marqueeIds = (doc: EditorDocument, start: Point, end: Point): string[] => {
  const b = pointsBounds([start, end]), crossing = end.x < start.x;
  return expandSelection(doc.shapes.filter(s => {
    if (!doc.layers[s.layer].visible) return false;
    const r = shapeBounds(s);
    return crossing ? r.x <= b.x + b.width && r.x + r.width >= b.x && r.y <= b.y + b.height && r.y + r.height >= b.y
      : r.x >= b.x && r.y >= b.y && r.x + r.width <= b.x + b.width && r.y + r.height <= b.y + b.height;
  }).map(s => s.id), doc);
};
export const directEditAction = (shape: EditorShape, doc: EditorDocument): 'nodes' | 'label' | null => {
  if (!editable(shape, doc)) return null;
  return isPath(shape) ? 'nodes' : ['text', 'rectangle', 'ellipse'].includes(shape.kind) ? 'label' : null;
};
export const moveShapes = (doc: EditorDocument, ids: string[], delta: Point): EditorDocument => {
  const selected = doc.shapes.filter(s => ids.includes(s.id) && editable(s, doc));
  if (!selected.length) return doc;
  const b = unionBounds(selected);
  const dx = round(Math.max(-b.x, Math.min(doc.bounds.width - b.x - b.width, delta.x)));
  const dy = round(Math.max(-b.y, Math.min(doc.bounds.height - b.y - b.height, delta.y)));
  const moveIds = new Set(selected.map(s => s.id));
  return { ...doc, shapes: doc.shapes.map(s => moveIds.has(s.id) ? { ...s, x: round(s.x + dx), y: round(s.y + dy) } : s) };
};
// The opposite corner remains fixed in world coordinates, including at arbitrary angles.
export const resizeShape = (s: EditorShape, corner: number, pointer: Point, uniform = false): EditorShape => {
  const local = worldToLocal(s, pointer), west = corner === 0 || corner === 3, north = corner === 0 || corner === 1;
  const fixed = { x: west ? s.width : 0, y: north ? s.height : 0 }, fixedWorld = localToWorld(s, fixed);
  let w = Math.max(0.2, west ? fixed.x - local.x : local.x - fixed.x);
  let h = Math.max(0.2, north ? fixed.y - local.y : local.y - fixed.y);
  if (uniform) { const scale = Math.max(w / s.width, h / s.height); w = s.width * scale; h = s.height * scale; }
  const next = { ...s, width: round(w), height: round(h), points: s.points.map(p => ({ x: round(p.x / s.width * w), y: round(p.y / s.height * h) })) };
  const nextFixed = localToWorld(next, { x: west ? w : 0, y: north ? h : 0 });
  next.x = round(next.x + fixedWorld.x - nextFixed.x); next.y = round(next.y + fixedWorld.y - nextFixed.y);
  return next;
};
export const replaceWorldPoints = (s: EditorShape, points: Point[]): EditorShape => {
  const b = pointsBounds(points);
  return { ...s, ...b, width: Math.max(0.01, b.width), height: Math.max(0.01, b.height), rotation: 0, points: points.map(p => ({ x: round(p.x - b.x), y: round(p.y - b.y) })) };
};
export interface EditorIssue { id?: string; severity: 'error' | 'warning'; message: string }
export const validateDocument = (doc: EditorDocument): EditorIssue[] => {
  const issues: EditorIssue[] = [], ids = new Set<string>(), hallIds = new Set<number>();
  if (![doc.bounds.width, doc.bounds.height].every(n => Number.isFinite(n) && n > 0 && n <= 10000)) issues.push({ severity: 'error', message: 'أبعاد الموقع يجب أن تكون بين 1 و10000 متر.' });
  if (!Number.isFinite(doc.grid.size) || doc.grid.size < 0.1 || doc.grid.size > 10) issues.push({ severity: 'error', message: 'حجم الشبكة غير صالح.' });
  if (doc.shapes.length > 3000) issues.push({ severity: 'error', message: 'تجاوز المخطط الحد الأقصى: 3000 عنصر.' });
  for (const s of doc.shapes) {
    const fail = (message: string) => issues.push({ id: s.id, severity: 'error', message: `${s.label || kindLabels[s.kind]}: ${message}` });
    if (!s.id || s.id.length > 100 || ids.has(s.id)) fail('معرف غير صالح أو مكرر.'); ids.add(s.id);
    if (![s.x, s.y, s.width, s.height, s.rotation, s.strokeWidth, s.fontSize].every(Number.isFinite) || s.width <= 0 || s.height <= 0 || s.rotation < 0 || s.rotation >= 360 || s.strokeWidth < 0.02 || s.strokeWidth > 2 || s.fontSize < 0.2 || s.fontSize > 10) { fail('قياسات غير صالحة.'); continue; }
    if (!insidePage(s, doc)) fail('خارج حدود الموقع.');
    if (s.points.length > 2000 || s.points.some(p => !Number.isFinite(p.x) || !Number.isFinite(p.y))) { fail('نقاط الرسم غير صالحة.'); continue; }
    if (isClosed(s) && (s.points.length < 3 || s.points.length > 512 || polygonArea(s.points) < 0.1 || selfIntersects(s.points))) fail('المضلع متقاطع أو مساحته صفر أو تجاوز 512 زاوية؛ عدّل الزوايا.');
    if (isPath(s) && !isClosed(s) && (s.points.length < 2 || pathLength(s.points) < .1)) fail('المسار يجب أن يحتوي نقطتين وطولاً موجباً.');
    if (s.label.length > 200) fail('التسمية تتجاوز 200 حرف.');
    if (s.points.some(p => p.x < -.005 || p.y < -.005 || p.x > s.width + .005 || p.y > s.height + .005)) fail('النقاط لا تتطابق مع حدود العنصر.');
    if (s.kind === 'hall') {
      if (!s.hallID || hallIds.has(s.hallID)) fail('يجب ربط القاعة بسجل مستقل.');
      if (s.hallID) hallIds.add(s.hallID);
    }
  }
  const halls = doc.shapes.filter(s => s.kind === 'hall');
  for (let i = 0; i < halls.length; i++) for (let j = i + 1; j < halls.length; j++) {
    if (polygonsOverlap(worldPoints(halls[i]), worldPoints(halls[j]))) issues.push({ id: halls[j].id, severity: 'warning', message: `حدود «${halls[j].label}» تتداخل مع «${halls[i].label}».` });
  }
  return issues;
};
const polygonsOverlap = (a: Point[], b: Point[]) => {
  const boundsA = pointsBounds(a), boundsB = pointsBounds(b);
  if (boundsA.x + boundsA.width <= boundsB.x || boundsB.x + boundsB.width <= boundsA.x || boundsA.y + boundsA.height <= boundsB.y || boundsB.y + boundsB.height <= boundsA.y) return false;
  const strictlyInside = (p: Point, poly: Point[]) => pointInPolygon(p, poly) && !poly.some((v,i) => onSegment(v, poly[(i+1)%poly.length], p));
  if (a.some(p => strictlyInside(p,b)) || b.some(p => strictlyInside(p,a))) return true;
  for (let i=0;i<a.length;i++) for(let j=0;j<b.length;j++) {
    const aa=a[i],ab=a[(i+1)%a.length],ba=b[j],bb=b[(j+1)%b.length];
    if (cross(aa,ab,ba)*cross(aa,ab,bb)<0 && cross(ba,bb,aa)*cross(ba,bb,ab)<0) return true;
  }
  // Coincident polygons and shared edges can overlap without a strict vertex crossing.
  for(let i=0;i<a.length;i++) {
    const p=a[i],q=a[(i+1)%a.length],len=distance(p,q);if(len<.001)continue;
    const middle={x:(p.x+q.x)/2,y:(p.y+q.y)/2},normal={x:-(q.y-p.y)/len*.001,y:(q.x-p.x)/len*.001};
    for(const sign of [-1,1]) { const probe={x:middle.x+normal.x*sign,y:middle.y+normal.y*sign};if(strictlyInside(probe,a)&&strictlyInside(probe,b))return true; }
  }
  return false;
};
export const serializeDocument = (doc: EditorDocument): string => JSON.stringify({
  ...doc,
  halls: doc.shapes.filter(s => s.kind === 'hall').map(s => {
    const b = shapeBounds(s);
    return { hallID: s.hallID, name: s.label, x: round(b.x), y: round(b.y), width: round(Math.max(b.width, 0.01)), height: round(Math.max(b.height, 0.01)) };
  }), hallDoors: [], elements: [],
});
export const hydrateDocument = (json: string, halls: HallSource[]): EditorDocument => {
  if (!json) return emptyDocument();
  const raw = JSON.parse(json);
  const doc = emptyDocument();
  const owned = new Map(halls.map(h => [h.hallID, h]));
  if (!raw || typeof raw !== 'object' || !raw.bounds) throw new Error('ملف المخطط غير صالح.');
  if (raw.schemaVersion !== undefined && ![1,2,3,4].includes(raw.schemaVersion)) throw new Error('إصدار المخطط غير مدعوم.');
  doc.bounds = { width: Number(raw.bounds.width), height: Number(raw.bounds.height) };
  if (raw.schemaVersion === 4 && Array.isArray(raw.shapes)) {
    doc.grid = { ...doc.grid, ...raw.grid };
    for (const l of layers) doc.layers[l.id] = { visible: raw.layers?.[l.id]?.visible !== false, locked: raw.layers?.[l.id]?.locked === true };
    doc.shapes = raw.shapes.filter((s: EditorShape) => s.kind !== 'hall' || owned.has(s.hallID!)).map((s: EditorShape) => {
      if (!s || !Object.hasOwn(kindLabels, s.kind) || !layers.some(l => l.id === s.layer) || !Array.isArray(s.points) || typeof s.id !== 'string') throw new Error('بنية عناصر المخطط غير صالحة.');
      return { ...s, label: s.kind === 'hall' ? owned.get(s.hallID!)!.hallName : String(s.label ?? ''), locked: s.locked === true };
    });
  } else {
    if (!Array.isArray(raw.halls)) throw new Error('ملف المخطط غير صالح.');
    for (const h of raw.halls) {
      if (!owned.has(h.hallID)) continue;
      const s = makeShape('hall', [{ x: h.x, y: h.y }, { x: h.x + h.width, y: h.y + h.height }], `hall-${h.hallID}`);
      s.points = rectanglePoints(s.width, s.height); s.hallID = h.hallID; s.label = owned.get(h.hallID)!.hallName; s.locked = h.locked === true; doc.shapes.push(s);
    }
    for (const e of raw.elements ?? []) {
      const kind: ShapeKind = e.type === 'emergencyExit' ? 'exit' : e.type === 'column' ? 'column' : 'rectangle';
      const s = makeShape(kind, [{ x: e.x, y: e.y }, { x: e.x + e.width, y: e.y + e.height }], String(e.id));
      s.rotation = Number(e.rotation) || 0; s.label = e.label ?? ''; s.locked = e.locked === true;
      if (e.type === 'wall') s.fill = '#334155';
      if (e.type === 'aisle') { s.fill = '#e0f2fe'; s.layer = 'movement'; }
      doc.shapes.push(s);
    }
    for (const d of raw.hallDoors ?? []) {
      const h = doc.shapes.find(s => s.hallID === d.hallID); if (!h) continue;
      const horizontal = d.edge === 'n' || d.edge === 's', width = Math.min(d.width, horizontal ? h.width : h.height);
      const along = d.offset * ((horizontal ? h.width : h.height) - width);
      const x = horizontal ? h.x + along : h.x + (d.edge === 'e' ? h.width : 0), y = horizontal ? h.y + (d.edge === 's' ? h.height : 0) : h.y + along;
      const s = makeShape('door', [{ x, y }, { x: x + width, y: y + width }], String(d.id));
      s.rotation = horizontal ? (d.edge === 's' ? 180 : 0) : (d.edge === 'w' ? 270 : 90); s.locked = d.locked === true; doc.shapes.push(s);
    }
  }
  // Validate structure before rendering imported content. Geometric errors remain editable.
  for (const s of doc.shapes) {
    if (![s.x, s.y, s.width, s.height, s.rotation, s.strokeWidth, s.fontSize].every(Number.isFinite) || !/^#[0-9a-f]{6}$/i.test(s.stroke) || !(s.fill === 'none' || /^#[0-9a-f]{6}$/i.test(s.fill)) || s.points.length > 2000 || s.points.some(p => !p || !Number.isFinite(p.x) || !Number.isFinite(p.y))) throw new Error('ملف المخطط يحتوي قيماً غير صالحة.');
  }
  if (doc.shapes.length > 3000 || ![doc.bounds.width, doc.bounds.height].every(n => Number.isFinite(n) && n > 0 && n <= 10000) || !Number.isFinite(doc.grid.size) || doc.grid.size < 0.1 || doc.grid.size > 10) throw new Error('حجم المخطط أو الشبكة غير صالح.');
  return doc;
};
export interface EditorHistory { document: EditorDocument; past: EditorDocument[]; future: EditorDocument[] }
export type HistoryAction = { type: 'reset'; document: EditorDocument } | { type: 'commit'; document: EditorDocument } | { type: 'renameHall'; hallID: number; name: string } | { type: 'undo' | 'redo' };
export const historyReducer = (state: EditorHistory, action: HistoryAction): EditorHistory => {
  if (action.type === 'renameHall') {
    const sync = (doc: EditorDocument) => ({ ...doc, shapes: doc.shapes.map(s => s.hallID === action.hallID ? { ...s, label: action.name } : s) });
    return { document: sync(state.document), past: state.past.map(sync), future: state.future.map(sync) };
  }
  if (action.type === 'reset') return { document: action.document, past: [], future: [] };
  if (action.type === 'commit') return JSON.stringify(state.document) === JSON.stringify(action.document) ? state : { document: action.document, past: [...state.past.slice(-79), state.document], future: [] };
  if (action.type === 'undo' && state.past.length) return { document: state.past.at(-1)!, past: state.past.slice(0, -1), future: [state.document, ...state.future] };
  if (action.type === 'redo' && state.future.length) return { document: state.future[0], past: [...state.past, state.document], future: state.future.slice(1) };
  return state;
};
