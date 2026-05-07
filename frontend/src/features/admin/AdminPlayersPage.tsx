import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { adminApi } from '../../api/services'
import { getErrorMessage } from '../../api/client'
import type { Player, UserRole } from '../../api/types'
import { FormField } from '../../components/FormField'
import { Panel } from '../../components/Panel'
import { SectionHeading } from '../../components/SectionHeading'
import { buttonClassName, inputClassName, secondaryButtonClassName } from '../../styles/ui'

const emptyCreateForm = {
  email: '',
  displayName: '',
  password: 'ChangeMe123!',
  role: 'Player' as UserRole,
}

export const AdminPlayersPage = () => {
  const queryClient = useQueryClient()
  const playersQuery = useQuery({ queryKey: ['admin', 'players'], queryFn: adminApi.getPlayers })
  const [selectedPlayer, setSelectedPlayer] = useState<Player | null>(null)
  const [createForm, setCreateForm] = useState(emptyCreateForm)
  const [editForm, setEditForm] = useState({
    email: '',
    displayName: '',
    role: 'Player' as UserRole,
    isActive: true,
  })
  const [feedback, setFeedback] = useState<string | null>(null)

  useEffect(() => {
    if (selectedPlayer) {
      setEditForm({
        email: selectedPlayer.email,
        displayName: selectedPlayer.displayName,
        role: selectedPlayer.role,
        isActive: selectedPlayer.isActive,
      })
    }
  }, [selectedPlayer])

  const refreshPlayers = async () => {
    await queryClient.invalidateQueries({ queryKey: ['admin', 'players'] })
  }

  const createMutation = useMutation({
    mutationFn: () => adminApi.createPlayer(createForm),
    onSuccess: async () => {
      setFeedback('Gracz został dodany.')
      setCreateForm(emptyCreateForm)
      await refreshPlayers()
    },
    onError: (error) => setFeedback(getErrorMessage(error)),
  })

  const updateMutation = useMutation({
    mutationFn: () => {
      if (!selectedPlayer) {
        throw new Error('Najpierw wybierz gracza do edycji.')
      }

      return adminApi.updatePlayer(selectedPlayer.id, editForm)
    },
    onSuccess: async () => {
      setFeedback('Dane gracza zostały zapisane.')
      await refreshPlayers()
    },
    onError: (error) => setFeedback(getErrorMessage(error)),
  })

  const deactivateMutation = useMutation({
    mutationFn: (playerId: string) => adminApi.deactivatePlayer(playerId),
    onSuccess: async () => {
      setFeedback('Gracz został dezaktywowany.')
      await refreshPlayers()
    },
    onError: (error) => setFeedback(getErrorMessage(error)),
  })

  const resetMutation = useMutation({
    mutationFn: (playerId: string) => adminApi.resetPassword(playerId),
    onSuccess: (data) => {
      setFeedback(`Nowe hasło tymczasowe: ${data.temporaryPassword}`)
    },
    onError: (error) => setFeedback(getErrorMessage(error)),
  })

  const handleCreate = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setFeedback(null)
    await createMutation.mutateAsync()
  }

  const handleUpdate = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!selectedPlayer) return
    setFeedback(null)
    await updateMutation.mutateAsync()
  }

  return (
    <div className="space-y-6">
      <SectionHeading
        eyebrow="Admin • Gracze"
        title="Zarządzanie graczami"
        description="Dodawaj nowe konta, edytuj role i w razie potrzeby resetuj hasła tymczasowe."
      />

      {feedback ? <Panel className="bg-sky-500/10 text-sm text-sky-100">{feedback}</Panel> : null}

      <div className="grid gap-6 xl:grid-cols-2">
        <Panel>
          <form className="space-y-4" onSubmit={(event) => void handleCreate(event)}>
            <p className="font-display text-2xl uppercase text-white">Dodaj gracza</p>
            <FormField label="Email">
              <input className={inputClassName} value={createForm.email} onChange={(event) => setCreateForm((current) => ({ ...current, email: event.target.value }))} />
            </FormField>
            <FormField label="Nazwa wyświetlana">
              <input className={inputClassName} value={createForm.displayName} onChange={(event) => setCreateForm((current) => ({ ...current, displayName: event.target.value }))} />
            </FormField>
            <FormField label="Hasło tymczasowe">
              <input className={inputClassName} value={createForm.password} onChange={(event) => setCreateForm((current) => ({ ...current, password: event.target.value }))} />
            </FormField>
            <FormField label="Rola">
              <select className={inputClassName} value={createForm.role} onChange={(event) => setCreateForm((current) => ({ ...current, role: event.target.value as UserRole }))}>
                <option value="Player">Player</option>
                <option value="Admin">Admin</option>
              </select>
            </FormField>
            <button className={buttonClassName} type="submit">Dodaj gracza</button>
          </form>
        </Panel>

        <Panel>
          <form className="space-y-4" onSubmit={(event) => void handleUpdate(event)}>
            <p className="font-display text-2xl uppercase text-white">Edytuj gracza</p>
            {selectedPlayer ? (
              <>
                <FormField label="Email">
                  <input className={inputClassName} value={editForm.email} onChange={(event) => setEditForm((current) => ({ ...current, email: event.target.value }))} />
                </FormField>
                <FormField label="Nazwa wyświetlana">
                  <input className={inputClassName} value={editForm.displayName} onChange={(event) => setEditForm((current) => ({ ...current, displayName: event.target.value }))} />
                </FormField>
                <FormField label="Rola">
                  <select className={inputClassName} value={editForm.role} onChange={(event) => setEditForm((current) => ({ ...current, role: event.target.value as UserRole }))}>
                    <option value="Player">Player</option>
                    <option value="Admin">Admin</option>
                  </select>
                </FormField>
                <label className="flex items-center gap-3 text-sm text-slate-300">
                  <input type="checkbox" checked={editForm.isActive} onChange={(event) => setEditForm((current) => ({ ...current, isActive: event.target.checked }))} />
                  Konto aktywne
                </label>
                <div className="flex flex-wrap gap-3">
                  <button className={buttonClassName} type="submit">Zapisz zmiany</button>
                  <button type="button" className={secondaryButtonClassName} onClick={() => void resetMutation.mutateAsync(selectedPlayer.id)}>
                    Resetuj hasło
                  </button>
                  <button type="button" className={secondaryButtonClassName} onClick={() => void deactivateMutation.mutateAsync(selectedPlayer.id)}>
                    Dezaktywuj
                  </button>
                </div>
              </>
            ) : (
              <p className="text-sm text-slate-400">Wybierz gracza z listy poniżej, aby edytować jego dane.</p>
            )}
          </form>
        </Panel>
      </div>

      <Panel className="overflow-hidden p-0">
        <div className="overflow-x-auto">
          <table className="min-w-full text-sm">
            <thead className="bg-slate-950/60 text-left uppercase tracking-[0.2em] text-slate-400">
              <tr>
                <th className="px-4 py-4">Gracz</th>
                <th className="px-4 py-4">Email</th>
                <th className="px-4 py-4">Rola</th>
                <th className="px-4 py-4">Status</th>
                <th className="px-4 py-4">Akcje</th>
              </tr>
            </thead>
            <tbody>
              {playersQuery.data?.map((player) => (
                <tr key={player.id} className="border-t border-white/5">
                  <td className="px-4 py-4 text-white">{player.displayName}</td>
                  <td className="px-4 py-4 text-slate-300">{player.email}</td>
                  <td className="px-4 py-4">{player.role}</td>
                  <td className="px-4 py-4">{player.isActive ? 'Aktywny' : 'Nieaktywny'}</td>
                  <td className="px-4 py-4">
                    <button type="button" className={secondaryButtonClassName} onClick={() => setSelectedPlayer(player)}>
                      Edytuj
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Panel>
    </div>
  )
}
