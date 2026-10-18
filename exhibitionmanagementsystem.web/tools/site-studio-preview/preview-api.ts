import {
  boothIssues,
  loadBoothDocument,
  type StudioApiBooth,
  type StudioHall,
} from "../../src/features/booths/utils/boothStudio";
import {
  emptyDocument,
  makeShape,
  serializeDocument,
  type EditorShape,
  type HallSource,
} from "../../src/features/venues/utils/siteEditor";
const document = emptyDocument();
const shape = (kind: EditorShape["kind"], points: number[][], label = "") => ({
  ...makeShape(
    kind,
    points.map(([x, y]) => ({ x, y })),
    crypto.randomUUID(),
  ),
  label,
});
const hallA = {
  ...shape(
    "hall",
    [
      [9, 12],
      [43, 12],
      [43, 27],
      [29, 27],
      [29, 46],
      [9, 46],
    ],
    "القاعة الرئيسية",
  ),
  hallID: 1,
  strokeWidth: 0.25,
};
const hallB = {
  ...shape(
    "hall",
    [
      [51, 17],
      [57, 12],
      [80, 12],
      [87, 18],
      [87, 30],
      [81, 35],
      [51, 35],
    ],
    "قاعة العروض",
  ),
  hallID: 2,
  fill: "#f5f3ff",
  stroke: "#7c3aed",
  strokeWidth: 0.25,
};
const hallC = {
  ...shape(
    "hall",
    [
      [54, 46],
      [83, 46],
      [83, 63],
      [54, 63],
    ],
    "قاعة المؤتمرات",
  ),
  hallID: 3,
  fill: "#fff7ed",
  stroke: "#c27822",
  strokeWidth: 0.25,
};
const welcome = shape(
  "text",
  [
    [8, 57],
    [39, 60],
  ],
  "ساحة الاستقبال",
);
welcome.stroke = "#64748b";
welcome.fontSize = 1.5;
const route = shape(
  "rectangle",
  [
    [35, 39],
    [87, 42],
  ],
  "ممر الربط",
);
route.layer = "movement";
route.fill = "#f0fdf4";
route.stroke = "#86b99a";
route.fontSize = 1.1;
const door = (x: number, y: number, rotation = 0) => ({
  ...shape("door", [
    [x, y],
    [x + 2, y + 2],
  ]),
  rotation,
});
document.shapes = [
  hallA,
  hallB,
  hallC,
  route,
  welcome,
  shape("wall", [
    [25, 12],
    [25, 27],
  ]),
  shape("wall", [
    [9, 27],
    [25, 27],
  ]),
  shape("wall", [
    [68, 46],
    [68, 63],
  ]),
  door(13, 12),
  door(39, 27, 180),
  door(59, 35, 180),
  door(59, 46),
  shape("column", [
    [16, 19],
    [17.2, 20.2],
  ]),
  shape("column", [
    [35, 19],
    [36.2, 20.2],
  ]),
  shape("column", [
    [58, 22],
    [59.2, 23.2],
  ]),
  shape("dimension", [
    [9, 8],
    [43, 8],
  ]),
  shape("dimension", [
    [91, 12],
    [91, 35],
  ]),
  shape("exit", [
    [9, 42],
    [12, 44],
  ]),
  shape("exit", [
    [83, 27],
    [86, 29],
  ]),
  shape(
    "text",
    [
      [9, 49],
      [29, 51],
    ],
    "مدخل الموقع",
  ),
];
let sources: HallSource[] = [
  {
    hallID: 1,
    hallName: "القاعة الرئيسية",
    floorPlanWidth: 34,
    floorPlanHeight: 34,
    isActive: true,
  },
  {
    hallID: 2,
    hallName: "قاعة العروض",
    floorPlanWidth: 36,
    floorPlanHeight: 23,
    isActive: true,
  },
  {
    hallID: 3,
    hallName: "قاعة المؤتمرات",
    floorPlanWidth: 29,
    floorPlanHeight: 17,
    isActive: true,
  },
  {
    hallID: 4,
    hallName: "قاعة الاجتماعات",
    floorPlanWidth: 12,
    floorPlanHeight: 8,
    isActive: true,
  },
];
let planJSON = serializeDocument(document),
  version = 1;
try {
  const stored = JSON.parse(
    localStorage.getItem("site-studio-preview-server") ?? "null",
  );
  if (stored) {
    sources = stored.sources;
    planJSON = stored.planJSON;
    version = stored.version;
  }
} catch {
  /* Start from fixture when storage is unavailable. */
}
const persist = () => {
  try {
    localStorage.setItem(
      "site-studio-preview-server",
      JSON.stringify({ sources, planJSON, version }),
    );
  } catch {
    /* Preview also works without browser storage. */
  }
};

