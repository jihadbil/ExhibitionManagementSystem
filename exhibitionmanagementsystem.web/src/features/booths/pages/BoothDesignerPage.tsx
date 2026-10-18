import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import {
  ArrowRight,
  ArrowLeftRight,
  Building2,
  Check,
  Columns3,
  Copy,
  Download,
  Grid3X3,
  Group,
  Hand,
  Hash,
  HelpCircle,
  Layers3,
  Lock,
  Maximize2,
  Merge,
  MousePointer2,
  PanelRightClose,
  Pentagon,
  Plus,
  Printer,
  Redo2,
  Ruler,
  Save,
  Settings2,
  Square,
  Trash2,
  Type,
  Undo2,
  Unlock,
  X,
  AlertTriangle,
  DoorOpen,
  Pencil,
} from "lucide-react";
import { useAuth } from "../../../app/providers/AuthContext";
import { useToast } from "../../../app/providers/ToastContext";
import { api, API_BASE_URL } from "../../../services/api";
import { getApiErrorMessage } from "../../../services/apiError";
import { SiteCanvas } from "../../venues/components/SiteCanvas";
import { SiteDrawing, SiteShape } from "../../venues/components/SiteShape";
import { SiteAlignmentMenu } from "../../venues/components/SiteAlignmentMenu";
import {
  shortcuts,
  useSiteEditor,
} from "../../venues/components/useSiteEditor";
import {
  editable,
  makeShape,
  polygonArea,
  shapeBounds,
  type EditorDocument,
  type Tool,
} from "../../venues/utils/siteEditor";
import {
  boothTypes,
  boothShapes,
  boothIssues,
  boothPayload,
  documentKey,
  loadBoothDocument,
  mergeBooths,
  nextBoothNumber,
  placeUnpositionedBooth,
  prepareBoothDocument,
  asBoothDocument,
  type StudioApiBooth,
  type StudioHall,
  type BoothDocument,
} from "../utils/boothStudio";
import {
  statusLabel,
  statusColor,
  type BoothStatus,
  type PlanElementType,
} from "../utils/boothLayout";
import {
  MeterInput,
  NumberDialog,
  RowsDialog,
  SplitDialog,
  StudioModal,
} from "../components/BoothStudioDialogs";
import "../../venues/styles/venue-designer.css";
import "../styles/booth-designer.css";

interface Venue {
  venueID: number;
  name: string;
}
interface Plan {
  planJSON: string;
  version: number;
}
const download = (data: string, mime: string, name: string) => {
  const url = URL.createObjectURL(new Blob([data], { type: mime })),
    a = document.createElement("a");
  a.href = url;
  a.download = name;
  a.click();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
};
const message = (error: unknown, fallback: string) =>
  error instanceof Error && !("response" in error)
    ? error.message
    : getApiErrorMessage(error, fallback);
const toolItems: { tool: Tool; label: string; Icon: typeof Square }[] = [
  { tool: "select", label: "تحديد", Icon: MousePointer2 },
  { tool: "pan", label: "تحريك اللوحة", Icon: Hand },
  { tool: "rectangle", label: "رسم جناح مستطيل", Icon: Square },
  { tool: "polygon", label: "رسم جناح متعدد الزوايا", Icon: Pentagon },
  { tool: "nodes", label: "تعديل الزوايا", Icon: Pencil },
  { tool: "dimension", label: "قياس", Icon: Ruler },
  { tool: "text", label: "نص", Icon: Type },
];

