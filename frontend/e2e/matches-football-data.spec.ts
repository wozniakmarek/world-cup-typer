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
    homeTeam: team('team-mex', 'Mexico', 'MEX', '🇲🇽', 'A'),
    awayTeam: team('team-rsa', 'South Africa', 'RSA', '🇿🇦', 'A'),
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

  await expect(page.getByText('Mexico')).toBeVisible()
  await expect(page.getByText('South Africa')).toBeVisible()
  await expect(page.getByText('Faza grupowa · Grupa A')).toBeVisible()
  await expect(page.getByText('#537327')).toHaveCount(0)
  await expect(page.getByText('Unknown team')).toHaveCount(0)
  await expect(page.getByText('RoundOf32')).toHaveCount(0)
})
