import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getErrorMessage } from '../../api/client'
import { predictionsApi, rankingApi } from '../../api/services'
import { formatKickoff, translateTeamName } from '../../app/formatters'
import { QueryState } from '../../components/QueryState'
import { Panel } from '../../components/Panel'
import { SectionHeading } from '../../components/SectionHeading'
import { StatCard } from '../../components/StatCard'
import { FormField } from '../../components/FormField'
import { InlineAlert } from '../../components/InlineAlert'
import { UserAvatar } from '../../components/UserAvatar'
import { buttonClassName, inputClassName } from '../../styles/ui'
import { useAuth } from '../auth/AuthContext'

export const ProfilePage = () => {
  const queryClient = useQueryClient()
  const { user, updateAvatar, changePassword } = useAuth()
  const [avatarUrl, setAvatarUrl] = useState(user?.avatarUrl ?? '')
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [passwordError, setPasswordError] = useState<string | null>(null)
  const [passwordSuccess, setPasswordSuccess] = useState(false)
  const [isChangingPassword, setIsChangingPassword] = useState(false)
  const myRankingQuery = useQuery({ queryKey: ['ranking', 'me'], queryFn: rankingApi.getMine })
  const progressQuery = useQuery({ queryKey: ['ranking', 'progress'], queryFn: rankingApi.getProgress })
  const predictionsQuery = useQuery({ queryKey: ['predictions', 'mine'], queryFn: predictionsApi.getMine })

  const ranking = myRankingQuery.data
  const avatarMutation = useMutation({
    mutationFn: (nextAvatarUrl?: string | null) => updateAvatar(nextAvatarUrl?.trim() || null),
    onSuccess: (currentUser) => {
      setAvatarUrl(currentUser.avatarUrl ?? '')
      void queryClient.invalidateQueries({ queryKey: ['ranking'] })
    },
  })

  useEffect(() => {
    setAvatarUrl(user?.avatarUrl ?? '')
  }, [user?.avatarUrl])

  const handlePasswordChange = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setPasswordError(null)
    setPasswordSuccess(false)

    if (newPassword !== confirmPassword) {
      setPasswordError('Powtórzone hasło musi być takie samo jak nowe hasło.')
      return
    }

    setIsChangingPassword(true)
    try {
      await changePassword(currentPassword, newPassword)
      setPasswordSuccess(true)
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
    } catch (err) {
      setPasswordError(getErrorMessage(err))
    } finally {
      setIsChangingPassword(false)
    }
  }

  return (
    <div className="space-y-6">
      <SectionHeading
        eyebrow="Profil"
        title="Moje statystyki"
        description="Twoje miejsce w tabeli, aktualny dorobek punktowy i historia typów."
      />

      <Panel>
        <div className="grid min-w-0 gap-5 lg:grid-cols-[auto_1fr] lg:items-center">
          <div className="flex min-w-0 items-center gap-4">
            <UserAvatar displayName={user?.displayName ?? 'Gracz'} avatarUrl={user?.avatarUrl} size="lg" />
            <div className="min-w-0">
              <p className="truncate font-display text-2xl uppercase text-white">{user?.displayName}</p>
              <p className="truncate text-sm text-slate-400">{user?.email}</p>
            </div>
          </div>

          <form
            className="grid gap-3 md:grid-cols-[1fr_auto]"
            onSubmit={(event) => {
              event.preventDefault()
              avatarMutation.mutate(avatarUrl)
            }}
          >
            <FormField label="Zdjęcie profilowe" hint="Wklej pełny adres URL obrazu albo zostaw puste, żeby użyć inicjałów.">
              <input
                className={inputClassName}
                value={avatarUrl}
                onChange={(event) => setAvatarUrl(event.target.value)}
                placeholder="https://..."
                type="url"
              />
            </FormField>

            <div className="flex flex-col gap-2 sm:flex-row sm:items-end">
              <button className={`${buttonClassName} w-full sm:w-auto`} type="submit" disabled={avatarMutation.isPending}>
                Zapisz
              </button>
              <button
                className={`${secondaryButtonClassName} w-full sm:w-auto`}
                type="button"
                disabled={avatarMutation.isPending}
                onClick={() => {
                  setAvatarUrl('')
                  avatarMutation.mutate(null)
                }}
              >
                Usuń
              </button>
            </div>
          </form>
        </div>

        {avatarMutation.isError ? (
          <InlineAlert className="mt-4" tone="error" message={getErrorMessage(avatarMutation.error)} />
        ) : null}
        {avatarMutation.isSuccess ? (
          <InlineAlert className="mt-4" tone="success" message="Avatar profilu został zapisany." />
        ) : null}
      </Panel>

      <QueryState
        isLoading={myRankingQuery.isLoading}
        isError={myRankingQuery.isError}
        errorMessage={getErrorMessage(myRankingQuery.error)}
        isEmpty={!ranking}
        emptyTitle="Brak pozycji w rankingu"
        emptyDescription="Gdy system utworzy Twój wpis rankingowy, zobaczysz tutaj bieżące podsumowanie."
        loadingTitle="Ładowanie podsumowania profilu"
        loadingDescription="Pobieram Twoje miejsce w tabeli i aktualny dorobek punktowy."
      >
        {ranking ? (
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <StatCard label="Miejsce" value={`#${ranking.position}`} />
            <StatCard label="Punkty" value={ranking.totalPoints} accent="text-emerald-300" />
            <StatCard label="Dokładne wyniki" value={ranking.exactScoreHits} />
            <StatCard label="Trafione rezultaty" value={ranking.correctOutcomeHits} />
          </div>
        ) : null}
      </QueryState>

      <div className="grid gap-6 xl:grid-cols-[1fr_1.3fr]">
        <QueryState
          isLoading={progressQuery.isLoading}
          isError={progressQuery.isError}
          errorMessage={getErrorMessage(progressQuery.error)}
          isEmpty={(progressQuery.data?.length ?? 0) === 0}
          emptyTitle="Brak progresu po meczach"
          emptyDescription="Gdy pojawią się rozliczone spotkania, zobaczysz tutaj rozwój swojej pozycji."
          loadingTitle="Ładowanie progresu"
          loadingDescription="Pobieram historię zmian Twojego dorobku punktowego."
        >
          <Panel className="space-y-4">
            <p className="font-display text-2xl uppercase text-white">Progres po meczach</p>
            <div className="space-y-3">
              {progressQuery.data?.map((point) => (
                <div key={point.matchId} className="rounded-2xl bg-slate-950/50 px-4 py-3">
                  <p className="font-semibold text-white">Po meczu #{point.matchNumber}</p>
                  <div className="mt-2 grid gap-2 text-sm text-slate-400 sm:grid-cols-3">
                    <p>Punkty: {point.totalPoints}</p>
                    <p>Pozycja: #{point.position}</p>
                    <p>Typy: {point.predictionsCount}</p>
                  </div>
                </div>
              ))}
            </div>
          </Panel>
        </QueryState>

        <QueryState
          isLoading={predictionsQuery.isLoading}
          isError={predictionsQuery.isError}
          errorMessage={getErrorMessage(predictionsQuery.error)}
          isEmpty={(predictionsQuery.data?.length ?? 0) === 0}
          emptyTitle="Historia typów jest pusta"
          emptyDescription="Po zapisaniu pierwszego typu wrócisz tutaj do swojej historii przewidywań."
          loadingTitle="Ładowanie historii typów"
          loadingDescription="Pobieram Twoje zapisane typy i punkty."
        >
          <Panel className="space-y-4">
            <p className="font-display text-2xl uppercase text-white">Historia typów</p>
            <div className="space-y-3">
              {predictionsQuery.data?.map((item) => (
                <div key={item.matchId} className="rounded-2xl bg-slate-950/50 px-4 py-3">
                  <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                    <div>
                      <p className="font-semibold text-white">
                        {translateTeamName(item.homeTeamName)} vs {translateTeamName(item.awayTeamName)}
                      </p>
                      <p className="text-sm text-slate-400">{formatKickoff(item.kickoffTimeUtc)}</p>
                    </div>
                    <div className="sm:text-right">
                      <p className="font-display text-2xl text-emerald-300">
                        {item.prediction.predictedHomeScore}:{item.prediction.predictedAwayScore}
                      </p>
                      <p className="text-xs text-slate-400">Punkty: {item.prediction.points ?? '-'}</p>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </Panel>
        </QueryState>
      </div>

      <Panel>
        <p className="font-display text-2xl uppercase text-white">Zmiana hasła</p>
        <p className="mt-1 text-sm text-slate-400">Nowe hasło musi mieć co najmniej 8 znaków.</p>

        {passwordError ? <InlineAlert tone="error" message={passwordError} className="mt-4" /> : null}
        {passwordSuccess ? <InlineAlert tone="success" message="Hasło zostało zmienione." className="mt-4" /> : null}

        <form className="mt-5 grid gap-4 sm:grid-cols-3" onSubmit={(event) => void handlePasswordChange(event)}>
          <label className="block space-y-2">
            <span className="text-sm text-slate-300">Obecne hasło</span>
            <input
              type="password"
              className={inputClassName}
              value={currentPassword}
              onChange={(event) => setCurrentPassword(event.target.value)}
              autoComplete="current-password"
              required
            />
          </label>
          <label className="block space-y-2">
            <span className="text-sm text-slate-300">Nowe hasło</span>
            <input
              type="password"
              className={inputClassName}
              value={newPassword}
              onChange={(event) => setNewPassword(event.target.value)}
              autoComplete="new-password"
              minLength={8}
              required
            />
          </label>
          <label className="block space-y-2">
            <span className="text-sm text-slate-300">Powtórz nowe hasło</span>
            <input
              type="password"
              className={inputClassName}
              value={confirmPassword}
              onChange={(event) => setConfirmPassword(event.target.value)}
              autoComplete="new-password"
              minLength={8}
              required
            />
          </label>
          <div className="sm:col-span-3">
            <button type="submit" disabled={isChangingPassword} className={`${buttonClassName} w-full sm:w-auto`}>
              {isChangingPassword ? 'Zapisywanie...' : 'Zmień hasło'}
            </button>
          </div>
        </form>
      </Panel>
    </div>
  )
}
