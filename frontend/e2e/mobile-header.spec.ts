import { expect, test } from '@playwright/test'
import { normalizeBaseUrl, readSecretValue } from './helpers/environment'

const currentUser = {
  id: 'user-1',
  email: 'player@example.com',
  displayName: 'Marek Wozniak',
  role: 'Admin',
  isActive: true,
}

const team = (id: string, name: string, shortName: string) => ({
  id,
  name,
  shortName,
  countryCode: shortName,
  groupName: 'A',
})

const matches = [
  {
    id: 'match-1',
    matchNumber: 1,
    phase: 'GroupStage',
    groupName: 'A',
    kickoffTimeUtc: '2026-06-11T19:00:00Z',
    venue: 'Mexico City',
    status: 'Scheduled',
    isSettled: false,
    homeScore90: null,
    awayScore90: null,
    homeTeam: team('team-1', 'Polska', 'POL'),
    awayTeam: team('team-2', 'Niemcy', 'GER'),
    myPrediction: null,
    myPoints: null,
    canEditPrediction: true,
  },
]

const runsAgainstLocalPreview = () => {
  const rawBaseUrl = readSecretValue(process.env.E2E_BASE_URL, 'E2E_BASE_URL') ?? 'http://127.0.0.1:4173'

  try {
    const { hostname } = new URL(normalizeBaseUrl(rawBaseUrl))
    return hostname === 'localhost' || hostname === '127.0.0.1' || hostname === '::1'
  } catch {
    return false
  }
}

function withBaseUrlEnv(value: string | undefined, assertion: () => void) {
  const previous = process.env.E2E_BASE_URL

  if (value === undefined) {
    delete process.env.E2E_BASE_URL
  } else {
    process.env.E2E_BASE_URL = value
  }

  try {
    assertion()
  } finally {
    if (previous === undefined) {
      delete process.env.E2E_BASE_URL
    } else {
      process.env.E2E_BASE_URL = previous
    }
  }
}

test('runsAgainstLocalPreview obsługuje adres bez schematu', () => {
  withBaseUrlEnv('127.0.0.1:4173', () => {
    expect(runsAgainstLocalPreview()).toBeTruthy()
  })
})

test('runsAgainstLocalPreview obsługuje format KEY=VALUE', () => {
  withBaseUrlEnv('E2E_BASE_URL=127.0.0.1:4173', () => {
    expect(runsAgainstLocalPreview()).toBeTruthy()
  })
})

test.beforeEach(async ({ page, baseURL }) => {
  test.skip(!baseURL, 'Mobilna regresja layoutu wymaga ustawionego baseURL w konfiguracji Playwright.')
  test.info().annotations.push({
    type: 'base-url-host',
    description: runsAgainstLocalPreview() ? 'lokalny-preview' : 'zdalny-host',
  })

  await page.setViewportSize({ width: 390, height: 844 })
  await page.addInitScript((user) => {
    window.localStorage.setItem('typer.auth.token', 'test-token')
    window.localStorage.setItem('typer.auth.user', JSON.stringify(user))
  }, currentUser)

  await page.route('**/api/auth/me', async (route) => route.fulfill({ json: currentUser }))
  await page.route('**/api/matches/upcoming', async (route) => route.fulfill({ json: matches }))
  await page.route('**/api/matches', async (route) => route.fulfill({ json: matches }))
  await page.route('**/api/ranking/top', async (route) => route.fulfill({ json: [] }))
  await page.route('**/api/admin/players', async (route) => route.fulfill({ json: [] }))
  await page.route('**/api/teams', async (route) => route.fulfill({ json: [] }))
  await page.route('**/api/admin/matches', async (route) => route.fulfill({ json: [] }))
})

test('logged-in mobile shell keeps primary content high in the first viewport', async ({ page }) => {
  await page.goto('/')

  const header = page.locator('header')
  const dashboardTitle = page.getByRole('heading', { name: /marek wozniak/i })

  await expect(dashboardTitle).toBeVisible()

  const headerBox = await header.boundingBox()
  expect(headerBox, 'Nie udało się odczytać położenia nagłówka. Upewnij się, że header jest widoczny.').not.toBeNull()
  if (!headerBox) {
    throw new Error('Nie udało się odczytać położenia nagłówka.')
  }

  const dashboardTitleBox = await dashboardTitle.boundingBox()
  expect(
    dashboardTitleBox,
    'Nie udało się odczytać położenia nagłówka strony z nazwą użytkownika.',
  ).not.toBeNull()
  if (!dashboardTitleBox) {
    throw new Error('Nie udało się odczytać położenia nagłówka strony z nazwą użytkownika.')
  }

  expect(headerBox.height).toBeLessThanOrEqual(144)
  expect(dashboardTitleBox.y).toBeLessThanOrEqual(210)
})
