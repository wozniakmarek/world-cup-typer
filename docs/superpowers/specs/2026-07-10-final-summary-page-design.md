# Final Summary Page Design

## Goal

After the World Cup ends, the public entry page becomes a tournament recap instead of a normal in-progress landing page. It should feel like a polished closing screen for the private group, while keeping login available for existing app functions.

The page has one main public experience: an animated full-table ranking story. Below it, the page shows global tournament curiosities. After login, each player gets a personal recap with facts about their own tournament.

## User Experience

### Public Hero

The first screen keeps the current WorldCupTyper visual language: dark pitch background, emerald accent, glass panels, Rajdhani display headings, and compact controls. It should look like the existing ranking/dashboard, not a separate microsite.

Hero copy:

- Main headline: "Cala tabela, mecz po meczu"
- Supporting copy: explain that the animation replays how the standings changed after each settled match.
- Primary action: replay the table animation.
- Secondary action: login for personal recap.

Small stats in the hero can summarize:

- settled matches count,
- player count,
- final leader,
- strongest movement or biggest finish.

### Animated Full Table

This replaces the static full table. There should not be a duplicate final table below the chart.

Chart behavior:

- X axis: settled matches in chronological order.
- Y axis: ranking position, with `#1` at the top.
- Every active player appears as a line.
- The whole animation draws lines from first settled match to final settled match.
- Final labels appear at the end for at least the top 5.
- The chart remains readable by making non-focused players thinner and lower opacity.
- The chart should support reduced-motion users with a static final state and a replay button that does not auto-animate.

Filters:

- `Wszyscy`
- `Podium`
- `Moj przebieg` for the authenticated player
- `Najwiekszy skok`
- `Najwiekszy spadek`
- selected players multi-select

Selected players should not hide all context by default. Recommended behavior: keep all lines visible as context, strengthen selected lines, and dim the rest. A "selected only" mode can be added later if users need it.

### Global Curiosities

Below the animation, show a larger set of short global facts. These should read like small stories, not raw metrics.

Initial fact pool:

- biggest climb after one match or round,
- biggest drop after one match or round,
- most exact-score-heavy match,
- match with many correct outcomes but no exact scores,
- player with most draw hits,
- strongest knockout-stage finish,
- most consistent player by low missed-prediction count,
- most profitable common scoreline pattern,
- most volatile player by position movement,
- closest podium miss,
- longest exact-score streak,
- most "one goal away" predictions.

The page can render 8-12 facts, ordered by interest and available data. Facts should be positive, playful, or neutral. Avoid public copy that feels like shaming.

### Personal Recap After Login

After login, each player can see a personal recap. Every player must get something about themselves, even if they finished low in the table.

Personal recap should pick 3-5 facts from a larger pool:

- best moment by points gained after a match or round,
- final rank and point total,
- biggest climb,
- best phase,
- strongest prediction pattern,
- most exact hits,
- most correct outcomes,
- draw specialist,
- knockout specialist,
- group-stage specialist,
- one-goal-away unlucky streak,
- favorite scoreline,
- most active or most complete prediction record,
- comparison to final winner,
- best match prediction.

Tone rule: facts should be framed as a story. For example, prefer "Najlepszy finisz w fazie pucharowej" over "Niskie miejsce".

## Information Architecture

Public page order:

1. Header with brand and login.
2. Hero with summary stats and animated full-table chart.
3. Global curiosities grid.
4. Personal recap teaser and login CTA.
5. Footer/status area if needed.

Authenticated flow:

1. Login remains available at `/login`.
2. After login, normal app functions remain available: dashboard, matches, ranking, profile, admin for admins.
3. The final summary page can expose "Moj recap" either on the public page when authenticated or via a new authenticated route.

Recommended routes:

- `/` renders final summary page after tournament completion.
- `/login` remains unchanged.
- `/ranking` can keep the current detailed ranking page for authenticated users.
- `/recap` or an in-page authenticated section renders the personal recap.

## Data Model And API

The frontend should not derive all facts from raw match/prediction lists. Add a dedicated backend endpoint for the final summary.

Recommended public endpoint:

- `GET /api/summary/final`

Response shape:

