import { useQuery } from '@tanstack/react-query'
import { CalendarDays, ChevronRight, Flag, Users } from 'lucide-react'
import { Link } from 'react-router-dom'
import { getErrorMessage } from '../../api/client'
import { adminApi, teamsApi } from '../../api/services'
import { Panel } from '../../components/Panel'
import { QueryState } from '../../components/QueryState'
import { SectionHeading } from '../../components/SectionHeading'
import { StatCard } from '../../components/StatCard'

const adminAreas = [
  {
    to: '/admin/players',
    title: 'Zarzadzanie graczami',
    description: 'Dodawaj nowe konta, edytuj role i szybko resetuj hasla tymczasowe dla organizatorow oraz uczestnikow.',
    hint: 'Najlepszy start przed otwarciem typowania',
    icon: Users,
    accent: 'emerald',
  },
  {
    to: '/admin/teams',
    title: 'Zarzadzanie druzynami',
    description: 'Pilnuj nazw, skrotow, kodow kraju i grup turniejowych, zanim zaczniesz budowac terminarz.',
    hint: 'Fundament dla terminarza',
    icon: Flag,
    accent: 'slate',
  },
  {
    to: '/admin/matches',
    title: 'Zarzadzanie meczami',
    description: 'Tworz terminarz, wpisuj wynik po 90 minutach i uruchamiaj rozliczenie, kiedy spotkanie jest gotowe.',
    hint: 'Najczesciej odwiedzany panel podczas turnieju',
    icon: CalendarDays,
    accent: 'slate',
  },
] as const

export const AdminDashboardPage = () => {
  const playersQuery = useQuery({ queryKey: ['admin', 'players'], queryFn: adminApi.getPlayers })
  const matchesQuery = useQuery({ queryKey: ['admin', 'matches'], queryFn: adminApi.getMatches })
  const teamsQuery = useQuery({ queryKey: ['teams'], queryFn: teamsApi.getAll })

  const players = playersQuery.data ?? []
  const matches = matchesQuery.data ?? []
  const teams = teamsQuery.data ?? []
  const matchesToSettle = matches.filter((match) => !match.isSettled && match.homeScore90 != null && match.awayScore90 != null).length
  const isLoading = playersQuery.isLoading || matchesQuery.isLoading || teamsQuery.isLoading
  const isError = playersQuery.isError || matchesQuery.isError || teamsQuery.isError
  const errorMessage = getErrorMessage(playersQuery.error ?? matchesQuery.error ?? teamsQuery.error)

  return (
    <div className="space-y-6">
      <SectionHeading
        eyebrow="Admin"
        title="Centrum sterowania"
        description="Zarzadzaj graczami, terminarzem, wynikami i rozliczaniem meczow z jednego miejsca."
      />

      <QueryState
        isLoading={isLoading}
        isError={isError}
        errorMessage={errorMessage}
        isEmpty={players.length === 0 && teams.length === 0 && matches.length === 0}
        emptyTitle="Panel czeka na dane startowe"
        emptyDescription="Dodaj druzyny, mecze albo pierwszych graczy, a centrum sterowania zacznie pokazywac postep."
        loadingTitle="Ladowanie panelu admina"
        loadingDescription="Pobieram graczy, druzyny i mecze potrzebne do podsumowania."
      >
        <div className="space-y-6">
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <StatCard label="Gracze" value={players.length} />
            <StatCard label="Druzyny" value={teams.length} />
            <StatCard label="Mecze" value={matches.length} />
            <StatCard label="Do rozliczenia" value={matchesToSettle} accent="text-emerald-300" />
          </div>

          <div className="grid gap-6 xl:grid-cols-3">
            {adminAreas.map(({ to, title, description, hint, icon: Icon, accent }) => (
              <Panel key={to} className="flex h-full flex-col gap-5">
                <div className="flex items-start justify-between gap-3">
                  <div
                    className={
                      accent === 'emerald'
                        ? 'rounded-2xl border border-emerald-400/20 bg-emerald-400/10 p-3 text-emerald-200'
                        : 'rounded-2xl border border-white/10 bg-white/5 p-3 text-slate-200'
                    }
                  >
                    <Icon className="h-5 w-5" />
                  </div>
                  <p className="rounded-full border border-white/10 bg-white/5 px-3 py-1 text-xs uppercase tracking-[0.18em] text-slate-400">
                    {to === '/admin/players' ? `${players.length} kont` : to === '/admin/teams' ? `${teams.length} reprezentacji` : `${matchesToSettle} do akcji`}
                  </p>
                </div>

                <div className="space-y-2">
                  <p className="font-display text-2xl uppercase text-white">{title}</p>
                  <p className="text-sm text-slate-400">{description}</p>
                </div>

                <div className="rounded-3xl bg-slate-950/45 px-4 py-4 text-sm text-slate-300">
                  <p>{hint}</p>
                </div>

                <Link className="inline-flex items-center gap-2 text-sm font-semibold text-emerald-300 hover:text-emerald-200" to={to}>
                  Otworz panel
                  <ChevronRight className="h-4 w-4" />
                </Link>
              </Panel>
            ))}
          </div>
        </div>
      </QueryState>
    </div>
  )
}
