import { useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getErrorMessage } from '../../api/client'
import { adminApi, teamsApi } from '../../api/services'
import type { AdminMatch, MatchPhase, MatchStatus } from '../../api/types'
import { formatKickoff, formatMatchContext, formatTeamDisplayName, fromDateTimeLocalValue, toDateTimeLocalValue } from '../../app/formatters'
import { FormField } from '../../components/FormField'
import { InlineAlert } from '../../components/InlineAlert'
import { Panel } from '../../components/Panel'
import { QueryState } from '../../components/QueryState'
import { SectionHeading } from '../../components/SectionHeading'
import { StatusPill } from '../../components/StatusPill'
import { buttonClassName, filterButtonClassName, inputClassName, mobileRecordClassName, secondaryButtonClassName } from '../../styles/ui'

const phaseOptions: MatchPhase[] = ['GroupStage', 'RoundOf32', 'RoundOf16', 'QuarterFinal', 'SemiFinal', 'ThirdPlace', 'Final']
const statusOptions: MatchStatus[] = ['Scheduled', 'InProgress', 'Finished', 'Settled', 'Cancelled']
const listFilters = [
  { key: 'upcoming', label: 'Nadchodzące' },
  { key: 'needsSettlement', label: 'Do rozliczenia' },
  { key: 'all', label: 'Wszystkie' },
] as const
const listFilterReferenceTime = Date.now()

const getDefaultKickoffValue = () => toDateTimeLocalValue(new Date().toISOString())

const parseAdminScoreValue = (value: string) => {
  const normalized = value.trim()

  if (!/^\d+$/.test(normalized)) {
    return null
  }

  return Number(normalized)
}

const createEmptyMatchForm = () => ({
  externalId: '',
  matchNumber: 1,
  phase: 'GroupStage' as MatchPhase,
  groupName: 'A',
  homeTeamId: '',
  awayTeamId: '',
  homeSlotRule: '',
  awaySlotRule: '',
  kickoffTimeUtc: getDefaultKickoffValue(),
  venue: '',
  status: 'Scheduled' as MatchStatus,
})

type FeedbackState = {
  tone: 'success' | 'error'
  message: string
  title?: string
}

type ResultFormState = {
  homeScore90: string
  awayScore90: string
  homeScoreFinal: string
  awayScoreFinal: string
}

type ParsedResultPayload = {
  homeScore90: number
  awayScore90: number
  homeScoreFinal: number | null
  awayScoreFinal: number | null
}

const parseResultPayload = (resultForm: ResultFormState): ParsedResultPayload => {
  const parsedHomeScore90 = parseAdminScoreValue(resultForm.homeScore90)
  const parsedAwayScore90 = parseAdminScoreValue(resultForm.awayScore90)
  const hasHomeFinalScore = resultForm.homeScoreFinal.trim().length > 0
  const hasAwayFinalScore = resultForm.awayScoreFinal.trim().length > 0
  const parsedHomeScoreFinal = hasHomeFinalScore ? parseAdminScoreValue(resultForm.homeScoreFinal) : null
  const parsedAwayScoreFinal = hasAwayFinalScore ? parseAdminScoreValue(resultForm.awayScoreFinal) : null

  if (parsedHomeScore90 == null || parsedAwayScore90 == null) {
    throw new Error('Wpisz nieujemne liczby całkowite dla wyniku po 90 minutach.')
  }

  if (hasHomeFinalScore !== hasAwayFinalScore) {
    throw new Error('Jeśli wpisujesz wynik końcowy, podaj gole obu drużyn.')
  }

  if ((hasHomeFinalScore && parsedHomeScoreFinal == null) || (hasAwayFinalScore && parsedAwayScoreFinal == null)) {
    throw new Error('Jeśli wpisujesz wynik końcowy, użyj nieujemnych liczb całkowitych.')
  }

  return {
    homeScore90: parsedHomeScore90,
    awayScore90: parsedAwayScore90,
    homeScoreFinal: parsedHomeScoreFinal,
    awayScoreFinal: parsedAwayScoreFinal,
  }
}

