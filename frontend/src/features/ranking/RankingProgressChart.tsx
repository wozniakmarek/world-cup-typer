import { useEffect, useMemo, useRef } from 'react'
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
import type { RankingProgressSeries } from '../../api/types'
import { Panel } from '../../components/Panel'
import { UserAvatar } from '../../components/UserAvatar'

const playerColors = [
  '#38bdf8',
  '#fb923c',
  '#4ade80',
  '#f87171',
  '#a78bfa',
  '#f472b6',
  '#22d3ee',
  '#facc15',
  '#c084fc',
  '#2dd4bf',
  '#e879f9',
  '#60a5fa',
  '#f59e0b',
  '#34d399',
  '#fb7185',
  '#818cf8',
  '#d946ef',
  '#14b8a6',
  '#f97316',
  '#84cc16',
]

type ChartRow = {
  matchId: string
  matchNumber: number
  matchLabel: string
  [userId: string]: string | number | null
}

type TooltipValue = number | string | Array<number | string>
type TooltipName = number | string

const buildChartRows = (series: RankingProgressSeries[]) => {
  const rowsByMatch = new Map<string, ChartRow>()

  for (const player of series) {
    for (const point of player.points) {
      const existing = rowsByMatch.get(point.matchId)
      const row = existing ?? {
        matchId: point.matchId,
        matchNumber: point.matchNumber,
        matchLabel: point.matchLabel,
      }

      row[player.userId] = point.totalPoints
      rowsByMatch.set(point.matchId, row)
    }
  }

  return [...rowsByMatch.values()].sort((first, second) => first.matchNumber - second.matchNumber)
}

const buildPlayerLines = (series: RankingProgressSeries[]) =>
  series.map((player, index) => ({
    userId: player.userId,
    displayName: player.displayName,
    avatarUrl: player.avatarUrl,
    isCurrentUser: player.isCurrentUser,
    color: playerColors[index % playerColors.length],
  }))

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
    .sort((first, second) => Number(second.value) - Number(first.value))

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
            <span className="font-semibold text-white">{item.value}</span>
          </div>
        ))}
      </div>
    </div>
  )
}

