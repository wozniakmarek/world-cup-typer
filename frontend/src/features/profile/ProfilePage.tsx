import { useRef, useState } from 'react'
import type { ChangeEvent, FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getErrorMessage } from '../../api/client'
import { predictionsApi, rankingApi } from '../../api/services'
import type { CurrentUser } from '../../api/types'
import { formatKickoff, translateTeamName } from '../../app/formatters'
import { QueryState } from '../../components/QueryState'
import { Panel } from '../../components/Panel'
import { SectionHeading } from '../../components/SectionHeading'
import { StatCard } from '../../components/StatCard'
import { InlineAlert } from '../../components/InlineAlert'
import { UserAvatar } from '../../components/UserAvatar'
import { buttonClassName, inputClassName, secondaryButtonClassName } from '../../styles/ui'
import { useAuth } from '../auth/useAuth'
import { NotificationSettingsPanel } from './NotificationSettingsPanel'

const avatarCanvasSize = 320
const avatarOutputQuality = 0.86
const maxAvatarFileSizeBytes = 5 * 1024 * 1024
const maxAvatarDataUrlLength = 100_000

const isImageDataAvatar = (value?: string | null) => Boolean(value?.startsWith('data:image/'))

const createAvatarDataUrl = async (file: File) => {
  if (!file.type.startsWith('image/') || file.type === 'image/svg+xml') {
    throw new Error('Wybierz plik PNG, JPEG, WebP albo GIF.')
  }

  if (file.size > maxAvatarFileSizeBytes) {
    throw new Error('Zdjęcie może mieć maksymalnie 5 MB.')
  }

  const objectUrl = URL.createObjectURL(file)

  try {
    const image = await new Promise<HTMLImageElement>((resolve, reject) => {
      const nextImage = new Image()
      nextImage.onload = () => resolve(nextImage)
      nextImage.onerror = () => reject(new Error('Nie udało się odczytać zdjęcia.'))
      nextImage.src = objectUrl
    })

    const width = image.naturalWidth || image.width
    const height = image.naturalHeight || image.height
    const sourceSize = Math.min(width, height)
    const sourceX = (width - sourceSize) / 2
    const sourceY = (height - sourceSize) / 2
    const canvas = document.createElement('canvas')
    canvas.width = avatarCanvasSize
    canvas.height = avatarCanvasSize

    const context = canvas.getContext('2d')
    if (!context) {
      throw new Error('Przeglądarka nie może przygotować miniatury zdjęcia.')
    }

    context.fillStyle = '#020617'
    context.fillRect(0, 0, avatarCanvasSize, avatarCanvasSize)
    context.drawImage(image, sourceX, sourceY, sourceSize, sourceSize, 0, 0, avatarCanvasSize, avatarCanvasSize)

    const dataUrl = canvas.toDataURL('image/jpeg', avatarOutputQuality)
    if (dataUrl.length > maxAvatarDataUrlLength) {
      throw new Error('Zdjęcie po zmniejszeniu jest nadal za duże.')
    }

    return dataUrl
  } finally {
    URL.revokeObjectURL(objectUrl)
  }
}

const getAvatarUrlInputValue = (avatarUrl?: string | null) => (isImageDataAvatar(avatarUrl) ? '' : (avatarUrl ?? ''))

