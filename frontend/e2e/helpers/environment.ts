const DEFAULT_LOCAL_PREVIEW_BASE_URL = 'http://127.0.0.1:4173'
const ENV_KEY_PATTERN = /^[A-Za-z_][A-Za-z0-9_]*$/

export function readSecretValue(value: string | undefined, key: string): string | undefined {
  const trimmed = value?.trim()

  if (!trimmed) {
    return undefined
  }

  const separatorIndex = trimmed.indexOf('=')
  if (separatorIndex <= 0) {
    return trimmed
  }

  const candidateKey = trimmed.slice(0, separatorIndex).trim()
  const candidateValue = trimmed.slice(separatorIndex + 1).trim()

  if (candidateKey.toLowerCase() === key.toLowerCase()) {
    return candidateValue
  }

  return ENV_KEY_PATTERN.test(candidateKey) ? undefined : trimmed
}

export function normalizeBaseUrl(rawBaseUrl: string): string {
  if (/^https?:\/\//i.test(rawBaseUrl)) {
    return rawBaseUrl
  }

  const host = rawBaseUrl.split('/')[0]?.split(':')[0]?.toLowerCase()
  const scheme = host === 'localhost' || host === '127.0.0.1' || host === '::1' ? 'http' : 'https'

  return `${scheme}://${rawBaseUrl}`
}

export function runsAgainstLocalPreview(baseUrlValue = process.env.E2E_BASE_URL): boolean {
  const rawBaseUrl = readSecretValue(baseUrlValue, 'E2E_BASE_URL') ?? DEFAULT_LOCAL_PREVIEW_BASE_URL

  try {
    const { hostname } = new URL(normalizeBaseUrl(rawBaseUrl))
    return hostname === 'localhost' || hostname === '127.0.0.1' || hostname === '::1'
  } catch {
    return false
  }
}
