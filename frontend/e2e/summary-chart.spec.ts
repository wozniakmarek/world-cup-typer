import { expect, test } from '@playwright/test'
import type { FinalRankingPositionSeries } from '../src/api/types'
import { buildFinalChartRows, getBiggestClimbUserIds, getBiggestDropUserIds } from '../src/features/summary/summaryChart'

const createSeries = (
  overrides: Partial<FinalRankingPositionSeries> & Pick<FinalRankingPositionSeries, 'userId' | 'displayName' | 'points'>,
): FinalRankingPositionSeries => ({
  avatarUrl: null,
  finalPosition: 1,
  finalPoints: 0,
  isCurrentUser: false,
  ...overrides,
})

test('summary chart helpers preserve backend point order for rows and movement', () => {
  const series: FinalRankingPositionSeries[] = [
    createSeries({
      userId: 'climber',
      displayName: 'Climber',
      points: [
        {
          matchId: 'late-number-first',
          matchNumber: 20,
          matchLabel: 'Później w numeracji',
          snapshotAtUtc: '2026-06-12T18:00:00Z',
          position: 8,
          totalPoints: 1,
        },
        {
          matchId: 'early-number-second',
          matchNumber: 2,
          matchLabel: 'Wcześniej w numeracji',
          snapshotAtUtc: '2026-06-11T18:00:00Z',
          position: 2,
          totalPoints: 4,
        },
      ],
    }),
    createSeries({
      userId: 'dropper',
      displayName: 'Dropper',
      points: [
        {
          matchId: 'late-number-first',
          matchNumber: 20,
          matchLabel: 'Później w numeracji',
          snapshotAtUtc: '2026-06-12T18:00:00Z',
          position: 1,
          totalPoints: 5,
        },
        {
          matchId: 'early-number-second',
          matchNumber: 2,
          matchLabel: 'Wcześniej w numeracji',
          snapshotAtUtc: '2026-06-11T18:00:00Z',
          position: 7,
          totalPoints: 5,
        },
      ],
    }),
  ]

  expect(buildFinalChartRows(series).map((row) => row.matchId)).toEqual(['late-number-first', 'early-number-second'])
  expect(getBiggestClimbUserIds(series)).toEqual(['climber'])
  expect(getBiggestDropUserIds(series)).toEqual(['dropper'])
})
