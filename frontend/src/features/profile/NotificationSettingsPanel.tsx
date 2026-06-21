import { Bell, BellOff, Save } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getErrorMessage } from '../../api/client'
import { notificationsApi } from '../../api/services'
import type { NotificationSettings } from '../../api/types'
import { InlineAlert } from '../../components/InlineAlert'
import { Panel } from '../../components/Panel'
import { buttonClassName, secondaryButtonClassName } from '../../styles/ui'
import {
  disableWebPushForCurrentDevice,
  enableWebPushForCurrentDevice,
  getPushSupportState,
  hasWebPushSubscriptionOnCurrentDevice,
} from './webPush'

type NotificationOption = {
  key: keyof Omit<NotificationSettings, 'hasActiveSubscription'>
  label: string
  description: string
}

const notificationOptions: NotificationOption[] = [
  {
    key: 'morningDigestEnabled',
    label: 'Poranne przypomnienie o meczach',
    description: 'Krotkie podsumowanie dnia meczowego i brakujacych typow.',
  },
  {
    key: 'missingPrediction2hEnabled',
    label: '2h przed meczem bez typu',
    description: 'Pierwszy sygnal, gdy mecz jest blisko, a typ nie jest zapisany.',
  },
  {
    key: 'missingPrediction30mEnabled',
    label: '30 min przed meczem bez typu',
    description: 'Ostatnie przypomnienie przed zamknieciem typowania.',
  },
  {
    key: 'rankingUpdatedEnabled',
    label: 'Po aktualizacji rankingu',
    description: 'Informacja po rozliczeniu meczu i przeliczeniu punktow.',
  },
]

const defaultSettings: NotificationSettings = {
  morningDigestEnabled: true,
  missingPrediction2hEnabled: true,
  missingPrediction30mEnabled: true,
  rankingUpdatedEnabled: true,
  hasActiveSubscription: false,
}

type CurrentDeviceSubscriptionState = 'checking' | boolean

const getInitialCurrentDeviceSubscriptionState = (): CurrentDeviceSubscriptionState => {
  if (typeof window === 'undefined') {
    return false
  }

  const supportState = getPushSupportState()
  return supportState === 'unsupported' || supportState === 'ios-install-required' ? false : 'checking'
}

