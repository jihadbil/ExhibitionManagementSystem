import {
  emptyDocument,
  makeShape,
  shapeBounds,
  worldPoints,
  worldToLocal,
  localToWorld,
  rectanglePoints,
  polygonArea,
  pointInPolygon,
  selfIntersects,
  validateDocument,
  type EditorDocument,
  type EditorShape,
  type Point,
  type EditorIssue,
} from "../../venues/utils/siteEditor.ts";
import {
  type BoothStatus,
  type PlanElement,
  statusColor,
} from "./boothLayout.ts";
const round = (n: number, precision = 3) =>
  Math.round(n * 10 ** precision) / 10 ** precision;

export interface StudioHall {
  hallID: number;
  venueID: number;
  hallName: string;
  floorPlanWidth?: number;
  floorPlanHeight?: number;
  floorPlanJSON?: string;
  isActive: boolean;
}
export interface StudioApiBooth {
  boothID: number;
  boothNumber: string;
  width?: number;
  height?: number;
  posX?: number;
  posY?: number;
  rotationAngle?: number;
  shapeType?: string;
  shapePolygonJSON?: string;
  boothType: string;
  assignedPriceRuleID?: number;
  status: BoothStatus;
  isMerged?: boolean;
}
export interface BoothInfo {
  boothID?: number;
  boothType: string;
  assignedPriceRuleID?: number;
  status: BoothStatus;
  protected?: boolean;
  needsPlacement?: boolean;
  originalPolygonJSON?: string;
}
export interface BoothDocument extends EditorDocument {
  boothData: Record<string, BoothInfo>;
  elementData: Record<string, Partial<PlanElement>>;
  backgroundImagePath?: string;
  outlineLinked?: boolean;
  legacyPlan: Record<string, unknown>;
}
export const boothTypes: Record<string, string> = {
  Standard: "قياسي",
  Corner: "زاوية",
  Premium: "مميز",
  VIP: "كبار الشخصيات",
  Custom: "مخصص",
};
export const asBoothDocument = (doc: EditorDocument): BoothDocument =>
  doc as BoothDocument;
export const boothShapes = (doc: EditorDocument) =>
  doc.shapes.filter((s) => asBoothDocument(doc).boothData?.[s.id]);
