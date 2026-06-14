import { expect, test } from '@playwright/test'

test('profil pozwala wlaczyc i zmienic ustawienia powiadomien', async ({ page }) => {
  await page.context().grantPermissions(['notifications'], {
    origin: process.env.E2E_BASE_URL ?? 'http://127.0.0.1:4173',
  })

  await page.addInitScript(() => {
    window.localStorage.setItem('typer.auth.token', 'test-token')
    window.localStorage.setItem(
      'typer.auth.user',
      JSON.stringify({
        id: 'user-1',
        email: 'marek@test.local',
        displayName: 'Marek',
        role: 'Player',
        isActive: true,
        requiresPasswordChange: false,
        avatarUrl: null,
      }),
    )
  })

  await page.route('**/api/auth/me', async (route) =>
    route.fulfill({
      json: {
        id: 'user-1',
        email: 'marek@test.local',
        displayName: 'Marek',
        role: 'Player',
        isActive: true,
        requiresPasswordChange: false,
        avatarUrl: null,
      },
    }),
  )
  await page.route('**/api/ranking/me', async (route) =>
    route.fulfill({
      json: {
        position: 1,
        userId: 'user-1',
        displayName: 'Marek',
        totalPoints: 12,
        exactScoreHits: 3,
        correctOutcomeHits: 4,
        predictionsCount: 5,
        avatarUrl: null,
        isCurrentUser: true,
      },
    }),
  )
  await page.route('**/api/ranking/progress', async (route) => route.fulfill({ json: [] }))
  await page.route('**/api/predictions/my', async (route) => route.fulfill({ json: [] }))
  await page.route('**/api/notifications/vapid-public-key', async (route) =>
    route.fulfill({ json: { publicKey: 'test-public-key' } }),
  )

  let savedSettings: Record<string, boolean> | undefined
  await page.route('**/api/notifications/settings', async (route) => {
    if (route.request().method() === 'PUT') {
      savedSettings = route.request().postDataJSON() as Record<string, boolean>
      await route.fulfill({
        json: {
          ...savedSettings,
          hasActiveSubscription: false,
        },
      })
      return
    }

    await route.fulfill({
      json: {
        morningDigestEnabled: true,
        missingPrediction2hEnabled: true,
        missingPrediction30mEnabled: true,
        rankingUpdatedEnabled: true,
        hasActiveSubscription: false,
      },
    })
  })

  await page.goto('/profile')

  await expect(page.getByRole('heading', { name: 'Powiadomienia' })).toBeVisible()
  await expect(page.getByText('Ustawienia dotycza konta')).toBeVisible()
  await page.getByLabel('30 min przed meczem bez typu').click()
  await page.getByRole('button', { name: 'Zapisz powiadomienia' }).click()

  await expect.poll(() => savedSettings).toMatchObject({
    morningDigestEnabled: true,
    missingPrediction2hEnabled: true,
    missingPrediction30mEnabled: false,
    rankingUpdatedEnabled: true,
  })
})