const ProfileAvatarPanel = ({
  user,
  updateAvatar,
}: {
  user: CurrentUser | null
  updateAvatar: (avatarUrl?: string | null) => Promise<CurrentUser>
}) => {
  const queryClient = useQueryClient()
  const avatarFileInputRef = useRef<HTMLInputElement | null>(null)
  const [avatarValue, setAvatarValue] = useState(user?.avatarUrl ?? '')
  const [avatarUrlInput, setAvatarUrlInput] = useState(getAvatarUrlInputValue(user?.avatarUrl))
  const [selectedAvatarFileName, setSelectedAvatarFileName] = useState<string | null>(null)
  const [avatarFileError, setAvatarFileError] = useState<string | null>(null)

  const avatarMutation = useMutation({
    mutationFn: (nextAvatarUrl?: string | null) => updateAvatar(nextAvatarUrl?.trim() || null),
    onSuccess: (currentUser) => {
      setAvatarValue(currentUser.avatarUrl ?? '')
      setAvatarUrlInput(getAvatarUrlInputValue(currentUser.avatarUrl))
      setSelectedAvatarFileName(null)
      setAvatarFileError(null)
      void queryClient.invalidateQueries({ queryKey: ['ranking'] })
    },
  })

  const handleAvatarFileChange = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    if (!file) {
      return
    }

    setAvatarFileError(null)

    try {
      const dataUrl = await createAvatarDataUrl(file)
      setAvatarValue(dataUrl)
      setAvatarUrlInput('')
      setSelectedAvatarFileName(file.name)
    } catch (err) {
      setAvatarFileError(err instanceof Error ? err.message : 'Nie udało się przygotować zdjęcia.')
    } finally {
      event.target.value = ''
    }
  }

  return (
    <Panel>
      <div className="grid min-w-0 gap-5 lg:grid-cols-[auto_1fr] lg:items-center">
        <div className="flex min-w-0 items-center gap-4">
          <UserAvatar displayName={user?.displayName ?? 'Gracz'} avatarUrl={avatarValue || null} size="lg" />
          <div className="min-w-0">
            <p className="truncate font-display text-2xl uppercase text-white">{user?.displayName}</p>
            <p className="truncate text-sm text-slate-400">{user?.email}</p>
          </div>
        </div>

        <form
          className="grid gap-3 md:grid-cols-[1fr_auto]"
          onSubmit={(event) => {
            event.preventDefault()
            avatarMutation.mutate(avatarValue)
          }}
        >
          <div className="flex flex-col gap-2 text-sm text-slate-300">
            <label className="font-medium text-slate-200" htmlFor="profile-avatar-url">
              Zdjęcie profilowe
            </label>
            <div className="grid gap-2 sm:grid-cols-[1fr_auto]">
              <input
                id="profile-avatar-url"
                className={inputClassName}
                value={avatarUrlInput}
                onChange={(event) => {
                  setAvatarUrlInput(event.target.value)
                  setAvatarValue(event.target.value)
                  setSelectedAvatarFileName(null)
                  setAvatarFileError(null)
                }}
                placeholder="https://..."
                type="text"
              />
              <input
                ref={avatarFileInputRef}
                aria-label="Zdjecie z galerii"
                className="sr-only"
                type="file"
                accept="image/png,image/jpeg,image/webp,image/gif"
                onChange={(event) => void handleAvatarFileChange(event)}
              />
              <button
                className={`${secondaryButtonClassName} w-full sm:w-auto`}
                type="button"
                disabled={avatarMutation.isPending}
                onClick={() => avatarFileInputRef.current?.click()}
              >
                Wybierz z galerii
              </button>
            </div>
            <span className="text-xs text-slate-500">
              Wklej pełny adres URL obrazu, wybierz zdjęcie z galerii albo zostaw puste, żeby użyć inicjałów.
            </span>
            {selectedAvatarFileName ? (
              <span className="text-xs font-medium text-emerald-300">Wybrano {selectedAvatarFileName}</span>
            ) : isImageDataAvatar(avatarValue) ? (
              <span className="text-xs font-medium text-emerald-300">Używasz zdjęcia z galerii.</span>
            ) : null}
            {avatarFileError ? (
              <span className="text-xs font-medium text-rose-300" role="alert">
                {avatarFileError}
              </span>
            ) : null}
          </div>

          <div className="flex flex-col gap-2 sm:flex-row sm:items-end">
            <button className={`${buttonClassName} w-full sm:w-auto`} type="submit" disabled={avatarMutation.isPending}>
              Zapisz
            </button>
            <button
              className={`${secondaryButtonClassName} w-full sm:w-auto`}
              type="button"
              disabled={avatarMutation.isPending}
              onClick={() => {
                setAvatarValue('')
                setAvatarUrlInput('')
                setSelectedAvatarFileName(null)
                setAvatarFileError(null)
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
  )
}

export const ProfilePage = () => {
  const { user, updateAvatar, changePassword } = useAuth()
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

      <ProfileAvatarPanel key={user?.id ?? 'anonymous'} user={user} updateAvatar={updateAvatar} />

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

      <NotificationSettingsPanel />

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