```ts
interface FinalSummaryResponse {
  stats: {
    settledMatchesCount: number
    activePlayersCount: number
    finalLeaderUserId: string
    finalLeaderDisplayName: string
  }
  positionSeries: FinalRankingPositionSeries[]
  finalTop: FinalRankingEntry[]
  globalFacts: FinalSummaryFact[]
}

interface FinalRankingPositionSeries {
  userId: string
  displayName: string
  avatarUrl?: string | null
  finalPosition: number
  finalPoints: number
  points: FinalRankingPositionPoint[]
}

interface FinalRankingPositionPoint {
  matchId: string
  matchNumber: number
  matchLabel: string
  snapshotAtUtc: string
  position: number
  totalPoints: number
}

interface FinalRankingEntry {
  userId: string
  displayName: string
  finalPosition: number
  totalPoints: number
  exactScoreHits: number
  correctOutcomeHits: number
  predictionsCount: number
}

interface FinalSummaryFact {
  id: string
  label: string
  title: string
  description: string
  relatedUserIds?: string[]
  relatedMatchIds?: string[]
}
```

Recommended authenticated endpoint:

- `GET /api/summary/final/me`

Response shape:

```ts
interface PersonalFinalSummaryResponse {
  userId: string
  displayName: string
  finalPosition: number
  totalPoints: number
  exactScoreHits: number
  correctOutcomeHits: number
  predictionsCount: number
  personalFacts: FinalSummaryFact[]
  highlightedMatchIds: string[]
}
```

The backend can reuse ranking snapshots where possible. If existing snapshots do not represent every settled match in a stable way, the summary service should rebuild the sequence using the same ordering and tie-breakers as `LeaderboardBuilder`.

## Components

New frontend components:

- `FinalSummaryPage`
- `FinalRankingStoryChart`
- `FinalSummaryFactGrid`
- `PersonalFinalRecap`
- `PlayerMultiSelect`

Reuse existing components:

- `PageShell`
- `Panel`
- `SectionHeading`
- `UserAvatar`
- current chart styling patterns from `RankingProgressChart`

The new chart should share helper functions with the existing ranking progress chart if that stays simple. If position animation logic becomes large, keep it separate to avoid making `RankingProgressChart` harder to maintain.

## Animation Details

Use Recharts if it can support the needed position-line animation cleanly. If Recharts animation is too limiting, use SVG paths generated from normalized points.

Animation requirements:

- replayable,
- pausable enough that users can inspect final state,
- no layout shift,
- mobile scroll or compressed chart for many matches,
- stable colors per player,
- final labels do not overlap on common desktop and mobile sizes,
- `prefers-reduced-motion` supported.

For many players, use full context plus focus:

- all players visible as low-opacity lines,
- selected players high opacity and thicker,
- selected player chips shown below chart,
- top final labels shown; full final order available through hover/tap tooltip rather than a static table.

## Error, Empty, And Loading States

Loading:

- show the page shell immediately,
- chart panel shows "Ladowanie historii tabeli",
- facts panel shows skeleton or compact loading state.

Empty:

- if there are no settled matches, fall back to current public home or show "Podsumowanie pojawi sie po rozliczeniu turnieju".

Error:

- show an inline alert in the chart panel,
- keep login CTA available,
- do not block authenticated app navigation.

## Testing And Validation

Backend tests:

- final summary service respects ranking tie-breakers,
- position history is sorted by settled match order,
- global facts are deterministic for a fixed dataset,
- personal recap returns at least one fact for every active player with data,
- public endpoint does not expose sensitive fields.

Frontend tests:

- final summary page renders public animation shell,
- selected-player filter highlights chosen players,
- no static full table appears below the chart,
- login CTA routes to `/login`,
- reduced-motion mode renders a stable chart state.

Manual checks:

- desktop and mobile screenshots,
- long player names,
- 20+ players,
- 70+ matches,
- empty/error states.

## Open Decisions

- Exact fact ranking order can be tuned after seeing production data.
- Whether `/recap` is a separate route or an authenticated section on `/` can be decided during implementation.
- Whether to expose final order through chart tooltip, side drawer, or collapsible panel can be decided after first working chart prototype.
