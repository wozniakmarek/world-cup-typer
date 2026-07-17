import { expect, test } from '@playwright/test'
import type { Page } from '@playwright/test'
import { readSecretValue, runsAgainstLocalPreview, shouldRunRoleLoginSmoke } from './helpers/environment'

const smokeMode = readSecretValue(process.env.E2E_SMOKE_MODE, 'E2E_SMOKE_MODE')?.toLowerCase() ?? 'production'
const isStagingSmoke = smokeMode === 'staging'
const isLocalPreview = runsAgainstLocalPreview()
const runRoleLoginSmoke = shouldRunRoleLoginSmoke({ smokeMode, isLocalPreview })

const localPlayer = {
  id: 'user-7',
  email: 'ania@example.com',
  displayName: 'Ania Kowalska',
  role: 'Player',
  isActive: true,
  requiresPasswordChange: false,
  avatarUrl: null,
}

const localFinalSummaryAvailabilityReady = {
  isReady: true,
  reason: 'ready',
  settledMatchesCount: 104,
  requiredSettledMatchesCount: 104,
  totalMatchesCount: 104,
  finalMatchLabel: 'ARG-ESP',
}

const localFinalSummaryAvailabilityLocked = {
  isReady: false,
  reason: 'matches-still-open',
  settledMatchesCount: 102,
  requiredSettledMatchesCount: 104,
  totalMatchesCount: 104,
  finalMatchLabel: 'ARG-ESP',
}

const localPersonalFinalSummary = {
  userId: localPlayer.id,
  displayName: localPlayer.displayName,
  avatarUrl: null,
  finalPosition: 7,
  totalPoints: 88,
  exactScoreHits: 18,
  correctOutcomeHits: 51,
  predictionsCount: 76,
  personalFacts: [
    {
      id: 'late-surge',
      label: 'Twój finisz',
      title: 'Awans o 5 miejsc w fazie pucharowej',
      description: 'Najwięcej punktów dorzuciłaś wtedy, gdy tabela była już ciasna.',
      relatedUserIds: [localPlayer.id],
      relatedMatchIds: ['match-42'],
    },
    {
      id: 'exact-specialist',
      label: 'Twój podpis',
      title: '18 dokładnych wyników',
      description: 'To były mecze, w których wynik siadł co do bramki.',
      relatedUserIds: [localPlayer.id],
      relatedMatchIds: [],
    },
  ],
  highlightedMatchIds: ['match-42'],
}

