import { clampPoint, distance, editable, isClosed, isPath, moveShapes, round, shapeBounds, snap, unionBounds, worldPoints, type EditorDocument, type Point } from './siteEditor.ts';

export type AlignmentGuide = { kind: 'axis'; axis: 'x' | 'y'; value: number; reference: Point; label: string }
  | { kind: 'direction'; from: Point; to: Point; reference?: [Point, Point]; label: string };
export type AlignmentMode = 'start' | 'center' | 'end' | 'top' | 'middle' | 'bottom' | 'horizontal' | 'vertical';
export const alignmentOptions: { value: AlignmentMode; label: string }[] = [
  { value: 'start', label: 'محاذاة اليسار' }, { value: 'center', label: 'محاذاة المركز عمودياً' }, { value: 'end', label: 'محاذاة اليمين' },
  { value: 'top', label: 'محاذاة الأعلى' }, { value: 'middle', label: 'محاذاة على نفس الصف' }, { value: 'bottom', label: 'محاذاة الأسفل' },
  { value: 'horizontal', label: 'مسافات أفقية متساوية' }, { value: 'vertical', label: 'مسافات عمودية متساوية' },
];
const visibleReferences = (doc: EditorDocument, exclude: string[]) => doc.shapes.filter(s => !exclude.includes(s.id) && doc.layers[s.layer].visible);
const selectionUnits = (doc: EditorDocument, ids: string[]) => {
  const grouped = new Map<string, typeof doc.shapes>();
  for (const s of doc.shapes.filter(s => ids.includes(s.id))) {
    const key = s.groupId ? `group:${s.groupId}` : `shape:${s.id}`;
    grouped.set(key, [...(grouped.get(key) ?? []), s]);
  }
  return [...grouped.values()].map(shapes => ({ shapes, b: unionBounds(shapes) }));
};
export const alignmentUnitCount = (doc: EditorDocument, ids: string[]) => selectionUnits(doc, ids).length;
const anchors = (doc: EditorDocument, exclude: string[], extra: Point[]) => [
  ...extra, ...visibleReferences(doc, exclude).flatMap(s => {
    const b = shapeBounds(s);
    return [...worldPoints(s), { x: b.x, y: b.y }, { x: b.x + b.width / 2, y: b.y + b.height / 2 }, { x: b.x + b.width, y: b.y + b.height }];
  }),
];
const segments = (doc: EditorDocument, exclude: string[]): [Point, Point][] => visibleReferences(doc, exclude)
  .filter(s => s.kind !== 'freehand' && s.kind !== 'ellipse' && s.kind !== 'column' && s.kind !== 'door')
  .flatMap(s => { const pts = worldPoints(s); return pts.slice(0, isPath(s) && !isClosed(s) ? -1 : undefined).map((p, i): [Point, Point] => [p, pts[(i + 1) % pts.length]]); });

// Screen-space tolerance keeps the attraction radius consistent at every zoom.
// Smart references take precedence over the grid; Alt bypasses both.
export const snapDrawingPoint = (doc: EditorDocument, raw: Point, zoom: number, options: {
  enabled?: boolean; bypass?: boolean; exclude?: string[]; extra?: Point[]; start?: Point; direction?: boolean; shift?: boolean;
} = {}): { point: Point; guides: AlignmentGuide[] } => {
  const base = clampPoint(snap(raw, doc, options.bypass), doc), tolerance = 8 / zoom;
  const references = options.enabled === false || options.bypass ? [] : anchors(doc, options.exclude ?? [], options.extra ?? []);
  let result = { ...base }, guides: AlignmentGuide[] = [];
  const start = options.start;
  if (options.direction && start && distance(start, raw) > .00001 && (options.shift || distance(start, raw) * zoom >= 16)) {
    const length = distance(start, raw), angle = Math.atan2(raw.y - start.y, raw.x - start.x);
    const directions: { angle: number; reference?: [Point, Point]; label: string }[] = options.shift
      ? [{ angle: Math.round(angle / (Math.PI / 4)) * Math.PI / 4, label: 'اتجاه مقيد 45°' }]
      : options.enabled === false || options.bypass ? [] : [{ angle: 0, label: 'خط أفقي' }, { angle: Math.PI / 2, label: 'خط عمودي' },
        ...segments(doc, options.exclude ?? []).filter(([a, b]) => distance(a, b) * zoom >= 16).map(([a, b]) => ({ angle: Math.atan2(b.y - a.y, b.x - a.x), reference: [a, b] as [Point, Point], label: 'موازٍ للخط المميز' }))];
    let best = Infinity;
    for (const candidate of directions) {
      const ux = Math.cos(candidate.angle), uy = Math.sin(candidate.angle), projection = (raw.x - start.x) * ux + (raw.y - start.y) * uy;
      const error = Math.abs((raw.x - start.x) * uy - (raw.y - start.y) * ux);
      if ((!options.shift && (error > tolerance || error / length > Math.sin(Math.PI / 60))) || error >= best) continue;
      best = error; result = { x: start.x + projection * ux, y: start.y + projection * uy };
      guides = [{ kind: 'direction', from: start, to: result, reference: candidate.reference, label: candidate.label }];
    }
  }
  // A direction constraint remains exact; only capture anchors lying on its ray.
  if (guides[0]?.kind === 'direction') {
    const direction = guides[0], dx = result.x - direction.from.x, dy = result.y - direction.from.y, length = Math.hypot(dx, dy);
    let best = tolerance;
    for (const p of references) {
      const error = distance(p, raw), cross = Math.abs((p.x - direction.from.x) * dy - (p.y - direction.from.y) * dx);
      if (error < best && cross / length < .00001) { best = error; result = { ...p }; }
    }
    direction.to = result;
  } else {
    for (const axis of ['x', 'y'] as const) {
      let best = tolerance, reference: Point | undefined;
      for (const p of references) { const error = Math.abs(raw[axis] - p[axis]); if (error < best) { best = error; reference = p; } }
      if (reference) { result[axis] = reference[axis]; guides.push({ kind: 'axis', axis, value: reference[axis], reference, label: axis === 'x' ? 'على نفس العمود' : 'على نفس المستوى' }); }
    }
  }
  const clamped = clampPoint(result, doc);
  // Never claim a match when a page boundary prevents reaching it.
  if (distance(clamped, result) > .00001) guides = [];
  return { point: { x: round(clamped.x), y: round(clamped.y) }, guides };
};

