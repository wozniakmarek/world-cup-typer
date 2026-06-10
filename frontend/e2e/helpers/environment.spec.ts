import { expect, test } from '@playwright/test'
import { normalizeBaseUrl, readSecretValue, runsAgainstLocalPreview, shouldRunRoleLoginSmoke } from './environment'

test('readSecretValue handles raw values and KEY=VALUE formatted secrets', () => {
  expect(readSecretValue('secret-value', 'E2E_BASE_URL')).toBe('secret-value')
  expect(readSecretValue('E2E_BASE_URL=127.0.0.1:4173', 'E2E_BASE_URL')).toBe('127.0.0.1:4173')
  expect(readSecretValue('OTHER_SECRET=value', 'E2E_BASE_URL')).toBeUndefined()
})

test('normalizeBaseUrl preserves URLs with schemes and adds http to host-only values', () => {
  expect(normalizeBaseUrl('https://example.com')).toBe('https://example.com')
  expect(normalizeBaseUrl('127.0.0.1:4173')).toBe('http://127.0.0.1:4173')
})

test('runsAgainstLocalPreview detects local preview with host-only and KEY=VALUE base URLs', () => {
  expect(runsAgainstLocalPreview('127.0.0.1:4173')).toBeTruthy()
  expect(runsAgainstLocalPreview('E2E_BASE_URL=localhost:4173')).toBeTruthy()
  expect(runsAgainstLocalPreview('https://example.com')).toBeFalsy()
})

test('shouldRunRoleLoginSmoke keeps production smoke light', () => {
  expect(shouldRunRoleLoginSmoke({ smokeMode: 'production', isLocalPreview: false })).toBeFalsy()
  expect(shouldRunRoleLoginSmoke({ smokeMode: 'staging', isLocalPreview: false })).toBeTruthy()
  expect(shouldRunRoleLoginSmoke({ smokeMode: 'staging', isLocalPreview: true })).toBeFalsy()
})
