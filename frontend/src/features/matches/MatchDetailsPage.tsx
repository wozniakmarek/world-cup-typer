import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { getErrorMessage } from '../../api/client'
import { matchesApi } from '../../api/services'
import { formatLongDate } from '../../app/formatters'
import { EmptyState } from '../../components/EmptyState'
import { FormField } from '../../components/FormField'
import { Panel } from '../../components/Panel'
import { SectionHeading } from '../../components/SectionHeading'
import { StatusPill } from '../../components/StatusPill'
import { buttonClassName, inputClassName, secondaryButtonClassName } from '../../styles/ui'

export const MatchDetailsPage = () => {
  const { matchId } = useParams()
  const queryClient = useQueryClient()
  const [homeScore, setHomeScore] = useState('0')
  const [awayScore, setAwayScore] = useState('0')
  const [feedback, setFeedback] = useState<string | null>(null)

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
    if (matchQuery.data?.myPrediction) {
      setHomeScore(String(matchQuery.data.myPrediction.predictedHomeScore))
      setAwayScore(String(matchQuery.data.myPrediction.predictedAwayScore))
    }
  }, [matchQuery.data?.myPrediction])

  const savePredictionMutation = useMutation({
    mutationFn: async () => {
      if (!matchId) {
        return null
      }

      const payload = {
        predictedHomeScore: Number(homeScore),
        predictedAwayScore: Number(awayScore),
      }

      if (matchQuery.data?.myPrediction) {
        return matchesApi.updatePrediction(matchId, payload)
      }

      return matchesApi.createPrediction(matchId, payload)
    },
    onSuccess: async () => {
      setFeedback('Typ zapisany.')
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['match', matchId] }),
        queryClient.invalidateQueries({ queryKey: ['matches'] }),
        queryClient.invalidateQueries({ queryKey: ['predictions', 'mine'] }),
      ])
    },
    onError: (error) => setFeedback(getErrorMessage(error)),
  })

  const match = matchQuery.data
  if (!match) {
    return <EmptyState title="Ładowanie meczu" description="Pobieram szczegóły spotkania i Twojego typu." />
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setFeedback(null)
    await savePredictionMutation.mutateAsync()
  }

  return (
    <div className="space-y-6">
      <SectionHeading
        eyebrow="Szczegóły meczu"
        title={`${match.homeTeam.name} vs ${match.awayTeam.name}`}
        description={`Kickoff: ${formatLongDate(match.kickoffTimeUtc)}${match.venue ? ` • ${match.venue}` : ''}`}
      />

      <Panel className="space-y-5">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="rounded-3xl bg-slate-950/50 px-4 py-4">
            <p className="font-display text-3xl uppercase text-white">
              {match.homeTeam.flagEmoji} {match.homeTeam.name}
            </p>
            <p className="mt-1 font-display text-3xl uppercase text-white">
              {match.awayTeam.flagEmoji} {match.awayTeam.name}
            </p>
          </div>
          <StatusPill status={match.status} isSettled={match.isSettled} />
        </div>

        {(match.homeScore90 ?? match.awayScore90) !== undefined ? (
          <div className="rounded-3xl border border-emerald-400/20 bg-emerald-400/10 px-4 py-3 text-sm text-emerald-100">
            Wynik po 90 min: <strong>{match.homeScore90 ?? '-'} : {match.awayScore90 ?? '-'}</strong>
            {match.isSettled ? ` • Twoje punkty: ${match.myPoints ?? 0}` : ''}
          </div>
        ) : null}

        <form className="grid gap-4 md:grid-cols-[1fr_1fr_auto]" onSubmit={(event) => void handleSubmit(event)}>
          <FormField label={`Typ: ${match.homeTeam.name}`}>
            <input
              type="number"
              min="0"
              value={homeScore}
              onChange={(event) => setHomeScore(event.target.value)}
              disabled={!match.canEditPrediction || savePredictionMutation.isPending}
              className={inputClassName}
            />
          </FormField>
          <FormField label={`Typ: ${match.awayTeam.name}`}>
            <input
              type="number"
              min="0"
              value={awayScore}
              onChange={(event) => setAwayScore(event.target.value)}
              disabled={!match.canEditPrediction || savePredictionMutation.isPending}
              className={inputClassName}
            />
          </FormField>
          <div className="flex items-end">
            <button type="submit" disabled={!match.canEditPrediction || savePredictionMutation.isPending} className={`${buttonClassName} w-full md:w-auto`}>
              {match.myPrediction ? 'Zapisz zmianę' : 'Zapisz typ'}
            </button>
          </div>
        </form>

        {!match.canEditPrediction ? (
          <div className="rounded-3xl border border-white/10 bg-white/5 px-4 py-3 text-sm text-slate-300">
            Typ zablokowany. Po kickoffie backend blokuje edycję niezależnie od UI.
          </div>
        ) : null}

        {feedback ? <div className="rounded-3xl bg-sky-500/15 px-4 py-3 text-sm text-sky-100">{feedback}</div> : null}
      </Panel>

      <Panel className="space-y-4">
        <div className="flex items-center justify-between gap-3">
          <div>
            <p className="font-display text-2xl uppercase text-white">Typy graczy</p>
            <p className="text-sm text-slate-400">
              {predictionsQuery.data?.canViewAllPredictions
                ? 'Kickoff minął, więc typy pozostałych graczy są widoczne.'
                : 'Przed kickoffem widzisz wyłącznie swój typ.'}
            </p>
          </div>
          <button
            type="button"
            onClick={() => void predictionsQuery.refetch()}
            className={secondaryButtonClassName}
          >
            Odśwież
          </button>
        </div>

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
      </Panel>
    </div>
  )
}
