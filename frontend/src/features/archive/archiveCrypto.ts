export interface IdentityKdf { salt: string; iterations: number; hash: string; keyBytes: number }
export interface PasswordEnvelope { kdf: IdentityKdf; iv: string; ciphertext: string }
const decode = (value: string) => Uint8Array.from(atob(value), character => character.charCodeAt(0))
const encode = (value: ArrayBuffer) => btoa(String.fromCharCode(...new Uint8Array(value)))

export const deriveArchiveKey = async (password: string, kdf: IdentityKdf): Promise<string> => {
  if (!['SHA-1', 'SHA-256', 'SHA-512'].includes(kdf.hash) || !Number.isInteger(kdf.iterations) || kdf.iterations < 1000 || kdf.iterations > 1000000 || !Number.isInteger(kdf.keyBytes) || kdf.keyBytes < 16 || kdf.keyBytes > 64) throw new Error('Nieprawidłowe parametry archiwum.')
  const material = await crypto.subtle.importKey('raw', new TextEncoder().encode(password), 'PBKDF2', false, ['deriveBits'])
  const subkey = new Uint8Array(await crypto.subtle.deriveBits({ name: 'PBKDF2', salt: decode(kdf.salt), iterations: kdf.iterations, hash: kdf.hash }, material, kdf.keyBytes * 8))
  const prefix = new TextEncoder().encode('world-cup-typer.archive.v1\0')
  const input = new Uint8Array(prefix.length + subkey.length)
  input.set(prefix); input.set(subkey, prefix.length)
  try { return encode(await crypto.subtle.digest('SHA-256', input)) }
  finally { subkey.fill(0); input.fill(0) }
}

export const decryptArchive = async (key: string, envelope: PasswordEnvelope): Promise<Record<string, unknown>> => {
  const imported = await crypto.subtle.importKey('raw', decode(key), 'AES-GCM', false, ['decrypt'])
  const plaintext = await crypto.subtle.decrypt({ name: 'AES-GCM', iv: decode(envelope.iv) }, imported, decode(envelope.ciphertext))
  return JSON.parse(new TextDecoder().decode(plaintext)) as Record<string, unknown>
}
