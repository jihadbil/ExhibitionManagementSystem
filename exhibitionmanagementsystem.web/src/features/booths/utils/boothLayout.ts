export type BoothStatus = 'Available' | 'Reserved' | 'Merged' | 'Maintenance';
export type BoothShape = 'Rect' | 'Rotated' | 'Polygon';
export type PlanElementType = 'entrance' | 'exit' | 'column' | 'restricted' | 'boothDoor' | 'aisle';
export type ElementEdge = 'n' | 'e' | 's' | 'w';

export interface VisualBooth {
  key: string;
  boothID?: number;
  number: string;
  x: number;
  y: number;
  width: number;
  height: number;
  rotation: number;
  shapeType: BoothShape;
  boothType: string;
  shapePolygonJSON: string;
  assignedPriceRuleID?: number;
  status: BoothStatus;
  hasCollision: boolean;
  outOfBounds: boolean;
}

export interface PlanElement {
  id: string;
  type: PlanElementType;
  x: number;
  y: number;
  width: number;
  height: number;
  rotation: number;
  label: string;
  edge?: ElementEdge;
  offset?: number;
  parentBoothNumber?: string;
}

export interface Point {
  x: number;
  y: number;
}

export interface HallPlanDocument {
  version: 2;
  elements: PlanElement[];
  backgroundImagePath?: string;
}

export const round = (value: number, precision = 2) => {
  const factor = 10 ** precision;
  return Math.round((value + Number.EPSILON) * factor) / factor;
};

export const snap = (value: number, size: number, enabled: boolean) =>
  enabled ? round(Math.round(value / size) * size) : round(value);

export const normalizeAngle = (value: number) => {
  const normalized = value % 360;
  return round(normalized < 0 ? normalized + 360 : normalized, 1);
};

export const rotatePoint = (point: Point, center: Point, degrees: number): Point => {
  const radians = degrees * Math.PI / 180;
  const cosine = Math.cos(radians);
  const sine = Math.sin(radians);
  const dx = point.x - center.x;
  const dy = point.y - center.y;
  return {
    x: center.x + dx * cosine - dy * sine,
    y: center.y + dx * sine + dy * cosine,
  };
};

export const boothCenter = (booth: VisualBooth): Point => ({
  x: booth.x + booth.width / 2,
  y: booth.y + booth.height / 2,
});

export const boothCorners = (booth: Pick<VisualBooth, 'x' | 'y' | 'width' | 'height' | 'rotation'>): Point[] => {
  const center = { x: booth.x + booth.width / 2, y: booth.y + booth.height / 2 };
  return [
    { x: booth.x, y: booth.y },
    { x: booth.x + booth.width, y: booth.y },
    { x: booth.x + booth.width, y: booth.y + booth.height },
    { x: booth.x, y: booth.y + booth.height },
  ].map(point => rotatePoint(point, center, booth.rotation));
};

const project = (points: Point[], axis: Point) => {
  const values = points.map(point => point.x * axis.x + point.y * axis.y);
  return { min: Math.min(...values), max: Math.max(...values) };
};

const hasSeparatingAxis = (source: Point[], other: Point[]) => {
  for (let index = 0; index < source.length; index += 1) {
    const current = source[index];
    const next = source[(index + 1) % source.length];
    const axis = { x: -(next.y - current.y), y: next.x - current.x };
    const first = project(source, axis);
    const second = project(other, axis);
    if (first.max <= second.min + 0.001 || second.max <= first.min + 0.001) return true;
  }
  return false;
};

export const boothsOverlap = (first: VisualBooth, second: VisualBooth) => {
  const firstCorners = boothCorners(first);
  const secondCorners = boothCorners(second);
  return !hasSeparatingAxis(firstCorners, secondCorners) && !hasSeparatingAxis(secondCorners, firstCorners);
};

const elementCorners = (element: PlanElement): Point[] => {
  const center = { x: element.x + element.width / 2, y: element.y + element.height / 2 };
  return [
    { x: element.x, y: element.y },
    { x: element.x + element.width, y: element.y },
    { x: element.x + element.width, y: element.y + element.height },
    { x: element.x, y: element.y + element.height },
  ].map(point => rotatePoint(point, center, element.rotation));
};

