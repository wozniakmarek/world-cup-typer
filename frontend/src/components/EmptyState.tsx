export const EmptyState = ({
  title,
  description,
  compact = false,
}: {
  title: string
  description: string
  compact?: boolean
}) => {
  return (
    <div
      className={`rounded-3xl border border-dashed border-slate-700/80 bg-slate-900/30 px-4 text-center ${
        compact ? 'py-6' : 'py-10'
      }`}
    >
      <p className="font-display text-xl uppercase tracking-wide text-white">{title}</p>
      <p className="mt-2 text-sm text-slate-400">{description}</p>
    </div>
  )
}
