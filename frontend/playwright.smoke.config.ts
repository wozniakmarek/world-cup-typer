import { defineConfig, devices } from '@playwright/test'

function readSecretValue(value: string | undefined, key: string): string | undefined {
  const trimmed = value?.trim()

  if (!trimmed) {
    return undefined
  }

  const separatorIndex = trimmed.indexOf('=')
  if (separatorIndex <= 0) {
    return trimmed
  }

  const candidateKey = trimmed.slice(0, separatorIndex).trim()
  const candidateValue = trimmed.slice(separatorIndex + 1).trim()

  if (candidateKey.toLowerCase() === key.toLowerCase()) {
    return candidateValue
  }

  return /^[A-Za-z_][A-Za-z0-9_]*$/.test(candidateKey) ? undefined : trimmed
}

const rawBaseUrl = readSecretValue(process.env.E2E_BASE_URL, 'E2E_BASE_URL') ?? 'http://127.0.0.1:4173'
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
