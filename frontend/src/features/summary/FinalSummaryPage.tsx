import { useQuery } from '@tanstack/react-query'
import { ArrowRight, BarChart3, Medal, Trophy, Users } from 'lucide-react'
import { Link } from 'react-router-dom'
import { getErrorMessage } from '../../api/client'
import { summaryApi } from '../../api/services'
import { UserAvatar } from '../../components/UserAvatar'
import { buttonClassName, secondaryButtonClassName } from '../../styles/ui'
import { FinalSummaryFactGrid } from './FinalSummaryFactGrid'

const numberFormatter = new Intl.NumberFormat('pl-PL')

const loadingText = 'Ladowanie'

export const FinalSummaryPage = () => {
  const summaryQuery = useQuery({ queryKey: ['summary', 'final'], queryFn: summaryApi.getFinal })
  const summary = summaryQuery.data
  const leader = summary?.finalTop[0]
  const isLoading = summaryQuery.isLoading

  const settledMatchesText = summary ? numberFormatter.format(summary.stats.settledMatchesCount) : loadingText
  const activePlayersText = summary ? numberFormatter.format(summary.stats.activePlayersCount) : loadingText
  const leaderText = summary ? (summary.stats.finalLeaderDisplayName ?? leader?.displayName ?? '-') : loadingText

  return (
    <main className="min-h-screen overflow-hidden bg-pitch-950 text-slate-100">
      <section className="relative border-b border-white/10 bg-[radial-gradient(circle_at_top_left,rgba(16,185,129,0.2),transparent_32rem)]">
        <div className="mx-auto flex max-w-7xl flex-col px-4 py-5 sm:px-6 lg:px-8">
          <header className="flex items-center justify-between gap-4">
            <Link to="/" className="font-display text-base font-bold uppercase text-white sm:text-xl">
              Typer MS
            </Link>
            <Link to="/login" className={secondaryButtonClassName}>
              Logowanie
            </Link>
          </header>

          <div className="grid gap-8 py-8 lg:grid-cols-[minmax(0,0.92fr)_minmax(0,1.08fr)] lg:py-10">
            <div className="min-w-0 self-center">
              <p className="font-display text-sm uppercase text-emerald-300">Final turnieju</p>
              <h1 className="mt-4 max-w-4xl break-words font-display text-4xl font-bold uppercase leading-tight text-white sm:text-6xl lg:text-7xl">
                Cala tabela, mecz po meczu
              </h1>
              <p className="mt-5 max-w-2xl text-base leading-7 text-slate-300 sm:text-lg">
                Publiczne podsumowanie pokazuje, jak zmieniala sie walka o miejsca po kazdym rozliczonym meczu.
              </p>
              <div className="mt-7 flex flex-col gap-3 sm:flex-row">
                <Link to="/login" className={buttonClassName}>
                  Zaloguj sie po swoj recap
                  <ArrowRight className="ml-2 h-4 w-4" aria-hidden="true" />
                </Link>
                <a href="#final-table" className={secondaryButtonClassName}>
                  Zobacz tabele
                </a>
              </div>
            </div>

            <section id="final-table" className="glass-card min-w-0 rounded-[2rem] p-5 sm:p-6">
              <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
                <div className="min-w-0">
                  <p className="font-display text-sm uppercase text-emerald-300">Animowana pelna tabela</p>
                  <h2 className="mt-2 break-words font-display text-3xl leading-tight text-white sm:text-4xl">
                    Finalowy ruch rankingu
                  </h2>
                </div>
                <BarChart3 className="h-8 w-8 shrink-0 text-emerald-300" aria-hidden="true" />
              </div>
              <div className="mt-6 flex min-h-64 items-center justify-center px-2 py-8">
                <div className="max-w-md text-center">
                  <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-emerald-300/10 text-emerald-300">
                    <BarChart3 className="h-6 w-6" aria-hidden="true" />
                  </div>
                  <p className="mt-5 font-display text-2xl text-white">Pelna animacja tabeli w kolejnym kroku</p>
                  <p className="mt-3 text-sm leading-6 text-slate-400">
                    {isLoading
                      ? 'Ladowanie miejsca na finalny wykres...'
                      : summaryQuery.isError
                        ? 'Nie udalo sie pobrac danych do publicznego podsumowania.'
                        : 'Tu trafi statyczny kontrakt i animacja pelnej tabeli z Task 5.'}
                  </p>
                </div>
              </div>
            </section>
          </div>
        </div>
      </section>

      <section className="mx-auto grid max-w-7xl gap-4 px-4 py-8 sm:grid-cols-3 sm:px-6 lg:px-8">
        <div className="rounded-2xl border border-white/10 bg-slate-950/45 p-4">
          <Medal className="h-5 w-5 text-emerald-300" aria-hidden="true" />
          <p className="mt-3 text-sm text-slate-400">Rozliczone mecze</p>
          <p className="font-display text-3xl text-white">{settledMatchesText}</p>
        </div>
        <div className="rounded-2xl border border-white/10 bg-slate-950/45 p-4">
          <Users className="h-5 w-5 text-emerald-300" aria-hidden="true" />
          <p className="mt-3 text-sm text-slate-400">Aktywni gracze</p>
          <p className="font-display text-3xl text-white">{activePlayersText}</p>
        </div>
        <div className="rounded-2xl border border-white/10 bg-slate-950/45 p-4">
          <Trophy className="h-5 w-5 text-emerald-300" aria-hidden="true" />
          <p className="mt-3 text-sm text-slate-400">Finalny lider</p>
          <p className="truncate font-display text-3xl text-white">{leaderText}</p>
        </div>
      </section>

      <section className="mx-auto grid max-w-7xl gap-8 px-4 pb-12 sm:px-6 lg:grid-cols-[0.9fr_1.1fr] lg:px-8">
        <div className="min-w-0">
          <p className="font-display text-sm uppercase text-emerald-300">Podium</p>
          <h2 className="mt-2 font-display text-3xl uppercase text-white sm:text-4xl">Finalna czolowka</h2>
          <div className="mt-5 space-y-3">
            {isLoading ? (
              [1, 2, 3].map((position) => (
                <article
                  key={position}
                  className="grid min-w-0 items-center gap-3 rounded-2xl border border-white/10 bg-slate-950/55 p-4 sm:grid-cols-[3rem_1fr_auto]"
                >
                  <p className="font-display text-2xl text-emerald-300">#{position}</p>
                  <div className="min-w-0">
                    <p className="font-semibold text-white">Ladowanie gracza</p>
                    <p className="text-sm text-slate-400">Czekamy na finalne statystyki.</p>
                  </div>
                  <p className="font-display text-2xl text-slate-500 sm:text-right">...</p>
                </article>
              ))
            ) : summaryQuery.isError ? (
              <div className="rounded-2xl border border-rose-400/30 bg-rose-950/30 p-5 text-sm leading-6 text-rose-100">
                Nie udalo sie pobrac finalnej czolowki.
              </div>
            ) : summary?.finalTop.length ? (
              summary.finalTop.slice(0, 3).map((entry) => (
                <article
                  key={entry.userId}
                  className="grid min-w-0 items-center gap-3 rounded-2xl border border-white/10 bg-slate-950/55 p-4 sm:grid-cols-[3rem_1fr_auto]"
                >
                  <p className="font-display text-2xl text-emerald-300">#{entry.finalPosition}</p>
                  <div className="flex min-w-0 items-center gap-3">
                    <UserAvatar displayName={entry.displayName} avatarUrl={entry.avatarUrl} size="sm" />
                    <div className="min-w-0">
                      <p className="truncate font-semibold text-white">{entry.displayName}</p>
                      <p className="text-sm text-slate-400">
                        {entry.exactScoreHits} dokladne, {entry.correctOutcomeHits} rezultaty
                      </p>
                    </div>
                  </div>
                  <p className="font-display text-3xl text-white sm:text-right">{entry.totalPoints}</p>
                </article>
              ))
            ) : (
              <div className="rounded-2xl border border-white/10 bg-slate-950/55 p-5 text-sm leading-6 text-slate-300">
                Brak finalnej czolowki do pokazania.
              </div>
            )}
          </div>
        </div>

        <div className="min-w-0">
          <p className="font-display text-sm uppercase text-emerald-300">Ciekawostki</p>
          <h2 className="mt-2 font-display text-3xl uppercase text-white sm:text-4xl">Co zostalo po finale</h2>
          <div className="mt-5">
            {isLoading ? (
              <div className="rounded-2xl border border-white/10 bg-slate-950/55 p-5 text-sm leading-6 text-slate-300">
                Ladowanie ciekawostek z finalu...
              </div>
            ) : summaryQuery.isError ? (
              <div className="rounded-2xl border border-rose-400/30 bg-rose-950/30 p-5 text-sm leading-6 text-rose-100">
                {getErrorMessage(summaryQuery.error)}
              </div>
            ) : (
              <FinalSummaryFactGrid facts={summary?.globalFacts ?? []} />
            )}
          </div>
        </div>
      </section>
    </main>
  )
}
