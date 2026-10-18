import test from 'node:test';
import assert from 'node:assert/strict';
import { AxiosHeaders } from 'axios';
import { loadSource, mountPage, findElements, settle } from './helpers/componentHarness.mjs';
import { normalizeAuditAction } from '../src/features/admin-audit/utils/auditActions.ts';

const deferred = () => { let resolve, reject; const promise = new Promise((ok, fail) => { resolve = ok; reject = fail; }); return { promise, resolve, reject }; };
const makeStorage = () => { const data = new Map(); return { getItem: key => data.get(key) ?? null, setItem: (key, value) => data.set(key, value), removeItem: key => data.delete(key) }; };
const account = id => ({ userId: id, tenantID: id === 'A' ? 1 : 2, roles: ['TenantAdmin'], accessToken: `${id}-access`, refreshToken: `${id}-refresh` });
const tokens = id => ({ accessToken: `${id}-new-access`, refreshToken: `${id}-new-refresh`, expiresAt: '2099-01-01T00:00:00Z' });

function sessionHarness() {
  const window = new EventTarget(); window.location = { origin: 'http://test.invalid' };
  const globals = { window, Event, sessionStorage: makeStorage(), localStorage: makeStorage() };
  const storage = loadSource('src/features/auth/services/auth.storage.ts', {}, globals);
  const flights = [], replayed = [];
  let requestInterceptor, failureInterceptor;
  const client = async config => { const request = requestInterceptor(config); replayed.push(request); return { data: {}, config: request }; };
  client.interceptors = { request: { use: callback => { requestInterceptor = callback; } }, response: { use: (_success, callback) => { failureInterceptor = callback; } } };
  const axios = { create: () => client, post: (...args) => { const flight = deferred(); flights.push({ ...flight, args }); return flight.promise; } };
  loadSource('src/services/api.ts', { axios, '/auth.storage': storage }, globals);
  const request = () => requestInterceptor({ method: 'post', url: '/api/financial/payments', headers: new AxiosHeaders() });
  return { storage, flights, replayed, window, request, unauthorized: config => failureInterceptor({ response: { status: 401 }, config }) };
}

test('refresh responses and queued retries cannot cross an account change', async () => {
  const h = sessionHarness(); h.storage.saveSession(account('A'));
  const first = h.unauthorized(h.request()), second = h.unauthorized(h.request());
  const checks = [assert.rejects(first), assert.rejects(second)];
  assert.equal(h.flights.length, 1);
  h.storage.saveSession(account('B')); h.flights[0].resolve({ data: tokens('A') });
  await Promise.all(checks);
  assert.equal(h.storage.getStoredUser().userId, 'B'); assert.equal(h.storage.getAccessToken(), 'B-access');
  assert.equal(h.replayed.length, 0);
});

test('failed old refresh cannot log out a newly signed-in account', async () => {
  const h = sessionHarness(); h.storage.saveSession(account('A'));
  let expired = 0; h.window.addEventListener('auth:expired', () => { expired += 1; });
  const check = assert.rejects(h.unauthorized(h.request()));
  h.storage.saveSession(account('B')); h.flights[0].reject(new Error('Old request failed')); await check;
  assert.equal(h.storage.getAccessToken(), 'B-access'); assert.equal(expired, 0);
});

test('logout and delayed 401 responses do not revive or replace a session', async () => {
  const h = sessionHarness(); h.storage.saveSession(account('A'));
  const old = h.request(), pending = assert.rejects(h.unauthorized(h.request()));
  h.storage.clearSession(); h.flights[0].resolve({ data: tokens('A') }); await pending;
  assert.equal(h.storage.getStoredUser(), null);
  h.storage.saveSession(account('B')); await assert.rejects(h.unauthorized(old));
  assert.equal(h.flights.length, 1); assert.equal(h.storage.getAccessToken(), 'B-access');
});

test('same-session requests share one refresh and retain their idempotency keys', async () => {
  const h = sessionHarness(); h.storage.saveSession(account('A'));
  const first = h.request(), second = h.request();
  const keys = [first.headers.get('Idempotency-Key'), second.headers.get('Idempotency-Key')];
  const pending = [h.unauthorized(first), h.unauthorized(second)];
  h.flights[0].resolve({ data: tokens('A') }); await Promise.all(pending);
  assert.equal(h.flights.length, 1); assert.equal(h.replayed.length, 2);
  assert.deepEqual(h.replayed.map(r => r.headers.get('Idempotency-Key')), keys);
  assert.ok(h.replayed.every(r => r.headers.get('Authorization') === 'Bearer A-new-access'));
});

