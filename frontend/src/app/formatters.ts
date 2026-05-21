import { format } from 'date-fns'
import type { MatchPhase, MatchStatus, PredictionSummary, Team } from '../api/types'

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

type MatchContext = {
  phase: MatchPhase
  groupName?: string | null
}

type MatchWithTeams = MatchContext & {
  homeTeam: Team
  awayTeam: Team
}

const phaseLabels: Record<MatchPhase, string> = {
  GroupStage: 'Faza grupowa',
  RoundOf32: '1/16 finału',
  RoundOf16: '1/8 finału',
  QuarterFinal: 'Ćwierćfinał',
  SemiFinal: 'Półfinał',
  ThirdPlace: 'Mecz o 3. miejsce',
  Final: 'Finał',
}

export const formatMatchContext = (match: MatchContext) => {
  const phaseLabel = phaseLabels[match.phase]
  return match.phase === 'GroupStage' && match.groupName
    ? `${phaseLabel} · Grupa ${match.groupName}`
    : phaseLabel
}

export const formatTeamDisplayName = (team: Team) =>
  team.flagEmoji ? `${team.flagEmoji} ${team.name}` : team.name

export const hasPlaceholderTeam = (team: Team) => {
  const values = [team.name, team.shortName, team.countryCode].map((value) => value.trim().toUpperCase())

  return values.some((value) =>
    ['UNKNOWN TEAM', 'UNKNOWN', 'TBA', 'TBD', 'TO BE ANNOUNCED'].includes(value)
      || value.startsWith('WINNER GROUP ')
      || value.startsWith('RUNNER-UP GROUP '))
}

export const shouldShowMatchToPlayer = (match: MatchWithTeams) =>
  match.phase === 'GroupStage' || (!hasPlaceholderTeam(match.homeTeam) && !hasPlaceholderTeam(match.awayTeam))

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
