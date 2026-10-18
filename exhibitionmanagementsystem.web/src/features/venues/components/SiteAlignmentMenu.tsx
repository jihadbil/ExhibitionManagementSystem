import { alignmentOptions, alignmentUnitCount, type AlignmentMode } from '../utils/siteAlignment';
import { editable } from '../utils/siteEditor';
import type { useSiteEditor } from './useSiteEditor';

export const SiteAlignmentMenu = ({ editor: e }: { editor: ReturnType<typeof useSiteEditor> }) => <select className="vs-alignment-menu" aria-label="محاذاة وتوزيع الأشكال" title="حدد شكلين أو أكثر للمحاذاة، وثلاثة للتوزيع" value=""
  disabled={alignmentUnitCount(e.doc, e.selection) < 2 || e.interacting || e.selected.some(s => !editable(s, e.doc) || !e.doc.layers[s.layer].visible)}
  onChange={event => { if (event.target.value) e.align(event.target.value as AlignmentMode); }}>
  <option value="" disabled>محاذاة وتوزيع</option>
  {alignmentOptions.map(option => <option key={option.value} value={option.value} disabled={['horizontal', 'vertical'].includes(option.value) && alignmentUnitCount(e.doc, e.selection) < 3}>{option.label}</option>)}
</select>;