export const NotificationSettingsPanel = () => {
  const queryClient = useQueryClient()
  const [draftOverrides, setDraftOverrides] = useState<Partial<NotificationSettings>>({})
  const [deviceError, setDeviceError] = useState<string | null>(null)
  const [deviceSuccess, setDeviceSuccess] = useState<string | null>(null)
  const [testResult, setTestResult] = useState<string | null>(null)
  const [currentDeviceSubscriptionState, setCurrentDeviceSubscriptionState] = useState<CurrentDeviceSubscriptionState>(
    getInitialCurrentDeviceSubscriptionState,
  )
  const supportState = useMemo(() => (typeof window === 'undefined' ? 'unsupported' : getPushSupportState()), [])

  const settingsQuery = useQuery({
    queryKey: ['notifications', 'settings'],
    queryFn: notificationsApi.getSettings,
  })

  const baseSettings = settingsQuery.data ?? defaultSettings
  const draft = { ...baseSettings, ...draftOverrides }

  const saveSettingsMutation = useMutation({
    mutationFn: notificationsApi.updateSettings,
    onSuccess: (settings) => {
      setDraftOverrides({})
      queryClient.setQueryData(['notifications', 'settings'], settings)
    },
  })

  const enableDeviceMutation = useMutation({
    mutationFn: enableWebPushForCurrentDevice,
    onMutate: () => {
      setDeviceError(null)
      setDeviceSuccess(null)
      setTestResult(null)
    },
    onSuccess: async () => {
      setCurrentDeviceSubscriptionState(true)
      setDeviceSuccess('Powiadomienia zostaly wlaczone na tym urzadzeniu.')
      await queryClient.invalidateQueries({ queryKey: ['notifications', 'settings'] })
    },
    onError: (error) => setDeviceError(getErrorMessage(error)),
  })

  const disableDeviceMutation = useMutation({
    mutationFn: disableWebPushForCurrentDevice,
    onMutate: () => {
      setDeviceError(null)
      setDeviceSuccess(null)
      setTestResult(null)
    },
    onSuccess: async () => {
      setCurrentDeviceSubscriptionState(false)
      setDeviceSuccess('Powiadomienia zostaly wylaczone na tym urzadzeniu.')
      await queryClient.invalidateQueries({ queryKey: ['notifications', 'settings'] })
    },
    onError: (error) => setDeviceError(getErrorMessage(error)),
  })

  const testDeviceMutation = useMutation({
    mutationFn: notificationsApi.sendTest,
    onMutate: () => {
      setDeviceError(null)
      setDeviceSuccess(null)
      setTestResult(null)
    },
    onSuccess: (result) => {
      setTestResult(`Test wyslany: ${result.sent}/${result.attempted}. Bledy: ${result.failed}, wygasle: ${result.revoked}.`)
    },
    onError: (error) => setDeviceError(getErrorMessage(error)),
  })

  const hasChanges = settingsQuery.data
    ? notificationOptions.some((option) => settingsQuery.data[option.key] !== draft[option.key])
    : false
  const isSaving = saveSettingsMutation.isPending
  const isDevicePending = enableDeviceMutation.isPending || disableDeviceMutation.isPending || testDeviceMutation.isPending
  const isUnsupported = supportState === 'unsupported'
  const needsIosInstall = supportState === 'ios-install-required'
  const isDenied = supportState === 'denied'
  const hasAnyActiveSubscription = Boolean(settingsQuery.data?.hasActiveSubscription)
  const hasCurrentDeviceSubscription = currentDeviceSubscriptionState === true
  const isCheckingCurrentDevice = currentDeviceSubscriptionState === 'checking'

  useEffect(() => {
    let isActive = true
    if (isUnsupported || needsIosInstall) {
      return () => {
        isActive = false
      }
    }

    void hasWebPushSubscriptionOnCurrentDevice()
      .then((hasSubscription) => {
        if (isActive) {
          setCurrentDeviceSubscriptionState(hasSubscription)
        }
      })
      .catch(() => {
        if (isActive) {
          setCurrentDeviceSubscriptionState(false)
        }
      })

    return () => {
      isActive = false
    }
  }, [isUnsupported, needsIosInstall])

  const statusLabel = needsIosInstall
    ? 'Dodaj aplikacje do ekranu poczatkowego'
    : isUnsupported
      ? 'Brak wsparcia w przegladarce'
      : isDenied
      ? 'Zablokowane w przegladarce'
      : hasCurrentDeviceSubscription
        ? 'Aktywne na tym urzadzeniu'
        : hasAnyActiveSubscription
        ? 'Aktywne na innym urzadzeniu'
        : 'Nieaktywne na tym urzadzeniu'
  const statusDetail = needsIosInstall
    ? 'Na iPhonie powiadomienia dzialaja po otwarciu aplikacji z ikony na ekranie poczatkowym.'
    : hasAnyActiveSubscription && !hasCurrentDeviceSubscription && !isUnsupported && !isDenied
      ? 'To konto ma aktywna subskrypcje gdzie indziej. Ten telefon lub ta przegladarka wymaga osobnego wlaczenia.'
    : 'Ustawienia dotycza konta, aktywacja dotyczy tej przegladarki.'

  return (
    <Panel className="space-y-5">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div className="min-w-0">
          <div className="flex items-center gap-3">
            <span className="inline-flex h-10 w-10 shrink-0 items-center justify-center rounded-2xl bg-emerald-400/15 text-emerald-200">
              {hasCurrentDeviceSubscription || hasAnyActiveSubscription ? (
                <Bell className="h-5 w-5" aria-hidden="true" />
              ) : (
                <BellOff className="h-5 w-5" aria-hidden="true" />
              )}
            </span>
            <div className="min-w-0">
              <h2 className="font-display text-2xl uppercase text-white">Powiadomienia</h2>
              <p className="text-sm text-slate-400">Ustaw, kiedy aplikacja ma przypominac o typach i rankingu.</p>
            </div>
          </div>
        </div>

        <div className="rounded-2xl border border-white/10 bg-slate-950/50 px-4 py-3 text-sm">
          <p className="font-semibold text-white">{statusLabel}</p>
          <p className="mt-1 text-slate-400">{statusDetail}</p>
        </div>
      </div>

      {settingsQuery.isError ? <InlineAlert tone="error" message={getErrorMessage(settingsQuery.error)} /> : null}
      {saveSettingsMutation.isError ? <InlineAlert tone="error" message={getErrorMessage(saveSettingsMutation.error)} /> : null}
      {saveSettingsMutation.isSuccess ? <InlineAlert tone="success" message="Ustawienia powiadomien zostaly zapisane." /> : null}
      {deviceError ? <InlineAlert tone="error" message={deviceError} /> : null}
      {deviceSuccess ? <InlineAlert tone="success" message={deviceSuccess} /> : null}
      {testResult ? <InlineAlert tone="success" message={testResult} /> : null}
      {isDenied ? (
        <InlineAlert tone="warning" message="Przegladarka blokuje powiadomienia. Zmien zgode w ustawieniach strony, zeby wlaczyc push." />
      ) : null}
      {needsIosInstall ? (
        <InlineAlert
          tone="warning"
          message="Na iPhonie stuknij Udostepnij, wybierz Dodaj do ekranu poczatkowego, potem otworz Typer MS z nowej ikony i wlacz powiadomienia."
        />
      ) : null}

      <div className="grid gap-3 md:grid-cols-2">
        {notificationOptions.map((option) => (
          <label
            key={option.key}
            className="flex min-h-28 cursor-pointer items-start gap-4 rounded-2xl border border-white/10 bg-slate-950/40 p-4 transition hover:border-emerald-300/40"
          >
            <input
              type="checkbox"
              className="mt-1 h-5 w-5 rounded border-slate-500 bg-slate-950 text-emerald-400 focus:ring-emerald-300"
              checked={draft[option.key]}
              disabled={settingsQuery.isLoading || isSaving}
              onChange={(event) => setDraftOverrides((current) => ({ ...current, [option.key]: event.target.checked }))}
            />
            <span className="min-w-0">
              <span className="block font-semibold text-white">{option.label}</span>
              <span className="mt-1 block text-sm text-slate-400">{option.description}</span>
            </span>
          </label>
        ))}
      </div>

      <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap">
        <button
          type="button"
          className={`${buttonClassName} w-full sm:w-auto`}
          disabled={!hasChanges || isSaving || settingsQuery.isLoading}
          onClick={() => {
            saveSettingsMutation.mutate({
              morningDigestEnabled: draft.morningDigestEnabled,
              missingPrediction2hEnabled: draft.missingPrediction2hEnabled,
              missingPrediction30mEnabled: draft.missingPrediction30mEnabled,
              rankingUpdatedEnabled: draft.rankingUpdatedEnabled,
            })
          }}
        >
          <Save className="h-4 w-4" aria-hidden="true" />
          {isSaving ? 'Zapisywanie...' : 'Zapisz powiadomienia'}
        </button>

        {hasCurrentDeviceSubscription ? (
          <>
            <button
              type="button"
              className={`${secondaryButtonClassName} w-full sm:w-auto`}
              disabled={isDevicePending || isCheckingCurrentDevice}
              onClick={() => testDeviceMutation.mutate()}
            >
              {testDeviceMutation.isPending ? 'Wysylanie testu...' : 'Wyslij test'}
            </button>
            <button
              type="button"
              className={`${secondaryButtonClassName} w-full sm:w-auto`}
              disabled={isDevicePending || isCheckingCurrentDevice}
              onClick={() => disableDeviceMutation.mutate()}
            >
              Wylacz na tym urzadzeniu
            </button>
          </>
        ) : (
          <button
            type="button"
            className={`${secondaryButtonClassName} w-full sm:w-auto`}
            disabled={needsIosInstall || isUnsupported || isDenied || isDevicePending || isCheckingCurrentDevice}
            onClick={() => enableDeviceMutation.mutate()}
          >
            {enableDeviceMutation.isPending ? 'Wlaczanie...' : 'Wlacz na tym urzadzeniu'}
          </button>
        )}
      </div>
    </Panel>
  )
}
