import { Navigate } from 'react-router-dom'
import { useAuth } from '../features/auth/AuthContext'

export const ProtectedRoute = ({
  children,
  requireAdmin = false,
}: {
  children: React.ReactNode
  requireAdmin?: boolean
}) => {
  const { isAuthenticated, isAdmin, isInitializing } = useAuth()

  if (isInitializing) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-pitch-950 text-slate-100">
        Ładowanie aplikacji...
      </div>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  if (requireAdmin && !isAdmin) {
    return <Navigate to="/" replace />
  }

  return <>{children}</>
}