export const BoothDesignerPage = () => {
  useEffect(() => {
    const before = document.title;
    document.title = "استوديو الأجنحة — التصميم المباشر";
    return () => {
      document.title = before;
    };
  }, []);
  const [query, setQuery] = useSearchParams();
  const [venues, setVenues] = useState<Venue[]>([]),
    [halls, setHalls] = useState<StudioHall[]>([]),
    [venueID, setVenueID] = useState(0),
    [hallID, setHallID] = useState(0),
    [error, setError] = useState(""),
    [loading, setLoading] = useState(true),
    [attempt, setAttempt] = useState(0);
  useEffect(() => {
    const abort = new AbortController();
    void (async () => {
      try {
        const venuesResult = await api.get<Venue[]>("/api/venues", {
          signal: abort.signal,
        });
        let venue = venuesResult.data[0]?.venueID ?? 0;
        const requestedHall = Number(query.get("hallId")),
          exhibitionID = Number(query.get("exhibitionId"));
        if (requestedHall > 0)
          venue = (
            await api.get<StudioHall>(`/api/halls/${requestedHall}`, {
              signal: abort.signal,
            })
          ).data.venueID;
        else if (exhibitionID > 0)
          venue = (
            await api.get<{ venueID: number }>(
              `/api/exhibitions/${exhibitionID}`,
              { signal: abort.signal },
            )
          ).data.venueID;
        const hallResult = venue
          ? await api.get<StudioHall[]>(`/api/halls/venue/${venue}`, {
              signal: abort.signal,
            })
          : { data: [] };
        if (abort.signal.aborted) return;
        setVenues(venuesResult.data);
        setVenueID(venue);
        setHalls(hallResult.data);
        setHallID(
          hallResult.data.find((h) => h.hallID === requestedHall)?.hallID ??
            hallResult.data.find((h) => h.isActive)?.hallID ??
            hallResult.data[0]?.hallID ??
            0,
        );
        setError("");
      } catch (e) {
        if (!abort.signal.aborted) setError(message(e, "تعذر تحميل القاعات."));
      } finally {
        if (!abort.signal.aborted) setLoading(false);
      }
    })();
    return () => abort.abort();
    // Query changes record the local selection; initialization only repeats on retry.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [attempt]);
  const chooseHall = (id: number) => {
    setHallID(id);
    setQuery(
      (q) => {
        q.set("hallId", String(id));
        return q;
      },
      { replace: true },
    );
  };
  const chooseVenue = async (id: number) => {
    setLoading(true);
    try {
      const r = await api.get<StudioHall[]>(`/api/halls/venue/${id}`);
      setVenueID(id);
      setHalls(r.data);
      chooseHall(
        r.data.find((h) => h.isActive)?.hallID ?? r.data[0]?.hallID ?? 0,
      );
    } catch (e) {
      setError(message(e, "تعذر تحميل قاعات الموقع."));
    } finally {
      setLoading(false);
    }
  };
  if (loading || error)
    return (
      <main className="vs-studio bs-studio bs-loading" dir="rtl">
        <Building2 size={34} />
        <h2>{loading ? "جارٍ فتح استوديو الأجنحة" : error}</h2>
        {error && (
          <button
            className="vs-save-button"
            onClick={() => {
              setLoading(true);
              setAttempt((a) => a + 1);
            }}
          >
            إعادة المحاولة
          </button>
        )}
      </main>
    );
  const hall = halls.find((h) => h.hallID === hallID);
  if (!hall)
    return (
      <main className="vs-studio bs-studio bs-loading" dir="rtl">
        <Building2 size={34} />
        <h2>أضف قاعة إلى الموقع للبدء</h2>
        <select
          aria-label="الموقع"
          value={venueID}
          onChange={(e) => void chooseVenue(Number(e.target.value))}
        >
          {venues.map((v) => (
            <option key={v.venueID} value={v.venueID}>
              {v.name}
            </option>
          ))}
        </select>
        <button
          className="vs-save-button"
          onClick={() => location.assign(`/venues/${venueID}/designer`)}
        >
          فتح مصمم الموقع
        </button>
      </main>
    );
  return (
    <BoothStudio
      key={`${venueID}:${hallID}`}
      hall={hall}
      halls={halls}
      venues={venues}
      venueID={venueID}
      chooseHall={chooseHall}
      chooseVenue={(id) => void chooseVenue(id)}
    />
  );
};

const BoothStudio = ({
  hall,
  halls,
  venues,
  venueID,
  chooseHall,
  chooseVenue,
}: {
  hall: StudioHall;
  halls: StudioHall[];
  venues: Venue[];
  venueID: number;
  chooseHall: (id: number) => void;
  chooseVenue: (id: number) => void;
}) => {
  const { user } = useAuth(),
    { showToast } = useToast(),
    navigate = useNavigate();
  const [loading, setLoading] = useState(true),
    [loadError, setLoadError] = useState(""),
    [attempt, setAttempt] = useState(0),
    [saving, setSaving] = useState(false),
    [saveError, setSaveError] = useState(""),
    [conflict, setConflict] = useState(false),
    [savedKey, setSavedKey] = useState(""),
    [savedAt, setSavedAt] = useState("");
  const [panel, setPanel] = useState<"booths" | "studio" | "issues">("booths"),
    [panelOpen, setPanelOpen] = useState(true),
    [fullscreen, setFullscreen] = useState(false),
    [search, setSearch] = useState(""),
    [dialog, setDialog] = useState<"rows" | "number" | "split" | "help" | null>(
      null,
    ),
    [pending, setPending] = useState<(() => void) | null>(null),
    [recovery, setRecovery] = useState<{
      document: BoothDocument;
      floor: string;
      version: number;
      stale?: boolean;
    } | null>(null);
  const rawFloor = useRef(hall.floorPlanJSON ?? ""),
    site = useRef<Plan>({ planJSON: "", version: 0 }),
    busy = useRef(false),
    fitted = useRef(false),
    mounted = useRef(true);
  const storageKey = `booth-studio:${user?.tenantID ?? 0}:${user?.userId ?? ""}:${hall.hallID}`;
  const editor = useSiteEditor(
    (m) => showToast(m, "warning"),
    () => {},
    loading || saving || !!dialog || !!pending || !!recovery,
    undefined,
    {
      prepareDocument: prepareBoothDocument,
      allowedTools: toolItems
        .map((t) => t.tool)
        .concat(["line", "freehand", "column", "exit"]),
      selectionFilter: (s) =>
        s.id !== "hall-boundary" && !s.id.startsWith("site-"),
    },
  );
  const { doc, history, single, selected, selection, tool, camera } = editor;
  const committed = asBoothDocument(history.document),
    data = asBoothDocument(doc).boothData ?? {},
    booths = boothShapes(doc),
    selectedBooths = selected.filter((s) => data[s.id]),
    info = single ? data[single.id] : undefined;
  const issues = useMemo(
      () => boothIssues(history.document),
      [history.document],
    ),
    errors = issues.filter((i) => i.severity === "error"),
    key = useMemo(() => documentKey(history.document), [history.document]),
    dirty = !loading && key !== savedKey;
  const staged = booths.filter((s) => data[s.id].needsPlacement),
    selectedEditable =
      selected.length > 0 &&
      selected.every((s) => editable(s, doc) && !s.id.startsWith("site-"));
  const canSplit =
    !!single &&
    !!info &&
    selectedEditable &&
    single.kind === "rectangle" &&
    !single.rotation &&
    info.status === "Available";
  const transaction =
    editor.interacting || !!editor.polyPoints.length || !!editor.editingTextId;
  const disabled = saving || loading || transaction;
  useEffect(() => {
    mounted.current = true;
    const abort = new AbortController();
    void (async () => {
      setLoading(true);
      try {
        const [h, b, p] = await Promise.all([
          api.get<StudioHall>(`/api/halls/${hall.hallID}`, {
            signal: abort.signal,
          }),
          api.get<StudioApiBooth[]>(`/api/booths/hall/${hall.hallID}`, {
            signal: abort.signal,
          }),
          api.get<Plan>(`/api/venues/${venueID}/site-plan`, {
            signal: abort.signal,
          }),
        ]);
        if (abort.signal.aborted) return;
        const next = loadBoothDocument(h.data, b.data, p.data.planJSON);
        site.current = p.data;
        rawFloor.current = h.data.floorPlanJSON ?? "";
        editor.dispatch({ type: "reset", document: next });
        editor.setSelection([]);
        editor.changeTool("select");
        setSavedKey(documentKey(next));
        setLoadError("");
        setSaveError("");
        setConflict(false);
        fitted.current = false;
        try {
          const local = JSON.parse(localStorage.getItem(storageKey) ?? "null");
          if (
            local?.document &&
            documentKey(local.document) !== documentKey(next)
          )
            setRecovery({
              ...local,
              stale:
                local.floor !== (h.data.floorPlanJSON ?? "") ||
                local.version !== p.data.version,
            });
        } catch {
          /* Optional local draft. */
        }
      } catch (e) {
        if (!abort.signal.aborted)
          setLoadError(message(e, "تعذر تحميل مخطط الأجنحة."));
      } finally {
        if (!abort.signal.aborted) setLoading(false);
      }
    })();
    return () => {
      mounted.current = false;
      abort.abort();
    };
    // Initialize per hall, user, or explicit reload.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [hall.hallID, venueID, attempt, storageKey]);
  useEffect(() => {
    if (!loading && !loadError && !fitted.current && editor.size.width > 100) {
      fitted.current = true;
      editor.fit(history.document);
    }
    // Preserve camera during drawing changes.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [loading, loadError, editor.size.width, editor.size.height]);
  useEffect(() => {
    if (loading || loadError || recovery) return;
    const timer = setTimeout(() => {
      try {
        if (dirty)
          localStorage.setItem(
            storageKey,
            JSON.stringify({
              document: committed,
              floor: rawFloor.current,
              version: site.current.version,
            }),
          );
        else localStorage.removeItem(storageKey);
      } catch {
        /* Optional local recovery. */
      }
    }, 400);
    return () => clearTimeout(timer);
  }, [committed, dirty, loading, loadError, recovery, storageKey]);
  useEffect(() => {
    const warn = (e: BeforeUnloadEvent) => {
      if (dirty) {
        e.preventDefault();
        e.returnValue = "";
      }
    };
    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [dirty]);
  const request = (action: () => void) => {
    if (transaction) {
      editor.cancel();
      editor.setPolyPoints([]);
      editor.setEditingTextId(null);
    }
    if (dirty) setPending(() => action);
    else action();
  };
  const apply = (next: EditorDocument | BoothDocument): boolean => {
    if (busy.current || transaction) return false;
    if (!editor.changeDocument(next)) return false;
    const added = next.shapes.filter(
      (s) => !committed.shapes.some((old) => old.id === s.id),
    );
    if (added.length) editor.setSelection(added.map((s) => s.id));
    return true;
  };
  const run = (action: () => EditorDocument) => {
    try {
      apply(action());
    } catch (e) {
      showToast(message(e, "تعذر تطبيق التعديل."), "warning");
    }
  };
  const save = useCallback(async (): Promise<boolean> => {
    if (
      busy.current ||
      loading ||
      editor.interacting ||
      editor.polyPoints.length ||
      editor.editingTextId ||
      recovery ||
      conflict
    )
      return false;
    const snapshot = editor.documentRef.current,
      validation = boothIssues(snapshot).filter((i) => i.severity === "error");
    if (validation.length) {
      setPanel("issues");
      setPanelOpen(true);
      showToast(validation[0].message, "warning");
      return false;
    }
    const payload = boothPayload(snapshot);
    busy.current = true;
    setSaving(true);
    setSaveError("");
    try {
      const response = await api.put<{
        booths: StudioApiBooth[];
        savedAtUtc: string;
        floorPlanJSON: string;
      }>(`/api/booths/hall/${hall.hallID}/layout`, {
        ...payload,
        expectedFloorPlanJSON: rawFloor.current,
        expectedSitePlanVersion: site.current.version,
      });
      if (!mounted.current) return true;
      const savedFloor = response.data.floorPlanJSON ?? payload.floorPlanJSON,
        next = loadBoothDocument(
          { ...hall, floorPlanJSON: savedFloor },
          response.data.booths,
          site.current.planJSON,
        );
      rawFloor.current = savedFloor;
      editor.dispatch({ type: "reset", document: next });
      editor.setSelection([]);
      setSavedKey(documentKey(next));
      setSavedAt(
        new Date(response.data.savedAtUtc).toLocaleTimeString("ar-LY", {
          hour: "2-digit",
          minute: "2-digit",
        }),
      );
      try {
        localStorage.removeItem(storageKey);
      } catch {
        /* Optional storage. */
      }
      showToast("تم حفظ مخطط الأجنحة في قاعدة البيانات.", "success");
      return true;
    } catch (e) {
      const code = (e as { response?: { data?: { errorCode?: string } } })
        ?.response?.data?.errorCode;
      const stale = code === "FLOOR_PLAN_CONFLICT" || code === "LAYOUT_STALE";
      setConflict(stale);
      const text = stale
        ? "تغير المخطط في جلسة أخرى. نزّل نسخة من عملك ثم أعد تحميل القاعة."
        : message(e, "تعذر الحفظ. احتفظت بمسودتك، ويمكنك إعادة المحاولة.");
      setSaveError(text);
      showToast(text, "danger");
      return false;
    } finally {
      busy.current = false;
      if (mounted.current) setSaving(false);
    }
    // Save reads the current editor snapshot, without capturing the document.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    loading,
    recovery,
    conflict,
    hall,
    storageKey,
    showToast,
    editor.interacting,
    editor.polyPoints.length,
    editor.editingTextId,
  ]);
  useEffect(() => {
    const shortcut = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "s") {
        e.preventDefault();
        if (!dialog && !pending) void save();
      }
    };
    window.addEventListener("keydown", shortcut);
    return () => window.removeEventListener("keydown", shortcut);
  }, [save, dialog, pending]);
  const addFreeBooth = () => {
    const id = crypto.randomUUID(),
      s = {
        ...makeShape(
          "rectangle",
          [
            { x: 0, y: 0 },
            { x: 3, y: 3 },
          ],
          id,
        ),
        label: nextBoothNumber(committed),
      };
    const next = prepareBoothDocument(
      {
        ...committed,
        shapes: [...committed.shapes, s],
        boothData: {
          ...committed.boothData,
          [id]: {
            boothType: "Standard",
            status: "Available",
            needsPlacement: true,
          },
        },
      },
      committed,
    );
    try {
      const placed = placeUnpositionedBooth(next, id);
      if (apply(placed)) editor.setSelection([id]);
    } catch (e) {
      showToast(message(e, "لا توجد مساحة خالية."), "warning");
    }
  };
  const addElement = (type: PlanElementType) => {
    const id = crypto.randomUUID(),
      column = type === "column",
      width = column ? 1 : 6,
      height = column ? 1 : 2,
      kind = type === "entrance" || type === "exit" ? "exit" : "rectangle",
      s = {
        ...makeShape(
          kind,
          [
            { x: 1, y: 1 },
            { x: 1 + width, y: 1 + height },
          ],
          id,
        ),
        label:
          type === "aisle"
            ? "ممر"
            : type === "restricted"
              ? "منطقة محظورة"
              : column
                ? "عمود"
                : type === "entrance"
                  ? "مدخل"
                  : "مخرج",
        fill:
          type === "aisle"
            ? "#eff6ff"
            : type === "restricted"
              ? "#fff1f2"
              : "#e2e8f0",
        stroke: type === "restricted" ? "#be123c" : "#64748b",
        fontSize: 0.7,
        layer: "movement" as const,
      };
    if (
      apply({
        ...committed,
        shapes: [...committed.shapes, s],
        elementData: { ...committed.elementData, [id]: { type } },
      })
    )
      editor.setSelection([id]);
  };
  const updateInfo = (type: string) => {
    if (!single || !info || info.protected) return;
    apply({
      ...committed,
      boothData: {
        ...committed.boothData,
        [single.id]: { ...info, boothType: type },
      },
    });
  };
  const selectShape = (s: (typeof doc.shapes)[number]) => {
    editor.changeTool("select");
    editor.setSelection([s.id]);
    if (!data[s.id]?.needsPlacement) {
      const b = shapeBounds(s);
      if (
        b.x < camera.x ||
        b.y < camera.y ||
        b.x + b.width > camera.x + editor.size.width / camera.zoom ||
        b.y + b.height > camera.y + editor.size.height / camera.zoom
      )
        editor.setCamera({
          ...camera,
          x: b.x + b.width / 2 - editor.size.width / camera.zoom / 2,
          y: b.y + b.height / 2 - editor.size.height / camera.zoom / 2,
        });
    }
  };
  const selectedError = single ? errors.filter((i) => i.id === single.id) : [];
  const exportDraft = () =>
    download(
      JSON.stringify(
        {
          format: "booth-studio",
          hallID: hall.hallID,
          document: committed,
          floor: rawFloor.current,
          version: site.current.version,
        },
        null,
        2,
      ),
      "application/json",
      `hall-${hall.hallID}-draft.json`,
    );
  const backgroundPath = asBoothDocument(doc).backgroundImagePath,
    backgroundURL =
      backgroundPath &&
      (/^(https?:|data:image\/(png|jpeg|webp|gif);base64,)/i.test(
        backgroundPath,
      )
        ? backgroundPath
        : backgroundPath.startsWith("/")
          ? API_BASE_URL + backgroundPath
          : undefined);
  const title = venues.find((v) => v.venueID === venueID)?.name ?? "";
  if (loading || loadError)
    return (
      <main
        className={`vs-studio bs-studio bs-loading ${fullscreen ? "vs-fullscreen" : ""}`}
        dir="rtl"
      >
        <Building2 size={34} />
        <h2>{loading ? "جارٍ تحميل مخطط القاعة" : loadError}</h2>
        {loadError && (
          <>
            <button
              className="vs-save-button"
              onClick={() => setAttempt((a) => a + 1)}
            >
              إعادة المحاولة
            </button>
            <button onClick={() => navigate(`/venues/${venueID}/designer`)}>
              فتح مصمم الموقع لتحديد حدود القاعة
            </button>
            <select
              aria-label="القاعة"
              value={hall.hallID}
              onChange={(e) => chooseHall(Number(e.target.value))}
            >
              {halls.map((h) => (
                <option key={h.hallID} value={h.hallID}>
                  {h.hallName}
                </option>
              ))}
            </select>
          </>
        )}
      </main>
    );
  return (
    <main
      className={`vs-studio bs-studio ${fullscreen ? "vs-fullscreen" : ""}`}
      dir="rtl"
    >
      <header className="vs-header">
        <button
          className="vs-back"
          title="العودة إلى الأجنحة"
          onClick={() => request(() => navigate("/booths"))}
        >
          <ArrowRight size={18} />
        </button>
        <div className="vs-brand">
          <span>
            <Layers3 size={23} />
          </span>
          <div>
            <strong>
              استوديو الأجنحة <span>/ التصميم المباشر</span>
            </strong>
            <small>{title}</small>
          </div>
        </div>
        <span
          className={`vs-save-status ${saving ? "saving" : saveError ? "error" : ""}`}
          role="status"
        >
          <span />
          {saving
            ? "جارٍ الحفظ…"
            : dirty
              ? "تعديلات غير محفوظة"
              : savedAt
                ? `تم الحفظ ${savedAt}`
                : "متزامن مع قاعدة البيانات"}
        </span>
        <div className="vs-header-actions">
          <button
            title="المساعدة والاختصارات"
            onClick={() => setDialog("help")}
          >
            <HelpCircle size={17} />
          </button>
          <button
            title={fullscreen ? "الخروج من العرض الواسع" : "عرض واسع"}
            onClick={() => setFullscreen((v) => !v)}
          >
            <Maximize2 size={17} />
          </button>
          <button
            className="vs-export-button"
            title="تنزيل مخطط SVG"
            onClick={() => {
              const svg = document.querySelector(".bs-print>svg");
              if (svg)
                download(
                  new XMLSerializer().serializeToString(svg),
                  "image/svg+xml",
                  `hall-${hall.hallID}.svg`,
                );
            }}
          >
            <Download size={15} />
            <span>تصدير</span>
          </button>
          <button title="طباعة المخطط" onClick={() => window.print()}>
            <Printer size={17} />
          </button>
          <button
            className="vs-save-button"
            disabled={disabled || !dirty || !!errors.length || conflict}
            onClick={() => void save()}
          >
            <Save size={15} />
            {saving ? "جارٍ الحفظ" : "حفظ المخطط"}
          </button>
        </div>
      </header>
      <div className="bs-location">
        <label>
          <Building2 size={15} />
          <select
            aria-label="الموقع"
            disabled={disabled}
            value={venueID}
            onChange={(e) => request(() => chooseVenue(Number(e.target.value)))}
          >
            {venues.map((v) => (
              <option key={v.venueID} value={v.venueID}>
                {v.name}
              </option>
            ))}
          </select>
        </label>
        <span className="bs-location-divider">/</span>
        <select
          aria-label="القاعة"
          disabled={disabled}
          value={hall.hallID}
          onChange={(e) => request(() => chooseHall(Number(e.target.value)))}
        >
          {halls.map((h) => (
            <option key={h.hallID} value={h.hallID}>
              {h.hallName}
              {h.isActive ? "" : " · غير نشطة"}
            </option>
          ))}
        </select>
        <span className="bs-outline-badge">
          <Check size={13} />
          {asBoothDocument(doc).outlineLinked
            ? "حدود القاعة مرتبطة بمخطط الموقع"
            : "أبعاد القاعة من قاعدة البيانات"}
        </span>
        <button
          disabled={saving}
          onClick={() => request(() => navigate(`/venues/${venueID}/designer`))}
        >
          مخطط الموقع <ArrowRight size={13} />
        </button>
      </div>
      <div
        className="vs-toolbar bs-toolbar"
        inert={saving || !!dialog || !!pending || !!recovery}
      >
        <div className="vs-tool-group">
          {toolItems.map(({ tool: t, label, Icon }) => (
            <button
              key={t}
              className={tool === t ? "active" : ""}
              aria-label={label}
              aria-pressed={tool === t}
              title={`${label}${shortcuts[t] ? ` (${shortcuts[t]})` : ""}`}
              onClick={() => editor.changeTool(t)}
            >
              <Icon size={18} />
            </button>
          ))}
        </div>
        <div className="vs-tool-group">
          <button
            title="تراجع Ctrl Z"
            disabled={!history.past.length || disabled}
            onClick={() => editor.undo()}
          >
            <Undo2 size={17} />
          </button>
          <button
            title="إعادة Ctrl Y"
            disabled={!history.future.length || disabled}
            onClick={() => editor.undo(true)}
          >
            <Redo2 size={17} />
          </button>
        </div>
        <button className="bs-create" onClick={addFreeBooth}>
          <Plus size={16} />
          جناح جديد
        </button>
        <button
          className="bs-create"
          onClick={() => setDialog("rows")}
          disabled={transaction}
        >
          <Grid3X3 size={16} />
          إنشاء صفوف
        </button>
        <span className="vs-toolbar-spacer" />
        <button
          className={`bs-studio-toggle ${panel === "studio" && panelOpen ? "active" : ""}`}
          onClick={() => {
            setPanel("studio");
            setPanelOpen((v) => (panel === "studio" ? !v : true));
          }}
        >
          <Settings2 size={16} />
          أدوات الاستوديو
        </button>
        <button
          aria-label="عرض لوحة الخصائص"
          onClick={() => setPanelOpen((v) => !v)}
        >
          <PanelRightClose size={17} />
        </button>
      </div>
      {!!saveError && (
        <div className="bs-save-error" role="alert">
          <AlertTriangle size={16} />
          {saveError}
          <button onClick={exportDraft}>تنزيل المسودة</button>
          {conflict && (
            <button
              disabled={saving}
              onClick={() =>
                request(() => {
                  try {
                    localStorage.removeItem(storageKey);
                  } catch {
                    /* Optional storage. */
                  }
                  setAttempt((a) => a + 1);
                })
              }
            >
              إعادة تحميل القاعة
            </button>
          )}
        </div>
      )}
      <div
        className={`bs-workspace ${panelOpen ? "" : "collapsed"}`}
        inert={saving || !!dialog || !!pending || !!recovery}
      >
        {panelOpen && (
          <aside className="bs-sidebar">
            <nav className="vs-panel-tabs">
              <button
                className={panel === "booths" ? "active" : ""}
                onClick={() => setPanel("booths")}
              >
                الأجنحة <small>{booths.length}</small>
              </button>
              <button
                className={panel === "studio" ? "active" : ""}
                onClick={() => setPanel("studio")}
              >
                الاستوديو
              </button>
              <button
                className={panel === "issues" ? "active" : ""}
                onClick={() => setPanel("issues")}
              >
                الفحص{" "}
                <small className={errors.length ? "bs-error-count" : ""}>
                  {errors.length}
                </small>
              </button>
            </nav>
            <div className="bs-sidebar-body">
              {single && (
                <section className="bs-properties">
                  <div className="bs-section-title">
                    <strong>
                      {info
                        ? `الجناح ${single.label}`
                        : single.id === "hall-boundary"
                          ? "حدود القاعة"
                          : "العنصر المحدد"}
                    </strong>
                    <button
                      title="إلغاء التحديد"
                      onClick={() => editor.setSelection([])}
                    >
                      <X size={14} />
                    </button>
                  </div>
                  {info && (
                    <>
                      <span
                        className="bs-status-pill"
                        style={{
                          background: statusColor[info.status].fill,
                          color: statusColor[info.status].stroke,
                        }}
                      >
                        {statusLabel[info.status]}
                        {info.boothID ? ` · #${info.boothID}` : " · جديد"}
                      </span>
                      <label className="bs-field">
                        <span>رقم الجناح</span>
                        <input
                          key={`${single.id}:${single.label}`}
                          defaultValue={single.label}
                          dir="ltr"
                          maxLength={20}
                          disabled={!selectedEditable}
                          onBlur={(e) => {
                            if (e.target.value !== single.label)
                              editor.patch({ label: e.target.value });
                          }}
                          onKeyDown={(e) => {
                            if (e.key === "Enter") e.currentTarget.blur();
                          }}
                        />
                      </label>
                    </>
                  )}
                  <div className="bs-field-grid">
                    {(
                      [
                        ["x", "الموضع X (م)"],
                        ["y", "الموضع Y (م)"],
                        ["width", "العرض (م)"],
                        ["height", "العمق (م)"],
                        ["rotation", "الدوران (°)"],
                      ] as const
                    ).map(([field, label]) => (
                      <MeterInput
                        key={field}
                        label={label}
                        value={single[field]}
                        min={field === "width" || field === "height" ? 0.5 : 0}
                        max={field === "rotation" ? 359.99 : 10000}
                        step={field === "rotation" ? 1 : 0.1}
                        disabled={!selectedEditable}
                        onChange={(n) => editor.patch({ [field]: n })}
                      />
                    ))}
                  </div>
                  {info && (
                    <>
                      <label className="bs-field">
                        <span>فئة الجناح</span>
                        <select
                          value={info.boothType}
                          disabled={!!info.protected}
                          onChange={(e) => updateInfo(e.target.value)}
                        >
                          {Object.entries(boothTypes).map(([value, label]) => (
                            <option value={value} key={value}>
                              {label}
                            </option>
                          ))}
                        </select>
                      </label>
                      <div className="bs-area">
                        <span>المساحة الفعلية</span>
                        <strong>
                          {(single.kind === "polygon"
                            ? polygonArea(single.points)
                            : single.width * single.height
                          ).toFixed(2)}{" "}
                          م²
                        </strong>
                      </div>
                      {info.assignedPriceRuleID && (
                        <p className="bs-muted">
                          قاعدة التسعير الحالية: #{info.assignedPriceRuleID}
                        </p>
                      )}
                      {info.protected && (
                        <p className="bs-muted">
                          <Lock size={12} />
                          الجناح مرتبط بحجز أو دمج؛ تعديل هندسته مقفل.
                        </p>
                      )}
                      {info.needsPlacement && (
                        <>
                          <button
                            className="bs-wide-button"
                            disabled={!!info.protected}
                            onClick={() =>
                              run(() =>
                                placeUnpositionedBooth(committed, single.id),
                              )
                            }
                          >
                            إيجاد موقع خالٍ بهذا المقاس
                          </button>
                          <button
                            className="bs-wide-button"
                            disabled={!!info.protected}
                            onClick={() =>
                              apply({
                                ...committed,
                                boothData: {
                                  ...committed.boothData,
                                  [single.id]: {
                                    ...info,
                                    needsPlacement: false,
                                  },
                                },
                              })
                            }
                          >
                            اعتماد المقاس والموضع المحددين
                          </button>
                        </>
                      )}
                    </>
                  )}
                  {selectedError.length > 0 && (
                    <p className="bs-inline-error">
                      {selectedError[0].message}
                    </p>
                  )}
                  <div className="bs-property-actions">
                    <button
                      disabled={!selectedEditable}
                      title="إنشاء نسخة Ctrl D"
                      onClick={() => {
                        editor.copy();
                        editor.paste();
                      }}
                    >
                      <Copy size={15} />
                    </button>
                    <button
                      disabled={
                        !info ||
                        !!info.protected ||
                        doc.layers[single.layer].locked
                      }
                      title={single.locked ? "فتح القفل" : "قفل الجناح"}
                      onClick={() => editor.patch({ locked: !single.locked })}
                    >
                      {single.locked ? (
                        <Unlock size={15} />
                      ) : (
                        <Lock size={15} />
                      )}
                    </button>
                    <button
                      disabled={!canSplit}
                      onClick={() => setDialog("split")}
                    >
                      <ArrowLeftRight size={15} />
                      تقسيم
                    </button>
                    <button
                      disabled={!selectedEditable}
                      title="حذف"
                      onClick={editor.remove}
                    >
                      <Trash2 size={15} />
                    </button>
                  </div>
                </section>
              )}
              {selected.length > 1 && (
                <section className="bs-properties">
                  <strong>{selected.length} عناصر محددة</strong>
                  <p className="bs-muted">
                    Shift + نقر لإضافة عنصر إلى التحديد.
                  </p>
                  <SiteAlignmentMenu editor={editor} />
                  <div className="bs-property-actions">
                    <button
                      disabled={!selectedEditable}
                      onClick={() =>
                        editor.group(
                          selected.some((s) => s.groupId !== undefined),
                        )
                      }
                    >
                      <Group size={15} />
                      {selected.some((s) => s.groupId) ? "فك التجميع" : "تجميع"}
                    </button>
                    <button
                      disabled={!selectedEditable}
                      onClick={editor.remove}
                    >
                      <Trash2 size={15} />
                    </button>
                  </div>
                </section>
              )}
              {panel === "booths" && (
                <>
                  <div className="bs-section-title">
                    <strong>أجنحة القاعة</strong>
                    <small>
                      {staged.length
                        ? `${staged.length} بانتظار التوزيع`
                        : "اسحب الجناح لتغيير موقعه"}
                    </small>
                  </div>
                  <input
                    className="bs-search"
                    aria-label="البحث عن جناح"
                    placeholder="ابحث برقم الجناح…"
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                  />
                  <div className="bs-booth-list">
                    {booths
                      .filter((s) =>
                        s.label.toLowerCase().includes(search.toLowerCase()),
                      )
                      .map((s) => (
                        <button
                          key={s.id}
                          className={selection.includes(s.id) ? "selected" : ""}
                          onClick={() => selectShape(s)}
                        >
                          <i
                            style={{
                              background:
                                statusColor[data[s.id].status]?.stroke,
                            }}
                          />
                          <strong dir="ltr">{s.label}</strong>
                          <span>
                            {data[s.id].needsPlacement
                              ? "غير موزع"
                              : `${s.width} × ${s.height}`}
                          </span>
                          {s.locked && <Lock size={11} />}
                        </button>
                      ))}
                  </div>
                  {!booths.length && (
                    <div className="bs-empty">
                      <Square size={27} />
                      <strong>ابدأ أول جناح</strong>
                      <p>ارسمه على اللوحة أو أضف صفوفاً بالمقاسات المطلوبة.</p>
                      <button className="vs-save-button" onClick={addFreeBooth}>
                        <Plus size={14} />
                        إضافة جناح
                      </button>
                    </div>
                  )}
                </>
              )}
              {panel === "studio" && (
                <>
                  <div className="bs-section-title">
                    <strong>هندسة التوزيع</strong>
                    <small>المقاسات بالمتر</small>
                  </div>
                  <div className="bs-engineering-tools">
                    <button onClick={() => setDialog("rows")}>
                      <Grid3X3 size={19} />
                      <strong>صفوف الأجنحة</strong>
                      <small>مقاسات · ممرات · ترقيم</small>
                    </button>
                    <button
                      disabled={
                        !selectedBooths.length ||
                        selectedBooths.some((s) => data[s.id].protected)
                      }
                      onClick={() => setDialog("number")}
                    >
                      <Hash size={19} />
                      <strong>ترقيم متسلسل</strong>
                      <small>الأجنحة المحددة</small>
                    </button>
                    <button
                      disabled={selectedBooths.length < 2 || !selectedEditable}
                      onClick={() =>
                        run(() => mergeBooths(committed, selection))
                      }
                    >
                      <Merge size={19} />
                      <strong>دمج هندسي</strong>
                      <small>أجنحة متاحة ومتلاصقة</small>
                    </button>
                    <button
                      disabled={!canSplit}
                      onClick={() => setDialog("split")}
                    >
                      <ArrowLeftRight size={19} />
                      <strong>تقسيم جناح</strong>
                      <small>أجزاء مستقلة</small>
                    </button>
                  </div>
                  <SiteAlignmentMenu editor={editor} />
                  <div className="bs-section-title">
                    <strong>عناصر القاعة</strong>
                  </div>
                  <div className="bs-element-tools">
                    {(
                      [
                        "aisle",
                        "restricted",
                        "column",
                        "entrance",
                        "exit",
                      ] as const
                    ).map((type) => (
                      <button key={type} onClick={() => addElement(type)}>
                        {type === "column" ? (
                          <Columns3 size={16} />
                        ) : type === "entrance" || type === "exit" ? (
                          <DoorOpen size={16} />
                        ) : (
                          <Square size={16} />
                        )}
                        {
                          {
                            aisle: "ممر",
                            restricted: "منطقة محظورة",
                            column: "عمود",
                            entrance: "مدخل",
                            exit: "مخرج",
                          }[type]
                        }
                      </button>
                    ))}
                  </div>
                  <div className="bs-element-tools">
                    <button onClick={() => editor.changeTool("line")}>
                      <Ruler size={16} />
                      خط توضيحي
                    </button>
                    <button onClick={() => editor.changeTool("freehand")}>
                      <Pencil size={16} />
                      رسم حر
                    </button>
                  </div>
                  <div className="bs-section-title">
                    <strong>الشبكة والالتقاط</strong>
                  </div>
                  <label className="bs-check">
                    <input
                      type="checkbox"
                      checked={doc.grid.visible}
                      onChange={(e) =>
                        apply({
                          ...committed,
                          grid: { ...doc.grid, visible: e.target.checked },
                        })
                      }
                    />
                    إظهار الشبكة
                  </label>
                  <label className="bs-check">
                    <input
                      type="checkbox"
                      checked={doc.grid.snap}
                      onChange={(e) =>
                        apply({
                          ...committed,
                          grid: { ...doc.grid, snap: e.target.checked },
                        })
                      }
                    />
                    الالتقاط على الشبكة
                  </label>
                  <MeterInput
                    label="خطوة الشبكة (م)"
                    value={doc.grid.size}
                    min={0.1}
                    max={10}
                    onChange={(n) => {
                      if (n >= 0.1 && n <= 10)
                        apply({ ...committed, grid: { ...doc.grid, size: n } });
                    }}
                  />
                  <button className="bs-wide-button" onClick={exportDraft}>
                    <Download size={14} />
                    تنزيل نسخة من المسودة
                  </button>
                  <p className="bs-muted">
                    حدود القاعة والجدران الأصلية تُعدَّل من مصمم الموقع.
                  </p>
                </>
              )}
              {panel === "issues" && (
                <>
                  <div
                    className={`bs-validation ${errors.length ? "invalid" : ""}`}
                  >
                    {errors.length ? (
                      <AlertTriangle size={26} />
                    ) : (
                      <Check size={26} />
                    )}
                    <strong>
                      {errors.length
                        ? `${errors.length} ملاحظات تحتاج المعالجة`
                        : "المخطط جاهز للحفظ"}
                    </strong>
                    <p>
                      {errors.length
                        ? "راجع العناصر الموضحة أدناه."
                        : "الأجنحة داخل القاعة، والمسارات خالية من التداخل."}
                    </p>
                  </div>
                  {errors.map((issue, i) => (
                    <button
                      key={`${issue.id}:${i}`}
                      className="bs-issue"
                      onClick={() => {
                        const s = doc.shapes.find((s) => s.id === issue.id);
                        if (s) selectShape(s);
                      }}
                    >
                      <AlertTriangle size={15} />
                      <span>{issue.message}</span>
                    </button>
                  ))}
                </>
              )}
            </div>
            <footer className="bs-sidebar-footer">
              <Lock size={12} />
              حالة الحجز مرتبطة بقاعدة البيانات
            </footer>
          </aside>
        )}
        <div className="bs-canvas-wrap">
          <SiteCanvas
            editor={editor}
            hallDraft={null}
            available={[]}
            placeHall={() => {}}
            openHalls={() => {}}
            openPageSize={() => {}}
            title={hall.hallName}
            canvasLabel="لوحة تصميم الأجنحة"
            paperFill="#e8edf3"
            resizePageEnabled={false}
            contextActions={null}
            renderContextMenu={(position, close) => (
              <div
                className="vs-site-context-menu"
                role="menu"
                aria-label="أوامر الأجنحة"
                style={{
                  left: Math.max(
                    8,
                    Math.min(editor.size.width - 214, position.x),
                  ),
                  top: Math.max(
                    28,
                    Math.min(editor.size.height - 245, position.y),
                  ),
                }}
              >
                <button
                  role="menuitem"
                  onClick={() => {
                    close();
                    addFreeBooth();
                  }}
                >
                  <Plus size={15} />
                  جناح جديد
                </button>
                <button
                  role="menuitem"
                  onClick={() => {
                    close();
                    setDialog("rows");
                  }}
                >
                  <Grid3X3 size={15} />
                  إنشاء صفوف
                </button>
                <button
                  role="menuitem"
                  disabled={!selectedEditable}
                  onClick={() => {
                    close();
                    editor.copy();
                    editor.paste();
                  }}
                >
                  <Copy size={15} />
                  إنشاء نسخة
                </button>
                <button
                  role="menuitem"
                  disabled={!canSplit}
                  onClick={() => {
                    close();
                    setDialog("split");
                  }}
                >
                  <ArrowLeftRight size={15} />
                  تقسيم الجناح
                </button>
                <button
                  role="menuitem"
                  disabled={!selectedEditable}
                  onClick={() => {
                    close();
                    editor.remove();
                  }}
                >
                  <Trash2 size={15} />
                  حذف المحدد
                </button>
                <button
                  role="menuitem"
                  onClick={() => {
                    close();
                    editor.setSelection(
                      booths.filter((s) => !s.locked).map((s) => s.id),
                    );
                  }}
                >
                  تحديد الأجنحة القابلة للتعديل
                </button>
              </div>
            )}
            renderShape={(s) => {
              const meta = data[s.id];
              if (meta?.needsPlacement) return null;
              if (s.id === "hall-boundary")
                return (
                  <g pointerEvents="none">
                    <SiteShape shape={s} />
                    {backgroundURL && (
                      <>
                        <defs>
                          <clipPath id="booth-hall-clip">
                            <path
                              d={
                                s.points
                                  .map(
                                    (p, i) => `${i ? "L" : "M"} ${p.x} ${p.y}`,
                                  )
                                  .join(" ") + " Z"
                              }
                            />
                          </clipPath>
                        </defs>
                        <image
                          href={backgroundURL}
                          width={s.width}
                          height={s.height}
                          opacity=".3"
                          preserveAspectRatio="none"
                          clipPath="url(#booth-hall-clip)"
                        />
                      </>
                    )}
                    {doc.grid.visible && (
                      <path
                        d={
                          s.points
                            .map((p, i) => `${i ? "L" : "M"} ${p.x} ${p.y}`)
                            .join(" ") + " Z"
                        }
                        fill="url(#studio-grid)"
                      />
                    )}
                  </g>
                );
              if (s.id.startsWith("site-"))
                return (
                  <g pointerEvents="none">
                    <SiteShape shape={s} />
                  </g>
                );
              const error = errors.some((i) => i.id === s.id);
              return (
                <>
                  <SiteShape
                    shape={
                      error && meta
                        ? { ...s, fill: "#fff1f2", stroke: "#e11d48" }
                        : s
                    }
                    selected={selection.includes(s.id)}
                    hitArea
                    zoom={camera.zoom}
                  />
                  {meta && (
                    <g
                      transform={`translate(${s.x} ${s.y}) rotate(${s.rotation} ${s.width / 2} ${s.height / 2})`}
                      pointerEvents="none"
                    >
                      <title>
                        {s.label} · {statusLabel[meta.status]} · {s.width} ×{" "}
                        {s.height} م
                      </title>
                      {s.kind === "rectangle" &&
                        camera.zoom > 13 &&
                        s.height > 2 && (
                          <text
                            x={s.width / 2}
                            y={s.height * 0.72}
                            textAnchor="middle"
                            fill={error ? "#be123c" : "#64748b"}
                            fontSize={Math.min(0.35, s.width * 0.1)}
                          >
                            {s.width} × {s.height} م
                          </text>
                        )}
                      {s.locked && (
                        <text
                          x={s.width - 0.35}
                          y={0.45}
                          fontSize=".35"
                          fill="#64748b"
                        >
                          ●
                        </text>
                      )}
                    </g>
                  )}
                </>
              );
            }}
          />
          {selectedBooths.length > 0 && !transaction && (
            <div
              className="bs-context-actions"
              role="toolbar"
              aria-label="أوامر الأجنحة المحددة"
            >
              <strong>
                {selectedBooths.length === 1
                  ? selectedBooths[0].label
                  : `${selectedBooths.length} أجنحة`}
              </strong>
              <span />
              <button
                disabled={!selectedEditable}
                title="إنشاء نسخة"
                onClick={() => {
                  editor.copy();
                  editor.paste();
                }}
              >
                <Copy size={15} />
              </button>
              {canSplit && (
                <button onClick={() => setDialog("split")}>
                  <ArrowLeftRight size={15} />
                  تقسيم
                </button>
              )}
              {selectedBooths.length > 1 && (
                <>
                  <SiteAlignmentMenu editor={editor} />
                  <button
                    disabled={
                      !selectedEditable ||
                      selectedBooths.some((s) => data[s.id].protected)
                    }
                    onClick={() => setDialog("number")}
                  >
                    <Hash size={15} />
                    ترقيم
                  </button>
                </>
              )}
              <button
                disabled={!selectedEditable}
                title="حذف الأجنحة المحددة"
                onClick={editor.remove}
              >
                <Trash2 size={15} />
              </button>
            </div>
          )}
        </div>
      </div>
      <footer className="bs-footer">
        <div className="bs-metrics">
          <span>
            <b>{booths.length}</b> جناحاً
          </span>
          <span>
            <b>
              {booths
                .reduce(
                  (a, s) =>
                    a +
                    (s.kind === "polygon"
                      ? polygonArea(s.points)
                      : s.width * s.height),
                  0,
                )
                .toFixed(1)}
            </b>{" "}
            م² أجنحة
          </span>
          <button
            onClick={() => {
              setPanel("issues");
              setPanelOpen(true);
            }}
            className={errors.length ? "bs-has-errors" : ""}
          >
            {errors.length ? <AlertTriangle size={13} /> : <Check size={13} />}{" "}
            {errors.length ? `${errors.length} ملاحظات` : "اجتاز الفحص"}
          </button>
        </div>
        <div className="bs-legend">
          {Object.entries(statusLabel).map(([status, label]) => (
            <span key={status}>
              <i
                style={{
                  background: statusColor[status as BoothStatus].stroke,
                }}
              />
              {label}
            </span>
          ))}
        </div>
        <span dir="ltr" className="bs-coordinate">
          X {editor.cursor.x.toFixed(2)} · Y {editor.cursor.y.toFixed(2)} m
        </span>
      </footer>
      {dialog === "rows" && (
        <RowsDialog
          doc={committed}
          apply={apply}
          close={() => setDialog(null)}
        />
      )}
      {dialog === "number" && (
        <NumberDialog
          doc={committed}
          ids={selection}
          apply={apply}
          close={() => setDialog(null)}
        />
      )}
      {dialog === "split" && single && (
        <SplitDialog
          doc={committed}
          id={single.id}
          apply={apply}
          close={() => setDialog(null)}
        />
      )}
      {dialog === "help" && (
        <StudioModal
          title="التصميم المباشر والاختصارات"
          close={() => setDialog(null)}
        >
          <div className="bs-help">
            <p>
              ارسم المستطيل بالسحب. للجناح متعدد الزوايا، انقر زواياه ثم اضغط
              Enter للإغلاق.
            </p>
            <p>
              حرّك الجناح مباشرة، أو استخدم حقول الموضع والمقاس. الخطوط الوردية
              تعني تطابق الحواف أو المراكز.
            </p>
            <p>
              Shift + نقر: تحديد متعدد · Space + سحب: تحريك اللوحة · العجلة:
              تكبير.
            </p>
            <p dir="ltr">
              V Select · R Rectangle · A Nodes · Ctrl D Duplicate · Ctrl Z Undo
              · Ctrl S Save
            </p>
            <p>
              الدمج والتقسيم هنا يغيّران هندسة الأجنحة المتاحة. الأجنحة المرتبطة
              بحجوزات أو دمج تجاري مقفلة.
            </p>
            <p>
              يُحفظ المخطط بزر الحفظ. يبدأ سجل التراجع من النسخة المحفوظة بعد
              نجاح الحفظ.
            </p>
          </div>
          <footer>
            <button className="vs-save-button" onClick={() => setDialog(null)}>
              فهمت
            </button>
          </footer>
        </StudioModal>
      )}
      {pending && (
        <StudioModal
          title="لديك تعديلات غير محفوظة"
          close={() => {
            if (!saving) setPending(null);
          }}
        >
          <p className="bs-muted">
            احفظ مخطط القاعة قبل المغادرة، أو تابع بعد تجاهل التعديلات.
          </p>
          <footer>
            <button disabled={saving} onClick={() => setPending(null)}>
              البقاء
            </button>
            <button
              disabled={saving}
              onClick={() => {
                try {
                  localStorage.removeItem(storageKey);
                } catch {
                  /* Optional storage. */
                }
                const action = pending;
                setPending(null);
                action();
              }}
            >
              تجاهل ومتابعة
            </button>
            <button
              className="vs-save-button"
              disabled={saving || !!errors.length || conflict}
              onClick={() =>
                void (async () => {
                  if (await save()) {
                    const action = pending;
                    setPending(null);
                    action();
                  }
                })()
              }
            >
              حفظ ومتابعة
            </button>
          </footer>
        </StudioModal>
      )}
      {recovery && (
        <StudioModal
          title="مسودة سابقة لهذه القاعة"
          close={() => {
            setRecovery(null);
          }}
        >
          <p className="bs-muted">وجدت تعديلات محفوظة على هذا المتصفح.</p>
          {!!recovery.stale && (
            <p className="bs-inline-error">
              تغير المخطط المحفوظ؛ نزّل المسودة للمراجعة. الاستعادة المباشرة
              متوقفة لتجنب استبدال بيانات أحدث.
            </p>
          )}
          <footer>
            <button onClick={() => setRecovery(null)}>
              استخدام النسخة المحفوظة
            </button>
            <button
              onClick={() =>
                download(
                  JSON.stringify(recovery, null, 2),
                  "application/json",
                  `hall-${hall.hallID}-recovery.json`,
                )
              }
            >
              تنزيل المسودة
            </button>
            <button
              className="vs-save-button"
              disabled={!!recovery.stale}
              onClick={() => {
                try {
                  const next = prepareBoothDocument(
                    recovery.document,
                    recovery.document,
                  );
                  editor.dispatch({ type: "reset", document: next });
                  setRecovery(null);
                } catch (e) {
                  showToast(message(e, "المسودة غير صالحة."), "danger");
                }
              }}
            >
              استعادة المسودة
            </button>
          </footer>
        </StudioModal>
      )}
      <div className="bs-print">
        <h1>
          {title} — {hall.hallName}
        </h1>
        <SiteDrawing document={committed} />
        <p>{booths.length} جناحاً · المقاسات بالمتر</p>
      </div>
    </main>
  );
};