const localFinalSummary = {
  stats: {
    settledMatchesCount: 76,
    activePlayersCount: 24,
    finalLeaderUserId: 'user-1',
    finalLeaderDisplayName: 'Marek',
  },
  positionSeries: [
    {
      userId: 'user-1',
      displayName: 'Marek',
      avatarUrl: null,
      finalPosition: 1,
      finalPoints: 121,
      isCurrentUser: false,
      points: [
        {
          matchId: 'match-1',
          matchNumber: 1,
          matchLabel: 'POL-GER',
          snapshotAtUtc: '2026-06-11T20:00:00Z',
          position: 2,
          totalPoints: 3,
        },
        {
          matchId: 'match-2',
          matchNumber: 2,
          matchLabel: 'FRA-ESP',
          snapshotAtUtc: '2026-06-12T20:00:00Z',
          position: 1,
          totalPoints: 6,
        },
      ],
    },
    {
      userId: localPlayer.id,
      displayName: localPlayer.displayName,
      avatarUrl: null,
      finalPosition: 7,
      finalPoints: 88,
      isCurrentUser: true,
      points: [
        {
          matchId: 'match-1',
          matchNumber: 1,
          matchLabel: 'POL-GER',
          snapshotAtUtc: '2026-06-11T20:00:00Z',
          position: 10,
          totalPoints: 1,
        },
        {
          matchId: 'match-2',
          matchNumber: 2,
          matchLabel: 'FRA-ESP',
          snapshotAtUtc: '2026-06-12T20:00:00Z',
          position: 7,
          totalPoints: 4,
        },
      ],
    },
  ],
  finalTop: [
    {
      userId: 'user-1',
      displayName: 'Marek',
      avatarUrl: null,
      finalPosition: 1,
      totalPoints: 121,
      exactScoreHits: 24,
      correctOutcomeHits: 73,
      predictionsCount: 104,
      isCurrentUser: false,
    },
    {
      userId: localPlayer.id,
      displayName: localPlayer.displayName,
      avatarUrl: null,
      finalPosition: 7,
      totalPoints: 88,
      exactScoreHits: 18,
      correctOutcomeHits: 51,
      predictionsCount: 76,
      isCurrentUser: true,
    },
  ],
  globalFacts: [
    {
      id: 'tournament-surge',
      label: 'Ciekawostka turniejowa',
      title: 'Największy skok turnieju: +7 miejsc',
      description: 'Najmocniejszy ruch w tabeli wydarzył się po drugim meczu finałowej serii.',
      relatedUserIds: ['user-1'],
      relatedMatchIds: ['match-2'],
    },
    {
      id: 'most-exact-match',
      label: 'Najbardziej trafiony mecz',
      title: 'POL-GER: 8 dokładnych typów',
      description: 'To spotkanie zebrało najwięcej idealnych wyników w całym turnieju.',
      relatedUserIds: [],
      relatedMatchIds: ['match-1'],
    },
  ],
}

async function mockLocalAuth(page: Page) {
  await page.route('**/api/auth/me', async (route) => route.fulfill({ json: localPlayer }))
}

async function mockLocalLogin(page: Page) {
  await page.route('**/api/auth/login', async (route) =>
    route.fulfill({
      json: {
        token: 'test-token',
        user: localPlayer,
      },
    }),
  )
}

async function mockLocalPersonalFinalSummary(page: Page) {
  await page.route('**/api/summary/final/me', async (route) =>
    route.fulfill({
      json: localPersonalFinalSummary,
    }),
  )
}

async function mockLocalFinalSummaryAvailability(page: Page, availability = localFinalSummaryAvailabilityReady) {
  await page.route('**/api/summary/final/availability', async (route) =>
    route.fulfill({
      json: availability,
    }),
  )
}

async function mockLocalFinalSummary(page: Page) {
  await mockLocalFinalSummaryAvailability(page)
  await page.route('**/api/summary/final', async (route) =>
    route.fulfill({
      json: localFinalSummary,
    }),
  )
}

async function submitLoginForm(page: Page, email: string, password: string) {
  await page.getByLabel('Login').fill(email)
  await page.getByLabel('Hasło').fill(password)
  await page.getByRole('button', { name: 'Wejdź do aplikacji' }).click()
}

async function login(page: Page, email: string, password: string) {
  await page.goto('/login')
  await submitLoginForm(page, email, password)
  await expect(page.getByRole('button', { name: 'Wyloguj' })).toBeVisible({ timeout: 15_000 })
}

