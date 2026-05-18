import { useQuery } from '@tanstack/react-query'
import { getErrorMessage } from '../../api/client'
import { predictionsApi, rankingApi } from '../../api/services'
import { formatKickoff } from '../../app/formatters'
import { QueryState } from '../../components/QueryState'
import { Panel } from '../../components/Panel'
import { SectionHeading } from '../../components/SectionHeading'
import { StatCard } from '../../components/StatCard'

export const ProfilePage = () => {
  const myRankingQuery = useQuery({ queryKey: ['ranking', 'me'], queryFn: rankingApi.getMine })
  const progressQuery = useQuery({ queryKey: ['ranking', 'progress'], queryFn: rankingApi.getProgress })
  const predictionsQuery = useQuery({ queryKey: ['predictions', 'mine'], queryFn: predictionsApi.getMine })

  const ranking = myRankingQuery.data

  return (
    <div className="space-y-6">
      <SectionHeading
        eyebrow="Profil"
        title="Moje statystyki"
        description="Twoje miejsce w tabeli, aktualny dorobek punktowy i historia typów."
      />

      <QueryState
        isLoading={myRankingQuery.isLoading}
        isError={myRankingQuery.isError}
        errorMessage={getErrorMessage(myRankingQuery.error)}
        isEmpty={!ranking}
        emptyTitle="Brak pozycji w rankingu"
        emptyDescription="Gdy system utworzy Twój wpis rankingowy, zobaczysz tutaj bieżące podsumowanie."
        loadingTitle="Ładowanie podsumowania profilu"
        loadingDescription="Pobieram Twoje miejsce w tabeli i aktualny dorobek punktowy."
      >
        {ranking ? (
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <StatCard label="Miejsce" value={`#${ranking.position}`} />
            <StatCard label="Punkty" value={ranking.totalPoints} accent="text-emerald-300" />
            <StatCard label="Dokładne wyniki" value={ranking.exactScoreHits} />
            <StatCard label="Trafione rezultaty" value={ranking.correctOutcomeHits} />
          </div>
        ) : null}
      </QueryState>

      <div className="grid gap-6 xl:grid-cols-[1fr_1.3fr]">
        <QueryState
          isLoading={progressQuery.isLoading}
          isError={progressQuery.isError}
          errorMessage={getErrorMessage(progressQuery.error)}
          isEmpty={(progressQuery.data?.length ?? 0) === 0}
          emptyTitle="Brak progresu po meczach"
          emptyDescription="Gdy pojawią się rozliczone spotkania, zobaczysz tutaj rozwój swojej pozycji."
          loadingTitle="Ładowanie progresu"
          loadingDescription="Pobieram historię zmian Twojego dorobku punktowego."
        >
          <Panel className="space-y-4">
            <p className="font-display text-2xl uppercase text-white">Progres po meczach</p>
            <div className="space-y-3">
              {progressQuery.data?.map((point) => (
                <div key={point.matchId} className="rounded-2xl bg-slate-950/50 px-4 py-3">
                  <p className="font-semibold text-white">Po meczu #{point.matchNumber}</p>
                  <div className="mt-2 grid gap-2 text-sm text-slate-400 sm:grid-cols-3">
                    <p>Punkty: {point.totalPoints}</p>
                    <p>Pozycja: #{point.position}</p>
                    <p>Typy: {point.predictionsCount}</p>
                  </div>
                </div>
              ))}
            </div>
          </Panel>
        </QueryState>

        <QueryState
          isLoading={predictionsQuery.isLoading}
          isError={predictionsQuery.isError}
          errorMessage={getErrorMessage(predictionsQuery.error)}
          isEmpty={(predictionsQuery.data?.length ?? 0) === 0}
          emptyTitle="Historia typów jest pusta"
          emptyDescription="Po zapisaniu pierwszego typu wrócisz tutaj do swojej historii przewidywań."
          loadingTitle="Ładowanie historii typów"
          loadingDescription="Pobieram Twoje zapisane typy i punkty."
        >
          <Panel className="space-y-4">
            <p className="font-display text-2xl uppercase text-white">Historia typów</p>
            <div className="space-y-3">
              {predictionsQuery.data?.map((item) => (
                <div key={item.matchId} className="rounded-2xl bg-slate-950/50 px-4 py-3">
                  <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                    <div>
                      <p className="font-semibold text-white">
                        {item.homeTeamName} vs {item.awayTeamName}
                      </p>
                      <p className="text-sm text-slate-400">{formatKickoff(item.kickoffTimeUtc)}</p>
                    </div>
                    <div className="sm:text-right">
                      <p className="font-display text-2xl text-emerald-300">
                        {item.prediction.predictedHomeScore}:{item.prediction.predictedAwayScore}
                      </p>
                      <p className="text-xs text-slate-400">Punkty: {item.prediction.points ?? '-'}</p>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </Panel>
        </QueryState>
      </div>
    </div>
  )
}
