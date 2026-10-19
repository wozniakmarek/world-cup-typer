import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: './e2e', testMatch: ['archive-crypto.spec.ts', 'archive-readonly.spec.ts'],
  fullyParallel: true, retries: 0, reporter: 'list',
  use: { baseURL: 'http://127.0.0.1:4189', screenshot: 'only-on-failure', trace: 'retain-on-failure' },
  projects: [{ name: 'desktop', use: { ...devices['Desktop Chrome'] } }, { name: 'mobile', use: { ...devices['Pixel 5'] } }],
  webServer: { command: 'npm run dev -- --host 127.0.0.1 --port 4189', url: 'http://127.0.0.1:4189', env: { VITE_ARCHIVE_MODE: 'true' }, reuseExistingServer: false },
})
