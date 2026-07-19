import { Sparkles } from 'lucide-react'
import type { FinalSummaryFact } from '../../api/types'

interface FinalSummaryFactGridProps {
  facts: FinalSummaryFact[]
}

export const FinalSummaryFactGrid = ({ facts }: FinalSummaryFactGridProps) => {
  if (facts.length === 0) {
    return (
      <div className="rounded-2xl border border-dashed border-white/15 bg-slate-950/45 p-5 text-sm leading-6 text-slate-300">
        Ciekawostki z finału pojawią się tutaj po przeliczeniu danych turnieju.
      </div>
    )
  }

  return (
    <div className="grid gap-3 md:grid-cols-2">
      {facts.map((fact) => (
        <article key={fact.id} className="min-w-0 rounded-2xl border border-white/10 bg-slate-950/55 p-4">
          <div className="flex items-start gap-3">
            <span className="mt-1 flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-emerald-400/12 text-emerald-300">
              <Sparkles className="h-4 w-4" aria-hidden="true" />
            </span>
            <div className="min-w-0">
              <p className="break-words font-display text-xs uppercase leading-5 text-emerald-300">{fact.label}</p>
              <h3 className="mt-1 break-words font-display text-xl leading-tight text-white">{fact.title}</h3>
              <p className="mt-2 break-words text-sm leading-6 text-slate-400">{fact.description}</p>
            </div>
          </div>
        </article>
      ))}
    </div>
  )
}