interface Fixture {
  booths: StudioApiBooth[];
  floorPlanJSON: string;
}
let boothFixtures: Record<number, Fixture> = {};
try {
  boothFixtures = JSON.parse(
    localStorage.getItem("booth-studio-preview-server") ?? "{}",
  );
} catch {
  /* Independent booth fixtures. */
}
const fixtureHall = (id: number): StudioHall => ({
  ...sources.find((h) => h.hallID === id)!,
  hallID: id,
  venueID: 1,
  floorPlanJSON: boothFixtures[id]?.floorPlanJSON ?? "",
});
const fixture = (id: number): Fixture => {
  if (boothFixtures[id]) return boothFixtures[id];
  const booths: StudioApiBooth[] = [];
  const base = loadBoothDocument(fixtureHall(id), [], planJSON);
  for (let y = 2; y + 3 <= base.bounds.height; y += 5)
    for (let x = 1; x + 3 <= base.bounds.width; x += 4) {
      if (booths.length >= 24) break;
      const b: StudioApiBooth = {
        boothID: id * 100 + booths.length + 1,
        boothNumber: "A-" + String(booths.length + 1).padStart(3, "0"),
        posX: x,
        posY: y,
        width: 3,
        height: 3,
        rotationAngle: 0,
        shapeType: "Rect",
        boothType: "Standard",
        status:
          booths.length === 0
            ? "Reserved"
            : booths.length === 5
              ? "Maintenance"
              : "Available",
      };
      if (
        !boothIssues(
          loadBoothDocument(fixtureHall(id), [...booths, b], planJSON),
        ).some((i) => i.severity === "error")
      )
        booths.push(b);
    }
  return (boothFixtures[id] = { booths, floorPlanJSON: "" });
};
const mockError = (code: string, errorMessage: string) =>
  Object.assign(new Error(errorMessage), {
    isAxiosError: true,
    response: { data: { errorCode: code, errorMessage } },
  });
const refreshFixtures = () => {
  try {
    const site = JSON.parse(
      localStorage.getItem("site-studio-preview-server") ?? "null",
    );
    if (site) {
      sources = site.sources;
      planJSON = site.planJSON;
      version = site.version;
    }
    boothFixtures = {
      ...boothFixtures,
      ...JSON.parse(
        localStorage.getItem("booth-studio-preview-server") ?? "{}",
      ),
    };
  } catch {
    /* Keep the currently loaded fixture if storage is unavailable. */
  }
};
export const api = {
  async get<T>(url: string): Promise<{ data: T }> {
    refreshFixtures();
    let data: unknown;
    if (url.endsWith("/site-plan")) data = { planJSON, version };
    else if (url === "/api/venues")
      data = [{ venueID: 1, name: "مركز المعارض والمؤتمرات" }];
    else if (url.includes("/booths/hall/"))
      data = fixture(Number(url.split("/").at(-1))).booths;
    else if (url.includes("/halls/venue/"))
      data = sources.map((h) => fixtureHall(h.hallID));
    else if (url.includes("/halls/"))
      data = fixtureHall(Number(url.split("/").at(-1)));
    else if (url.includes("/exhibitions/")) data = { venueID: 1 };
    else data = { venueID: 1, name: "مركز المعارض والمؤتمرات" };
    return { data: structuredClone(data) as T };
  },
  async put<T>(
    url: string,
    payload: Record<string, unknown>,
  ): Promise<{ data: T }> {
    if (url.endsWith("/site-plan")) {
      if (payload.expectedVersion !== version)
        throw mockError("SITE_PLAN_CONFLICT", "تغير مخطط الموقع.");
      planJSON = String(payload.planJSON);
      version++;
      persist();
      return { data: { planJSON, version } as T };
    }
    if (url.includes("/booths/hall/") && url.endsWith("/layout")) {
      const id = Number(url.split("/").at(-2)),
        old = fixture(id),
        latestSite = JSON.parse(
          localStorage.getItem("site-studio-preview-server") ?? "null",
        ),
        latestBooths = JSON.parse(
          localStorage.getItem("booth-studio-preview-server") ?? "{}",
        );
      if (
        payload.expectedSitePlanVersion !== (latestSite?.version ?? version) ||
        payload.expectedFloorPlanJSON !==
          (latestBooths[id]?.floorPlanJSON ?? old.floorPlanJSON)
      )
        throw mockError("FLOOR_PLAN_CONFLICT", "تغير مخطط القاعة.");
      let nextID =
        Math.max(
          ...Object.values(boothFixtures).flatMap((f) =>
            f.booths.map((b) => b.boothID),
          ),
          1000,
        ) + 1;
      const booths = (payload.booths as StudioApiBooth[]).map((b) => ({
        ...b,
        boothID: b.boothID ?? nextID++,
        status:
          old.booths.find((o) => o.boothID === b.boothID)?.status ??
          "Available",
      }));
      const floorPlanJSON = JSON.stringify({
          ...JSON.parse(String(payload.floorPlanJSON)),
          layoutRevision: crypto.randomUUID(),
        }),
        issues = boothIssues(
          loadBoothDocument(
            { ...fixtureHall(id), floorPlanJSON },
            booths,
            planJSON,
          ),
        );
      if (issues.some((i) => i.severity === "error"))
        throw mockError("INVALID_LAYOUT", issues[0].message);
      boothFixtures = {
        ...boothFixtures,
        ...latestBooths,
        [id]: { booths, floorPlanJSON },
      };
      localStorage.setItem(
        "booth-studio-preview-server",
        JSON.stringify(boothFixtures),
      );
      return {
        data: {
          booths,
          floorPlanJSON,
          savedAtUtc: new Date().toISOString(),
        } as T,
      };
    }
    const hallID = Number(url.split("/").at(-1)),
      updated = {
        ...sources.find((h) => h.hallID === hallID),
        ...payload,
        hallID,
      } as HallSource;
    sources = sources.map((h) => (h.hallID === hallID ? updated : h));
    persist();
    return { data: updated as T };
  },
  async post<T>(
    _url: string,
    payload: Record<string, unknown>,
  ): Promise<{ data: T }> {
    const hall = {
      ...payload,
      hallID: Math.max(...sources.map((h) => h.hallID), 0) + 1,
    } as unknown as HallSource;
    sources.push(hall);
    persist();
    return { data: hall as T };
  },
};

export const API_BASE_URL = "";
