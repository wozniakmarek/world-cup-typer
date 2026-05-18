import type { ReactNode } from 'react'
import clsx from 'clsx'

export const ResponsiveTable = ({
  table,
  cards,
  className,
}: {
  table: ReactNode
  cards: ReactNode
  className?: string
}) => {
  return (
    <div className={clsx(className)}>
      <div className="hidden md:block">{table}</div>
      <div className="space-y-3 md:hidden">{cards}</div>
    </div>
  )
}
