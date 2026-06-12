import { expect, test } from '@playwright/test'
import { runsAgainstLocalPreview } from './helpers/environment'

const currentUser = {
  id: 'user-1',
  email: 'player@example.com',
  displayName: 'Marek Wozniak',
  role: 'Player',
  isActive: true,
}

const team = (id: string, name: string, shortName: string, flagEmoji?: string, groupName?: string) => ({
  id,
  name,
  shortName,
  countryCode: shortName,
  flagEmoji,
  groupName,
})

const matches = [
  {
    id: 'match-group',
    matchNumber: 537327,
    phase: 'GroupStage',
    groupName: 'A',
    kickoffTimeUtc: '2026-06-11T19:00:00Z',
    venue: null,
    status: 'Scheduled',
    isSettled: false,
    homeScore90: null,
    awayScore90: null,
    homeTeam: team('team-mex', 'Mexico', 'MEX', 'MX', 'A'),
    awayTeam: team('team-rsa', 'South Africa', 'RSA', 'ZA', 'A'),
    myPrediction: null,
    myPoints: null,
    canEditPrediction: true,
  },
  {
    id: 'match-unlisted-flag',
    matchNumber: 537328,
    phase: 'GroupStage',
    groupName: 'B',
    kickoffTimeUtc: '2026-06-12T19:00:00Z',
    venue: 'Arrowhead Stadium',
    status: 'Scheduled',
    isSettled: false,
    homeScore90: null,
    awayScore90: null,
    homeTeam: team('team-nga', 'Nigeria', 'NGA'),
    awayTeam: team('team-nor', 'Norway', 'NOR'),
    myPrediction: null,
    myPoints: null,
    canEditPrediction: true,
  },
  {
    id: 'match-in-progress',
    matchNumber: 537331,
    phase: 'GroupStage',
    groupName: 'C',
    kickoffTimeUtc: '2026-06-11T18:00:00Z',
    venue: 'MetLife Stadium',
    status: 'Scheduled',
    isSettled: false,
    homeScore90: null,
    awayScore90: null,
    homeTeam: team('team-eng', 'England', 'ENG'),
    awayTeam: team('team-usa', 'United States', 'USA'),
    myPrediction: {
      id: 'prediction-in-progress',
      predictedHomeScore: 2,
      predictedAwayScore: 1,
      createdAtUtc: '2026-06-10T12:00:00Z',
      updatedAtUtc: null,
      lockedAtUtc: '2026-06-11T18:00:00Z',
      points: null,
      isExactScore: null,
      isCorrectOutcome: null,
    },
    myPoints: null,
    canEditPrediction: true,
  },
  {
    id: 'match-fifa-aliases-one',
    matchNumber: 537329,
    phase: 'GroupStage',
    groupName: 'H',
    kickoffTimeUtc: '2026-06-13T19:00:00Z',
    venue: null,
    status: 'Scheduled',
    isSettled: false,
    homeScore90: null,
    awayScore90: null,
    homeTeam: team('team-cpv', 'Cape Verde Islands', 'CPV', 'CPV', 'H'),
    awayTeam: team('team-cod', 'Congo DR', 'COD', 'COD', 'H'),
    myPrediction: null,
    myPoints: null,
    canEditPrediction: true,
  },
  {
    id: 'match-fifa-aliases-two',
    matchNumber: 537330,
    phase: 'GroupStage',
    groupName: 'H',
    kickoffTimeUtc: '2026-06-14T19:00:00Z',
    venue: null,
    status: 'Scheduled',
    isSettled: false,
    homeScore90: null,
    awayScore90: null,
    homeTeam: team('team-ksa', 'Saudi Arabia', 'KSA', 'KSA', 'H'),
    awayTeam: team('team-ury', 'Uruguay', 'URY', 'URY', 'H'),
    myPrediction: null,
    myPoints: null,
    canEditPrediction: true,
  },
  {
    id: 'match-placeholder',
    matchNumber: 537417,
    phase: 'RoundOf32',
    groupName: null,
    kickoffTimeUtc: '2026-06-28T19:00:00Z',
    venue: null,
    status: 'Scheduled',
    isSettled: false,
    homeScore90: null,
    awayScore90: null,
    homeTeam: team('team-unknown-home', 'Unknown team', 'TBA'),
    awayTeam: team('team-unknown-away', 'Unknown team', 'TBA'),
    myPrediction: null,
    myPoints: null,
    canEditPrediction: true,
  },
]

