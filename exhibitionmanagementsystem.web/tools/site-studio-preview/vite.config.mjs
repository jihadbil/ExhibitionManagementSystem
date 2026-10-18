import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath } from 'node:url';
const path = name => fileURLToPath(new URL(name, import.meta.url));
export default defineConfig({
  root: path('./'), plugins: [react()],
  resolve: { alias: [
    { find: '../../../services/api', replacement: path('./preview-api.ts') },
    { find: '../../../app/providers/AuthContext', replacement: path('./preview-auth.ts') },
  ] },
  server: { host: '127.0.0.1', port: 53513, strictPort: true, fs: { allow: [path('../../')] } },
});
