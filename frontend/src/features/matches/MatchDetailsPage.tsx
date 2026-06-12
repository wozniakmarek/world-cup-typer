import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { getErrorMessage } from '../../api/client'
import { matchesApi } from '../../api/services'
import { canEditMatchPrediction, formatLongDate, formatTeamDisplayName, translateTeamName } from '../../app/formatters'
import { EmptyState } from '../../components/EmptyState'
import { FormField } from '../../components/FormField'
import { InlineAlert } from '../../components/InlineAlert'
import { Panel } from '../../components/Panel'
import { QueryState } from '../../components/QueryState'
import { SectionHeading } from '../../components/SectionHeading'
import { StatusPill } from '../../components/StatusPill'
import { buttonClassName, inputClassName, secondaryButtonClassName } from '../../styles/ui'

const parseScoreValue = (value: string) => {
  const normalized = value.trim()

  if (!/^\d+$/.test(normalized)) {
    return null
  }

  return Number(normalized)
}

export const MatchDetailsPage = () => {
  const { matchId } = useParams()
  const queryClient = useQueryClient()
  const [homeScore, setHomeScore] = useState('')
  const [awayScore, setAwayScore] = useState('')
  const [feedback, setFeedback] = useState<{ tone: 'success' | 'error'; message: string } | null>(null)

  const matchQuery = useQuery({
    queryKey: ['match', matchId],
    queryFn: () => matchesApi.getById(matchId!),
    enabled: Boolean(matchId),
  })

  const predictionsQuery = useQuery({
    queryKey: ['match-predictions', matchId],
    queryFn: () => matchesApi.getPredictions(matchId!),
    enabled: Boolean(matchId),
  })

  useEffect(() => {
    setFeedback(null)
  }, [matchId])

  useEffect(() => {
    if (matchQuery.data?.myPrediction) {
      setHomeScore(String(matchQuery.data.myPrediction.predictedHomeScore))
      setAwayScore(String(matchQuery.data.myPrediction.predictedAwayScore))
      return
    }

    setHomeScore('')
    setAwayScore('')
  }, [matchId, matchQuery.data?.myPrediction])

  const savePredictionMutation = useMutation({
    mutationFn: async () => {
      if (!matchId) {
        return null
      }

      const parsedHomeScore = parseScoreValue(homeScore)
      const parsedAwayScore = parseScoreValue(awayScore)

      if (parsedHomeScore == null || parsedAwayScore == null) {
        throw new Error('Wpisz nieujemne liczby całkowite dla obu drużyn.')
      }

      const payload = {
        predictedHomeScore: parsedHomeScore,
        predictedAwayScore: parsedAwayScore,
      }

      if (matchQuery.data?.myPrediction) {
        return matchesApi.updatePrediction(matchId, payload)
      }

      return matchesApi.createPrediction(matchId, payload)
    },
    onSuccess: async () => {
      setFeedback({ tone: 'success', message: 'Typ zapisany.' })
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['match', matchId] }),
        queryClient.invalidateQueries({ queryKey: ['match-predictions', matchId] }),
        queryClient.invalidateQueries({ queryKey: ['matches'] }),
        queryClient.invalidateQueries({ queryKey: ['predictions', 'mine'] }),
      ])
    },
    onError: (error) => setFeedback({ tone: 'error', message: getErrorMessage(error) }),
  })

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setFeedback(null)

    if (!canEditPrediction) {
      setFeedback({ tone: 'error', message: 'Nie można zmienić typu po rozpoczęciu meczu.' })
      return
    }

    const parsedHomeScore = parseScoreValue(homeScore)
    const parsedAwayScore = parseScoreValue(awayScore)

    if (parsedHomeScore == null || parsedAwayScore == null) {
      setFeedback({ tone: 'error', message: 'Wpisz nieujemne liczby całkowite dla obu drużyn.' })
      return
    }

    await savePredictionMutation.mutateAsync()
  }

  const match = matchQuery.data
  const canEditPrediction = match ? canEditMatchPrediction(match) : false
  const isLocked = Boolean(match && !canEditPrediction && !match.isSettled)
  const scoreAvailable = match ? match.homeScore90 != null || match.awayScore90 != null : false
  const predictionInfoMessage = match?.canViewPredictions
    ? 'Kickoff minął, więc typy pozostałych graczy są już widoczne.'
    : 'Przed kickoffem widzisz wyłącznie swój typ.'

  return (
    <div className="space-y-6">
      <QueryState
        isLoading={matchQuery.isLoading}
        isError={matchQuery.isError}
        errorMessage={getErrorMessage(matchQuery.error)}
        isEmpty={!match}
        emptyTitle="Nie znaleziono meczu"
        emptyDescription="To spotkanie nie jest dostępne albo zostało usunięte z terminarza."
        loadingTitle="Ładowanie meczu"
        loadingDescription="Pobieram szczegóły spotkania i Twój aktualny typ."
      >
        {match ? (
          <>
            <SectionHeading
              eyebrow="Szczegóły meczu"
              title={`${translateTeamName(match.homeTeam.name)} vs ${translateTeamName(match.awayTeam.name)}`}
              description={`Kickoff: ${formatLongDate(match.kickoffTimeUtc)}${match.venue ? ` • ${match.venue}` : ''}`}
            />

            <Panel className="space-y-5">
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div className="flex-1 rounded-3xl bg-slate-950/50 px-4 py-4">
                  <div className="grid gap-3 md:grid-cols-[1fr_auto_1fr] md:items-center">
                    <div>
                      <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Gospodarze</p>
                      <p className="mt-1 font-display text-2xl uppercase text-white md:text-3xl">
                        {formatTeamDisplayName(match.homeTeam)}
                      </p>
                    </div>
                    <p className="text-center font-display text-sm uppercase tracking-[0.3em] text-slate-500">vs</p>
                    <div className="md:text-right">
                      <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Goście</p>
                      <p className="mt-1 font-display text-2xl uppercase text-white md:text-3xl">
                        {formatTeamDisplayName(match.awayTeam)}
                      </p>
                    </div>
                  </div>
                </div>
                <StatusPill
                  status={match.status}
                  isSettled={match.isSettled}
                  kickoffTimeUtc={match.kickoffTimeUtc}
                />
              </div>

              {scoreAvailable ? (
                <InlineAlert
                  tone={match.isSettled ? 'success' : 'info'}
                  title="Wynik po 90 minutach"
                  message={`${match.homeScore90 ?? '-'} : ${match.awayScore90 ?? '-'}${match.isSettled ? ` • Twoje punkty: ${match.myPoints ?? 0}` : ''}`}
                />
              ) : null}

              {canEditPrediction ? (
                <InlineAlert
                  tone="info"
                  title="Typowanie jest otwarte"
                  message="Możesz zapisać nowy typ albo poprawić obecny wynik do momentu kickoffu."
                />
              ) : null}

              {isLocked ? (
                <InlineAlert
                  tone="warning"
                  title="Typowanie zostało zablokowane"
                  message="Po kickoffie backend blokuje edycję typu niezależnie od stanu formularza."
                />
              ) : null}

              {match.isSettled ? (
                <InlineAlert
                  tone="success"
                  title="Mecz rozliczony"
                  message={`To spotkanie zostało rozliczone. Twój wynik punktowy: ${match.myPoints ?? 0}.`}
                />
              ) : null}

              <form className="grid gap-4 md:grid-cols-[1fr_1fr_auto]" onSubmit={(event) => void handleSubmit(event)}>
                <FormField label={`Typ: ${translateTeamName(match.homeTeam.name)}`}>
                  <input
                    type="number"
                    min="0"
                    step="1"
                    inputMode="numeric"
                    value={homeScore}
                    onChange={(event) => setHomeScore(event.target.value)}
                    disabled={!canEditPrediction || savePredictionMutation.isPending}
                    className={inputClassName}
                  />
                </FormField>
                <FormField label={`Typ: ${translateTeamName(match.awayTeam.name)}`}>
                  <input
                    type="number"
                    min="0"
                    step="1"
                    inputMode="numeric"
                    value={awayScore}
                    onChange={(event) => setAwayScore(event.target.value)}
                    disabled={!canEditPrediction || savePredictionMutation.isPending}
                    className={inputClassName}
                  />
                </FormField>
                <div className="flex items-end">
                  <button
                    type="submit"
                    disabled={!canEditPrediction || savePredictionMutation.isPending}
                    className={`${buttonClassName} w-full md:w-auto`}
                  >
                    {match.myPrediction ? 'Zapisz zmianę' : 'Zapisz typ'}
                  </button>
                </div>
              </form>

              {feedback ? <InlineAlert tone={feedback.tone} message={feedback.message} /> : null}
            </Panel>

            <Panel className="space-y-4">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <p className="font-display text-2xl uppercase text-white">Typy graczy</p>
                  <p className="text-sm text-slate-400">{predictionInfoMessage}</p>
                </div>
                <button
                  type="button"
                  onClick={() => void predictionsQuery.refetch()}
                  className={secondaryButtonClassName}
                >
                  Odśwież
                </button>
              </div>

              {predictionsQuery.isLoading ? (
                <EmptyState
                  compact
                  title="Ładowanie typów"
                  description="Pobieram listę przewidywań dla tego meczu."
                />
              ) : null}

              {predictionsQuery.isError ? (
                <InlineAlert
                  tone="error"
                  title="Nie udało się pobrać typów"
                  message={getErrorMessage(predictionsQuery.error)}
                />
              ) : null}

              {!predictionsQuery.isLoading && !predictionsQuery.isError && (predictionsQuery.data?.predictions.length ?? 0) === 0 ? (
                <EmptyState
                  compact
                  title="Brak widocznych typów"
                  description={
                    match.canViewPredictions
                      ? 'Ten mecz nie ma jeszcze zapisanych typów.'
                      : 'Pozostali gracze staną się widoczni dopiero po kickoffie.'
                  }
                />
              ) : null}

              {!predictionsQuery.isLoading && !predictionsQuery.isError && (predictionsQuery.data?.predictions.length ?? 0) > 0 ? (
                <div className="space-y-3">
                  {predictionsQuery.data?.predictions.map((prediction) => (
                    <div key={prediction.predictionId} className="flex items-center justify-between rounded-2xl bg-slate-950/50 px-4 py-3">
                      <div>
                        <p className="font-semibold text-white">{prediction.displayName}</p>
                        <p className="text-xs text-slate-400">
                          {prediction.points != null ? `Punkty: ${prediction.points}` : 'Jeszcze bez rozliczenia'}
                        </p>
                      </div>
                      <p className="font-display text-2xl text-emerald-300">
                        {prediction.predictedHomeScore}:{prediction.predictedAwayScore}
                      </p>
                    </div>
                  ))}
                </div>
              ) : null}
            </Panel>
          </>
        ) : null}
      </QueryState>
    </div>
  )
}