test.beforeEach(async ({ page }) => {
  test.skip(!runsAgainstLocalPreview(), 'Regresja terminarza wymaga lokalnego preview z kodem z PR-a')

  await page.addInitScript((user) => {
    window.localStorage.setItem('typer.auth.token', 'test-token')
    window.localStorage.setItem('typer.auth.user', JSON.stringify(user))
  }, currentUser)

  await page.route('**/api/auth/me', async (route) => route.fulfill({ json: currentUser }))
  await page.route('**/api/matches/upcoming', async (route) => route.fulfill({ json: [matches[0]] }))
  await page.route('**/api/matches', async (route) => route.fulfill({ json: matches }))
  await page.route('**/api/ranking/top', async (route) => route.fulfill({ json: [] }))
})

test('player match list presents football-data schedule without API ids or unresolved knockout placeholders', async ({ page }) => {
  await page.goto('/matches')

  await expect(page.getByText('🇲🇽 Meksyk')).toBeVisible()
  await expect(page.getByText('🇿🇦 Republika Południowej Afryki')).toBeVisible()
  await expect(page.getByText('Faza grupowa · Grupa A')).toBeVisible()
  await expect(page.getByText('#537327')).toHaveCount(0)
  await expect(page.getByText('Unknown team')).toHaveCount(0)
  await expect(page.getByText('RoundOf32')).toHaveCount(0)
})

test('desktop match list derives missing flags and avoids horizontal overflow on laptop width', async ({ page }) => {
  await page.setViewportSize({ width: 1366, height: 768 })

  await page.goto('/matches')

  await expect(page.getByText('🇳🇬 Nigeria')).toBeVisible()
  await expect(page.getByText('🇳🇴 Norwegia')).toBeVisible()
  await expect(page.getByText('🇨🇻 Republika Zielonego Przylądka')).toBeVisible()
  await expect(page.getByText('🇨🇩 Demokratyczna Republika Konga')).toBeVisible()
  await expect(page.getByText('🇸🇦 Arabia Saudyjska')).toBeVisible()
  await expect(page.getByText('🇺🇾 Urugwaj')).toBeVisible()

  const hasHorizontalOverflow = await page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth)
  expect(hasHorizontalOverflow).toBe(false)

  const cardRects = await page.locator('article').evaluateAll((cards) =>
    cards.map((card) => {
      const rect = card.getBoundingClientRect()
      return { top: Math.round(rect.top), width: Math.round(rect.width) }
    }),
  )
  expect(cardRects.length).toBeGreaterThanOrEqual(2)
  expect(cardRects[0].top).toBe(cardRects[1].top)
  expect(Math.min(...cardRects.map((rect) => rect.width))).toBeGreaterThanOrEqual(560)
})

test('player match list labels locked post-kickoff scheduled match as in progress', async ({ page }) => {
  await page.goto('/matches')

  const inProgressCard = page.locator('article').filter({ hasText: 'Anglia' }).filter({ hasText: 'Stany Zjednoczone' })

  await expect(inProgressCard.getByText('W trakcie')).toBeVisible()
  await expect(inProgressCard.getByText('Typ zablokowany')).toBeVisible()
  await expect(inProgressCard.getByText('Zaplanowany')).toHaveCount(0)

  await page.getByRole('button', { name: 'Do obstawienia' }).click()
  await expect(inProgressCard).toHaveCount(0)

  await page.getByRole('button', { name: 'Zablokowane' }).click()
  await expect(inProgressCard.getByText('W trakcie')).toBeVisible()
})
