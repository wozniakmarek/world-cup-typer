import clsx from 'clsx'
import type { MatchStatus } from '../api/types'
import { getResultBadgeClass, matchStatusLabel } from '../app/formatters'

export const StatusPill = ({
  status,
  isSettled,
}: {
  status: MatchStatus
  isSettled: boolean
}) => {
  return (
    <span className={clsx('inline-flex rounded-full px-3 py-1 text-xs font-semibold', getResultBadgeClass(status, isSettled))}>
      {matchStatusLabel[status]}
    </span>
  )
}