test('a new account can refresh while an older account refresh is pending', async () => {
  const h = sessionHarness(); h.storage.saveSession(account('A'));
  const old = assert.rejects(h.unauthorized(h.request()));
  h.storage.saveSession(account('B')); const current = h.unauthorized(h.request());
  h.flights[0].resolve({ data: tokens('A') }); await old;
  h.flights[1].resolve({ data: tokens('B') }); await current;
  assert.equal(h.storage.getStoredUser().userId, 'B'); assert.equal(h.storage.getAccessToken(), 'B-new-access');
  assert.equal(h.replayed.length, 1);
});

test('contracts render the server field names and numeric enum without crashing', () => {
  const page = mountPage('src/features/sponsorship/pages/SponsorshipPage.tsx', 'SponsorshipPage', { states: {
    0: 'contracts', 6: [{ contractID: 1, sponsorCompanyName: 'Actual sponsor', totalAmount: 750, currencyCode: 'LYD', status: 3, statusName: 'Confirmed', signedDate: '2026-10-17T00:00:00Z' }], 8: false,
  } });
  const html = page.html(); assert.ok(html.includes('Actual sponsor')); assert.ok(html.includes('750')); assert.ok(html.includes('Confirmed')); assert.ok(!html.includes('Invalid Date'));
});

test('package form sends the numeric sponsorship level expected by MVC', async () => {
  const calls = [];
  const page = mountPage('src/features/sponsorship/pages/SponsorshipPage.tsx', 'SponsorshipPage', { states: {
    2: '1', 13: true, 14: { packageName: 'Gold package', level: 'Gold', price: '5000', currencyCode: 'LYD', maxSponsorsCount: '3', entitlementsDescription: '' },
  }, api: { post: async (...args) => calls.push(args), get: async () => ({ data: [] }) } });
  const form = findElements(page.render(), el => el.type === 'form')[0]; await form.props.onSubmit({ preventDefault() {} });
  assert.equal(calls[0][0], '/api/sponsorship/packages'); assert.equal(calls[0][1].level, 2);
});

test('audit actions normalize historical spellings for rendering, counters and filters', () => {
  assert.equal(normalizeAuditAction('Added'), 'Insert'); assert.equal(normalizeAuditAction('Modified'), 'Update'); assert.equal(normalizeAuditAction('Deleted'), 'Delete');
  const log = { logID: 1, userName: 'Test', tableName: 'Services', recordID: '1', action: 'Added', actionAt: '2026-10-17T12:00:00Z' };
  const page = mountPage('src/features/admin-audit/pages/AdminAuditPage.tsx', 'AdminAuditPage', { states: { 0: [log], 3: 'insert', 4: false } });
  page.html(); assert.equal(page.memos[0].length, 1); assert.equal(page.memos.find(v => v?.total === 1)?.inserts, 1);
});

test('audit page requests the complete paginated directory instead of the first 200 records', async () => {
  const rows = Array.from({ length: 201 }, (_, index) => ({ logID: index + 1 })); const urls = [];
  const page = mountPage('src/features/admin-audit/pages/AdminAuditPage.tsx', 'AdminAuditPage', { getAllItems: async url => { urls.push(url); return rows; } });
  page.render(); page.flushEffects(); await settle(); assert.deepEqual(urls, ['/api/admin/audit-logs']); assert.equal(page.values[0].length, 201); page.unmount();
});

test('active tickets render as valid rather than danger', () => {
  const page = mountPage('src/features/tickets/pages/TicketsPage.tsx', 'TicketsPage', { states: { 3: [{ ticketID: 1, visitorName: 'Visitor', ticketType: 'Regular', price: 0, currencyCode: 'LYD', qrCode: 'QR-1', status: 'Active', issuedAt: '2026-10-17T00:00:00Z', scansCount: 0 }] } });
  assert.ok(page.html().includes('<span class="badge badge-success">صالحة</span>'));
});

