import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { adminApi, teamsApi } from '../../api/services'
import { Panel } from '../../components/Panel'
import { SectionHeading } from '../../components/SectionHeading'
import { StatCard } from '../../components/StatCard'

export const AdminDashboardPage = () => {
  const playersQuery = useQuery({ queryKey: ['admin', 'players'], queryFn: adminApi.getPlayers })
  const matchesQuery = useQuery({ queryKey: ['admin', 'matches'], queryFn: adminApi.getMatches })
  const teamsQuery = useQuery({ queryKey: ['teams'], queryFn: teamsApi.getAll })

  const players = playersQuery.data ?? []
  const matches = matchesQuery.data ?? []
  const teams = teamsQuery.data ?? []
  const matchesToSettle = matches.filter((match) => !match.isSettled && match.homeScore90 != null && match.awayScore90 != null).length

  return (
    <div className="space-y-6">
      <SectionHeading
        eyebrow="Admin"
        title="Centrum sterowania"
        description="Zarządzaj graczami, terminarzem, wynikami i rozliczaniem meczów z jednego miejsca."
      />

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <StatCard label="Gracze" value={players.length} />
        <StatCard label="Drużyny" value={teams.length} />
        <StatCard label="Mecze" value={matches.length} />
        <StatCard label="Do rozliczenia" value={matchesToSettle} accent="text-emerald-300" />
      </div>

      <div className="grid gap-6 xl:grid-cols-3">
        <Panel className="space-y-3">
          <p className="font-display text-2xl uppercase text-white">Zarządzanie graczami</p>
          <p className="text-sm text-slate-400">Dodawaj graczy, edytuj role i ustawiaj tymczasowe hasła.</p>
          <Link className="inline-flex text-sm font-semibold text-emerald-300 hover:text-emerald-200" to="/admin/players">
            Otwórz panel graczy
          </Link>
        </Panel>

        <Panel className="space-y-3">
          <p className="font-display text-2xl uppercase text-white">Zarządzanie drużynami</p>
          <p className="text-sm text-slate-400">Utrzymuj listę reprezentacji, skróty i grupy turniejowe przed układaniem terminarza.</p>
          <Link className="inline-flex text-sm font-semibold text-emerald-300 hover:text-emerald-200" to="/admin/teams">
            Otwórz panel drużyn
          </Link>
        </Panel>

        <Panel className="space-y-3">
          <p className="font-display text-2xl uppercase text-white">Zarządzanie meczami</p>
          <p className="text-sm text-slate-400">Dodawaj mecze, wpisuj wyniki po 90 minutach i rozliczaj ranking.</p>
          <Link className="inline-flex text-sm font-semibold text-emerald-300 hover:text-emerald-200" to="/admin/matches">
            Otwórz panel meczów
          </Link>
        </Panel>
      </div>
    </div>
  )
}
