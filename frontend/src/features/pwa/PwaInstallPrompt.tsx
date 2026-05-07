import { useEffect, useState } from 'react'
import { Panel } from '../../components/Panel'

interface BeforeInstallPromptEvent extends Event {
  prompt: () => Promise<void>
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed'; platform: string }>
}

export const PwaInstallPrompt = () => {
  const [installEvent, setInstallEvent] = useState<BeforeInstallPromptEvent | null>(null)

  useEffect(() => {
    const handleBeforeInstallPrompt = (event: Event) => {
      event.preventDefault()
      setInstallEvent(event as BeforeInstallPromptEvent)
    }

    window.addEventListener('beforeinstallprompt', handleBeforeInstallPrompt)
    return () => window.removeEventListener('beforeinstallprompt', handleBeforeInstallPrompt)
  }, [])

  if (!installEvent) {
    return null
  }

  return (
    <div className="mx-auto mt-4 max-w-7xl px-4 sm:px-6 lg:px-8">
      <Panel className="flex flex-col gap-3 border border-emerald-400/20 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <p className="font-display text-xl uppercase text-white">Dodaj aplikację do ekranu głównego</p>
          <p className="text-sm text-slate-300">MVP jest już PWA-ready, więc możesz testować zachowanie jak aplikacja mobilna.</p>
        </div>
        <button
          type="button"
          onClick={() => void installEvent.prompt()}
          className="rounded-full bg-emerald-400 px-4 py-2 text-sm font-semibold text-slate-950 transition hover:bg-emerald-300"
        >
          Zainstaluj
        </button>
      </Panel>
    </div>
  )
}
