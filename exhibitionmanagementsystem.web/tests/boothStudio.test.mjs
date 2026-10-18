import test from "node:test";
import assert from "node:assert/strict";
import {
  localToWorld,
  emptyDocument,
  makeShape,
  rectanglePoints,
  historyReducer,
  worldPoints,
} from "../src/features/venues/utils/siteEditor.ts";
import {
  documentKey,
  boothShapes,
  boothIssues,
  boothPayload,
  loadBoothDocument,
  makeBoothRows,
  mergeBooths,
  splitBooth,
  renumberBooths,
  prepareBoothDocument,
  polygonInside,
  polygonsOverlap,
  placeUnpositionedBooth,
} from "../src/features/booths/utils/boothStudio.ts";
const hall = {
  hallID: 10,
  venueID: 5,
  hallName: "L",
  floorPlanWidth: 20,
  floorPlanHeight: 20,
  isActive: true,
};
const lPoints = [
  { x: 0, y: 0 },
  { x: 20, y: 0 },
  { x: 20, y: 8 },
  { x: 8, y: 8 },
  { x: 8, y: 20 },
  { x: 0, y: 20 },
];
const site = (extra = []) =>
  JSON.stringify({
    ...emptyDocument(),
    shapes: [{ ...makeShape("hall", lPoints, "h"), hallID: 10 }, ...extra],
  });
const booth = (id = 1, x = 1, y = 1, width = 3, height = 3) => ({
  boothID: id,
  boothNumber: `A-${id}`,
  width,
  height,
  posX: x,
  posY: y,
  rotationAngle: 0,
  shapeType: "Rect",
  boothType: "Standard",
  status: "Available",
});
const doc = (booths = [], plan = site()) =>
  loadBoothDocument(hall, booths, plan);
const rowOptions = {
  x: 1,
  y: 1,
  width: 3,
  height: 3,
  columns: 2,
  rows: 2,
  gap: 0,
  aisle: 2,
  prefix: "B-",
  start: 1,
  boothType: "Corner",
};
const ids = () => {
  let i = 0;
  return () => `new-${i++}`;
};

