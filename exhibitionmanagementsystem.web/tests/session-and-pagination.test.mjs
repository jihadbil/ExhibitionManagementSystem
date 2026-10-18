import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';
import ts from 'typescript';
import { collectPages } from '../src/services/paginationUtils.ts';
import * as storage from '../src/features/auth/services/auth.storage.ts';

test('loads entries beyond the first page even when the server caps page size', async () => {
  const source = Array.from({ length: 451 }, (_, id) => ({ id }));
  const calls = [];
  const actual = await collectPages(async (page, size) => {
    calls.push([page, size]);
    return { items: source.slice((page - 1) * 150, page * 150), totalCount: source.length };
  });
  assert.deepEqual(actual, source);
  assert.deepEqual(calls.map(([page]) => page), [1, 2, 3, 4]);
});

test('a failed subsequent page rejects instead of displaying an incomplete directory', async () => {
  await assert.rejects(collectPages(async page => {
    if (page === 2) throw new Error('Directory unavailable');
    return { items: [1], totalCount: 3 };
  }), /Directory unavailable/);
});

test('empty and full last pages stop without requesting nonexistent pages', async () => {
  let requests = 0;
  assert.deepEqual(await collectPages(async () => { requests += 1; return { items: [], totalCount: 0 }; }), []);
  assert.equal(requests, 1);
  requests = 0;
  assert.deepEqual(await collectPages(async page => {
    requests += 1;
    return { items: [page * 2 - 1, page * 2], totalCount: 4 };
  }, 2), [1, 2, 3, 4]);
  assert.equal(requests, 2);
});

test('profile updates preserve rotated tokens and cannot revive a cleared session', () => {
  const makeStorage = () => {
    const entries = new Map();
    return { getItem: key => entries.get(key) ?? null, setItem: (key, value) => entries.set(key, value), removeItem: key => entries.delete(key) };
  };
  globalThis.sessionStorage = makeStorage(); globalThis.localStorage = makeStorage();
  globalThis.window = new EventTarget();
  storage.saveSession({ userId: 'synthetic-user', fullName: 'Synthetic', roles: ['TenantAdmin'], accessToken: 'old-access', refreshToken: 'old-refresh' });
  let state;
  const cleanups = [];
  const react = {
    createContext: () => ({ Provider: 'Provider' }),
    useState: initial => { state = initial(); return [state, value => { state = typeof value === 'function' ? value(state) : value; }]; },
    useEffect: effect => cleanups.push(effect()), useContext: () => {},
    createElement: (type, props) => ({ type, props }),
  };
  const source = fs.readFileSync(new URL('../src/app/providers/AuthContext.tsx', import.meta.url), 'utf8');
  const compiled = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.React, esModuleInterop: true } }).outputText;
  const exports = {};
  vm.runInNewContext(compiled, { exports, window, require: name => {
    if (name === 'react') return react;
    if (name.endsWith('auth.storage')) return storage;
    if (name.endsWith('auth.service')) return { authService: { getCurrentUser: storage.getStoredUser } };
    throw new Error('Unexpected dependency: ' + name);
  } });
  const provider = exports.AuthProvider({ children: null });
  storage.updateSessionTokens({ accessToken: 'new-access', refreshToken: 'new-refresh', expiresAt: '2099-01-01T00:00:00Z' });
  assert.equal(state.refreshToken, 'new-refresh');
  provider.props.value.updateCurrentUser({ fullName: 'Renamed' });
  assert.equal(storage.getAccessToken(), 'new-access');
  assert.equal(storage.getRefreshToken(), 'new-refresh');
  assert.equal(state.fullName, 'Renamed');
  storage.clearSession();
  provider.props.value.updateCurrentUser({ fullName: 'Must not restore session' });
  assert.equal(storage.getStoredUser(), null);
  assert.equal(state, null);
  cleanups.forEach(cleanup => cleanup?.());
});
