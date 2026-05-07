import { useQuery } from '@tanstack/react-query'
import { predictionsApi, rankingApi } from '../../api/services'
import { formatKickoff } from '../../app/formatters'
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

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <StatCard label="Miejsce" value={ranking ? `#${ranking.position}` : '-'} />
        <StatCard label="Punkty" value={ranking?.totalPoints ?? 0} accent="text-emerald-300" />
        <StatCard label="Dokładne wyniki" value={ranking?.exactScoreHits ?? 0} />
        <StatCard label="Trafione rezultaty" value={ranking?.correctOutcomeHits ?? 0} />
      </div>

      <div className="grid gap-6 xl:grid-cols-[1fr_1.3fr]">
        <Panel className="space-y-4">
          <p className="font-display text-2xl uppercase text-white">Progres po meczach</p>
          <div className="space-y-3">
            {progressQuery.data?.map((point) => (
              <div key={point.matchId} className="rounded-2xl bg-slate-950/50 px-4 py-3">
                <p className="font-semibold text-white">Po meczu #{point.matchNumber}</p>
                <p className="text-sm text-slate-400">
                  Punkty: {point.totalPoints} • Pozycja: #{point.position} • Typy: {point.predictionsCount}
                </p>
              </div>
            ))}
          </div>
        </Panel>

        <Panel className="space-y-4">
          <p className="font-display text-2xl uppercase text-white">Historia typów</p>
          <div className="space-y-3">
            {predictionsQuery.data?.map((item) => (
              <div key={item.matchId} className="flex items-center justify-between rounded-2xl bg-slate-950/50 px-4 py-3">
                <div>
                  <p className="font-semibold text-white">
                    {item.homeTeamName} vs {item.awayTeamName}
                  </p>
                  <p className="text-sm text-slate-400">{formatKickoff(item.kickoffTimeUtc)}</p>
                </div>
                <div className="text-right">
                  <p className="font-display text-2xl text-emerald-300">
                    {item.prediction.predictedHomeScore}:{item.prediction.predictedAwayScore}
                  </p>
                  <p className="text-xs text-slate-400">Punkty: {item.prediction.points ?? '-'}</p>
                </div>
              </div>
            ))}
          </div>
        </Panel>
      </div>
    </div>
  )
}
