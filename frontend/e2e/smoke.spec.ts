import { expect, test } from '@playwright/test'
import type { Page } from '@playwright/test'
import { readSecretValue } from './helpers/environment'

const smokeMode = readSecretValue(process.env.E2E_SMOKE_MODE, 'E2E_SMOKE_MODE')?.toLowerCase() ?? 'production'
const isStagingSmoke = smokeMode === 'staging'

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
  await expect(page.getByRole('button', { name: 'Wejdź do aplikacji' })).toBeVisible()
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
