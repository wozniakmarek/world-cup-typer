import { Link } from 'react-router-dom'
import type { MatchSummary } from '../api/types'
import { formatKickoff, getPredictionLabel } from '../app/formatters'
import { StatusPill } from './StatusPill'

export const MatchCard = ({ match }: { match: MatchSummary }) => {
  const statusMessage = match.isSettled
    ? `Punkty: ${match.myPoints ?? 0}`
    : match.canEditPrediction
      ? 'Typowanie otwarte'
      : 'Typ zablokowany'

  return (
    <article className="glass-card rounded-3xl p-4 sm:p-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="space-y-1">
          <p className="font-display text-xs uppercase tracking-[0.28em] text-emerald-300/80 sm:text-sm">
            Mecz #{match.matchNumber} • {match.phase}
          </p>
          <p className="text-sm text-slate-400">{formatKickoff(match.kickoffTimeUtc)}</p>
          <p className="text-xs uppercase tracking-[0.2em] text-slate-500">{match.venue || 'Lokalizacja TBD'}</p>
        </div>
        <StatusPill status={match.status} isSettled={match.isSettled} />
      </div>

      <div className="mt-5 space-y-3">
        <div className="grid gap-3 rounded-[1.75rem] bg-slate-950/45 px-4 py-4 sm:grid-cols-[1fr_auto_1fr] sm:items-center">
          <div>
            <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Gospodarze</p>
            <p className="mt-1 font-display text-lg uppercase text-white sm:text-xl">
              {match.homeTeam.flagEmoji} {match.homeTeam.name}
            </p>
          </div>
          <span className="text-center font-display text-sm uppercase tracking-[0.3em] text-slate-500">vs</span>
          <div className="text-left sm:text-right">
            <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Goście</p>
            <p className="mt-1 font-display text-lg uppercase text-white sm:text-xl">
              {match.awayTeam.flagEmoji} {match.awayTeam.name}
            </p>
          </div>
        </div>

        <div className="grid gap-3 rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm text-slate-300 sm:grid-cols-2">
          <div>
            <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Twój typ</p>
            <p className="mt-1 font-display text-2xl text-white">{getPredictionLabel(match.myPrediction)}</p>
          </div>
          <div className="sm:text-right">
            <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Status</p>
            <p className="mt-1 font-semibold text-white">{statusMessage}</p>
          </div>
        </div>
      </div>

      <div className="mt-5 flex justify-end sm:justify-start">
        <Link
          to={`/matches/${match.id}`}
          className="inline-flex w-full items-center justify-center rounded-full bg-emerald-400 px-4 py-2 text-sm font-semibold text-slate-950 transition hover:bg-emerald-300 sm:w-auto"
        >
          Szczegóły meczu
        </Link>
      </div>
    </article>
  )
}
