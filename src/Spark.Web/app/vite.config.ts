import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { resolve } from 'path'

// https://vite.dev/config/
export default defineConfig({
  base: '/',
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src'),
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/fhir': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/maintenanceHub': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        ws: true,
      },
    },
  },
  build: {
    outDir: '../wwwroot',
    emptyOutDir: false,
    rollupOptions: {
      output: {
        manualChunks: {
          vendor: ['react', 'react-dom', 'react-router-dom'],
          aria: ['react-aria-components'],
        },
      },
    },
  },
})
