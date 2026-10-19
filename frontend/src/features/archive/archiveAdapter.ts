import { AxiosError, AxiosHeaders } from 'axios'
import type { AxiosAdapter, InternalAxiosRequestConfig } from 'axios'
import { decryptArchive, deriveArchiveKey } from './archiveCrypto'
import type { IdentityKdf, PasswordEnvelope } from './archiveCrypto'

type Responses = Record<string, unknown>
interface Manifest { version: number; publicResponses: Responses; aliases: Record<string, string>; accounts: Record<string, IdentityKdf> }
interface Session { id: string; key: string }
const sessionKey = 'typer.archive.session.v1'

export const createArchiveAdapter = (): AxiosAdapter => {
  let manifestPromise: Promise<Manifest> | undefined
  let unlocked: { id: string; responses: Responses } | undefined
  const readFile = async <T,>(path: string): Promise<T> => {
    const response = await fetch(`${import.meta.env.BASE_URL}archive/${path}`)
    if (!response.ok) throw new Error('Nie udało się pobrać archiwum. Odśwież stronę i spróbuj ponownie.')
    return response.json() as Promise<T>
  }
  const manifest = () => manifestPromise ??= readFile<Manifest>('manifest.json').then(value => {
    if (value.version !== 1) throw new Error('Nieobsługiwana wersja archiwum.')
    return value
  }).catch(error => { manifestPromise = undefined; throw error })
  const account = (id: string) => {
    if (!/^[a-f0-9-]{36}$/.test(id)) throw new Error('Nieprawidłowe konto archiwum.')
    return readFile<PasswordEnvelope>(`accounts/${id}.json`)
  }
  const restoreSession = async () => {
    const raw = sessionStorage.getItem(sessionKey)
    if (!raw) { unlocked = undefined; return undefined }
    try {
      const session = JSON.parse(raw) as Session
      if (!unlocked || unlocked.id !== session.id) unlocked = { id: session.id, responses: await decryptArchive(session.key, await account(session.id)) }
      return unlocked
    } catch { sessionStorage.removeItem(sessionKey); unlocked = undefined; return undefined }
  }
  const reject = (config: InternalAxiosRequestConfig, status: number, message: string): never => {
    throw new AxiosError(message, status === 401 || status === 403 ? 'ERR_BAD_REQUEST' : 'ERR_ARCHIVE_READ_ONLY', config, undefined,
      { status, statusText: message, data: { message }, headers: new AxiosHeaders(), config })
  }
  return async config => {
    const path = new URL(config.url ?? '/', window.location.origin).pathname.replace(/^\/api(?=\/)/, '').replace(/\/$/, '') || '/'
    const method = (config.method ?? 'get').toLowerCase()
    let data: unknown
    if (method === 'post' && path === '/auth/logout') {
      sessionStorage.removeItem(sessionKey); unlocked = undefined; data = null
    } else if (method === 'post' && path === '/auth/login') {
      sessionStorage.removeItem(sessionKey); unlocked = undefined
      const body = (typeof config.data === 'string' ? JSON.parse(config.data) : config.data) as { login?: string; password?: string }
      if (!body?.login?.trim() || !body?.password) reject(config, 401, 'Nieprawidłowy login lub hasło.')
      const archive = await manifest()
      const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(body.login!.trim().toLowerCase()))
      const alias = [...new Uint8Array(digest)].map(byte => byte.toString(16).padStart(2, '0')).join('')
      const id = archive.aliases[alias]
      if (!id || !archive.accounts[id]) reject(config, 401, 'Nieprawidłowy login lub hasło.')
      const envelope = await account(id)
      let responses: Responses
      let key: string
      try {
        key = await deriveArchiveKey(body.password!, archive.accounts[id])
        responses = await decryptArchive(key, envelope)
      } catch { return reject(config, 401, 'Nieprawidłowy login lub hasło.') }
      sessionStorage.setItem(sessionKey, JSON.stringify({ id, key }))
      unlocked = { id, responses }
      data = { token: `archive:${id}`, user: responses['/auth/me'] }
    } else if (method !== 'get') {
      return reject(config, 409, 'Turniej jest zakończony. Archiwum służy do przeglądania; zmiany są wyłączone.')
    } else {
      const archive = await manifest()
      const session = await restoreSession()
      const responses = session?.responses ?? archive.publicResponses
      if (Object.hasOwn(responses, path)) data = responses[path]
      else if (!session) return reject(config, 401, 'Zaloguj się, aby otworzyć swoje archiwum.')
      else return reject(config, path.startsWith('/admin/') ? 403 : 404, 'Ten widok nie jest dostępny dla tego konta w archiwum.')
    }
    return { status: 200, statusText: 'OK', data: structuredClone(data), headers: new AxiosHeaders(), config }
  }
}
