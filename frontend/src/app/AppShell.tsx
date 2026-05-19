import clsx from 'clsx'
import { Outlet } from 'react-router-dom'
import { AppNavigation } from '../components/AppNavigation'
import { PageShell } from '../components/PageShell'
import { useAuth } from '../features/auth/AuthContext'
import { PwaInstallPrompt } from '../features/pwa/PwaInstallPrompt'

export const AppShell = () => {
  const { isAdmin } = useAuth()

  return (
    <div className={clsx('min-h-screen sm:pb-0', isAdmin ? 'pb-40' : 'pb-24')}>
      <AppNavigation />
      <PwaInstallPrompt />
      <PageShell>
        <Outlet />
      </PageShell>
    </div>
  )
}
