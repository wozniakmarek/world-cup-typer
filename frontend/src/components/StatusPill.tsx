import clsx from 'clsx'
import type { MatchStatus } from '../api/types'
import { getPresentationMatchStatus, getResultBadgeClass, matchStatusLabel } from '../app/formatters'

export const StatusPill = ({
  status,
  isSettled,
  kickoffTimeUtc,
}: {
  status: MatchStatus
  isSettled: boolean
  kickoffTimeUtc?: string
}) => {
  const presentationStatus = kickoffTimeUtc
    ? getPresentationMatchStatus({ status, isSettled, kickoffTimeUtc })
    : status

  return (
    <span className={clsx('inline-flex rounded-full px-3 py-1 text-xs font-semibold', getResultBadgeClass(presentationStatus, isSettled))}>
      {matchStatusLabel[presentationStatus]}
    </span>
  )
}
