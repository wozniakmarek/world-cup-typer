import { useEffect, useMemo, useRef, useState } from 'react'
import { BarChart3 } from 'lucide-react'
import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { TooltipProps } from 'recharts'
import type { FinalRankingPositionSeries } from '../../api/types'
import { UserAvatar } from '../../components/UserAvatar'
import {
  buildFinalChartRows,
  finalChartColors,
  getBiggestClimbUserIds,
  getBiggestDropUserIds,
} from './summaryChart'

type FilterMode = 'all' | 'podium' | 'mine' | 'climb' | 'drop' | 'selected'
type TooltipValue = number | string | Array<number | string>
type TooltipName = number | string

interface FinalRankingStoryChartProps {
  series: FinalRankingPositionSeries[]
  eyebrow?: string
  title?: string
  description?: string
  initialFilterMode?: FilterMode
}

const filterButtons: Array<{ mode: FilterMode; label: string }> = [
  { mode: 'all', label: 'Wszyscy' },
  { mode: 'podium', label: 'Podium' },
  { mode: 'mine', label: 'Mój przebieg' },
  { mode: 'climb', label: 'Największy skok' },
  { mode: 'drop', label: 'Największy spadek' },
  { mode: 'selected', label: 'Wybrani zawodnicy' },
]

const usePrefersReducedMotion = () => {
  const [prefersReducedMotion, setPrefersReducedMotion] = useState(false)

  useEffect(() => {
    if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
      return undefined
    }

    const mediaQuery = window.matchMedia('(prefers-reduced-motion: reduce)')
    const updatePreference = () => setPrefersReducedMotion(mediaQuery.matches)

    updatePreference()

    if (typeof mediaQuery.addEventListener === 'function') {
      mediaQuery.addEventListener('change', updatePreference)

      return () => mediaQuery.removeEventListener('change', updatePreference)
    }

    if (typeof mediaQuery.addListener !== 'function') {
      return undefined
    }

    mediaQuery.addListener(updatePreference)

    return () => mediaQuery.removeListener(updatePreference)
  }, [])

  return prefersReducedMotion
}

const formatPosition = (value: unknown) => (typeof value === 'number' ? `#${value}` : String(value))

const CustomTooltip = ({
  active,
  label,
  payload,
}: TooltipProps<TooltipValue, TooltipName>) => {
  if (!active || !payload?.length) {
    return null
  }

  const visiblePayload = payload
    .filter((item) => typeof item.value === 'number')
    .sort((first, second) => Number(first.value) - Number(second.value))

  return (
    <div className="max-w-xs rounded-2xl border border-white/10 bg-slate-950/95 p-3 text-sm shadow-2xl">
      <p className="font-display text-base uppercase text-white">{label}</p>
      <div className="mt-2 space-y-1">
        {visiblePayload.map((item) => (
          <div key={`${item.name}-${String(item.dataKey)}`} className="flex items-center justify-between gap-6">
            <span className="flex min-w-0 items-center gap-2 text-slate-300">
              <span className="h-2.5 w-2.5 shrink-0 rounded-full" style={{ backgroundColor: item.color }} />
              <span className="truncate">{item.name}</span>
            </span>
            <span className="font-semibold text-white">{formatPosition(item.value)}</span>
          </div>
        ))}
      </div>
    </div>
  )
}

