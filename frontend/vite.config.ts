import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { VitePWA } from 'vite-plugin-pwa'

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      injectRegister: 'auto',
      includeAssets: ['apple-touch-icon.svg', 'favicon.svg'],
      manifest: {
        name: 'Typer Mistrzostw Świata',
        short_name: 'Typer MŚ',
        description: 'Prywatna aplikacja do typowania wyników meczów Mistrzostw Świata.',
        theme_color: '#071321',
        background_color: '#04101d',
        display: 'standalone',
        display_override: ['standalone', 'fullscreen'],
        id: '/',
        start_url: '/',
        scope: '/',
        icons: [
          {
            src: '/pwa-192.svg',
            sizes: '192x192',
            type: 'image/svg+xml',
            purpose: 'any',
          },
          {
            src: '/pwa-512.svg',
            sizes: '512x512',
            type: 'image/svg+xml',
            purpose: 'any',
          },
          {
            src: '/maskable-icon.svg',
            sizes: '512x512',
            type: 'image/svg+xml',
            purpose: 'maskable',
          },
        ],
      },
      workbox: {
        globPatterns: ['**/*.{js,css,html,svg,png,ico}'],
      },
    }),
  ],
  server: {
    port: 5173,
  },
})
