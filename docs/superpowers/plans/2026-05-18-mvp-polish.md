# MVP Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dopiac frontendowe MVP przez wspolne stany `loading/error/empty/success`, lepsza mobilnosc i czytelniejsze widoki gracza i admina bez zmian backendu ani API.

**Architecture:** Wprowadzamy maly zestaw wspolnych komponentow UI w `frontend/src/components`, a nastepnie stosujemy je konsekwentnie na istniejacych stronach funkcjonalnych. Zakres pozostaje po stronie frontendu `React + TanStack Query + Tailwind`, bez nowych endpointow i bez refaktoru routingu.

**Tech Stack:** React, TypeScript, Vite, Tailwind CSS, TanStack Query, React Router

---

### Task 1: Shared Query And Feedback Patterns

**Files:**
- Create: `frontend/src/components/InlineAlert.tsx`
- Create: `frontend/src/components/QueryState.tsx`
- Create: `frontend/src/components/ResponsiveTable.tsx`
- Modify: `frontend/src/components/EmptyState.tsx`
- Modify: `frontend/src/styles/ui.ts`

- [ ] **Step 1: Write the failing test target as a build baseline**

Run:

```bash
cd frontend
npm run build
```

Expected: PASS before changes, giving a known-good baseline for the UI refactor.

- [ ] **Step 2: Add a reusable inline alert component**

Create `frontend/src/components/InlineAlert.tsx`:

```tsx
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
```

- [ ] **Step 3: Add a shared query state renderer**

Create `frontend/src/components/QueryState.tsx`:

```tsx
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

  return <>{children}</>
}
```

- [ ] **Step 4: Add a mobile-friendly record list wrapper**

Create `frontend/src/components/ResponsiveTable.tsx`:

```tsx
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
```

- [ ] **Step 5: Expand shared styling helpers used by the new components**

Modify `frontend/src/styles/ui.ts`:

```ts
export const inputClassName =
  'w-full rounded-2xl border border-white/10 bg-slate-950/60 px-4 py-3 text-sm text-white outline-none transition placeholder:text-slate-500 focus:border-emerald-400/70'

export const buttonClassName =
  'inline-flex items-center justify-center rounded-full bg-emerald-400 px-4 py-2 text-sm font-semibold text-slate-950 transition hover:bg-emerald-300 disabled:cursor-not-allowed disabled:opacity-60'

export const secondaryButtonClassName =
  'inline-flex items-center justify-center rounded-full border border-white/10 px-4 py-2 text-sm font-semibold text-slate-200 transition hover:border-emerald-400/50 hover:text-white'

export const filterButtonClassName = (active: boolean) =>
  `rounded-full px-4 py-2 text-sm font-semibold transition ${
    active
      ? 'bg-emerald-400 text-slate-950'
      : 'bg-white/5 text-slate-300 hover:bg-white/10 hover:text-white'
  }`

export const mobileRecordClassName =
  'rounded-3xl border border-white/10 bg-slate-950/45 px-4 py-4'
```

Modify `frontend/src/components/EmptyState.tsx`:

```tsx
export const EmptyState = ({
  title,
  description,
  compact = false,
}: {
  title: string
  description: string
  compact?: boolean
}) => {
  return (
    <div className={`rounded-3xl border border-dashed border-slate-700/80 bg-slate-900/30 px-4 text-center ${compact ? 'py-6' : 'py-10'}`}>
      <p className="font-display text-xl uppercase tracking-wide text-white">{title}</p>
      <p className="mt-2 text-sm text-slate-400">{description}</p>
    </div>
  )
}
```

- [ ] **Step 6: Run the frontend build after shared component changes**

Run:

```bash
cd frontend
npm run build
```

Expected: PASS with the new shared UI layer in place.

- [ ] **Step 7: Commit the shared UI foundation**

Run:

```bash
git add frontend/src/components/InlineAlert.tsx frontend/src/components/QueryState.tsx frontend/src/components/ResponsiveTable.tsx frontend/src/components/EmptyState.tsx frontend/src/styles/ui.ts
git commit -m "feat: add shared UI polish patterns"
```

