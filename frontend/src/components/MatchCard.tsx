import { Link } from 'react-router-dom'
import type { MatchSummary } from '../api/types'
import { canEditMatchPrediction, formatKickoff, formatMatchContext, formatTeamDisplayName, getPredictionLabel } from '../app/formatters'
import { StatusPill } from './StatusPill'

type MatchCardProps = {
  match: MatchSummary
  onOpenDetails?: () => void
}

export const MatchCard = ({ match, onOpenDetails }: MatchCardProps) => {
  const canEditPrediction = canEditMatchPrediction(match)
  const statusMessage = match.isSettled
    ? `Punkty: ${match.myPoints ?? 0}`
    : canEditPrediction
      ? 'Typowanie otwarte'
      : 'Typ zablokowany'

  return (
    <article className="glass-card min-w-0 overflow-hidden rounded-3xl p-4 sm:p-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="min-w-0 space-y-1">
          <p className="font-display text-xs uppercase tracking-[0.28em] text-emerald-300/80 sm:text-sm">
            {formatMatchContext(match)}
          </p>
          <p className="text-sm text-slate-400">{formatKickoff(match.kickoffTimeUtc)}</p>
          <p className="text-xs uppercase tracking-[0.2em] text-slate-500">{match.venue || 'Miejsce do potwierdzenia'}</p>
        </div>
        <StatusPill
          status={match.status}
          isSettled={match.isSettled}
          kickoffTimeUtc={match.kickoffTimeUtc}
        />
      </div>

      <div className="mt-5 space-y-3">
        <div className="grid gap-3 rounded-[1.75rem] bg-slate-950/45 px-4 py-4 sm:grid-cols-[1fr_auto_1fr] sm:items-center">
          <div>
            <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Gospodarze</p>
            <p className="mt-1 break-words font-display text-lg uppercase text-white sm:text-xl">
              {formatTeamDisplayName(match.homeTeam)}
            </p>
          </div>
          <span className="text-center font-display text-sm uppercase tracking-[0.3em] text-slate-500">vs</span>
          <div className="text-left sm:text-right">
            <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Goście</p>
            <p className="mt-1 break-words font-display text-lg uppercase text-white sm:text-xl">
              {formatTeamDisplayName(match.awayTeam)}
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
          onClick={onOpenDetails}
          className="inline-flex w-full items-center justify-center rounded-full bg-emerald-400 px-4 py-2 text-sm font-semibold text-slate-950 transition hover:bg-emerald-300 sm:w-auto"
        >
          Szczegóły meczu
        </Link>
      </div>
    </article>
  )
}
