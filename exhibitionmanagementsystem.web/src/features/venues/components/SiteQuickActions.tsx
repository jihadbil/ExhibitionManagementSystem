import { useEffect, useRef } from 'react';
import { BringToFront, Copy, Group, Lock, MousePointer2, Pentagon, Plus, SendToBack, Trash2, Type, Ungroup, Unlock } from 'lucide-react';
import { editable, isPath, shapeBounds, unionBounds, type EditorShape, type Point } from '../utils/siteEditor';
import { SiteAlignmentMenu } from './SiteAlignmentMenu';
import type { useSiteEditor } from './useSiteEditor';

type Editor = ReturnType<typeof useSiteEditor>;
export const InlineLabel = ({ shape, editor: e }: { shape: EditorShape; editor: Editor }) => {
  const inputRef = useRef<HTMLInputElement | null>(null), cancelled = useRef(false);
  const { camera, size, commitLabel, setEditingTextId } = e;
  const b = shapeBounds(shape), width = Math.min(Math.max(160, b.width * camera.zoom), Math.max(160, size.width - 32));
  const left = Math.max(16, Math.min(size.width - width - 16, (b.x + b.width / 2 - camera.x) * camera.zoom - width / 2));
  const top = Math.max(28, Math.min(size.height - 50, (b.y + b.height / 2 - camera.y) * camera.zoom - 19));
  useEffect(() => { inputRef.current?.focus(); inputRef.current?.select(); }, []);
  return <div className="vs-inline-label" style={{ left, top, width }} dir="rtl"><input ref={inputRef} aria-label="تعديل النص مباشرة" defaultValue={shape.label} maxLength={200}
    onBlur={event => { if (!cancelled.current) commitLabel(shape.id, event.target.value); }}
    onKeyDown={event => {
      event.stopPropagation();
      if (event.key === 'Enter') { event.preventDefault(); commitLabel(shape.id, event.currentTarget.value); }
      if (event.key === 'Escape') { event.preventDefault(); cancelled.current = true; setEditingTextId(null); }
    }} /><small>Enter للحفظ · Esc للإلغاء</small></div>;
};

export const SelectionActions = ({ editor: e }: { editor: Editor }) => {
  const { doc, single, selected, size, camera, interacting, polyPoints, tool, editingTextId, changeTool, editLabel, copy, paste, patch, remove, group } = e;
  if (!selected.length || interacting || polyPoints.length || editingTextId || !['select', 'nodes'].includes(tool)) return null;
  const b = unionBounds(selected), width = single ? 280 : 300;
  const left = Math.max(28, Math.min(Math.max(28, size.width - width - 12), (b.x + b.width / 2 - camera.x) * camera.zoom - width / 2));
  const top = Math.max(31, Math.min(size.height - 80, (b.y - camera.y) * camera.zoom - 68));
  const allowed = selected.every(s => editable(s, doc));
  return <div className="vs-selection-actions" role="toolbar" aria-label="أوامر العنصر المحدد" style={{ left, top }} dir="rtl">
    {single ? <>
      {isPath(single) && <button disabled={!allowed} onClick={() => changeTool(tool === 'nodes' ? 'select' : 'nodes')} title="تعديل الزوايا بالنقر المزدوج"><Pentagon size={15} />{tool === 'nodes' ? 'إنهاء الزوايا' : 'زوايا'}</button>}
      {(single.kind === 'hall' || ['text', 'rectangle', 'ellipse'].includes(single.kind)) && <button disabled={!allowed} onClick={() => editLabel()} title="تعديل التسمية F2"><Type size={15} />{single.kind === 'hall' ? 'الاسم' : 'النص'}</button>}
      <button disabled={!allowed || single.kind === 'hall'} onClick={() => { copy(); paste(); }} title="نسخ العنصر"><Copy size={15} /></button>
      <button disabled={doc.layers[single.layer].locked} onClick={() => patch({ locked: !single.locked })} title={single.locked ? 'فتح العنصر' : 'قفل العنصر'}>{single.locked ? <Unlock size={15} /> : <Lock size={15} />}</button>
    </> : <>
      <button disabled={!allowed} onClick={() => group(selected.some(s => s.groupId !== undefined))} title="تجميع / فك التجميع">{selected.some(s => s.groupId) ? <Ungroup size={15} /> : <Group size={15} />}{selected.some(s => s.groupId) ? 'فك التجميع' : 'تجميع'}</button>
      <SiteAlignmentMenu editor={e} />
    </>}
    <span /><button disabled={!selected.some(s => editable(s, doc))} onClick={remove} title="حذف المحدد" className="danger"><Trash2 size={15} /></button>
  </div>;
};