export const snapShapeMove = (doc: EditorDocument, ids: string[], rawDelta: Point, zoom: number, enabled = true, bypass = false) => {
  const origin = unionBounds(doc.shapes.filter(s => ids.includes(s.id))), grid = snap({ x: origin.x + rawDelta.x, y: origin.y + rawDelta.y }, doc, bypass);
  const delta = { x: grid.x - origin.x, y: grid.y - origin.y }, guides: AlignmentGuide[] = [];
  if (enabled && !bypass) for (const axis of ['x', 'y'] as const) {
    const length = axis === 'x' ? 'width' : 'height'; let best = 8 / zoom, adjustment = 0, guide: AlignmentGuide | undefined;
    for (const s of visibleReferences(doc, ids)) {
      const b = shapeBounds(s);
      for (const fraction of [0, .5, 1]) for (const own of [0, .5, 1]) {
        const value = b[axis] + b[length] * fraction, current = origin[axis] + origin[length] * own;
        const error = Math.abs(value - current - rawDelta[axis]);
        if (error < best) { best = error; adjustment = value - current; guide = { kind: 'axis', axis, value, reference: { x: b.x + b.width * fraction, y: b.y + b.height * fraction }, label: own === .5 && fraction === .5 ? axis === 'x' ? 'مراكز على نفس العمود' : 'مراكز على نفس الصف' : axis === 'x' ? 'محاذاة عمودية' : 'محاذاة أفقية' }; }
      }
    }
    if (guide) { delta[axis] = adjustment; guides.push(guide); }
  }
  const next = moveShapes(doc, ids, delta), actual = unionBounds(next.shapes.filter(s => ids.includes(s.id)));
  return { document: next, guides: guides.filter(g => g.kind === 'axis' && [0, .5, 1].some(f => Math.abs(actual[g.axis] + actual[g.axis === 'x' ? 'width' : 'height'] * f - g.value) < .002)) };
};

export const alignShapes = (doc: EditorDocument, ids: string[], mode: AlignmentMode): EditorDocument => {
  const selected = doc.shapes.filter(s => ids.includes(s.id));
  if (selected.length < 2 || selected.some(s => !editable(s, doc) || !doc.layers[s.layer].visible)) return doc;
  const units = selectionUnits(doc, ids);
  if (units.length < 2) return doc;
  const vertical = ['top', 'middle', 'bottom', 'vertical'].includes(mode), axis = vertical ? 'y' : 'x', length = vertical ? 'height' : 'width';
  const area = unionBounds(selected), deltas = new Map<string, number>();
  if (mode === 'horizontal' || mode === 'vertical') {
    if (units.length < 3) return doc;
    const sorted = units.sort((a, b) => a.b[axis] - b.b[axis]);
    const first = sorted[0], last = sorted.at(-1)!;
    const gap = (last.b[axis] - first.b[axis] - sorted.slice(0, -1).reduce((sum, item) => sum + item.b[length], 0)) / (sorted.length - 1);
    if (gap < 0) return doc;
    let position = first.b[axis] + first.b[length] + gap;
    for (const item of sorted.slice(1, -1)) { for (const s of item.shapes) deltas.set(s.id, position - item.b[axis]); position += item.b[length] + gap; }
  } else {
    const fraction = ['center', 'middle'].includes(mode) ? .5 : ['end', 'bottom'].includes(mode) ? 1 : 0;
    for (const unit of units) for (const s of unit.shapes) deltas.set(s.id, area[axis] + area[length] * fraction - unit.b[axis] - unit.b[length] * fraction);
  }
  return { ...doc, shapes: doc.shapes.map(s => deltas.has(s.id) ? { ...s, [axis]: round(s[axis] + deltas.get(s.id)!) } : s) };
};
