import path from 'node:path'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    // Deterministic port — the API's SPA proxy and docs point at 5173.
    strictPort: true,
    port: 5173,
    proxy: {
      // Proxy API calls to the SmartLibrary.Api dev server — no CORS in dev.
      '/api': 'http://localhost:5205',
      '/health': 'http://localhost:5205',
    },
  },
})