const cross = (a: Point, b: Point, c: Point) =>
  (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
const properIntersection = (a: Point, b: Point, c: Point, d: Point) =>
  cross(a, b, c) * cross(a, b, d) < -1e-9 &&
  cross(c, d, a) * cross(c, d, b) < -1e-9;
const onEdge = (p: Point, polygon: Point[]) =>
  polygon.some((a, i) => {
    const b = polygon[(i + 1) % polygon.length];
    return (
      Math.abs(cross(a, b, p)) < 1e-6 &&
      p.x >= Math.min(a.x, b.x) - 1e-6 &&
      p.x <= Math.max(a.x, b.x) + 1e-6 &&
      p.y >= Math.min(a.y, b.y) - 1e-6 &&
      p.y <= Math.max(a.y, b.y) + 1e-6
    );
  });
export const polygonInside = (points: Point[], boundary: Point[]) =>
  points.every((p) => pointInPolygon(p, boundary)) &&
  points.every((a, i) => {
    const b = points[(i + 1) % points.length];
    // Sampling split intervals handles segments crossing a concave notch through its vertices.
    const cuts = [0, 1];
    for (let j = 0; j < boundary.length; j++) {
      const c = boundary[j],
        d = boundary[(j + 1) % boundary.length],
        dx = b.x - a.x,
        dy = b.y - a.y,
        ex = d.x - c.x,
        ey = d.y - c.y,
        den = dx * ey - dy * ex;
      if (Math.abs(den) < 1e-9) continue;
      const t = ((c.x - a.x) * ey - (c.y - a.y) * ex) / den,
        u = ((c.x - a.x) * dy - (c.y - a.y) * dx) / den;
      if (t > 0 && t < 1 && u >= -1e-8 && u <= 1 + 1e-8) cuts.push(t);
    }
    cuts.sort((x, y) => x - y);
    return cuts.slice(1).every((t, j) =>
      pointInPolygon(
        {
          x: a.x + ((b.x - a.x) * (t + cuts[j])) / 2,
          y: a.y + ((b.y - a.y) * (t + cuts[j])) / 2,
        },
        boundary,
      ),
    );
  });
export const polygonsOverlap = (a: Point[], b: Point[]) =>
  a.some((p, i) =>
    b.some((q, j) =>
      properIntersection(p, a[(i + 1) % a.length], q, b[(j + 1) % b.length]),
    ),
  ) ||
  a.some((p) => pointInPolygon(p, b) && !onEdge(p, b)) ||
  b.some((p) => pointInPolygon(p, a) && !onEdge(p, a)) ||
  a.some((p, i) => {
    const q = a[(i + 1) % a.length],
      mid = { x: (p.x + q.x) / 2, y: (p.y + q.y) / 2 };
    if (pointInPolygon(mid, b) && !onEdge(mid, b)) return true;
    const len = Math.hypot(q.x - p.x, q.y - p.y) || 1;
    return [-1, 1].some((sign) => {
      const probe = {
        x: mid.x - ((sign * (q.y - p.y)) / len) * 0.0001,
        y: mid.y + ((sign * (q.x - p.x)) / len) * 0.0001,
      };
      return (
        pointInPolygon(probe, a) &&
        pointInPolygon(probe, b) &&
        !onEdge(probe, b)
      );
    });
  });

export const readBoothPolygon = (
  raw: string | undefined,
  width: number,
  height: number,
): Point[] => {
  if (!raw?.trim()) throw new Error("الجناح متعدد الزوايا يفتقد بيانات حدوده.");
  const parsed = JSON.parse(raw),
    points = Array.isArray(parsed) ? parsed : parsed.points;
  if (
    !Array.isArray(points) ||
    points.length < 3 ||
    points.length > 512 ||
    points.some(
      (p) =>
        !Number.isFinite(p.x) ||
        !Number.isFinite(p.y) ||
        p.x < 0 ||
        p.y < 0 ||
        p.x > width + 0.005 ||
        p.y > height + 0.005,
    ) ||
    selfIntersects(points) ||
    polygonArea(points) < 0.1
  )
    throw new Error("حدود جناح متعدد الزوايا غير صالحة.");
  return points;
};
export const hallOutline = (hall: StudioHall, siteJSON: string) => {
  const parsed = siteJSON?.trim() ? JSON.parse(siteJSON) : null;
  const linked: EditorShape | undefined =
    parsed?.schemaVersion === 4
      ? parsed.shapes.find(
          (s: EditorShape) => s.kind === "hall" && s.hallID === hall.hallID,
        )
      : undefined;
  if (linked) {
    if (
      !linked.points?.length ||
      selfIntersects(linked.points) ||
      polygonArea(linked.points) < 0.1
    )
      throw new Error("حدود القاعة في مخطط الموقع غير صالحة.");
    return {
      width: linked.width,
      height: linked.height,
      points: linked.points,
      linked,
      siteShapes: parsed.shapes as EditorShape[],
    };
  }
  if (!hall.floorPlanWidth || !hall.floorPlanHeight)
    throw new Error("حدد أبعاد القاعة أو ارسم حدودها في مصمم الموقع أولاً.");
  return {
    width: hall.floorPlanWidth,
    height: hall.floorPlanHeight,
    points: rectanglePoints(hall.floorPlanWidth, hall.floorPlanHeight),
    linked: undefined,
    siteShapes: [] as EditorShape[],
  };
};
const elementShape = (element: PlanElement) => {
  const kind =
    element.type === "column"
      ? "rectangle"
      : element.type === "entrance" || element.type === "exit"
        ? "exit"
        : element.type === "boothDoor"
          ? "door"
          : "rectangle";
  const s = makeShape(
    kind,
    [
      { x: element.x, y: element.y },
      {
        x: element.x + element.width,
        y: element.y + Math.max(0.1, element.height),
      },
    ],
    element.id,
  );
  return {
    ...s,
    rotation: element.rotation || 0,
    layer: "structure" as const,
    label: element.label,
    fill:
      element.type === "aisle"
        ? "#eff6ff"
        : element.type === "restricted"
          ? "#fff1f2"
          : "#e2e8f0",
    stroke: element.type === "restricted" ? "#be123c" : "#64748b",
    fontSize: 1,
  };
};
export const loadBoothDocument = (
  hall: StudioHall,
  apiBooths: StudioApiBooth[],
  siteJSON: string,
): BoothDocument => {
  const outline = hallOutline(hall, siteJSON),
    base = emptyDocument();
  const plan = hall.floorPlanJSON?.trim() ? JSON.parse(hall.floorPlanJSON) : {};
  const doc: BoothDocument = {
    ...base,
    bounds: { width: outline.width, height: outline.height },
    grid: plan.studio?.grid ?? { size: 0.5, snap: true, visible: false },
    shapes: [],
    boothData: {},
    elementData: {},
    legacyPlan: plan,
    backgroundImagePath: plan.backgroundImagePath ?? plan.BackgroundImagePath,
    outlineLinked: !!outline.linked,
  };
  doc.layers.halls = { visible: true, locked: true };
  const boundary = {
    ...makeShape("polygon", outline.points, "hall-boundary"),
    layer: "halls" as const,
    locked: true,
    label: "",
    fill: "#ffffff",
    stroke: "#64748b",
    strokeWidth: 0.2,
  };
  doc.shapes.push(boundary);
  if (outline.linked)
    for (const original of outline.siteShapes.filter(
      (s) =>
        ["wall", "door", "column", "exit"].includes(s.kind) &&
        s.layer !== "annotations",
    )) {
      const center = worldToLocal(outline.linked, {
        x: original.x + original.width / 2,
        y: original.y + original.height / 2,
      });
      const s = {
        ...original,
        id: `site-${original.id}`,
        x: round(center.x - original.width / 2),
        y: round(center.y - original.height / 2),
        rotation: round(
          (original.rotation - outline.linked.rotation + 360) % 360,
        ),
        layer: "structure" as const,
        locked: true,
      };
      if (
        s.kind === "wall"
          ? worldPoints(s)
              .slice(1)
              .some((p, i) => {
                const a = worldPoints(s)[i];
                return pointInPolygon(
                  { x: (p.x + a.x) / 2, y: (p.y + a.y) / 2 },
                  outline.points,
                );
              })
          : pointInPolygon(center, outline.points)
      ) {
        doc.shapes.push(s);
        if (s.kind === "column")
          doc.elementData[s.id] = { type: "column", label: "عمود القاعة" };
      }
    }
  if (Array.isArray(plan.elements))
    for (const element of plan.elements as PlanElement[]) {
      if (
        !element.id ||
        element.type === ("hallDoor" as string) ||
        doc.shapes.some((s) => s.id === element.id)
      )
        continue;
      const s = elementShape(element);
      doc.shapes.push(s);
      doc.elementData[s.id] = { ...element };
    }
  if (plan.studio?.shapes)
    for (const s of plan.studio.shapes as EditorShape[]) {
      if (!doc.shapes.some((x) => x.id === s.id)) {
        doc.shapes.push(s);
        if (plan.studio.elementData?.[s.id])
          doc.elementData[s.id] = plan.studio.elementData[s.id];
      }
    }
  for (const booth of apiBooths) {
    const needsPlacement =
      ![booth.width, booth.height, booth.posX, booth.posY].every(
        (n) => n != null && Number.isFinite(n),
      ) ||
      !booth.width ||
      !booth.height;
    const width = booth.width && booth.width > 0 ? booth.width : 3,
      height = booth.height && booth.height > 0 ? booth.height : 3,
      x = booth.posX ?? 0,
      y = booth.posY ?? 0,
      id = `booth-${booth.boothID}`;
    const s = makeShape(
      booth.shapeType === "Polygon" ? "polygon" : "rectangle",
      [
        { x, y },
        { x: x + width, y: y + height },
      ],
      id,
    );
    if (booth.shapeType === "Polygon")
      s.points = readBoothPolygon(booth.shapePolygonJSON, width, height);
    s.label = booth.boothNumber;
    s.rotation = booth.rotationAngle ?? 0;
    s.fontSize = Math.max(0.6, Math.min(width, height) * 0.22);
    const metadata =
      plan.studio?.boothsByNumber?.[booth.boothNumber] ??
      plan.studio?.booths?.[booth.boothID];
    s.locked = !!metadata?.locked;
    s.groupId = metadata?.groupId;
    doc.boothData[id] = {
      boothID: booth.boothID,
      boothType: booth.boothType || "Standard",
      assignedPriceRuleID: booth.assignedPriceRuleID,
      status: booth.status,
      needsPlacement,
      originalPolygonJSON: booth.shapePolygonJSON,
      protected:
        booth.status === "Reserved" ||
        booth.status === "Merged" ||
        booth.isMerged,
    };
    doc.shapes.push(s);
  }
  return prepareBoothDocument(doc, doc);
};
export const nextBoothNumber = (
  doc: EditorDocument,
  prefix = "A-",
  start = 1,
): string => {
  const used = new Set(
    boothShapes(doc).map((s) => s.label.toLocaleLowerCase()),
  );
  let n = start;
  while (used.has(`${prefix}${String(n).padStart(3, "0")}`.toLocaleLowerCase()))
    n++;
  return `${prefix}${String(n).padStart(3, "0")}`;
};
// Every editor commit carries identity metadata through copy, delete and undo.
export const prepareBoothDocument = (
  next: EditorDocument | BoothDocument,
  previous: EditorDocument,
): BoothDocument => {
  const prior = asBoothDocument(previous),
    doc = {
      ...next,
      boothData: { ...prior.boothData, ...asBoothDocument(next).boothData },
      elementData: Object.fromEntries(
        Object.entries({
          ...prior.elementData,
          ...asBoothDocument(next).elementData,
        }).map(([id, value]) => [id, { ...value }]),
      ),
      legacyPlan: asBoothDocument(next).legacyPlan ?? prior.legacyPlan ?? {},
    } as BoothDocument;
  const used = new Set<string>();
  doc.shapes = next.shapes.map((shape) => {
    const s = { ...shape };
    if (
      s.kind === "column" &&
      !s.id.startsWith("site-") &&
      !doc.elementData[s.id]
    )
      doc.elementData[s.id] = { type: "column" };
    if (
      !doc.boothData[s.id] &&
      !doc.elementData[s.id] &&
      s.layer !== "halls" &&
      !s.id.startsWith("site-") &&
      ["rectangle", "polygon"].includes(s.kind)
    ) {
      const source = previous.shapes.find(
        (old) => old.label === s.label && prior.boothData?.[old.id],
      );
      doc.boothData[s.id] = {
        boothType: source ? prior.boothData[source.id].boothType : "Standard",
        assignedPriceRuleID: source
          ? prior.boothData[source.id].assignedPriceRuleID
          : undefined,
        status: "Available",
      };
      let index = 1;
      s.label = nextBoothNumber(doc, "A-", index);
      while (used.has(s.label.toLocaleLowerCase()))
        s.label = nextBoothNumber(doc, "A-", ++index);
      s.locked = false;
    }
    const info = doc.boothData[s.id];
    if (info) {
      s.x = round(s.x, 2);
      s.y = round(s.y, 2);
      s.width = round(s.width, 2);
      s.height = round(s.height, 2);
      s.rotation = ((round(s.rotation, 2) % 360) + 360) % 360;
      s.points = s.points.map((p) => ({ x: round(p.x, 2), y: round(p.y, 2) }));
      if (
        !s.label.trim() ||
        s.label.trim().length > 20 ||
        used.has(s.label.trim().toLocaleLowerCase())
      )
        throw new Error("رقم الجناح يجب أن يكون فريداً ومن 1 إلى 20 حرفاً.");
      used.add(s.label.trim().toLocaleLowerCase());
      s.label = s.label.trim();
      const palette = statusColor[info.status] ?? statusColor.Available;
      s.fill = palette.fill;
      s.stroke = palette.stroke;
      s.strokeWidth = 0.09;
      s.fontSize = Math.max(0.5, Math.min(s.width, s.height) * 0.22);
      if (info.protected) s.locked = true;
    }
    return s;
  });
  // Keep attached booth doors aligned with their owner after moving/renaming it.
  for (const s of doc.shapes) {
    const meta = doc.elementData[s.id];
    if (meta?.type !== "boothDoor" || !meta.parentBoothNumber) continue;
    const before = previous.shapes.find(
        (b) => prior.boothData?.[b.id] && b.label === meta.parentBoothNumber,
      ),
      owner = doc.shapes.find((b) => b.id === before?.id);
    if (before && owner) {
      meta.parentBoothNumber = owner.label;
      if (
        ["x", "y", "width", "height", "rotation"].some(
          (key) => before[key as "x"] !== owner[key as "x"],
        )
      ) {
        const edge = meta.edge ?? "s",
          offset = Math.max(0, Math.min(1, meta.offset ?? 0.5)),
          angle = ({ n: 0, e: 90, s: 180, w: 270 } as const)[edge],
          normal = (
            {
              n: { x: 0, y: 1 },
              e: { x: -1, y: 0 },
              s: { x: 0, y: -1 },
              w: { x: 1, y: 0 },
            } as const
          )[edge];
        const center =
          edge === "n" || edge === "s"
            ? { x: owner.width * offset, y: edge === "n" ? 0 : owner.height }
            : { x: edge === "w" ? 0 : owner.width, y: owner.height * offset };
        const world = localToWorld(owner, {
          x: center.x + (normal.x * s.height) / 2,
          y: center.y + (normal.y * s.height) / 2,
        });
        s.x = round(world.x - s.width / 2, 2);
        s.y = round(world.y - s.height / 2, 2);
        s.rotation = (owner.rotation + angle) % 360;
      }
    }
  }
  doc.shapes = doc.shapes.filter((s) => {
    const meta = doc.elementData[s.id];
    return (
      meta?.type !== "boothDoor" ||
      doc.shapes.some(
        (b) => doc.boothData[b.id] && b.label === meta.parentBoothNumber,
      )
    );
  });
  return doc;
};
export const boothIssues = (source: EditorDocument): EditorIssue[] => {
  const doc = asBoothDocument(source),
    result = validateDocument(doc).filter((i) => !i.id?.startsWith("site-")),
    booths = boothShapes(doc),
    boundary = doc.shapes.find((s) => s.id === "hall-boundary");
  if (booths.length > 2000)
    result.push({
      severity: "error",
      message: "الحد الأقصى 2000 جناح في القاعة.",
    });
  const geometry = new Map(
    booths.map((s) => [
      s.id,
      { points: worldPoints(s), bounds: shapeBounds(s) },
    ]),
  );
  const obstacles = doc.shapes.filter(
    (s) =>
      s.kind === "wall" ||
      ["aisle", "column", "restricted"].includes(
        doc.elementData[s.id]?.type ?? "",
      ),
  );
  for (const booth of booths) {
    if (doc.boothData[booth.id].needsPlacement) {
      result.push({
        id: booth.id,
        severity: "error",
        message: `حدد مقاس وموقع الجناح ${booth.label} قبل الحفظ.`,
      });
      continue;
    }
    const points = geometry.get(booth.id)!.points;
    if (boundary && !polygonInside(points, worldPoints(boundary)))
      result.push({
        id: booth.id,
        severity: "error",
        message: `الجناح ${booth.label} خارج الحدود الفعلية للقاعة.`,
      });
    if (booth.width < 0.5 || booth.height < 0.5)
      result.push({
        id: booth.id,
        severity: "error",
        message: `مقاس الجناح ${booth.label} أقل من 0.5 متر.`,
      });
    if (!boothTypes[doc.boothData[booth.id].boothType])
      result.push({
        id: booth.id,
        severity: "error",
        message: `فئة الجناح ${booth.label} غير صالحة.`,
      });
    for (const obstacle of obstacles)
      if (obstaclePolygons(obstacle).some((p) => polygonsOverlap(points, p)))
        result.push({
          id: booth.id,
          severity: "error",
          message: `الجناح ${booth.label} يتداخل مع ${obstacle.label || "عائق"}.`,
        });
  }
  for (let i = 0; i < booths.length; i++)
    for (let j = i + 1; j < booths.length; j++) {
      if (
        doc.boothData[booths[i].id].needsPlacement ||
        doc.boothData[booths[j].id].needsPlacement
      )
        continue;
      const a = geometry.get(booths[i].id)!.bounds,
        b = geometry.get(booths[j].id)!.bounds;
      if (
        a.x + a.width <= b.x ||
        b.x + b.width <= a.x ||
        a.y + a.height <= b.y ||
        b.y + b.height <= a.y
      )
        continue;
      if (
        polygonsOverlap(
          geometry.get(booths[i].id)!.points,
          geometry.get(booths[j].id)!.points,
        )
      ) {
        for (const s of [booths[i], booths[j]])
          result.push({
            id: s.id,
            severity: "error",
            message: `تداخل بين الجناحين ${booths[i].label} و${booths[j].label}.`,
          });
      }
    }
  return result;
};
export const boothPayload = (source: EditorDocument) => {
  const src = asBoothDocument(source),
    doc = {
      ...src,
      boothData: src.boothData ?? {},
      elementData: src.elementData ?? {},
      legacyPlan: src.legacyPlan ?? {},
    },
    shapes = boothShapes(doc),
    present = new Set(shapes.map((s) => s.id));
  const elements = doc.shapes
    .filter((s) => doc.elementData[s.id] && !s.id.startsWith("site-"))
    .map((s) => ({
      ...doc.elementData[s.id],
      id: s.id,
      x: round(s.x, 2),
      y: round(s.y, 2),
      width: round(s.width, 2),
      height: round(s.height, 2),
      rotation: round(s.rotation, 2),
      label: s.label,
    }));
  const studio = {
    grid: doc.grid,
    boothsByNumber: Object.fromEntries(
      shapes.map((s) => [s.label, { locked: s.locked, groupId: s.groupId }]),
    ),
    booths: Object.fromEntries(
      shapes
        .filter((s) => doc.boothData[s.id].boothID)
        .map((s) => [
          doc.boothData[s.id].boothID,
          { locked: s.locked, groupId: s.groupId },
        ]),
    ),
    shapes: doc.shapes.filter(
      (s) =>
        !doc.boothData[s.id] &&
        !doc.elementData[s.id] &&
        s.layer !== "halls" &&
        !s.id.startsWith("site-"),
    ),
    elementData: doc.elementData,
  };
  return {
    booths: shapes.map((s) => {
      const info = doc.boothData[s.id];
      return {
        boothID: info.boothID ?? null,
        boothNumber: s.label,
        posX: round(s.x, 2),
        posY: round(s.y, 2),
        width: round(s.width, 2),
        height: round(s.height, 2),
        rotationAngle: round(((s.rotation % 360) + 360) % 360, 2),
        shapeType:
          s.kind === "polygon" ? "Polygon" : s.rotation ? "Rotated" : "Rect",
        shapePolygonJSON:
          info.protected && info.originalPolygonJSON
            ? info.originalPolygonJSON
            : s.kind === "polygon"
              ? JSON.stringify(
                  s.points.map((p) => ({ x: round(p.x, 2), y: round(p.y, 2) })),
                )
              : "",
        boothType: info.boothType,
        assignedPriceRuleID: info.assignedPriceRuleID ?? null,
      };
    }),
    deletedBoothIDs: Object.entries(doc.boothData)
      .filter(([id, info]) => info.boothID && !present.has(id))
      .map(([, info]) => info.boothID!),
    floorPlanJSON: JSON.stringify({
      ...doc.legacyPlan,
      version: 2,
      elements,
      backgroundImagePath: doc.backgroundImagePath,
      studio,
    }),
  };
};
export const documentKey = (doc: EditorDocument) =>
  JSON.stringify({
    payload: boothPayload(doc),
    unpositioned: boothShapes(doc)
      .filter((s) => asBoothDocument(doc).boothData[s.id].needsPlacement)
      .map((s) => s.id),
  });
export interface BoothRowOptions {
  x: number;
  y: number;
  width: number;
  height: number;
  columns: number;
  rows: number;
  gap: number;
  aisle: number;
  prefix: string;
  start: number;
  boothType: string;
}
export const makeBoothRows = (
  source: EditorDocument,
  options: BoothRowOptions,
  id: () => string = () => crypto.randomUUID(),
): BoothDocument => {
  if (
    !Object.values(options)
      .filter((v) => typeof v === "number")
      .every(Number.isFinite) ||
    options.rows < 1 ||
    options.columns < 1 ||
    !Number.isInteger(options.rows) ||
    !Number.isInteger(options.columns) ||
    options.rows * options.columns > 500 ||
    options.width < 0.5 ||
    options.height < 0.5 ||
    options.gap < 0 ||
    options.aisle < 0 ||
    options.start < 1 ||
    !Number.isInteger(options.start)
  )
    throw new Error("تحقق من مقاسات الصفوف. الحد الأقصى 500 جناح في العملية.");
  const doc = asBoothDocument(source),
    next = { ...doc, shapes: [...doc.shapes], boothData: { ...doc.boothData } };
  let n = options.start;
  for (let row = 0; row < options.rows; row++)
    for (let col = 0; col < options.columns; col++) {
      const x = options.x + col * (options.width + options.gap),
        y = options.y + row * (options.height + options.aisle),
        s = makeShape(
          "rectangle",
          [
            { x, y },
            { x: x + options.width, y: y + options.height },
          ],
          id(),
        );
      s.label = nextBoothNumber(next, options.prefix, n++);
      next.boothData[s.id] = {
        boothType: options.boothType,
        status: "Available",
      };
      next.shapes.push(s);
    }
  const prepared = prepareBoothDocument(next, source),
    previousIssues = boothIssues(source),
    issue = boothIssues(prepared).find(
      (i) =>
        i.severity === "error" &&
        !previousIssues.some(
          (old) => old.id === i.id && old.message === i.message,
        ),
    );
  if (issue) throw new Error(issue.message);
  return prepared;
};
export const renumberBooths = (
  source: EditorDocument,
  ids: string[],
  prefix: string,
  start: number,
): BoothDocument => {
  if (!Number.isInteger(start) || start < 1)
    throw new Error("بداية الترقيم يجب أن تكون عدداً صحيحاً موجباً.");
  const doc = asBoothDocument(source),
    chosen = boothShapes(doc)
      .filter((s) => ids.includes(s.id))
      .sort((a, b) => (Math.abs(a.y - b.y) < 0.1 ? a.x - b.x : a.y - b.y)),
    labels = new Map(
      chosen.map((s, i) => [
        s.id,
        `${prefix}${String(start + i).padStart(3, "0")}`,
      ]),
    );
  return prepareBoothDocument(
    {
      ...doc,
      shapes: doc.shapes.map((s) =>
        labels.has(s.id) ? { ...s, label: labels.get(s.id)! } : s,
      ),
    },
    doc,
  );
};
export const mergeBooths = (
  source: EditorDocument,
  ids: string[],
  id = crypto.randomUUID(),
): BoothDocument => {
  const doc = asBoothDocument(source),
    chosen = boothShapes(doc).filter((s) => ids.includes(s.id));
  if (
    chosen.length < 2 ||
    chosen.some(
      (s) =>
        s.locked ||
        s.rotation ||
        s.kind !== "rectangle" ||
        doc.boothData[s.id].status !== "Available",
    )
  )
    throw new Error("الدمج الهندسي يتطلب أجنحة مستطيلة متاحة وغير مقفلة.");
  const meta = doc.boothData[chosen[0].id];
  if (
    chosen.some(
      (s) =>
        doc.boothData[s.id].boothType !== meta.boothType ||
        doc.boothData[s.id].assignedPriceRuleID !== meta.assignedPriceRuleID,
    )
  )
    throw new Error("اختر أجنحة من نفس الفئة وقاعدة التسعير.");
  const x = Math.min(...chosen.map((s) => s.x)),
    y = Math.min(...chosen.map((s) => s.y)),
    width = Math.max(...chosen.map((s) => s.x + s.width)) - x,
    height = Math.max(...chosen.map((s) => s.y + s.height)) - y;
  if (
    Math.abs(
      width * height - chosen.reduce((a, s) => a + s.width * s.height, 0),
    ) > 0.005 ||
    chosen.some((s, i) =>
      chosen
        .slice(i + 1)
        .some((b) => polygonsOverlap(worldPoints(s), worldPoints(b))),
    )
  )
    throw new Error("اختر أجنحة متلاصقة تكوّن مستطيلاً دون فراغات أو تداخل.");
  const shape = {
    ...makeShape(
      "rectangle",
      [
        { x, y },
        { x: x + width, y: y + height },
      ],
      id,
    ),
    label: chosen[0].label,
  };
  const next = prepareBoothDocument(
    {
      ...doc,
      shapes: [
        ...doc.shapes.filter((s) => !chosen.some((b) => b.id === s.id)),
        shape,
      ],
      boothData: { ...doc.boothData, [id]: { ...meta, boothID: undefined } },
    },
    doc,
  );
  return next;
};
export const splitBooth = (
  source: EditorDocument,
  shapeID: string,
  count: number,
  vertical: boolean,
  id = () => crypto.randomUUID(),
): BoothDocument => {
  const doc = asBoothDocument(source),
    s = doc.shapes.find((s) => s.id === shapeID),
    meta = doc.boothData[shapeID];
  if (
    !s ||
    !meta ||
    s.locked ||
    s.kind !== "rectangle" ||
    s.rotation ||
    meta.status !== "Available"
  )
    throw new Error("التقسيم يتطلب جناحاً مستطيلاً متاحاً وغير مقفل.");
  if (
    !Number.isInteger(count) ||
    count < 2 ||
    count > 20 ||
    (vertical ? s.height : s.width) / count < 0.5
  )
    throw new Error("اختر من 2 إلى 20 جزءاً، بمقاس لا يقل عن 0.5 متر.");
  const next = {
    ...doc,
    shapes: doc.shapes.filter((b) => b.id !== shapeID),
    boothData: { ...doc.boothData },
  };
  for (let i = 0; i < count; i++) {
    const start = round(((vertical ? s.height : s.width) * i) / count, 2),
      end = round(((vertical ? s.height : s.width) * (i + 1)) / count, 2),
      width = vertical ? s.width : end - start,
      height = vertical ? end - start : s.height,
      x = s.x + (vertical ? 0 : start),
      y = s.y + (vertical ? start : 0),
      shape = {
        ...makeShape(
          "rectangle",
          [
            { x, y },
            { x: x + width, y: y + height },
          ],
          id(),
        ),
        label: `${s.label}-${i + 1}`,
      };
    next.shapes.push(shape);
    next.boothData[shape.id] = { ...meta, boothID: undefined };
  }
  return prepareBoothDocument(next, doc);
};

export const obstaclePolygons = (shape: EditorShape): Point[][] => {
  const points = worldPoints(shape);
  if (shape.kind !== "wall") return [points];
  return points.slice(1).map((b, i) => {
    const a = points[i],
      length = Math.hypot(b.x - a.x, b.y - a.y) || 1,
      nx = ((-(b.y - a.y) / length) * shape.strokeWidth) / 2,
      ny = (((b.x - a.x) / length) * shape.strokeWidth) / 2;
    return [
      { x: a.x + nx, y: a.y + ny },
      { x: b.x + nx, y: b.y + ny },
      { x: b.x - nx, y: b.y - ny },
      { x: a.x - nx, y: a.y - ny },
    ];
  });
};
export const placeUnpositionedBooth = (
  source: EditorDocument,
  shapeID: string,
): BoothDocument => {
  const doc = asBoothDocument(source),
    shape = doc.shapes.find((s) => s.id === shapeID),
    info = doc.boothData[shapeID];
  if (!shape || !info || info.protected)
    throw new Error("لا يمكن تغيير موقع جناح محجوز أو مدمج.");
  const boundary = doc.shapes.find((s) => s.id === "hall-boundary"),
    others = boothShapes(doc)
      .filter((s) => s.id !== shapeID && !doc.boothData[s.id].needsPlacement)
      .map((s) => worldPoints(s)),
    obstacles = doc.shapes
      .filter(
        (s) =>
          s.kind === "wall" ||
          ["aisle", "restricted", "column"].includes(
            doc.elementData[s.id]?.type ?? "",
          ),
      )
      .flatMap(obstaclePolygons);
  let attempts = 0;
  for (let y = 0.5; y + shape.height <= doc.bounds.height; y += 0.5)
    for (let x = 0.5; x + shape.width <= doc.bounds.width; x += 0.5) {
      if (++attempts > 20000)
        throw new Error(
          "لم أجد مساحة خالية قريبة. ارسم الجناح أو حدد موضعه يدوياً.",
        );
      const placed = { ...shape, x, y },
        points = worldPoints(placed);
      if (boundary && !polygonInside(points, worldPoints(boundary))) continue;
      if ([...others, ...obstacles].some((p) => polygonsOverlap(points, p)))
        continue;
      return prepareBoothDocument(
        {
          ...doc,
          shapes: doc.shapes.map((s) => (s.id === shapeID ? placed : s)),
          boothData: {
            ...doc.boothData,
            [shapeID]: { ...info, needsPlacement: false },
          },
        },
        doc,
      );
    }
  throw new Error(
    "لم أجد مساحة خالية بهذا المقاس. عدّل المقاس أو حرر مساحة داخل القاعة.",
  );
};