export const AdminMatchesPage = () => {
  const queryClient = useQueryClient()
  const teamsQuery = useQuery({ queryKey: ['teams'], queryFn: teamsApi.getAll })
  const matchesQuery = useQuery({ queryKey: ['admin', 'matches'], queryFn: adminApi.getMatches })
  const [selectedMatch, setSelectedMatch] = useState<AdminMatch | null>(null)
  const [matchForm, setMatchForm] = useState(createEmptyMatchForm())
  const [resultForm, setResultForm] = useState<ResultFormState>({
    homeScore90: '0',
    awayScore90: '0',
    homeScoreFinal: '',
    awayScoreFinal: '',
  })
  const [listFilter, setListFilter] = useState<(typeof listFilters)[number]['key']>('upcoming')
  const [feedback, setFeedback] = useState<FeedbackState | null>(null)

  const teams = teamsQuery.data ?? []
  const sortedMatches = matchesQuery.data ?? []
  const visibleMatches = sortedMatches.filter((match) => {
    const kickoffTime = new Date(match.kickoffTimeUtc).getTime()

    if (listFilter === 'upcoming') {
      return kickoffTime >= listFilterReferenceTime && !match.isSettled
    }

    if (listFilter === 'needsSettlement') {
      return kickoffTime < listFilterReferenceTime && !match.isSettled
    }

    return true
  })

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
      setFeedback({ tone: 'success', message: 'Mecz został dodany.' })
      setMatchForm(createEmptyMatchForm())
      await refreshData()
    },
    onError: (error) => setFeedback({ tone: 'error', title: 'Nie udało się dodać meczu', message: getErrorMessage(error) }),
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
      setFeedback({ tone: 'success', message: 'Mecz został zaktualizowany.' })
      await refreshData()
    },
    onError: (error) => setFeedback({ tone: 'error', title: 'Nie udało się zapisać zmian', message: getErrorMessage(error) }),
  })

  const resultMutation = useMutation({
    mutationFn: () => {
      if (!selectedMatch) {
        throw new Error('Najpierw wybierz mecz do wpisania wyniku.')
      }

      const payload = parseResultPayload(resultForm)

      return adminApi.setMatchResult(selectedMatch.id, {
        ...payload,
        winnerTeamId: null,
      })
    },
    onSuccess: async () => {
      setFeedback({ tone: 'success', message: 'Wynik meczu został zapisany.' })
      await refreshData()
    },
    onError: (error) => setFeedback({ tone: 'error', title: 'Nie udało się zapisać wyniku', message: getErrorMessage(error) }),
  })

  const settleMutation = useMutation({
    mutationFn: (matchId: string) => adminApi.settleMatch(matchId),
    onSuccess: async () => {
      setFeedback({ tone: 'success', message: 'Mecz został rozliczony.' })
      await refreshData()
    },
    onError: (error) => setFeedback({ tone: 'error', title: 'Nie udało się rozliczyć meczu', message: getErrorMessage(error) }),
  })

  const recalculateMutation = useMutation({
    mutationFn: () => adminApi.recalculateRanking(),
    onSuccess: async () => {
      setFeedback({ tone: 'success', message: 'Ranking został przeliczony.' })
      await refreshData()
    },
    onError: (error) => setFeedback({ tone: 'error', title: 'Nie udało się przeliczyć rankingu', message: getErrorMessage(error) }),
  })

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

  const clearSelection = () => {
    setSelectedMatch(null)
    setMatchForm(createEmptyMatchForm())
    setResultForm({
      homeScore90: '0',
      awayScore90: '0',
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

    try {
      parseResultPayload(resultForm)
    } catch (error) {
      setFeedback({ tone: 'error', message: getErrorMessage(error) })
      return
    }

    await resultMutation.mutateAsync()
  }

  return (
    <div className="space-y-6">
      <SectionHeading
        eyebrow="Admin / Mecze"
        title="Zarządzanie terminarzem"
        description="Dodawaj mecze, wpisuj wyniki po 90 minutach i rozliczaj ranking po zakończonych spotkaniach."
      />

      {feedback ? <InlineAlert tone={feedback.tone} title={feedback.title} message={feedback.message} /> : null}

      <QueryState
        isLoading={teamsQuery.isLoading || matchesQuery.isLoading}
        isError={teamsQuery.isError || matchesQuery.isError}
        errorMessage={getErrorMessage(teamsQuery.error ?? matchesQuery.error)}
        isEmpty={teams.length === 0 && sortedMatches.length === 0}
        emptyTitle="Brak terminarza i drużyn"
        emptyDescription="Dodaj reprezentacje albo pierwszy mecz, aby zacząć budować harmonogram turnieju."
        loadingTitle="Ładowanie panelu meczów"
        loadingDescription="Pobieram terminarz, drużyny i dane potrzebne do rozliczenia."
      >
        <div className="grid gap-6 2xl:grid-cols-[1.1fr_1.1fr_1.4fr]">
          <Panel>
            <form className="grid gap-4" onSubmit={(event) => void handleMatchSubmit(event)}>
              <div className="space-y-2">
                <p className="font-display text-2xl uppercase text-white">{selectedMatch ? 'Edytuj mecz' : 'Dodaj mecz'}</p>
                <p className="text-sm text-slate-400">Ustaw fazę, drużyny, kickoff i podstawowe informacje potrzebne do typowania.</p>
              </div>
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
                <FormField label="Stadion / miejsce">
                  <input className={inputClassName} value={matchForm.venue} onChange={(event) => setMatchForm((current) => ({ ...current, venue: event.target.value }))} />
                </FormField>
              </div>
              <div className="flex flex-wrap gap-3">
                <button className={buttonClassName} type="submit">{selectedMatch ? 'Zapisz zmiany' : 'Dodaj mecz'}</button>
                {selectedMatch ? (
                  <button type="button" className={secondaryButtonClassName} onClick={clearSelection}>
                    Wyczyść formularz
                  </button>
                ) : null}
              </div>
            </form>
          </Panel>

          <Panel>
            <form className="grid gap-4" onSubmit={(event) => void handleResultSubmit(event)}>
              <div className="space-y-2">
                <p className="font-display text-2xl uppercase text-white">Wpisz wynik / rozlicz</p>
                <p className="text-sm text-slate-400">Ta sekcja służy do wpisania wyniku po 90 minutach i uruchomienia rozliczenia meczu.</p>
              </div>
              {selectedMatch ? (
                <>
                  <div className="rounded-3xl bg-slate-950/45 px-4 py-4 text-sm text-slate-300">
                    Wybrany mecz: <strong className="text-white">{selectedMatch.homeTeam.name} vs {selectedMatch.awayTeam.name}</strong>
                  </div>
                  <div className="grid gap-4 md:grid-cols-2">
                    <FormField label={`${selectedMatch.homeTeam.name} (90 min)`}>
                      <input type="number" min="0" step="1" className={inputClassName} value={resultForm.homeScore90} onChange={(event) => setResultForm((current) => ({ ...current, homeScore90: event.target.value }))} />
                    </FormField>
                    <FormField label={`${selectedMatch.awayTeam.name} (90 min)`}>
                      <input type="number" min="0" step="1" className={inputClassName} value={resultForm.awayScore90} onChange={(event) => setResultForm((current) => ({ ...current, awayScore90: event.target.value }))} />
                    </FormField>
                    <FormField label={`${selectedMatch.homeTeam.name} (koniec meczu)`}>
                      <input type="number" min="0" step="1" className={inputClassName} value={resultForm.homeScoreFinal} onChange={(event) => setResultForm((current) => ({ ...current, homeScoreFinal: event.target.value }))} />
                    </FormField>
                    <FormField label={`${selectedMatch.awayTeam.name} (koniec meczu)`}>
                      <input type="number" min="0" step="1" className={inputClassName} value={resultForm.awayScoreFinal} onChange={(event) => setResultForm((current) => ({ ...current, awayScoreFinal: event.target.value }))} />
                    </FormField>
                  </div>
                  <p className="text-sm text-slate-400">
                    Wynik końcowy podaj tylko wtedy, gdy chcesz zapisać dane po dogrywce lub karnych. Uzupełnij wtedy oba pola.
                  </p>
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
                <div className="rounded-3xl border border-dashed border-white/10 bg-white/5 px-4 py-6 text-sm text-slate-300">
                  Wybierz mecz z listy po prawej, aby wpisać wynik po 90 minutach albo uruchomić rozliczenie.
                </div>
              )}
            </form>
          </Panel>

          <Panel className="space-y-4">
            <div className="space-y-2">
              <p className="font-display text-2xl uppercase text-white">Lista meczów</p>
              <p className="text-sm text-slate-400">Szybko wybierz spotkanie do edycji, wpisania wyniku albo rozliczenia.</p>
            </div>
            <div className="flex flex-wrap gap-2">
              {listFilters.map((filter) => (
                <button
                  key={filter.key}
                  type="button"
                  className={filterButtonClassName(listFilter === filter.key)}
                  onClick={() => setListFilter(filter.key)}
                >
                  {filter.label}
                </button>
              ))}
            </div>
            <QueryState
              isLoading={matchesQuery.isLoading}
              isError={matchesQuery.isError}
              errorMessage={getErrorMessage(matchesQuery.error)}
              isEmpty={visibleMatches.length === 0}
              emptyTitle="Brak meczów"
              emptyDescription="Dodaj pierwszy mecz, aby zacząć budować terminarz."
            >
              <div className="space-y-3">
                {visibleMatches.map((match) => (
                  <article key={match.id} className={mobileRecordClassName}>
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="font-semibold text-white">
                          {formatTeamDisplayName(match.homeTeam)} vs {formatTeamDisplayName(match.awayTeam)}
                        </p>
                        <p className="mt-1 text-sm text-slate-400">{formatMatchContext(match)}</p>
                        <p className="mt-1 text-sm text-slate-400">
                          {formatKickoff(match.kickoffTimeUtc)} / Typów: {match.predictionsCount}
                        </p>
                      </div>
                      <StatusPill status={match.status} isSettled={match.isSettled} />
                    </div>
                    <div className="mt-4 flex flex-wrap gap-3">
                      <button type="button" className={secondaryButtonClassName} onClick={() => selectMatch(match)}>
                        Edytuj
                      </button>
                      {!match.isSettled ? (
                        <button type="button" className={secondaryButtonClassName} onClick={() => void settleMutation.mutateAsync(match.id)}>
                          Rozlicz
                        </button>
                      ) : null}
                    </div>
                  </article>
                ))}
              </div>
            </QueryState>
          </Panel>
        </div>
      </QueryState>
    </div>
  )
}
