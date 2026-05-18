import type { ReactNode } from 'react'
import { EmptyState } from './EmptyState'
import { InlineAlert } from './InlineAlert'
import { Panel } from './Panel'

export const QueryState = ({
  isLoading,
  isError,
  errorMessage,
  isEmpty,
  emptyTitle,
  emptyDescription,
  loadingTitle = 'Ladowanie danych',
  loadingDescription = 'Pobieram najnowsze informacje.',
  children,
}: {
  isLoading: boolean
  isError: boolean
  errorMessage?: string
  isEmpty?: boolean
  emptyTitle?: string
  emptyDescription?: string
  loadingTitle?: string
  loadingDescription?: string
  children: ReactNode
}) => {
  if (isLoading) {
    return (
      <Panel>
        <EmptyState title={loadingTitle} description={loadingDescription} />
      </Panel>
    )
  }

  if (isError) {
    return (
      <Panel>
        <InlineAlert
          tone="error"
          title="Nie udalo sie pobrac danych"
          message={errorMessage ?? 'Sprobuj ponownie za chwile.'}
        />
      </Panel>
    )
  }

  if (isEmpty) {
    return (
      <Panel>
        <EmptyState
          title={emptyTitle ?? 'Brak danych'}
          description={emptyDescription ?? 'Jeszcze nic tutaj nie ma.'}
        />
      </Panel>
    )
  }

  return children
}
