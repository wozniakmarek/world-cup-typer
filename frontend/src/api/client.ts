import axios from 'axios'

const normalizeBaseUrl = (value?: string) => {
  const raw = value?.trim() || 'http://localhost:5000'
  return raw.endsWith('/api') ? raw : `${raw.replace(/\/$/, '')}/api`
}

export const apiClient = axios.create({
  baseURL: normalizeBaseUrl(import.meta.env.VITE_API_BASE_URL),
})

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('typer.auth.token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})

export const getErrorMessage = (error: unknown) => {
  if (axios.isAxiosError<{ message?: string }>(error)) {
    return error.response?.data?.message || error.message
  }

  if (error instanceof Error) {
    return error.message
  }

  return 'Wystąpił nieoczekiwany błąd.'
}
