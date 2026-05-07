import { useQuery } from '@tanstack/react-query'
import { rankingApi } from '../../api/services'
import { EmptyState } from '../../components/EmptyState'
import { Panel } from '../../components/Panel'
import { SectionHeading } from '../../components/SectionHeading'

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

      <Panel className="overflow-hidden p-0">
        {ranking.length === 0 ? (
          <div className="p-6">
            <EmptyState title="Ranking jest pusty" description="Po rozliczeniu pierwszego meczu pojawią się tutaj pozycje graczy." />
          </div>
        ) : (
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
                  <tr key={entry.userId} className={entry.isCurrentUser ? 'bg-emerald-400/10' : 'border-t border-white/5'}>
                    <td className="px-4 py-4 font-display text-xl text-white">#{entry.position}</td>
                    <td className="px-4 py-4 text-white">{entry.displayName}</td>
                    <td className="px-4 py-4 text-emerald-300">{entry.totalPoints}</td>
                    <td className="px-4 py-4">{entry.exactScoreHits}</td>
                    <td className="px-4 py-4">{entry.correctOutcomeHits}</td>
                    <td className="px-4 py-4">{entry.predictionsCount}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Panel>
    </div>
  )
}
