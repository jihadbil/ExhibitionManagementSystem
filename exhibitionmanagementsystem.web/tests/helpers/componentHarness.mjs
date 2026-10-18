import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';
import ts from 'typescript';
import React from 'react';
import { renderToString } from 'react-dom/server';

const require = createRequire(import.meta.url);
const root = fileURLToPath(new URL('../../', import.meta.url));

export const loadSource = (relativePath, dependencies = {}, globals = {}) => {
  const cache = new Map();
  const load = file => {
    if (cache.has(file)) return cache.get(file).exports;
    const source = fs.readFileSync(file, 'utf8').replaceAll('import.meta.env', '({ VITE_API_URL: "" })');
    const compiled = ts.transpileModule(source, { compilerOptions: {
      module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX, target: ts.ScriptTarget.ES2022, esModuleInterop: true,
    } }).outputText;
    const module = { exports: {} };
    cache.set(file, module);
    const resolve = id => {
      const match = Object.keys(dependencies).find(key => id === key || (key.startsWith('/') && id.endsWith(key)));
      if (match) return dependencies[match];
      if (id === 'lucide-react') return new Proxy({}, { get: () => () => null });
      if (id.startsWith('.')) {
        const base = path.resolve(path.dirname(file), id);
        const target = [base, `${base}.ts`, `${base}.tsx`].find(candidate => fs.existsSync(candidate) && fs.statSync(candidate).isFile());
        if (target) return load(target);
      }
      return require(id);
    };
    vm.runInNewContext(compiled, { module, exports: module.exports, require: resolve, console, ...globals }, { filename: file });
    return module.exports;
  };
  return load(path.resolve(root, relativePath));
};

export const findElements = (tree, predicate) => {
  const result = [];
  const visit = value => {
    if (Array.isArray(value)) { value.forEach(visit); return; }
    if (!value || typeof value !== 'object') return;
    if (predicate(value)) result.push(value);
    visit(value.props?.children);
  };
  visit(tree);
  return result;
};

export const mountPage = (file, name, { states = {}, api = {}, getAllItems = async () => [], globals = {} } = {}) => {
  const values = { ...states }, refs = [], effects = [], pendingEffects = [], memos = [], toasts = [];
  let stateIndex = 0, refIndex = 0, effectIndex = 0;
  const react = {
    ...React,
    useState(initial) {
      const index = stateIndex++;
      if (!(index in values)) values[index] = typeof initial === 'function' ? initial() : initial;
      return [values[index], update => { values[index] = typeof update === 'function' ? update(values[index]) : update; }];
    },
    useRef(initial) { const index = refIndex++; return refs[index] ??= { current: initial }; },
    useMemo(fn) { const value = fn(); memos.push(value); return value; },
    useCallback(fn) { return fn; },
    useEffect(effect, deps) {
      const index = effectIndex++, previous = effects[index];
      if (!previous || !deps || deps.some((value, i) => value !== previous.deps?.[i])) {
        pendingEffects.push(() => { previous?.cleanup?.(); effects[index] = { deps, cleanup: effect() }; });
      }
    },
  };
  const exports = loadSource(file, {
    react,
    '/AuthContext': { useAuth: () => ({ user: { roles: ['TenantAdmin'], baseCurrency: 'LYD' } }) },
    '/ToastContext': { useToast: () => ({ showToast: (...args) => toasts.push(args) }) },
    '/api': { api }, '/pagination': { getAllItems },
    'react-router-dom': { useSearchParams: () => [new URLSearchParams(), () => {}], NavLink: ({ children }) => React.createElement('a', null, children) },
  }, globals);
  const render = () => { pendingEffects.length = 0; stateIndex = 0; refIndex = 0; effectIndex = 0; memos.length = 0; return exports[name](); };
  return { values, memos, toasts, render, html: () => renderToString(render()),
    flushEffects: () => { while (pendingEffects.length) pendingEffects.shift()(); },
    unmount: () => effects.forEach(effect => effect.cleanup?.()),
  };
};

export const settle = () => new Promise(resolve => setImmediate(resolve));
