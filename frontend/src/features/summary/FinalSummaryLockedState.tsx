import { CalendarClock, LockKeyhole, Trophy } from 'lucide-react'
import type { FinalSummaryAvailability } from '../../api/types'
import { Panel } from '../../components/Panel'
import { secondaryButtonClassName } from '../../styles/ui'

const numberFormatter = new Intl.NumberFormat('pl-PL')

const reasonCopy: Record<string, string> = {
  'final-match-missing': 'Czekamy na potwierdzenie pary finałowej w danych turnieju.',
  'final-match-not-settled': 'Do pokazania finalnego podsumowania brakuje jeszcze rozliczenia finału.',
  'matches-still-open': 'Do pokazania finalnego podsumowania brakuje jeszcze rozliczenia wszystkich meczów.',
  'final-results-not-calculated': 'Finał jest już rozliczony, ale typy muszą zostać przeliczone.',
  'final-ranking-not-snapshotted': 'Finał jest już rozliczony, ale ranking czeka jeszcze na finalny snapshot.',
}

export const FinalSummaryLockedState = ({
  availability,
  showLoginLink = false,
}: {
  availability: FinalSummaryAvailability
  showLoginLink?: boolean
}) => {
  const finalMatchLabel = availability.finalMatchLabel || 'ARG-ESP'
  const settled = numberFormatter.format(availability.settledMatchesCount)
  const required = numberFormatter.format(availability.requiredSettledMatchesCount)
  const total = numberFormatter.format(availability.totalMatchesCount)
  const message = reasonCopy[availability.reason] ?? 'Finalne podsumowanie odblokuje się automatycznie po domknięciu danych.'

  return (
    <Panel className="relative overflow-hidden border-emerald-300/20 bg-slate-950/70 p-5 sm:p-7">
      <div className="absolute inset-x-0 top-0 h-1 bg-emerald-300/80" aria-hidden="true" />
      <div className="relative grid gap-6 lg:grid-cols-[auto_1fr] lg:items-start">
        <div className="flex h-16 w-16 items-center justify-center rounded-2xl border border-emerald-300/25 bg-emerald-300/10 text-emerald-300">
          <LockKeyhole className="h-7 w-7" aria-hidden="true" />
        </div>
        <div className="min-w-0">
          <p className="font-display text-xs uppercase tracking-[0.28em] text-emerald-300/80">Recap zamknięty</p>
          <h2 className="mt-3 break-words font-display text-3xl font-bold uppercase leading-tight text-white sm:text-4xl">
            Recap odblokuje się po finale {finalMatchLabel}
          </h2>
          <p className="mt-4 max-w-2xl text-sm leading-6 text-slate-300 sm:text-base">{message}</p>

          <div className="mt-6 grid gap-3 sm:grid-cols-2">
            <div className="rounded-2xl border border-white/10 bg-white/[0.04] p-4">
              <CalendarClock className="h-5 w-5 text-emerald-300" aria-hidden="true" />
              <p className="mt-3 text-xs uppercase tracking-[0.2em] text-slate-400">Status danych</p>
              <p className="mt-1 font-display text-2xl text-white">
                {settled} z {required} meczów rozliczonych
              </p>
            </div>
            <div className="rounded-2xl border border-white/10 bg-white/[0.04] p-4">
              <Trophy className="h-5 w-5 text-emerald-300" aria-hidden="true" />
              <p className="mt-3 text-xs uppercase tracking-[0.2em] text-slate-400">Pełny turniej</p>
              <p className="mt-1 font-display text-2xl text-white">{total} mecze w harmonogramie</p>
            </div>
          </div>

          {showLoginLink ? (
            <div className="mt-6">
              <a href="/login?returnTo=%2Fsummary%2Ffinal%2Fme" className={secondaryButtonClassName}>
                Zaloguj się po swój recap
              </a>
            </div>
          ) : null}
        </div>
      </div>
    </Panel>
  )
}
