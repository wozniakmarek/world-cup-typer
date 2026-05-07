import { format } from 'date-fns'
import type { MatchStatus, PredictionSummary } from '../api/types'

export const formatKickoff = (value: string) => format(new Date(value), 'dd.MM HH:mm')

export const formatLongDate = (value: string) => format(new Date(value), 'dd.MM.yyyy HH:mm')

export const toDateTimeLocalValue = (value: string) => {
  const date = new Date(value)
  const offset = date.getTimezoneOffset()
  return new Date(date.getTime() - offset * 60_000).toISOString().slice(0, 16)
}

export const fromDateTimeLocalValue = (value: string) => new Date(value).toISOString()

export const matchStatusLabel: Record<MatchStatus, string> = {
  Scheduled: 'Zaplanowany',
  InProgress: 'Trwa',
  Finished: 'Po 90 min',
  Settled: 'Rozliczony',
  Cancelled: 'Anulowany',
}

export const getPredictionLabel = (prediction?: PredictionSummary | null) =>
  prediction ? `${prediction.predictedHomeScore}:${prediction.predictedAwayScore}` : 'Brak typu'

export const getResultBadgeClass = (status: MatchStatus, isSettled: boolean) => {
  if (isSettled || status === 'Settled') {
    return 'bg-emerald-500/15 text-emerald-300 ring-1 ring-inset ring-emerald-500/30'
  }

  if (status === 'Finished' || status === 'InProgress') {
    return 'bg-amber-500/15 text-amber-200 ring-1 ring-inset ring-amber-500/30'
  }

  if (status === 'Cancelled') {
    return 'bg-rose-500/15 text-rose-200 ring-1 ring-inset ring-rose-500/30'
  }

  return 'bg-sky-500/15 text-sky-200 ring-1 ring-inset ring-sky-500/30'
}
