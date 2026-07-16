import clsx from 'clsx'
import { CalendarDays, LayoutDashboard, LogOut, Shield, Sparkles, Trophy, UserCircle2, UsersRound } from 'lucide-react'
import { Link, NavLink } from 'react-router-dom'
import { useAuth } from '../features/auth/useAuth'
import { UserAvatar } from './UserAvatar'

const commonLinks = [
  { to: '/', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/summary/final/me', label: 'Mój recap', icon: Sparkles },
  { to: '/matches', label: 'Mecze', icon: CalendarDays },
  { to: '/ranking', label: 'Ranking', icon: Trophy },
  { to: '/profile', label: 'Profil', icon: UserCircle2 },
]

const adminLinks = [
  { to: '/admin', label: 'Admin', icon: Shield },
  { to: '/admin/players', label: 'Gracze', icon: UsersRound },
  { to: '/admin/teams', label: 'Drużyny', icon: Trophy },
  { to: '/admin/matches', label: 'Mecze Admin', icon: CalendarDays },
]

const navigationLinkClasses = (isActive: boolean, variant: 'desktop' | 'mobile' | 'admin') =>
  clsx(
    'transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-emerald-300/80',
    variant === 'desktop' &&
      'inline-flex shrink-0 items-center gap-2 rounded-full px-4 py-2 text-sm font-medium',
    variant === 'mobile' &&
      'flex min-h-14 flex-1 flex-col items-center justify-center gap-1 rounded-2xl px-1 text-[0.7rem] font-semibold',
    variant === 'admin' &&
      'flex min-h-11 min-w-0 flex-col items-center justify-center gap-1 rounded-2xl px-1.5 py-2 text-center text-[0.65rem] font-semibold leading-tight',
    isActive
      ? 'bg-emerald-400 text-slate-950 shadow-lg shadow-emerald-950/30'
      : 'bg-white/5 text-slate-300 hover:bg-white/10 hover:text-white',
  )

export const AppNavigation = () => {
  const { isAdmin, user, logout } = useAuth()
  const links = [...commonLinks, ...(isAdmin ? adminLinks : [])]

  return (
    <>
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

          <nav className="hidden gap-2 overflow-x-auto pb-1 sm:flex" aria-label="Nawigacja główna">
            {links.map(({ to, label, icon: Icon }) => (
              <NavLink
                key={to}
                to={to}
                end={to === '/'}
                className={({ isActive }) => navigationLinkClasses(isActive, 'desktop')}
              >
                <Icon className="h-4 w-4" aria-hidden="true" />
                {label}
              </NavLink>
            ))}
          </nav>
        </div>
      </header>
      <nav
        className="fixed inset-x-0 bottom-0 z-30 overflow-x-hidden border-t border-white/10 bg-pitch-950/95 px-3 pb-[calc(env(safe-area-inset-bottom)+0.5rem)] pt-2 shadow-[0_-18px_45px_-30px_rgba(0,0,0,0.95)] backdrop-blur-xl sm:hidden"
        aria-label="Nawigacja mobilna gracza"
      >
        {isAdmin ? (
          <div className="mb-2" aria-label="Nawigacja mobilna administratora">
            <div className="grid grid-cols-4 gap-1.5">
              {adminLinks.map(({ to, label, icon: Icon }) => (
                <NavLink
                  key={to}
                  to={to}
                  end={to === '/admin'}
                  className={({ isActive }) => navigationLinkClasses(isActive, 'admin')}
                >
                  <Icon className="h-4 w-4" aria-hidden="true" />
                  <span>{label}</span>
                </NavLink>
              ))}
            </div>
          </div>
        ) : null}
        <div className="flex gap-1.5">
          {commonLinks.map(({ to, label, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              end={to === '/'}
              className={({ isActive }) => navigationLinkClasses(isActive, 'mobile')}
            >
              <Icon className="h-5 w-5" aria-hidden="true" />
              <span>{label}</span>
            </NavLink>
          ))}
        </div>
      </nav>
    </>
  )
}
