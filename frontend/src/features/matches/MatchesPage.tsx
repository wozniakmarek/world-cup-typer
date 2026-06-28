import { useEffect, useRef, useState } from 'react'
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
  { key: 'today', label: 'Dzisiaj' },
  { key: 'tomorrow', label: 'Jutro' },
  { key: 'open', label: 'Do obstawienia' },
  { key: 'locked', label: 'Zablokowane' },
  { key: 'settled', label: 'Rozliczone' },
] as const

type MatchFilterKey = (typeof filters)[number]['key']

const matchesScrollStorageKey = 'typer.matches.scrollY'
const matchesFilterStorageKey = 'typer.matches.filter'

const isMatchFilterKey = (value: string): value is MatchFilterKey =>
  filters.some((filter) => filter.key === value)

const readStoredMatchesScrollY = () => {
  try {
    const rawValue = window.sessionStorage.getItem(matchesScrollStorageKey)
    const scrollY = rawValue == null ? null : Number(rawValue)
    return scrollY != null && Number.isFinite(scrollY) && scrollY > 0 ? scrollY : null
  } catch {
    return null
  }
}

const saveMatchesScrollY = () => {
  try {
    window.sessionStorage.setItem(matchesScrollStorageKey, String(Math.max(0, Math.round(window.scrollY))))
  } catch {
    return
  }
}

const readStoredMatchesFilter = (): MatchFilterKey => {
  try {
    const rawValue = window.sessionStorage.getItem(matchesFilterStorageKey)
    return rawValue && isMatchFilterKey(rawValue) ? rawValue : 'all'
  } catch {
    return 'all'
  }
}

const saveMatchesFilter = (filter: MatchFilterKey) => {
  try {
    window.sessionStorage.setItem(matchesFilterStorageKey, filter)
  } catch {
    return
  }
}

export const MatchesPage = () => {
  const [filter, setFilter] = useState<MatchFilterKey>(() => readStoredMatchesFilter())
  const didRestoreScroll = useRef(false)
  const isOpeningMatchDetails = useRef(false)
  const matchesQuery = useQuery({ queryKey: ['matches'], queryFn: matchesApi.getAll })
  const handleFilterChange = (nextFilter: MatchFilterKey) => {
    setFilter(nextFilter)
    saveMatchesFilter(nextFilter)
  }
  const handleOpenMatchDetails = () => {
    saveMatchesScrollY()
    isOpeningMatchDetails.current = true
  }

  const visibleMatches = (matchesQuery.data ?? []).filter((match) => shouldShowMatchToPlayer(match)).filter((match) => {
    const canEditPrediction = canEditMatchPrediction(match)

    if (filter === 'today' || filter === 'tomorrow') {
      const kickoff = new Date(match.kickoffTimeUtc)
      const today = new Date()
      today.setHours(0, 0, 0, 0)
      const target = new Date(today)
      if (filter === 'tomorrow') target.setDate(target.getDate() + 1)
      const next = new Date(target)
      next.setDate(next.getDate() + 1)
      return kickoff >= target && kickoff < next
    }

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
  const matches = filter === 'settled'
    ? [...visibleMatches].sort((current, next) =>
        new Date(next.kickoffTimeUtc).getTime() - new Date(current.kickoffTimeUtc).getTime()
        || next.matchNumber - current.matchNumber)
    : visibleMatches

  useEffect(() => {
    const saveCurrentMatchesScrollY = () => {
      if (!isOpeningMatchDetails.current) {
        saveMatchesScrollY()
      }
    }

    window.addEventListener('scroll', saveCurrentMatchesScrollY, { passive: true })
    window.addEventListener('pagehide', saveCurrentMatchesScrollY)

    return () => {
      window.removeEventListener('scroll', saveCurrentMatchesScrollY)
      window.removeEventListener('pagehide', saveCurrentMatchesScrollY)
    }
  }, [])

  useEffect(() => {
    if (didRestoreScroll.current || matchesQuery.isLoading || matches.length === 0) {
      return
    }

    didRestoreScroll.current = true
    const scrollY = readStoredMatchesScrollY()
    if (scrollY == null) {
      return
    }

    const animationFrameId = window.requestAnimationFrame(() => window.scrollTo({ top: scrollY }))
    return () => window.cancelAnimationFrame(animationFrameId)
  }, [matches.length, matchesQuery.isLoading])

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
            onClick={() => handleFilterChange(item.key)}
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
            <MatchCard key={match.id} match={match} onOpenDetails={handleOpenMatchDetails} />
          ))}
        </div>
      </QueryState>
    </div>
  )
}