export const RankingProgressChart = ({
  series,
  focusedUserIds,
  onFocusedUserIdsChange,
}: {
  series: RankingProgressSeries[]
  focusedUserIds: string[]
  onFocusedUserIdsChange: (userIds: string[]) => void
}) => {
  const scrollRef = useRef<HTMLDivElement>(null)

  const chartRows = useMemo(() => buildChartRows(series), [series])
  const playerLines = useMemo(() => buildPlayerLines(series), [series])
  const focusedUserIdSet = useMemo(() => new Set(focusedUserIds), [focusedUserIds])

  const matchCount = chartRows.length

  // ≤15 matches: fill container width (no horizontal scroll on mobile)
  // >15 matches: fixed per-match width with horizontal scrolling
  const needsScroll = matchCount > 15
  const perMatchWidth = matchCount > 60 ? 40 : matchCount > 30 ? 52 : 72
  const chartWidth = needsScroll ? Math.max(600, matchCount * perMatchWidth) : undefined

  // Auto-scroll to latest only when the chart overflows
  useEffect(() => {
    if (scrollRef.current && needsScroll) {
      scrollRef.current.scrollLeft = scrollRef.current.scrollWidth
    }
  }, [chartRows.length, needsScroll])

  // Y-axis: 5-pt intervals for small ranges, 10-pt for large; height grows with range
  const allValues = chartRows.flatMap((row) =>
    playerLines.map((p) => row[p.userId]),
  ).filter((v): v is number => typeof v === 'number')
  const maxValue = allValues.length > 0 ? Math.max(...allValues) : 10
  const yDomainMin = 0
  const tickInterval = maxValue > 50 ? 10 : 5
  const yDomainMax = Math.ceil(maxValue / tickInterval) * tickInterval
  const yTicks = Array.from(
    { length: yDomainMax / tickInterval + 1 },
    (_, i) => i * tickInterval,
  )

  // 40px per tick interval + margins; smaller min height when few matches
  const CHART_MARGIN_TOP = 24
  const CHART_MARGIN_BOTTOM = 72
  const PX_PER_INTERVAL = 40
  const chartHeight = Math.max(
    needsScroll ? 400 : 260,
    (yDomainMax / tickInterval) * PX_PER_INTERVAL + CHART_MARGIN_TOP + CHART_MARGIN_BOTTOM,
  )

  const showDots = matchCount <= 40
  const hasFocusedLine = playerLines.some((player) => focusedUserIdSet.has(player.userId))

  const toggleFocusedPlayer = (userId: string) => {
    onFocusedUserIdsChange(
      focusedUserIdSet.has(userId)
        ? focusedUserIds.filter((focusedUserId) => focusedUserId !== userId)
        : [...focusedUserIds, userId],
    )
  }

  return (
    <Panel className="space-y-5">
      <div className="flex flex-col gap-3 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="font-display text-2xl uppercase text-white">Progres punktów po meczach</p>
          <p className="mt-1 text-sm text-slate-400">
            Kliknij zawodników w tabeli albo legendzie, żeby wyróżnić kilka linii naraz.
          </p>
        </div>
        {hasFocusedLine ? (
          <button
            className="self-start rounded-full border border-white/10 px-4 py-2 text-sm font-semibold text-slate-200 transition hover:border-emerald-400/50 hover:text-white lg:self-auto"
            type="button"
            onClick={() => onFocusedUserIdsChange([])}
          >
            Pokaż wszystkich
          </button>
        ) : null}
      </div>

      <div ref={scrollRef} className="overflow-x-auto pb-3">
        <div style={{ width: chartWidth ?? '100%', height: chartHeight }}>
          <ResponsiveContainer width="100%" height="100%">
            <LineChart
              data={chartRows}
              margin={{ top: CHART_MARGIN_TOP, right: 36, bottom: CHART_MARGIN_BOTTOM, left: 4 }}
            >
              <CartesianGrid stroke="rgba(148, 163, 184, 0.16)" strokeDasharray="4 4" />
              <XAxis
                dataKey="matchLabel"
                angle={-58}
                height={88}
                interval={0}
                tick={(props) => {
                  return (
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
                  )
                }}
                tickLine={{ stroke: 'rgba(148, 163, 184, 0.28)' }}
                axisLine={{ stroke: 'rgba(148, 163, 184, 0.22)' }}
              />
              <YAxis
                allowDecimals={false}
                width={42}
                domain={[yDomainMin, yDomainMax]}
                ticks={yTicks}
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
                    type="monotone"
                    dataKey={player.userId}
                    name={player.displayName}
                    stroke={player.color}
                    strokeWidth={isFocused ? 4.5 : 2}
                    strokeOpacity={isDimmed ? 0.16 : 0.92}
                    dot={showDots ? { r: isFocused ? 4 : 2.6, strokeWidth: 0, fill: player.color } : false}
                    activeDot={{ r: 5, strokeWidth: 0, fill: player.color }}
                    connectNulls
                    isAnimationActive={false}
                    onClick={() => toggleFocusedPlayer(player.userId)}
                  />
                )
              })}
            </LineChart>
          </ResponsiveContainer>
        </div>
      </div>

      <div className="flex flex-wrap gap-2">
        {playerLines.map((player) => {
          const isFocused = focusedUserIdSet.has(player.userId)
          const isDimmed = hasFocusedLine && !isFocused

          return (
            <button
              key={player.userId}
              className={`inline-flex max-w-full items-center gap-2 rounded-full border px-3 py-2 text-sm transition ${
                isFocused
                  ? 'border-emerald-300/70 bg-emerald-300/12 text-white'
                  : 'border-white/10 bg-white/5 text-slate-300 hover:border-white/25 hover:text-white'
              } ${isDimmed ? 'opacity-45' : ''}`}
              type="button"
              aria-pressed={isFocused}
              onClick={() => toggleFocusedPlayer(player.userId)}
            >
              <span className="h-2.5 w-2.5 shrink-0 rounded-full" style={{ backgroundColor: player.color }} />
              <UserAvatar displayName={player.displayName} avatarUrl={player.avatarUrl} size="sm" />
              <span className="truncate">{player.displayName}</span>
              {player.isCurrentUser ? <span className="text-xs text-emerald-200">Ty</span> : null}
            </button>
          )
        })}
      </div>
    </Panel>
  )
}
