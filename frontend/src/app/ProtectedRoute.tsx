import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../features/auth/useAuth'

export const ProtectedRoute = ({
  children,
  requireAdmin = false,
  allowPasswordChange = false,
}: {
  children: React.ReactNode
  requireAdmin?: boolean
  allowPasswordChange?: boolean
}) => {
  const { isAuthenticated, isAdmin, isInitializing, requiresPasswordChange } = useAuth()
  const location = useLocation()

  if (isInitializing) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-pitch-950 text-slate-100">
        Ładowanie aplikacji...
      </div>
    )
  }

  if (!isAuthenticated) {
    const returnTo = `${location.pathname}${location.search}${location.hash}`
    const loginPath = returnTo === '/' ? '/login' : `/login?returnTo=${encodeURIComponent(returnTo)}`

    return <Navigate to={loginPath} replace />
  }

  if (requiresPasswordChange && !allowPasswordChange) {
    return <Navigate to="/change-password" replace />
  }

  if (allowPasswordChange && !requiresPasswordChange) {
    return <Navigate to="/" replace />
  }

  if (requireAdmin && !isAdmin) {
    return <Navigate to="/" replace />
  }

  return <>{children}</>
}
