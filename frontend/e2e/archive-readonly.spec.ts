import { expect, test } from '@playwright/test'
import { createCipheriv, createHash, pbkdf2Sync, randomBytes } from 'node:crypto'

test('same account unlocks saved views, survives refresh, rejects writes and stays private after logout', async ({ page }) => {
  const id = '11111111-1111-4111-8111-111111111111'
  const user = { id, displayName: 'Test player', email: 'player@example.test', role: 'Player', isActive: true, requiresPasswordChange: false, avatarUrl: null }
  const ranking = { position: 1, userId: id, displayName: user.displayName, totalPoints: 24, exactScoreHits: 5, correctOutcomeHits: 9, predictionsCount: 12, isCurrentUser: true, avatarUrl: null }
  const publicResponses = { '/ranking/top': [{ ...ranking, isCurrentUser: false }], '/summary/final/availability': { isReady: false, reason: 'PendingFinal', settledMatchesCount: 1, requiredSettledMatchesCount: 104, totalMatchesCount: 104, finalMatchLabel: 'Final' } }
  const responses = { ...publicResponses, '/auth/me': user, '/ranking': [ranking], '/ranking/me': ranking, '/ranking/progress': [], '/ranking/progress/all': [], '/matches': [], '/matches/today': [], '/matches/upcoming': [], '/predictions/my': [], '/notifications/settings': { morningDigestEnabled: false, missingPrediction2hEnabled: false, missingPrediction30mEnabled: false, rankingUpdatedEnabled: false, hasActiveSubscription: false }, '/notifications/vapid-public-key': { publicKey: '' } }
  const salt = randomBytes(16)
  const key = createHash('sha256').update('world-cup-typer.archive.v1\0').update(pbkdf2Sync('ArchivePassword42!', salt, 100000, 32, 'sha512')).digest()
  const iv = randomBytes(12)
  const cipher = createCipheriv('aes-256-gcm', key, iv)
  const encrypted = Buffer.concat([cipher.update(JSON.stringify(responses)), cipher.final(), cipher.getAuthTag()])
  const kdf = { salt: salt.toString('base64'), iterations: 100000, hash: 'SHA-512', keyBytes: 32 }
  const alias = createHash('sha256').update('test player').digest('hex')
  const apiCalls: string[] = []
  page.on('request', request => { if (new URL(request.url()).pathname.startsWith('/api/')) apiCalls.push(request.url()) })
  await page.route('**/archive/manifest.json', route => route.fulfill({ json: { version: 1, publicResponses, aliases: { [alias]: id }, accounts: { [id]: kdf } } }))
  await page.route(`**/archive/accounts/${id}.json`, route => route.fulfill({ json: { kdf, iv: iv.toString('base64'), ciphertext: encrypted.toString('base64') } }))
  await page.route(/^https?:\/\/[^/]+\/api\//, route => route.abort())
  await page.goto('/login')
  await page.getByLabel('Login', { exact: true }).fill('  TEST PLAYER  ')
  await page.getByLabel('Hasło', { exact: true }).fill('wrong-password')
  await page.getByRole('button', { name: 'Wejdź do aplikacji' }).click()
  await expect(page.getByText('Nieprawidłowy login lub hasło.', { exact: true })).toBeVisible()
  await page.getByLabel('Hasło', { exact: true }).fill('ArchivePassword42!')
  await page.getByRole('button', { name: 'Wejdź do aplikacji' }).click()
  await expect(page.getByRole('button', { name: 'Wyloguj', exact: true })).toBeVisible()
  await page.goto('/ranking')
  await expect(page.getByRole('heading', { name: 'Tabela liderów' })).toBeVisible()
  await expect(page.getByText('24', { exact: true }).filter({ visible: true }).first()).toBeVisible()
  await page.goto('/profile')
  await expect(page.getByText('Test player', { exact: true }).filter({ visible: true }).first()).toBeVisible()
  await page.reload()
  await expect(page.getByRole('button', { name: 'Wyloguj', exact: true })).toBeVisible()
  const rejected = await page.evaluate(async () => {
    const { apiClient } = await import('/src/api/client.ts' /* @vite-ignore */)
    const status = async (method: 'get' | 'put', url: string) => {
      try { await apiClient.request({ method, url, data: {} }); return 200 } catch (error) { return (error as { response?: { status: number } }).response?.status }
    }
    return [await status('put', '/auth/me/avatar'), await status('get', '/admin/players')]
  })
  expect(rejected).toEqual([409, 403])
  await page.getByRole('button', { name: 'Wyloguj', exact: true }).click()
  await page.goto('/profile')
  await expect(page.getByLabel('Hasło', { exact: true })).toBeVisible()
  expect(apiCalls).toEqual([])
})
