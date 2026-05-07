import { Outlet } from 'react-router-dom'
import { AppNavigation } from '../components/AppNavigation'
import { PageShell } from '../components/PageShell'
import { PwaInstallPrompt } from '../features/pwa/PwaInstallPrompt'

export const AppShell = () => {
  return (
    <div className="min-h-screen">
      <AppNavigation />
      <PwaInstallPrompt />
      <PageShell>
        <Outlet />
      </PageShell>
    </div>
  )
}