export const SiteContextMenu = ({ editor: e, position, close }: { editor: Editor; position: Point; close: () => void }) => {
  const menuRef = useRef<HTMLDivElement | null>(null);
  const { doc, selected, single, size, changeTool, editLabel, copy, paste, remove, patch, order, setSelection } = e;
  const allowed = selected.length > 0 && selected.every(s => editable(s, doc));
  const run = (fn: () => void) => { close(); fn(); };
  useEffect(() => { menuRef.current?.querySelector<HTMLButtonElement>('button:not(:disabled)')?.focus(); }, []);
  return <div className="vs-site-context-menu" role="menu" aria-label="أوامر الرسم السريعة" ref={menuRef} dir="rtl"
    style={{ left: Math.max(8, Math.min(size.width - 214, position.x)), top: Math.max(28, Math.min(size.height - 340, position.y)) }}
    onKeyDown={event => {
      if (['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) {
        event.preventDefault(); event.stopPropagation(); const buttons = [...(menuRef.current?.querySelectorAll<HTMLButtonElement>('button:not(:disabled)') ?? [])];
        const current = buttons.indexOf(document.activeElement as HTMLButtonElement), next = event.key === 'Home' ? 0 : event.key === 'End' ? buttons.length - 1 : (current + (event.key === 'ArrowDown' ? 1 : -1) + buttons.length) % buttons.length;
        buttons[next]?.focus();
      }
    }}>
    {selected.length ? <>
      {single && isPath(single) && <button role="menuitem" disabled={!allowed} onClick={() => run(() => changeTool('nodes'))}><Pentagon size={15} /> تعديل الزوايا <kbd>نقرتان</kbd></button>}
      {single && (single.kind === 'hall' || ['text', 'rectangle', 'ellipse'].includes(single.kind)) && <button role="menuitem" disabled={!allowed} onClick={() => run(() => editLabel())}><Type size={15} /> تعديل التسمية <kbd>F2</kbd></button>}
      <button role="menuitem" disabled={selected.every(s => s.kind === 'hall')} onClick={() => run(copy)}><Copy size={15} /> نسخ <kbd>Ctrl C</kbd></button>
      <button role="menuitem" disabled={!allowed || selected.some(s => s.kind === 'hall')} onClick={() => run(() => { copy(); paste(); })}><Copy size={15} /> إنشاء نسخة <kbd>Ctrl D</kbd></button>
      <div className="vs-menu-divider" />
      <button role="menuitem" disabled={!allowed} onClick={() => run(() => order(true))}><BringToFront size={15} /> إحضار للأمام</button><button role="menuitem" disabled={!allowed} onClick={() => run(() => order(false))}><SendToBack size={15} /> إرسال للخلف</button>
      {single && <button role="menuitem" disabled={doc.layers[single.layer].locked} onClick={() => run(() => patch({ locked: !single.locked }))}>{single.locked ? <Unlock size={15} /> : <Lock size={15} />} {single.locked ? 'فتح القفل' : 'قفل العنصر'}</button>}
      <button role="menuitem" className="danger" disabled={!selected.some(s => editable(s, doc))} onClick={() => run(remove)}><Trash2 size={15} /> حذف المحدد <kbd>Delete</kbd></button>
    </> : <>
      <button role="menuitem" onClick={() => run(() => changeTool('hall'))}><Plus size={15} /> رسم قاعة</button><button role="menuitem" onClick={() => run(() => changeTool('wall'))}><Plus size={15} /> رسم جدار</button><button role="menuitem" onClick={() => run(paste)}><Copy size={15} /> لصق <kbd>Ctrl V</kbd></button><div className="vs-menu-divider" /><button role="menuitem" onClick={() => run(() => setSelection(doc.shapes.filter(s => doc.layers[s.layer].visible).map(s => s.id)))}><MousePointer2 size={15} /> تحديد الكل <kbd>Ctrl A</kbd></button>
    </>}
  </div>;
};
