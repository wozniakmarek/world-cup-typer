export const StatCard = ({
  label,
  value,
  accent,
}: {
  label: string
  value: React.ReactNode
  accent?: string
}) => {
  return (
    <div className="glass-card animate-rise rounded-3xl p-4">
      <p className="text-xs uppercase tracking-[0.24em] text-slate-400">{label}</p>
      <p className={`mt-3 font-display text-3xl font-bold text-white ${accent ?? ''}`}>{value}</p>
    </div>
  )
}
