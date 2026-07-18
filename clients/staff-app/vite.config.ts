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
    proxy: {
      // Proxy API calls to the SmartLibrary.Api dev server — no CORS in dev.
      '/api': 'http://localhost:5205',
      '/health': 'http://localhost:5205',
    },
  },
})