test('strona logowania ładuje się poprawnie', async ({ page }) => {
  await page.goto('/login')
  await expect(page.getByText('Logowanie')).toBeVisible()
  await expect(page.getByText('Zaloguj się mailem albo nazwą gracza.')).toBeVisible()
  await expect(page.getByLabel('Login')).toBeVisible()
  await expect(page.getByLabel('Hasło')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Wejdź do aplikacji' })).toBeVisible()
})

test('publiczny home pokazuje landing z rankingiem przed logowaniem', async ({ page }) => {
  test.skip(!isLocalPreview, 'Publiczny landing z mockowanym API sprawdzamy lokalnie')

  await page.route('**/api/ranking/top', async (route) =>
    route.fulfill({
      json: [
        {
          position: 1,
          userId: 'user-1',
          displayName: 'Marek',
          totalPoints: 18,
          exactScoreHits: 4,
          correctOutcomeHits: 6,
          predictionsCount: 12,
          avatarUrl: null,
          isCurrentUser: false,
        },
        {
          position: 2,
          userId: 'user-2',
          displayName: 'Kuba',
          totalPoints: 15,
          exactScoreHits: 3,
          correctOutcomeHits: 6,
          predictionsCount: 11,
          avatarUrl: null,
          isCurrentUser: false,
        },
      ],
    }),
  )

  await page.goto('/')

  await expect(page.getByRole('heading', { name: 'Typer Mistrzostw Świata' })).toBeVisible()
  await expect(page.getByText('Publiczny ranking')).toBeVisible()
  await expect(page.locator('#ranking').getByText('Marek')).toBeVisible()
  await expect(page.getByRole('link', { name: 'Przejdź do logowania' })).toHaveAttribute('href', '/login')
  await expect(page.locator('[data-testid="final-ranking-story-chart"]')).toHaveCount(0)
})

test('publiczny finalny recap jest jeszcze zablokowany przed rozliczeniem finalu', async ({ page }) => {
  test.skip(!isLocalPreview, 'Zablokowany final summary z mockowanym API sprawdzamy lokalnie')

  let finalSummaryCalled = false
  await mockLocalFinalSummaryAvailability(page, localFinalSummaryAvailabilityLocked)
  await page.route('**/api/summary/final', async (route) => {
    finalSummaryCalled = true
    await route.fulfill({
      status: 409,
      json: {
        message: 'Finalny recap będzie dostępny po rozliczeniu finału.',
        availability: localFinalSummaryAvailabilityLocked,
      },
    })
  })

  await page.goto('/summary/final')

  await expect(page.getByRole('heading', { name: 'Recap odblokuje się po finale ARG-ESP' })).toBeVisible()
  await expect(page.getByText('102 z 104 meczów rozliczonych')).toBeVisible()
  await expect(page.getByText('Do pokazania finalnego podsumowania brakuje jeszcze rozliczenia wszystkich meczów.')).toBeVisible()
  await expect(page.locator('[data-testid="final-ranking-story-chart"]')).toHaveCount(0)
  expect(finalSummaryCalled).toBe(false)
})

test('publiczne finalne podsumowanie jest na dedykowanej stronie', async ({ page }) => {
  test.skip(!isLocalPreview, 'Publiczny final summary z mockowanym API sprawdzamy lokalnie')

  await mockLocalFinalSummary(page)

  await page.goto('/summary/final')

  await expect(page).toHaveURL(/\/summary\/final$/)
  await expect(page.getByRole('heading', { name: 'Cala tabela, mecz po meczu' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Podium' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Wybrani zawodnicy' })).toBeVisible()
  await page.getByRole('button', { name: 'Ania Kowalska' }).click()
  await expect(page.getByRole('button', { name: 'Ania Kowalska' })).toHaveAttribute('aria-pressed', 'true')
  await expect(page.locator('[data-testid="final-ranking-story-chart"]')).toBeVisible()
  await expect(page.locator('[data-testid="static-final-ranking-table"]')).toHaveCount(0)
  await expect(page.getByText('Animowana pełna tabela')).toBeVisible()
  await expect(page.locator('#final-table .border-dashed')).toHaveCount(0)
  await expect(page.getByText('76', { exact: true })).toBeVisible()
  await expect(page.getByText('24', { exact: true })).toBeVisible()
  await expect(page.getByText('121', { exact: true })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Największy skok turnieju: +7 miejsc' })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Zaloguj sie po swoj recap' })).toHaveAttribute(
    'href',
    '/login?returnTo=%2Fsummary%2Ffinal%2Fme',
  )
})

