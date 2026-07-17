import { useQuery } from '@tanstack/react-query'
import { AlertTriangle, ArrowLeft, BarChart3, Medal, Sparkles, Target, Trophy } from 'lucide-react'
import { Link } from 'react-router-dom'
import { getErrorMessage } from '../../api/client'
import { summaryApi } from '../../api/services'
import { Panel } from '../../components/Panel'
import { QueryState } from '../../components/QueryState'
import { SectionHeading } from '../../components/SectionHeading'
import { StatCard } from '../../components/StatCard'
import { UserAvatar } from '../../components/UserAvatar'
import { secondaryButtonClassName } from '../../styles/ui'
import { FinalRankingStoryChart } from './FinalRankingStoryChart'
import { FinalSummaryFactGrid } from './FinalSummaryFactGrid'

const numberFormatter = new Intl.NumberFormat('pl-PL')

const formatNumber = (value: number) => numberFormatter.format(value)

export const PersonalFinalSummaryPage = () => {
  const summaryQuery = useQuery({ queryKey: ['summary', 'final', 'me'], queryFn: summaryApi.getMine })
  const finalSummaryQuery = useQuery({
    queryKey: ['summary', 'final', 'authenticated'],
    queryFn: summaryApi.getFinal,
  })
  const summary = summaryQuery.data
  const finalSummary = finalSummaryQuery.data

  return (
    <div className="space-y-6">
      <SectionHeading
        eyebrow="Mój recap"
        title="Twój finałowy recap"
        description="Osobiste podsumowanie turnieju: miejsce, punkty, trafienia i ciekawostki tylko o Twoim typowaniu."
      />

      <QueryState
        isLoading={summaryQuery.isLoading}
        isError={summaryQuery.isError}
        errorMessage={getErrorMessage(summaryQuery.error)}
        isEmpty={!summary}
        emptyTitle="Brak personalnego recap"
        emptyDescription="Gdy finalne dane zostaną przeliczone dla Twojego konta, pojawią się tutaj."
        loadingTitle="Ładowanie Twojego recap"
        loadingDescription="Pobieram finałowe statystyki i ciekawostki gracza."
      >
        {summary ? (
          <>
            <Panel className="relative overflow-hidden bg-[radial-gradient(circle_at_top_left,rgba(16,185,129,0.22),transparent_28rem)] p-5 sm:p-6">
              <div className="relative flex flex-col gap-6 lg:flex-row lg:items-center lg:justify-between">
                <div className="flex min-w-0 flex-col gap-4 sm:flex-row sm:items-center">
                  <UserAvatar
                    displayName={summary.displayName}
                    avatarUrl={summary.avatarUrl}
                    size="lg"
                    className="h-20 w-20 text-2xl"
                  />
                  <div className="min-w-0">
                    <p className="font-display text-xs uppercase tracking-[0.28em] text-emerald-300/80">
                      Personalne podsumowanie
                    </p>
                    <h2 className="mt-2 break-words font-display text-3xl font-bold uppercase leading-tight text-white sm:text-4xl">
                      {summary.displayName}
                    </h2>
                    <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-300">
                      Tak wygląda Twój turniej po ostatnim gwizdku: finałowe miejsce, dorobek punktowy i momenty, które
                      wyróżniły Twoje typy.
                    </p>
                  </div>
                </div>

                <div className="grid gap-4 sm:grid-cols-2 lg:min-w-80">
                  <div className="border-l border-white/10 pl-4">
                    <Medal className="h-5 w-5 text-emerald-300" aria-hidden="true" />
                    <p className="mt-3 text-xs uppercase tracking-[0.22em] text-slate-400">Finalne miejsce</p>
                    <p className="font-display text-4xl text-white">{formatNumber(summary.finalPosition)}. miejsce</p>
                  </div>
                  <div className="border-l border-white/10 pl-4">
                    <Trophy className="h-5 w-5 text-emerald-300" aria-hidden="true" />
                    <p className="mt-3 text-xs uppercase tracking-[0.22em] text-slate-400">Punkty</p>
                    <p className="font-display text-4xl text-white">{formatNumber(summary.totalPoints)} pkt</p>
                  </div>
                </div>
              </div>
            </Panel>

            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
              <StatCard label="Finalne miejsce" value={`#${formatNumber(summary.finalPosition)}`} accent="text-emerald-300" />
              <StatCard label="Punkty" value={formatNumber(summary.totalPoints)} />
              <StatCard label="Dokładne wyniki" value={formatNumber(summary.exactScoreHits)} />
              <StatCard label="Trafione rezultaty" value={formatNumber(summary.correctOutcomeHits)} />
              <StatCard label="Oddane typy" value={formatNumber(summary.predictionsCount)} />
            </div>

            <section className="space-y-4">
              <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
                <div className="min-w-0">
                  <p className="font-display text-sm uppercase tracking-[0.28em] text-emerald-300/80">
                    Twoje ciekawostki
                  </p>
                  <h2 className="mt-2 break-words font-display text-2xl font-bold uppercase leading-tight text-white sm:text-3xl">
                    Co było Twoim znakiem rozpoznawczym
                  </h2>
                </div>
                <Link to="/" className={secondaryButtonClassName}>
                  <ArrowLeft className="mr-2 h-4 w-4" aria-hidden="true" />
                  Wróć do dashboardu
                </Link>
              </div>

              <FinalSummaryFactGrid facts={summary.personalFacts} />
            </section>

            {finalSummaryQuery.isLoading ? (
              <Panel className="flex items-start gap-3 border-white/10 bg-slate-950/45">
                <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-emerald-400/12 text-emerald-300">
                  <BarChart3 className="h-5 w-5" aria-hidden="true" />
                </span>
                <div className="min-w-0">
                  <p className="font-display text-lg uppercase text-white">Ładowanie danych turnieju</p>
                  <p className="mt-1 text-sm leading-6 text-slate-400">
                    Pobieram ogólne ciekawostki i przebieg tabeli dla zalogowanego gracza.
                  </p>
                </div>
              </Panel>
            ) : finalSummaryQuery.isError ? (
              <div role="alert">
                <Panel className="flex items-start gap-3 border-rose-400/30 bg-rose-950/30">
                  <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-rose-400/12 text-rose-200">
                    <AlertTriangle className="h-5 w-5" aria-hidden="true" />
                  </span>
                  <div className="min-w-0">
                    <p className="font-display text-lg uppercase text-white">Nie udało się pobrać danych turnieju</p>
                    <p className="mt-1 text-sm leading-6 text-rose-100/85">
                      Personalny recap zostaje dostępny. Dodatkowe ciekawostki i wykres można spróbować wczytać
                      ponownie po odświeżeniu strony.
                    </p>
                    <p className="mt-2 break-words text-xs leading-5 text-rose-100/70">
                      {getErrorMessage(finalSummaryQuery.error)}
                    </p>
                  </div>
                </Panel>
              </div>
            ) : finalSummary ? (
              <>
                <section className="space-y-4">
                  <div className="min-w-0">
                    <p className="font-display text-sm uppercase tracking-[0.28em] text-emerald-300/80">
                      Ogólne ciekawostki
                    </p>
                    <h2 className="mt-2 break-words font-display text-2xl font-bold uppercase leading-tight text-white sm:text-3xl">
                      Co wyróżniło cały turniej
                    </h2>
                  </div>

                  <FinalSummaryFactGrid facts={finalSummary.globalFacts} />
                </section>

                <FinalRankingStoryChart
                  series={finalSummary.positionSeries}
                  eyebrow="Twój przebieg w tabeli"
                  title="Jak zmieniało się Twoje miejsce"
                  description="Linie pokazują finalny ruch rankingu po kolejnych meczach. Filtr Mój przebieg podświetla Twoją linię bez tracenia kontekstu całej stawki."
                  initialFilterMode="mine"
                />
              </>
            ) : (
              <Panel className="flex items-start gap-3 border-white/10 bg-slate-950/45">
                <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-emerald-400/12 text-emerald-300">
                  <BarChart3 className="h-5 w-5" aria-hidden="true" />
                </span>
                <div className="min-w-0">
                  <p className="font-display text-lg uppercase text-white">Brak ogólnego podsumowania</p>
                  <p className="mt-1 text-sm leading-6 text-slate-400">
                    Gdy finalne dane turnieju będą dostępne, pojawią się tutaj ogólne ciekawostki i przebieg tabeli.
                  </p>
                </div>
              </Panel>
            )}

            <Panel className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div className="flex min-w-0 items-start gap-3">
                <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-emerald-400/12 text-emerald-300">
                  {summary.personalFacts.length ? (
                    <Sparkles className="h-5 w-5" aria-hidden="true" />
                  ) : (
                    <Target className="h-5 w-5" aria-hidden="true" />
                  )}
                </span>
                <div className="min-w-0">
                  <p className="font-display text-lg uppercase text-white">Finałowa metryka</p>
                  <p className="text-sm leading-6 text-slate-400">
                    {summary.predictionsCount > 0
                      ? `Masz ${formatNumber(summary.predictionsCount)} typów w finalnym rozliczeniu.`
                      : 'Nie ma jeszcze zapisanych typów w finalnym rozliczeniu.'}
                  </p>
                </div>
              </div>
              <Link to="/ranking" className={secondaryButtonClassName}>
                Zobacz ranking
              </Link>
            </Panel>
          </>
        ) : null}
      </QueryState>
    </div>
  )
}
