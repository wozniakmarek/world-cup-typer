import clsx from 'clsx'

export const Panel = ({
  children,
  className,
}: {
  children: React.ReactNode
  className?: string
}) => {
  return <section className={clsx('glass-card min-w-0 rounded-3xl p-4 text-slate-100 sm:p-5', className)}>{children}</section>
}
