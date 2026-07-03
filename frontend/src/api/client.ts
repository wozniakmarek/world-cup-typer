import axios from 'axios'

const normalizeBaseUrl = (value?: string) => {
  const raw = value?.trim() || 'http://localhost:5000'
  return raw.endsWith('/api') ? raw : `${raw.replace(/\/$/, '')}/api`
}

const primaryBaseUrl = normalizeBaseUrl(import.meta.env.VITE_API_BASE_URL)
const fallbackBaseUrl = import.meta.env.VITE_API_FALLBACK_BASE_URL
  ? normalizeBaseUrl(import.meta.env.VITE_API_FALLBACK_BASE_URL)
  : undefined

type RetriableConfig = {
  _usedFallbackBaseUrl?: boolean
}

export const apiClient = axios.create({
  baseURL: primaryBaseUrl,
})

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('typer.auth.token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (!axios.isAxiosError(error)) {
      throw error
    }

    const requestConfig = error.config as (typeof error.config & RetriableConfig) | undefined
    const shouldRetryWithFallback = Boolean(
      fallbackBaseUrl &&
      requestConfig &&
      !error.response &&
      error.code === 'ERR_NETWORK' &&
      !requestConfig._usedFallbackBaseUrl &&
      (requestConfig.baseURL ?? primaryBaseUrl) !== fallbackBaseUrl,
    )

    if (!shouldRetryWithFallback || !requestConfig || !fallbackBaseUrl) {
      throw error
    }

    const retryConfig = {
      ...requestConfig,
      baseURL: fallbackBaseUrl,
      _usedFallbackBaseUrl: true,
    }

    return apiClient.request(retryConfig)
  },
)

export const getErrorMessage = (error: unknown) => {
  if (axios.isAxiosError<{ message?: string }>(error)) {
    return error.response?.data?.message || error.message
  }

  if (error instanceof Error) {
    return error.message
  }

  return 'Wyst\u0105pi\u0142 nieoczekiwany b\u0142\u0105d.'
}

export const isAuthenticationFailure = (error: unknown) => {
  return axios.isAxiosError(error) && (error.response?.status === 401 || error.response?.status === 403)
}
