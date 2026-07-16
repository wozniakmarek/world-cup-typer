import { expect, test } from '@playwright/test'
import type { Page } from '@playwright/test'
import { readSecretValue, runsAgainstLocalPreview, shouldRunRoleLoginSmoke } from './helpers/environment'

const smokeMode = readSecretValue(process.env.E2E_SMOKE_MODE, 'E2E_SMOKE_MODE')?.toLowerCase() ?? 'production'
const isStagingSmoke = smokeMode === 'staging'
const isLocalPreview = runsAgainstLocalPreview()
const runRoleLoginSmoke = shouldRunRoleLoginSmoke({ smokeMode, isLocalPreview })

async function login(page: Page, email: string, password: string) {
  await page.goto('/login')
  await page.getByLabel('Login').fill(email)
  await page.getByLabel('Hasło').fill(password)
  await page.getByRole('button', { name: 'Wejdź do aplikacji' }).click()
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

test('publiczny home pokazuje finalne podsumowanie turnieju', async ({ page }) => {
  test.skip(!isLocalPreview, 'Publiczny final summary z mockowanym API sprawdzamy lokalnie')

  await page.route('**/api/summary/final', async (route) =>
    route.fulfill({
      json: {
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
              { matchId: 'match-1', matchNumber: 1, matchLabel: 'POL-GER', snapshotAtUtc: '2026-06-11T20:00:00Z', position: 2, totalPoints: 3 },
              { matchId: 'match-2', matchNumber: 2, matchLabel: 'FRA-ESP', snapshotAtUtc: '2026-06-12T20:00:00Z', position: 1, totalPoints: 6 },
            ],
          },
          {
            userId: 'user-2',
            displayName: 'Tomek',
            avatarUrl: null,
            finalPosition: 2,
            finalPoints: 117,
            isCurrentUser: false,
            points: [
              { matchId: 'match-1', matchNumber: 1, matchLabel: 'POL-GER', snapshotAtUtc: '2026-06-11T20:00:00Z', position: 1, totalPoints: 3 },
              { matchId: 'match-2', matchNumber: 2, matchLabel: 'FRA-ESP', snapshotAtUtc: '2026-06-12T20:00:00Z', position: 2, totalPoints: 4 },
            ],
          },
        ],
        finalTop: [
          { userId: 'user-1', displayName: 'Marek', avatarUrl: null, finalPosition: 1, totalPoints: 121, exactScoreHits: 24, correctOutcomeHits: 73, predictionsCount: 104, isCurrentUser: false },
          { userId: 'user-2', displayName: 'Tomek', avatarUrl: null, finalPosition: 2, totalPoints: 117, exactScoreHits: 22, correctOutcomeHits: 71, predictionsCount: 104, isCurrentUser: false },
        ],
        globalFacts: [
          { id: 'biggest-climb', label: 'Najwiekszy skok', title: 'Marek: +7 miejsc', description: 'Najmocniejszy ruch tabeli.', relatedUserIds: ['user-1'], relatedMatchIds: [] },
          { id: 'most-exact-match', label: 'Najbardziej trafiony mecz', title: 'POL-GER: 8 dokladnych', description: 'Wspolny jackpot kolejki.', relatedUserIds: [], relatedMatchIds: ['match-1'] },
        ],
      },
    }),
  )

  await page.goto('/')

  await expect(page.getByRole('heading', { name: 'Cala tabela, mecz po meczu' })).toBeVisible()
  await expect(page.getByText('Animowana pelna tabela')).toBeVisible()
  await expect(page.getByText('Najwiekszy skok')).toBeVisible()
  await expect(page.getByRole('link', { name: 'Zaloguj sie po swoj recap' })).toHaveAttribute('href', '/login')
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
