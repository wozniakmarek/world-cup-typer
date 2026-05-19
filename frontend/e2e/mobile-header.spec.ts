import { expect, test } from '@playwright/test'
import type { Locator } from '@playwright/test'
import { runsAgainstLocalPreview } from './helpers/environment'

const MAX_TITLE_OFFSET_FROM_HEADER = 96 // allows typical spacing below header plus CI font/render variance
const MAX_TITLE_Y_IN_FIRST_VIEWPORT = 360 // ~43% of 844px viewport height

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

async function getVisibleBoundingBox(locator: Locator, message: string) {
  const box = await locator.boundingBox()
  expect(box, message).not.toBeNull()
  return box as NonNullable<typeof box>
}

test.beforeEach(async ({ page, baseURL }) => {
  test.skip(!baseURL, 'Mobile layout regression test requires baseURL in Playwright configuration.')
  test.info().annotations.push({
    type: 'base-url-host',
    description: runsAgainstLocalPreview() ? 'local-preview' : 'remote-host',
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
  const dashboardTitle = page.getByRole('heading', {
    name: new RegExp(currentUser.displayName, 'i'),
  })

  await expect(dashboardTitle).toBeVisible()

  const headerBox = await getVisibleBoundingBox(
    header,
    'Could not read header position. Make sure the header is visible.',
  )
  const dashboardTitleBox = await getVisibleBoundingBox(
    dashboardTitle,
    'Could not read dashboard title position.',
  )

  const headerBottom = headerBox.y + headerBox.height

  // Keep the main heading close to the header, allowing ~96px for content spacing and CI font variance.
  expect(dashboardTitleBox.y).toBeLessThanOrEqual(headerBottom + MAX_TITLE_OFFSET_FROM_HEADER)
  // In a 390x844 viewport, 360px (~43%) keeps the heading in the upper part of the first screen.
  expect(dashboardTitleBox.y).toBeLessThanOrEqual(MAX_TITLE_Y_IN_FIRST_VIEWPORT)
})
