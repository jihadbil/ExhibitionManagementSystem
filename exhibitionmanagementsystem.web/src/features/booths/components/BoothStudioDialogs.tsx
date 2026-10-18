import { useEffect, useRef, useState, type ReactNode } from "react";
import { X, Grid3X3, ArrowLeftRight, Hash } from "lucide-react";
import { SiteDrawing } from "../../venues/components/SiteShape";
import {
  boothShapes,
  boothTypes,
  makeBoothRows,
  renumberBooths,
  splitBooth,
  type BoothRowOptions,
} from "../utils/boothStudio";
import type { EditorDocument } from "../../venues/utils/siteEditor";

export const StudioModal = ({
  title,
  children,
  close,
  wide = false,
}: {
  title: string;
  children: ReactNode;
  close: () => void;
  wide?: boolean;
}) => {
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const before = document.activeElement as HTMLElement;
    ref.current?.querySelector<HTMLElement>("input,button,select")?.focus();
    return () => before?.focus();
  }, []);
  return (
    <div
      className="bs-modal-backdrop"
      onMouseDown={(e) => {
        if (e.target === e.currentTarget) close();
      }}
    >
      <div
        ref={ref}
        className={`bs-modal ${wide ? "wide" : ""}`}
        role="dialog"
        aria-modal="true"
        aria-label={title}
        dir="rtl"
        onKeyDown={(event) => {
          if (event.key === "Escape") {
            event.stopPropagation();
            close();
          }
          if (event.key === "Tab") {
            const nodes = [
              ...(ref.current?.querySelectorAll<HTMLElement>(
                'button:not(:disabled),input:not(:disabled),select:not(:disabled),[tabindex="0"]',
              ) ?? []),
            ];
            const i = nodes.indexOf(document.activeElement as HTMLElement);
            if (event.shiftKey && i <= 0) {
              event.preventDefault();
              nodes.at(-1)?.focus();
            } else if (!event.shiftKey && i === nodes.length - 1) {
              event.preventDefault();
              nodes[0]?.focus();
            }
          }
        }}
      >
        <header>
          <strong>{title}</strong>
          <button onClick={close} aria-label="إغلاق">
            <X size={19} />
          </button>
        </header>
        {children}
      </div>
    </div>
  );
};
export const MeterInput = ({
  label,
  value,
  onChange,
  min = 0,
  max = 10000,
  step = 0.1,
  disabled = false,
}: {
  label: string;
  value: number;
  onChange: (n: number) => void;
  min?: number;
  max?: number;
  step?: number;
  disabled?: boolean;
}) => (
  <label className="bs-field">
    <span>{label}</span>
    <input
      type="number"
      dir="ltr"
      value={value}
      min={min}
      max={max}
      step={step}
      disabled={disabled}
      onChange={(e) => {
        if (e.target.value !== "" && Number.isFinite(e.target.valueAsNumber))
          onChange(e.target.valueAsNumber);
      }}
    />
  </label>
);

