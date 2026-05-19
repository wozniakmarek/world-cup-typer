import { createContext, useContext, useEffect, useMemo, useState } from 'react'
import { authApi } from '../../api/services'
import { getErrorMessage } from '../../api/client'
import type { CurrentUser } from '../../api/types'

interface AuthContextValue {
  token: string | null
  user: CurrentUser | null
  isAuthenticated: boolean
  isAdmin: boolean
  isInitializing: boolean
  login: (login: string, password: string) => Promise<void>
  logout: () => Promise<void>
  updateAvatar: (avatarUrl?: string | null) => Promise<CurrentUser>
}

const TOKEN_KEY = 'typer.auth.token'
const USER_KEY = 'typer.auth.user'

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem(TOKEN_KEY))
  const [user, setUser] = useState<CurrentUser | null>(() => {
    const raw = localStorage.getItem(USER_KEY)
    return raw ? (JSON.parse(raw) as CurrentUser) : null
  })
  const [isInitializing, setIsInitializing] = useState(Boolean(token))

  useEffect(() => {
    if (!token) {
      setIsInitializing(false)
      return
    }

    authApi
      .me()
      .then((currentUser) => {
        setUser(currentUser)
        localStorage.setItem(USER_KEY, JSON.stringify(currentUser))
      })
      .catch(() => {
        localStorage.removeItem(TOKEN_KEY)
        localStorage.removeItem(USER_KEY)
        setToken(null)
        setUser(null)
      })
      .finally(() => {
        setIsInitializing(false)
      })
  }, [token])

  const value = useMemo<AuthContextValue>(
    () => ({
      token,
      user,
      isAuthenticated: Boolean(token && user),
      isAdmin: user?.role === 'Admin',
      isInitializing,
      login: async (loginValue, password) => {
        const response = await authApi.login({ login: loginValue, password })
        setToken(response.token)
        setUser(response.user)
        localStorage.setItem(TOKEN_KEY, response.token)
        localStorage.setItem(USER_KEY, JSON.stringify(response.user))
      },
      updateAvatar: async (avatarUrl) => {
        const currentUser = await authApi.updateAvatar({ avatarUrl })
        setUser(currentUser)
        localStorage.setItem(USER_KEY, JSON.stringify(currentUser))

        return currentUser
      },
      logout: async () => {
        try {
          await authApi.logout()
        } catch (error) {
          console.warn(getErrorMessage(error))
        } finally {
          localStorage.removeItem(TOKEN_KEY)
          localStorage.removeItem(USER_KEY)
          setToken(null)
          setUser(null)
        }
      },
    }),
    [isInitializing, token, user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export const useAuth = () => {
  const context = useContext(AuthContext)

  if (!context) {
    throw new Error('useAuth must be used within AuthProvider')
  }

  return context
}
