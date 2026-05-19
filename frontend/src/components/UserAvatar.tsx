import { useEffect, useState } from 'react'
import clsx from 'clsx'

const sizeClassMap = {
  sm: 'h-8 w-8 text-xs',
  md: 'h-10 w-10 text-sm',
  lg: 'h-16 w-16 text-xl',
} as const

const getInitials = (displayName: string) => {
  const parts = displayName.trim().split(/\s+/).filter(Boolean)
  const initials = parts.slice(0, 2).map((part) => part[0]?.toUpperCase()).join('')

  return initials || '?'
}

export const UserAvatar = ({
  displayName,
  avatarUrl,
  size = 'md',
  className,
}: {
  displayName: string
  avatarUrl?: string | null
  size?: keyof typeof sizeClassMap
  className?: string
}) => {
  const [imageFailed, setImageFailed] = useState(false)
  const showImage = Boolean(avatarUrl && !imageFailed)

  useEffect(() => {
    setImageFailed(false)
  }, [avatarUrl])

  return (
    <span
      className={clsx(
        'inline-flex shrink-0 items-center justify-center overflow-hidden rounded-full border border-white/10 bg-emerald-400/15 font-display text-emerald-100 shadow-sm shadow-black/20',
        sizeClassMap[size],
        className,
      )}
      aria-label={`Avatar: ${displayName}`}
      role="img"
    >
      {showImage ? (
        <img
          src={avatarUrl ?? undefined}
          alt=""
          className="h-full w-full object-cover"
          loading="lazy"
          referrerPolicy="no-referrer"
          onError={() => setImageFailed(true)}
        />
      ) : (
        <span aria-hidden="true">{getInitials(displayName)}</span>
      )}
    </span>
  )
}
