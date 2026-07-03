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
  {
    id: 'match-settled-old',
    matchNumber: 537332,
    phase: 'GroupStage',
    groupName: 'D',
    kickoffTimeUtc: '2026-06-15T19:00:00Z',
    venue: 'Lumen Field',
    status: 'Settled',
    isSettled: true,
    homeScore90: 1,
    awayScore90: 0,
    homeTeam: team('team-fra', 'France', 'FRA', 'FRA', 'D'),
    awayTeam: team('team-sen', 'Senegal', 'SEN', 'SEN', 'D'),
    myPrediction: {
      id: 'prediction-settled-old',
      predictedHomeScore: 1,
      predictedAwayScore: 0,
      createdAtUtc: '2026-06-14T12:00:00Z',
      updatedAtUtc: null,
      lockedAtUtc: '2026-06-15T19:00:00Z',
      points: 3,
      isExactScore: true,
      isCorrectOutcome: true,
    },
    myPoints: 3,
    canEditPrediction: false,
  },
  {
    id: 'match-settled-new',
    matchNumber: 537333,
    phase: 'GroupStage',
    groupName: 'E',
    kickoffTimeUtc: '2026-06-27T19:00:00Z',
    venue: 'BC Place',
    status: 'Settled',
    isSettled: true,
    homeScore90: 3,
    awayScore90: 0,
    homeTeam: team('team-ger', 'Germany', 'GER', 'GER', 'E'),
    awayTeam: team('team-hai', 'Haiti', 'HAI', 'HAI', 'E'),
    myPrediction: {
      id: 'prediction-settled-new',
      predictedHomeScore: 2,
      predictedAwayScore: 0,
      createdAtUtc: '2026-06-26T12:00:00Z',
      updatedAtUtc: null,
      lockedAtUtc: '2026-06-27T19:00:00Z',
      points: 1,
      isExactScore: false,
      isCorrectOutcome: true,
    },
    myPoints: 1,
    canEditPrediction: false,
  },
  {
    id: 'match-open-one',
    matchNumber: 537334,
    phase: 'GroupStage',
    groupName: 'F',
    kickoffTimeUtc: '2026-07-01T19:00:00Z',
    venue: 'SoFi Stadium',
    status: 'Scheduled',
    isSettled: false,
    homeScore90: null,
    awayScore90: null,
    homeTeam: team('team-pol-open', 'Poland', 'POL', 'POL', 'F'),
    awayTeam: team('team-por-open', 'Portugal', 'POR', 'POR', 'F'),
    myPrediction: null,
    myPoints: null,
    canEditPrediction: true,
  },
  {
    id: 'match-open-two',
    matchNumber: 537335,
    phase: 'GroupStage',
    groupName: 'F',
    kickoffTimeUtc: '2026-07-02T19:00:00Z',
    venue: 'SoFi Stadium',
    status: 'Scheduled',
    isSettled: false,
    homeScore90: null,
    awayScore90: null,
    homeTeam: team('team-esp-open', 'Spain', 'ESP', 'ESP', 'F'),
    awayTeam: team('team-ita-open', 'Italy', 'ITA', 'ITA', 'F'),
    myPrediction: null,
    myPoints: null,
    canEditPrediction: true,
  },
  {
    id: 'match-open-three',
    matchNumber: 537336,
    phase: 'GroupStage',
    groupName: 'G',
    kickoffTimeUtc: '2026-07-03T19:00:00Z',
    venue: 'Hard Rock Stadium',
    status: 'Scheduled',
    isSettled: false,
    homeScore90: null,
    awayScore90: null,
    homeTeam: team('team-bra-open', 'Brazil', 'BRA', 'BRA', 'G'),
    awayTeam: team('team-arg-open', 'Argentina', 'ARG', 'ARG', 'G'),
    myPrediction: null,
    myPoints: null,
    canEditPrediction: true,
  },
  {
    id: 'match-open-four',
    matchNumber: 537337,
    phase: 'GroupStage',
    groupName: 'G',
    kickoffTimeUtc: '2026-07-04T19:00:00Z',
    venue: 'Hard Rock Stadium',
    status: 'Scheduled',
    isSettled: false,
    homeScore90: null,
    awayScore90: null,
    homeTeam: team('team-mar-open', 'Morocco', 'MAR', 'MAR', 'G'),
    awayTeam: team('team-jpn-open', 'Japan', 'JPN', 'JPN', 'G'),
    myPrediction: null,
    myPoints: null,
    canEditPrediction: true,
  },
  {
    id: 'match-open-bottom',
    matchNumber: 537338,
    phase: 'GroupStage',
    groupName: 'G',
    kickoffTimeUtc: '2026-07-05T19:00:00Z',
    venue: 'Hard Rock Stadium',
    status: 'Scheduled',
    isSettled: false,
    homeScore90: null,
    awayScore90: null,
    homeTeam: team('team-can-open', 'Canada', 'CAN', 'CAN', 'G'),
    awayTeam: team('team-ned-open', 'Netherlands', 'NED', 'NED', 'G'),
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
  await page.route('**/api/matches/match-in-progress', async (route) => route.fulfill({
    json: {
      ...matches[2],
      homeScore90: 0,
      awayScore90: 1,
      homeScoreFinal: 0,
      awayScoreFinal: 1,
      canViewPredictions: true,
    },
  }))
  await page.route('**/api/matches/match-in-progress/predictions', async (route) => route.fulfill({
    json: {
      canViewAllPredictions: true,
      predictions: [],
    },
  }))
  await page.route('**/api/matches/match-open-bottom', async (route) => route.fulfill({
    json: {
      ...matches.find((match) => match.id === 'match-open-bottom'),
      homeScoreFinal: null,
      awayScoreFinal: null,
      canViewPredictions: true,
    },
  }))
  await page.route('**/api/matches/match-open-bottom/predictions', async (route) => route.fulfill({
    json: {
      canViewAllPredictions: true,
      predictions: Array.from({ length: 18 }, (_, index) => ({
        predictionId: `prediction-open-bottom-${index}`,
        userId: `user-open-bottom-${index}`,
        displayName: `Gracz ${index + 1}`,
        predictedHomeScore: index % 4,
        predictedAwayScore: (index + 1) % 3,
        points: null,
        isExactScore: null,
        isCorrectOutcome: null,
      })),
    },
  }))
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

test('player match list keeps the latest settled matches at the top of the settled filter', async ({ page }) => {
  await page.goto('/matches')

  await page.getByRole('button', { name: 'Rozliczone' }).click()

  const settledCards = page.locator('article')
  await expect(settledCards).toHaveCount(2)
  await expect(settledCards.nth(0)).toContainText('Niemcy')
  await expect(settledCards.nth(0)).toContainText('Haiti')
  await expect(settledCards.nth(1)).toContainText('Francja')
  await expect(settledCards.nth(1)).toContainText('Senegal')
})

test('mobile match navigation restores the last list position after leaving matches', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 667 })
  await page.goto('/matches')

  const lowerMatchCard = page.locator('article').filter({ hasText: 'Niemcy' }).filter({ hasText: 'Haiti' })
  await lowerMatchCard.scrollIntoViewIfNeeded()

  const savedScrollY = await page.evaluate(() => Math.round(window.scrollY))
  expect(savedScrollY).toBeGreaterThan(200)

  const mobileNavigation = page.getByLabel('Nawigacja mobilna gracza')
  await mobileNavigation.getByRole('link', { name: 'Dashboard' }).click()
  await expect(page).toHaveURL(/\/$/)
  await page.evaluate(() => window.scrollTo(0, 0))

  await mobileNavigation.getByRole('link', { name: 'Mecze' }).click()
  await expect(page).toHaveURL(/\/matches$/)
  await expect.poll(() => page.evaluate(() => Math.round(window.scrollY))).toBeGreaterThan(savedScrollY - 120)
  await expect(lowerMatchCard).toBeInViewport()
})

