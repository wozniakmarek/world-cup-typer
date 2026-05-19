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

  // Ignore KEY=VALUE pairs for other variables, but preserve URL-like inputs containing '='.
  return /^[A-Za-z_][A-Za-z0-9_]*$/.test(candidateKey) ? undefined : trimmed
}

export function normalizeBaseUrl(rawBaseUrl: string): string {
  return /^https?:\/\//i.test(rawBaseUrl) ? rawBaseUrl : `https://${rawBaseUrl}`
}

export function runsAgainstLocalPreview(baseUrlValue = process.env.E2E_BASE_URL): boolean {
  const rawBaseUrl = readSecretValue(baseUrlValue, 'E2E_BASE_URL') ?? 'http://127.0.0.1:4173'

  try {
    const { hostname } = new URL(normalizeBaseUrl(rawBaseUrl))
    return hostname === 'localhost' || hostname === '127.0.0.1' || hostname === '::1'
  } catch {
    return false
  }
}
