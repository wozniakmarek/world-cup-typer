import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { getErrorMessage } from '../../api/client'
import { matchesApi, rankingApi } from '../../api/services'
import { MatchCard } from '../../components/MatchCard'
import { Panel } from '../../components/Panel'
import { QueryState } from '../../components/QueryState'
import { SectionHeading } from '../../components/SectionHeading'
import { StatCard } from '../../components/StatCard'
import { useAuth } from '../auth/AuthContext'

export const DashboardPage = () => {
  const { user } = useAuth()
  const matchesQuery = useQuery({ queryKey: ['matches'], queryFn: matchesApi.getAll })
  const upcomingQuery = useQuery({ queryKey: ['matches', 'upcoming'], queryFn: matchesApi.getUpcoming })
  const topQuery = useQuery({ queryKey: ['ranking', 'top'], queryFn: rankingApi.getTop })

  const matches = matchesQuery.data ?? []
  const upcomingMatches = upcomingQuery.data ?? []
  const openMatchesWithoutPrediction = matches.filter((match) => match.canEditPrediction && !match.myPrediction).length

  const summaryIsLoading = matchesQuery.isLoading || upcomingQuery.isLoading
  const summaryIsError = matchesQuery.isError || upcomingQuery.isError
  const summaryErrorMessage = matchesQuery.isError
    ? getErrorMessage(matchesQuery.error)
    : upcomingQuery.isError
      ? getErrorMessage(upcomingQuery.error)
      : undefined

  return (
    <div className="space-y-6">
      <SectionHeading
        eyebrow="Dashboard"
        title={`Cześć, ${user?.displayName ?? 'Graczu'}`}
        description="Najbliższe mecze, szybki podgląd rankingu i status Twoich typów w jednym miejscu."
      />

      <QueryState
        isLoading={summaryIsLoading}
        isError={summaryIsError}
        errorMessage={summaryErrorMessage}
        loadingTitle="Ładowanie podsumowania"
        loadingDescription="Pobieram najważniejsze liczby dla Twojego pulpitu."
      >
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <StatCard label="Mecze bez typu" value={openMatchesWithoutPrediction} accent="text-emerald-300" />
          <StatCard label="Wszystkie mecze" value={matches.length} />
          <StatCard label="Najbliższe mecze" value={upcomingMatches.length} />
          <StatCard label="Zasady" value="3 / 1 / 0" />
        </div>
      </QueryState>

      <div className="grid gap-6 xl:grid-cols-[1.7fr_1fr]">
        <Panel className="space-y-4">
          <div className="flex items-center justify-between gap-3">
            <div>
              <p className="font-display text-2xl uppercase text-white">Najbliższe mecze</p>
              <p className="text-sm text-slate-400">Szybki dostęp do typowania i blokad kickoffu.</p>
            </div>
            <Link className="text-sm font-semibold text-emerald-300 hover:text-emerald-200" to="/matches">
              Zobacz wszystkie
            </Link>
          </div>

          <QueryState
            isLoading={upcomingQuery.isLoading}
            isError={upcomingQuery.isError}
            errorMessage={getErrorMessage(upcomingQuery.error)}
            isEmpty={upcomingMatches.length === 0}
            emptyTitle="Brak nadchodzących meczów"
            emptyDescription="Gdy terminarz pojawi się w systemie, tutaj zobaczysz kolejne spotkania do typowania."
            loadingTitle="Ładowanie terminarza"
            loadingDescription="Pobieram najbliższe mecze i status otwartych typów."
          >
            <div className="grid gap-4">
              {upcomingMatches.slice(0, 3).map((match) => (
                <MatchCard key={match.id} match={match} />
              ))}
            </div>
          </QueryState>
        </Panel>

        <div className="space-y-6">
          <Panel className="space-y-4">
            <p className="font-display text-2xl uppercase text-white">TOP 5 rankingu</p>

            <QueryState
              isLoading={topQuery.isLoading}
              isError={topQuery.isError}
              errorMessage={getErrorMessage(topQuery.error)}
              isEmpty={(topQuery.data?.length ?? 0) === 0}
              emptyTitle="Ranking czeka na pierwsze punkty"
              emptyDescription="Po rozliczeniu meczów pojawi się tutaj aktualne TOP 5."
              loadingTitle="Ładowanie rankingu"
              loadingDescription="Pobieram aktualnych liderów tabeli."
            >
              <div className="space-y-3">
                {topQuery.data?.map((entry) => (
                  <div key={entry.userId} className="flex items-center justify-between rounded-2xl bg-slate-950/50 px-4 py-3">
                    <div>
                      <p className="font-semibold text-white">
                        #{entry.position} {entry.displayName}
                      </p>
                      <p className="text-xs text-slate-400">
                        Dokładne: {entry.exactScoreHits} • Rezultaty: {entry.correctOutcomeHits}
                      </p>
                    </div>
                    <p className="font-display text-2xl text-emerald-300">{entry.totalPoints}</p>
                  </div>
                ))}
              </div>
            </QueryState>
          </Panel>

          <Panel className="space-y-3">
            <p className="font-display text-2xl uppercase text-white">Skrót zasad</p>
            <ul className="space-y-2 text-sm text-slate-300">
              <li>3 pkt za dokładny wynik po 90 minucie.</li>
              <li>1 pkt za trafionego zwycięzcę albo remis.</li>
              <li>Dogrywki i karne nie są liczone do punktacji.</li>
            </ul>
          </Panel>
        </div>
      </div>
    </div>
  )
}
