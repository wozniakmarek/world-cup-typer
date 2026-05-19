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

  return /^[A-Za-z_][A-Za-z0-9_]*$/.test(candidateKey) ? undefined : trimmed
}

export function normalizeBaseUrl(rawBaseUrl: string): string {
  return /^https?:\/\//i.test(rawBaseUrl) ? rawBaseUrl : `https://${rawBaseUrl}`
}
