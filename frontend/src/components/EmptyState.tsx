export const EmptyState = ({
  title,
  description,
}: {
  title: string
  description: string
}) => {
  return (
    <div className="rounded-3xl border border-dashed border-slate-700/80 bg-slate-900/30 px-4 py-10 text-center">
      <p className="font-display text-xl uppercase tracking-wide text-white">{title}</p>
      <p className="mt-2 text-sm text-slate-400">{description}</p>
    </div>
  )
}
