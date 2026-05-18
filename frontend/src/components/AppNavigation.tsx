import clsx from 'clsx'
import { LayoutDashboard, Shield, Trophy, UserCircle2 } from 'lucide-react'
import { NavLink } from 'react-router-dom'
import { useAuth } from '../features/auth/AuthContext'

const commonLinks = [
  { to: '/', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/matches', label: 'Mecze', icon: Trophy },
  { to: '/ranking', label: 'Ranking', icon: Trophy },
  { to: '/profile', label: 'Profil', icon: UserCircle2 },
]

const adminLinks = [
  { to: '/admin', label: 'Admin', icon: Shield },
  { to: '/admin/players', label: 'Gracze', icon: Shield },
  { to: '/admin/teams', label: 'Druzyny', icon: Shield },
  { to: '/admin/matches', label: 'Mecze Admin', icon: Shield },
]

export const AppNavigation = () => {
  const { isAdmin, user, logout } = useAuth()
  const links = [...commonLinks, ...(isAdmin ? adminLinks : [])]

  return (
    <header className="sticky top-0 z-20 border-b border-white/5 bg-pitch-950/80 backdrop-blur-xl">
      <div className="mx-auto flex max-w-7xl flex-col gap-4 px-4 py-4 sm:px-6 lg:px-8">
        <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <p className="font-display text-xl uppercase tracking-[0.24em] text-white">Typer Mistrzostw Swiata</p>
            <p className="text-sm text-slate-400">marekwozniak.me</p>
          </div>
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-end">
            <div className="rounded-full border border-white/10 bg-white/5 px-4 py-2 text-sm text-slate-200">
              {user?.displayName}
            </div>
            <button
              type="button"
              onClick={() => void logout()}
              className="rounded-full border border-white/10 px-4 py-2 text-sm text-slate-200 transition hover:border-emerald-400/60 hover:text-white"
            >
              Wyloguj
            </button>
          </div>
        </div>

        <nav className="flex gap-2 overflow-x-auto pb-1">
          {links.map(({ to, label, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              end={to === '/'}
              className={({ isActive }) =>
                clsx(
                  'inline-flex shrink-0 items-center gap-2 rounded-full px-4 py-2 text-sm font-medium transition',
                  isActive
                    ? 'bg-emerald-400 text-slate-950'
                    : 'bg-white/5 text-slate-300 hover:bg-white/10 hover:text-white',
                )
              }
            >
              <Icon className="h-4 w-4" />
              {label}
            </NavLink>
          ))}
        </nav>
      </div>
    </header>
  )
}