### Task 2: Polish Player-Facing Pages

**Files:**
- Modify: `frontend/src/components/MatchCard.tsx`
- Modify: `frontend/src/features/dashboard/DashboardPage.tsx`
- Modify: `frontend/src/features/matches/MatchesPage.tsx`
- Modify: `frontend/src/features/matches/MatchDetailsPage.tsx`
- Modify: `frontend/src/features/ranking/RankingPage.tsx`
- Modify: `frontend/src/features/profile/ProfilePage.tsx`

- [ ] **Step 1: Write the failing test target as a build guard**

Run:

```bash
cd frontend
npm run build
```

Expected: PASS before page changes, confirming Task 1 did not destabilize the app.

- [ ] **Step 2: Improve dashboard states and empty handling**

Modify `frontend/src/features/dashboard/DashboardPage.tsx` to use `QueryState` and `InlineAlert` patterns:

```tsx
import { EmptyState } from '../../components/EmptyState'
import { QueryState } from '../../components/QueryState'

const topEntries = topQuery.data ?? []

<QueryState
  isLoading={matchesQuery.isLoading || upcomingQuery.isLoading || topQuery.isLoading}
  isError={matchesQuery.isError || upcomingQuery.isError || topQuery.isError}
  errorMessage="Nie udalo sie pobrac dashboardu. Odswiez widok i sprobuj ponownie."
>
  <div className="grid gap-6 xl:grid-cols-[1.7fr_1fr]">
    <Panel className="space-y-4">
      <div className="grid gap-4">
        {upcomingMatches.length > 0 ? (
          upcomingMatches.slice(0, 3).map((match) => <MatchCard key={match.id} match={match} />)
        ) : (
          <EmptyState
            compact
            title="Brak nadchodzacych meczow"
            description="Kiedy pojawia sie kolejne spotkania, zobaczysz je tutaj."
          />
        )}
      </div>
    </Panel>
  </div>
</QueryState>
```

- [ ] **Step 3: Improve match list filters and empty results**

Modify `frontend/src/features/matches/MatchesPage.tsx`:

```tsx
import { EmptyState } from '../../components/EmptyState'
import { QueryState } from '../../components/QueryState'
import { filterButtonClassName } from '../../styles/ui'

<div className="flex flex-wrap gap-2">
  {filters.map((item) => (
    <button
      key={item.key}
      type="button"
      onClick={() => setFilter(item.key)}
      className={filterButtonClassName(filter === item.key)}
    >
      {item.label}
    </button>
  ))}
</div>

<QueryState
  isLoading={matchesQuery.isLoading}
  isError={matchesQuery.isError}
  errorMessage="Nie udalo sie pobrac listy meczow."
  isEmpty={matches.length === 0}
  emptyTitle="Brak meczow dla tego filtra"
  emptyDescription="Zmien filtr albo wroc pozniej, gdy pojawia sie nowe spotkania."
>
  <div className="grid gap-4 xl:grid-cols-2">
    {matches.map((match) => (
      <MatchCard key={match.id} match={match} />
    ))}
  </div>
</QueryState>
```

- [ ] **Step 4: Polish match card and match details states**

Modify `frontend/src/components/MatchCard.tsx`:

```tsx
<article className="glass-card rounded-3xl p-5">
  <div className="mt-5 rounded-2xl bg-slate-950/40 px-4 py-4">
    <div className="flex items-start justify-between gap-3">
      <div className="min-w-0">
        <p className="font-display text-lg uppercase leading-tight sm:text-xl">
          {match.homeTeam.flagEmoji} {match.homeTeam.name}
        </p>
        <p className="mt-2 text-xs uppercase tracking-[0.2em] text-slate-500">kontra</p>
        <p className="mt-2 font-display text-lg uppercase leading-tight sm:text-xl">
          {match.awayTeam.flagEmoji} {match.awayTeam.name}
        </p>
      </div>
      <div className="text-right text-xs text-slate-400">
        <p>Twoj typ</p>
        <p className="mt-1 font-semibold text-white">{getPredictionLabel(match.myPrediction)}</p>
      </div>
    </div>
  </div>
</article>
```

