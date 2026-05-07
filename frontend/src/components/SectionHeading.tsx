export const SectionHeading = ({
  eyebrow,
  title,
  description,
}: {
  eyebrow?: string
  title: string
  description?: string
}) => {
  return (
    <div className="space-y-1">
      {eyebrow ? <p className="font-display text-sm uppercase tracking-[0.28em] text-emerald-300/80">{eyebrow}</p> : null}
      <h1 className="font-display text-3xl font-bold uppercase tracking-wide text-white sm:text-4xl">{title}</h1>
      {description ? <p className="max-w-3xl text-sm text-slate-300 sm:text-base">{description}</p> : null}
    </div>
  )
}
