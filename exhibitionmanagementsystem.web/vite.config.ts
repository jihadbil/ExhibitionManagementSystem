import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [plugin()],
    server: {
        port: 53512,
        proxy: {
            '/api': {
                target: process.env.VITE_DEV_API_URL || 'https://localhost:7213',
                changeOrigin: true,
                secure: false,
            },
        },
    }
})
