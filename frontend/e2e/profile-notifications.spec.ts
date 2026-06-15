import { expect, test } from '@playwright/test'

const signIn = async (page: Parameters<Parameters<typeof test>[1]>[0]['page']) => {
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
}

const mockProfileApis = async (page: Parameters<Parameters<typeof test>[1]>[0]['page']) => {
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
}

const mockNotificationSettings = async (page: Parameters<Parameters<typeof test>[1]>[0]['page']) => {
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

  return {
    getSavedSettings: () => savedSettings,
  }
}

const mockPushSupportWithoutCurrentSubscription = async (page: Parameters<Parameters<typeof test>[1]>[0]['page']) => {
  await page.addInitScript(() => {
    Object.defineProperty(window, 'Notification', {
      value: class Notification {
        static permission = 'granted'
        static requestPermission = async () => 'granted'
      },
      configurable: true,
    })

    const registration = {
      pushManager: {
        getSubscription: async () => null,
      },
    }

    Object.defineProperty(window, 'PushManager', { value: function PushManager() {}, configurable: true })
    Object.defineProperty(navigator, 'serviceWorker', {
      value: {
        getRegistration: async () => registration,
        ready: Promise.resolve(registration),
      },
      configurable: true,
    })
  })
}

test('profil pozwala wlaczyc i zmienic ustawienia powiadomien', async ({ page }) => {
  await page.context().grantPermissions(['notifications'], {
    origin: process.env.E2E_BASE_URL ?? 'http://127.0.0.1:4173',
  })

  await signIn(page)
  await mockProfileApis(page)
  await page.route('**/api/notifications/vapid-public-key', async (route) =>
    route.fulfill({ json: { publicKey: 'test-public-key' } }),
  )
  const notificationSettings = await mockNotificationSettings(page)

  await page.goto('/profile')

  await expect(page.getByRole('heading', { name: 'Powiadomienia' })).toBeVisible()
  await expect(page.getByText('Ustawienia dotycza konta')).toBeVisible()
  await page.getByLabel('30 min przed meczem bez typu').click()
  await page.getByRole('button', { name: 'Zapisz powiadomienia' }).click()

  await expect.poll(notificationSettings.getSavedSettings).toMatchObject({
    morningDigestEnabled: true,
    missingPrediction2hEnabled: true,
    missingPrediction30mEnabled: false,
    rankingUpdatedEnabled: true,
  })
})

test('iPhone w Safari dostaje instrukcje instalacji PWA zamiast braku wsparcia', async ({ page }) => {
  await page.addInitScript(() => {
    Object.defineProperty(navigator, 'userAgent', {
      value: 'Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1',
      configurable: true,
    })
    delete (window as Window & { PushManager?: unknown }).PushManager
  })

  await signIn(page)
  await mockProfileApis(page)
  await mockNotificationSettings(page)

  await page.goto('/profile')

  await expect(page.getByText('Dodaj aplikacje do ekranu poczatkowego')).toBeVisible()
  await expect(page.getByText('Na iPhonie powiadomienia dzialaja po otwarciu aplikacji z ikony na ekranie poczatkowym.')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Wlacz na tym urzadzeniu' })).toBeDisabled()
})

test('konto z aktywna subskrypcja na innym urzadzeniu nadal pozwala wlaczyc obecna przegladarke', async ({ page }) => {
  await mockPushSupportWithoutCurrentSubscription(page)
  await signIn(page)
  await mockProfileApis(page)
  await page.route('**/api/notifications/settings', async (route) => {
    await route.fulfill({
      json: {
        morningDigestEnabled: true,
        missingPrediction2hEnabled: true,
        missingPrediction30mEnabled: true,
        rankingUpdatedEnabled: true,
        hasActiveSubscription: true,
      },
    })
  })

  await page.goto('/profile')

  await expect(page.getByText('Aktywne na innym urzadzeniu')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Wlacz na tym urzadzeniu' })).toBeEnabled()
})

test('wlaczenie powiadomien ignoruje niewidoczny BOM w kluczu VAPID', async ({ page }) => {
  await page.context().grantPermissions(['notifications'], {
    origin: process.env.E2E_BASE_URL ?? 'http://127.0.0.1:4173',
  })
  await page.addInitScript(() => {
    Object.defineProperty(window, 'Notification', {
      value: class Notification {
        static permission = 'default'
        static requestPermission = async () => 'granted'
      },
      configurable: true,
    })
    const subscription = {
      endpoint: 'https://push.example/current-device',
      toJSON: () => ({
        endpoint: 'https://push.example/current-device',
        keys: {
          p256dh: 'p256dh-key',
          auth: 'auth-key',
        },
      }),
    }
    const registration = {
      pushManager: {
        getSubscription: async () => null,
        subscribe: async (options: PushSubscriptionOptionsInit) => {
          window.localStorage.setItem('typer.push.keyLength', String(options.applicationServerKey?.byteLength ?? 0))
          return subscription
        },
      },
    }

    Object.defineProperty(window, 'PushManager', { value: function PushManager() {}, configurable: true })
    Object.defineProperty(navigator, 'serviceWorker', {
      value: {
        register: async () => registration,
        ready: Promise.resolve(registration),
        getRegistration: async () => registration,
      },
      configurable: true,
    })
  })

  await signIn(page)
  await mockProfileApis(page)
  await mockNotificationSettings(page)
  await page.route('**/api/notifications/vapid-public-key', async (route) =>
    route.fulfill({
      json: {
        publicKey: '\uFEFFBC7iL_awOuAHTawclVSrRpA7mraA5lIL4V5kpg7VE2FeRQ-B01x3B_mI3X5nM-dQcNtX6h-8HNX4MaSMHo1MNNc',
      },
    }),
  )
  await page.route('**/api/notifications/subscriptions', async (route) => route.fulfill({ status: 204 }))

  await page.goto('/profile')
  await page.getByRole('button', { name: 'Wlacz na tym urzadzeniu' }).click()

  await expect(page.getByText('Powiadomienia zostaly wlaczone na tym urzadzeniu.')).toBeVisible()
  await expect.poll(() => page.evaluate(() => window.localStorage.getItem('typer.push.keyLength'))).toBe('65')
})