test('zalogowany gracz widzi swój finałowy recap', async ({ page }) => {
  test.skip(!isLocalPreview, 'Personal final summary z mockowanym API sprawdzamy lokalnie')

  await page.addInitScript((user) => {
    window.localStorage.setItem('typer.auth.token', 'test-token')
    window.localStorage.setItem('typer.auth.user', JSON.stringify(user))
  }, localPlayer)

  await mockLocalAuth(page)
  await mockLocalPersonalFinalSummary(page)
  await mockLocalFinalSummary(page)

  await page.goto('/summary/final/me')

  await expect(page.getByRole('heading', { name: 'Twój finałowy recap' })).toBeVisible()
  await expect(page.getByRole('heading', { name: localPlayer.displayName })).toBeVisible()
  await expect(page.getByText('#7', { exact: true }).first()).toBeVisible()
  await expect(page.getByText('88', { exact: true })).toBeVisible()
  await expect(page.getByText('18', { exact: true })).toBeVisible()
  await expect(page.getByText('51', { exact: true })).toBeVisible()
  await expect(page.getByText('76', { exact: true })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Awans o 5 miejsc w fazie pucharowej' })).toBeVisible()
  await expect(page.getByRole('heading', { name: '18 dokładnych wyników' })).toBeVisible()
  await expect(page.locator('[data-testid="final-ranking-story-chart"]')).toBeVisible()
  const myRunFilter = page.getByRole('button', { name: 'Mój przebieg' })
  await expect(myRunFilter).toBeVisible()
  await expect(myRunFilter).toBeEnabled()
  const personalFact = page.getByRole('heading', { name: '18 dokładnych wyników' })
  const globalFact = page.getByRole('heading', { name: 'Największy skok turnieju: +7 miejsc' })
  await expect(globalFact).toBeVisible()
  const personalFactBox = await personalFact.boundingBox()
  const globalFactBox = await globalFact.boundingBox()
  expect(personalFactBox, 'personal fact should have a rendered position').not.toBeNull()
  expect(globalFactBox, 'global tournament fact should have a rendered position').not.toBeNull()
  expect(globalFactBox!.y).toBeGreaterThan(personalFactBox!.y)
  await expect(page.getByRole('link', { name: 'Mój recap', exact: true }).first()).toBeVisible()
})

test('personalny finalny recap jest jeszcze zablokowany przed rozliczeniem finalu', async ({ page }) => {
  test.skip(!isLocalPreview, 'Zablokowany personal final summary z mockowanym API sprawdzamy lokalnie')

  let personalSummaryCalled = false
  await page.addInitScript((user) => {
    window.localStorage.setItem('typer.auth.token', 'test-token')
    window.localStorage.setItem('typer.auth.user', JSON.stringify(user))
  }, localPlayer)

  await mockLocalAuth(page)
  await mockLocalFinalSummaryAvailability(page, localFinalSummaryAvailabilityLocked)
  await page.route('**/api/summary/final/me', async (route) => {
    personalSummaryCalled = true
    await route.fulfill({
      status: 409,
      json: {
        message: 'Finalny recap będzie dostępny po rozliczeniu finału.',
        availability: localFinalSummaryAvailabilityLocked,
      },
    })
  })

  await page.goto('/summary/final/me')

  await expect(page.getByRole('heading', { name: 'Recap odblokuje się po finale ARG-ESP' })).toBeVisible()
  await expect(page.getByText('102 z 104 meczów rozliczonych')).toBeVisible()
  await expect(page.locator('[data-testid="final-ranking-story-chart"]')).toHaveCount(0)
  expect(personalSummaryCalled).toBe(false)
})

test('anonimowy gracz wraca po loginie do chronionego recap', async ({ page }) => {
  test.skip(!isLocalPreview, 'Return target dla chronionego recap sprawdzamy lokalnie')

  await mockLocalLogin(page)
  await mockLocalAuth(page)
  await mockLocalPersonalFinalSummary(page)
  await mockLocalFinalSummary(page)

  await page.goto('/summary/final/me')

  await expect(page).toHaveURL(/\/login\?returnTo=%2Fsummary%2Ffinal%2Fme$/)
  await expect(page.getByText('Logowanie')).toBeVisible()
  await submitLoginForm(page, localPlayer.email, 'test-password')

  await expect(page).toHaveURL(/\/summary\/final\/me$/)
  await expect(page.getByRole('heading', { name: 'Twój finałowy recap' })).toBeVisible()
  await expect(page.getByRole('heading', { name: localPlayer.displayName })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Awans o 5 miejsc w fazie pucharowej' })).toBeVisible()
  await expect(page.locator('[data-testid="final-ranking-story-chart"]')).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Największy skok turnieju: +7 miejsc' })).toBeVisible()
})

test('login ignoruje zewnętrzny returnTo przy aktywnej sesji', async ({ page }) => {
  test.skip(!isLocalPreview, 'Bezpieczny returnTo sprawdzamy lokalnie')

  await page.addInitScript((user) => {
    window.localStorage.setItem('typer.auth.token', 'test-token')
    window.localStorage.setItem('typer.auth.user', JSON.stringify(user))
  }, localPlayer)
  await mockLocalAuth(page)

  await page.goto('/login?returnTo=https%3A%2F%2Fevil.example')

  await expect(page).toHaveURL(/\/$/)
  expect(new URL(page.url()).origin).not.toBe('https://evil.example')
})

const roles = [
  {
    name: 'admin',
    email: readSecretValue(process.env.E2E_ADMIN_EMAIL, 'E2E_ADMIN_EMAIL'),
    password: readSecretValue(process.env.E2E_ADMIN_PASSWORD, 'E2E_ADMIN_PASSWORD'),
  },
  {
    name: 'player',
    email: readSecretValue(process.env.E2E_PLAYER_EMAIL, 'E2E_PLAYER_EMAIL'),
    password: readSecretValue(process.env.E2E_PLAYER_PASSWORD, 'E2E_PLAYER_PASSWORD'),
  },
]

for (const role of roles) {
  test(`logowanie smoke (${role.name})`, async ({ page }) => {
    test.skip(!runRoleLoginSmoke, `Logowanie smoke dla roli ${role.name} uruchamiamy tylko na stagingu`)
    test.skip(!role.email || !role.password, `Brak danych logowania dla roli: ${role.name}`)
    await login(page, role.email!, role.password!)
  })
}

test('staging smoke (player): mecze, ranking i profil', async ({ page }) => {
  const player = roles.find((role) => role.name === 'player')

  test.skip(!isStagingSmoke, 'Rozszerzony smoke jest uruchamiany tylko dla staging')
  test.skip(!player?.email || !player.password, 'Brak danych logowania dla roli: player')

  await login(page, player.email!, player.password!)

  await page.getByRole('link', { name: 'Mecze', exact: true }).click()
  await expect(page).toHaveURL(/\/matches$/)
  await expect(page.getByText('Typowanie spotkań')).toBeVisible()

  await page.getByRole('link', { name: 'Ranking', exact: true }).click()
  await expect(page).toHaveURL(/\/ranking$/)
  await expect(page.getByText('Tabela liderów')).toBeVisible()

  await page.getByRole('link', { name: 'Profil', exact: true }).click()
  await expect(page).toHaveURL(/\/profile$/)
  await expect(page.getByText('Moje statystyki')).toBeVisible()
})

test('staging smoke (admin): wejście do panelu admina', async ({ page }) => {
  const admin = roles.find((role) => role.name === 'admin')

  test.skip(!isStagingSmoke, 'Rozszerzony smoke jest uruchamiany tylko dla staging')
  test.skip(!admin?.email || !admin.password, 'Brak danych logowania dla roli: admin')

  await login(page, admin.email!, admin.password!)

  await page.getByRole('link', { name: 'Admin', exact: true }).click()
  await expect(page).toHaveURL(/\/admin$/)
  await expect(page.getByText('Centrum sterowania')).toBeVisible()
})