test("loads actual linked hall outline even without DB dimensions", () => {
  const d = loadBoothDocument(
    { ...hall, floorPlanWidth: undefined, floorPlanHeight: undefined },
    [booth()],
    site(),
  );
  assert.equal(d.bounds.width, 20);
  assert.equal(d.shapes[0].points.length, 6);
  assert.deepEqual(boothIssues(d), []);
  assert.ok(
    boothIssues(doc([booth(1, 12, 12)])).some((i) =>
      i.message.includes("خارج الحدود"),
    ),
  );
});
test("rejects concave boundary crossings through vertices and permits touching edges", () => {
  const u = [
    { x: 0, y: 0 },
    { x: 10, y: 0 },
    { x: 10, y: 10 },
    { x: 7, y: 10 },
    { x: 7, y: 3 },
    { x: 3, y: 3 },
    { x: 3, y: 10 },
    { x: 0, y: 10 },
  ];
  assert.equal(
    polygonInside(
      [
        { x: 1, y: 5 },
        { x: 9, y: 5 },
        { x: 9, y: 8 },
        { x: 1, y: 8 },
      ],
      u,
    ),
    false,
  );
  assert.equal(
    polygonsOverlap(
      rectanglePoints(3, 3),
      rectanglePoints(3, 3).map((p) => ({ x: p.x + 3, y: p.y })),
    ),
    false,
  );
  assert.equal(
    polygonsOverlap(rectanglePoints(3, 3), rectanglePoints(3, 3)),
    true,
  );
});
test("actual concave booth polygons do not collide through empty bounding-box notches", () => {
  const a = {
    ...booth(1, 1, 1, 6, 6),
    shapeType: "Polygon",
    shapePolygonJSON: JSON.stringify([
      { x: 0, y: 0 },
      { x: 6, y: 0 },
      { x: 6, y: 2 },
      { x: 2, y: 2 },
      { x: 2, y: 6 },
      { x: 0, y: 6 },
    ]),
  };
  const d = doc([a, booth(2, 4, 4, 2, 2)]);
  assert.equal(boothIssues(d).length, 0);
  assert.equal(
    JSON.parse(boothPayload(d).booths[0].shapePolygonJSON).length,
    6,
  );
  assert.throws(() => doc([{ ...a, shapePolygonJSON: "[]" }]));
});
test("site walls and columns cannot be crossed and are not duplicated on save reload", () => {
  const wall = makeShape(
      "wall",
      [
        { x: 5, y: 0 },
        { x: 5, y: 8 },
      ],
      "wall",
    ),
    column = makeShape(
      "column",
      [
        { x: 1, y: 6 },
        { x: 2, y: 7 },
      ],
      "column",
    );
  const raw = site([wall, column]),
    d = doc([booth(1, 4, 1)], raw);
  assert.ok(boothIssues(d).some((i) => i.message.includes("يتداخل")));
  const good = doc([booth()], raw),
    payload = boothPayload(good);
  assert.equal(JSON.parse(payload.floorPlanJSON).elements.length, 0);
  const reloaded = loadBoothDocument(
    { ...hall, floorPlanJSON: payload.floorPlanJSON },
    [booth()],
    raw,
  );
  assert.equal(reloaded.shapes.length, good.shapes.length);
  assert.equal(boothIssues(reloaded).length, 0);
});
test("row builder rejects footprints outside concave halls, duplicate numbers and unsafe counts", () => {
  const d = makeBoothRows(doc(), rowOptions, ids());
  assert.equal(boothShapes(d).length, 4);
  assert.equal(boothIssues(d).length, 0);
  assert.equal(boothShapes(d)[2].y, 6);
  assert.throws(() =>
    makeBoothRows(doc(), { ...rowOptions, x: 12, y: 12 }, ids()),
  );
  assert.throws(() =>
    makeBoothRows(doc(), { ...rowOptions, columns: 501 }, ids()),
  );
  assert.throws(() =>
    renumberBooths(
      doc([{ ...booth(1), boothNumber: "A-001" }, booth(2, 5, 1)]),
      ["booth-2"],
      "A-",
      1,
    ),
  );
});
test("copy, undo and deletion retain identities and never clone database IDs or reservation state", () => {
  const initial = doc([{ ...booth(), status: "Reserved" }]);
  const copy = {
    ...initial.shapes.find((s) => s.id === "booth-1"),
    id: "copy",
    x: 5,
    locked: false,
  };
  const next = prepareBoothDocument(
    { ...initial, shapes: [...initial.shapes, copy] },
    initial,
  );
  assert.equal(next.boothData.copy.boothID, undefined);
  assert.equal(next.boothData.copy.status, "Available");
  assert.equal(next.shapes.at(-1).locked, false);
  assert.notEqual(next.shapes.at(-1).label, "A-1");
  let history = historyReducer(
    { document: initial, past: [], future: [] },
    { type: "commit", document: next },
  );
  history = historyReducer(history, { type: "undo" });
  assert.equal(boothShapes(history.document).length, 1);
  const deleted = prepareBoothDocument(
    { ...initial, shapes: initial.shapes.filter((s) => s.id !== "booth-1") },
    initial,
  );
  assert.deepEqual(boothPayload(deleted).deletedBoothIDs, [1]);
});
test("split and merge preserve accurate area after decimal rounding and track replaced IDs", () => {
  const d = doc([booth(1, 1, 1, 4, 3)]),
    split = splitBooth(d, "booth-1", 3, false, ids()),
    parts = boothShapes(split);
  assert.equal(parts.length, 3);
  assert.ok(
    Math.abs(parts.reduce((a, s) => a + s.width * s.height, 0) - 12) < 1e-8,
  );
  assert.ok(Math.abs(parts[2].x + parts[2].width - 5) < 1e-8);
  assert.deepEqual(boothPayload(split).deletedBoothIDs, [1]);
  const merged = mergeBooths(
    split,
    parts.map((s) => s.id),
    "merged",
  );
  assert.equal(boothShapes(merged).length, 1);
  assert.equal(boothShapes(merged)[0].width, 4);
  assert.equal(merged.boothData.merged.boothID, undefined);
  assert.throws(() =>
    splitBooth(doc([{ ...booth(), status: "Reserved" }]), "booth-1", 2, false),
  );
  assert.throws(() =>
    mergeBooths(doc([booth(1), booth(2, 5, 1)]), ["booth-1", "booth-2"]),
  );
});
test("unpositioned database booths need explicit placement and maintain existing record IDs", () => {
  const d = doc([
    { ...booth(), posX: null, posY: null, width: null, height: null },
  ]);
  assert.equal(d.boothData["booth-1"].needsPlacement, true);
  assert.ok(boothIssues(d).length);
  const placed = placeUnpositionedBooth(d, "booth-1");
  assert.equal(placed.boothData["booth-1"].needsPlacement, false);
  assert.equal(boothPayload(placed).booths[0].boothID, 1);
  assert.equal(boothIssues(placed).length, 0);
});
test("locks, groups, legacy extras, architecture and annotations survive server-assigned IDs", () => {
  const d = makeBoothRows(doc(), { ...rowOptions, rows: 1 }, ids());
  d.shapes
    .filter((s) => d.boothData[s.id])
    .forEach((s) => {
      s.locked = true;
      s.groupId = "unit";
    });
  d.legacyPlan.custom = "legacy";
  const payload = boothPayload(d),
    api = payload.booths.map((b, i) => ({
      ...b,
      boothID: 100 + i,
      status: "Available",
    }));
  const loaded = loadBoothDocument(
    { ...hall, floorPlanJSON: payload.floorPlanJSON },
    api,
    site(),
  );
  assert.equal(loaded.legacyPlan.custom, "legacy");
  assert.ok(boothShapes(loaded).every((s) => s.locked && s.groupId === "unit"));
  assert.equal(loaded.boothData["booth-100"].boothID, 100);
});
test("invalid polygon drafts, duplicate booth labels and malformed stored plans stay explicit errors", () => {
  assert.throws(() =>
    loadBoothDocument({ ...hall, floorPlanJSON: "{" }, [], site()),
  );
  assert.throws(() =>
    doc([{ ...booth(1) }, { ...booth(2, 5, 1), boothNumber: "A-1" }]),
  );
  const d = doc([booth()]),
    p = worldPoints(d.shapes.find((s) => s.id === "booth-1"));
  assert.equal(p.length, 4);
});

