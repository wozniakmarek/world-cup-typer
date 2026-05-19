import { expect, test } from '@playwright/test'
import { runsAgainstLocalPreview } from './environment'

test('runsAgainstLocalPreview handles address without scheme', () => {
  expect(runsAgainstLocalPreview('127.0.0.1:4173')).toBeTruthy()
})

test('runsAgainstLocalPreview handles KEY=VALUE format', () => {
  expect(runsAgainstLocalPreview('E2E_BASE_URL=127.0.0.1:4173')).toBeTruthy()
})
