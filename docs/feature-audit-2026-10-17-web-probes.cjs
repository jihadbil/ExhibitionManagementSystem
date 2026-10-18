// Runs original TS/TSX modules with injected state and deterministic I/O.
// No production service, browser storage, email or real account is touched.
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const root = path.resolve(__dirname, '..');
const web = path.join(root, 'exhibitionmanagementsystem.web');
const { createRequire } = require('node:module');
const webRequire = createRequire(path.join(web, 'package.json'));
const ts = webRequire('typescript');
const React = webRequire('react');
const { renderToString } = webRequire('react-dom/server');
const evidenceDir = process.env.EMS_FEATURE_EVIDENCE;
const results = [];
let memoValues = [];
function load(file, mockRequire, globals = {}) {
  const input = fs.readFileSync(file, 'utf8').replaceAll('import.meta.env', '({ VITE_API_URL: "" })');
  const code = ts.transpileModule(input, { compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX, target: ts.ScriptTarget.ES2022, esModuleInterop: true } }).outputText;
  const module = { exports: {} };
  vm.runInNewContext(code, { module, exports: module.exports, require: mockRequire, console, ...globals }, { filename: file });
  return module.exports;
}
function renderPage(file, exportedName, states) {
  let stateIndex = 0;
  const hooks = { ...React, useState(initial) { const index = stateIndex++; return [index in states ? states[index] : (typeof initial === 'function' ? initial() : initial), () => {}]; }, useEffect() {}, useMemo(fn) { const value=fn();memoValues.push(value);return value; }, useCallback(fn) { return fn; } };
  const module = load(path.join(web, file), id => {
    if (id === 'react') return hooks;
    if (id === 'lucide-react') return new Proxy({}, {get:()=>()=>null});
    if (id.endsWith('/AuthContext')) return { useAuth: () => ({user:{roles:['TenantAdmin']}}) };
    if (id.endsWith('/ToastContext')) return { useToast: () => ({showToast(){}}) };
    if (id.endsWith('/api')) return {api:{}};
    if (id.endsWith('/pagination')) return {getAllItems(){}};
    if (id.endsWith('/apiError')) return {getApiErrorMessage(){}};
    if (id.endsWith('/BadgeCard')) return {BadgeCard:()=>null};
    return webRequire(id);
  });
  return renderToString(React.createElement(module[exportedName]));
}
const contracts = JSON.parse(fs.readFileSync(path.join(evidenceDir, 'sponsorship-contracts.json')));
let failure = null;
try { renderPage('src/features/sponsorship/pages/SponsorshipPage.tsx', 'SponsorshipPage', {0:'contracts',6:contracts,8:false}); } catch (error) { failure = error.message; }
assert.match(failure, /toLocaleString/);
results.push({probe:'Actual contract JSON breaks original SponsorshipPage',error:failure,contractKeys:Object.keys(contracts[0])});

// Even correcting the money/name/date fields leaves an enum type mismatch.
const adapted = contracts.map(c => ({...c,contractAmount:c.totalAmount,sponsorName:c.sponsorCompanyName,signedAt:c.signedDate}));
failure = null;
try { renderPage('src/features/sponsorship/pages/SponsorshipPage.tsx', 'SponsorshipPage', {0:'contracts',6:adapted,8:false}); } catch (error) { failure = error.message; }
assert.match(failure, /toLowerCase/);
results.push({probe:'Contract status is numeric but original page calls toLowerCase',error:failure});

const log = {logID:1,userName:'Synthetic',tableName:'Services',recordID:'1',action:'Added',actionAt:'2026-10-17T12:00:00Z'};
memoValues=[];
renderPage('src/features/admin-audit/pages/AdminAuditPage.tsx','AdminAuditPage',{0:[log],3:'insert',4:false});
const stats=memoValues.find(value=>value&&value.total===1&&'inserts' in value);
assert.equal(stats.inserts,0);assert.equal(memoValues[0].length,0);
results.push({probe:'Added action is absent from insertion filter and insertion counter',actualAction:log.action,selectedFilter:'insert',visibleRows:memoValues[0].length,stats});

const tickets=JSON.parse(fs.readFileSync(path.join(evidenceDir,'tickets.json')));
const ticketHtml=renderPage('src/features/tickets/pages/TicketsPage.tsx','TicketsPage',{3:tickets});
assert.ok(ticketHtml.includes('<span class="badge badge-danger">Active</span>'));
results.push({probe:'A real active ticket is rendered with danger styling',serverStatus:tickets[0].status,html:'<span class="badge badge-danger">Active</span>'});

// Capture a refresh request before switching the stored account.
const values = new Map();
class ProbeEvent { constructor(type) { this.type=type; } }
const globals = { sessionStorage:{getItem:k=>values.get(k)??null,setItem:(k,v)=>values.set(k,v),removeItem:k=>values.delete(k)},localStorage:{removeItem(){},getItem(){return null},setItem(){}},window:{dispatchEvent(){},location:{origin:'http://audit.invalid'}},Event:ProbeEvent };
const storage = load(path.join(web,'src/features/auth/services/auth.storage.ts'), webRequire, globals);
let onFailure;
let finishRefresh;
const refreshed = new Promise(resolve=>{finishRefresh=resolve;});
const replayed=[];
const instance = async request => {replayed.push(request.headers.Authorization);return {status:200,data:{}};};
instance.interceptors={request:{use(){}},response:{use(_success,failure){onFailure=failure;}}};
const axios={create:()=>instance,post:()=>refreshed};
load(path.join(web,'src/services/api.ts'), id=>id==='axios'?axios:storage, globals);
storage.saveSession({userId:'account-A',tenantID:1,roles:['TenantAdmin'],accessToken:'A-old-access',refreshToken:'A-refresh'});
const pending=onFailure({response:{status:401},config:{method:'get',url:'/api/exhibitions',headers:{Authorization:'Bearer A-old-access'}}});
storage.saveSession({userId:'account-B',tenantID:2,roles:['TenantAdmin'],accessToken:'B-access',refreshToken:'B-refresh'});
finishRefresh({data:{accessToken:'A-new-access',refreshToken:'A-new-refresh',expiresAt:'2099-01-01T00:00:00Z'}});
pending.then(()=>{
  const session=storage.getStoredUser();
  assert.equal(session.userId,'account-B');assert.equal(session.accessToken,'A-new-access');
  results.push({probe:'In-flight refresh replaces tokens of a newly logged-in account',storedUserId:session.userId,storedTenant:session.tenantID,storedAccessToken:session.accessToken,replayed});
  fs.writeFileSync(path.join(evidenceDir,'web-probes.json'),JSON.stringify(results,null,2));
  console.log(JSON.stringify(results,null,2));
}).catch(error=>{console.error(error);process.exitCode=1;});
