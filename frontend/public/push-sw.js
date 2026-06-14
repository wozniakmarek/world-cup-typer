self.addEventListener('push', (event) => {
  const fallback = {
    title: 'World Cup Typer',
    body: 'Masz nowe powiadomienie.',
    url: '/',
  }

  const data = event.data ? event.data.json() : fallback
  const title = data.title || fallback.title
  const options = {
    body: data.body || fallback.body,
    data: {
      url: data.url || fallback.url,
    },
  }

  event.waitUntil(self.registration.showNotification(title, options))
})

self.addEventListener('notificationclick', (event) => {
  event.notification.close()
  const targetUrl = new URL(event.notification.data?.url || '/', self.location.origin).href

  event.waitUntil(
    self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then((clientList) => {
      for (const client of clientList) {
        if ('focus' in client && client.url === targetUrl) {
          return client.focus()
        }
      }

      if (self.clients.openWindow) {
        return self.clients.openWindow(targetUrl)
      }

      return undefined
    }),
  )
})
