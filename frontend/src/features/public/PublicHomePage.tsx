import { useQuery } from '@tanstack/react-query'
import { Activity, ArrowRight, CalendarDays, Medal, ShieldCheck, Sparkles, Trophy } from 'lucide-react'
import { Link } from 'react-router-dom'
import heroImage from '../../assets/hero.png'
import { getErrorMessage } from '../../api/client'
import { rankingApi } from '../../api/services'
import { UserAvatar } from '../../components/UserAvatar'
import { buttonClassName, secondaryButtonClassName } from '../../styles/ui'

const highlights = [
  { label: 'Punkty 3/1/0', value: 'czytelne zasady', icon: Medal },
  { label: 'Ranking live', value: 'publiczna tabela', icon: Trophy },
  { label: 'Terminarz', value: 'mecze i typy', icon: CalendarDays },
]

export const PublicHomePage = () => {
  const rankingQuery = useQuery({ queryKey: ['ranking', 'public-top'], queryFn: rankingApi.getTop })
  const ranking = rankingQuery.data ?? []
  const leader = ranking[0]

  return (
    <main className="min-h-screen overflow-hidden bg-pitch-950 text-slate-100">
      <section className="relative isolate overflow-hidden">
        <img
          src={heroImage}
          alt=""
          className="absolute inset-0 -z-20 h-full w-full object-cover opacity-42"
        />
        <div className="absolute inset-0 -z-10 bg-[linear-gradient(110deg,rgba(2,8,23,0.96)_0%,rgba(2,8,23,0.86)_42%,rgba(2,8,23,0.45)_100%)]" />
        <div className="absolute inset-x-0 bottom-0 -z-10 h-40 bg-gradient-to-t from-pitch-950 to-transparent" />

        <div className="mx-auto flex max-w-7xl flex-col px-4 py-5 sm:px-6 lg:px-8">
          <header className="flex items-center justify-between gap-4">
            <Link
              to="/"
              className="font-display text-base font-bold uppercase tracking-[0.22em] text-white sm:text-xl"
            >
              Typer MS
            </Link>
            <Link to="/login" className={secondaryButtonClassName}>
              Logowanie
            </Link>
          </header>

          <div className="grid items-center gap-10 py-8 lg:grid-cols-[minmax(0,1.1fr)_minmax(22rem,0.9fr)] lg:py-10">
            <div className="max-w-3xl">
              <p className="font-display text-sm uppercase tracking-[0.3em] text-emerald-300">Mistrzostwa Swiata 2026</p>
              <h1 className="mt-5 font-display text-5xl font-bold uppercase leading-none text-white sm:text-6xl lg:text-7xl">
                Typer Mistrzostw Świata
              </h1>
              <p className="mt-6 max-w-2xl text-lg leading-8 text-slate-200 sm:text-xl">
                Publiczny pulpit rozgrywki: aktualny ranking, szybki podgląd zasad i wejście do prywatnego typowania dla zaproszonych graczy.
              </p>
              <div className="mt-8 flex flex-col gap-3 sm:flex-row">
                <Link to="/login" className={buttonClassName}>
                  Przejdź do logowania
                  <ArrowRight className="ml-2 h-4 w-4" aria-hidden="true" />
                </Link>
                <a href="#ranking" className={secondaryButtonClassName}>
                  Zobacz ranking
                </a>
              </div>
            </div>

            <aside className="glass-card rounded-[2rem] p-5 sm:p-6">
              <div className="flex items-start justify-between gap-4">
                <div>
                  <p className="font-display text-sm uppercase tracking-[0.24em] text-emerald-300">Lider</p>
                  <p className="mt-2 text-sm text-slate-400">Najwyżej w publicznej tabeli</p>
                </div>
                <Trophy className="h-8 w-8 text-emerald-300" aria-hidden="true" />
              </div>

              {leader ? (
                <div className="mt-8 flex items-center gap-4">
                  <UserAvatar displayName={leader.displayName} avatarUrl={leader.avatarUrl} />
                  <div className="min-w-0">
                    <p className="truncate font-display text-3xl text-white">{leader.displayName}</p>
                    <p className="mt-1 text-sm text-slate-400">{leader.exactScoreHits} dokładne wyniki</p>
                  </div>
                  <p className="ml-auto font-display text-5xl text-emerald-300">{leader.totalPoints}</p>
                </div>
              ) : (
                <p className="mt-8 text-sm text-slate-300">
                  {rankingQuery.isLoading
                    ? 'Ładuję publiczny ranking...'
                    : rankingQuery.isError
                      ? getErrorMessage(rankingQuery.error)
                      : 'Ranking pojawi się po pierwszych rozliczonych typach.'}
                </p>
              )}

              <div className="mt-8 grid gap-3 sm:grid-cols-3 lg:grid-cols-1 xl:grid-cols-3">
                {highlights.map(({ label, value, icon: Icon }) => (
                  <div key={label} className="rounded-2xl border border-white/10 bg-white/[0.04] p-4">
                    <Icon className="h-5 w-5 text-emerald-300" aria-hidden="true" />
                    <p className="mt-3 font-display text-lg text-white">{label}</p>
                    <p className="text-sm text-slate-400">{value}</p>
                  </div>
                ))}
              </div>
            </aside>
          </div>
        </div>
      </section>

      <section id="ranking" className="mx-auto grid max-w-7xl gap-8 px-4 pb-16 pt-6 sm:px-6 lg:grid-cols-[0.82fr_1.18fr] lg:px-8">
        <div>
          <p className="font-display text-sm uppercase tracking-[0.28em] text-emerald-300">Publiczny ranking</p>
          <h2 className="mt-3 font-display text-4xl uppercase text-white sm:text-5xl">Tabela liderów</h2>
          <p className="mt-4 max-w-xl text-base leading-7 text-slate-400">
            Widoczne są tylko informacje potrzebne do rywalizacji: nazwa gracza, punkty i skuteczność typowania.
          </p>
        </div>

        <div className="space-y-3">
          {ranking.length > 0 ? (
            ranking.map((entry) => (
              <article
                key={entry.userId}
                className="grid items-center gap-4 rounded-2xl border border-white/10 bg-slate-950/55 p-4 sm:grid-cols-[4rem_1fr_auto]"
              >
                <p className="font-display text-3xl text-emerald-300">#{entry.position}</p>
                <div className="flex min-w-0 items-center gap-3">
                  <UserAvatar displayName={entry.displayName} avatarUrl={entry.avatarUrl} size="sm" />
                  <div className="min-w-0">
                    <p className="truncate font-semibold text-white">{entry.displayName}</p>
                    <p className="text-sm text-slate-400">
                      {entry.exactScoreHits} dokładne · {entry.correctOutcomeHits} rezultaty · {entry.predictionsCount} typy
                    </p>
                  </div>
                </div>
                <div className="flex items-center gap-2 text-right sm:block">
                  <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Punkty</p>
                  <p className="font-display text-3xl text-white">{entry.totalPoints}</p>
                </div>
              </article>
            ))
          ) : (
            <div className="rounded-2xl border border-white/10 bg-slate-950/55 p-6">
              <Sparkles className="h-6 w-6 text-emerald-300" aria-hidden="true" />
              <p className="mt-4 font-display text-2xl text-white">
                {rankingQuery.isLoading ? 'Ładowanie rankingu' : 'Ranking jest jeszcze pusty'}
              </p>
              <p className="mt-2 text-sm text-slate-400">
                {rankingQuery.isError
                  ? getErrorMessage(rankingQuery.error)
                  : 'Po rozliczeniu pierwszego meczu pojawią się tutaj liderzy.'}
              </p>
            </div>
          )}
        </div>
      </section>

      <section className="border-t border-white/10 bg-slate-950/40">
        <div className="mx-auto grid max-w-7xl gap-5 px-4 py-8 sm:grid-cols-3 sm:px-6 lg:px-8">
          <div className="flex items-start gap-3">
            <ShieldCheck className="mt-1 h-5 w-5 text-emerald-300" aria-hidden="true" />
            <p className="text-sm leading-6 text-slate-300">Logowanie chroni typy, profil i sekcje administracyjne.</p>
          </div>
          <div className="flex items-start gap-3">
            <Activity className="mt-1 h-5 w-5 text-emerald-300" aria-hidden="true" />
            <p className="text-sm leading-6 text-slate-300">Publiczna część pokazuje wyłącznie rywalizację i status aplikacji.</p>
          </div>
          <div className="flex items-start gap-3">
            <Trophy className="mt-1 h-5 w-5 text-emerald-300" aria-hidden="true" />
            <p className="text-sm leading-6 text-slate-300">Po zalogowaniu gracz trafia do dashboardu i właściwych sekcji.</p>
          </div>
        </div>
      </section>
    </main>
  )
}