Modify `frontend/src/features/matches/MatchDetailsPage.tsx` to use proper loading/error/success blocks:

```tsx
import { InlineAlert } from '../../components/InlineAlert'
import { QueryState } from '../../components/QueryState'

<QueryState
  isLoading={matchQuery.isLoading}
  isError={matchQuery.isError}
  errorMessage="Nie udalo sie pobrac szczegolow meczu."
>
  {feedback ? <InlineAlert tone="success" message={feedback} /> : null}
  {!match.canEditPrediction ? (
    <InlineAlert
      tone="warning"
      title="Typ zablokowany"
      message="Po kickoffie backend blokuje zapis i edycje typu niezaleznie od UI."
    />
  ) : null}
</QueryState>
```

- [ ] **Step 5: Make ranking and profile mobile-first**

Modify `frontend/src/features/ranking/RankingPage.tsx` and `frontend/src/features/profile/ProfilePage.tsx`:

```tsx
import { QueryState } from '../../components/QueryState'
import { ResponsiveTable } from '../../components/ResponsiveTable'
import { mobileRecordClassName } from '../../styles/ui'

<QueryState
  isLoading={rankingQuery.isLoading}
  isError={rankingQuery.isError}
  errorMessage="Nie udalo sie pobrac rankingu."
  isEmpty={ranking.length === 0}
  emptyTitle="Ranking jest pusty"
  emptyDescription="Po rozliczeniu pierwszego meczu pojawia sie tutaj tabela."
>
  <ResponsiveTable
    table={<table className="min-w-full text-sm">...</table>}
    cards={ranking.map((entry) => (
      <article key={entry.userId} className={mobileRecordClassName}>
        <p className="font-display text-xl text-white">#{entry.position} {entry.displayName}</p>
        <p className="mt-2 text-sm text-slate-300">Punkty: {entry.totalPoints}</p>
      </article>
    ))}
  />
</QueryState>
```

For profile history/progress:

```tsx
<QueryState
  isLoading={myRankingQuery.isLoading || progressQuery.isLoading || predictionsQuery.isLoading}
  isError={myRankingQuery.isError || progressQuery.isError || predictionsQuery.isError}
  errorMessage="Nie udalo sie pobrac Twoich statystyk."
>
  {progress.length === 0 ? (
    <EmptyState compact title="Brak snapshotow progresu" description="Po rozliczeniu kolejnych meczow zobaczysz tutaj przebieg turnieju." />
  ) : (
    progress.map((point) => <div key={point.matchId}>...</div>)
  )}
</QueryState>
```

- [ ] **Step 6: Run the frontend build after player page updates**

Run:

```bash
cd frontend
npm run build
```

Expected: PASS with player pages using the new shared state patterns.

- [ ] **Step 7: Commit the player-facing polish**

Run:

```bash
git add frontend/src/components/MatchCard.tsx frontend/src/features/dashboard/DashboardPage.tsx frontend/src/features/matches/MatchesPage.tsx frontend/src/features/matches/MatchDetailsPage.tsx frontend/src/features/ranking/RankingPage.tsx frontend/src/features/profile/ProfilePage.tsx
git commit -m "feat: polish player-facing MVP flows"
```

### Task 3: Polish Admin Pages

**Files:**
- Modify: `frontend/src/features/admin/AdminDashboardPage.tsx`
- Modify: `frontend/src/features/admin/AdminPlayersPage.tsx`
- Modify: `frontend/src/features/admin/AdminTeamsPage.tsx`
- Modify: `frontend/src/features/admin/AdminMatchesPage.tsx`
- Modify: `frontend/src/components/AppNavigation.tsx`

- [ ] **Step 1: Write the failing test target as a build guard**

Run:

```bash
cd frontend
npm run build
```

