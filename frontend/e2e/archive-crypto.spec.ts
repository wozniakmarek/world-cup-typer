import { test, expect } from '@playwright/test'
import { createCipheriv, createHash, pbkdf2Sync, randomBytes } from 'node:crypto'
import { deriveArchiveKey, decryptArchive } from '../src/features/archive/archiveCrypto'

test('archive accepts the Identity PBKDF2 password and rejects wrong passwords and tampering', async () => {
  const password = 'SyntheticPassword-42!'
  const salt = randomBytes(16)
  const subkey = pbkdf2Sync(password, salt, 100000, 32, 'sha512')
  const key = createHash('sha256').update('world-cup-typer.archive.v1\0').update(subkey).digest()
  const iv = randomBytes(12)
  const cipher = createCipheriv('aes-256-gcm', key, iv)
  const ciphertext = Buffer.concat([cipher.update(JSON.stringify({ '/auth/me': { displayName: 'Test player' } })), cipher.final(), cipher.getAuthTag()])
  const envelope = { kdf: { salt: salt.toString('base64'), iterations: 100000, hash: 'SHA-512', keyBytes: 32 }, iv: iv.toString('base64'), ciphertext: ciphertext.toString('base64') }
  expect(await deriveArchiveKey(password, envelope.kdf)).toBe(key.toString('base64'))
  expect(await decryptArchive(await deriveArchiveKey(password, envelope.kdf), envelope)).toEqual({ '/auth/me': { displayName: 'Test player' } })
  await expect(decryptArchive(await deriveArchiveKey('wrong', envelope.kdf), envelope)).rejects.toThrow()
  ciphertext[0] ^= 1
  await expect(decryptArchive(key.toString('base64'), { ...envelope, ciphertext: ciphertext.toString('base64') })).rejects.toThrow()
})
