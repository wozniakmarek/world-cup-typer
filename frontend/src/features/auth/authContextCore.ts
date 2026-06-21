import { createContext } from 'react'
import type { CurrentUser } from '../../api/types'

export interface AuthContextValue {
  token: string | null
  user: CurrentUser | null
  isAuthenticated: boolean
  isAdmin: boolean
  requiresPasswordChange: boolean
  isInitializing: boolean
  login: (login: string, password: string) => Promise<void>
  changePassword: (currentPassword: string, newPassword: string) => Promise<CurrentUser>
  logout: () => Promise<void>
  updateAvatar: (avatarUrl?: string | null) => Promise<CurrentUser>
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)
