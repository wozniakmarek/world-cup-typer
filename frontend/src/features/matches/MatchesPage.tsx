import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { getErrorMessage } from '../../api/client'
import { matchesApi } from '../../api/services'
import { canEditMatchPrediction, shouldShowMatchToPlayer } from '../../app/formatters'
import { MatchCard } from '../../components/MatchCard'
import { QueryState } from '../../components/QueryState'
import { SectionHeading } from '../../components/SectionHeading'
import { filterButtonClassName } from '../../styles/ui'

const filters = [
  { key: 'all', label: 'Wszystkie' },
  { key: 'open', label: 'Do obstawienia' },
  { key: 'locked', label: 'Zablokowane' },
  { key: 'settled', label: 'Rozliczone' },
] as const

export const MatchesPage = () => {
  const [filter, setFilter] = useState<(typeof filters)[number]['key']>('all')
  const matchesQuery = useQuery({ queryKey: ['matches'], queryFn: matchesApi.getAll })

  const matches = (matchesQuery.data ?? []).filter((match) => shouldShowMatchToPlayer(match)).filter((match) => {
    const canEditPrediction = canEditMatchPrediction(match)

    if (filter === 'open') {
      return canEditPrediction
    }

    if (filter === 'locked') {
      return !canEditPrediction && !match.isSettled
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
            className={filterButtonClassName(filter === item.key)}
          >
            {item.label}
          </button>
        ))}
      </div>

      <QueryState
        isLoading={matchesQuery.isLoading}
        isError={matchesQuery.isError}
        errorMessage={getErrorMessage(matchesQuery.error)}
        isEmpty={matches.length === 0}
        emptyTitle="Brak meczów dla tego filtra"
        emptyDescription="Zmień filtr albo wróć później, gdy pojawią się kolejne spotkania."
        loadingTitle="Ładowanie meczów"
        loadingDescription="Pobieram terminarz i status Twoich typów."
      >
        <div className="grid gap-4 xl:grid-cols-2">
          {matches.map((match) => (
            <MatchCard key={match.id} match={match} />
          ))}
        </div>
      </QueryState>
    </div>
  )
}
