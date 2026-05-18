import clsx from 'clsx'

const toneClassMap = {
  info: 'border-sky-400/30 bg-sky-500/10 text-sky-100',
  success: 'border-emerald-400/30 bg-emerald-500/10 text-emerald-100',
  warning: 'border-amber-400/30 bg-amber-500/10 text-amber-100',
  error: 'border-rose-400/30 bg-rose-500/10 text-rose-100',
} as const

export const InlineAlert = ({
  title,
  message,
  tone = 'info',
  className,
}: {
  title?: string
  message: string
  tone?: keyof typeof toneClassMap
  className?: string
}) => {
  return (
    <div className={clsx('rounded-3xl border px-4 py-3 text-sm', toneClassMap[tone], className)}>
      {title ? <p className="font-semibold text-white">{title}</p> : null}
      <p className={title ? 'mt-1' : undefined}>{message}</p>
    </div>
  )
}
