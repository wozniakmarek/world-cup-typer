import { useQuery } from '@tanstack/react-query'
import { matchesApi, summaryApi } from '../../api/services'
import type { FinalSummaryAvailability, MatchSummary, Team } from '../../api/types'

const finalTeamCodes = ['ARG', 'ESP']

const teamMatchesCode = (team: Team, code: string) =>
  team.shortName.toUpperCase() === code || team.countryCode.toUpperCase() === code

const matchHasTeam = (match: MatchSummary, code: string) =>
  teamMatchesCode(match.homeTeam, code) || teamMatchesCode(match.awayTeam, code)

const isArgentinaSpainFinal = (match: MatchSummary) =>
  match.phase === 'Final' && finalTeamCodes.every((code) => matchHasTeam(match, code))

const compareMatchOrder = (left: MatchSummary, right: MatchSummary) => {
  const kickoffDiff = new Date(left.kickoffTimeUtc).getTime() - new Date(right.kickoffTimeUtc).getTime()

  return kickoffDiff === 0 ? left.matchNumber - right.matchNumber : kickoffDiff
}

export const shouldShowFinalRecapCta = (
  availability: FinalSummaryAvailability | undefined,
  matches: MatchSummary[] | undefined,
) => {
  if (availability?.isReady) {
    return true
  }

  const finalMatch = matches?.find(isArgentinaSpainFinal)

  if (!finalMatch) {
    return false
  }

  if (finalMatch.isSettled) {
    return true
  }

  return !(matches ?? []).some((match) => !match.isSettled && compareMatchOrder(match, finalMatch) < 0)
}

export const useFinalRecapCtaVisibility = () => {
  const availabilityQuery = useQuery({
    queryKey: ['summary', 'final', 'availability'],
    queryFn: summaryApi.getFinalAvailability,
  })
  const matchesQuery = useQuery({
    queryKey: ['matches'],
    queryFn: matchesApi.getAll,
  })

  return shouldShowFinalRecapCta(availabilityQuery.data, matchesQuery.data)
}
