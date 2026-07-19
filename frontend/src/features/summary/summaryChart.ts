import type { FinalRankingPositionSeries } from '../../api/types'

export const finalChartColors = [
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

export type FinalChartRow = {
  matchId: string
  matchNumber: number
  matchLabel: string
  snapshotAtUtc: string
  [userId: string]: string | number | null
}

export const buildFinalChartRows = (series: FinalRankingPositionSeries[]) => {
  const rowsByMatch = new Map<string, FinalChartRow>()

  for (const player of series) {
    for (const point of player.points) {
      const existing = rowsByMatch.get(point.matchId)
      const row = existing ?? {
        matchId: point.matchId,
        matchNumber: point.matchNumber,
        matchLabel: point.matchLabel,
        snapshotAtUtc: point.snapshotAtUtc,
      }

      row[player.userId] = point.position
      rowsByMatch.set(point.matchId, row)
    }
  }

  return [...rowsByMatch.values()]
}

const getFirstAndLastPositions = (player: FinalRankingPositionSeries) => {
  const first = player.points[0]
  const last = player.points.at(-1)

  if (!first || !last) {
    return null
  }

  return { first: first.position, last: last.position }
}

const getBestMovementUserIds = (
  series: FinalRankingPositionSeries[],
  getMovement: (positions: { first: number; last: number }) => number,
) => {
  const movements = series
    .map((player) => {
      const positions = getFirstAndLastPositions(player)

      return {
        userId: player.userId,
        movement: positions ? getMovement(positions) : 0,
      }
    })
    .filter((player) => player.movement > 0)

  const bestMovement = Math.max(0, ...movements.map((player) => player.movement))

  return movements.filter((player) => player.movement === bestMovement).map((player) => player.userId)
}

export const getBiggestClimbUserIds = (series: FinalRankingPositionSeries[]) =>
  getBestMovementUserIds(series, ({ first, last }) => first - last)

export const getBiggestDropUserIds = (series: FinalRankingPositionSeries[]) =>
  getBestMovementUserIds(series, ({ first, last }) => last - first)
