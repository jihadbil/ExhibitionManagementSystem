import test from 'node:test';
import assert from 'node:assert/strict';
import { emptyDocument, makeShape, rectanglePoints, polygonArea, selfIntersects, pointInPolygon, localToWorld, worldToLocal, shapeBounds, resizeShape, resizePage, minimumPageBounds, insidePage, replaceWorldPoints, worldPoints, moveShapes, editable, expandSelection, marqueeIds, directEditAction, historyReducer, serializeDocument, hydrateDocument, validateDocument, snap } from '../src/features/venues/utils/siteEditor.ts';
const hallSources = [{ hallID: 10, hallName: 'قاعة L', floorPlanWidth: 40, floorPlanHeight: 30, isActive: true }];
const lPoints = [{x:5,y:5},{x:45,y:5},{x:45,y:17},{x:20,y:17},{x:20,y:35},{x:5,y:35}];
const hall = () => ({ ...makeShape('hall', lPoints, 'hall-10'), hallID: 10, label: 'قاعة L' });
const rectangle = (id = 'r', x = 10, y = 10) => makeShape('rectangle', [{ x, y }, { x: x + 8, y: y + 4 }], id);

test('concave hall area uses its actual outline rather than bounding rectangle', () => {
  const s = hall(); assert.equal(s.width * s.height, 1200); assert.equal(polygonArea(s.points), 750); assert.equal(selfIntersects(s.points), false);
  assert.equal(pointInPolygon({x:30,y:20},s.points),false); assert.equal(pointInPolygon({x:5,y:20},s.points),true);
});
test('rejects crossed, zero-area and duplicate-vertex hall outlines', () => {
  const doc=emptyDocument();
  for (const pts of [[{x:1,y:1},{x:10,y:10},{x:1,y:10},{x:10,y:1}], [{x:1,y:1},{x:2,y:1},{x:3,y:1}], [{x:1,y:1},{x:10,y:1},{x:10,y:10},{x:10,y:1},{x:1,y:10}]]) {
    const s={...makeShape('hall',pts,'bad'),hallID:10}; assert.ok(validateDocument({...doc,shapes:[s]}).some(i=>i.severity==='error'));
  }
});
test('local and world transforms round trip at arbitrary angles', () => {
  for(const rotation of [0,45,90,137,270]) { const s={...rectangle(),rotation}, p={x:3,y:2}; const actual=worldToLocal(s,localToWorld(s,p)); assert.ok(Math.abs(actual.x-p.x)<1e-8);assert.ok(Math.abs(actual.y-p.y)<1e-8); }
});
test('resizing from a rotated handle without moving it preserves geometry', () => {
  for(const rotation of [0,45,90,137]) for(let corner=0;corner<4;corner++) {
    const s={...rectangle(),rotation}, p=localToWorld(s,rectanglePoints(s.width,s.height)[corner]), next=resizeShape(s,corner,p);
    assert.equal(next.width,s.width);assert.equal(next.height,s.height);assert.equal(next.x,s.x);assert.equal(next.y,s.y);
  }
});
test('resizing rotated geometry anchors its opposite corner', () => {
  const s={...rectangle(),rotation:90}, fixed=localToWorld(s,{x:0,y:0});
  const next=resizeShape(s,2,localToWorld(s,{x:12,y:7})), after=localToWorld(next,{x:0,y:0});
  assert.equal(next.width,12);assert.equal(next.height,7);assert.ok(Math.abs(after.x-fixed.x)<.001);assert.ok(Math.abs(after.y-fixed.y)<.001);
});
test('rotated elements cannot escape site bounds unnoticed', () => {
  const doc=emptyDocument(), s={...rectangle('r',0,0),rotation:90};
  assert.equal(insidePage(s,doc),false);assert.ok(validateDocument({...doc,shapes:[s]}).some(i=>i.severity==='error'));
});
test('ellipse bounds use rotated ellipse extents rather than rectangle corners', () => {
  const s={...makeShape('ellipse',[{x:10,y:10},{x:18,y:14}],'e'),rotation:45}, b=shapeBounds(s);
  assert.ok(Math.abs(b.width-Math.sqrt(40))<1e-8);assert.ok(Math.abs(b.height-Math.sqrt(40))<1e-8);
});
test('editing an angled polygon vertex bakes the transform without moving other vertices', () => {
  const s={...hall(),rotation:30}, pts=worldPoints(s), expected=pts.slice(1);pts[0]={x:8,y:8};
  const next=replaceWorldPoints(s,pts);assert.equal(next.rotation,0);
  worldPoints(next).slice(1).forEach((p,i)=>{assert.ok(Math.abs(p.x-expected[i].x)<.002);assert.ok(Math.abs(p.y-expected[i].y)<.002);});
});
test('moving several shapes clamps the entire selection as one unit', () => {
  const doc={...emptyDocument(),shapes:[rectangle('a',10,10),rectangle('b',40,20)]}, next=moveShapes(doc,['a','b'],{x:100,y:-100});
  assert.equal(next.shapes[1].x-next.shapes[0].x,30); assert.equal(next.shapes[0].y,0);assert.equal(next.shapes[1].x+next.shapes[1].width,100);
});
test('shape and layer locks prevent movement and retain selection membership', () => {
  const s={...rectangle(),locked:true,groupId:'g'}, second={...rectangle('b',25,20),groupId:'g'},doc={...emptyDocument(),shapes:[s,second]};
  assert.equal(editable(s,doc),false);assert.equal(moveShapes(doc,['r'],{x:5,y:5}).shapes[0].x,10);assert.deepEqual(expandSelection(['r'],doc),['r','b']);
  doc.layers.structure.locked=true;assert.equal(editable(second,doc),false);assert.equal(moveShapes(doc,['b'],{x:5,y:5}).shapes[1].x,25);
});
test('history is pure, ignores no-op commits and clears redo on a new edit', () => {
  const initial={document:emptyDocument(),past:[],future:[]}, first={...initial.document,shapes:[hall()]};
  assert.equal(historyReducer(initial,{type:'commit',document:structuredClone(initial.document)}),initial);
  const committed=historyReducer(initial,{type:'commit',document:first}), undone=historyReducer(committed,{type:'undo'}), redone=historyReducer(undone,{type:'redo'});
  assert.deepEqual(redone.document,first);assert.deepEqual(initial.past,[]);
  assert.equal(historyReducer(undone,{type:'commit',document:{...initial.document,shapes:[rectangle()]}}).future.length,0);
});
test('save/reload preserves custom hall outline independently of source rectangle dimensions', () => {
  const doc={...emptyDocument(),shapes:[hall()]}, restored=hydrateDocument(serializeDocument(doc),[{...hallSources[0],floorPlanWidth:99}]);
  assert.deepEqual(restored.shapes[0].points,doc.shapes[0].points);assert.equal(restored.shapes[0].width,40);assert.equal(restored.shapes[0].label,'قاعة L');
});
test('serialization exports hall ownership and rotated world bounds for the API', () => {
  const s={...hall(),rotation:90}, json=JSON.parse(serializeDocument({...emptyDocument(),shapes:[s]})), b=shapeBounds(s);
  assert.equal(json.halls[0].hallID,10);assert.equal(json.halls[0].width,b.width);assert.equal(json.halls[0].height,b.height);assert.equal(json.hallDoors.length,0);assert.equal(json.elements.length,0);
});
test('legacy site plans migrate halls, partitions and hall doors without losing them', () => {
  const old={schemaVersion:3,bounds:{width:100,height:75},halls:[{hallID:10,x:5,y:5,width:40,height:30,locked:true}],hallDoors:[{id:'door',hallID:10,edge:'n',offset:.5,width:2}],elements:[{id:'wall',type:'wall',x:7,y:7,width:8,height:.35,rotation:0,label:'تقسيم'}]};
  const doc=hydrateDocument(JSON.stringify(old),hallSources);assert.equal(doc.schemaVersion,4);assert.equal(doc.shapes.length,3);assert.equal(doc.shapes[0].locked,true);assert.equal(doc.shapes[1].fill,'#334155');assert.equal(doc.shapes[2].kind,'door');
});
test('removed or foreign hall records are not resurrected when a plan is imported', () => {
  const doc=hydrateDocument(serializeDocument({...emptyDocument(),shapes:[hall(),rectangle()]}),[]);assert.equal(doc.shapes.length,1);assert.equal(doc.shapes[0].kind,'rectangle');
});
test('corrupt imports fail explicitly instead of replacing saved plans with defaults', () => {
  assert.throws(()=>hydrateDocument('{',hallSources));assert.throws(()=>hydrateDocument(JSON.stringify({bounds:{width:-1,height:75},halls:[]}),hallSources));
  const doc={...emptyDocument(),shapes:[{...rectangle(),stroke:'url(https://example.org)'}]};assert.throws(()=>hydrateDocument(serializeDocument(doc),hallSources));
});
test('grid precision preserves quarter-meter coordinates and Alt bypasses snapping', () => {
  const doc=emptyDocument();doc.grid.size=.25;
  assert.deepEqual(snap({x:1.37,y:3.13},doc),{x:1.25,y:3.25});assert.deepEqual(snap({x:1.37,y:3.13},doc,true),{x:1.37,y:3.13});
});
test('two drawings cannot claim the same hall record', () => {
  const doc={...emptyDocument(),shapes:[hall(),{...hall(),id:'another'}]};assert.ok(validateDocument(doc).some(i=>i.message.includes('سجل مستقل')));
});
test('overlapping halls warn while halls sharing only an edge do not', () => {
  const a={...makeShape('hall',[{x:5,y:5},{x:15,y:5},{x:15,y:15},{x:5,y:15}],'a'),hallID:10,label:'A'};
  const b={...a,id:'b',hallID:11,label:'B',x:10};
  const warnings=doc=>validateDocument(doc).filter(i=>i.severity==='warning');
  assert.equal(warnings({...emptyDocument(),shapes:[a,b]}).length,1);
  assert.equal(warnings({...emptyDocument(),shapes:[a,{...b,x:15}]}).length,0);
  assert.equal(warnings({...emptyDocument(),shapes:[a,{...b,x:5}]}).length,1);
});
test('corrupt zero-length paths and out-of-box vertices are rejected',()=>{
  const zero=makeShape('line',[{x:5,y:5},{x:5,y:5}],'zero');
  assert.ok(validateDocument({...emptyDocument(),shapes:[zero]}).some(i=>i.severity==='error'));
  const outside={...hall(),points:[{x:0,y:0},{x:100,y:0},{x:0,y:20}]};
  assert.ok(validateDocument({...emptyDocument(),shapes:[outside]}).some(i=>i.message.includes('النقاط')));
});
test('renaming a hall preserves drawing history while synchronizing its source name',()=>{
  const first={...emptyDocument(),shapes:[hall()]},next={...first,shapes:[{...hall(),x:12}]};
  const state=historyReducer({document:first,past:[],future:[]},{type:'commit',document:next});
  const renamed=historyReducer(state,{type:'renameHall',hallID:10,name:'الاسم الجديد'}),undone=historyReducer(renamed,{type:'undo'});
  assert.equal(undone.document.shapes[0].x,5);assert.equal(undone.document.shapes[0].label,'الاسم الجديد');assert.equal(renamed.document.shapes[0].x,12);
});
test('marquee direction supports containment and crossing selection',()=>{
  const a=rectangle('a',10,10),b=rectangle('b',30,10),doc={...emptyDocument(),shapes:[a,b]};
  assert.deepEqual(marqueeIds(doc,{x:5,y:5},{x:15,y:20}),[]);
  assert.deepEqual(marqueeIds(doc,{x:15,y:5},{x:5,y:20}),['a']);
  assert.deepEqual(marqueeIds(doc,{x:5,y:5},{x:40,y:20}),['a','b']);
  doc.layers.structure.visible=false;assert.deepEqual(marqueeIds(doc,{x:40,y:5},{x:5,y:20}),[]);
});
test('direct editing chooses polygon vertices or labels without bypassing locks',()=>{
  const doc=emptyDocument();assert.equal(directEditAction(hall(),doc),'nodes');assert.equal(directEditAction(rectangle(),doc),'label');
  const text=makeShape('text',[{x:10,y:10},{x:20,y:12}],'text');assert.equal(directEditAction(text,doc),'label');
  assert.equal(directEditAction({...hall(),locked:true},doc),null);doc.layers.halls.locked=true;assert.equal(directEditAction(hall(),doc),null);
});
test('expanding the page preserves hall geometry and locked/hidden objects',()=>{
  const s={...rectangle(),rotation:45,locked:true},doc={...emptyDocument(),shapes:[hall(),s]};doc.layers.structure.visible=false;
  const next=resizePage(doc,{width:180,height:120});
  assert.deepEqual(next.bounds,{width:180,height:120});assert.equal(next.shapes,doc.shapes);assert.equal(next.layers,doc.layers);assert.deepEqual(doc.bounds,{width:100,height:75});
});
test('page minimum dimensions include transformed bounds of hidden elements',()=>{
  const s={...rectangle('edge',80,60),rotation:45},doc={...emptyDocument(),shapes:[s]};doc.layers.structure.visible=false;
  const b=shapeBounds(s),minimum=minimumPageBounds(doc);
  assert.ok(minimum.width>=b.x+b.width);assert.ok(minimum.height>=b.y+b.height);
  assert.throws(()=>resizePage(doc,{width:80,height:70}),/ستقصّ/);
  assert.deepEqual(resizePage(doc,{width:90,height:70}).bounds,{width:90,height:70});
});
test('existing imported errors do not prevent increasing an unaffected page axis',()=>{
  const doc={...emptyDocument(),shapes:[rectangle('old-error',110,-5)]};
  assert.deepEqual(resizePage(doc,{width:150,height:75}).bounds,{width:150,height:75});
});
test('invalid page dimensions are rejected explicitly',()=>{
  for(const width of [NaN,Infinity,4,10001])assert.throws(()=>resizePage(emptyDocument(),{width,height:75}),/بين 5 و10000/);
});
test('page enlargement supports undo and save/reload without changing hall outlines',()=>{
  const doc={...emptyDocument(),shapes:[hall()]},next=resizePage(doc,{width:180,height:120});
  const state=historyReducer({document:doc,past:[],future:[]},{type:'commit',document:next});
  assert.deepEqual(historyReducer(state,{type:'undo'}).document.bounds,doc.bounds);
  const restored=hydrateDocument(serializeDocument(next),hallSources);assert.deepEqual(restored.bounds,next.bounds);assert.deepEqual(restored.shapes[0].points,doc.shapes[0].points);
});