const boothOverlapsObstacle = (booth: VisualBooth, element: PlanElement) => {
  if (!['aisle', 'column', 'restricted'].includes(element.type)) return false;
  const boothPoints = boothCorners(booth);
  const obstaclePoints = elementCorners(element);
  return !hasSeparatingAxis(boothPoints, obstaclePoints) && !hasSeparatingAxis(obstaclePoints, boothPoints);
};

export const annotateBooths = (
  booths: VisualBooth[],
  hallWidth: number,
  hallHeight: number,
  elements: PlanElement[] = [],
): VisualBooth[] =>
  booths.map(booth => {
    const corners = boothCorners(booth);
    const outOfBounds = corners.some(point =>
      point.x < -0.001 || point.y < -0.001 || point.x > hallWidth + 0.001 || point.y > hallHeight + 0.001);
    const hasCollision = booths.some(other => other.key !== booth.key && boothsOverlap(booth, other)) ||
      elements.some(element => boothOverlapsObstacle(booth, element));
    return { ...booth, hasCollision, outOfBounds };
  });

export const findFreePosition = (
  booths: VisualBooth[],
  width: number,
  height: number,
  hallWidth: number,
  hallHeight: number,
  spacing: number,
): Point | null => {
  const step = Math.max(0.5, spacing / 2);
  for (let y = spacing; y + height <= hallHeight - spacing + 0.001; y += step) {
    for (let x = spacing; x + width <= hallWidth - spacing + 0.001; x += step) {
      const candidate: VisualBooth = {
        key: 'candidate', number: '', x, y, width, height, rotation: 0,
        shapeType: 'Rect', boothType: 'Standard', shapePolygonJSON: '', status: 'Available',
        hasCollision: false, outOfBounds: false,
      };
      if (!booths.some(booth => boothsOverlap(candidate, booth))) return { x: round(x), y: round(y) };
    }
  }
  return null;
};

export const autoArrangeBooths = (
  booths: VisualBooth[],
  hallWidth: number,
  hallHeight: number,
  aisle: number,
): VisualBooth[] => {
  let x = aisle;
  let y = aisle;
  let rowHeight = 0;
  return booths.map(booth => {
    const width = booth.width;
    const height = booth.height;
    if (x + width > hallWidth - aisle + 0.001) {
      x = aisle;
      y += rowHeight + aisle;
      rowHeight = 0;
    }
    const arranged = { ...booth, x: round(x), y: round(y), rotation: 0 };
    x += width + aisle;
    rowHeight = Math.max(rowHeight, height);
    if (arranged.y + height > hallHeight - aisle + 0.001) arranged.outOfBounds = true;
    return arranged;
  });
};

export const readPlanDocument = (value?: string): HallPlanDocument => {
  if (!value?.trim()) return { version: 2, elements: [] };
  try {
    const parsed = JSON.parse(value) as Partial<HallPlanDocument> & { BackgroundImagePath?: string };
    if (parsed.version === 2 && Array.isArray(parsed.elements)) {
      const elements = (parsed.elements as Array<Omit<PlanElement, 'type'> & { type: PlanElementType | 'hallDoor' }>)
        .filter((element): element is PlanElement => element.type !== 'hallDoor');
      return { version: 2, elements, backgroundImagePath: parsed.backgroundImagePath };
    }
    const legacyBackground = parsed.BackgroundImagePath ?? parsed.backgroundImagePath;
    return { version: 2, elements: [], backgroundImagePath: legacyBackground };
  } catch {
    return { version: 2, elements: [] };
  }
};

export const statusLabel: Record<BoothStatus, string> = {
  Available: 'متاح',
  Reserved: 'محجوز',
  Merged: 'مدمج',
  Maintenance: 'صيانة',
};

export const statusColor: Record<BoothStatus, { fill: string; stroke: string; text: string }> = {
  Available: { fill: '#dcfce7', stroke: '#16a34a', text: '#166534' },
  Reserved: { fill: '#dbeafe', stroke: '#2563eb', text: '#1e3a8a' },
  Merged: { fill: '#ede9fe', stroke: '#7c3aed', text: '#5b21b6' },
  Maintenance: { fill: '#fef3c7', stroke: '#d97706', text: '#92400e' },
};