test('mobile match details start at the top after opening a lower match card', async ({ page }) => {
  await page.addInitScript(() => {
    const trackedWindow = window as Window & { __scrollToTopCalls: number[] }
    const originalScrollTo = window.scrollTo.bind(window)
    trackedWindow.__scrollToTopCalls = []
    window.scrollTo = ((optionsOrX?: ScrollToOptions | number, y?: number) => {
      const top = typeof optionsOrX === 'object' ? optionsOrX.top : y
      if (typeof top === 'number') {
        trackedWindow.__scrollToTopCalls.push(Math.round(top))
      }

      if (typeof optionsOrX === 'object') {
        originalScrollTo(optionsOrX)
        return
      }

      originalScrollTo(optionsOrX ?? 0, y ?? 0)
    }) as typeof window.scrollTo
  })

  await page.setViewportSize({ width: 390, height: 667 })
  await page.goto('/matches')

  await page.getByRole('button', { name: 'Do obstawienia' }).click()
  const lowerOpenMatchCard = page.locator('article').filter({ hasText: 'Kanada' }).filter({ hasText: 'Holandia' })
  await lowerOpenMatchCard.scrollIntoViewIfNeeded()

  const listScrollY = await page.evaluate(() => Math.round(window.scrollY))
  expect(listScrollY).toBeGreaterThan(200)

  await lowerOpenMatchCard.getByRole('link', { name: 'Szczegóły meczu' }).click()

  await expect(page).toHaveURL(/\/matches\/match-open-bottom$/)
  await expect(page.getByRole('heading', { name: /Kanada vs Holandia/ })).toBeVisible()
  await expect.poll(() => page.evaluate(() => {
    const trackedWindow = window as Window & { __scrollToTopCalls?: number[] }
    return trackedWindow.__scrollToTopCalls?.includes(0) ?? false
  })).toBe(true)
  await expect.poll(() => page.evaluate(() => Math.round(window.scrollY))).toBe(0)
})