test("confirming a legacy unpositioned booth marks a real save even when suggested geometry stays the same", () => {
  const initial = doc([
    { ...booth(), posX: null, posY: null, width: null, height: null },
  ]);
  const confirmed = prepareBoothDocument(
    {
      ...initial,
      boothData: {
        ...initial.boothData,
        "booth-1": { ...initial.boothData["booth-1"], needsPlacement: false },
      },
    },
    initial,
  );
  assert.notEqual(documentKey(initial), documentKey(confirmed));
  assert.equal(boothIssues(confirmed).length, 0);
});
test("legacy booth doors follow their owner through move, resize, rotation and renaming", () => {
  const plan = {
    elements: [
      {
        id: "door",
        type: "boothDoor",
        parentBoothNumber: "A-1",
        edge: "s",
        offset: 0.5,
        x: 2,
        y: 3.9,
        width: 1,
        height: 0.1,
        rotation: 180,
        label: "باب",
      },
    ],
  };
  const initial = loadBoothDocument(
    { ...hall, floorPlanJSON: JSON.stringify(plan) },
    [booth()],
    site(),
  );
  const next = prepareBoothDocument(
    {
      ...initial,
      shapes: initial.shapes.map((s) =>
        s.id === "booth-1"
          ? { ...s, x: 5, y: 5, width: 6, rotation: 90, label: "renamed" }
          : s,
      ),
    },
    initial,
  );
  const door = next.shapes.find((s) => s.id === "door"),
    owner = next.shapes.find((s) => s.id === "booth-1");
  const baseline = localToWorld(door, { x: door.width / 2, y: 0 }),
    edge = localToWorld(owner, { x: owner.width / 2, y: owner.height });
  assert.ok(Math.hypot(baseline.x - edge.x, baseline.y - edge.y) < 0.002);
  assert.equal(next.elementData.door.parentBoothNumber, "renamed");
  assert.equal(
    JSON.parse(boothPayload(next).floorPlanJSON).elements[0].parentBoothNumber,
    "renamed",
  );
});

test("saving an untouched protected polygon preserves its database JSON representation", () => {
  const raw = JSON.stringify(
    {
      points: [
        { x: 0, y: 0 },
        { x: 3, y: 0 },
        { x: 3, y: 3 },
        { x: 0, y: 3 },
      ],
      unit: "meter",
    },
    null,
    2,
  );
  const d = doc([
    {
      ...booth(),
      status: "Reserved",
      shapeType: "Polygon",
      shapePolygonJSON: raw,
    },
  ]);
  assert.equal(boothPayload(d).booths[0].shapePolygonJSON, raw);
  assert.equal(boothShapes(d)[0].locked, true);
});
