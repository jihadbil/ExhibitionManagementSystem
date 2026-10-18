import { useState } from 'react';
import { Maximize2, Plus, X } from 'lucide-react';
import { round, type EditorDocument } from '../utils/siteEditor';

export const PageSizeDialog = ({ document: doc, apply, close }: {
  document: EditorDocument; apply: (bounds: EditorDocument['bounds']) => boolean; close: () => void;
}) => {
  const [width, setWidth] = useState(String(doc.bounds.width)), [height, setHeight] = useState(String(doc.bounds.height));
  const increase = () => {
    setWidth(String(round(Math.min(10000, (Number(width) || doc.bounds.width) * 1.25))));
    setHeight(String(round(Math.min(10000, (Number(height) || doc.bounds.height) * 1.25))));
  };
  return <div className="vs-modal-backdrop" onClick={event => { if (event.target === event.currentTarget) close(); }}>
    <form className="vs-modal vs-page-size-dialog" role="dialog" aria-modal="true" aria-labelledby="site-page-size-title"
      onKeyDown={event => { if (event.key === 'Escape') { event.preventDefault(); event.stopPropagation(); close(); } }}
      onSubmit={event => { event.preventDefault(); if (apply({ width: round(Number(width)), height: round(Number(height)) })) close(); }}>
      <div className="vs-modal-header"><span><Maximize2 size={23} /></span><div><h2 id="site-page-size-title">مساحة الرسم</h2><p>اضبط أبعاد الصفحة بالمتر</p></div><button type="button" title="إغلاق أبعاد مساحة الرسم" onClick={close}><X size={18} /></button></div>
      <div className="vs-field-grid"><label className="vs-field"><span>عرض مساحة الرسم (متر)</span><input autoFocus type="number" required min={5} max={10000} step="any" value={width} onChange={event => setWidth(event.target.value)} /></label><label className="vs-field"><span>طول مساحة الرسم (متر)</span><input type="number" required min={5} max={10000} step="any" value={height} onChange={event => setHeight(event.target.value)} /></label></div>
      <div className="vs-page-area"><small>المساحة الإجمالية</small><strong>{round(Math.max(0, Number(width) * Number(height) || 0)).toLocaleString('ar-LY')} م²</strong><button type="button" onClick={increase}><Plus size={14} /> زيادة الأبعاد 25%</button></div>
      <div className="vs-note">توسيع مساحة الرسم يحافظ على مقاسات القاعات ومواضعها. يمكن أيضاً سحب مقابض حافتي الصفحة أو زاويتها السفلية بأداة التحديد.</div>
      <div className="vs-modal-actions"><button className="vs-save-button" type="submit">تطبيق الأبعاد</button><button type="button" onClick={close}>إلغاء</button></div>
    </form>
  </div>;
};
