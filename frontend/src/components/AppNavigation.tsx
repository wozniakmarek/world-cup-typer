import clsx from 'clsx'
import { LayoutDashboard, LogOut, Shield, Trophy, UserCircle2 } from 'lucide-react'
import { Link, NavLink } from 'react-router-dom'
import { useAuth } from '../features/auth/AuthContext'
import { UserAvatar } from './UserAvatar'

const commonLinks = [
  { to: '/', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/matches', label: 'Mecze', icon: Trophy },
  { to: '/ranking', label: 'Ranking', icon: Trophy },
  { to: '/profile', label: 'Profil', icon: UserCircle2 },
]

const adminLinks = [
  { to: '/admin', label: 'Admin', icon: Shield },
  { to: '/admin/players', label: 'Gracze', icon: Shield },
  { to: '/admin/teams', label: 'Drużyny', icon: Shield },
  { to: '/admin/matches', label: 'Mecze Admin', icon: Shield },
]

export const AppNavigation = () => {
  const { isAdmin, user, logout } = useAuth()
  const links = [...commonLinks, ...(isAdmin ? adminLinks : [])]

  return (
    <header className="sticky top-0 z-20 border-b border-white/5 bg-pitch-950/80 backdrop-blur-xl">
      <div className="mx-auto flex max-w-7xl flex-col gap-2 px-4 py-2.5 sm:gap-4 sm:px-6 sm:py-4 lg:px-8">
        <div className="flex items-center justify-between gap-3">
          <div className="min-w-0">
            <p className="truncate font-display text-sm uppercase tracking-[0.18em] text-white sm:text-xl sm:tracking-[0.24em]">
              <span className="sm:hidden">Typer MŚ</span>
              <span className="hidden sm:inline">Typer Mistrzostw Świata</span>
            </p>
            <p className="hidden text-sm text-slate-400 sm:block">marekwozniak.me</p>
          </div>
          <div className="flex shrink-0 items-center gap-2 sm:gap-3">
            <Link
              to="/profile"
              className="flex max-w-[10.5rem] items-center gap-2 rounded-full border border-white/10 bg-white/5 py-1 pl-1 pr-3 text-xs text-slate-200 transition hover:border-emerald-400/50 hover:text-white focus-visible:border-emerald-400/70 focus-visible:outline-none sm:max-w-none sm:pr-4 sm:text-sm"
              aria-label="Przejdź do profilu"
            >
              <UserAvatar displayName={user?.displayName ?? 'Gracz'} avatarUrl={user?.avatarUrl} size="sm" />
              <span className="truncate">{user?.displayName}</span>
            </Link>
            <button
              type="button"
              onClick={() => void logout()}
              aria-label="Wyloguj"
              className="inline-flex h-8 w-8 items-center justify-center rounded-full border border-white/10 text-slate-200 transition hover:border-emerald-400/60 hover:text-white sm:h-auto sm:w-auto sm:px-4 sm:py-2 sm:text-sm"
            >
              <LogOut className="h-4 w-4 sm:hidden" aria-hidden="true" />
              <span className="sr-only sm:not-sr-only">Wyloguj</span>
            </button>
          </div>
        </div>

        <nav className="-mx-4 flex gap-1.5 overflow-x-auto px-4 pb-0.5 sm:mx-0 sm:gap-2 sm:px-0 sm:pb-1">
          {links.map(({ to, label, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              end={to === '/'}
              className={({ isActive }) =>
                clsx(
                  'inline-flex shrink-0 items-center gap-1.5 rounded-full px-3 py-1.5 text-xs font-medium transition sm:gap-2 sm:px-4 sm:py-2 sm:text-sm',
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