test('mobile matches navigation returns to the previous filter and list position from details', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 667 })
  await page.goto('/matches')

  await page.getByRole('button', { name: 'Do obstawienia' }).click()
  const lowerOpenMatchCard = page.locator('article').filter({ hasText: 'Kanada' }).filter({ hasText: 'Holandia' })
  await lowerOpenMatchCard.scrollIntoViewIfNeeded()

  const savedScrollY = await page.evaluate(() => Math.round(window.scrollY))
  expect(savedScrollY).toBeGreaterThan(200)

  await lowerOpenMatchCard.getByRole('link', { name: 'Szczegóły meczu' }).click()
  await expect(page).toHaveURL(/\/matches\/match-open-bottom$/)

  await page.getByLabel('Nawigacja mobilna gracza').getByRole('link', { name: 'Mecze' }).click()

  await expect(page).toHaveURL(/\/matches$/)
  await expect(page.locator('article')).toHaveCount(5)
  await expect(page.getByText('Meksyk')).toHaveCount(0)
  await expect.poll(() => page.evaluate(() => Math.round(window.scrollY))).toBeGreaterThan(savedScrollY - 120)
  await expect(lowerOpenMatchCard).toBeInViewport()
})

test('player match details hide unsafe in-progress score from the 90 minute result block', async ({ page }) => {
  await page.goto('/matches/match-in-progress')

  await expect(page.getByText('W trakcie')).toBeVisible()
  await expect(page.getByText('Wynik po 90 minutach')).toHaveCount(0)
  await expect(page.getByText('0 : 1')).toHaveCount(0)
  await expect(page.getByText('Po kickoffie backend blokuje')).toHaveCount(0)
})