Expected: PASS before admin polish begins.

- [ ] **Step 2: Improve admin dashboard states and entry points**

Modify `frontend/src/features/admin/AdminDashboardPage.tsx`:

```tsx
import { QueryState } from '../../components/QueryState'

<QueryState
  isLoading={playersQuery.isLoading || matchesQuery.isLoading || teamsQuery.isLoading}
  isError={playersQuery.isError || matchesQuery.isError || teamsQuery.isError}
  errorMessage="Nie udalo sie pobrac danych panelu admina."
>
  <div className="grid gap-6 xl:grid-cols-3">
    <Panel className="space-y-4">
      <p className="font-display text-2xl uppercase text-white">Zarzadzanie graczami</p>
      <Link ...>Otworz panel graczy</Link>
    </Panel>
  </div>
</QueryState>
```

- [ ] **Step 3: Convert players and teams lists to responsive table + cards**

Modify `frontend/src/features/admin/AdminPlayersPage.tsx`:

```tsx
import { InlineAlert } from '../../components/InlineAlert'
import { QueryState } from '../../components/QueryState'
import { ResponsiveTable } from '../../components/ResponsiveTable'
import { mobileRecordClassName } from '../../styles/ui'

{feedback ? <InlineAlert tone="success" message={feedback} /> : null}

<QueryState
  isLoading={playersQuery.isLoading}
  isError={playersQuery.isError}
  errorMessage="Nie udalo sie pobrac listy graczy."
  isEmpty={(playersQuery.data ?? []).length === 0}
  emptyTitle="Brak graczy"
  emptyDescription="Dodaj pierwsze konto gracza, aby zaczac."
>
  <ResponsiveTable
    table={<table className="min-w-full text-sm">...</table>}
    cards={(playersQuery.data ?? []).map((player) => (
      <article key={player.id} className={mobileRecordClassName}>
        <p className="font-semibold text-white">{player.displayName}</p>
        <p className="mt-1 text-sm text-slate-400">{player.email}</p>
      </article>
    ))}
  />
</QueryState>
```

Apply the same pattern in `frontend/src/features/admin/AdminTeamsPage.tsx`:

```tsx
<ResponsiveTable
  table={<table className="min-w-full text-sm">...</table>}
  cards={(teamsQuery.data ?? []).map((team) => (
    <article key={team.id} className={mobileRecordClassName}>
      <p className="font-semibold text-white">{team.flagEmoji} {team.name}</p>
      <p className="mt-1 text-sm text-slate-400">Skrot: {team.shortName} • Grupa: {team.groupName || '—'}</p>
    </article>
  ))}
/>
```

- [ ] **Step 4: Break up the heavy admin match page and standardize alerts**

Modify `frontend/src/features/admin/AdminMatchesPage.tsx`:

```tsx
import { InlineAlert } from '../../components/InlineAlert'
import { QueryState } from '../../components/QueryState'
import { mobileRecordClassName } from '../../styles/ui'

{feedback ? <InlineAlert tone="success" message={feedback} /> : null}

<div className="grid gap-6 2xl:grid-cols-[1.1fr_1.1fr_1.4fr]">
  <Panel>...formularz meczu...</Panel>
  <Panel>...wynik i rozliczenie...</Panel>
  <Panel className="space-y-4">
    <QueryState
      isLoading={matchesQuery.isLoading}
      isError={matchesQuery.isError}
      errorMessage="Nie udalo sie pobrac meczow admina."
      isEmpty={sortedMatches.length === 0}
      emptyTitle="Brak meczow"
      emptyDescription="Dodaj pierwszy mecz, aby zaczac ukladac terminarz."
    >
      <div className="space-y-3">
        {sortedMatches.map((match) => (
          <article key={match.id} className={mobileRecordClassName}>
            <p className="font-semibold text-white">#{match.matchNumber} {match.homeTeam.name} vs {match.awayTeam.name}</p>
          </article>
        ))}
      </div>
    </QueryState>
  </Panel>
</div>
```

