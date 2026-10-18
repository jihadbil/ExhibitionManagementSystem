import fs from 'node:fs';
import vm from 'node:vm';
import { fileURLToPath } from 'node:url';
import ts from '../exhibitionmanagementsystem.web/node_modules/typescript/lib/typescript.js';
import * as storage from '../exhibitionmanagementsystem.web/src/features/auth/services/auth.storage.ts';

// Execute the actual AuthProvider with minimal React hooks and synthetic session storage.
const makeStorage = () => {
  const values = new Map();
  return { getItem: key => values.get(key) ?? null, setItem: (key, value) => values.set(key, value), removeItem: key => values.delete(key) };
};
globalThis.sessionStorage = makeStorage();
globalThis.localStorage = makeStorage();
storage.saveSession({ fullName: 'Synthetic user', accessToken: 'synthetic-old-access', refreshToken: 'synthetic-old-refresh', roles: ['TenantAdmin'] });
let currentUser;
const react = {
  createContext: () => ({ Provider: 'Provider' }),
  useState: initial => {
    currentUser = typeof initial === 'function' ? initial() : initial;
    return [currentUser, change => { currentUser = typeof change === 'function' ? change(currentUser) : change; }];
  },
  useEffect: () => {},
  useContext: () => {},
  createElement: (type, props) => ({ type, props }),
};
const source = fs.readFileSync(fileURLToPath(new URL('../exhibitionmanagementsystem.web/src/app/providers/AuthContext.tsx', import.meta.url)), 'utf8');
const compiled = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.React, esModuleInterop: true } }).outputText;
const exports = {};
vm.runInNewContext(compiled, {
  exports,
  require: name => {
    if (name === 'react') return react;
    if (name.endsWith('auth.storage')) return storage;
    if (name.endsWith('auth.service')) return { authService: { getCurrentUser: storage.getStoredUser } };
    throw new Error('Unexpected probe dependency: ' + name);
  },
  window: { addEventListener() {}, removeEventListener() {} },
});
const provider = exports.AuthProvider({ children: null });
storage.updateSessionTokens({ accessToken: 'synthetic-new-access', refreshToken: 'synthetic-new-refresh', expiresAt: '2099-01-01T00:00:00Z' });
console.log('After refresh:', storage.getAccessToken(), storage.getRefreshToken());
provider.props.value.updateCurrentUser({ fullName: 'Renamed synthetic user' });
console.log('After profile update:', storage.getAccessToken(), storage.getRefreshToken());
if (storage.getRefreshToken() !== 'synthetic-new-refresh') {
  console.log('CONFIRMED: updating the profile restores a revoked refresh token from React state.');
  process.exitCode = 1;
}
