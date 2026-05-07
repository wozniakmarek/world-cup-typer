import { useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { adminApi, teamsApi } from '../../api/services'
import { getErrorMessage } from '../../api/client'
import type { AdminMatch, MatchPhase, MatchStatus } from '../../api/types'
import { formatKickoff, fromDateTimeLocalValue, toDateTimeLocalValue } from '../../app/formatters'
import { FormField } from '../../components/FormField'
import { Panel } from '../../components/Panel'
import { SectionHeading } from '../../components/SectionHeading'
import { StatusPill } from '../../components/StatusPill'
import { buttonClassName, inputClassName, secondaryButtonClassName } from '../../styles/ui'

const phaseOptions: MatchPhase[] = ['GroupStage', 'RoundOf32', 'RoundOf16', 'QuarterFinal', 'SemiFinal', 'ThirdPlace', 'Final']
const statusOptions: MatchStatus[] = ['Scheduled', 'InProgress', 'Finished', 'Settled', 'Cancelled']

const createEmptyMatchForm = () => ({
  externalId: '',
  matchNumber: 1,
  phase: 'GroupStage' as MatchPhase,
  groupName: 'A',
  homeTeamId: '',
  awayTeamId: '',
  homeSlotRule: '',
  awaySlotRule: '',
  kickoffTimeUtc: new Date().toISOString().slice(0, 16),
  venue: '',
  status: 'Scheduled' as MatchStatus,
})

export const AdminMatchesPage = () => {
  const queryClient = useQueryClient()
  const teamsQuery = useQuery({ queryKey: ['teams'], queryFn: teamsApi.getAll })
  const matchesQuery = useQuery({ queryKey: ['admin', 'matches'], queryFn: adminApi.getMatches })
  const [selectedMatch, setSelectedMatch] = useState<AdminMatch | null>(null)
  const [matchForm, setMatchForm] = useState(createEmptyMatchForm())
  const [resultForm, setResultForm] = useState({
    homeScore90: '0',
    awayScore90: '0',
    homeScoreFinal: '',
    awayScoreFinal: '',
  })
  const [feedback, setFeedback] = useState<string | null>(null)

  const teams = teamsQuery.data ?? []

  const refreshData = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['admin', 'matches'] }),
      queryClient.invalidateQueries({ queryKey: ['matches'] }),
      queryClient.invalidateQueries({ queryKey: ['ranking'] }),
      queryClient.invalidateQueries({ queryKey: ['ranking', 'top'] }),
      queryClient.invalidateQueries({ queryKey: ['ranking', 'me'] }),
      queryClient.invalidateQueries({ queryKey: ['ranking', 'progress'] }),
    ])
  }

  const createMutation = useMutation({
    mutationFn: () =>
      adminApi.createMatch({
        ...matchForm,
        kickoffTimeUtc: fromDateTimeLocalValue(matchForm.kickoffTimeUtc),
      }),
    onSuccess: async () => {
      setFeedback('Mecz został dodany.')
      setMatchForm(createEmptyMatchForm())
      await refreshData()
    },
    onError: (error) => setFeedback(getErrorMessage(error)),
  })

  const updateMutation = useMutation({
    mutationFn: () => {
      if (!selectedMatch) {
        throw new Error('Najpierw wybierz mecz do edycji.')
      }

      return adminApi.updateMatch(selectedMatch.id, {
        ...matchForm,
        kickoffTimeUtc: fromDateTimeLocalValue(matchForm.kickoffTimeUtc),
      })
    },
    onSuccess: async () => {
      setFeedback('Mecz został zaktualizowany.')
      await refreshData()
    },
    onError: (error) => setFeedback(getErrorMessage(error)),
  })

  const resultMutation = useMutation({
    mutationFn: () => {
      if (!selectedMatch) {
        throw new Error('Najpierw wybierz mecz do wpisania wyniku.')
      }

      return adminApi.setMatchResult(selectedMatch.id, {
        homeScore90: Number(resultForm.homeScore90),
        awayScore90: Number(resultForm.awayScore90),
        homeScoreFinal: resultForm.homeScoreFinal ? Number(resultForm.homeScoreFinal) : null,
        awayScoreFinal: resultForm.awayScoreFinal ? Number(resultForm.awayScoreFinal) : null,
        winnerTeamId: null,
      })
    },
    onSuccess: async () => {
      setFeedback('Wynik meczu został zapisany.')
      await refreshData()
    },
    onError: (error) => setFeedback(getErrorMessage(error)),
  })

  const settleMutation = useMutation({
    mutationFn: (matchId: string) => adminApi.settleMatch(matchId),
    onSuccess: async () => {
      setFeedback('Mecz został rozliczony.')
      await refreshData()
    },
    onError: (error) => setFeedback(getErrorMessage(error)),
  })

  const recalculateMutation = useMutation({
    mutationFn: () => adminApi.recalculateRanking(),
    onSuccess: async () => {
      setFeedback('Ranking został przeliczony.')
      await refreshData()
    },
    onError: (error) => setFeedback(getErrorMessage(error)),
  })

  const sortedMatches = matchesQuery.data ?? []

  const selectMatch = (match: AdminMatch) => {
    setSelectedMatch(match)
    setMatchForm({
      externalId: '',
      matchNumber: match.matchNumber,
      phase: match.phase,
      groupName: match.groupName || '',
      homeTeamId: match.homeTeam.id,
      awayTeamId: match.awayTeam.id,
      homeSlotRule: '',
      awaySlotRule: '',
      kickoffTimeUtc: toDateTimeLocalValue(match.kickoffTimeUtc),
      venue: match.venue || '',
      status: match.status,
    })
    setResultForm({
      homeScore90: String(match.homeScore90 ?? 0),
      awayScore90: String(match.awayScore90 ?? 0),
      homeScoreFinal: '',
      awayScoreFinal: '',
    })
  }

  const handleMatchSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setFeedback(null)

    if (selectedMatch) {
      await updateMutation.mutateAsync()
      return
    }

    await createMutation.mutateAsync()
  }

  const handleResultSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!selectedMatch) return
    setFeedback(null)
    await resultMutation.mutateAsync()
  }

  return (
    <div className="space-y-6">
      <SectionHeading
        eyebrow="Admin • Mecze"
        title="Zarządzanie terminarzem"
        description="Dodawaj mecze, wpisuj wyniki po 90 minutach i rozliczaj ranking po zakończonych spotkaniach."
      />

      {feedback ? <Panel className="bg-sky-500/10 text-sm text-sky-100">{feedback}</Panel> : null}

      <div className="grid gap-6 xl:grid-cols-2">
        <Panel>
          <form className="grid gap-4" onSubmit={(event) => void handleMatchSubmit(event)}>
            <p className="font-display text-2xl uppercase text-white">{selectedMatch ? 'Edytuj mecz' : 'Dodaj mecz'}</p>
            <div className="grid gap-4 md:grid-cols-2">
              <FormField label="Numer meczu">
                <input type="number" className={inputClassName} value={matchForm.matchNumber} onChange={(event) => setMatchForm((current) => ({ ...current, matchNumber: Number(event.target.value) }))} />
              </FormField>
              <FormField label="Faza">
                <select className={inputClassName} value={matchForm.phase} onChange={(event) => setMatchForm((current) => ({ ...current, phase: event.target.value as MatchPhase }))}>
                  {phaseOptions.map((phase) => (
                    <option key={phase} value={phase}>{phase}</option>
                  ))}
                </select>
              </FormField>
              <FormField label="Drużyna gospodarzy">
                <select className={inputClassName} value={matchForm.homeTeamId} onChange={(event) => setMatchForm((current) => ({ ...current, homeTeamId: event.target.value }))}>
                  <option value="">Wybierz drużynę</option>
                  {teams.map((team) => <option key={team.id} value={team.id}>{team.name}</option>)}
                </select>
              </FormField>
              <FormField label="Drużyna gości">
                <select className={inputClassName} value={matchForm.awayTeamId} onChange={(event) => setMatchForm((current) => ({ ...current, awayTeamId: event.target.value }))}>
                  <option value="">Wybierz drużynę</option>
                  {teams.map((team) => <option key={team.id} value={team.id}>{team.name}</option>)}
                </select>
              </FormField>
              <FormField label="Grupa">
                <input className={inputClassName} value={matchForm.groupName} onChange={(event) => setMatchForm((current) => ({ ...current, groupName: event.target.value }))} />
              </FormField>
              <FormField label="Status">
                <select className={inputClassName} value={matchForm.status} onChange={(event) => setMatchForm((current) => ({ ...current, status: event.target.value as MatchStatus }))}>
                  {statusOptions.map((status) => <option key={status} value={status}>{status}</option>)}
                </select>
              </FormField>
              <FormField label="Kickoff">
                <input type="datetime-local" className={inputClassName} value={matchForm.kickoffTimeUtc} onChange={(event) => setMatchForm((current) => ({ ...current, kickoffTimeUtc: event.target.value }))} />
              </FormField>
              <FormField label="Stadion / venue">
                <input className={inputClassName} value={matchForm.venue} onChange={(event) => setMatchForm((current) => ({ ...current, venue: event.target.value }))} />
              </FormField>
            </div>
            <div className="flex flex-wrap gap-3">
              <button className={buttonClassName} type="submit">{selectedMatch ? 'Zapisz zmiany' : 'Dodaj mecz'}</button>
              {selectedMatch ? (
                <button type="button" className={secondaryButtonClassName} onClick={() => { setSelectedMatch(null); setMatchForm(createEmptyMatchForm()) }}>
                  Wyczyść formularz
                </button>
              ) : null}
            </div>
          </form>
        </Panel>

        <Panel>
          <form className="grid gap-4" onSubmit={(event) => void handleResultSubmit(event)}>
            <p className="font-display text-2xl uppercase text-white">Wpisz wynik / rozlicz</p>
            {selectedMatch ? (
              <>
                <p className="text-sm text-slate-400">
                  Wybrany mecz: <strong className="text-white">{selectedMatch.homeTeam.name} vs {selectedMatch.awayTeam.name}</strong>
                </p>
                <div className="grid gap-4 md:grid-cols-2">
                  <FormField label={`${selectedMatch.homeTeam.name} (90 min)`}>
                    <input type="number" className={inputClassName} value={resultForm.homeScore90} onChange={(event) => setResultForm((current) => ({ ...current, homeScore90: event.target.value }))} />
                  </FormField>
                  <FormField label={`${selectedMatch.awayTeam.name} (90 min)`}>
                    <input type="number" className={inputClassName} value={resultForm.awayScore90} onChange={(event) => setResultForm((current) => ({ ...current, awayScore90: event.target.value }))} />
                  </FormField>
                </div>
                <div className="flex flex-wrap gap-3">
                  <button className={buttonClassName} type="submit">Zapisz wynik</button>
                  <button type="button" className={secondaryButtonClassName} onClick={() => void settleMutation.mutateAsync(selectedMatch.id)}>
                    Rozlicz mecz
                  </button>
                  <button type="button" className={secondaryButtonClassName} onClick={() => void recalculateMutation.mutateAsync()}>
                    Przelicz ranking
                  </button>
                </div>
              </>
            ) : (
              <p className="text-sm text-slate-400">Wybierz mecz z listy poniżej, aby wpisać wynik po 90 minutach albo go rozliczyć.</p>
            )}
          </form>
        </Panel>
      </div>

      <Panel className="space-y-4">
        <p className="font-display text-2xl uppercase text-white">Lista meczów</p>
        <div className="space-y-3">
          {sortedMatches.map((match) => (
            <div key={match.id} className="flex flex-col gap-3 rounded-3xl bg-slate-950/50 px-4 py-4 lg:flex-row lg:items-center lg:justify-between">
              <div>
                <p className="font-semibold text-white">
                  #{match.matchNumber} • {match.homeTeam.name} vs {match.awayTeam.name}
                </p>
                <p className="text-sm text-slate-400">
                  {formatKickoff(match.kickoffTimeUtc)} • Typów: {match.predictionsCount}
                </p>
              </div>
              <div className="flex flex-wrap items-center gap-3">
                <StatusPill status={match.status} isSettled={match.isSettled} />
                <button type="button" className={secondaryButtonClassName} onClick={() => selectMatch(match)}>
                  Edytuj
                </button>
                {!match.isSettled ? (
                  <button type="button" className={secondaryButtonClassName} onClick={() => void settleMutation.mutateAsync(match.id)}>
                    Rozlicz
                  </button>
                ) : null}
              </div>
            </div>
          ))}
        </div>
      </Panel>
    </div>
  )
}
