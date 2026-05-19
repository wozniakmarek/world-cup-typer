import { defineConfig, devices } from '@playwright/test'

const rawBaseUrl = (process.env.E2E_BASE_URL ?? 'http://127.0.0.1:4173').trim()
const normalizedBaseUrl = /^https?:\/\//i.test(rawBaseUrl)
  ? rawBaseUrl
  : `https://${rawBaseUrl}`

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  retries: 1,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: normalizedBaseUrl.replace(/\/+$/, ''),
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
})