- [ ] **Step 5: Improve navigation wrapping for smaller viewports**

Modify `frontend/src/components/AppNavigation.tsx`:

```tsx
<div className="flex flex-col gap-3 sm:flex-row sm:items-center">
  <div className="rounded-full border border-white/10 bg-white/5 px-4 py-2 text-sm text-slate-200">
    {user?.displayName}
  </div>
  <button ...>Wyloguj</button>
</div>

<nav className="flex gap-2 overflow-x-auto pb-1">
  {links.map(({ to, label, icon: Icon }) => (
    <NavLink
      key={to}
      to={to}
      className={({ isActive }) => clsx('inline-flex shrink-0 items-center gap-2 rounded-full px-4 py-2 text-sm font-medium transition', ...)}
    >
      <Icon className="h-4 w-4" />
      {label}
    </NavLink>
  ))}
</nav>
```

- [ ] **Step 6: Run the frontend build after admin page updates**

Run:

```bash
cd frontend
npm run build
```

Expected: PASS with admin pages using mobile-friendly records and consistent feedback.

- [ ] **Step 7: Commit the admin polish**

Run:

```bash
git add frontend/src/features/admin/AdminDashboardPage.tsx frontend/src/features/admin/AdminPlayersPage.tsx frontend/src/features/admin/AdminTeamsPage.tsx frontend/src/features/admin/AdminMatchesPage.tsx frontend/src/components/AppNavigation.tsx
git commit -m "feat: polish admin MVP flows"
```

### Task 4: Manual Verification And Final Review

**Files:**
- Modify: `docs/mvp-status.md`

- [ ] **Step 1: Run the production build one last time**

Run:

```bash
cd frontend
npm run build
```

Expected: PASS with no TypeScript or Vite build errors.

- [ ] **Step 2: Start the local frontend for manual verification**

Run:

```bash
cd frontend
npm run dev
```

Expected: Vite dev server starts on `http://localhost:5173`.

- [ ] **Step 3: Walk the representative player flows**

Verify manually:

```text
1. Login as seeded player account.
2. Open dashboard and confirm loading/empty handling looks intentional.
3. Open matches and switch every filter.
4. Open match details and verify locked/open/result states are clearer.
5. Open ranking and profile on narrow viewport widths.
```

Expected: No raw empty lists, no cramped card layout, and no unreadable status blocks.

- [ ] **Step 4: Walk the representative admin flows**

Verify manually:

```text
1. Login as admin.
2. Open admin dashboard.
3. Open players, teams, and matches pages on phone-width viewport.
4. Trigger at least one success message (for example save or reset password).
5. Confirm list views remain usable without relying only on horizontal scroll.
```

Expected: Core admin actions stay tappable and readable on smaller screens.

- [ ] **Step 5: Update the MVP status document with the completed polish milestone**

Modify `docs/mvp-status.md`:

```md
## Dodatkowo dopiete po MVP bazowym

- wspolne stany `loading/error/empty/success` po stronie frontendu
- lepsza mobilnosc widokow gracza i admina
- bardziej przewidywalne komunikaty po akcjach formularzy i mutacji
```

- [ ] **Step 6: Commit the polish verification and status update**

Run:

```bash
git add docs/mvp-status.md
git commit -m "docs: update MVP polish status"
```

## Self-Review

- Spec coverage: plan obejmuje wspolne wzorce UI, widoki player, widoki admin i koncowa weryfikacje bez dotykania backendu.
- Placeholder scan: brak pustych sekcji i opisow bez konkretnych plikow albo komend.
- Type consistency: wszystkie nowe elementy odnosza sie do realnych plikow i obecnej struktury `frontend/src/components`, `frontend/src/features` oraz `docs`.

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-05-18-mvp-polish.md`. Two execution options:

**1. Subagent-Driven (recommended)** - I dispatch a fresh subagent per task, review between tasks, fast iteration

**2. Inline Execution** - Execute tasks in this session using executing-plans, batch execution with checkpoints

Which approach?
