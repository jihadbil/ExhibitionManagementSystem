# Site Studio

Replacement for the venue site-plan designer. Its scope is the site and hall architecture; it does not read, render, create, or edit booths.

## Run the interactive preview

From `exhibitionmanagementsystem.web` run `npm run preview:site-studio`, then open `http://127.0.0.1:53513/venues/1/designer`.

The preview runs the production editor components against a fixture API and separate fixture identity. It does not call the real API. Changes persist only in the browser's preview storage. The sample includes a concave L-shaped hall, an angled hall, partitions, swing doors, columns, dimensions and exits.

The production route remains `/venues/:venueId/designer`. Restart the backend after building to load the new v4 server validation. No database migration or new frontend dependency is required.

## Drawing

- The persistent **مساحة الرسم** button above the canvas opens width/height controls and a 25% expansion preset, even while a hall is selected. In selection mode, drag the right-edge, bottom-edge, or bottom-right page handle to resize the page. Expansion preserves every object's geometry and position; shrinking is rejected when it would crop elements, including rotated/hidden ones. Page resizing supports undo and the existing save/reload flow.
- `B`: choose rectangle drag or free-point outline in the hall library. For an outline, click each corner and finish with Enter, double-click, first-corner click, or right-click. Choose an existing unplaced hall or create a named hall record.
- `W`: drag a straight wall by default. Switch to multiple points in the library for a chain; Enter completes the partition.
- `A`: select a polygon/path and drag vertices. Click a midpoint `+` to insert a corner; select a corner and press Delete to remove it.
- `V`: select, drag, marquee-select and use Shift for multiple selection. Resize handles preserve the opposite world-space corner after rotation. Rotation supports any angle; Shift uses 15-degree steps.
- Double-click a hall or path to edit its corners; double-click a text/rectangle/ellipse to edit its label directly. F2 edits the selected label or opens the hall-name form. Enter commits inline text; Escape cancels it.
- A floating selection toolbar and right-click menu expose edit, lock, duplicate, delete and ordering commands next to the drawing. The menu supports arrows and Escape.
- `Space` + drag pans the workspace. Wheel zooms about the pointer without an extra modifier. Shift + wheel pans horizontally; Alt + wheel pans vertically.
- Drag a selection box left-to-right for full containment or right-to-left for crossing selection. Shift adds to the current selection. Rectangles, ellipses and lines automatically return to selection after drawing; walls and freehand remain active for repeated strokes.
- `L`, `R`, `E`, `P`, `T`, `D`: line, rectangle, ellipse, freehand, text and dimension. Doors, columns and exits are available in the library.
- Ctrl+Z/Y: undo/redo. Ctrl+C/V/D: copy/paste/duplicate drawing objects. Halls have distinct database identities and cannot be duplicated onto the same hall record.
- Ctrl+G / Ctrl+Shift+G: group/ungroup. Arrow keys nudge by grid size; Shift uses ten steps.

## Data and safety

The v4 JSON stores local polygon vertices, transforms, style, layers and hall ownership. Exported `halls` contain the world-space bounding boxes required by the existing API. Hall outlines are independent of source rectangle dimensions; reload does not replace a drawn outline with a rectangle. Existing v1–v3 plans are migrated in memory, including their halls, elements and doors.

Both client and server validate finite geometry, rotated bounds, simple polygon outlines, unique hall ownership, style values and document limits. Hall overlaps are shown as warnings. Layer and element locks preserve selection while blocking edits. Removing a hall drawing removes its placement only, leaving its hall record available in the library.

Autosave uses the existing optimistic version API. In-flight saves compare their snapshot against the current drawing. Browser recovery is scoped to tenant, user and venue. Conflicts suspend saving and allow draft export. A stale recovered draft cannot overwrite a newer server version. Leaving with unfinished/unsaved work is protected.

SVG export preserves visible layers as vector geometry. JSON supports editable backup/import. Print supports browser PDF export. On narrow screens, the header toggles the drawing tools and properties panels.

## Validation

- `npm test`: geometry, rotated resize, polygon editing, history, ownership, overlaps, precision and legacy migration.
- `npm run build` and targeted ESLint on the editor modules.
- `dotnet test ExhibitionManagementSystem.Api.Tests/ExhibitionManagementSystem.Api.Tests.csproj -c Release --filter FullyQualifiedName~VenueSitePlanServiceTests`: version conflicts and v4 server geometry/ownership tests.
- Interactive browser checks: six-corner hall drawing and linking, save/reload retaining vertices and dimensions, node insertion/deletion, selecting locked objects, SVG download, desktop and narrow-screen layout.
- Usability browser checks: direct double-click vertex editing, inline text commit/cancel/undo, menu duplication/undo, rectangle hall drag, wall drag and wheel-only zoom.
- Page resizing browser checks: expansion from 100×75 to 150×110 with a hall selected, unchanged object paths/transforms, persistence after reload, corner drag enlargement and undo, and rejected shrink restoring the numeric input.

## Smart alignment

Smart alignment is enabled by default and has a canvas-header toggle independent of the grid. Drawing, moving, resizing and vertex editing capture visible vertices, bounds and centers within eight screen pixels. Pink guides mark matching coordinates. Green guides identify horizontal/vertical directions or parallel edges (including rotated and concave outlines), highlighting the reference segment and showing an Arabic confirmation. Alt bypasses smart capture and grid capture; Shift still explicitly constrains directions to 45-degree steps.

Select multiple shapes and use **محاذاة وتوزيع** in the main or floating toolbar: six edge/center alignment commands and horizontal/vertical equal-gap distribution for three or more objects. Groups move as units. Distribution preserves the outer objects and rejects insufficient room. Locks and hidden layers remain respected. Commands support undo and the existing save format without database changes.

`siteAlignment.test.mjs` covers off-grid capture, zoom tolerance, hidden/locked references, Alt/Shift, rotated parallel segments, bounded group movement, all six alignment commands, unequal-size distribution and grouped-object offsets. `smart-alignment.jpg` shows the live parallel indicator in the running preview.

`desktop.jpg` shows the actual running preview, with vertex editing selected.
