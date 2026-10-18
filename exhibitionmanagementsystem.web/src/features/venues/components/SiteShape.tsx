import { memo, useId } from 'react';
import { distance, isClosed, isPath, pointInPolygon, type EditorDocument, type EditorShape, type Point } from '../utils/siteEditor';

const pathData = (points: Point[], closed: boolean) => points.map((p, i) => `${i ? 'L' : 'M'} ${p.x} ${p.y}`).join(' ') + (closed ? ' Z' : '');
const labelPoint = (s: EditorShape): Point => {
  const center = { x: s.width / 2, y: s.height / 2 };
  if (s.kind !== 'hall' && s.kind !== 'polygon') return center;
  let best = s.points[0], score = -1;
  for (let x = 1; x < 16; x++) for (let y = 1; y < 16; y++) {
    const p = { x: s.width * x / 16, y: s.height * y / 16 };
    if (!pointInPolygon(p, s.points)) continue;
    const minDistance = Math.min(...s.points.map((a, i) => {
      const b = s.points[(i + 1) % s.points.length], dx = b.x - a.x, dy = b.y - a.y;
      const t = Math.max(0, Math.min(1, ((p.x - a.x) * dx + (p.y - a.y) * dy) / (dx * dx + dy * dy || 1)));
      return distance(p, { x: a.x + dx * t, y: a.y + dy * t });
    }));
    if (minDistance > score) { score = minDistance; best = p; }
  }
  return best;
};

export const SiteShape = memo(({ shape: s, selected = false, hitArea = false, zoom = 8 }: { shape: EditorShape; selected?: boolean; hitArea?: boolean; zoom?: number }) => {
  const path = pathData(s.points, isClosed(s));
  const label = labelPoint(s);
  const stroke = selected ? '#2563eb' : s.stroke;
  const common = { stroke, strokeWidth: s.strokeWidth, strokeLinejoin: 'round' as const, strokeLinecap: 'round' as const };
  return <g transform={`translate(${s.x} ${s.y}) rotate(${s.rotation} ${s.width / 2} ${s.height / 2})`}>
    {isPath(s) && <>
      {hitArea && <path d={path} fill={isClosed(s) ? 'transparent' : 'none'} stroke="transparent" strokeWidth={Math.max(s.strokeWidth, 12 / zoom)} />}
      <path d={path} fill={isClosed(s) ? s.fill : 'none'} {...common} />
    </>}
    {(s.kind === 'rectangle' || s.kind === 'exit') && <rect width={s.width} height={s.height} fill={s.fill} {...common} rx={s.kind === 'exit' ? 0.2 : 0} />}
    {(s.kind === 'ellipse' || s.kind === 'column') && <ellipse cx={s.width / 2} cy={s.height / 2} rx={s.width / 2} ry={s.height / 2} fill={s.fill} {...common} />}
    {s.kind === 'door' && <>
      {hitArea && <rect width={s.width} height={s.height} fill="transparent" />}
      <path d={`M 0 0 H ${s.width}`} stroke="#ffffff" strokeWidth={Math.max(0.4, s.strokeWidth * 3)} />
      <path d={`M 0 0 V ${s.height} M 0 ${s.height} A ${s.width} ${s.height} 0 0 0 ${s.width} 0`} fill="none" {...common} />
      <path d={`M -.15 -.3 V .3 M ${s.width + .15} -.3 V .3`} fill="none" {...common} />
    </>}
    {s.kind === 'text' && <>
      {hitArea && <rect width={s.width} height={s.height} fill="transparent" />}
      <text x={s.width / 2} y={s.height / 2} fill={s.stroke} fontSize={s.fontSize} textAnchor="middle" dominantBaseline="middle" direction="rtl" style={{ whiteSpace: 'pre' }}>{s.label}</text>
    </>}
    {s.kind === 'dimension' && s.points.length >= 2 && (() => {
      const a = s.points[0], b = s.points.at(-1)!, length = distance(a, b), nx = -(b.y - a.y) / (length || 1) * .45, ny = (b.x - a.x) / (length || 1) * .45;
      return <>
        <path d={`M ${a.x - nx} ${a.y - ny} L ${a.x + nx} ${a.y + ny} M ${b.x - nx} ${b.y - ny} L ${b.x + nx} ${b.y + ny}`} fill="none" {...common} />
        <text x={(a.x + b.x) / 2} y={(a.y + b.y) / 2 - .6} fontSize={s.fontSize} fill={s.stroke} textAnchor="middle" paintOrder="stroke" stroke="#ffffff" strokeWidth="0.4">{length.toFixed(2)} م</text>
      </>;
    })()}
    {s.kind === 'exit' && <text x={s.width / 2} y={s.height / 2} dominantBaseline="middle" textAnchor="middle" fontSize={Math.min(s.height * .65, s.width * .7)} fill={s.stroke}>↗</text>}
    {s.label && s.kind !== 'text' && s.kind !== 'dimension' && s.kind !== 'door' && s.kind !== 'exit' && <text x={label.x} y={label.y} textAnchor="middle" dominantBaseline="middle" fontSize={s.fontSize} fill={s.stroke} direction="rtl" pointerEvents="none">{s.label}</text>}
  </g>;
});
SiteShape.displayName = 'SiteShape';

export const SiteDrawing = ({ document: doc }: { document: EditorDocument }) => {
  const id = useId().replace(/:/g, '');
  return <svg xmlns="http://www.w3.org/2000/svg" fontFamily="Arial, sans-serif" viewBox={`-2 -2 ${doc.bounds.width + 4} ${doc.bounds.height + 4}`} role="img" aria-label="مخطط الموقع والقاعات">
    <defs><pattern id={`export-grid-${id}`} width={doc.grid.size * 5} height={doc.grid.size * 5} patternUnits="userSpaceOnUse"><path d={`M ${doc.grid.size * 5} 0 H 0 V ${doc.grid.size * 5}`} fill="none" stroke="#e2e8f0" strokeWidth=".04" /></pattern></defs>
    <rect width={doc.bounds.width} height={doc.bounds.height} fill="#fff" stroke="#94a3b8" strokeWidth=".08" />
    {doc.grid.visible && <rect width={doc.bounds.width} height={doc.bounds.height} fill={`url(#export-grid-${id})`} />}
    {doc.shapes.filter(s => doc.layers[s.layer].visible).map(s => <SiteShape key={s.id} shape={s} />)}
  </svg>;
};
