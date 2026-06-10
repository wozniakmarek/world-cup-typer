import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AppShell } from '../app/AppShell'
import { ProtectedRoute } from '../app/ProtectedRoute'
import { AdminDashboardPage } from '../features/admin/AdminDashboardPage'
import { AdminMatchesPage } from '../features/admin/AdminMatchesPage'
import { AdminPlayersPage } from '../features/admin/AdminPlayersPage'
import { AdminTeamsPage } from '../features/admin/AdminTeamsPage'
import { ChangePasswordPage } from '../features/auth/ChangePasswordPage'
import { useAuth } from '../features/auth/AuthContext'
import { DashboardPage } from '../features/dashboard/DashboardPage'
import { LoginPage } from '../features/auth/LoginPage'
import { MatchDetailsPage } from '../features/matches/MatchDetailsPage'
import { MatchesPage } from '../features/matches/MatchesPage'
import { ProfilePage } from '../features/profile/ProfilePage'
import { PublicHomePage } from '../features/public/PublicHomePage'
import { RankingPage } from '../features/ranking/RankingPage'

const RootRoute = () => {
  const { isAuthenticated, isInitializing, requiresPasswordChange } = useAuth()

  if (isInitializing) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-pitch-950 text-slate-100">
        Ładowanie aplikacji...
      </div>
    )
  }

  if (!isAuthenticated) {
    return <PublicHomePage />
  }

  if (requiresPasswordChange) {
    return <Navigate to="/change-password" replace />
  }

  return <AppShell />
}

export const AppRouter = () => {
  return (
    <BrowserRouter basename={import.meta.env.BASE_URL}>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/" element={<RootRoute />}>
          <Route index element={<DashboardPage />} />
        </Route>
        <Route
          path="/change-password"
          element={
            <ProtectedRoute allowPasswordChange>
              <ChangePasswordPage />
            </ProtectedRoute>
          }
        />
        <Route
          element={
            <ProtectedRoute>
              <AppShell />
            </ProtectedRoute>
          }
        >
          <Route path="/matches" element={<MatchesPage />} />
          <Route path="/matches/:matchId" element={<MatchDetailsPage />} />
          <Route path="/ranking" element={<RankingPage />} />
          <Route path="/profile" element={<ProfilePage />} />
        </Route>
        <Route
          element={
            <ProtectedRoute requireAdmin>
              <AppShell />
            </ProtectedRoute>
          }
        >
          <Route path="/admin" element={<AdminDashboardPage />} />
          <Route path="/admin/players" element={<AdminPlayersPage />} />
          <Route path="/admin/teams" element={<AdminTeamsPage />} />
          <Route path="/admin/matches" element={<AdminMatchesPage />} />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
