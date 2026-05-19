import { expect, test } from '@playwright/test'

test('strona logowania ładuje się poprawnie', async ({ page }) => {
  await page.goto('/login')
  await expect(page.getByText('Logowanie')).toBeVisible()
  await expect(page.getByText('Zaloguj się mailem albo nazwą gracza.')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Wejdź do aplikacji' })).toBeVisible()
})

const roles = [
  {
    name: 'admin',
    email: process.env.E2E_ADMIN_EMAIL,
    password: process.env.E2E_ADMIN_PASSWORD,
  },
  {
    name: 'player',
    email: process.env.E2E_PLAYER_EMAIL,
    password: process.env.E2E_PLAYER_PASSWORD,
  },
]

for (const role of roles) {
  test(`logowanie smoke (${role.name})`, async ({ page }) => {
    test.skip(!role.email || !role.password, `Brak danych logowania dla roli: ${role.name}`)

    await page.goto('/login')
    await page.getByLabel('Login').fill(role.email!)
    await page.getByLabel('Hasło').fill(role.password!)
    await page.getByRole('button', { name: 'Wejdź do aplikacji' }).click()

    await expect(page.getByRole('button', { name: 'Wyloguj' })).toBeVisible({ timeout: 15_000 })
  })
}