export const FinalRankingStoryChart = ({
  series,
  eyebrow = 'Animowana pełna tabela',
  title = 'Finalowy ruch rankingu',
  description = 'Linie pokazują pozycję każdego gracza po kolejnym rozliczonym meczu. Filtry podświetlają historię bez wycinania reszty stawki z kontekstu.',
  initialFilterMode = 'all',
}: FinalRankingStoryChartProps) => {
  const scrollRef = useRef<HTMLDivElement>(null)
  const prefersReducedMotion = usePrefersReducedMotion()
  const [requestedFilterMode, setRequestedFilterMode] = useState<FilterMode>(initialFilterMode)
  const [selectedUserIds, setSelectedUserIds] = useState<string[]>([])

  const chartRows = useMemo(() => buildFinalChartRows(series), [series])
  const playerLines = useMemo(
    () =>
      series.map((player, index) => ({
        ...player,
        color: finalChartColors[index % finalChartColors.length],
      })),
    [series],
  )
  const selectedUserIdSet = useMemo(() => new Set(selectedUserIds), [selectedUserIds])
  const climbUserIdSet = useMemo(() => new Set(getBiggestClimbUserIds(series)), [series])
  const dropUserIdSet = useMemo(() => new Set(getBiggestDropUserIds(series)), [series])
  const hasCurrentUserLine = playerLines.some((player) => player.isCurrentUser)
  const hasSelectedPlayers = selectedUserIds.length > 0
  const filterMode =
    requestedFilterMode === 'mine' && !hasCurrentUserLine
      ? 'all'
      : requestedFilterMode === 'selected' && !hasSelectedPlayers
        ? 'all'
        : requestedFilterMode

  const matchCount = chartRows.length
  const needsScroll = matchCount > 12
  const perMatchWidth = matchCount > 60 ? 40 : matchCount > 30 ? 52 : 76
  const chartWidth = needsScroll ? Math.max(660, matchCount * perMatchWidth) : undefined
  const maxPosition = Math.max(1, ...playerLines.map((player) => player.finalPosition))
  const yTicks = Array.from({ length: maxPosition }, (_, index) => index + 1)
  const chartHeight = Math.max(420, Math.min(720, maxPosition * 28 + 160))
  const showDots = matchCount <= 40

  useEffect(() => {
    if (scrollRef.current && needsScroll) {
      scrollRef.current.scrollLeft = scrollRef.current.scrollWidth
    }
  }, [chartRows.length, needsScroll])

  const getFocusedUserIds = () => {
    if (filterMode === 'podium') {
      return playerLines.filter((player) => player.finalPosition <= 3).map((player) => player.userId)
    }

    if (filterMode === 'mine' && hasCurrentUserLine) {
      return playerLines.filter((player) => player.isCurrentUser).map((player) => player.userId)
    }

    if (filterMode === 'climb') {
      return [...climbUserIdSet]
    }

    if (filterMode === 'drop') {
      return [...dropUserIdSet]
    }

    if (filterMode === 'selected' && hasSelectedPlayers) {
      return selectedUserIds
    }

    return []
  }

  const focusedUserIds = getFocusedUserIds()
  const focusedUserIdSet = new Set(focusedUserIds)
  const hasFocusedLine = focusedUserIds.length > 0

  const toggleSelectedPlayer = (userId: string) => {
    setSelectedUserIds((currentUserIds) => {
      if (!currentUserIds.includes(userId)) {
        setRequestedFilterMode('selected')
        return [...currentUserIds, userId]
      }

      const nextUserIds = currentUserIds.filter((currentUserId) => currentUserId !== userId)

      if (nextUserIds.length === 0) {
        setRequestedFilterMode('all')
      }

      return nextUserIds
    })
  }

  return (
    <section
      id="final-table"
      data-testid="final-ranking-story-chart"
      className="glass-card min-w-0 rounded-[2rem] p-5 sm:p-6"
    >
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div className="min-w-0">
          <p className="font-display text-sm uppercase text-emerald-300">{eyebrow}</p>
          <h2 className="mt-2 break-words font-display text-3xl leading-tight text-white sm:text-4xl">
            {title}
          </h2>
          <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-300">
            {description}
          </p>
        </div>
        <BarChart3 className="h-8 w-8 shrink-0 text-emerald-300" aria-hidden="true" />
      </div>

      <div className="mt-5 flex flex-wrap gap-2">
        {filterButtons.map((filter) => {
          const isActive = filterMode === filter.mode
          const isDisabled =
            (filter.mode === 'mine' && !hasCurrentUserLine) || (filter.mode === 'selected' && !hasSelectedPlayers)
          const disabledTitle =
            filter.mode === 'mine'
              ? 'Mój przebieg jest dostępny po zalogowaniu.'
              : filter.mode === 'selected'
                ? 'Wybierz najpierw zawodnika z listy.'
                : undefined

          return (
            <button
              key={filter.mode}
              className={`rounded-full border px-3 py-2 text-sm font-semibold transition ${
                isActive
                  ? 'border-emerald-300/70 bg-emerald-300/15 text-white'
                  : 'border-white/10 bg-white/5 text-slate-300 hover:border-white/25 hover:text-white disabled:cursor-not-allowed disabled:opacity-45 disabled:hover:border-white/10 disabled:hover:text-slate-300'
              }`}
              type="button"
              aria-pressed={isActive && !isDisabled}
              disabled={isDisabled}
              title={isDisabled ? disabledTitle : undefined}
              onClick={() => setRequestedFilterMode(filter.mode)}
            >
              {filter.label}
            </button>
          )
        })}
      </div>

      {series.length === 0 || chartRows.length === 0 ? (
        <div className="mt-6 flex min-h-64 items-center justify-center px-2 py-8 text-center">
          <div className="max-w-md">
            <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-emerald-300/10 text-emerald-300">
              <BarChart3 className="h-6 w-6" aria-hidden="true" />
            </div>
            <p className="mt-5 font-display text-2xl text-white">Brak przebiegu tabeli</p>
            <p className="mt-3 text-sm leading-6 text-slate-400">
              Po rozliczeniu meczów pojawi się tu animowana historia miejsc wszystkich graczy.
            </p>
          </div>
        </div>
      ) : (
        <>
          <div ref={scrollRef} className="mt-6 overflow-x-auto pb-3">
            <div style={{ width: chartWidth ?? '100%', height: chartHeight }}>
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={chartRows} margin={{ top: 24, right: 34, bottom: 72, left: 6 }}>
                  <CartesianGrid stroke="rgba(148, 163, 184, 0.16)" strokeDasharray="4 4" />
                  <XAxis
                    dataKey="matchLabel"
                    angle={-58}
                    height={88}
                    interval={0}
                    tick={(props) => (
                      <text
                        x={props.x}
                        y={props.y}
                        dy={16}
                        fill="rgb(148 163 184)"
                        fontSize={11}
                        textAnchor="end"
                        transform={`rotate(-58 ${props.x} ${props.y})`}
                      >
                        {String(props.payload.value)}
                      </text>
                    )}
                    tickLine={{ stroke: 'rgba(148, 163, 184, 0.28)' }}
                    axisLine={{ stroke: 'rgba(148, 163, 184, 0.22)' }}
                  />
                  <YAxis
                    allowDecimals={false}
                    width={46}
                    domain={[1, maxPosition]}
                    reversed
                    ticks={yTicks}
                    tickFormatter={(value) => `#${value}`}
                    tick={{ fill: 'rgb(148 163 184)', fontSize: 12 }}
                    tickLine={{ stroke: 'rgba(148, 163, 184, 0.28)' }}
                    axisLine={{ stroke: 'rgba(148, 163, 184, 0.22)' }}
                  />
                  <Tooltip content={(props) => <CustomTooltip {...props} />} />
                  {playerLines.map((player) => {
                    const isFocused = focusedUserIdSet.has(player.userId)
                    const isDimmed = hasFocusedLine && !isFocused

                    return (
                      <Line
                        key={player.userId}
                        type="linear"
                        dataKey={player.userId}
                        name={player.displayName}
                        stroke={player.color}
                        strokeWidth={isFocused ? 4.5 : 2}
                        strokeOpacity={isDimmed ? 0.14 : 0.9}
                        dot={showDots ? { r: isFocused ? 4 : 2.5, strokeWidth: 0, fill: player.color } : false}
                        activeDot={{ r: 5, strokeWidth: 0, fill: player.color }}
                        connectNulls
                        isAnimationActive={!prefersReducedMotion}
                        animationDuration={prefersReducedMotion ? 0 : 900}
                        onClick={() => toggleSelectedPlayer(player.userId)}
                      />
                    )
                  })}
                </LineChart>
              </ResponsiveContainer>
            </div>
          </div>

          <div className="mt-4 flex flex-wrap gap-2">
            {playerLines.map((player) => {
              const isSelected = selectedUserIdSet.has(player.userId)
              const isFocused = focusedUserIdSet.has(player.userId)
              const isDimmed = hasFocusedLine && !isFocused

              return (
                <button
                  key={player.userId}
                  className={`inline-flex max-w-full items-center gap-2 rounded-full border px-3 py-2 text-sm transition ${
                    isSelected
                      ? 'border-emerald-300/70 bg-emerald-300/15 text-white'
                      : 'border-white/10 bg-white/5 text-slate-300 hover:border-white/25 hover:text-white'
                  } ${isDimmed ? 'opacity-45' : ''}`}
                  type="button"
                  aria-pressed={isSelected}
                  onClick={() => toggleSelectedPlayer(player.userId)}
                >
                  <span className="h-2.5 w-2.5 shrink-0 rounded-full" style={{ backgroundColor: player.color }} />
                  <UserAvatar displayName={player.displayName} avatarUrl={player.avatarUrl} size="sm" />
                  <span className="truncate">{player.displayName}</span>
                  {player.isCurrentUser ? <span className="text-xs text-emerald-200">Ty</span> : null}
                </button>
              )
            })}
          </div>
        </>
      )}
    </section>
  )
}
