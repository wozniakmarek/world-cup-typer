import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getErrorMessage } from '../../api/client'
import { adminApi, teamsApi } from '../../api/services'
import type { Team } from '../../api/types'
import { formatTeamDisplayName, getTeamFlagEmoji, translateTeamName } from '../../app/formatters'
import { FormField } from '../../components/FormField'
import { InlineAlert } from '../../components/InlineAlert'
import { Panel } from '../../components/Panel'
import { QueryState } from '../../components/QueryState'
import { ResponsiveTable } from '../../components/ResponsiveTable'
import { SectionHeading } from '../../components/SectionHeading'
import { buttonClassName, inputClassName, mobileRecordClassName, secondaryButtonClassName } from '../../styles/ui'

const emptyTeamForm = {
  name: '',
  shortName: '',
  countryCode: '',
  flagEmoji: '',
  groupName: '',
}

type FeedbackState = {
  tone: 'success' | 'error'
  message: string
  title?: string
}

export const AdminTeamsPage = () => {
  const queryClient = useQueryClient()
  const teamsQuery = useQuery({ queryKey: ['teams'], queryFn: teamsApi.getAll })
  const [selectedTeam, setSelectedTeam] = useState<Team | null>(null)
  const [createForm, setCreateForm] = useState(emptyTeamForm)
  const [editForm, setEditForm] = useState(emptyTeamForm)
  const [feedback, setFeedback] = useState<FeedbackState | null>(null)

  useEffect(() => {
    if (!selectedTeam) {
      return
    }

    setEditForm({
      name: selectedTeam.name,
      shortName: selectedTeam.shortName,
      countryCode: selectedTeam.countryCode,
      flagEmoji: selectedTeam.flagEmoji ?? '',
      groupName: selectedTeam.groupName ?? '',
    })
  }, [selectedTeam])

  const refreshTeams = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['teams'] }),
      queryClient.invalidateQueries({ queryKey: ['matches'] }),
      queryClient.invalidateQueries({ queryKey: ['admin', 'matches'] }),
    ])
  }

  const createMutation = useMutation({
    mutationFn: () =>
      adminApi.createTeam({
        ...createForm,
        flagEmoji: createForm.flagEmoji || undefined,
        groupName: createForm.groupName || undefined,
      }),
    onSuccess: async () => {
      setFeedback({ tone: 'success', message: 'Drużyna została dodana.' })
      setCreateForm(emptyTeamForm)
      await refreshTeams()
    },
    onError: (error) => setFeedback({ tone: 'error', title: 'Nie udało się dodać drużyny', message: getErrorMessage(error) }),
  })

  const updateMutation = useMutation({
    mutationFn: () => {
      if (!selectedTeam) {
        throw new Error('Najpierw wybierz drużynę do edycji.')
      }

      return adminApi.updateTeam(selectedTeam.id, {
        ...editForm,
        flagEmoji: editForm.flagEmoji || undefined,
        groupName: editForm.groupName || undefined,
      })
    },
    onSuccess: async () => {
      setFeedback({ tone: 'success', message: 'Drużyna została zaktualizowana.' })
      await refreshTeams()
    },
    onError: (error) => setFeedback({ tone: 'error', title: 'Nie udało się zapisać zmian', message: getErrorMessage(error) }),
  })

  const handleCreate = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setFeedback(null)
    await createMutation.mutateAsync()
  }

  const handleUpdate = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!selectedTeam) return
    setFeedback(null)
    await updateMutation.mutateAsync()
  }

  const teams = teamsQuery.data ?? []

  return (
    <div className="space-y-6">
      <SectionHeading
        eyebrow="Admin / Drużyny"
        title="Zarządzanie drużynami"
        description="Dodawaj zespoły, skróty i grupy turniejowe, żeby później wygodnie układać terminarz."
      />

      {feedback ? <InlineAlert tone={feedback.tone} title={feedback.title} message={feedback.message} /> : null}

      <div className="grid gap-6 xl:grid-cols-2">
        <Panel>
          <form className="space-y-4" onSubmit={(event) => void handleCreate(event)}>
            <p className="font-display text-2xl uppercase text-white">Dodaj drużynę</p>
            <FormField label="Nazwa">
              <input className={inputClassName} value={createForm.name} onChange={(event) => setCreateForm((current) => ({ ...current, name: event.target.value }))} />
            </FormField>
            <div className="grid gap-4 md:grid-cols-2">
              <FormField label="Skrót">
                <input className={inputClassName} value={createForm.shortName} onChange={(event) => setCreateForm((current) => ({ ...current, shortName: event.target.value.toUpperCase() }))} />
              </FormField>
              <FormField label="Kod kraju">
                <input className={inputClassName} value={createForm.countryCode} onChange={(event) => setCreateForm((current) => ({ ...current, countryCode: event.target.value.toUpperCase() }))} />
              </FormField>
              <FormField label="Flaga / emoji">
                <input className={inputClassName} value={createForm.flagEmoji} onChange={(event) => setCreateForm((current) => ({ ...current, flagEmoji: event.target.value }))} />
              </FormField>
              <FormField label="Grupa">
                <input className={inputClassName} value={createForm.groupName} onChange={(event) => setCreateForm((current) => ({ ...current, groupName: event.target.value.toUpperCase() }))} />
              </FormField>
            </div>
            <button className={buttonClassName} type="submit">Dodaj drużynę</button>
          </form>
        </Panel>

        <Panel>
          <form className="space-y-4" onSubmit={(event) => void handleUpdate(event)}>
            <p className="font-display text-2xl uppercase text-white">Edytuj drużynę</p>
            {selectedTeam ? (
              <>
                <FormField label="Nazwa">
                  <input className={inputClassName} value={editForm.name} onChange={(event) => setEditForm((current) => ({ ...current, name: event.target.value }))} />
                </FormField>
                <div className="grid gap-4 md:grid-cols-2">
                  <FormField label="Skrót">
                    <input className={inputClassName} value={editForm.shortName} onChange={(event) => setEditForm((current) => ({ ...current, shortName: event.target.value.toUpperCase() }))} />
                  </FormField>
                  <FormField label="Kod kraju">
                    <input className={inputClassName} value={editForm.countryCode} onChange={(event) => setEditForm((current) => ({ ...current, countryCode: event.target.value.toUpperCase() }))} />
                  </FormField>
                  <FormField label="Flaga / emoji">
                    <input className={inputClassName} value={editForm.flagEmoji} onChange={(event) => setEditForm((current) => ({ ...current, flagEmoji: event.target.value }))} />
                  </FormField>
                  <FormField label="Grupa">
                    <input className={inputClassName} value={editForm.groupName} onChange={(event) => setEditForm((current) => ({ ...current, groupName: event.target.value.toUpperCase() }))} />
                  </FormField>
                </div>
                <div className="flex flex-wrap gap-3">
                  <button className={buttonClassName} type="submit">Zapisz zmiany</button>
                  <button
                    type="button"
                    className={secondaryButtonClassName}
                    onClick={() => {
                      setSelectedTeam(null)
                      setEditForm(emptyTeamForm)
                    }}
                  >
                    Wyczyść formularz
                  </button>
                </div>
              </>
            ) : (
              <p className="text-sm text-slate-400">Wybierz drużynę z listy poniżej, aby zmienić jej dane.</p>
            )}
          </form>
        </Panel>
      </div>

      <QueryState
        isLoading={teamsQuery.isLoading}
        isError={teamsQuery.isError}
        errorMessage={getErrorMessage(teamsQuery.error)}
        isEmpty={teams.length === 0}
        emptyTitle="Brak drużyn"
        emptyDescription="Dodaj pierwszą reprezentację, aby potem tworzyć terminarz i przypisywać grupy."
        loadingTitle="Ładowanie drużyn"
        loadingDescription="Pobieram listę reprezentacji i ich dane turniejowe."
      >
        <Panel className="overflow-hidden p-0">
          <ResponsiveTable
            table={
              <div className="overflow-x-auto">
                <table className="min-w-full text-sm">
                  <thead className="bg-slate-950/60 text-left uppercase tracking-[0.2em] text-slate-400">
                    <tr>
                      <th className="px-4 py-4">Drużyna</th>
                      <th className="px-4 py-4">Skrót</th>
                      <th className="px-4 py-4">Kod</th>
                      <th className="px-4 py-4">Grupa</th>
                      <th className="px-4 py-4">Akcje</th>
                    </tr>
                  </thead>
                  <tbody>
                    {teams.map((team) => (
                      <tr key={team.id} className="border-t border-white/5">
                        <td className="px-4 py-4 text-white">
                          <span className="inline-flex items-center gap-2">
                            {getTeamFlagEmoji(team) ? <span>{getTeamFlagEmoji(team)}</span> : null}
                            <span>{translateTeamName(team.name)}</span>
                          </span>
                        </td>
                        <td className="px-4 py-4 text-slate-300">{team.shortName}</td>
                        <td className="px-4 py-4 text-slate-300">{team.countryCode}</td>
                        <td className="px-4 py-4 text-slate-300">{team.groupName || '-'}</td>
                        <td className="px-4 py-4">
                          <button type="button" className={secondaryButtonClassName} onClick={() => setSelectedTeam(team)}>
                            Edytuj
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            }
            cards={teams.map((team) => (
              <article key={team.id} className={mobileRecordClassName}>
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p className="font-semibold text-white">
                      {formatTeamDisplayName(team)}
                    </p>
                    <p className="mt-1 text-sm text-slate-400">
                      {team.shortName} / {team.countryCode}
                    </p>
                  </div>
                  <p className="rounded-full border border-white/10 bg-white/5 px-3 py-1 text-xs uppercase tracking-[0.18em] text-slate-300">
                    {team.groupName || 'Bez grupy'}
                  </p>
                </div>

                <div className="mt-4 flex items-center justify-end">
                  <button type="button" className={secondaryButtonClassName} onClick={() => setSelectedTeam(team)}>
                    Edytuj
                  </button>
                </div>
              </article>
            ))}
          />
        </Panel>
      </QueryState>
    </div>
  )
}
