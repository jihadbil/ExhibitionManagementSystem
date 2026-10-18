import test from 'node:test';
import assert from 'node:assert/strict';
import { emptyDocument, makeShape, shapeBounds, worldPoints, insidePage } from '../src/features/venues/utils/siteEditor.ts';
import { alignShapes, snapDrawingPoint, snapShapeMove } from '../src/features/venues/utils/siteAlignment.ts';
const rect = (id, x, y, w = 8, h = 4) => makeShape('rectangle', [{ x, y }, { x: x + w, y: y + h }], id);
const docOf = (...shapes) => ({ ...emptyDocument(), shapes });
const near = (a, b) => assert.ok(Math.abs(a - b) < .002, `${a} != ${b}`);

test('drawing snaps to off-grid vertices and shows both coordinate matches', () => {
  const doc = docOf(rect('r', 10.25, 14.25)), result = snapDrawingPoint(doc, { x: 10.3, y: 14.4 }, 10);
  assert.deepEqual(result.point, { x: 10.25, y: 14.25 }); assert.equal(result.guides.length, 2);
});
test('smart attraction stays eight screen pixels across zoom levels', () => {
  const doc = docOf(rect('r', 10, 20, 80));
  for (const zoom of [2, 10, 50]) {
    near(snapDrawingPoint(doc, { x: 10 + 7 / zoom, y: 60 }, zoom).point.x, 10);
    assert.equal(snapDrawingPoint(doc, { x: 10 - 9 / zoom, y: 60 }, zoom).guides.length, 0);
  }
});
test('hidden and excluded shapes do not attract drawing; locked references still do', () => {
  const doc = docOf({ ...rect('r', 10.25, 14.25), locked: true });
  assert.equal(snapDrawingPoint(doc, { x: 10.3, y: 14.4 }, 10).guides.length, 2);
  assert.equal(snapDrawingPoint(doc, { x: 10.3, y: 14.4 }, 10, { exclude: ['r'] }).guides.length, 0);
  doc.layers.structure.visible = false;
  assert.equal(snapDrawingPoint(doc, { x: 10.3, y: 14.4 }, 10).guides.length, 0);
});
test('Alt bypasses grid and smart references; disabling smart alignment preserves grid', () => {
  const doc = docOf(rect('r', 10.25, 14.25)), raw = { x: 10.3, y: 14.4 };
  assert.deepEqual(snapDrawingPoint(doc, raw, 10, { bypass: true }), { point: raw, guides: [] });
  assert.deepEqual(snapDrawingPoint(doc, raw, 10, { enabled: false }), { point: { x: 10, y: 14 }, guides: [] });
});
test('drawing automatically locks horizontal and vertical directions without Shift', () => {
  const doc = docOf(), start = { x: 20.25, y: 20.25 };
  const horizontal = snapDrawingPoint(doc, { x: 40, y: 20.6 }, 10, { start, direction: true });
  near(horizontal.point.y, start.y); assert.equal(horizontal.guides[0].label, 'خط أفقي');
  const vertical = snapDrawingPoint(doc, { x: 20.6, y: 40 }, 10, { start, direction: true });
  near(vertical.point.x, start.x); assert.equal(vertical.guides[0].label, 'خط عمودي');
});
test('parallel drawing follows actual rotated world segments and marks the reference', () => {
  const line = { ...makeShape('line', [{ x: 10, y: 10 }, { x: 30, y: 20 }], 'l'), rotation: 20 }, [a, b] = worldPoints(line);
  const start = { x: 35, y: 30 }, dx = b.x - a.x, dy = b.y - a.y;
  const result = snapDrawingPoint(docOf(line), { x: start.x + dx, y: start.y + dy + .2 }, 10, { start, direction: true });
  near(((result.point.x - start.x) * dy - (result.point.y - start.y) * dx) / Math.hypot(dx, dy), 0);
  assert.equal(result.guides[0].label, 'موازٍ للخط المميز'); assert.deepEqual(result.guides[0].reference, [a, b]);
});
test('Shift constraint wins over nearby angled segments and survives Alt bypass', () => {
  const doc = docOf(makeShape('line', [{ x: 1, y: 1 }, { x: 21, y: 11 }], 'l')), start = { x: 30, y: 30 };
  for (const bypass of [false, true]) {
    const result = snapDrawingPoint(doc, { x: 50, y: 41 }, 10, { start, direction: true, shift: true, bypass });
    near(result.point.x - start.x, result.point.y - start.y); assert.equal(result.guides[0].label, 'اتجاه مقيد 45°');
  }
});
test('a line not close in angle is not falsely declared parallel', () => {
  const doc = docOf(makeShape('line', [{ x: 10, y: 10 }, { x: 30, y: 20 }], 'l'));
  const result = snapDrawingPoint(doc, { x: 50, y: 48 }, 10, { start: { x: 30, y: 30 }, direction: true });
  assert.ok(!result.guides.some(g => g.kind === 'direction'));
});
test('moving uses raw pointer proximity before the grid and retains a group delta', () => {
  const doc = docOf(rect('reference', 10.25, 10.25), rect('a', 40, 40), rect('b', 50, 45));
  const result = snapShapeMove(doc, ['a', 'b'], { x: -29.7, y: -29.7 }, 10);
  near(result.document.shapes[1].x, 10.25); near(result.document.shapes[1].y, 10.25);
  near(result.document.shapes[2].x - result.document.shapes[1].x, 10); assert.equal(result.guides.length, 2);
});
test('page clamping removes alignment guides that cannot actually be reached', () => {
  const doc = docOf(rect('ref', 98, 20, 2, 4), rect('a', 20, 40, 10, 4));
  const result = snapShapeMove(doc, ['a'], { x: 78, y: 0 }, 10);
  assert.ok(result.document.shapes.every(s => insidePage(s, doc)));
  assert.ok(result.guides.every(g => g.kind !== 'axis' || g.axis !== 'x' || [90, 95, 100].includes(g.value)));
});
test('six alignment commands align real bounds including rotated shapes', () => {
  const doc = docOf(rect('a', 10, 10), { ...rect('b', 30, 35), rotation: 30 });
  for (const [mode, axis, fraction] of [['start','x',0],['center','x',.5],['end','x',1],['top','y',0],['middle','y',.5],['bottom','y',1]]) {
    const result = alignShapes(doc, ['a', 'b'], mode), [a, b] = result.shapes.map(shapeBounds), length = axis === 'x' ? 'width' : 'height';
    near(a[axis] + a[length] * fraction, b[axis] + b[length] * fraction);
    assert.ok(result.shapes.every(s => insidePage(s, doc)));
  }
});
test('distribution uses equal edge gaps with unequal sizes and fixes outer shapes', () => {
  for (const mode of ['horizontal','vertical']) {
    const doc = docOf(rect('a', 5, 5, 3, 4), rect('b', 20, 20, 8, 7), rect('c', 50, 50, 5, 5));
    const result = alignShapes(doc, ['a', 'b', 'c'], mode), [a, b, c] = result.shapes.map(shapeBounds), axis = mode === 'horizontal' ? 'x' : 'y', length = axis === 'x' ? 'width' : 'height';
    near(b[axis] - a[axis] - a[length], c[axis] - b[axis] - b[length]);
    assert.deepEqual(result.shapes[0], doc.shapes[0]); assert.deepEqual(result.shapes[2], doc.shapes[2]);
  }
});
test('alignment refuses locked selections and distribution refuses insufficient room', () => {
  const locked = docOf(rect('a', 10, 10), { ...rect('b', 30, 35), locked: true });
  assert.equal(alignShapes(locked, ['a','b'], 'top'), locked);
  const tight = docOf(rect('a', 10, 10), rect('b', 12, 12), rect('c', 14, 14));
  assert.equal(alignShapes(tight, ['a','b','c'], 'horizontal'), tight);
});
test('alignment treats groups as a unit and preserves their internal offsets', () => {
  const doc = docOf({ ...rect('a', 10, 10), groupId: 'g' }, { ...rect('b', 14, 20), groupId: 'g' }, rect('c', 50, 40));
  const result = alignShapes(doc, ['a','b','c'], 'bottom');
  near(result.shapes[1].y - result.shapes[0].y, 10); near(result.shapes[1].x - result.shapes[0].x, 4);
  near(shapeBounds(result.shapes[1]).y + 4, shapeBounds(result.shapes[2]).y + 4);
  assert.equal(alignShapes(doc, ['a','b'], 'top'), doc);
});
