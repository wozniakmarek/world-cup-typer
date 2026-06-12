import { expect, test } from '@playwright/test'
import type { Page } from '@playwright/test'
import { runsAgainstLocalPreview } from './helpers/environment'

const currentUser = {
  id: 'user-1',
  email: 'marek@example.com',
  displayName: 'Marek',
  role: 'Admin',
  isActive: true,
}

const buildRankingEntry = (index: number) => ({
  position: index,
  userId: `user-${index}`,
  displayName: `Player ${index}`,
  totalPoints: 20 - index,
  exactScoreHits: 0,
  correctOutcomeHits: 0,
  predictionsCount: 1,
  avatarUrl: null,
  isCurrentUser: index === 1,
})

const ranking = Array.from({ length: 10 }, (_, index) => buildRankingEntry(index + 1))

const progress = ranking.map((player, index) => ({
  userId: player.userId,
  displayName: player.displayName,
  avatarUrl: null,
  isCurrentUser: player.isCurrentUser,
  points: [
    {
      matchId: 'match-1',
      matchNumber: 1,
      matchLabel: 'KOR-CZE',
      snapshotAtUtc: '2026-06-11T21:00:00Z',
      totalPoints: 10 - index,
      exactScoreHits: 0,
      correctOutcomeHits: 0,
      predictionsCount: 1,
      position: index + 1,
    },
  ],
}))

async function mockLoggedInRanking(page: Page) {
  await page.addInitScript((user) => {
    window.localStorage.setItem('typer.auth.token', 'test-token')
    window.localStorage.setItem('typer.auth.user', JSON.stringify(user))
  }, currentUser)

  await page.route('**/api/auth/me', async (route) => route.fulfill({ json: currentUser }))
  await page.route('**/api/ranking/progress/all', async (route) => route.fulfill({ json: progress }))
  await page.route('**/api/ranking', async (route) => route.fulfill({ json: ranking }))
}

test('ranking progress tooltip shows every player with points for hovered match', async ({ page }) => {
  test.skip(!runsAgainstLocalPreview(), 'Ranking progress regression requires local preview with PR code')

  await mockLoggedInRanking(page)
  await page.goto('/ranking')

  await expect(page.getByText('Progres punktów po meczach')).toBeVisible()

  const chart = page.locator('.recharts-wrapper').first()
  await expect(chart).toBeVisible()
  await chart.scrollIntoViewIfNeeded()

  const chartBox = await chart.boundingBox()
  expect(chartBox).not.toBeNull()

  await page.mouse.move((chartBox?.x ?? 0) + (chartBox?.width ?? 0) - 64, (chartBox?.y ?? 0) + 120)

  const tooltip = page.locator('.recharts-tooltip-wrapper')
  await expect(tooltip.getByText('Player 10')).toBeVisible()
})