test('failed exhibition detail loading cannot open a form that overwrites existing fields', async () => {
  const page = mountPage('src/features/exhibitions/pages/ExhibitionsPage.tsx', 'ExhibitionsPage', { states: { 0: [{ exhibitionID: 1, name: 'Existing', type: 'Technical', startDate: '2026-10-17', endDate: '2026-10-18', status: 'Planning' }], 4: false }, api: { get: async () => { throw new Error('Unavailable'); } } });
  const edit = findElements(page.render(), el => el.type === 'button' && [el.props.children].flat().some(child => typeof child === 'string' && child.includes('تعديل')))[0];
  assert.ok(edit); await edit.props.onClick(); await settle(); assert.equal(page.values[7], false); assert.equal(page.values[8], null); assert.ok(page.toasts.some(t => t[1] === 'danger'));
});

function kioskWindow() {
  const frames = new Map(); let nextId = 0, printed = 0;
  return { window: {
    requestAnimationFrame: callback => { frames.set(++nextId, callback); return nextId; },
    cancelAnimationFrame: id => frames.delete(id),
    setTimeout: () => ++nextId, clearTimeout() {}, print: () => { printed += 1; },
  }, frame: () => { const current = [...frames.values()]; frames.clear(); current.forEach(fn => fn()); }, printed: () => printed };
}
const badge = { fullName: 'Actual attendee', companyName: 'Actual company', participantTypeTitle: 'Visitor', qrCode: 'QR-1', exhibitionName: 'Exhibition', widthMm: 86, heightMm: 120, orientation: 1, headerBgColor: '#000000', headerTextColor: '#ffffff', accentColor: '#333333', categoryBadgeText: 'Visitor', showLogo: false, showQRCode: true, showBarcode: false, showCompanyName: true, showJobTitle: true, showHallName: true, showDates: true };

test('kiosk loads the real badge and waits for its render before auto-printing', async () => {
  const h = kioskWindow(), response = deferred();
  const page = mountPage('src/features/kiosk/pages/CheckInKioskPage.tsx', 'CheckInKioskPage', { states: { 0: 'QR-1', 3: true }, globals: { window: h.window }, api: {
    post: async () => ({ data: { scanID: 1, ticketID: 1, qrCode: 'QR-1', scanTime: '2026-10-17T12:00:00Z', scanDirection: 'In' } }),
    get: () => response.promise,
  } });
  const form = findElements(page.render(), el => el.type === 'form')[0]; page.flushEffects();
  const submitted = form.props.onSubmit({ preventDefault() {} }); await settle();
  page.render(); page.flushEffects(); h.frame(); h.frame(); assert.equal(h.printed(), 0);
  response.resolve({ data: badge }); await submitted;
  const target = findElements(page.render(), el => el.props?.className === 'badge-print-target')[0];
  assert.equal(target.props.data.fullName, 'Actual attendee'); assert.equal(target.props.data.qrCode, 'QR-1');
  assert.ok(page.html().includes('Actual attendee')); page.flushEffects();
  assert.equal(h.printed(), 0); h.frame(); assert.equal(h.printed(), 0); h.frame(); assert.equal(h.printed(), 1); page.unmount();
});

test('kiosk badge failures preserve the successful entry and can retry without another scan', async () => {
  const h = kioskWindow(); let scans = 0, loads = 0;
  const page = mountPage('src/features/kiosk/pages/CheckInKioskPage.tsx', 'CheckInKioskPage', { states: { 0: 'QR-1' }, globals: { window: h.window }, api: {
    post: async () => { scans += 1; return { data: { scanID: 1, ticketID: 1, qrCode: 'QR-1', scanTime: '2026-10-17T12:00:00Z', scanDirection: 'In' } }; },
    get: async () => { loads += 1; if (loads === 1) throw new Error('Unavailable'); return { data: badge }; },
  } });
  await findElements(page.render(), el => el.type === 'form')[0].props.onSubmit({ preventDefault() {} });
  assert.equal(page.values[1], 1); assert.equal(page.values[7], true); assert.equal(scans, 1);
  const retry = findElements(page.render(), el => el.type === 'button' && el.props.children === 'إعادة تحميل الشارة للطباعة')[0];
  assert.ok(retry); retry.props.onClick(); await settle();
  assert.equal(page.values[8].fullName, 'Actual attendee'); assert.equal(scans, 1); assert.equal(loads, 2); assert.equal(h.printed(), 0);
});
