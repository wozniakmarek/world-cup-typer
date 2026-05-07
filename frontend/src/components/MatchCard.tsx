import { Link } from 'react-router-dom'
import type { MatchSummary } from '../api/types'
import { formatKickoff, getPredictionLabel } from '../app/formatters'
import { StatusPill } from './StatusPill'

export const MatchCard = ({ match }: { match: MatchSummary }) => {
  return (
    <article className="glass-card rounded-3xl p-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="font-display text-sm uppercase tracking-[0.24em] text-emerald-300/80">
            Mecz #{match.matchNumber} • {match.phase}
          </p>
          <p className="mt-1 text-sm text-slate-400">{formatKickoff(match.kickoffTimeUtc)} • {match.venue || 'Lokalizacja TBD'}</p>
        </div>
        <StatusPill status={match.status} isSettled={match.isSettled} />
      </div>

      <div className="mt-5 space-y-3">
        <div className="flex items-center justify-between rounded-2xl bg-slate-950/40 px-4 py-3">
          <span className="font-display text-xl uppercase">{match.homeTeam.flagEmoji} {match.homeTeam.name}</span>
          <span className="text-slate-500">vs</span>
          <span className="font-display text-xl uppercase text-right">{match.awayTeam.flagEmoji} {match.awayTeam.name}</span>
        </div>
        <div className="flex flex-wrap items-center justify-between gap-3 text-sm text-slate-300">
          <span>Twój typ: <strong className="text-white">{getPredictionLabel(match.myPrediction)}</strong></span>
          <span>{match.isSettled ? `Punkty: ${match.myPoints ?? 0}` : match.canEditPrediction ? 'Typowanie otwarte' : 'Typ zablokowany'}</span>
        </div>
      </div>

      <div className="mt-5">
        <Link
          to={`/matches/${match.id}`}
          className="inline-flex rounded-full bg-emerald-400 px-4 py-2 text-sm font-semibold text-slate-950 transition hover:bg-emerald-300"
        >
          Szczegóły meczu
        </Link>
      </div>
    </article>
  )
}
