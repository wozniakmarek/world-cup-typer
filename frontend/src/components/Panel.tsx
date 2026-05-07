import clsx from 'clsx'

export const Panel = ({
  children,
  className,
}: {
  children: React.ReactNode
  className?: string
}) => {
  return <section className={clsx('glass-card rounded-3xl p-5 text-slate-100', className)}>{children}</section>
}