export const RowsDialog = ({
  doc,
  apply,
  close,
}: {
  doc: EditorDocument;
  apply: (next: EditorDocument) => boolean;
  close: () => void;
}) => {
  const [options, setOptions] = useState<BoothRowOptions>({
    x: 1,
    y: 1,
    width: 3,
    height: 3,
    columns: 4,
    rows: 2,
    gap: 0,
    aisle: 2,
    prefix: "B-",
    start: 1,
    boothType: "Standard",
  });
  const update = (key: keyof BoothRowOptions, value: number | string) =>
    setOptions((o) => ({ ...o, [key]: value }));
  let next: EditorDocument | undefined,
    error = "";
  try {
    next = makeBoothRows(
      doc,
      options,
      (() => {
        let i = 0;
        return () => `row-preview-${i++}`;
      })(),
    );
  } catch (e) {
    error = e instanceof Error ? e.message : "تعذر إنشاء الصفوف.";
  }
  return (
    <StudioModal title="إنشاء صفوف الأجنحة" close={close} wide>
      <div className="bs-row-dialog">
        <section>
          <p className="bs-muted">
            اضبط المقاسات والمسافات؛ المعاينة تتحقق من حدود القاعة والعوائق.
          </p>
          <div className="bs-field-grid">
            <MeterInput
              label="عرض الجناح (م)"
              value={options.width}
              min={0.5}
              onChange={(n) => update("width", n)}
            />
            <MeterInput
              label="عمق الجناح (م)"
              value={options.height}
              min={0.5}
              onChange={(n) => update("height", n)}
            />
            <MeterInput
              label="الأجنحة في الصف"
              value={options.columns}
              min={1}
              max={500}
              step={1}
              onChange={(n) => update("columns", n)}
            />
            <MeterInput
              label="عدد الصفوف"
              value={options.rows}
              min={1}
              max={500}
              step={1}
              onChange={(n) => update("rows", n)}
            />
            <MeterInput
              label="فراغ بين الأجنحة (م)"
              value={options.gap}
              onChange={(n) => update("gap", n)}
            />
            <MeterInput
              label="ممر بين الصفوف (م)"
              value={options.aisle}
              onChange={(n) => update("aisle", n)}
            />
            <MeterInput
              label="الموضع X (م)"
              value={options.x}
              onChange={(n) => update("x", n)}
            />
            <MeterInput
              label="الموضع Y (م)"
              value={options.y}
              onChange={(n) => update("y", n)}
            />
            <label className="bs-field">
              <span>بادئة الترقيم</span>
              <input
                value={options.prefix}
                maxLength={14}
                dir="ltr"
                onChange={(e) => update("prefix", e.target.value)}
              />
            </label>
            <MeterInput
              label="بداية الترقيم"
              value={options.start}
              min={1}
              step={1}
              onChange={(n) => update("start", n)}
            />
          </div>
          <label className="bs-field">
            <span>فئة الأجنحة</span>
            <select
              value={options.boothType}
              onChange={(e) => update("boothType", e.target.value)}
            >
              {Object.entries(boothTypes).map(([v, label]) => (
                <option key={v} value={v}>
                  {label}
                </option>
              ))}
            </select>
          </label>
        </section>
        <section className="bs-row-preview">
          <SiteDrawing document={next ?? doc} />
          <strong>
            <Grid3X3 size={15} />
            {options.columns * options.rows} جناحاً
          </strong>
          <span dir="ltr">
            {(
              (options.width + options.gap) * options.columns -
              options.gap
            ).toFixed(2)}{" "}
            ×{" "}
            {(
              (options.height + options.aisle) * options.rows -
              options.aisle
            ).toFixed(2)}{" "}
            m
          </span>
        </section>
      </div>
      {error && (
        <p className="bs-inline-error" role="alert">
          {error}
        </p>
      )}
      <footer>
        <button onClick={close}>إلغاء</button>
        <button
          className="vs-save-button"
          disabled={!next}
          onClick={() => {
            try {
              const result = makeBoothRows(doc, options);
              if (apply(result)) close();
            } catch {
              /* The preview keeps invalid input disabled. */
            }
          }}
        >
          <Grid3X3 size={15} />
          إنشاء الصفوف
        </button>
      </footer>
    </StudioModal>
  );
};
export const NumberDialog = ({
  doc,
  ids,
  apply,
  close,
}: {
  doc: EditorDocument;
  ids: string[];
  apply: (next: EditorDocument) => boolean;
  close: () => void;
}) => {
  const [prefix, setPrefix] = useState("A-"),
    [start, setStart] = useState(1),
    [error, setError] = useState("");
  const count = boothShapes(doc).filter((s) => ids.includes(s.id)).length;
  return (
    <StudioModal title="ترقيم الأجنحة المحددة" close={close}>
      <p className="bs-muted">
        الترقيم من أعلى إلى أسفل، ومن اليسار إلى اليمين داخل كل صف.
      </p>
      <div className="bs-field-grid">
        <label className="bs-field">
          <span>بادئة الترقيم</span>
          <input
            dir="ltr"
            value={prefix}
            maxLength={14}
            onChange={(e) => setPrefix(e.target.value)}
          />
        </label>
        <MeterInput
          label="البداية"
          value={start}
          min={1}
          step={1}
          onChange={setStart}
        />
      </div>
      <p className="bs-muted">
        {count} جناحاً ·{" "}
        <b dir="ltr">
          {prefix}
          {String(start).padStart(3, "0")} → {prefix}
          {String(start + count - 1).padStart(3, "0")}
        </b>
      </p>
      {error && (
        <p role="alert" className="bs-inline-error">
          {error}
        </p>
      )}
      <footer>
        <button onClick={close}>إلغاء</button>
        <button
          className="vs-save-button"
          disabled={!count}
          onClick={() => {
            try {
              if (apply(renumberBooths(doc, ids, prefix, start))) close();
            } catch (e) {
              setError(e instanceof Error ? e.message : "تعذر الترقيم.");
            }
          }}
        >
          <Hash size={15} />
          تطبيق الترقيم
        </button>
      </footer>
    </StudioModal>
  );
};
export const SplitDialog = ({
  doc,
  id,
  apply,
  close,
}: {
  doc: EditorDocument;
  id: string;
  apply: (next: EditorDocument) => boolean;
  close: () => void;
}) => {
  const [count, setCount] = useState(2),
    [vertical, setVertical] = useState(false),
    [error, setError] = useState("");
  const s = doc.shapes.find((s) => s.id === id)!;
  return (
    <StudioModal title={`تقسيم الجناح ${s.label}`} close={close}>
      <p className="bs-muted">
        ينشئ أجنحة مستقلة ويستبدل الجناح الحالي عند الحفظ. يمكن التراجع قبل
        الحفظ.
      </p>
      <div className="bs-field-grid">
        <MeterInput
          label="عدد الأجزاء"
          value={count}
          min={2}
          max={20}
          step={1}
          onChange={setCount}
        />
        <label className="bs-field">
          <span>اتجاه التقسيم</span>
          <select
            value={vertical ? "vertical" : "horizontal"}
            onChange={(e) => setVertical(e.target.value === "vertical")}
          >
            <option value="horizontal">عبر العرض</option>
            <option value="vertical">عبر العمق</option>
          </select>
        </label>
      </div>
      <p className="bs-muted">
        مقاس الجزء:{" "}
        <b dir="ltr">
          {(vertical ? s.width : s.width / count).toFixed(2)} ×{" "}
          {(vertical ? s.height / count : s.height).toFixed(2)} م
        </b>
      </p>
      {error && (
        <p className="bs-inline-error" role="alert">
          {error}
        </p>
      )}
      <footer>
        <button onClick={close}>إلغاء</button>
        <button
          className="vs-save-button"
          onClick={() => {
            try {
              if (apply(splitBooth(doc, id, count, vertical))) close();
            } catch (e) {
              setError(e instanceof Error ? e.message : "تعذر التقسيم.");
            }
          }}
        >
          <ArrowLeftRight size={15} />
          تقسيم الجناح
        </button>
      </footer>
    </StudioModal>
  );
};
