import { expect, test } from '@playwright/test'
import type { Locator } from '@playwright/test'
import { runsAgainstLocalPreview } from './helpers/environment'

const MAX_TITLE_OFFSET_FROM_HEADER = 96
const MAX_TITLE_Y_IN_FIRST_VIEWPORT = 260

const currentUser = {
  id: 'user-1',
  email: 'marek.wozniak.with.long.email@example.com',
  displayName: 'Marek Wozniak Bardzo Dlugie Nazwisko',
  role: 'Admin',
  isActive: true,
}

const playerUser = {
  ...currentUser,
  role: 'Player',
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

const ranking = [
  {
    position: 1,
    userId: currentUser.id,
    displayName: currentUser.displayName,
    totalPoints: 123,
    exactScoreHits: 12,
    correctOutcomeHits: 34,
    predictionsCount: 56,
    avatarUrl: null,
    isCurrentUser: true,
  },
]

const rankingProgress = [
  {
    matchId: 'match-1',
    matchNumber: 1,
    snapshotAtUtc: '2026-06-11T21:00:00Z',
    totalPoints: 123,
    exactScoreHits: 12,
    correctOutcomeHits: 34,
    predictionsCount: 56,
    position: 1,
  },
]

async function getVisibleBoundingBox(locator: Locator, message: string) {
  const box = await locator.boundingBox()
  expect(box, message).not.toBeNull()
  return box as NonNullable<typeof box>
}

test.beforeEach(async ({ page }) => {
  test.skip(!runsAgainstLocalPreview(), 'Mobilna regresja layoutu wymaga lokalnego preview z kodem z PR-a')

  await page.setViewportSize({ width: 390, height: 844 })
  await page.addInitScript((user) => {
    window.localStorage.setItem('typer.auth.token', 'test-token')
    window.localStorage.setItem('typer.auth.user', JSON.stringify(user))
  }, currentUser)

  await page.route('**/api/auth/me', async (route) => route.fulfill({ json: currentUser }))
  await page.route('**/api/matches/match-1/predictions', async (route) =>
    route.fulfill({
      json: {
        canViewAllPredictions: false,
        predictions: [
          {
            predictionId: 'prediction-1',
            userId: currentUser.id,
            displayName: currentUser.displayName,
            predictedHomeScore: 2,
            predictedAwayScore: 1,
            points: null,
          },
        ],
      },
    }),
  )
  await page.route('**/api/matches/match-1', async (route) =>
    route.fulfill({
      json: {
        ...matches[0],
        homeScoreFinal: null,
        awayScoreFinal: null,
        canViewPredictions: false,
      },
    }),
  )
  await page.route('**/api/matches/upcoming', async (route) => route.fulfill({ json: matches }))
  await page.route('**/api/matches', async (route) => route.fulfill({ json: matches }))
  await page.route('**/api/ranking/top', async (route) => route.fulfill({ json: ranking }))
  await page.route('**/api/ranking/me', async (route) => route.fulfill({ json: ranking[0] }))
  await page.route('**/api/ranking/progress', async (route) => route.fulfill({ json: rankingProgress }))
  await page.route('**/api/ranking', async (route) => route.fulfill({ json: ranking }))
  await page.route('**/api/predictions/my', async (route) =>
    route.fulfill({
      json: [
        {
          matchId: 'match-1',
          homeTeamName: matches[0].homeTeam.name,
          awayTeamName: matches[0].awayTeam.name,
          kickoffTimeUtc: matches[0].kickoffTimeUtc,
          prediction: {
            id: 'prediction-1',
            predictedHomeScore: 2,
            predictedAwayScore: 1,
            createdAtUtc: '2026-06-01T12:00:00Z',
            points: null,
          },
        },
      ],
    }),
  )
  await page.route('**/api/admin/players', async (route) => route.fulfill({ json: [] }))
  await page.route('**/api/teams', async (route) => route.fulfill({ json: [] }))
  await page.route('**/api/admin/matches', async (route) => route.fulfill({ json: [] }))
})

test('logged-in mobile pages do not create horizontal document scroll', async ({ page }) => {
  for (const path of [
    '/',
    '/matches',
    '/matches/match-1',
    '/ranking',
    '/profile',
    '/admin',
    '/admin/players',
    '/admin/teams',
    '/admin/matches',
  ]) {
    await page.goto(path)
    await page.waitForLoadState('networkidle')

    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)
    expect(overflow, `${path} should fit a 390px viewport without horizontal scroll`).toBeLessThanOrEqual(1)
  }
})

test('logged-in mobile shell keeps primary content high in the first viewport', async ({ page }) => {
  await page.goto('/')

  const header = page.locator('header')
  const dashboardTitle = page.getByRole('heading', { name: new RegExp(currentUser.displayName, 'i') })

  await expect(dashboardTitle).toBeVisible()

  const headerBox = await getVisibleBoundingBox(header, 'Could not read header position. Make sure the header is visible.')
  const dashboardTitleBox = await getVisibleBoundingBox(dashboardTitle, 'Could not read dashboard title position.')
  const headerBottom = headerBox.y + headerBox.height

  expect(dashboardTitleBox.y).toBeLessThanOrEqual(headerBottom + MAX_TITLE_OFFSET_FROM_HEADER)
  expect(dashboardTitleBox.y).toBeLessThanOrEqual(MAX_TITLE_Y_IN_FIRST_VIEWPORT)
})

test('player mobile navigation keeps primary destinations in a fixed thumb bar', async ({ page }) => {
  await page.route('**/api/auth/me', async (route) => route.fulfill({ json: playerUser }))
  await page.addInitScript((user) => {
    window.localStorage.setItem('typer.auth.user', JSON.stringify(user))
  }, playerUser)

  await page.goto('/')

  const bottomNavigation = page.getByRole('navigation', { name: 'Nawigacja mobilna gracza' })
  await expect(bottomNavigation).toBeVisible()

  const viewport = page.viewportSize()
  expect(viewport).not.toBeNull()

  const navigationBox = await getVisibleBoundingBox(bottomNavigation, 'Could not read mobile bottom nav position.')
  expect(navigationBox.y + navigationBox.height).toBeGreaterThanOrEqual((viewport?.height ?? 0) - 24)

  for (const label of ['Dashboard', 'Mecze', 'Ranking', 'Profil']) {
    await expect(bottomNavigation.getByRole('link', { name: label, exact: true })).toBeVisible()
  }

  await expect(bottomNavigation.getByRole('link', { name: 'Admin', exact: true })).toHaveCount(0)
})

test('admin mobile navigation keeps player destinations and exposes admin shortcuts', async ({ page }) => {
  await page.goto('/')

  const bottomNavigation = page.getByRole('navigation', { name: 'Nawigacja mobilna gracza' })
  await expect(bottomNavigation).toBeVisible()

  for (const label of ['Dashboard', 'Mecze', 'Ranking', 'Profil']) {
    await expect(bottomNavigation.getByRole('link', { name: label, exact: true })).toBeVisible()
  }

  for (const label of ['Admin', 'Gracze', 'Drużyny', 'Mecze Admin']) {
    await expect(bottomNavigation.getByRole('link', { name: label, exact: true })).toBeVisible()
  }
})
