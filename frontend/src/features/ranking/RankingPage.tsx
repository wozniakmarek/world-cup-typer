import { useQuery } from '@tanstack/react-query'
import { getErrorMessage } from '../../api/client'
import { rankingApi } from '../../api/services'
import { Panel } from '../../components/Panel'
import { QueryState } from '../../components/QueryState'
import { ResponsiveTable } from '../../components/ResponsiveTable'
import { SectionHeading } from '../../components/SectionHeading'
import { UserAvatar } from '../../components/UserAvatar'
import { mobileRecordClassName } from '../../styles/ui'

export const RankingPage = () => {
  const rankingQuery = useQuery({ queryKey: ['ranking'], queryFn: rankingApi.getAll })
  const ranking = rankingQuery.data ?? []

  return (
    <div className="space-y-6">
      <SectionHeading
        eyebrow="Ranking"
        title="Tabela liderów"
        description="Sortowanie: punkty, dokładne wyniki, trafione rezultaty, liczba oddanych typów i nazwa gracza."
      />

      <QueryState
        isLoading={rankingQuery.isLoading}
        isError={rankingQuery.isError}
        errorMessage={getErrorMessage(rankingQuery.error)}
        isEmpty={ranking.length === 0}
        emptyTitle="Ranking jest pusty"
        emptyDescription="Po rozliczeniu pierwszego meczu pojawią się tutaj pozycje graczy."
        loadingTitle="Ładowanie rankingu"
        loadingDescription="Pobieram aktualną tabelę liderów."
      >
        <Panel className="overflow-hidden p-0">
          <ResponsiveTable
            table={
              <div className="overflow-x-auto">
                <table className="min-w-full text-sm">
                  <thead className="bg-slate-950/60 text-left uppercase tracking-[0.2em] text-slate-400">
                    <tr>
                      <th className="px-4 py-4">Miejsce</th>
                      <th className="px-4 py-4">Gracz</th>
                      <th className="px-4 py-4">Punkty</th>
                      <th className="px-4 py-4">Dokładne</th>
                      <th className="px-4 py-4">Rezultaty</th>
                      <th className="px-4 py-4">Typy</th>
                    </tr>
                  </thead>
                  <tbody>
                    {ranking.map((entry) => (
                      <tr
                        key={entry.userId}
                        className={entry.isCurrentUser ? 'border-t border-emerald-400/20 bg-emerald-400/12' : 'border-t border-white/5'}
                      >
                        <td className="px-4 py-4 font-display text-xl text-white">#{entry.position}</td>
                        <td className="px-4 py-4 text-white">
                          <span className="flex items-center gap-3">
                            <UserAvatar displayName={entry.displayName} avatarUrl={entry.avatarUrl} size="sm" />
                            <span className={entry.isCurrentUser ? 'font-semibold text-emerald-200' : undefined}>
                              {entry.displayName}
                            </span>
                          </span>
                        </td>
                        <td className="px-4 py-4 text-emerald-300">{entry.totalPoints}</td>
                        <td className="px-4 py-4">{entry.exactScoreHits}</td>
                        <td className="px-4 py-4">{entry.correctOutcomeHits}</td>
                        <td className="px-4 py-4">{entry.predictionsCount}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            }
            cards={ranking.map((entry) => (
              <article
                key={entry.userId}
                className={`${mobileRecordClassName} ${entry.isCurrentUser ? 'border-emerald-400/30 bg-emerald-400/10' : ''}`}
              >
                <div className="flex items-start justify-between gap-3">
                  <div className="flex min-w-0 items-center gap-3">
                    <UserAvatar displayName={entry.displayName} avatarUrl={entry.avatarUrl} />
                    <div className="min-w-0">
                      <p className="font-display text-xl text-white">#{entry.position}</p>
                      <p className={`mt-1 truncate text-sm ${entry.isCurrentUser ? 'font-semibold text-emerald-200' : 'text-white'}`}>
                        {entry.displayName}
                      </p>
                    </div>
                  </div>
                  <div className="text-right">
                    <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Punkty</p>
                    <p className="font-display text-3xl text-emerald-300">{entry.totalPoints}</p>
                  </div>
                </div>

                <div className="mt-4 grid grid-cols-3 gap-3 text-sm text-slate-300">
                  <div>
                    <p className="text-xs uppercase tracking-[0.18em] text-slate-500">Dokładne</p>
                    <p className="mt-1 text-white">{entry.exactScoreHits}</p>
                  </div>
                  <div>
                    <p className="text-xs uppercase tracking-[0.18em] text-slate-500">Rezultaty</p>
                    <p className="mt-1 text-white">{entry.correctOutcomeHits}</p>
                  </div>
                  <div>
                    <p className="text-xs uppercase tracking-[0.18em] text-slate-500">Typy</p>
                    <p className="mt-1 text-white">{entry.predictionsCount}</p>
                  </div>
                </div>
              </article>
            ))}
          />
        </Panel>
      </QueryState>
    </div>
  )
}
