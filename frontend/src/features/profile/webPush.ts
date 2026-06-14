import { notificationsApi } from '../../api/services'

const PUSH_SERVICE_WORKER_URL = '/push-sw.js'

const base64UrlToUint8Array = (value: string) => {
  const padding = '='.repeat((4 - (value.length % 4)) % 4)
  const base64 = `${value}${padding}`.replace(/-/g, '+').replace(/_/g, '/')
  const raw = window.atob(base64)
  const output = new Uint8Array(raw.length)

  for (let index = 0; index < raw.length; index += 1) {
    output[index] = raw.charCodeAt(index)
  }

  return output
}

export const getPushSupportState = () => {
  if (!('Notification' in window) || !('serviceWorker' in navigator) || !('PushManager' in window)) {
    return 'unsupported' as const
  }

  return Notification.permission
}

export const enableWebPushForCurrentDevice = async () => {
  if (getPushSupportState() === 'unsupported') {
    throw new Error('Ta przegladarka nie obsluguje powiadomien push.')
  }

  const permission = await Notification.requestPermission()
  if (permission !== 'granted') {
    throw new Error('Powiadomienia nie zostaly wlaczone w przegladarce.')
  }

  const { publicKey } = await notificationsApi.getVapidPublicKey()
  if (!publicKey.trim()) {
    throw new Error('Powiadomienia push nie sa jeszcze skonfigurowane na tym srodowisku.')
  }

  const registration = await navigator.serviceWorker.register(PUSH_SERVICE_WORKER_URL)
  const existingSubscription = await registration.pushManager.getSubscription()
  const subscription = existingSubscription ?? await registration.pushManager.subscribe({
    userVisibleOnly: true,
    applicationServerKey: base64UrlToUint8Array(publicKey),
  })

  const json = subscription.toJSON()
  if (!json.endpoint || !json.keys?.p256dh || !json.keys.auth) {
    throw new Error('Przegladarka zwrocila niekompletna subskrypcje push.')
  }

  await notificationsApi.saveSubscription({
    endpoint: json.endpoint,
    keys: {
      p256dh: json.keys.p256dh,
      auth: json.keys.auth,
    },
    userAgent: navigator.userAgent,
  })
}

export const disableWebPushForCurrentDevice = async () => {
  if (!('serviceWorker' in navigator)) {
    return
  }

  const registration = await navigator.serviceWorker.getRegistration(PUSH_SERVICE_WORKER_URL)
    ?? await navigator.serviceWorker.ready.catch(() => null)
  const subscription = await registration?.pushManager.getSubscription()

  if (!subscription) {
    return
  }

  await notificationsApi.revokeCurrentSubscription({ endpoint: subscription.endpoint })
  await subscription.unsubscribe()
}
