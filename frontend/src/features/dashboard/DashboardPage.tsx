import { useQuery } from '@tanstack/react-query'
import { ArrowRight, Sparkles } from 'lucide-react'
import { Link } from 'react-router-dom'
import { getErrorMessage } from '../../api/client'
import { matchesApi, rankingApi } from '../../api/services'
import { MatchCard } from '../../components/MatchCard'
import { Panel } from '../../components/Panel'
import { QueryState } from '../../components/QueryState'
import { SectionHeading } from '../../components/SectionHeading'
import { StatCard } from '../../components/StatCard'
import { UserAvatar } from '../../components/UserAvatar'
import { secondaryButtonClassName } from '../../styles/ui'
import { useAuth } from '../auth/useAuth'

export const DashboardPage = () => {
  const { user } = useAuth()
  const matchesQuery = useQuery({ queryKey: ['matches'], queryFn: matchesApi.getAll })
  const upcomingQuery = useQuery({ queryKey: ['matches', 'upcoming'], queryFn: matchesApi.getUpcoming })
  const topQuery = useQuery({ queryKey: ['ranking', 'top'], queryFn: rankingApi.getTop })

  const matches = matchesQuery.data ?? []
  const upcomingMatches = upcomingQuery.data ?? []
  const openMatchesWithoutPrediction = matches.filter((match) => match.canEditPrediction && !match.myPrediction).length

  const now = new Date()
  const next24h = new Date(now.getTime() + 24 * 60 * 60 * 1000)
  const matchesNext24hCount = matches.filter((match) => {
    const kickoff = new Date(match.kickoffTimeUtc)
    return kickoff >= now && kickoff < next24h
  }).length

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
          <StatCard label="Najbliższe mecze" value={matchesNext24hCount} />
          <StatCard label="Zasady" value="3 / 1 / 0" />
        </div>
      </QueryState>

      <div className="grid min-w-0 gap-6 xl:grid-cols-[1.7fr_1fr]">
        <Panel className="space-y-4">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="min-w-0">
              <p className="font-display text-2xl uppercase text-white">Najbliższe mecze</p>
              <p className="text-sm text-slate-400">Szybki dostęp do typowania i blokad kickoffu.</p>
            </div>
            <Link className="shrink-0 text-sm font-semibold text-emerald-300 hover:text-emerald-200" to="/matches">
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

        <div className="min-w-0 space-y-6">
          <Panel className="space-y-4">
            <div className="flex min-w-0 items-start gap-3">
              <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-emerald-400/12 text-emerald-300">
                <Sparkles className="h-5 w-5" aria-hidden="true" />
              </span>
              <div className="min-w-0">
                <p className="font-display text-2xl uppercase text-white">Mój finałowy recap</p>
                <p className="mt-1 text-sm leading-6 text-slate-400">
                  Osobiste miejsce, punkty, trafienia i ciekawostki z całego turnieju.
                </p>
              </div>
            </div>
            <Link to="/summary/final/me" className={`${secondaryButtonClassName} w-full sm:w-auto`}>
              Zobacz mój recap
              <ArrowRight className="ml-2 h-4 w-4" aria-hidden="true" />
            </Link>
          </Panel>

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
                    <div key={entry.userId} className="flex min-w-0 items-center justify-between gap-3 rounded-2xl bg-slate-950/50 px-4 py-3">
                    <div className="flex min-w-0 items-center gap-3">
                      <UserAvatar displayName={entry.displayName} avatarUrl={entry.avatarUrl} size="sm" />
                      <div className="min-w-0">
                        <p className="truncate font-semibold text-white">
                          #{entry.position} {entry.displayName}
                        </p>
                        <p className="text-xs text-slate-400">
                          Dokładne: {entry.exactScoreHits} / Rezultaty: {entry.correctOutcomeHits}
                        </p>
                      </div>
                    </div>
                    <p className="shrink-0 font-display text-2xl text-emerald-300">{entry.totalPoints}</p>
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
