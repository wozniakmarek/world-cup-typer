import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { matchesApi } from '../../api/services'
import { MatchCard } from '../../components/MatchCard'
import { SectionHeading } from '../../components/SectionHeading'

const filters = [
  { key: 'all', label: 'Wszystkie' },
  { key: 'open', label: 'Do obstawienia' },
  { key: 'locked', label: 'Zablokowane' },
  { key: 'settled', label: 'Rozliczone' },
] as const

export const MatchesPage = () => {
  const [filter, setFilter] = useState<(typeof filters)[number]['key']>('all')
  const matchesQuery = useQuery({ queryKey: ['matches'], queryFn: matchesApi.getAll })

  const matches = (matchesQuery.data ?? []).filter((match) => {
    if (filter === 'open') {
      return match.canEditPrediction
    }

    if (filter === 'locked') {
      return !match.canEditPrediction && !match.isSettled
    }

    if (filter === 'settled') {
      return match.isSettled
    }

    return true
  })

  return (
    <div className="space-y-6">
      <SectionHeading
        eyebrow="Mecze"
        title="Typowanie spotkań"
        description="Filtruj mecze, sprawdzaj status blokady i przechodź od razu do szczegółów typu."
      />

      <div className="flex flex-wrap gap-2">
        {filters.map((item) => (
          <button
            key={item.key}
            type="button"
            onClick={() => setFilter(item.key)}
            className={`rounded-full px-4 py-2 text-sm font-semibold transition ${
              filter === item.key
                ? 'bg-emerald-400 text-slate-950'
                : 'bg-white/5 text-slate-300 hover:bg-white/10 hover:text-white'
            }`}
          >
            {item.label}
          </button>
        ))}
      </div>

      <div className="grid gap-4 xl:grid-cols-2">
        {matches.map((match) => (
          <MatchCard key={match.id} match={match} />
        ))}
      </div>
    </div>
  )
}
